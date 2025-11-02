using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using System;
public class Deck : MonoBehaviourPunCallbacks {
  public static Deck Instance;
  public GameObject cardPrefab;  // Prefab for card objects
  public List<Card> cards;       // List to hold the cards in the deck
  private PlayerManager playerManager;
  public Transform[] SpawnPos;  // Array for spawn positions for cards
  public int cardsToDistributePerPlayer = 2;
  public List<Card> communityCards = new List<Card>();
  public Transform communityCardsParent;
  public Transform DeckParent;
  public int communityCardsCount = 3;
  public bool DeckSpawned = false;
    public bool roundResetInProgress = false;
   public bool isDistributing = false;
  	[PunRPC]
	private void ResetInProgressF()
	{
		
		roundResetInProgress = false;
	}
		[PunRPC]
	private void Dfalse()
	{
		
		isDistributing = false;
	}
	[PunRPC]
	private void ResetInProgressT()
	{
		
		roundResetInProgress = true;
	}
  [PunRPC]
  public void Start() {
    if (PhotonNetwork.IsMasterClient) {
      InitializeDeck();
      ShuffleDeck();
    }
  }
  [PunRPC]
public void RPC_ClearCommunityAndDeck()
{
   if(isDistributing)
	   return;
    communityCards.Clear();
    cards.Clear();
    DeckSpawned = false;

    // ✅ Destroy every card object in scene
    GameObject[] allCards = GameObject.FindGameObjectsWithTag("Card");
    foreach (GameObject card in allCards)
    {
        PhotonNetwork.Destroy(card);
    }

    // ✅ Clear every player's hand list
    PlayerManager[] allPlayers = FindObjectsOfType<PlayerManager>();
    foreach (PlayerManager pm in allPlayers)
    {
        if (pm.playerHand != null && pm.playerHand.Count > 0)
        {
            // Destroy all card objects in their hand
            foreach (Card c in pm.playerHand)
            {
                if (c != null)
                    PhotonNetwork.Destroy(c.gameObject);
            }

            pm.playerHand.Clear();
        }
    }

    photonView.RPC("ResetInProgressF", RpcTarget.AllBuffered);
    
}

	 public IEnumerator Next() {
    yield return new WaitForSeconds(1f);
if (PhotonNetwork.IsMasterClient)
    {  photonView.RPC("ResetInProgressT", RpcTarget.AllBuffered);
        StartCoroutine(IniAfterDelay());
	 }}
	
  private IEnumerator IniAfterDelay() {
    yield return new WaitForSeconds(1f);

    photonView.RPC("InitializeDeck", RpcTarget.MasterClient);

    StartCoroutine(ShuffleDeckAfterDelay());
    StartCoroutine(DelayedDistributeCards());
  }
  private void Awake() {
    if (Instance != null && Instance != this) {
      Debug.LogWarning("Duplicate GameManager found, destroying it: " + gameObject.name);
      Destroy(gameObject);
      return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);
  }
  
  private Card DrawCard() {
    if (cards.Count == 0) {
      // Debug.LogWarning("Deck is empty!");
      return null;
    }

    Card topCard = cards[0];  // Take the first card in the deck
    cards.RemoveAt(0);        // Remove it from the list
    return topCard;           // Return it to be used
  }
  [PunRPC]
  private void DealTurnCardRPC() {
    Card drawnCard = DrawCard();
    if (drawnCard != null) {
      communityCards.Add(drawnCard);
      PhotonView cardView = drawnCard.GetComponent<PhotonView>();
      if (cardView != null) {
        photonView.RPC("AddCommunityCardRPC", RpcTarget.AllBuffered, cardView.ViewID,
                       communityCards.Count - 1);  // Adjusted to use the new count
      }
    } else {
      ////Debug.LogError("Failed to draw a card for the turn.");
    }
  }
  [PunRPC]
  private void DealRiverCardRPC() {
    Card drawnCard = DrawCard();
    if (drawnCard != null) {
      communityCards.Add(drawnCard);
      PhotonView cardView = drawnCard.GetComponent<PhotonView>();
      if (cardView != null) {
        photonView.RPC("AddCommunityCardRPC", RpcTarget.AllBuffered, cardView.ViewID,
                       communityCards.Count - 1);  // Adjusted to use the new count
        photonView.RPC("DealCommunityCardsRPC", RpcTarget.AllViaServer);
      }
    } else {
      ////Debug.LogError("Failed to draw a card for the river.");
    }
    StartCoroutine(DelayedEval());
  }
  private IEnumerator DelayedEval() {
    yield return new WaitForSeconds(1f);
  }
  [PunRPC]
  private void DealCommunityCardsRPC() {
    PlayerManager[] handlers = FindObjectsOfType<PlayerManager>();
    foreach (PlayerManager handler in handlers) {
      handler.photonView.RPC("AddCommunityCardsRPC", RpcTarget.All, ConvertCardListToViewIDs(communityCards));
    }
  }
  private int[] ConvertCardListToViewIDs(List<Card> cards) {
    return cards.Select(card => card.photonView.ViewID).ToArray();
  }
  public IEnumerator DelayedRestart() {
    yield return new WaitForSeconds(0.5f);  // Adjust the delay time as needed

 photonView.RPC("RPC_ClearCommunityAndDeck", RpcTarget.MasterClient);
  }

  [PunRPC]
  public void Decktrue() {
  DeckSpawned = true;
  }

