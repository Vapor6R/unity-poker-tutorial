using System.Collections.Generic;
using UnityEngine;

public class CommunityCardDistribution : MonoBehaviour
{
    public Deck deck;
    public List<Player> players = new List<Player>();
    public int communityCardsCount = 5;
    public Transform communityCardsParent;
    public float cardSpacing = 1.0f;

    private List<Card> communityCards = new List<Card>();

    private void Start()
    {
        DealCommunityCards();
        
	
		PositionCommunityCards();
    }

    private void DealCommunityCards()
    {
        for (int i = 0; i < communityCardsCount; i++)
        {
            Card card = deck.DealCard();
            if (card != null)
            {
                communityCards.Add(card);
            }
        }

        foreach (Player player in players)
        {
            player.ClearCommunityCards();
            foreach (Card card in communityCards)
            {
                player.AddCommunityCard(card);
            }
        }
    }

    private void PositionCommunityCards()
    {

		        float handWidth = (communityCards.Count - 1) * cardSpacing;
        Vector3 startPosition = communityCardsParent.position - new Vector3(handWidth / 2f, 0f, 0f);

        // Position each card in the hand
        for (int i = 0; i < communityCards.Count; i++)
        {
            Vector3 cardPosition = startPosition + new Vector3(i * cardSpacing, 0f, 0f);
            communityCards[i].transform.position = cardPosition;
        }
		Debug.Log("Number of community cards: " + communityCards.Count);
}}

