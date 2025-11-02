using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum HandRank
{
    HighCard = 1,
    OnePair = 2,
    TwoPair = 3,
    ThreeOfAKind = 4,
    Straight = 5,
    Flush = 6,
    FullHouse = 7,
    FourOfAKind = 8,
    StraightFlush = 9,
    RoyalFlush = 10
}

public static class HandEvaluator
{
    public static (HandRank rank, List<Card> bestCards) Evaluate(List<Card> cards7)
    {
        if (cards7 == null || cards7.Count != 7)
        {
            Debug.LogError("HandEvaluator: Need EXACTLY 7 cards!");
            return (HandRank.HighCard, new List<Card>());
        }

        // Sort cards by rank (high → low)
        var sorted = cards7.OrderByDescending(c => (int)c.rank).ToList();

        // Group by rank
        var groups = sorted.GroupBy(c => c.rank)
                           .OrderByDescending(g => g.Count())      // bigger groups first
                           .ThenByDescending(g => (int)g.Key)      // higher rank groups first
                           .ToList();

        // Group by suit
        var suitGroups = sorted.GroupBy(c => c.suit)
                               .Where(g => g.Count() >= 5)
                               .ToList();

        // ✅ CHECK: Flush / Straight Flush
        if (suitGroups.Count > 0)
        {
            var flushCards = suitGroups.First().OrderByDescending(c => c.rank).ToList();

            var straightFlush = FindStraight(flushCards);
            if (straightFlush != null)
            {
                // Royal Flush
                if (straightFlush.First().rank == Rank.Ace &&
                    straightFlush.Last().rank == Rank.Ten)
                {
                    return (HandRank.RoyalFlush, straightFlush);
                }

                return (HandRank.StraightFlush, straightFlush);
            }
        }

        // ✅ CHECK: Four of a kind
       var four = groups.FirstOrDefault(g => g.Count() == 4);
if (four != null)
{
    var best = four.ToList();
    
    // ✅ Get highest kicker that's not part of the quad
    var kicker = sorted.FirstOrDefault(c => c.rank != four.Key);
    if (kicker != null)
        best.Add(kicker);
    
    return (HandRank.FourOfAKind, best);
}

        // ✅ CHECK: Full House
        var three = groups.Where(g => g.Count() == 3).ToList();
        if (three.Count >= 1)
        {
            var pairGroup = groups.Where(g => g.Count() >= 2 && g.Key != three.First().Key)
                                  .OrderByDescending(g => g.Key)
                                  .FirstOrDefault();

            if (pairGroup != null)
            {
                var best = three.First().Take(3).ToList();
                best.AddRange(pairGroup.Take(2));
                return (HandRank.FullHouse, best);
            }
        }

        // ✅ CHECK: Flush
        if (suitGroups.Count > 0)
        {
            var flush = suitGroups.First()
                                  .OrderByDescending(c => c.rank)
                                  .Take(5)
                                  .ToList();

            return (HandRank.Flush, flush);
        }

        // ✅ CHECK: Straight
        var straight5 = FindStraight(sorted);
        if (straight5 != null)
        {
            return (HandRank.Straight, straight5);
        }

        // ✅ CHECK: Three of a kind
        if (three.Count > 0)
        {
            var best = three.First().Take(3).ToList();
            best.AddRange(sorted.Where(c => c.rank != three.First().Key).Take(2));
            return (HandRank.ThreeOfAKind, best);
        }

        // ✅ CHECK: Two Pair
       var pairs = groups.Where(g => g.Count() == 2).ToList();
if (pairs.Count >= 2)
{
    // ✅ Get top 2 pairs by rank
    var top2 = pairs.OrderByDescending(g => (int)g.Key).Take(2).ToList();
    
    var best = top2[0].Take(2).ToList();
    best.AddRange(top2[1].Take(2));

    // ✅ Get highest kicker
    var kicker = sorted.FirstOrDefault(c =>
        c.rank != top2[0].Key &&
        c.rank != top2[1].Key);
    
    if (kicker != null)
        best.Add(kicker);

    return (HandRank.TwoPair, best);
}

        // ✅ CHECK: One Pair
if (pairs.Count >= 1)
{
    // ✅ FIX: Get the HIGHEST pair (already sorted by rank in groups)
    var highestPair = pairs.OrderByDescending(g => (int)g.Key).First();
    
    var best = highestPair.Take(2).ToList();
    
    // ✅ Get kickers excluding the pair rank, sorted high to low
    best.AddRange(sorted.Where(c => c.rank != highestPair.Key)
                        .OrderByDescending(c => (int)c.rank)
                        .Take(3));
    
    return (HandRank.OnePair, best);
}

        // ✅ High Card
        return (HandRank.HighCard, sorted.Take(5).ToList());
    }

    // ----------------------------------------
    // ✅ Helper: Detect Straight in any list
    // ----------------------------------------
    private static List<Card> FindStraight(List<Card> cards)
    {
        var sorted = cards.OrderByDescending(c => c.rank)
                          .GroupBy(c => c.rank)
                          .Select(g => g.First()) // remove duplicates
                          .ToList();

        List<Card> run = new List<Card>();

        for (int i = 0; i < sorted.Count; i++)
        {
            if (run.Count == 0)
            {
                run.Add(sorted[i]);
                continue;
            }

            if ((int)sorted[i].rank == (int)run.Last().rank - 1)
            {
                run.Add(sorted[i]);

                if (run.Count == 5)
                    return new List<Card>(run);
            }
            else if ((int)sorted[i].rank != (int)run.Last().rank)
            {
                run.Clear();
                run.Add(sorted[i]);
            }
        }

        // ✅ Special case A-2-3-4-5
        bool hasAce = sorted.Any(c => c.rank == Rank.Ace);
        bool hasTwo = sorted.Any(c => c.rank == Rank.Two);
        bool hasThree = sorted.Any(c => c.rank == Rank.Three);
        bool hasFour = sorted.Any(c => c.rank == Rank.Four);
        bool hasFive = sorted.Any(c => c.rank == Rank.Five);

        if (hasAce && hasTwo && hasThree && hasFour && hasFive)
        {
            return new List<Card>
            {
                sorted.First(c => c.rank == Rank.Five),
                sorted.First(c => c.rank == Rank.Four),
                sorted.First(c => c.rank == Rank.Three),
                sorted.First(c => c.rank == Rank.Two),
                sorted.First(c => c.rank == Rank.Ace)
            };
        }

        return null;
    }
}
