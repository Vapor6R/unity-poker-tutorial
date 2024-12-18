using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System;

public class GameManager : MonoBehaviourPunCallbacks
{public PlayerPosition currentPlayerPosition;
public Dictionary<PlayerPosition, PlayerCardHandler> playerCardHandlers = new Dictionary<PlayerPosition, PlayerCardHandler>();
    public static GameManager Instance { get; private set; }
 public List<PlayerCardHandler> players = new List<PlayerCardHandler>();
    private PlayerCardHandler currentPlayer;
 private int totalPlayers;
    public List<int> playerActorNumbers = new List<int>(); 
	public List<int> ActivePlayers = new List<int>();
    public const int SMALL_BLIND_AMOUNT = 10;
    public const int BIG_BLIND_AMOUNT = 20;
    public int potAmount = 0;
    public bool isFirstRound = true; 
	public bool bigblind = false; 
		public bool raisetrue = false;
public bool firstround = false;	// Track if it's the first round
    public int currentBetAmount = 0;

    private PhotonView photonView;
    public int currentPlayerTurn = 0;  // Track which player is currently taking their turn
    public GameObject UI;  // Reference to the UI for the player's turn
    public Deck DeckInstance;
    public Slider raiseSlider;
    private int newChipCount = 0;
    public TextMeshProUGUI raiseAmountText;

	public bool firstTurn = false;
	private bool lastround = false;
	public TextMeshProUGUI callAmountText;
	 public TextMeshProUGUI potAmountText;
	     private bool turn = false;
	    private bool flop = false;
    private bool river = false;
	private int playersFinished = 0;
	public int currentRaiseAmount = 0;
	private bool playedonce = false;
	public List<Player> playersInGame; // List of players in the current round
    public int currentPlayerIndex = 0; // Tracks whose turn it is

	public int smallBlindIndex;
    public int bigBlindIndex;

public List<PlayerHandData> playerHandDataList = new List<PlayerHandData>();
    [Serializable]
public struct PlayerHandData
{
    public int ActorNumber;    // Player's ActorNumber
    public HandRank HandRank;  // Player's HandRank

    public PlayerHandData(int actorNumber, HandRank handRank)
    {
        ActorNumber = actorNumber;
        HandRank = handRank;
    }
}

public Dictionary<int, HandRank> playerHandRanks = new Dictionary<int, HandRank>();
public void RaiseClick()
{

        
        PhotonView localPhotonView = FindLocalPlayerPhotonView();
        if (localPhotonView != null)
        {
            // Get the PlayerCardHandler component from the local player
            PlayerCardHandler playerCardHandler = localPhotonView.GetComponent<PlayerCardHandler>();
            if (playerCardHandler != null)
            {
                // Deduct chips for the current raise amount
                int newChipCount = playerCardHandler.GetChipCount() - currentRaiseAmount;

                // Ensure the player has enough chips to make the raise
                if (newChipCount >= 0)
                {
                    // Call RPC to deduct chips on the MasterClient
                    playerCardHandler.photonView.RPC("DeductChipsRPC", RpcTarget.All, currentRaiseAmount);

                    // Update pot amount on all clients
                    potAmount += currentRaiseAmount;
					 
                    photonView.RPC("UpdatePotAmountRPC", RpcTarget.All, potAmount);

                    // Update chip count for all clients
                    playerCardHandler.photonView.RPC("UpdateChipCountRPC", RpcTarget.MasterClient, newChipCount);
	photonView.RPC("UpdateCallAmountText", RpcTarget.Others, currentRaiseAmount);
photonView.RPC("AmountToCall", RpcTarget.All, currentRaiseAmount);
                    EndTurn();
					  photonView.RPC("PlayerFinishedTurn", RpcTarget.MasterClient);
                }
                else
                {
                    //Debug.LogError("Player does not have enough chips to make this raise.");
                }
            }
            else
            {
                //Debug.LogError("PlayerCardHandler component not found on local PhotonView.");
            }
        }
        else
        {
            //Debug.LogError("Local PhotonView not found.");
        }
	
		photonView.RPC("raise", RpcTarget.All);

    
}
  public void OnSliderValueChange(float value)
    {
        currentRaiseAmount = Mathf.RoundToInt(value);
        UpdateRaiseAmountText();
    }
[PunRPC]
private void AmountToCall(int valeur)
{
	currentBetAmount = valeur;
	photonView.RPC("UpdateCallAmountText", RpcTarget.All, currentBetAmount);
	
}
private void FalseTurn()
{
	GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
foreach (GameObject player in players)
{
    PlayerCardHandler playerCardHandler = player.GetComponent<PlayerCardHandler>();
    if (playerCardHandler != null)
    {playerCardHandler.isMyTurn = false;
	}}
}


