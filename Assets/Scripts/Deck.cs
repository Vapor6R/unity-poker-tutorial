using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class Deck : MonoBehaviourPunCallbacks
{   public int communityCardsCount = 3;
    public List<Card> communityCards = new List<Card>();
    public List<Card> cards = new List<Card>();
    public GameObject cardPrefab;
    public Transform[] CARDSPAWN;
    private bool distributed = false;
    public bool isDeckSpawned = false;
    public Transform communityCardsParent; // Add this for community cards
    public int cardsToDistributePerPlayer = 2; // Number of cards to distribute per player
private PlayerCardHandler playerCardHandler;

     // Reference to your UI GameObject
    
 public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        
    }
  
	 
	
 
	 
    




    


    // Example method for handling player actions (e.g., End Turn button click)
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("Player left: " + otherPlayer.NickName);
        
        // Optionally, you can handle additional logic here,
        // such as updating the UI to reflect the player's departure
    }
    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            CreateDeck();
        }

playerCardHandler = FindObjectOfType<PlayerCardHandler>();
 
    }

    public void CreateDeck()
    {
        cards.Clear();
        int spawnIndex = 0;

        foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
            {
                GameObject newCard = PhotonNetwork.Instantiate(cardPrefab.name, CARDSPAWN[spawnIndex].position, Quaternion.identity);
                Card cardComponent = newCard.GetComponent<Card>();
                if (cardComponent != null)
                {
                    photonView.RPC("InitializeCard", RpcTarget.AllBuffered, cardComponent.photonView.ViewID, (int)rank, (int)suit);
                    cards.Add(cardComponent);
                    Debug.Log($"Created card: {rank} of {suit}");
                }
                spawnIndex = (spawnIndex + 1) % CARDSPAWN.Length;
            }
        }

        isDeckSpawned = true;
    }

    [PunRPC]
    private void InitializeCard(int viewID, int rank, int suit)
    {
        PhotonView view = PhotonView.Find(viewID);
        if (view != null)
        {
            Card cardComponent = view.GetComponent<Card>();
            if (cardComponent != null)
            {
                cardComponent.InitializeCard((Rank)rank, (Suit)suit);
            }
        }
    }

    [PunRPC]
    private void ShuffleDeckRPC()
    {
        ShuffleDeck();
        Debug.Log("Deck shuffled on all clients.");
    }

    private void ShuffleDeck()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            Card temp = cards[i];
            int randomIndex = Random.Range(i, cards.Count);
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }

        Debug.Log("Deck shuffled.");
    }

    public Card DrawCard()
    {
        if (cards == null || cards.Count == 0)
        {
            Debug.LogWarning("No cards left to draw or deck not initialized.");
            return null;
        }

        Card drawnCard = cards[0];
        cards.RemoveAt(0);
        Debug.Log($"Card drawn: {drawnCard}");
        return drawnCard;
    }

[PunRPC]
    private void DistributeCardsRPC()
    {
 
        {
            // Find all players with the tag "player" and distribute cards to them
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject playerGO in players)
            {
                PlayerCardHandler playerHandler = playerGO.GetComponent<PlayerCardHandler>();
                if (playerHandler != null)
                {
                    for (int i = 0; i < cardsToDistributePerPlayer; i++)
                    {
                        Card drawnCard = DrawCard();
                        if (drawnCard != null)
                        {
                            // Add card to player's hand
                            playerHandler.photonView.RPC("AddCardToPlayerHandRPC", RpcTarget.All, drawnCard.photonView.ViewID);
                        }
                    }
                }
            }


        }
    }
	void restartgame()
	{
 communityCards.Clear();
	}
	
