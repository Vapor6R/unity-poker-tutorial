using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using System;
using TMPro;
using System.Collections;
using ExitGames.Client.Photon;
using UnityEngine.UI;
public enum PlayerPosition
{None = -1,
    DEALER = 0,    // Dealer position
    UTG =1,      // Under the Gun (first player to act after the blinds)
    UTG_PLUS_1=2, // UTG+1 (second player to act)
    POSITION_3=3, // 3rd player to act
    POSITION_4=4, // 4th player to act
    POSITION_5=5, // 5th player to act
    POSITION_6=6, // 6th player to act
    POSITION_7=7, // 7th player to act
    POSITION_8=8 // 8th player to act
}

public class PlayerCardHandler : MonoBehaviourPunCallbacks  // Inherit from MonoBehaviourPunCallbacks instead of MonoBehaviourPun
{ private PlayerPosition previousPosition;
 public string playerName;
[SerializeField] private TextMeshProUGUI chipCountText;
    public PlayerPosition playerPosition;
    public List<Card> playerHand = new List<Card>();
    public Transform cardHandPosition;
    public int chipCount = 1000;
    public GameObject dealerLogo;
    public GameObject bigBlindLogo;
    public GameObject SmallBlind;
    private List<int> playerActorNumbers = new List<int>();    
    public int playerNumber;
private static int playerCount = 1;
 public TextMeshProUGUI handRankText;
 public HandRank playerHandRank = HandRank.None;
  public static List<PlayerPosition> availablePositions = new List<PlayerPosition>
    {PlayerPosition.None,
        PlayerPosition.DEALER,
        PlayerPosition.UTG,
        PlayerPosition.UTG_PLUS_1,
        PlayerPosition.POSITION_4,
        PlayerPosition.POSITION_5,
        PlayerPosition.POSITION_6,
        PlayerPosition.POSITION_7,
        PlayerPosition.POSITION_8
    };
    public bool isMyTurn = false;


	public int GetChipCount()
    {
        return chipCount;
    }
	public void StartTurn()
    {
        if (photonView.IsMine)
        {
            isMyTurn = true;
            //Debug.Log("It's my turn! Position: " + playerPosition);
            // Enable the UI elements for the player to interact (like the call button)
            GameManager.Instance.UpdateTurnUI();
        }
    }
	public void ReinstatePosition()
    {
        if (!availablePositions.Contains(previousPosition))
        {
            availablePositions.Add(previousPosition);
            Debug.Log($"Player's position {previousPosition} reinserted into availablePositions.");
        }
        else
        {
            Debug.LogWarning($"Position {previousPosition} is already in availablePositions.");
        }
    }
	 private void Awake()
    {
        // Automatically register the player with GameManager when it is created
        if (photonView.IsMine)
        {
            GameManager.Instance.RegisterPlayer(this);
        }
    }
public override void OnEnable()
{
    base.OnEnable();

    playerName = PhotonNetwork.NickName; // Assign player name from Photon
    AssignPosition(); // Assign a position from available positions
    GameManager.Instance.photonView.RPC("AssignActive", RpcTarget.All);
}


public bool IsMyTurn { get; private set; }

   public void DisablePlayerUI()
{
    if (GameManager.Instance.UI != null)
    {
        GameManager.Instance.UI.SetActive(false);
        //Debug.Log($"Disabled UI for player at position: {playerPosition}");
    }
}


     public void AssignPosition()
    {
        // If the player is already assigned a position, store it as the previous position
        if (previousPosition != PlayerPosition.None)
        {
            // Reassign the player's previous position when they sit again
            playerPosition = previousPosition;
            previousPosition = PlayerPosition.None; // Reset the stored previous position
            Debug.Log($"{gameObject.name} has been reassigned to position: {playerPosition}");
        }
        else
        {
            // Assign a new position if the player has no previous position saved
            if (availablePositions.Count > 0)
            {
                playerPosition = availablePositions[0];
                availablePositions.RemoveAt(0); // Remove the assigned position from the list
                Debug.Log($"{gameObject.name} has been assigned to position: {playerPosition}");
            }
            else
            {
                Debug.LogWarning("No available positions left.");
            }
        }
    }
public void StorePreviousPosition()
    {
        // Save the player's current position as their previous position
        previousPosition = playerPosition;
        Debug.Log($"Player's position {playerPosition} stored as previous.");
    }

public void EnablePlayerUI()
{
    if (GameManager.Instance.UI != null)
    {
        GameManager.Instance.UI.SetActive(true);
        //Debug.Log($"Enabled UI for player at position: {playerPosition}");
    }
}
public static void RefreshAvailablePositions()
    {
        availablePositions = new List<PlayerPosition>
        {
            PlayerPosition.DEALER,
            PlayerPosition.UTG,
            PlayerPosition.UTG_PLUS_1,
            PlayerPosition.POSITION_3,
            PlayerPosition.POSITION_4,
            PlayerPosition.POSITION_5,
            PlayerPosition.POSITION_6,
            PlayerPosition.POSITION_7,
            PlayerPosition.POSITION_8
        };
        Debug.Log("Available Positions have been refreshed.");
    }