	public void RegisterPlayer(PlayerCardHandler player)
    {
players.Clear();

        // Find all PlayerController components in the scene
        PlayerCardHandler[] playerCardHandlers = FindObjectsOfType<PlayerCardHandler>();

        // Iterate over all found PlayerController components
        foreach (PlayerCardHandler playerCardHandler in playerCardHandlers)
        {
            // Add each player to the players list
            players.Add(playerCardHandler);

            // Optionally, //Debug.Log the player's name or other info
            //Debug.Log("Registered player: " + playerCardHandler.gameObject.name);
        }

        // Debugging log to check the number of players
        //Debug.Log("Total players registered: " + players.Count);
    }
    
	public void UpdateTurnUI()
    {

UI.SetActive(true);
 
    }
	 [PunRPC]
	private void ResetAmount()
    {
        currentRaiseAmount = 0;
        currentBetAmount = 0;
		photonView.RPC("UpdateCallAmountText", RpcTarget.All, currentBetAmount);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
	[PunRPC]
private void RotateBlindIndices()
{
    if (playerActorNumbers.Count > 0)
    {
        smallBlindIndex = (smallBlindIndex + 1) % playerActorNumbers.Count;
        bigBlindIndex = (smallBlindIndex + 1) % playerActorNumbers.Count;

        // Synchronize indices for all clients
        photonView.RPC("SyncBlindIndices", RpcTarget.All, smallBlindIndex, bigBlindIndex);
    }
}


[PunRPC]
public void PostSmallBlind(int playerActorNumber)
{
    DeductBlind(playerActorNumber, SMALL_BLIND_AMOUNT);
	       
}

[PunRPC]
public void updateSlider()
{
UpdateSliderValues();}
[PunRPC]
public void PostBigBlind(int playerActorNumber)
{
    DeductBlind(playerActorNumber, BIG_BLIND_AMOUNT);
	bigblind=true;
	          
}

private void DeductBlind(int playerActorNumber, int amount)
{
    GameObject playerObject = GameObject.FindGameObjectsWithTag("Player")
        .FirstOrDefault(go => go.GetComponent<PhotonView>().OwnerActorNr == playerActorNumber);
    if (playerObject != null)
    {
        PlayerCardHandler handler = playerObject.GetComponent<PlayerCardHandler>();
        if (handler != null)
        {
            handler.DeductBlind(amount);
            //Debug.Log($"Deducted {amount} chips from player {playerActorNumber}");
        }
        else
        {
            //Debug.LogWarning("PlayerCardHandler not found.");
        }
    }
    else
    {
        //Debug.LogWarning("Player object not found.");
    }
}

	[PunRPC]
private void UpdateCallAmountText(int displayedAmount)
{
    if (callAmountText != null)
    {
        if (currentPlayerTurn == smallBlindIndex &&!firstTurn)
        {
            displayedAmount -= SMALL_BLIND_AMOUNT;
        }
		if (currentPlayerTurn == bigBlindIndex &&!firstTurn &&!raisetrue)
        {
         displayedAmount=0;
		 
        }

        callAmountText.text = $"Call: {displayedAmount}";
		photonView.RPC("first", RpcTarget.All);
    }
}
 [PunRPC]
    private void UpdatePotAmountRPC(int newPotAmount)
    {
        potAmount = newPotAmount;
        potAmountText.text = $"Pot: {potAmount}";
        //Debug.Log($"Updated pot amount: {potAmount}"); // Debug to verify the update
    }


[PunRPC]
private void PostBlinds()
{
    //Debug.Log($"Posting blinds: Small Blind Index = {smallBlindIndex}, Big Blind Index = {bigBlindIndex}");

    // Post small blind
    photonView.RPC("PostSmallBlind", RpcTarget.MasterClient, playerActorNumbers[smallBlindIndex]);

    // Post big blind
    photonView.RPC("PostBigBlind", RpcTarget.MasterClient, playerActorNumbers[bigBlindIndex]);

    // Update pot and bet amounts
    currentBetAmount = BIG_BLIND_AMOUNT;
    potAmount = SMALL_BLIND_AMOUNT + BIG_BLIND_AMOUNT;

    // Update pot and call amounts for all clients
    photonView.RPC("UpdatePotAmountRPC", RpcTarget.All, potAmount);
    photonView.RPC("UpdateCallAmountText", RpcTarget.All, currentBetAmount);
}

public void OnFoldClick()
{PhotonView localPhotonView = FindLocalPlayerPhotonView();
if (localPhotonView != null)
    {
        PlayerCardHandler playerCardHandler = localPhotonView.GetComponent<PlayerCardHandler>();
        if (playerCardHandler.playerPosition == PlayerPosition.UTG)
        {

currentPlayerIndex = (currentPlayerIndex + 1) % totalPlayers;
int best = currentPlayerIndex;
photonView.RPC("SyncIndex", RpcTarget.All, best);
	
        }
		 if (PhotonNetwork.CurrentRoom.PlayerCount <= 2)
        {photonView.RPC("Reset", RpcTarget.All);
			            photonView.RPC("ResetTurnStatesForOthers", RpcTarget.All);

  
            photonView.RPC("ResetAmount", RpcTarget.All);
            photonView.RPC("first", RpcTarget.All);
			photonView.RPC("lastf", RpcTarget.All);
	Rotate();
	photonView.RPC("firstround1", RpcTarget.All);
	photonView.RPC("StartTurnUT", RpcTarget.MasterClient);
	DeclareWinner2P();
	RotateBlindIndices();
	photonView.RPC("NewRound", RpcTarget.MasterClient);
photonView.RPC("PostBlinds", RpcTarget.MasterClient);
 
		photonView.RPC("RestartGameRPC", RpcTarget.MasterClient);
        }}}
		
public void DeclareWinner2P()
{
    // Assuming you have access to the players and their fold status
    Player[] players = PhotonNetwork.PlayerList;

    // If the current player folds, the winner is the other player
    Player winner = null;

    foreach (var player in players)
    {
        if (player != PhotonNetwork.LocalPlayer)
        {
            winner = player;  // The other player is the winner
            break;
        }
    }

    // You can then notify the winner or update the UI
    if (winner != null)
    {
        Debug.Log(winner.NickName + " is the winner!");
         int winnerID = winner.ActorNumber;
		 photonView.RPC("TransferPotToWinner", RpcTarget.MasterClient, winnerID);
        // Optionally, you can update the UI to reflect the winner
        // ShowWinner(winner);
    }
}

  [PunRPC]
    private void RestartGameRPC()
    {if(PhotonNetwork.IsMasterClient)
		{
		                StartCoroutine(DeckInstance.DelayedRestart());				
    }
	}
	
 public IEnumerator Logo()
    {
        yield return new WaitForSeconds(1f); // Adjust the delay time as needed
	{
		GameObject[] pokerPlayerObjects = GameObject.FindGameObjectsWithTag("Player");
     if (pokerPlayerObjects.Length > 0)
    {
        // Loop through each GameObject in the array
        foreach (GameObject pokerPlayerObject in pokerPlayerObjects)
        {
            // Get the PhotonView component from the GameObject
            PhotonView photonView = pokerPlayerObject.GetComponent<PhotonView>();

            if (photonView != null)
            {
           photonView.RPC("Logos", RpcTarget.All);
        }
    }
}}}
        

	private void Search()
	{
		GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
foreach (GameObject player in players)
{
    PlayerCardHandler playerCardHandler = player.GetComponent<PlayerCardHandler>();
    if (playerCardHandler != null)
    {
        if (playerCardHandler.playerPosition == PlayerPosition.UTG)
        {
           
            //Debug.Log("Small blind index set to 1 for player at UTG position.");
        }
    }
}

	}
private PhotonView FindPhotonViewByActorNumber(int actorNumber)
{
    GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
    foreach (GameObject player in players)
    {
        PhotonView photonView = player.GetComponent<PhotonView>();
        if (photonView != null && photonView.Owner.ActorNumber == actorNumber)
        {
            return photonView;
        }
    }
    return null;
}
[PunRPC]
private void AnnounceWinner(int winnerID)
{
    //Debug.Log($"The winner is player {winnerID}");
    // Update UI or game state to reflect the winner
    if (PhotonNetwork.LocalPlayer.ActorNumber == winnerID)
    {
        Debug.Log("You are the winner!");
    }
}[PunRPC]
private void TransferPotToWinner(int winnerID, int amount)
{
    Debug.Log($"Transferring {amount} chips to player {winnerID}. Pot amount before transfer: {potAmount}");

    PhotonView winnerPhotonView = FindPhotonViewByActorNumber(winnerID);
    if (winnerPhotonView != null)
    {
        PlayerCardHandler playerCardHandler = winnerPhotonView.GetComponent<PlayerCardHandler>();
        if (playerCardHandler != null)
        {
            playerCardHandler.AddChips(amount);
            Debug.Log($"Added {amount} chips to player {winnerID}.");
        }
        else
        {
            Debug.LogError("PlayerCardHandler component not found.");
        }
    }
    else
    {
        Debug.LogError("PhotonView for winner not found.");
    }

    // Deduct the transferred amount from the pot
    potAmount -= amount;

    // Ensure the pot amount does not go negative
    if (potAmount < 0)
    {
        potAmount = 0;
    }
}
	public void Rotate()
{
GameObject[] pokerPlayerObjects = GameObject.FindGameObjectsWithTag("Player");
     if (pokerPlayerObjects.Length > 0)
    {
        // Loop through each GameObject in the array
        foreach (GameObject pokerPlayerObject in pokerPlayerObjects)
        {
            // Get the PhotonView component from the GameObject
            PhotonView photonView = pokerPlayerObject.GetComponent<PhotonView>();

            if (photonView != null)
            {
            photonView.RPC("RotatePlayerPositions", RpcTarget.All);
        }
    }
}}
[PunRPC]
private void last()
{
	lastround = true;
	photonView.RPC("firstround1", RpcTarget.All);
}
[PunRPC]
private void lastf()
{
	lastround = false;

}

 
public void StartNewRound()
{
	
	
    isFirstRound = true;
photonView.RPC("firstround1", RpcTarget.All);

    photonView.RPC("StartTurnUT", RpcTarget.All); // Start the turn with the small blind


    // Post blinds for the new round
    photonView.RPC("PostBlinds", RpcTarget.MasterClient);
}
[PunRPC]
private void firstfalse()
{
	 firstTurn = false;
}
[PunRPC]
private void raise()
{
	 raisetrue = true;
}
    public void CallButton()
{
    int callAmount = currentBetAmount;
    
    if (raisetrue)
    {
        callAmount = currentRaiseAmount;
    }

    if (currentPlayerIndex == smallBlindIndex && !firstTurn)
    {
        //Debug.Log("Small blind calling.");
        firstTurn = true;
        callAmount = BIG_BLIND_AMOUNT - SMALL_BLIND_AMOUNT;
    }
    else if (currentPlayerIndex == bigBlindIndex)
    {
        //Debug.Log("Big blind calling.");
        callAmount = currentBetAmount;
    }

    PhotonView localPhotonView = FindLocalPlayerPhotonView();
    if (localPhotonView != null)
    {
        PlayerCardHandler playerCardHandler = localPhotonView.GetComponent<PlayerCardHandler>();
        if (playerCardHandler != null)
        {
            int chipCount = playerCardHandler.GetChipCount();
            if (callAmount > chipCount)
            {
                callAmount = chipCount;
                int localPlayerActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
                ActivePlayers.Remove(localPlayerActorNumber);
                
                if (ActivePlayers.Count == 1)
                {
                    if (!flop)
                    {
                        photonView.RPC("floptrue", RpcTarget.All);
                        DeckInstance.photonView.RPC("DistributeAndAddCommunityCards", RpcTarget.AllViaServer);
                        DeckInstance.photonView.RPC("DealTurnCardRPC", RpcTarget.AllViaServer);
                        photonView.RPC("turntrue", RpcTarget.All);
                        DeckInstance.photonView.RPC("DealRiverCardRPC", RpcTarget.AllViaServer);
                        photonView.RPC("rivertrue", RpcTarget.All);
                    }
                    else if (flop && !turn)
                    {
                        DeckInstance.photonView.RPC("DealTurnCardRPC", RpcTarget.AllViaServer);
                        photonView.RPC("turntrue", RpcTarget.All);
                        DeckInstance.photonView.RPC("DealRiverCardRPC", RpcTarget.AllViaServer);
                        photonView.RPC("rivertrue", RpcTarget.All);
                    }
                    else if (flop && turn && !river)
                    {
                        DeckInstance.photonView.RPC("DealRiverCardRPC", RpcTarget.AllViaServer);
                        photonView.RPC("rivertrue", RpcTarget.All);
                    }
					
                }
            }
        playerCardHandler.photonView.RPC("DeductChipsRPC", RpcTarget.All, callAmount);}
    }


 

    potAmount += callAmount;
    photonView.RPC("UpdatePotAmountRPC", RpcTarget.All, potAmount);

  photonView.RPC("UpdateCallAmountText", RpcTarget.All, currentBetAmount);
    photonView.RPC("PlayerFinishedTurn", RpcTarget.MasterClient);
	
currentPlayerIndex = (currentPlayerIndex + 1) % totalPlayers;
int best = currentPlayerIndex;
photonView.RPC("SyncIndex", RpcTarget.All, best);
photonView.RPC("NotifyTurn", RpcTarget.All, currentPlayerIndex);
        
}


    [PunRPC]
	public void floptrue()
    {
        flop = true;
	  }
	  [PunRPC]
	public void turntrue()
    {
        turn = true;
	  }
	  [PunRPC]
	public void rivertrue()
    {
        river = true;
	  }
	   [PunRPC]
    private void ResetTurnStatesForOthers()
    {
        playersFinished = 0;
        // Handle any additional state reset logic for other players
    }
  [PunRPC]
    public void AssignActive()
    {
		ActivePlayers.Clear();

            playerActorNumbers.Clear();
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                ActivePlayers.Add(player.ActorNumber);
				 playerActorNumbers.Add(player.ActorNumber);
            }
	}
    [PunRPC]
    public void PlayerFinishedTurn()
    {
        playersFinished++;
        if (playersFinished >= totalPlayers && !flop)
        {
            photonView.RPC("floptrue", RpcTarget.All);
            DeckInstance.photonView.RPC("DistributeAndAddCommunityCards", RpcTarget.AllViaServer);
            photonView.RPC("ResetTurnStatesForOthers", RpcTarget.All);
            photonView.RPC("ResetAmount", RpcTarget.All);
        }
        else if (playersFinished >= totalPlayers && flop && !turn)
        {
            photonView.RPC("ResetTurnStatesForOthers", RpcTarget.All);
            DeckInstance.photonView.RPC("DealTurnCardRPC", RpcTarget.AllViaServer);
            photonView.RPC("turntrue", RpcTarget.All);
            photonView.RPC("ResetAmount", RpcTarget.All);
        }
        else if (playersFinished >= totalPlayers && flop && turn && !river)
        {
            photonView.RPC("ResetTurnStatesForOthers", RpcTarget.All);
            DeckInstance.photonView.RPC("DealRiverCardRPC", RpcTarget.AllViaServer);
			
            photonView.RPC("rivertrue", RpcTarget.All);
        }
        else if (playersFinished >= totalPlayers && flop && turn && river)
        {  
			Invoke("Final", 0.5f);
			 Invoke("RestartD", 2f);
			//Debug.Log($"DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
			
			//Debug.Log($"RRRRRRRRRRRRRRRRRRR");
        }
    }private void Final()
	{
	 photonView.RPC("SendHandRanks", RpcTarget.All);
            photonView.RPC("ResetTurnStatesForOthers", RpcTarget.All);
            RotateBlindIndices();
            photonView.RPC("Reset", RpcTarget.All);
            photonView.RPC("ResetAmount", RpcTarget.All);
            
			photonView.RPC("lastf", RpcTarget.All);
			photonView.RPC("firstfalse", RpcTarget.All);}
	private void RestartD()
	{

			photonView.RPC("StartTurnUT", RpcTarget.All);
			photonView.RPC("NewRound", RpcTarget.MasterClient);
			
			photonView.RPC("PostBlinds", RpcTarget.MasterClient);
			
	}
	[PunRPC]
    public void SendHandRanks()
    {
        PhotonView localPhotonView = FindLocalPlayerPhotonView();
        if (localPhotonView != null)
        {
            PlayerCardHandler playerCardHandler = localPhotonView.GetComponent<PlayerCardHandler>();
            if (playerCardHandler != null)
            {
                HandRank handRank = playerCardHandler.GetPlayerHandRank();
                Debug.Log($"Sending hand rank for player {PhotonNetwork.LocalPlayer.ActorNumber}: {handRank}");
                
                // Ensure the correct RPC method name and parameters
                photonView.RPC("ReceiveHandRank", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, handRank);
            }
            else
            {
                //Debug.LogError("PlayerCardHandler component not found.");
            }
        }
        else
        {
            //Debug.LogError("Local PhotonView not found.");
        }
    }[PunRPC]
public void ReceiveHandRank(int playerID, HandRank handRank)
{
    // Check if the player data already exists and update it
    bool playerExists = false;

    for (int i = 0; i < playerHandDataList.Count; i++)
    {
        if (playerHandDataList[i].ActorNumber == playerID)
        {
            playerHandDataList[i] = new PlayerHandData(playerID, handRank);
            playerExists = true;
            break;
        }
    }

    // If the player doesn't exist, add them to the list
    if (!playerExists)
    {
        playerHandDataList.Add(new PlayerHandData(playerID, handRank));
    }

    Debug.Log($"Received hand rank for player {playerID}: {handRank}");
    Debug.Log($"Total players with hand ranks: {playerHandDataList.Count}");

    // Check if all players have submitted their ranks
    if (playerHandDataList.Count == PhotonNetwork.PlayerList.Length)
    {
        Debug.Log("All hand ranks received. Evaluating winner...");
        EvaluateAndDeclareWinner();  // Call the method to evaluate the winner
    }
    else
    {
        Debug.LogWarning("Not all players have reported their hand ranks yet.");
    }
}private void EvaluateAndDeclareWinner()
{
    if (playerHandDataList.Count == 0)
    {
        Debug.LogError("No hand data to evaluate.");
        return;
    }

    PlayerHandData bestHand = playerHandDataList[0];
    List<int> winners = new List<int> { bestHand.ActorNumber };

    // Compare each player's hand rank
    for (int i = 1; i < playerHandDataList.Count; i++)
    {
        var currentHand = playerHandDataList[i];

        if (currentHand.HandRank > bestHand.HandRank)
        {
            // New best hand found
            bestHand = currentHand;
            winners.Clear();
            winners.Add(currentHand.ActorNumber);
        }
        else if (currentHand.HandRank == bestHand.HandRank)
        {
            // Tie, add to winners
            winners.Add(currentHand.ActorNumber);
        }
    }

    // Handle the result
    if (winners.Count == 1)
    {
        Debug.Log($"Player {winners[0]} wins with hand rank: {bestHand.HandRank}");
        photonView.RPC("TransferPotToWinner", RpcTarget.MasterClient, winners[0], potAmount);
    }
    else
    {
        Debug.Log($"Split pot among {winners.Count} players with hand rank: {bestHand.HandRank}");
        int splitAmount = potAmount / winners.Count;
        foreach (int winnerID in winners)
        {
            photonView.RPC("TransferPotToWinner", RpcTarget.MasterClient, winnerID, splitAmount);
        }
    }

    // Reset game state
    photonView.RPC("Reset", RpcTarget.All);
    photonView.RPC("ResetPot", RpcTarget.All);
}
[PunRPC]
private void ResetPot()
{
	potAmount=0;
}
[PunRPC]
private void EvaluateWinner()
{
    
}
	[PunRPC]
private void Reset()
{
	flop = false;
	turn = false;
	river = false;
}
[PunRPC]
	private void first()
	{
		firstTurn = true;
	}
	
void InitializeGame()
{
    if (!PhotonNetwork.IsMasterClient)
    {
        //Debug.LogWarning("Only the Master Client can initialize the game.");
        return;
    }

    // Ensure the playersInGame list is empty before adding players
    playersInGame = new List<Player>(PhotonNetwork.PlayerList);

    if (playersInGame.Count == 0)
    {
        //Debug.LogError("No players in the game. Initialization failed.");
        return;
    }

    
    //Debug.Log("Game initialized with the following players:");
    foreach (var player in playersInGame)
    {
        //Debug.Log($"- {player.NickName} (Actor Number: {player.ActorNumber})");
    }

     InitializePlayerUI();
    
    // Set initial turn (you can set it to 0 to start with the first player)
    currentPlayerTurn = 1;
}
private void UpdateSliderValues()
    {
		PhotonView localPhotonView = FindLocalPlayerPhotonView();
        if (localPhotonView != null)
        {
            PlayerCardHandler playerCardHandler = localPhotonView.GetComponent<PlayerCardHandler>();
            if (playerCardHandler != null && raiseSlider != null)
            {
       
            int chipCount = playerCardHandler.GetChipCount();
            raiseSlider.minValue = 0;
            raiseSlider.maxValue = chipCount;  // Set the slider’s maximum value to the chip count
            raiseSlider.value = 0; 
			newChipCount = chipCount;// Optionally set the slider’s value to match chip count
        
    } } }
 private void UpdateRaiseAmountText()
    {
        if (raiseAmountText != null && raiseSlider != null)
        {
            raiseAmountText.text = $"Raise: {Mathf.RoundToInt(raiseSlider.value)}";
        }
        else
        {
            //Debug.LogError("TextMeshProUGUI or Slider reference is not assigned.");
        }
    }
    void Start()
    {    raiseSlider.onValueChanged.AddListener(OnSliderValueChange);
	 
 currentPlayerIndex = 1;
        totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
photonView = GetComponent<PhotonView>();
        playerActorNumbers.Clear();
		ActivePlayers.Clear();
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            playerActorNumbers.Add(player.ActorNumber);
			ActivePlayers.Add(player.ActorNumber);
            //Debug.Log($"Player added: {player.NickName} with ActorNumber {player.ActorNumber}");
        }
    }
