using System;
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
public static class HandEvaluator
{

    public static HandRank EvaluateBestHand(List<Card> playerCards)
    {
        if (playerCards == null || playerCards.Count != 7)
        {
            throw new ArgumentException("Exactly 7 cards are required to evaluate the best poker hand.");
        }

        List<List<Card>> fiveCardCombinations = GetFiveCardCombinations(playerCards);
        HandRank bestHandRank = HandRank.HighCard;

        foreach (var combination in fiveCardCombinations)
        {
            HandRank currentHandRank = EvaluateHand(combination);
            if (currentHandRank > bestHandRank)
            {
                bestHandRank = currentHandRank;
            }
        }

        return bestHandRank;
    }

    private static List<List<Card>> GetFiveCardCombinations(List<Card> cards)
    {
        List<List<Card>> combinations = new List<List<Card>>();
        int[] indices = { 0, 1, 2, 3, 4, 5, 6 };
        var result = new int[5];

        foreach (var combination in Combinations(indices, 5))
        {
            List<Card> fiveCards = new List<Card>();
            foreach (var index in combination)
            {
                fiveCards.Add(cards[index]);
            }
            combinations.Add(fiveCards);
        }

        return combinations;
    }

    private static IEnumerable<int[]> Combinations(int[] set, int k)
    {
        int[] result = new int[k];
        Stack<int> stack = new Stack<int>();
        stack.Push(0);

        while (stack.Count > 0)
        {
            int index = stack.Count - 1;
            int value = stack.Pop();

            while (value < set.Length)
            {
                result[index++] = set[value++];
                stack.Push(value);

                if (index == k)
                {
                    yield return (int[])result.Clone();
                    break;
                }
            }
        }
    }

    private static HandRank EvaluateHand(List<Card> cards)
    {
        // The cards must be sorted by rank to evaluate straights and flushes
        cards = cards.OrderBy(card => card.rank).ToList();

        bool isFlush = cards.All(card => card.suit == cards[0].suit);
        bool isStraight = IsStraight(cards);

        if (isFlush && isStraight)
        {
            if (cards.Last().rank == Rank.Ace)
                return HandRank.RoyalFlush;
            else
                return HandRank.StraightFlush;
        }

        var rankGroups = cards.GroupBy(card => card.rank).OrderByDescending(group => group.Count()).ToList();
        var counts = rankGroups.Select(group => group.Count()).ToList();

        if (counts.SequenceEqual(new[] { 4, 1 }))
            return HandRank.FourOfAKind;

        if (counts.SequenceEqual(new[] { 3, 2 }))
            return HandRank.FullHouse;

        if (isFlush)
            return HandRank.Flush;

        if (isStraight)
            return HandRank.Straight;

        if (counts.SequenceEqual(new[] { 3, 1, 1 }))
            return HandRank.ThreeOfAKind;

        if (counts.SequenceEqual(new[] { 2, 2, 1 }))
            return HandRank.TwoPair;

        if (counts.SequenceEqual(new[] { 2, 1, 1, 1 }))
            return HandRank.OnePair;

        return HandRank.HighCard;
    }

    private static bool IsStraight(List<Card> cards)
    {
        for (int i = 0; i < cards.Count - 1; i++)
        {
            if (cards[i + 1].rank - cards[i].rank != 1)
            {
                return false;
            }
        }

        // Check for Ace-low straight (A-2-3-4-5)
        if (cards[0].rank == Rank.Ace &&
            cards[1].rank == Rank.Two &&
            cards[2].rank == Rank.Three &&
            cards[3].rank == Rank.Four &&
            cards[4].rank == Rank.Five)
        {
            return true;
        }

        return true;
    }
}