 public void CallSetPlayerHandRankRPC(HandRank handRank)
{
    photonView.RPC("SetPlayerHandRankRPC", RpcTarget.AllBuffered, handRank);
}
[PunRPC]
public void SetPlayerHandRankRPC(HandRank handRank)
{
    SetPlayerHandRank(handRank);
}
public void SetPlayerHandRank(HandRank handRank)
    {
        playerHandRank = handRank;
		
    }
   public void Assign()
    {
        // Assign the current player number and increment for the next player
        playerNumber = playerCount;
        
        // Increment the static counter
        playerCount++;
        
        // Optionally limit the player number to 8
        if (playerCount > 8)
        {
            playerCount = 0;
        }

        // Print the player's assigned number for debugging
        //Debug.Log("Player Number: " + playerNumber);
    }
	[PunRPC]
public void RotatePlayerPositions()
{
    // Create a list of player positions (could be 9 max positions)
    List<PlayerPosition> positions = new List<PlayerPosition>
    {
        PlayerPosition.DEALER, 
        PlayerPosition.UTG, 
        PlayerPosition.UTG_PLUS_1, 
        PlayerPosition.POSITION_3, 
        PlayerPosition.POSITION_4, 
        PlayerPosition.POSITION_5, 
        PlayerPosition.POSITION_6, 
        PlayerPosition.POSITION_7, 
        PlayerPosition.POSITION_8, 
    };

    // Get the current player's position index
    int currentPositionIndex = positions.IndexOf(playerPosition);

    // Check the number of players in the room
    int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;

    // Rotate based on player count
    if (playerCount == 2)
    {
        if (currentPositionIndex == 0)  // DEALER position
        {
            playerPosition = PlayerPosition.UTG;  // Move to UTG
        }
        else if (currentPositionIndex == 1)  // UTG position
        {
            playerPosition = PlayerPosition.DEALER;  // Move to DEALER
        }
    }
    else if (playerCount == 3)
    {
        // Rotate among the first 3 positions
        if (currentPositionIndex == 0)  // DEALER position
        {
            playerPosition = PlayerPosition.UTG;  // Move to UTG
        }
        else if (currentPositionIndex == 1)  // UTG position
        {
            playerPosition = PlayerPosition.UTG_PLUS_1;  // Move to UTG+1
        }
        else if (currentPositionIndex == 2)  // UTG+1 position
        {
            playerPosition = PlayerPosition.DEALER;  // Move to DEALER
        }
    }
    else if (playerCount == 4)
    {
        // Rotate among the first 4 positions
        if (currentPositionIndex == 0)  // DEALER position
        {
            playerPosition = PlayerPosition.UTG;  // Move to UTG
        }
        else if (currentPositionIndex == 1)  // UTG position
        {
            playerPosition = PlayerPosition.UTG_PLUS_1;  // Move to UTG+1
        }
        else if (currentPositionIndex == 2)  // UTG+1 position
        {
            playerPosition = PlayerPosition.POSITION_3;  // Move to POSITION_3
        }
        else if (currentPositionIndex == 3)  // POSITION_3 position
        {
            playerPosition = PlayerPosition.DEALER;  // Move to DEALER
        }
    }
    else if (playerCount == 5)
    {
        // Rotate among the first 5 positions
        if (currentPositionIndex == 0)  // DEALER position
        {
            playerPosition = PlayerPosition.UTG;  // Move to UTG
        }
        else if (currentPositionIndex == 1)  // UTG position
        {
            playerPosition = PlayerPosition.UTG_PLUS_1;  // Move to UTG+1
        }
        else if (currentPositionIndex == 2)  // UTG+1 position
        {
            playerPosition = PlayerPosition.POSITION_3;  // Move to POSITION_3
        }
        else if (currentPositionIndex == 3)  // POSITION_3 position
        {
            playerPosition = PlayerPosition.POSITION_4;  // Move to POSITION_4
        }
        else if (currentPositionIndex == 4)  // POSITION_4 position
        {
            playerPosition = PlayerPosition.DEALER;  // Move to DEALER
        }
    }
    else if (playerCount == 6)
    {
        // Rotate among the first 6 positions
        if (currentPositionIndex == 0)  // DEALER position
        {
            playerPosition = PlayerPosition.UTG;  // Move to UTG
        }
        else if (currentPositionIndex == 1)  // UTG position
        {
            playerPosition = PlayerPosition.UTG_PLUS_1;  // Move to UTG+1
        }
        else if (currentPositionIndex == 2)  // UTG+1 position
        {
            playerPosition = PlayerPosition.POSITION_3;  // Move to POSITION_3
        }
        else if (currentPositionIndex == 3)  // POSITION_3 position
        {
            playerPosition = PlayerPosition.POSITION_4;  // Move to POSITION_4
        }
        else if (currentPositionIndex == 4)  // POSITION_4 position
        {
            playerPosition = PlayerPosition.POSITION_5;  // Move to POSITION_5
        }
        else if (currentPositionIndex == 5)  // POSITION_5 position
        {
            playerPosition = PlayerPosition.DEALER;  // Move to DEALER
        }
    }
    else if (playerCount == 7)
    {
        // Rotate among the first 7 positions
        if (currentPositionIndex == 0)  // DEALER position
        {
            playerPosition = PlayerPosition.UTG;  // Move to UTG
        }
        else if (currentPositionIndex == 1)  // UTG position
        {
            playerPosition = PlayerPosition.UTG_PLUS_1;  // Move to UTG+1
        }
        else if (currentPositionIndex == 2)  // UTG+1 position
        {
            playerPosition = PlayerPosition.POSITION_3;  // Move to POSITION_3
        }
        else if (currentPositionIndex == 3)  // POSITION_3 position
        {
            playerPosition = PlayerPosition.POSITION_4;  // Move to POSITION_4
        }
        else if (currentPositionIndex == 4)  // POSITION_4 position
        {
            playerPosition = PlayerPosition.POSITION_5;  // Move to POSITION_5
        }
        else if (currentPositionIndex == 5)  // POSITION_5 position
        {
            playerPosition = PlayerPosition.POSITION_6;  // Move to POSITION_6
        }
        else if (currentPositionIndex == 6)  // POSITION_6 position
        {
            playerPosition = PlayerPosition.DEALER;  // Move to DEALER
        }
    }
    else if (playerCount == 8)
    {
        // Rotate among the first 8 positions
        if (currentPositionIndex == 0)  // DEALER position
        {
            playerPosition = PlayerPosition.UTG;  // Move to UTG
        }
        else if (currentPositionIndex == 1)  // UTG position
        {
            playerPosition = PlayerPosition.UTG_PLUS_1;  // Move to UTG+1
        }
        else if (currentPositionIndex == 2)  // UTG+1 position
        {
            playerPosition = PlayerPosition.POSITION_3;  // Move to POSITION_3
        }
        else if (currentPositionIndex == 3)  // POSITION_3 position
        {
            playerPosition = PlayerPosition.POSITION_4;  // Move to POSITION_4
        }
        else if (currentPositionIndex == 4)  // POSITION_4 position
        {
            playerPosition = PlayerPosition.POSITION_5;  // Move to POSITION_5
        }
        else if (currentPositionIndex == 5)  // POSITION_5 position
        {
            playerPosition = PlayerPosition.POSITION_6;  // Move to POSITION_6
        }
        else if (currentPositionIndex == 6)  // POSITION_6 position
        {
            playerPosition = PlayerPosition.POSITION_7;  // Move to POSITION_7
        }
        else if (currentPositionIndex == 7)  // POSITION_7 position
        {
            playerPosition = PlayerPosition.DEALER;  // Move to DEALER
        }
    }
    else if (playerCount == 9)
    {
        // Rotate among all 9 positions
        int nextPositionIndex = (currentPositionIndex + 1) % positions.Count;
        playerPosition = positions[nextPositionIndex];  // Update player position
    }

    //Debug.Log($"Rotated position: {playerPosition}");

    // Update player position UI (logos, etc.)
    photonView.RPC("Logos", RpcTarget.All);
}

[PunRPC]
    public void AddCommunityCardsRPC(int[] cardViewIDs)
    {
        //Debug.Log("Adding community cards...");
        foreach (int cardViewID in cardViewIDs)
        {
            PhotonView cardView = PhotonView.Find(cardViewID);
            if (cardView != null)
            {
                Card card = cardView.GetComponent<Card>();
                if (card != null)
                {
                    if (!playerHand.Contains(card))
                    {
                        playerHand.Add(card);
                        //Debug.Log($"Added community card {card.rank} of {card.suit}.");
                    }
                }
            }
        }
    }
    [PunRPC]
    public void AddCardToPlayerHandRPC(int cardViewID)
    {
        //Debug.Log($"Adding card with view ID {cardViewID} to player hand.");
        PhotonView cardView = PhotonView.Find(cardViewID);
        if (cardView != null)
        {
            Card card = cardView.GetComponent<Card>();
            if (card != null)
            {
                playerHand.Add(card);
                card.transform.SetParent(cardHandPosition);
                card.gameObject.SetActive(photonView.IsMine);
                TransformCardPositions();
				
            
            }
        }
    }