[PunRPC]
    private void DistributeAndAddCommunityCards()
    {
        Debug.Log("Distributing community cards...");
        for (int i = 0; i < communityCardsCount; i++)
        {
            Card drawnCard = DrawCard();
            if (drawnCard != null)
            {
                communityCards.Add(drawnCard);

                // Set the card's parent and position
                PhotonView cardView = drawnCard.GetComponent<PhotonView>();
                if (cardView != null)
                {
                    Debug.Log($"Community card");
					photonView.RPC("AddCommunityCardRPC", RpcTarget.AllBuffered, cardView.ViewID, i);
           
				}
            }
            else
            {
                Debug.LogError("Failed to draw a card for community cards.");
            }
        }
	
    } 

    [PunRPC]
    private void AddCommunityCardRPC(int cardViewID, int positionIndex)
    {
        PhotonView cardView = PhotonView.Find(cardViewID);
        if (cardView != null)
        {
            Card card = cardView.GetComponent<Card>();
            if (card != null && communityCardsParent != null)
            {
                Debug.Log($"Adding community card {card.rank} of {card.suit} to position {positionIndex}");
                card.transform.SetParent(communityCardsParent, false); // False to keep the local position
                 float spacing = 15.0f; // Adjust the spacing value as needed
            Vector3 cardPosition = new Vector3(positionIndex * spacing, 0, 0);
            
            // Set the local position of the card
            card.transform.localPosition = cardPosition;
                
                Debug.Log($"Community card {card.rank} of {card.suit} added at position {positionIndex}.");
            }
            else
            {
                Debug.LogError("Community card or parent is null.");
            }
        }
        else
        {
            Debug.LogError("CardView not found.");
        }
    }


    public IEnumerator DelayedDistributeCards()
    {
        yield return new WaitForSeconds(1.5f); // Adjust the delay time as needed
        photonView.RPC("DistributeCardsRPC", RpcTarget.MasterClient);
		
    }
	 public IEnumerator DelayedRestart()
    {
        yield return new WaitForSeconds(5.5f); // Adjust the delay time as needed
        
		photonView.RPC("RESTARTRPC", RpcTarget.All);
		
    }
	
	private void ClearCommunityCards()
{
    // Clear the list of community cards
    communityCards.Clear();

    // Clear the visual representation of community cards (if needed)
    // Example: Remove cards from UI or hide them
}



    [PunRPC]
    private void DealTurnCardRPC()
    {
        Card drawnCard = DrawCard();
        if (drawnCard != null)
        {
            communityCards.Add(drawnCard);
            PhotonView cardView = drawnCard.GetComponent<PhotonView>();
            if (cardView != null)
            {
                photonView.RPC("AddCommunityCardRPC", RpcTarget.AllBuffered, cardView.ViewID, communityCards.Count - 1); // Adjusted to use the new count
           
			}
        }
        else
        {
            Debug.LogError("Failed to draw a card for the turn.");
        }
    }
	private void ClearPlayerHands()
    {
        // Find all players and clear their hands
        PlayerCardHandler[] playerHandlers = FindObjectsOfType<PlayerCardHandler>();
        foreach (PlayerCardHandler handler in playerHandlers)
        {
            handler.ClearHand(); // Implement a method in PlayerCardHandler to clear the hand
        }
    }
	[PunRPC]
    public void RESTARTRPC()
    {
        ClearCommunityCards();
        ClearPlayerHands();
        photonView.RPC("ShuffleDeckRPC", RpcTarget.All);
		if(PhotonNetwork.IsMasterClient)
		{
        StartCoroutine(DelayedDistributeCards());
    }}
	 [PunRPC]
    private void DealRiverCardRPC()
    {
        Card drawnCard = DrawCard();
        if (drawnCard != null)
        {
            communityCards.Add(drawnCard);
            PhotonView cardView = drawnCard.GetComponent<PhotonView>();
            if (cardView != null)
            {
                photonView.RPC("AddCommunityCardRPC", RpcTarget.AllBuffered, cardView.ViewID, communityCards.Count - 1); // Adjusted to use the new count
			  photonView.RPC("DealCommunityCardsRPC", RpcTarget.AllViaServer);

			}
        }
        else
        {
            Debug.LogError("Failed to draw a card for the river.");
        }
    }[PunRPC]
private void DealCommunityCardsRPC()
{
    PlayerCardHandler[] handlers = FindObjectsOfType<PlayerCardHandler>();
    foreach (PlayerCardHandler handler in handlers)
    {
        handler.photonView.RPC("AddCommunityCardsRPC", RpcTarget.All, ConvertCardListToViewIDs(communityCards));
    }
}
private int[] ConvertCardListToViewIDs(List<Card> cards)
{
    return cards.Select(card => card.photonView.ViewID).ToArray();
}
}
