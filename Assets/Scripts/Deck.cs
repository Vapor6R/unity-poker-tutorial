using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class Deck : MonoBehaviourPunCallbacks
{    public int communityCardsCount = 3;
    public List<Card> communityCards = new List<Card>();
    public List<Card> cards = new List<Card>();
    public GameObject cardPrefab;
    public Transform[] CARDSPAWN;
    private bool distributed = false;
    public bool isDeckSpawned = false;
    public Transform communityCardsParent; // Add this for community cards
    public int cardsToDistributePerPlayer = 2; // Number of cards to distribute per player
private PlayerCardHandler playerCardHandler;
public List<int> playerActorNumbers = new List<int>();
    public GameObject UI; // Reference to your UI GameObject
    private int currentPlayerIndex = 0;
    private bool isPlayerTurn = false;
	private bool comcard = false;
		private bool turncard = false;
			private bool rivercard = false;
			
	public InputField raiseInputField;
	public int potAmount = 0;
	private int turnCount = 0;
	
	    public const int SMALL_BLIND_AMOUNT = 10;
    public const int BIG_BLIND_AMOUNT = 20;
public TextMeshProUGUI potAmountText;
    public int smallBlindIndex;
    public int bigBlindIndex;
 public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("New player joined: " + newPlayer.NickName);
            if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && !distributed && PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("ShuffleDeckRPC", RpcTarget.All);
            StartCoroutine(DelayedDistributeCards());
        }
		 if (PhotonNetwork.CurrentRoom.PlayerCount >= 2 && PhotonNetwork.IsMasterClient)
        {
            playerActorNumbers.Clear(); // Clear the list first
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                playerActorNumbers.Add(player.ActorNumber);
            }
            StartGame();
        }
    }
   void StartGame()
    {
        currentPlayerIndex = 0;
		Debug.Log("Starting the game.");

        currentPlayerIndex = 0;
        potAmount = 0; // Reset the pot amount at the start of the game
        turnCount = 0; // Reset the turn count at the start of the game
        communityCards.Clear(); // Clear community cards at the start of the game // Clear community cards display
        smallBlindIndex = 0;
        bigBlindIndex = (smallBlindIndex + 1) % playerActorNumbers.Count;

        PostBlinds();

        StartTurn((bigBlindIndex + 1) % playerActorNumbers.Count);
    }
	 void PostBlinds()
    {
        Debug.Log($"Small blind posted by player {playerActorNumbers[smallBlindIndex]}: {SMALL_BLIND_AMOUNT}");
        Debug.Log($"Big blind posted by player {playerActorNumbers[bigBlindIndex]}: {BIG_BLIND_AMOUNT}");

        potAmount += SMALL_BLIND_AMOUNT + BIG_BLIND_AMOUNT;

        photonView.RPC("UpdatePotAmountRPC", RpcTarget.All, potAmount);
    }
	[PunRPC]
    private void UpdatePotAmountRPC(int newPotAmount)
    {
        potAmount = newPotAmount;
        Debug.Log($"Updated pot amount: {potAmount}");
		potAmountText.text = $"Pot: {potAmount}";
    }
 [PunRPC]
    void RestartGameRPC()
    {
        Debug.Log("Restarting game...");
        RestartGame();
    }
	  void RestartGame()
    {
        // Reset game state logic
        currentPlayerIndex = 0;
        StartGame();
    }
    void StartTurn(int playerIndex)
    {
        int currentActorNumber = playerActorNumbers[playerIndex];
		 Debug.Log($"Starting turn for player with ActorNumber {currentActorNumber}");
        if (PhotonNetwork.LocalPlayer.ActorNumber == currentActorNumber)
        {
            // It's the local player's turn
            isPlayerTurn = true;
            // Show UI elements relevant to the player's turn (e.g., buttons for actions)
            Debug.Log($"Starting turn for player {currentActorNumber}");
            UI.SetActive(true);
        }
        else
        {
            // It's not the local player's turn
            isPlayerTurn = false;
            // Hide UI elements not relevant to the current player's turn
            Debug.Log($"It's not your turn (Player {PhotonNetwork.LocalPlayer.ActorNumber}), current turn is for player {currentActorNumber}");
            UI.SetActive(false);
        }
    }