[PunRPC]
public void UpdateCurrentPlayerTurn(int newTurn)
{
    currentPlayerTurn = newTurn;
    //Debug.Log($"Updated currentPlayerTurn to: {currentPlayerTurn}");
}
 public override void OnPlayerEnteredRoom(Player newPlayer)
    {

smallBlindIndex=1;
    foreach (var player in players)
    {
        //Debug.Log($"Player in list: {player.playerName}, Position: {player.playerPosition}");
    }
		InitializeGame();
	Search();
		totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
    
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
 ActivePlayers.Clear();

            playerActorNumbers.Clear();
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                ActivePlayers.Add(player.ActorNumber);
				 playerActorNumbers.Add(player.ActorNumber);
            }

  

 

        }totalPlayers = PhotonNetwork.PlayerList.Length;currentPlayerIndex = 1;
	}
public void EndTurn()
    {

currentPlayerIndex = (currentPlayerIndex + 1) % totalPlayers;
int best = currentPlayerIndex;
photonView.RPC("SyncIndex", RpcTarget.All, best);
photonView.RPC("NotifyTurn", RpcTarget.All, currentPlayerIndex);
        
    }


    [PunRPC]
    void NotifyTurn(int currentPlayerIndex)
    {

		
		int currentActorNumber = playerActorNumbers[currentPlayerIndex];
        if (PhotonNetwork.LocalPlayer.ActorNumber == currentActorNumber)
        {
            // Enable UI for the current player
            UI.SetActive(true);
        }
        else
        {
            // Disable UI for other players
            UI.SetActive(false);
        }
    }

 
    
    
    // Function to assign the player number based on their actor number


    // Function to set the player's position based on player number
    

    // RPC to start sitting down when the game begins
    [PunRPC]
    public void StartSit()
    {
        if (IsAnyPlayerInstantiated())
        {photonView.RPC("RestartGameRPC", RpcTarget.MasterClient);
        
        }
    }
	[PunRPC]
