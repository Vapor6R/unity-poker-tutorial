using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
public class Player : MonoBehaviour
{
public Player player;

public string playerName;
    public List<Card> hand = new List<Card>();
public Transform handPosition; // The position where the player's hand will be placed
public List<Card> allhands = new List<Card>();
    private float cardSpacing = 170f;
	public List<Card> communityCards = new List<Card>();
	public List<Card> holeCards = new List<Card>();
  public Text handRankText;

    public void AddCardToHand(Card card)
    {
        hand.Add(card);
		allhands.Add(card);
		           UpdateHandPosition();
    }
    public void Awake()
	{
	}
	
	public void Update()
    {
		
		
	
	Debug.Log("Contents of allCards:");

foreach (Card card in allhands)
{
    Debug.Log(card.GetRank() + " of " + card.GetSuit());
}
		         HandStrength handStrength = EvaluateHand(allhands);
        Debug.Log("Player Hand Rank: " + handStrength);
handRankText.text = handStrength.ToString();

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
	public void AddCOMTOALL(List<Card> communityCards)
    {
        hand.AddRange(allhands);
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
	 public void AddCOMTOALL(Card card)
    {
        
		allhands.Add(card);
    }


    public void ClearHoleCards()
    {
        holeCards.Clear();
    }

    public void ClearCommunityCards()
    {
        communityCards.Clear();
    }


 private HandStrength EvaluateHand(List<Card> allhands)
    {
            
        allhands.Sort((a, b) => b.GetRank().CompareTo(a.GetRank()));
if (IsRoyalFlush(allhands))
        {
            return HandStrength.RoyalFlush;
        }
        else if (IsStraightFlush(allhands))
        {
            return HandStrength.StraightFlush;
        }
        else if (IsFourOfAKind(allhands))
        {
            return HandStrength.FourOfAKind;
        }
        else if (IsFullHouse(allhands))
        {
            return HandStrength.FullHouse;
        }
        else if (IsFlush(allhands))
        {
            return HandStrength.Flush;
        }
        else if (IsStraight(allhands))
        {
            return HandStrength.Straight;
        }
        else if (IsThreeOfAKind(allhands))
        {
            return HandStrength.ThreeOfAKind;
        }
        else if (IsTwoPair(allhands))
        {
            return HandStrength.TwoPair;
        }
        else if (IsOnePair(allhands))
        {
            return HandStrength.OnePair;
        }
        else
        {
            return HandStrength.HighCard;
        }
    }

    private bool IsRoyalFlush(List<Card> allhands)
    {
        return IsStraightFlush(allhands) && allhands.Any(card => card.GetRank() == 12);
    }

    private bool IsStraightFlush(List<Card> allhands)
    {
        return IsFlush(allhands) && IsStraight(allhands);
    }

    private bool IsFourOfAKind(List<Card> allhands)
    {
        for (int i = 0; i <= allhands.Count - 4; i++)
        {
            if (allhands[i].GetRank() == allhands[i + 1].GetRank() &&
                allhands[i].GetRank() == allhands[i + 2].GetRank() &&
                allhands[i].GetRank() == allhands[i + 3].GetRank())
            {
                return true;
            }
        }
        return false;
    }

    private bool IsFullHouse(List<Card> allhands)
    {
            if (IsThreeOfAKind(allhands))
    {
        List<Card> remainingCards = RemoveThreeOfAKind(allhands);
        return IsOnePair(remainingCards);
    }
    return false;
}

private List<Card> RemoveThreeOfAKind(List<Card> allhands)
{
    Dictionary<int, int> rankCounts = new Dictionary<int, int>();

    // Count the occurrences of each rank
    foreach (Card card in allhands)
    {
        if (!rankCounts.ContainsKey(card.GetRank()))
        {
            rankCounts[card.GetRank()] = 0;
        }
        rankCounts[card.GetRank()]++;
    }

    // Remove the three of a kind cards from the list
    List<Card> remainingCards = new List<Card>();
    foreach (Card card in allhands)
    {
        if (rankCounts[card.GetRank()] != 3)
        {
            remainingCards.Add(card);
        }
    }

    return remainingCards;
}


    private bool IsFlush(List<Card> allhands)
    {
        return allhands.All(card => card.GetSuit() == allhands[0].GetSuit());
    }

    private bool IsStraight(List<Card> allhands)
    {
        for (int i = 0; i < allhands.Count - 1; i++)
        {
            if (allhands[i].GetRank() != allhands[i + 1].GetRank() + 1)
            {
                return false;
            }
        }
        return true;
    }

    private bool IsThreeOfAKind(List<Card> allhands)
    {
        for (int i = 0; i <= allhands.Count - 3; i++)
        {
            if (allhands[i].GetRank() == allhands[i + 1].GetRank() &&
                allhands[i].GetRank() == allhands[i + 2].GetRank())
            {
                return true;
            }
        }
        return false;
    }

    private bool IsTwoPair(List<Card> allhands)
    {
        int pairCount = 0;
        for (int i = 0; i <= allhands.Count - 2; i++)
        {
            if (allhands[i].GetRank() == allhands[i + 1].GetRank())
            {
                pairCount++;
                i++;
            }
        }
        return pairCount == 2;
    }

    private bool IsOnePair(List<Card> allhands)
    {
        for (int i = 0; i <= allhands.Count - 2; i++)
        {
            if (allhands[i].GetRank() == allhands[i + 1].GetRank())
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