void EndTurn()
{
    isPlayerTurn = false;
    UI.SetActive(false); // Hide UI elements for actions

    currentPlayerIndex = (currentPlayerIndex + 1) % playerActorNumbers.Count;

    if (currentPlayerIndex == 0 && !comcard)
    {
        comcard = true;
        photonView.RPC("DistributeAndAddCommunityCards", RpcTarget.AllViaServer);
        photonView.RPC("StartTurnRPC", RpcTarget.All, currentPlayerIndex);
    }
    else if (currentPlayerIndex == 0 && comcard && !turncard)
    {
        turncard = true;
        photonView.RPC("DealTurnCardRPC", RpcTarget.AllViaServer);
        photonView.RPC("StartTurnRPC", RpcTarget.All, currentPlayerIndex);
    }
    else if (currentPlayerIndex == 0 && comcard && turncard && !rivercard)
    {
        rivercard = true;
        photonView.RPC("DealRiverCardRPC", RpcTarget.AllViaServer);
StartCoroutine(DelayedRestart());
    }
    else
    {
        photonView.RPC("StartTurnRPC", RpcTarget.All, currentPlayerIndex); // RPC to start the next player's turn
    }
}


    [PunRPC]
    void StartTurnRPC(int newPlayerIndex)
    {
        currentPlayerIndex = newPlayerIndex;
        StartTurn(currentPlayerIndex);
    }


    // Example method for handling player actions (e.g., End Turn button click)
    public void FOLDButtonClick()
    {
        if (isPlayerTurn)
        {
            EndTurn();
if(PhotonNetwork.CurrentRoom.PlayerCount <= 2)
{
Debug.Log("Player folded. Restarting game...");
            photonView.RPC("RestartGameRPC", RpcTarget.All);
}
        }
    }
	 public void CALLButtonClick()
    {
        if (isPlayerTurn)
        {
            EndTurn(); // Call this method when the player ends their turn
        }
    }
	 public void RAISEButtonClick()
    {
        if (isPlayerTurn)
        {
            if (int.TryParse(raiseInputField.text, out int raiseAmount))
            {
                Debug.Log($"Player {PhotonNetwork.LocalPlayer.ActorNumber} raised {raiseAmount}.");

                // Update the pot amount (this can be more complex depending on game rules)
                potAmount += raiseAmount;

                // Synchronize the raised amount with all players
                photonView.RPC("UpdatePotAmountRPC", RpcTarget.All, potAmount);

                // End the current player's turn
                EndTurn();
            }
            else
            {
                Debug.Log("Invalid raise amount entered.");
            }
        }
        else
        {
            Debug.Log("Cannot raise because it's not the local player's turn.");
        }
    }
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
 if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
	 {		 playerActorNumbers.Clear(); // Clear the list first
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                playerActorNumbers.Add(player.ActorNumber);
            }
            StartGame();
}
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
        if (!distributed)
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

            distributed = true;
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
                 float spacing = 150.0f; // Adjust the spacing value as needed
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


    private IEnumerator DelayedDistributeCards()
    {
        yield return new WaitForSeconds(0.5f); // Adjust the delay time as needed
        photonView.RPC("DistributeCardsRPC", RpcTarget.AllViaServer);
		
    }
	 private IEnumerator DelayedRestart()
    {
        yield return new WaitForSeconds(2.5f); // Adjust the delay time as needed
        
		photonView.RPC("RESTARTRPC", RpcTarget.AllViaServer);
		
    }
	[PunRPC]
	public void RESTARTRPC()
	{
		ClearCommunityCards();
    ClearPlayerHands();
		CreateDeck();
	photonView.RPC("ShuffleDeckRPC", RpcTarget.All);
            StartCoroutine(DelayedDistributeCards());
	}
	private void ClearCommunityCards()
{
    // Clear the list of community cards
    communityCards.Clear();

    // Clear the visual representation of community cards (if needed)
    // Example: Remove cards from UI or hide them
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