private void firstround1()
{
	firstround = true;
}
[PunRPC]
private void firstroundfalse()
{
	firstround = false;
}
public void SwitchPlayerUI()
{
    if (players.Count == 0)
    {
        //Debug.LogError("No players available in the game.");
        return;
    }

    // Deactivate UI for the current player


    // Update turn to the next player
    currentPlayerTurn = (currentPlayerTurn + 1) % players.Count;

    // Activate UI for the next player

}

public void TogglePlayerUI(bool isActive)
{
    if (UI != null)
    {
        UI.SetActive(isActive);
    }
}

	[PunRPC]
	private void NewRound()
	{if(totalPlayers ==2)
		{
			{smallBlindIndex = (smallBlindIndex + 1) % playerActorNumbers.Count;
				bigBlindIndex = (smallBlindIndex + 1) % playerActorNumbers.Count; // Rotates between 0 and 1
    photonView.RPC("SyncBlindIndices", RpcTarget.AllBuffered, smallBlindIndex, bigBlindIndex);
}}}
	[PunRPC]
private void SyncBlindIndices(int smallBlind, int bigBlind)
{
    smallBlindIndex = smallBlind;
    bigBlindIndex = bigBlind;

    //Debug.Log($"Synchronized blinds: Small Blind Index = {smallBlindIndex}, Big Blind Index = {bigBlindIndex}");
}
[PunRPC]
private void SyncIndex(int best)
{
    currentPlayerIndex = best;
    
}
	public void StartTurn(int playerIndex)
    {
        

        int currentActorNumber = playerActorNumbers[playerIndex];
        //Debug.Log($"Starting turn for player with ActorNumber {currentActorNumber}");

        if (PhotonNetwork.LocalPlayer.ActorNumber == currentActorNumber)
        {
          
            UI.SetActive(true);
            // Activate UI elements for the player's turn
        }
        else
        {
          
            UI.SetActive(false); // Deactivate UI elements for other players
        }

        // //Debug.Logs to check if the method is being called correctly

        //Debug.Log($"Current Player Index: {currentPlayerTurn}");
        //Debug.Log($"Current Player ActorNumber: {currentActorNumber}");
    }


 [PunRPC]
    public void StartTurnRPC(int newPlayerIndex)
    {
        currentPlayerTurn = newPlayerIndex;
        StartTurn(currentPlayerTurn);  // Ensure the correct index is passed
    }