    private void TransformCardPositions()
    {
        for (int i = 0; i < playerHand.Count; i++)
        {
            Card card = playerHand[i];
            if (card != null)
            {
                Vector3 desiredPosition = new Vector3(i * 125f, 0f, 0f);

                // Ensure the card is positioned relative to the parent
                card.transform.localPosition = desiredPosition;
                card.transform.localRotation = Quaternion.identity; // Reset rotation

                // Optionally, adjust the scale if needed
                card.transform.localScale = Vector3.one;

                //Debug.Log($"Card {card.rank} of {card.suit} moved to position {desiredPosition}.");
            }
            else
            {
                //Debug.LogError("Card is null in TransformCardPositions().");
            }
        }
    }
	
	public void AddChips(int amount)
    {
        // Ensure you have a field for chip count
        chipCount += amount;
        photonView.RPC("UpdateChipCountRPC", RpcTarget.All, chipCount);
    }
	void Update()
{
    chipCountText.text = "Chips: " + chipCount.ToString();
 // Update the handRankText and display the evaluation results
          HandEvaluation evaluation = HandEvaluator.EvaluateBestHand(playerHand);
		
		if (handRankText != null)
        {
            handRankText.text = "Hand: " + evaluation.Rank.ToString() + ", Kickers: " + string.Join(", ", evaluation.Kickers);
        }}


