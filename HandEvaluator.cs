using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum HandRank
{
    None,
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

public struct HandEvaluation : IComparable<HandEvaluation>
{
    public HandRank Rank;        // Primary hand rank (e.g., Pair, Flush)
    public List<Rank> Kickers;   // Kickers for tie-breaking

    public int CompareTo(HandEvaluation other)
{
    if (Rank != other.Rank)
    {
        return Rank.CompareTo(other.Rank);
    }

    for (int i = 0; i < Math.Min(Kickers.Count, other.Kickers.Count); i++)
    {
        int comparison = Kickers[i].CompareTo(other.Kickers[i]);
        if (comparison != 0)
        {
            return comparison;
        }
    }

    return 0; // Hands are identical
}
}

public static class HandEvaluator
{
    public static HandEvaluation EvaluateBestHand(List<Card> cards)
    {
        List<List<Card>> fiveCardCombinations = GetFiveCardCombinations(cards);
        HandEvaluation bestHand = new HandEvaluation { Rank = HandRank.HighCard, Kickers = new List<Rank>() };

        foreach (var combination in fiveCardCombinations)
        {
            HandEvaluation currentHand = EvaluateHandWithKickers(combination);
            if (currentHand.CompareTo(bestHand) > 0)
            {
                bestHand = currentHand;
            }
        }

        return bestHand;
    }

    private static List<List<Card>> GetFiveCardCombinations(List<Card> cards)
    {
        List<List<Card>> combinations = new List<List<Card>>();
        int cardCount = cards.Count;

        int[] indices = Enumerable.Range(0, cardCount).ToArray();
        foreach (var combination in Combinations(indices, 5))
        {
            combinations.Add(combination.Select(index => cards[index]).ToList());
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
    }public static HandEvaluation EvaluateHandWithKickers(List<Card> cards)
{
    // Sort cards by rank in descending order
    cards = cards.OrderByDescending(card => card.rank).ToList();

    HandEvaluation handEvaluation = new HandEvaluation
    {
        Kickers = new List<Rank>()
    };

    // Check for Flush and Straight
    bool isFlush = cards.All(card => card.suit == cards[0].suit);
    bool isStraight = IsStraight(cards);

    // 1. Royal Flush or Straight Flush
    if (isFlush && isStraight)
    {
        handEvaluation.Rank = cards.Last().rank == Rank.Ace ? HandRank.RoyalFlush : HandRank.StraightFlush;
        handEvaluation.Kickers.Add(cards.Max(card => card.rank)); // Add Ace or highest card for StraightFlush
        return handEvaluation;
    }

    // Group cards by rank and count occurrences
    var rankGroups = cards.GroupBy(card => card.rank)
                          .OrderByDescending(group => group.Count()) // Group with highest frequency first
                          .ThenByDescending(group => group.Key)      // Break ties with higher rank
                          .ToList();

    var counts = rankGroups.Select(group => group.Count()).ToList();

    // 2. Four of a Kind
    if (counts.SequenceEqual(new[] { 4, 1 }))
    {
        handEvaluation.Rank = HandRank.FourOfAKind;
        handEvaluation.Kickers.Add(rankGroups[0].Key); // Four of a Kind
        handEvaluation.Kickers.Add(rankGroups[1].Key); // Kicker
        return handEvaluation;
    }

    // 3. Full House
    if (counts.SequenceEqual(new[] { 3, 2 }))
    {
        handEvaluation.Rank = HandRank.FullHouse;
        handEvaluation.Kickers.Add(rankGroups[0].Key); // Three of a Kind
        handEvaluation.Kickers.Add(rankGroups[1].Key); // Pair
        return handEvaluation;
    }

    // 4. Flush
    if (isFlush)
    {
        handEvaluation.Rank = HandRank.Flush;
        handEvaluation.Kickers.AddRange(cards.Select(card => card.rank)); // All cards as kickers, already sorted
        return handEvaluation;
    }

    // 5. Straight
    if (isStraight)
    {
        handEvaluation.Rank = HandRank.Straight;
        handEvaluation.Kickers.Add(cards.Max(card => card.rank)); // Highest card for Straight
        return handEvaluation;
    }

    // 6. Three of a Kind
    if (counts.SequenceEqual(new[] { 3, 1, 1 }))
    {
        handEvaluation.Rank = HandRank.ThreeOfAKind;
        handEvaluation.Kickers.Add(rankGroups[0].Key); // Three of a Kind
        handEvaluation.Kickers.AddRange(rankGroups.Skip(1).Select(group => group.Key)); // Remaining kickers
        return handEvaluation;
    }

    // 7. Two Pair
    if (counts.SequenceEqual(new[] { 2, 2, 1 }))
    {
        handEvaluation.Rank = HandRank.TwoPair;
        handEvaluation.Kickers.Add(rankGroups[0].Key); // Highest pair
        handEvaluation.Kickers.Add(rankGroups[1].Key); // Second-highest pair
        handEvaluation.Kickers.Add(rankGroups[2].Key); // Remaining kicker
        return handEvaluation;
    }

    // 8. One Pair
    if (counts.SequenceEqual(new[] { 2, 1, 1, 1 }))
    {
        handEvaluation.Rank = HandRank.OnePair;
        handEvaluation.Kickers.Add(rankGroups[0].Key); // Pair

        // Add the remaining cards as kickers in descending order
        handEvaluation.Kickers.AddRange(cards.Where(card => card.rank != rankGroups[0].Key)
                                             .Select(card => card.rank)
                                             .OrderByDescending(rank => rank)); // Ensure descending order for kickers
        return handEvaluation;
    }

    // 9. High Card (default case)
    handEvaluation.Rank = HandRank.HighCard;
    handEvaluation.Kickers.AddRange(cards.Select(card => card.rank).Take(5)); // Top 5 cards as kickers
    return handEvaluation;
}


   private static bool IsStraight(List<Card> cards)
{
    // Sort cards by rank first
    cards = cards.OrderBy(card => card.rank).ToList();

    // Check if the cards form a standard straight (e.g., 2, 3, 4, 5, 6)
    for (int i = 0; i < cards.Count - 1; i++)
    {
        if (cards[i + 1].rank - cards[i].rank != 1)
        {
            return false;
        }
    }

    // Special case: Ace-low straight (A, 2, 3, 4, 5)
    if (cards[0].rank == Rank.Ace &&
        cards[1].rank == Rank.Two &&
        cards[2].rank == Rank.Three &&
        cards[3].rank == Rank.Four &&
        cards[4].rank == Rank.Five)
    {
        return true;
    }

    return true; // If standard straight is valid
}

}