private void StartTurnWithDelay()
{
    StartCoroutine(DelayedStartTurn());
}
public void InitializePlayerUI()
{
    foreach (var player in players)
    {

    }

    // Activate the first player's UI based on the initial turn
    PlayerCardHandler firstPlayerHandler = players.Find(player => player.playerPosition == (PlayerPosition)currentPlayerTurn);
    if (firstPlayerHandler != null)
    {

    }
}


 [PunRPC]
    public void SyncCurrentPlayerTurn(int newTurn)
    {
        currentPlayerTurn = newTurn;
        //Debug.Log($"Turn synchronized: Current Player Turn is now Player {playerActorNumbers[currentPlayerTurn]}");
        // You can update your UI or any other logic related to the turn here
    }
	

   
private IEnumerator DelayedStartTurn()
{
    // Wait for 0.5 seconds
    yield return new WaitForSeconds(0.5f);
    
    // Now call the RPC after the delay
    photonView.RPC("StartTurnRPC2", RpcTarget.All, currentPlayerTurn);
}

    public PhotonView FindLocalPlayerPhotonView()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            PhotonView photonView = player.GetComponent<PhotonView>();
            if (photonView != null && photonView.Owner == PhotonNetwork.LocalPlayer)
            {
                return photonView;
            }
        }

        return null; // Return null if not found (handle this case in your logic)
    }