  [PunRPC]
  public void InitializeDeck() {
   if (DeckSpawned && cards != null && cards.Count == 52)
      return;
    cards.Clear();  // Clear the existing cards
  photonView.RPC("Decktrue", RpcTarget.AllBuffered);
    foreach (Suit suit in Enum.GetValues(typeof(Suit))) {
      foreach (Rank rank in Enum.GetValues(typeof(Rank))) {
        Quaternion spawnRotation = Quaternion.Euler(0, 0, 0);
        GameObject newCard = PhotonNetwork.Instantiate(cardPrefab.name, SpawnPos[0].position, spawnRotation, 0);
        

        Card cardComponent = newCard.GetComponent<Card>();
        if (cardComponent != null) {
          photonView.RPC("InitializeCard", RpcTarget.AllBuffered, cardComponent.photonView.ViewID, (int)rank, (int)suit);
          cards.Add(cardComponent);  // Add the created card to the deck
        }
      }
    }
  }

  public IEnumerator DelayedDistributeCards() {
    yield return new WaitForSeconds(0.1f);
    PlayerManager[] players = FindObjectsOfType<PlayerManager>();
    if (players.Length >= 2) {
      photonView.RPC("DistributeCardsRPC", RpcTarget.MasterClient);
	  GameManager.Instance.photonView.RPC("PostBlinds", RpcTarget.MasterClient);
    }
  }

  [PunRPC]
  private void DistributeAndAddCommunityCards() {
    ////Debug.Log("Distributing community cards...");
    for (int i = 0; i < communityCardsCount; i++) {
      Card drawnCard = DrawCard();
      if (drawnCard != null) {
        communityCards.Add(drawnCard);

        // Set the card's parent and position
        PhotonView cardView = drawnCard.GetComponent<PhotonView>();
        if (cardView != null) {
          ////Debug.Log($"Community card");
          photonView.RPC("AddCommunityCardRPC", RpcTarget.AllBuffered, cardView.ViewID, i);
        }
      } else {
        ////Debug.LogError("Failed to draw a card for community cards.");
      }
    }
  }
  void ShuffleDeck() {
    for (int i = cards.Count - 1; i > 0; i--) {
      int randomIndex = UnityEngine.Random.Range(0, i + 1);
      Card temp = cards[i];
      cards[i] = cards[randomIndex];
      cards[randomIndex] = temp;
    }
  }
  [PunRPC]
  private void ShuffleDeckRPC() {
    ShuffleDeck();
    ////Debug.Log("Deck shuffled on all clients.");
  }
  private IEnumerator ShuffleDeckAfterDelay() {
    yield return new WaitForSeconds(0f);
    photonView.RPC("ShuffleDeckRPC", RpcTarget.MasterClient);
  }
 
  [PunRPC]
  private void InitializeCard(int viewID, int rank, int suit) {
    PhotonView view = PhotonView.Find(viewID);
    if (view != null) {
      Card cardComponent = view.GetComponent<Card>();
      if (cardComponent != null) {
        cardComponent.InitializeCard((Rank)rank, (Suit)suit);
      }
    }
  }
  [PunRPC]
  private void AddCommunityCardRPC(int cardViewID, int positionIndex) {
    PhotonView cardView = PhotonView.Find(cardViewID);
    if (cardView == null)
      return;

    Card card = cardView.GetComponent<Card>();
    if (card == null || communityCardsParent == null)
      return;

    // Place the card visually
    card.transform.SetParent(communityCardsParent, false);
    float spacing = +1.5f;
    card.transform.localPosition = new Vector3(positionIndex * spacing, 0, 0);
    card.transform.localRotation = Quaternion.Euler(0, 0, 0);

    // ✅ Make sure this is run only by MasterClient
    PhotonView localPlayerView = FindLocalPlayerPhotonView();
    if (localPlayerView != null) {
      localPlayerView.RPC("AddCommunityCardToHand", RpcTarget.AllBuffered, cardViewID);
    }
  }
  public PhotonView FindLocalPlayerPhotonView() {
    GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

    foreach (GameObject player in players) {
      PhotonView photonView = player.GetComponent<PhotonView>();
      if (photonView != null && photonView.Owner == PhotonNetwork.LocalPlayer) {
        return photonView;
      }
    }

    return null;  // Return null if not found (handle this case in your logic)
  }
  private PhotonView GetPlayerManagerPhotonView(Player player) {
    foreach (var obj in FindObjectsOfType<PlayerManager>()) {
      if (obj.photonView.Owner == player) {
        return obj.photonView;
      }
    }
    return null;
  }
  
  [PunRPC]
  private void Distributing() {  isDistributing=true;}
  [PunRPC]
  private void DistributeCardsRPC() {
    {if(isDistributing)
		return;
	 photonView.RPC("Distributing", RpcTarget.AllBuffered);
      GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
      foreach (GameObject playerGO in players) {
        PlayerManager playerHandler = playerGO.GetComponent<PlayerManager>();
        if (playerHandler != null) {
          foreach (Card oldCard in playerHandler.playerHand) {
            if (oldCard != null)
              Destroy(oldCard.gameObject);  // or SetActive(false)
          }
          playerHandler.playerHand.Clear();
          for (int i = 0; i < cardsToDistributePerPlayer; i++) {
            Card drawnCard = DrawCard();
            if (drawnCard != null) {
              playerHandler.photonView.RPC("AddCardToPlayerHandRPC", RpcTarget.AllBuffered, drawnCard.photonView.ViewID);
			   playerHandler.IsPlayingIN=true;
              GameManager.Instance.photonView.RPC("ProgressTrue", RpcTarget.All);
            }
          }
        }
      }
    }
  }
}