	 public HandRank GetPlayerHandRank()
{
    if (playerHand == null || !playerHand.Any())
    {
        return HandRank.None; // Return "None" for invalid hands
    }

    List<Card> combinedCards = new List<Card>(playerHand);
    
    if (combinedCards.Count < 5)
    {
        return HandRank.None; // Return "None" if not enough cards
    }

    HandEvaluation evaluation = HandEvaluator.EvaluateBestHand(combinedCards);
    playerHandRank = evaluation.Rank; // Update the player's rank

    return playerHandRank; // Return the evaluated rank
}

public void ResetPlayerHandRankOnAllClientsWithDelay(float delay)
    {
        StartCoroutine(ResetPlayerHandRankCoroutine(delay));
    }
	private IEnumerator ResetPlayerHandRankCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        photonView.RPC("ResetPlayerHandRank", RpcTarget.All);
    }
	 [PunRPC]
    public void ResetPlayerHandRank()
    {
        playerHandRank = HandRank.None;
        //Debug.Log("Player hand rank reset to None.");
    }
	public void ClearHand()
{
    // Iterate over the list to destroy each card's GameObject
    foreach (Card card in playerHand)
    {
        PhotonNetwork.Destroy(card.gameObject);  // Destroy the card's GameObject
    }

    // Now clear the entire list once all cards are destroyed
    playerHand.Clear();
    
    //Debug.Log("Cleared player hand.");
}
public void DeductBlind(int blindAmount)
    {
        if (blindAmount > 0 && chipCount >= blindAmount)
        {
            int newChipCount = chipCount - blindAmount;
            photonView.RPC("UpdateChipCountRPC", RpcTarget.All, newChipCount);
            //Debug.Log($"Blind of {blindAmount} deducted. New chip count: {chipCount}");
        }
        else
        {
            //Debug.LogWarning("Insufficient chips or invalid blind amount.");
        }
		GameManager.Instance.photonView.RPC("updateSlider", RpcTarget.All);
    }
	
    [PunRPC]
    public void UpdateChipCountRPC(int newChipCount)
    {
        chipCount = newChipCount;
        if (chipCountText != null)
        {
            chipCountText.text = $"Chips: {chipCount}";
            //Debug.Log($"Chip count text updated to: {chipCount}");
        }
    }
