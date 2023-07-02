using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    public GameObject cardPrefab;
    public Sprite[] cardSprites;

    private List<Card> deck = new List<Card>();

    private void Awake()
    {
        // Create the deck of cards
        for (int i = 0; i < cardSprites.Length; i++)
        {
            int rank = i % 13; //example :2 mod 13 = 2 / 13mod13=0
            int suit = i / 13; // 0 / 13= 0 

// Output the suit name
Debug.Log("Card belongs to suit " + suit + " (cardrank=" + rank + ")");

            GameObject cardObject = Instantiate(cardPrefab, transform);
            Card card = cardObject.GetComponent<Card>();

            card.SetSprite(cardSprites[i]);
            card.SetRankSuit(rank, suit);

            deck.Add(card);
        }

        ShuffleDeck();
    }

    private void ShuffleDeck()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Card temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    public Card DealCard()
    {
        if (deck.Count > 0)
        {
            Card card = deck[0];
            deck.RemoveAt(0);
            return card;
        }

        Debug.LogWarning("Deck is empty!");
        return null;
    }
}
