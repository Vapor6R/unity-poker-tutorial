using System.Collections.Generic;
using UnityEngine;

public class CardDistribution : MonoBehaviour
{
    public Deck deck;
    public List<Player> players = new List<Player>();

    public int cardsPerPlayer = 2;


    private void Start()
    {
        DistributeCards();
		
    }

    private void DistributeCards()
    {
        foreach (Player player in players)
        {
            for (int i = 0; i < cardsPerPlayer; i++)
            {
                Card card = deck.DealCard();
                if (card != null)
                {
                    player.AddCardToHand(card);
                }
            }
        }
    }
	
}
