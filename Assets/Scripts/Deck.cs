using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using System;
public class Deck : MonoBehaviourPunCallbacks
{
    public GameObject cardPrefab;  // Prefab for card objects
    public List<Card> cards;  // List to hold the cards in the deck
    private PlayerManager playerManager;
    public Transform[] SpawnPos;  // Array for spawn positions for cards
 public int cardsToDistributePerPlayer = 2;
  public List<Card> communityCards = new List<Card>();
  public Transform communityCardsParent; 
  public Transform DeckParent; 
   public int communityCardsCount = 3;
    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            InitializeDeck();
            ShuffleDeck();
        }
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
            //Debug.LogError("Failed to draw a card for the turn.");
        }
    }
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
            //Debug.LogError("Failed to draw a card for the river.");
        }
    }

[PunRPC]
private void DealCommunityCardsRPC()
{
    PlayerManager[] handlers = FindObjectsOfType<PlayerManager>();
    foreach (PlayerManager handler in handlers)
    {
        handler.photonView.RPC("AddCommunityCardsRPC", RpcTarget.All, ConvertCardListToViewIDs(communityCards));
    }
}
private int[] ConvertCardListToViewIDs(List<Card> cards)
{
    return cards.Select(card => card.photonView.ViewID).ToArray();
}
 public IEnumerator DelayedRestart()
    {
        yield return new WaitForSeconds(0.5f); // Adjust the delay time as needed
        
		photonView.RPC("RestartRPC", RpcTarget.MasterClient);
		
    }
	[PunRPC]
	private void ClearPlayerHands()
    {
        // Find all players and clear their hands
        PlayerManager[] playerHandlers = FindObjectsOfType<PlayerManager>();
        foreach (PlayerManager handler in playerHandlers)
        {
            handler.ClearHand(); // Implement a method in PlayerCardHandler to clear the hand
        }
    }
	
	[PunRPC]
private void ClearCommunityCards()
{
    // Iterate over the list of community cards
    foreach (Card card in communityCards)
    {
        // Assuming each card has a reference to its GameObject
        if (card != null && card.gameObject != null)
        {
            PhotonNetwork.Destroy(card.gameObject);
        }
    }

    // Clear the list of community cards
    communityCards.Clear();

    // Destroy all children of communityCardsParent
    foreach (Transform child in communityCardsParent)
    {
        GameObject.Destroy(child.gameObject);
    }

    //Debug.Log("Community cards cleared.");
}
[PunRPC]
public void ClearCards()
{
    foreach (Card card in cards)
    {
        // Assuming each card has a reference to its GameObject
        if (card != null && card.gameObject != null)
        {
            PhotonNetwork.Destroy(card.gameObject);
        }
    }

    // Clear the list of community cards
    cards.Clear();
photonView.RPC("ClearPlayerHands", RpcTarget.All);
if(PhotonNetwork.IsMasterClient){
	
StartCoroutine(IniAfterDelay());
}

}

	[PunRPC]
    public void RestartRPC()
    {
photonView.RPC("ClearCommunityCards", RpcTarget.MasterClient);
        photonView.RPC("ClearPlayerHands", RpcTarget.AllBuffered);
		photonView.RPC("ClearCards", RpcTarget.All);
	
    }
    // Method to initialize the deck with all 52 cards
    [PunRPC]
    public void InitializeDeck()
    {
        cards.Clear();  // Clear the existing cards
        

        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
            {
                Quaternion spawnRotation = Quaternion.Euler(0, 0, 0);
                GameObject newCard = PhotonNetwork.Instantiate(cardPrefab.name, SpawnPos[0].position, spawnRotation, 0);



                Card cardComponent = newCard.GetComponent<Card>();
                if (cardComponent != null)
                {
                    photonView.RPC("InitializeCard", RpcTarget.AllBuffered, cardComponent.photonView.ViewID, (int)rank, (int)suit);
                    cards.Add(cardComponent);  // Add the created card to the deck
                    //Debug.Log($"Created card: {rank} of {suit}");
                }
            }
        }
    }

   public IEnumerator DelayedDistributeCards()
    {
        yield return new WaitForSeconds(0f); // Adjust the delay time as needed
        photonView.RPC("DistributeCardsRPC", RpcTarget.MasterClient);
		
    }
	
	[PunRPC]
    private void DistributeAndAddCommunityCards()
    {
        //Debug.Log("Distributing community cards...");
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
                    //Debug.Log($"Community card");
					photonView.RPC("AddCommunityCardRPC", RpcTarget.AllBuffered, cardView.ViewID, i);
           
				}
            }
            else
            {
                //Debug.LogError("Failed to draw a card for community cards.");
            }
        }
	
    }
    void ShuffleDeck()
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            Card temp = cards[i];
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }
    }
  [PunRPC]
    private void ShuffleDeckRPC()
    {
        ShuffleDeck();
        //Debug.Log("Deck shuffled on all clients.");
    }
   private IEnumerator ShuffleDeckAfterDelay()
    {
        yield return new WaitForSeconds(0f);
        photonView.RPC("ShuffleDeckRPC", RpcTarget.MasterClient);

    }
	   private IEnumerator IniAfterDelay()
    {
        yield return new WaitForSeconds(1f);
       
photonView.RPC("InitializeDeck", RpcTarget.MasterClient);

       StartCoroutine(ShuffleDeckAfterDelay());
	    StartCoroutine(DelayedDistributeCards());
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
private void AddCommunityCardRPC(int cardViewID, int positionIndex)
{
    PhotonView cardView = PhotonView.Find(cardViewID);
    if (cardView != null)
    {
        Card card = cardView.GetComponent<Card>();
        if (card != null && communityCardsParent != null)
        {
            //Debug.Log($"Adding community card {card.rank} of {card.suit} to position {positionIndex}");
            card.transform.SetParent(communityCardsParent, false); // False to keep the local position

            float spacing = -3f; // Adjust the spacing value as needed
            Vector3 cardPosition = new Vector3(positionIndex * spacing, 0, 0);
            
            // Set the local position of the card
            card.transform.localPosition = cardPosition;
            
            // Rotate the card by 180 degrees around the Z-axis
            card.transform.localRotation = Quaternion.Euler(0, 0, 180);

            //Debug.Log($"Community card {card.rank} of {card.suit} added at position {positionIndex}.");
        }
        else
        {
            //Debug.LogError("Community card or parent is null.");
        }
    }
    else
    {
        //Debug.LogError("CardView not found.");
    }
}

	public Card DrawCard()
    {
        if (cards == null || cards.Count == 0)
        {
            //Debug.LogWarning("No cards left to draw or deck not initialized.");
            return null;
        }

        Card drawnCard = cards[0];
        cards.RemoveAt(0);
        //Debug.Log($"Card drawn: {drawnCard}");
        return drawnCard;
    }
[PunRPC]
    private void DistributeCardsRPC()
    {
 
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject playerGO in players)
            {
                PlayerManager playerHandler = playerGO.GetComponent<PlayerManager>();
                if (playerHandler != null)
                {
                    for (int i = 0; i < cardsToDistributePerPlayer; i++)
                    {
                        Card drawnCard = DrawCard();
                        if (drawnCard != null)
                        {GameManager.Instance.photonView.RPC("ProgressTrue", RpcTarget.AllBuffered);
                            playerHandler.photonView.RPC("AddCardToPlayerHandRPC", RpcTarget.AllBuffered, drawnCard.photonView.ViewID);
							
                        }
                    }
                }
            }


        }
    }
}