public void UpdateChips(int chipChange)
    {
        chipCount += chipChange;
        //Debug.Log($"Player's remaining chips: {chipCount}");
    }
    void SetPlayerPosition(int playerNumber)
    {
        switch (playerNumber)
        {
            case 1:
                playerPosition = PlayerPosition.DEALER;
                break;
            case 2:
                playerPosition = PlayerPosition.UTG;
                break;
            case 3:
                playerPosition = PlayerPosition.UTG_PLUS_1;
                break;
            case 4:
                playerPosition = PlayerPosition.POSITION_3;
                break;
            case 5:
                playerPosition = PlayerPosition.POSITION_4;
                break;
            case 6:
                playerPosition = PlayerPosition.POSITION_5;
                break;
            case 7:
                playerPosition = PlayerPosition.POSITION_6;
                break;
				case 8:
                playerPosition = PlayerPosition.POSITION_7;
                break;
				case 9:
                playerPosition = PlayerPosition.POSITION_8;
                break;
            default:
                //Debug.LogError("Invalid player number!");
                break;
        }

        //Debug.Log("Player " + playerNumber + " is in the position: " + playerPosition);
    }
[PunRPC]
    public void DeductChipsRPC(int amount)
    {

            chipCount -= amount;
            chipCountText.text = $"Chips: {chipCount}";
            //Debug.Log($"Chips deducted by {amount}. New chip count: {chipCount}");
        
    }
 
private void Start()
{

   
    if (PhotonNetwork.IsMasterClient)
    {
        // You can add logic here for the master client to initialize the game
        //Debug.Log("Master client detected. Initializing game.");
    }

    // Clear the list of player actor numbers (used for turn order)
    playerActorNumbers.Clear();

    // Add all players in the Photon network room to the playerActorNumbers list
    foreach (Player player in PhotonNetwork.PlayerList)
    {
        playerActorNumbers.Add(player.ActorNumber);
        //Debug.Log($"Player added: {player.NickName} with ActorNumber {player.ActorNumber}");
    }

    // Call the method to assign positions
    Assign();

    // Set player position based on player number
    SetPlayerPosition(playerNumber);
}


    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        //Debug.Log("New player joined: " + newPlayer.NickName);
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2 && PhotonNetwork.IsMasterClient)
        {
            playerActorNumbers.Clear();
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                playerActorNumbers.Add(player.ActorNumber);
				
            }
photonView.RPC("Logos", RpcTarget.All);
        }
		//Debug.Log($"New player joined: {newPlayer.NickName}");

    }


   [PunRPC]
private void Logos()
{
    if (PhotonNetwork.CurrentRoom.PlayerCount <= 2)  // Check player count
    {
        if (playerPosition == PlayerPosition.DEALER)
        {
            ActivateDealerLogo();
            ActivateBigBlindLogo();
            DeactivateSmallBlindLogo();
        }
        else if (playerPosition == PlayerPosition.UTG)
        {
            ActivateSmallBlindLogo();
            DeactivateDealerLogo();
            DeactivateBigBlindLogo();
        }
        else
        {
            DeactivateDealerLogo();
            DeactivateSmallBlindLogo();
            DeactivateBigBlindLogo();
        }
    }
    else
    {
        if (playerPosition == PlayerPosition.DEALER)
        {
            ActivateDealerLogo();
        }
        else
        {
            DeactivateDealerLogo();
        }

        if (playerPosition == PlayerPosition.UTG)
        {
            ActivateSmallBlindLogo();
        }
        else
        {
            DeactivateSmallBlindLogo();
        }

        if (playerPosition == PlayerPosition.UTG_PLUS_1)  // Big Blind
        {
            ActivateBigBlindLogo();
        }
        else
        {
            DeactivateBigBlindLogo();
        }
    }
}


    public void ActivateSmallBlindLogo()
    {
        if (SmallBlind != null)
        {
            SmallBlind.SetActive(true);
        }
    }

    public void DeactivateSmallBlindLogo()
    {
        if (SmallBlind != null)
        {
            SmallBlind.SetActive(false);
        }
    }

    public void ActivateDealerLogo()
    {
        if (dealerLogo != null)
        {
            dealerLogo.SetActive(true);
        }
    }

    public void DeactivateDealerLogo()
    {
        if (dealerLogo != null)
        {
            dealerLogo.SetActive(false);
        }
    }

    public void ActivateBigBlindLogo()
    {
        if (bigBlindLogo != null)
        {
            bigBlindLogo.SetActive(true);
        }
    }

    public void DeactivateBigBlindLogo()
    {
        if (bigBlindLogo != null)
        {
            bigBlindLogo.SetActive(false);
        }
    }
}
