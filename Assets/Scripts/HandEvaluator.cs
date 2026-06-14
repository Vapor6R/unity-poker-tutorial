using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Evaluates Texas Hold'em hands. Picks best 5 from any 7 cards.
/// Rank order: HighCard=1 … RoyalFlush=10
/// </summary>
public static class HandEvaluator
{
    // ── Public types ──────────────────────────────────────────────────────────

    public enum HandRank
    {
        HighCard      = 1,
        OnePair       = 2,
        TwoPair       = 3,
        ThreeOfAKind  = 4,
        Straight      = 5,
        Flush         = 6,
        FullHouse     = 7,
        FourOfAKind   = 8,
        StraightFlush = 9,
        RoyalFlush    = 10
    }

    public class HandResult
    {
        public HandRank rank;
        public List<int> tiebreakers; // descending rank values used to break ties
        public List<string> bestHand; // the 5 cards chosen

        public string RankName => rank.ToString();

        public override string ToString() =>
            $"{RankName} [{string.Join(", ", bestHand)}]";
    }

    // ── Card helpers ──────────────────────────────────────────────────────────

    // Card string format from DeckManager: RankSuit  e.g. "10H", "AS", "KD"
    static int CardRank(string card)
    {
        string r = card.Substring(0, card.Length - 1);
        return r switch
        {
            "2"  => 2,  "3" => 3,  "4" => 4,  "5" => 5,
            "6"  => 6,  "7" => 7,  "8" => 8,  "9" => 9,
            "10" => 10, "J" => 11, "Q" => 12, "K" => 13, "A" => 14,
            _ => 0
        };
    }

    static char CardSuit(string card) => card[card.Length - 1];

    // ── Main entry point ──────────────────────────────────────────────────────

    /// <summary>
    /// Evaluate the best 5-card hand from up to 7 cards.
    /// </summary>
    public static HandResult Evaluate(List<string> holeCards, List<string> communityCards)
    {
        List<string> all = new List<string>(holeCards);
        all.AddRange(communityCards);
if (all.Distinct().Count() != all.Count)
    Debug.LogError($"HandEvaluator: duplicate cards! {string.Join(",", all)}");
        if (all.Count < 2)
        {
            Debug.LogError("HandEvaluator: not enough cards.");
            return null;
        }

        // Try every C(n,5) combination, keep best
        HandResult best = null;
        foreach (var combo in Combinations(all, 5))
        {
            HandResult r = EvaluateFive(combo);
            if (best == null || Compare(r, best) > 0)
                best = r;
        }
        return best;
    }

    // ── Showdown ──────────────────────────────────────────────────────────────

    public class ShowdownResult
    {
        public List<PlayerManager> winners;
        public HandResult winningHand;
    }

    /// <summary>
    /// Compare all active (non-folded) players and return winner(s).
    /// Call this on MasterClient only; broadcast result via RPC yourself.
    /// </summary>
    public static ShowdownResult DetermineWinner(
        List<PlayerManager> players,
        List<string> communityCards)
    {
        var active = players.Where(p => !p.isFolded && p.hand.Count >= 2).ToList();

        if (active.Count == 0)
        {
            Debug.LogError("ShowDown: no active players.");
            return null;
        }

        // Evaluate every player
        var results = new Dictionary<PlayerManager, HandResult>();
        foreach (var p in active)
        {
            results[p] = Evaluate(p.hand, communityCards);
            Debug.Log($"Seat {p.seatIndex}: {results[p]}");
        }

        // Find best hand
        HandResult bestHand = null;
        foreach (var r in results.Values)
            if (bestHand == null || Compare(r, bestHand) > 0)
                bestHand = r;

        // Collect winners (handles ties)
        var winners = active
            .Where(p => Compare(results[p], bestHand) == 0)
            .ToList();

        return new ShowdownResult { winners = winners, winningHand = bestHand };
    }

