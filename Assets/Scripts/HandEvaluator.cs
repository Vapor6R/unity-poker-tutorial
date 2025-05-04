using System.Collections.Generic;
using System.Linq;

public enum HandRank
{
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

public class HandValue
{
    public HandRank Rank { get; set; }
    public List<int> MainRanks { get; set; } = new List<int>();
    public List<int> Kickers { get; set; } = new List<int>();
}

public static class HandEvaluator
{
    public static HandValue EvaluateHand(List<Card> cards)
    {
        var all5CardCombos = Get5CardCombinations(cards);

        HandValue bestValue = null;

        foreach (var combo in all5CardCombos)
        {
            var value = EvaluateFiveCardHand(combo);
            if (bestValue == null || CompareHands(value, bestValue) > 0)
            {
                bestValue = value;
            }
        }

        return bestValue;
    }

    private static HandValue EvaluateFiveCardHand(List<Card> hand)
    {
        var ranks = hand.Select(c => (int)c.rank).OrderByDescending(r => r).ToList();
        var suits = hand.Select(c => c.suit).ToList();

        bool isFlush = suits.Distinct().Count() == 1;
        bool isStraight = IsStraight(ranks);

        var grouped = hand.GroupBy(c => (int)c.rank)
                          .OrderByDescending(g => g.Count())
                          .ThenByDescending(g => g.Key)
                          .ToList();

        var result = new HandValue();

        if (isFlush && isStraight)
        {
            result.Rank = ranks.Max() == 14 && ranks.Contains(10) ? HandRank.RoyalFlush : HandRank.StraightFlush;
            result.MainRanks.Add(ranks.Max());
            return result;
        }

        if (grouped[0].Count() == 4)
        {
            result.Rank = HandRank.FourOfAKind;
            result.MainRanks.Add(grouped[0].Key);
            result.Kickers.Add(grouped[1].Key);
            return result;
        }

        if (grouped[0].Count() == 3 && grouped[1].Count() == 2)
        {
            result.Rank = HandRank.FullHouse;
            result.MainRanks.Add(grouped[0].Key);
            result.MainRanks.Add(grouped[1].Key);
            return result;
        }

        if (isFlush)
        {
            result.Rank = HandRank.Flush;
            result.MainRanks = ranks;
            return result;
        }

        if (isStraight)
        {
            result.Rank = HandRank.Straight;
            result.MainRanks.Add(GetHighCardForStraight(ranks));
            return result;
        }

        if (grouped[0].Count() == 3)
        {
            result.Rank = HandRank.ThreeOfAKind;
            result.MainRanks.Add(grouped[0].Key);
            result.Kickers = grouped.Skip(1).Select(g => g.Key).Take(2).ToList();
            return result;
        }

        if (grouped[0].Count() == 2 && grouped[1].Count() == 2)
        {
            result.Rank = HandRank.TwoPair;
            result.MainRanks.Add(grouped[0].Key);
            result.MainRanks.Add(grouped[1].Key);
            result.Kickers.Add(grouped[2].Key);
            return result;
        }

        if (grouped[0].Count() == 2)
        {
            result.Rank = HandRank.OnePair;
            result.MainRanks.Add(grouped[0].Key);
            result.Kickers = grouped.Skip(1).Select(g => g.Key).Take(3).ToList();
            return result;
        }

        result.Rank = HandRank.HighCard;
        result.MainRanks = ranks;
        return result;
    }

    private static bool IsStraight(List<int> ranks)
    {
        var distinctRanks = ranks.Distinct().OrderBy(r => r).ToList();

        if (distinctRanks.Contains(14))
        {
            distinctRanks.Add(1);
            distinctRanks = distinctRanks.Distinct().OrderBy(r => r).ToList();
        }

        for (int i = 0; i <= distinctRanks.Count - 5; i++)
        {
            if (distinctRanks[i + 4] - distinctRanks[i] == 4)
                return true;
        }

        return false;
    }

    private static int GetHighCardForStraight(List<int> ranks)
    {
        var distinctRanks = ranks.Distinct().OrderBy(r => r).ToList();

        if (distinctRanks.Contains(14))
        {
            distinctRanks.Add(1);
            distinctRanks = distinctRanks.Distinct().OrderBy(r => r).ToList();
        }

        for (int i = 0; i <= distinctRanks.Count - 5; i++)
        {
            if (distinctRanks[i + 4] - distinctRanks[i] == 4)
                return distinctRanks[i + 4];
        }

        return 0;
    }

    private static List<List<Card>> Get5CardCombinations(List<Card> cards)
    {
        var combinations = new List<List<Card>>();
        int n = cards.Count;

        for (int i = 0; i < n - 4; i++)
        {
            for (int j = i + 1; j < n - 3; j++)
            {
                for (int k = j + 1; k < n - 2; k++)
                {
                    for (int l = k + 1; l < n - 1; l++)
                    {
                        for (int m = l + 1; m < n; m++)
                        {
                            combinations.Add(new List<Card> {
                                cards[i], cards[j], cards[k], cards[l], cards[m]
                            });
                        }
                    }
                }
            }
        }

        return combinations;
    }

    public static int CompareHands(HandValue hv1, HandValue hv2)
    {
        if (hv1.Rank != hv2.Rank)
            return hv1.Rank.CompareTo(hv2.Rank);

        for (int i = 0; i < hv1.MainRanks.Count; i++)
        {
            if (i >= hv2.MainRanks.Count) return 1;
            if (hv1.MainRanks[i] != hv2.MainRanks[i])
                return hv1.MainRanks[i].CompareTo(hv2.MainRanks[i]);
        }

        for (int i = 0; i < hv1.Kickers.Count; i++)
        {
            if (i >= hv2.Kickers.Count) return 1;
            if (hv1.Kickers[i] != hv2.Kickers[i])
                return hv1.Kickers[i].CompareTo(hv2.Kickers[i]);
        }

        return 0;
    }
}