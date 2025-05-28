using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum HandRank
{
	None,
    HighCard,
    OnePair,
    TwoPair,
    ThreeOfAKind,
    Straight,
    Flush,
    FullHouse,
    FourOfAKind,
    StraightFlush,
    RoyalFlush
}
public class HandEvaluator
{
    public class EvaluatedHand
    {
        public HandRank Rank;
        public List<Card> CardsInHand;  // The 5 cards that make up the hand
        public List<int> RankValues;    // Values for tie-breaking (e.g., kicker cards)

        public EvaluatedHand(HandRank rank, List<Card> cards, List<int> rankValues)
        {
            Rank = rank;
            CardsInHand = cards;
            RankValues = rankValues;
        }
    }

    public EvaluatedHand EvaluateHand(List<Card> cards)
    {
        // 7 cards => evaluate best 5 card hand
        if (cards == null || cards.Count < 5)
        {
            Debug.LogError("Not enough cards to evaluate a hand.");
            return null;
        }

        // Sort cards by rank descending for easier evaluation
        var sortedCards = cards.OrderByDescending(c => (int)c.rank).ToList();

        // Get all combinations of 5 cards out of 7 to find the best hand
        var all5CardCombos = GetCombinations(sortedCards, 5);

        EvaluatedHand bestHand = null;

        foreach (var combo in all5CardCombos)
        {
            var evaluated = EvaluateFiveCardHand(combo);
            if (bestHand == null || evaluated.Rank > bestHand.Rank ||
                (evaluated.Rank == bestHand.Rank && CompareRankValues(evaluated.RankValues, bestHand.RankValues) > 0))
            {
                bestHand = evaluated;
            }
        }

        return bestHand;
    }

    // Helper: Compare two rank value lists for tie breaking (return 1 if first is better, -1 if second better, 0 equal)
    private int CompareRankValues(List<int> a, List<int> b)
    {
        for (int i = 0; i < Mathf.Min(a.Count, b.Count); i++)
        {
            if (a[i] > b[i]) return 1;
            if (a[i] < b[i]) return -1;
        }
        return 0;
    }

    // Evaluate a 5-card hand and return its HandRank and tie-break info
    private EvaluatedHand EvaluateFiveCardHand(List<Card> hand)
    {
        var ranks = hand.Select(c => (int)c.rank).OrderByDescending(r => r).ToList();
        var suits = hand.Select(c => c.suit).ToList();

        bool isFlush = suits.Distinct().Count() == 1;
        bool isStraight = IsStraight(ranks);

        var groupedRanks = ranks.GroupBy(r => r)
                                .OrderByDescending(g => g.Count())
                                .ThenByDescending(g => g.Key)
                                .ToList();

        if (isFlush && isStraight && ranks[0] == (int)Rank.Ace)
        {
            // Royal Flush
            return new EvaluatedHand(HandRank.RoyalFlush, hand, ranks);
        }
        if (isFlush && isStraight)
        {
            // Straight Flush
            return new EvaluatedHand(HandRank.StraightFlush, hand, ranks);
        }
        if (groupedRanks[0].Count() == 4)
        {
            // Four of a Kind
            var rankValues = new List<int> { groupedRanks[0].Key, groupedRanks[1].Key };
            return new EvaluatedHand(HandRank.FourOfAKind, hand, rankValues);
        }
        if (groupedRanks[0].Count() == 3 && groupedRanks[1].Count() == 2)
        {
            // Full House
            var rankValues = new List<int> { groupedRanks[0].Key, groupedRanks[1].Key };
            return new EvaluatedHand(HandRank.FullHouse, hand, rankValues);
        }
        if (isFlush)
        {
            // Flush
            return new EvaluatedHand(HandRank.Flush, hand, ranks);
        }
        if (isStraight)
        {
            // Straight
            return new EvaluatedHand(HandRank.Straight, hand, ranks);
        }
        if (groupedRanks[0].Count() == 3)
        {
            // Three of a Kind
            var rankValues = new List<int> { groupedRanks[0].Key };
            rankValues.AddRange(groupedRanks.Skip(1).Select(g => g.Key));
            return new EvaluatedHand(HandRank.ThreeOfAKind, hand, rankValues);
        }
        if (groupedRanks[0].Count() == 2 && groupedRanks[1].Count() == 2)
        {
            // Two Pair
            var rankValues = new List<int> { groupedRanks[0].Key, groupedRanks[1].Key };
            rankValues.Add(groupedRanks[2].Key);
            return new EvaluatedHand(HandRank.TwoPair, hand, rankValues);
        }
        if (groupedRanks[0].Count() == 2)
        {
            // One Pair
            var rankValues = new List<int> { groupedRanks[0].Key };
            rankValues.AddRange(groupedRanks.Skip(1).Select(g => g.Key));
            return new EvaluatedHand(HandRank.OnePair, hand, rankValues);
        }

        // High Card
        return new EvaluatedHand(HandRank.HighCard, hand, ranks);
    }

    // Check if the list of ranks forms a straight (handles Ace-low straight too)
    private bool IsStraight(List<int> ranks)
    {
        // Remove duplicates
        var distinctRanks = ranks.Distinct().OrderByDescending(r => r).ToList();

        // Check normal straight
        if (distinctRanks.Count < 5) return false;

        for (int i = 0; i <= distinctRanks.Count - 5; i++)
        {
            if (distinctRanks[i] - distinctRanks[i + 4] == 4)
                return true;
        }

        // Check Ace-low straight (A-2-3-4-5)
        if (distinctRanks.Contains((int)Rank.Ace) &&
            distinctRanks.Contains(2) &&
            distinctRanks.Contains(3) &&
            distinctRanks.Contains(4) &&
            distinctRanks.Contains(5))
        {
            return true;
        }

        return false;
    }

    // Helper: Generate all combinations of a list taken k at a time
    private List<List<T>> GetCombinations<T>(List<T> list, int k)
    {
        var result = new List<List<T>>();
        int[] indices = new int[k];

        void Recurse(int depth, int start)
        {
            if (depth == k)
            {
                var combo = new List<T>();
                for (int i = 0; i < k; i++) combo.Add(list[indices[i]]);
                result.Add(combo);
                return;
            }

            for (int i = start; i < list.Count; i++)
            {
                indices[depth] = i;
                Recurse(depth + 1, i + 1);
            }
        }

        Recurse(0, 0);
        return result;
    }
}