    // ── Comparison ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns >0 if a beats b, 0 if tie, <0 if b beats a.
    /// </summary>
    public static int Compare(HandResult a, HandResult b)
    {
        int rankCmp = a.rank.CompareTo(b.rank);
        if (rankCmp != 0) return rankCmp;

        // Same rank → compare tiebreakers lexicographically
        int len = System.Math.Min(a.tiebreakers.Count, b.tiebreakers.Count);

        for (int i = 0; i < len; i++)
        {
            int c = a.tiebreakers[i].CompareTo(b.tiebreakers[i]);
            if (c != 0) return c;
        }
        return 0; // exact tie
    }

    // ── Evaluate exactly 5 cards ──────────────────────────────────────────────

    static HandResult EvaluateFive(List<string> cards)
    {
        // Sort descending by rank
        var sorted = cards.OrderByDescending(CardRank).ToList();
        int[] ranks   = sorted.Select(CardRank).ToArray();
        char[] suits  = sorted.Select(CardSuit).ToArray();

        bool isFlush    = suits.Distinct().Count() == 1;
        bool isStraight = IsStraight(ranks, out int straightHigh);

        // Royal Flush
        if (isFlush && isStraight && straightHigh == 14)
            return MakeResult(HandRank.RoyalFlush, sorted, new List<int> { 14 });

        // Straight Flush
        if (isFlush && isStraight)
            return MakeResult(HandRank.StraightFlush, sorted, new List<int> { straightHigh });

        // Group by rank
        var groups = ranks.GroupBy(r => r)
                          .OrderByDescending(g => g.Count())
                          .ThenByDescending(g => g.Key)
                          .ToList();

        int[] counts = groups.Select(g => g.Count()).ToArray();
        int[] keys   = groups.Select(g => g.Key).ToArray();

        // Four of a Kind
        if (counts[0] == 4)
            return MakeResult(HandRank.FourOfAKind, sorted,
                new List<int> { keys[0], keys[1] });

        // Full House
        if (counts[0] == 3 && counts[1] == 2)
            return MakeResult(HandRank.FullHouse, sorted,
                new List<int> { keys[0], keys[1] });

        // Flush
        if (isFlush)
            return MakeResult(HandRank.Flush, sorted, ranks.ToList());

        // Straight
        if (isStraight)
            return MakeResult(HandRank.Straight, sorted, new List<int> { straightHigh });

        // Three of a Kind
        if (counts[0] == 3)
            return MakeResult(HandRank.ThreeOfAKind, sorted,
                new List<int> { keys[0], keys[1], keys[2] });

        // Two Pair
        if (counts[0] == 2 && counts[1] == 2)
            return MakeResult(HandRank.TwoPair, sorted,
                new List<int> { keys[0], keys[1], keys[2] });

        // One Pair
        if (counts[0] == 2)
            return MakeResult(HandRank.OnePair, sorted,
                new List<int> { keys[0], keys[1], keys[2], keys[3] });

        // High Card
        return MakeResult(HandRank.HighCard, sorted, ranks.ToList());
    }

    static bool IsStraight(int[] descRanks, out int high)
    {
        high = descRanks[0];

        // Normal straight
        bool normal = true;
        for (int i = 0; i < descRanks.Length - 1; i++)
            if (descRanks[i] - descRanks[i + 1] != 1) { normal = false; break; }
        if (normal) return true;

        // Wheel: A-2-3-4-5  (A counted as 1)
        int[] wheel = { 14, 5, 4, 3, 2 };
        if (descRanks.SequenceEqual(wheel)) { high = 5; return true; }

        return false;
    }

    static HandResult MakeResult(HandRank rank, List<string> cards, List<int> tiebreakers) =>
        new HandResult { rank = rank, tiebreakers = tiebreakers, bestHand = cards };

    // ── Combinations helper ───────────────────────────────────────────────────

    static IEnumerable<List<T>> Combinations<T>(List<T> list, int k)
    {
        if (k == 0) { yield return new List<T>(); yield break; }
        for (int i = 0; i <= list.Count - k; i++)
            foreach (var rest in Combinations(list.GetRange(i + 1, list.Count - i - 1), k - 1))
            {
                var combo = new List<T> { list[i] };
                combo.AddRange(rest);
                yield return combo;
            }
    }
}
