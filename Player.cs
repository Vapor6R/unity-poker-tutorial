using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class Player : MonoBehaviour
{
public Player player;

public string playerName;
    public List<Card> hand = new List<Card>();
public Transform handPosition; // The position where the player's hand will be placed
public List<Card> allCards = new List<Card>();
    private float cardSpacing = 01.5f;
	public List<Card> communityCards = new List<Card>();
	public List<Card> holeCards = new List<Card>();
    public void AddCardToHand(Card card)
    {
        hand.Add(card);
		           UpdateHandPosition();
    }
    private void Start()
    {

        HandStrength handStrength = EvaluateHand(hand);
        Debug.Log("Player Hand Rank: " + handStrength);
    }
    public void ClearHand()
    {
        hand.Clear();
		UpdateHandPosition();
    }
	    public void AddCommunityCards(List<Card> communityCards)
    {
        hand.AddRange(communityCards);
    }
	 private void UpdateHandPosition()
    {
        // Calculate the initial position of the first card
        float handWidth = (hand.Count - 1) * cardSpacing;
        Vector3 startPosition = handPosition.position - new Vector3(handWidth / 2f, 0f, 0f);

        // Position each card in the hand
        for (int i = 0; i < hand.Count; i++)
        {
            Vector3 cardPosition = startPosition + new Vector3(i * cardSpacing, 0f, 0f);
            hand[i].transform.position = cardPosition;
        }
    }
	  public void AddHoleCard(Card card)
    {
        holeCards.Add(card);
    }

    public void AddCommunityCard(Card card)
    {
        communityCards.Add(card);
    }

    public void ClearHoleCards()
    {
        holeCards.Clear();
    }

    public void ClearCommunityCards()
    {
        communityCards.Clear();
    }

    private HandStrength EvaluateHand(List<Card> hand)
    {
            List<Card> allCards = new List<Card>();
    allCards.AddRange(hand);
    allCards.AddRange(communityCards);// Sort the hand in descending order of ranks
        hand.Sort((a, b) => b.GetRank().CompareTo(a.GetRank()));

        if (IsRoyalFlush(allCards))
        {
            return HandStrength.RoyalFlush;
        }
        else if (IsStraightFlush(allCards))
        {
            return HandStrength.StraightFlush;
        }
        else if (IsFourOfAKind(allCards))
        {
            return HandStrength.FourOfAKind;
        }
        else if (IsFullHouse(allCards))
        {
            return HandStrength.FullHouse;
        }
        else if (IsFlush(allCards))
        {
            return HandStrength.Flush;
        }
        else if (IsStraight(allCards))
        {
            return HandStrength.Straight;
        }
        else if (IsThreeOfAKind(allCards))
        {
            return HandStrength.ThreeOfAKind;
        }
        else if (IsTwoPair(allCards))
        {
            return HandStrength.TwoPair;
        }
        else if (IsOnePair(allCards))
        {
            return HandStrength.OnePair;
        }
        else
        {
            return HandStrength.HighCard;
        }
    }

    private bool IsRoyalFlush(List<Card> hand)
    {
        return IsStraightFlush(hand) && hand.Any(card => card.GetRank() == 12);
    }

    private bool IsStraightFlush(List<Card> hand)
    {
        return IsFlush(hand) && IsStraight(hand);
    }

    private bool IsFourOfAKind(List<Card> hand)
    {
        for (int i = 0; i <= hand.Count - 4; i++)
        {
            if (hand[i].GetRank() == hand[i + 1].GetRank() &&
                hand[i].GetRank() == hand[i + 2].GetRank() &&
                hand[i].GetRank() == hand[i + 3].GetRank())
            {
                return true;
            }
        }
        return false;
    }

    private bool IsFullHouse(List<Card> hand)
    {
        return IsThreeOfAKind(hand) && IsOnePair(hand);
    }

    private bool IsFlush(List<Card> hand)
    {
        return hand.All(card => card.GetSuit() == hand[0].GetSuit());
    }

    private bool IsStraight(List<Card> hand)
    {
        for (int i = 0; i < hand.Count - 1; i++)
        {
            if (hand[i].GetRank() == hand[i + 1].GetRank() + 1)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsThreeOfAKind(List<Card> hand)
    {
        for (int i = 0; i <= hand.Count - 3; i++)
        {
            if (hand[i].GetRank() == hand[i + 1].GetRank() &&
                hand[i].GetRank() == hand[i + 2].GetRank())
            {
                return true;
            }
        }
        return false;
    }

    private bool IsTwoPair(List<Card> hand)
    {
        int pairCount = 0;
        for (int i = 0; i <= hand.Count - 2; i++)
        {
            if (hand[i].GetRank() == hand[i + 1].GetRank())
            {
                pairCount++;
                i++;
            }
        }
        return pairCount == 2;
    }

    private bool IsOnePair(List<Card> hand)
    {
        for (int i = 0; i <= hand.Count - 2; i++)
        {
            if (hand[i].GetRank() == hand[i + 1].GetRank())
            {
                return true;
            }
        }
        return false;
    }
}

public enum HandStrength
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