[PunRPC]
public void StartTurnUT()
{

photonView.RPC("firstfalse", RpcTarget.All);



       photonView.RPC("StartTurnUTG", RpcTarget.All);
		// Start turn only if the newPlayerIndex is valid
}

    // Check if there are any players instantiated
    private bool IsAnyPlayerInstantiated()
    {
        return GameObject.FindGameObjectsWithTag("Player").Length > 1;
    }

    
	
[PunRPC]
public void StartTurnUTG()
{ 

    PhotonView localPhotonView = FindLocalPlayerPhotonView();
    if (localPhotonView != null)
    {
        PlayerCardHandler playerCardHandler = localPhotonView.GetComponent<PlayerCardHandler>();
        if (playerCardHandler.playerPosition == PlayerPosition.UTG)
        {
            // Activate UI for the UTG player
          UpdateSliderValues();
            UI.SetActive(true);
			
			playedonce = true;
        }
		else{
			
			UI.SetActive(false);
		}
		}
			
		
		
    }

	[PunRPC]
	public void NextTurnRPC()
{
    PhotonView localPhotonView = FindLocalPlayerPhotonView();
    if (localPhotonView != null)
    {
        PlayerCardHandler playerCardHandler = localPhotonView.GetComponent<PlayerCardHandler>();
        if (playerCardHandler.playerPosition == PlayerPosition.DEALER)
        {
            // Activate UI for the UTG player
            
            UI.SetActive(false);
        }	
		else{
			
			UI.SetActive(false);
		}
		
	}}
	





}
