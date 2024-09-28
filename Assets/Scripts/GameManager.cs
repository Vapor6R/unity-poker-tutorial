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
public class GameManager : MonoBehaviourPunCallbacks
{private Dictionary<int, HandRank> playerHandRanks = new Dictionary<int, HandRank>();
	 private int playersFinished = 0;
    private int totalPlayers;
	private GameState currentState;
public PlayerCardHandler playerCardHandler;
public Dictionary<int, PlayerAction> playerActions;
private bool smallBlindAlreadyDeducted = false;
    public List<int> playerActorNumbers = new List<int>();
    public GameObject UI;
    public Slider raiseSlider;
	private List<Photon.Realtime.Player> players;
    public TextMeshProUGUI raiseAmountText;
    private int currentPlayerIndex = 0;
    private bool isPlayerTurn = false;
    private bool comcard = false;
    private bool turn = false;
	    private bool flop = false;
    private bool river = false;
	private bool PlayerRaise = false;
    private int currentBetAmount = 0;
    public TextMeshProUGUI callAmountText;
    public Deck DeckInstance;
    public int potAmount = 0;
    private int turnCount = 0;
    public const int SMALL_BLIND_AMOUNT = 10;
    public const int BIG_BLIND_AMOUNT = 20;
    public TextMeshProUGUI potAmountText;
    public int smallBlindIndex;
    public int bigBlindIndex;
    private bool distributed = false;
	private bool firstblood = false;
	private int currentRaiseAmount = 0;
		private int newChipCount = 0;
		private bool firsturn = false;
		private bool hasplayedonce = false;
private bool hasplayed = false;
public Transform[] seatingPositions; // Assign these in the Inspector
    private static bool[] positionOccupied; 
	public Transform cameraTransform; // Assign the Camera's transform in the Inspector
    public Transform targetPositionTransform;// Desired camera position
    public float rotationSpeed = 10f;
	public static GameManager Instance;
public enum PlayerAction
    {
        Waiting,
        Called,
        Raised,
        Folded
    }
 public enum GameState
    {
        Waiting,
        Playing,
        BettingRound,
        DistributingFlop,
        DistributingTurn,
        DistributingRiver
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
    public void UpdatePlayerHandRankRPC(int playerID, HandRank handRank)
    {
        playerHandRanks[playerID] = handRank;
        Debug.Log($"Received hand rank for player {playerID}: {handRank}");
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
	[PunRPC]
	public void PlayerFinishedTurn()
    {
        playersFinished++;
        if (playersFinished >= totalPlayers &&!flop)
        {
           photonView.RPC("floptrue", RpcTarget.All);
            DeckInstance.photonView.RPC("DistributeAndAddCommunityCards", RpcTarget.AllViaServer);
      photonView.RPC("ResetTurnStatesForOthers", RpcTarget.All);
	  photonView.RPC("EndFlop", RpcTarget.All);
	  }
	  else if(playersFinished >= totalPlayers &&flop && !turn)
	  {photonView.RPC("ResetTurnStatesForOthers", RpcTarget.All);
		   DeckInstance.photonView.RPC("DealTurnCardRPC", RpcTarget.AllViaServer);
		 photonView.RPC("turntrue", RpcTarget.All);
		 photonView.RPC("EndFlop", RpcTarget.All);
	  }
	  else if(playersFinished >= totalPlayers &&flop &&turn && !river)
	  {DeckInstance.photonView.RPC("DealRiverCardRPC", RpcTarget.AllViaServer);
  photonView.RPC("RequestHandRanks", RpcTarget.MasterClient);
StartCoroutine(DelayedEvaluateWinner(1f));
		  photonView.RPC("rivertrue", RpcTarget.All);
photonView.RPC("ResetTurnStatesForOthers", RpcTarget.All);
photonView.RPC("RestartGameRPC", RpcTarget.MasterClient);
photonView.RPC("Reset", RpcTarget.All);
photonView.RPC("EndFlop", RpcTarget.All);
photonView.RPC("firsurn", RpcTarget.All);
	  }
    }
private IEnumerator DelayedEvaluateWinner(float delay)
{
    yield return new WaitForSeconds(delay);
    photonView.RPC("EvaluateWinner", RpcTarget.MasterClient);
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
	 
    
    void Start()
    {totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
		currentState = GameState.Playing;
		playerActions = new Dictionary<int, PlayerAction>();
		raiseSlider.onValueChanged.AddListener(OnSliderValueChange);
	players = new List<Photon.Realtime.Player>(PhotonNetwork.PlayerList);
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            Debug.Log("Player count is sufficient, processing player data...");
            playerActorNumbers.Clear();
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                playerActorNumbers.Add(player.ActorNumber);
                Debug.Log($"Player added: {player.NickName} with ActorNumber {player.ActorNumber}");
            }
        }

    }
	private void RotateTableForLocalPlayer()
{
    // Rotate the table or camera to align with the local player's view
    transform.RotateAround(Vector3.zero, Vector3.up, 40);
}

 private void ArrangeOtherPlayers()
    {
        PhotonView[] allPlayers = FindObjectsOfType<PhotonView>();

        int seatingIndex = 1; // Start from the second position in the array

        foreach (PhotonView player in allPlayers)
        {
            if (player.IsMine)
            {
                continue; // Skip the local player
            }

            // Find the next available position
            while (positionOccupied[seatingIndex] && seatingIndex < seatingPositions.Length)
            {
                seatingIndex++;
            }

            if (seatingIndex < seatingPositions.Length)
            {
                // Position the other players around the table
                player.transform.position = seatingPositions[seatingIndex].position;
                positionOccupied[seatingIndex] = true;
                seatingIndex++;
            }
        }
    }
public void SitPlayer(int positionIndex)
{
    
}
	[PunRPC]
	private void EndFlop()
    {
        currentRaiseAmount = 0;
        currentBetAmount = 0;
    }
	[PunRPC]
public void PostSmallBlind(int playerActorNumber)
{
    Debug.Log($"PostSmallBlind called for playerActorNumber: {playerActorNumber}");

    // Find the player
    Player player = PhotonNetwork.PlayerList.FirstOrDefault(p => p.ActorNumber == playerActorNumber);
    if (player != null)
    {
        Debug.Log($"Player found: {player.NickName}");
        
        // Find the PhotonView for the player
        GameObject playerObject = GameObject.FindGameObjectsWithTag("Player")
        .FirstOrDefault(go => go.GetComponent<PhotonView>().OwnerActorNr == playerActorNumber);
        if (playerObject != null)
    {
        PhotonView playerPhotonView = playerObject.GetComponent<PhotonView>();
        if (playerPhotonView != null)
        {
            PlayerCardHandler playerCardHandler = playerPhotonView.GetComponent<PlayerCardHandler>();
            if (playerCardHandler != null)
            {
                playerCardHandler.DeductBlind(SMALL_BLIND_AMOUNT);
                Debug.Log($"Deducted {SMALL_BLIND_AMOUNT} from player with ActorNumber: {playerActorNumber}");
            }
            else
            {
                Debug.LogWarning("PlayerCardHandler not found on PhotonView.");
            }
        }
        else
        {
            Debug.LogWarning("PhotonView not found on player GameObject.");
        }
    }
    else
    {
        Debug.LogWarning("Player GameObject not found.");
    }
}}

[PunRPC]
public void PostBigBlind(int playerActorNumber)
{
     Debug.Log($"PostSmallBlind called for playerActorNumber: {playerActorNumber}");

    // Find the player
    Player player = PhotonNetwork.PlayerList.FirstOrDefault(p => p.ActorNumber == playerActorNumber);
    if (player != null)
    {
        Debug.Log($"Player found: {player.NickName}");
        
        // Find the PhotonView for the player
        GameObject playerObject = GameObject.FindGameObjectsWithTag("Player")
        .FirstOrDefault(go => go.GetComponent<PhotonView>().OwnerActorNr == playerActorNumber);
        if (playerObject != null)
    {
        PhotonView playerPhotonView = playerObject.GetComponent<PhotonView>();
        if (playerPhotonView != null)
        {
            PlayerCardHandler playerCardHandler = playerPhotonView.GetComponent<PlayerCardHandler>();
            if (playerCardHandler != null)
            {
                playerCardHandler.DeductBlind(BIG_BLIND_AMOUNT);
                Debug.Log($"Deducted {BIG_BLIND_AMOUNT} from player with ActorNumber: {playerActorNumber}");
            }
            else
            {
                Debug.LogWarning("PlayerCardHandler not found on PhotonView.");
            }
        }
        else
        {
            Debug.LogWarning("PhotonView not found on player GameObject.");
        }
    }
    else
    {
        Debug.LogWarning("Player GameObject not found.");
    }
}
}
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        Debug.Log("New player joined: " + newPlayer.NickName);
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2 && PhotonNetwork.IsMasterClient)
        {
            playerActorNumbers.Clear();
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                playerActorNumbers.Add(player.ActorNumber);
            }

                


        }
    }

    private bool IsAnyPlayerInstantiated()
    {
        return GameObject.FindGameObjectsWithTag("Player").Length > 1;
    }

    [PunRPC]
    public void StartSit()
    {
        if (IsAnyPlayerInstantiated())
        {
            DeckInstance.photonView.RPC("ShuffleDeckRPC", RpcTarget.MasterClient);
			StartCoroutine(WaitForPlayersInitialization());
			
            StartCoroutine(DeckInstance.DelayedDistributeCards());
			
        }

    }
	 
    private IEnumerator WaitForPlayersInitialization()
    {
		
        yield return new WaitForSeconds(1f);
				
        StartGame();
    }

    private void StartGame()
    { 
 foreach (var player in PhotonNetwork.PlayerList)
        {
            playerActions[player.ActorNumber] = PlayerAction.Waiting;
        }
        currentPlayerIndex = 0;
        Debug.Log("Starting the game.");
        potAmount = 0;
        turnCount = 0;
        smallBlindIndex = 0;
        bigBlindIndex = (smallBlindIndex + 1) % playerActorNumbers.Count;
       
        StartTurn((bigBlindIndex + 1) % playerActorNumbers.Count);
		 photonView.RPC("PostBlinds", RpcTarget.MasterClient);
    }
	  void MoveCameraToPosition1()
    {
      cameraTransform.position = targetPositionTransform.position;
        cameraTransform.rotation = targetPositionTransform.rotation;
    }

    void RotateScene()
    {
        // Rotate the entire scene or a specific GameObject
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime); // Rotate around the y-axis
    }
[PunRPC]
private void PostBlinds()
{
    // Post small blind for the player in MasterClient
    photonView.RPC("PostSmallBlind", RpcTarget.MasterClient, playerActorNumbers[smallBlindIndex]);

    // Post big blind for other players
    photonView.RPC("PostBigBlind", RpcTarget.OthersBuffered, playerActorNumbers[bigBlindIndex]);

    // Calculate and update pot amount and current bet amount
    currentBetAmount = BIG_BLIND_AMOUNT; // Since big blind is posted separately
    potAmount = SMALL_BLIND_AMOUNT + BIG_BLIND_AMOUNT;

    // Update pot amount for all clients
    photonView.RPC("UpdatePotAmountRPC", RpcTarget.All, potAmount);
	photonView.RPC("UpdateCallAmountText", RpcTarget.All, currentBetAmount);
	
}




    [PunRPC]
    private void RestartGameRPC()
    {if(PhotonNetwork.IsMasterClient)
		{
        Debug.Log("Restarting game...");

        StartGame();
		
		                StartCoroutine(DeckInstance.DelayedRestart());
    }
	}
[PunRPC]
private void Reset()
{
	flop = false;
	turn = false;
	river = false;
}
    [PunRPC]
    private void StartTurnRPC(int newPlayerIndex)
    {
        currentPlayerIndex = newPlayerIndex;
        StartTurn(currentPlayerIndex);
    }


private void StartTurn(int playerIndex)
{
    PhotonView localPhotonView = FindLocalPlayerPhotonView();
    if (localPhotonView != null)
    {
        PlayerCardHandler playerCardHandler = localPhotonView.GetComponent<PlayerCardHandler>();
        if (playerCardHandler != null)
        {
            UpdateSliderValues(); // Update UI slider values if necessary
        }
        else
        {
            Debug.LogError("PlayerCardHandler reference is not assigned.");
        }
    }
    else
    {
        Debug.LogError("Local PhotonView not found.");
    }

    int currentActorNumber = playerActorNumbers[playerIndex];
    Debug.Log($"Starting turn for player with ActorNumber {currentActorNumber}");

    if (PhotonNetwork.LocalPlayer.ActorNumber == currentActorNumber)
    {
        isPlayerTurn = true;
        UI.SetActive(true);
		// Activate UI elements for the player's turn
    }
    else
    {
        isPlayerTurn = false;
        UI.SetActive(false); // Deactivate UI elements for other players
    }

    // Debug logs to check if the method is being called correctly
    Debug.Log($"Is Player Turn: {isPlayerTurn}");
    Debug.Log($"Current Player Index: {currentPlayerIndex}");
    Debug.Log($"Current Player ActorNumber: {currentActorNumber}");
}
private void EndTurn()
{
    isPlayerTurn = false;
    UI.SetActive(false);
    currentPlayerIndex = (currentPlayerIndex + 1) % playerActorNumbers.Count;
            photonView.RPC("StartTurnRPC", RpcTarget.All, currentPlayerIndex);    

}
 private bool AllPlayersHavePlayed()
    {
        foreach (var action in playerActions.Values)
        {
            if (action == PlayerAction.Waiting)
            {
                return false;
            }
        }
        return true;
    }

	private bool AllPlayersHaveCalledOrFolded()
    {
        foreach (var action in playerActions.Values)
        {
            if (action == PlayerAction.Raised || action == PlayerAction.Waiting)
            {
                return false;
            }
        }
        return true;
    }

    public void FoldClick()
    {
        if (isPlayerTurn)
        {
            EndTurn();
            if (PhotonNetwork.CurrentRoom.PlayerCount <= 2)
            {
                Debug.Log("Player folded. Restarting game...");
                photonView.RPC("RestartGameRPC", RpcTarget.MasterClient);
            }
SetPlayerAction(PlayerAction.Folded);
        photonView.RPC("PlayerFinishedTurn", RpcTarget.MasterClient);
		}
    }
	private void SetPlayerAction(PlayerAction action)
    {
        int playerId = PhotonNetwork.LocalPlayer.ActorNumber;
        playerActions[playerId] = action;

        Hashtable props = new Hashtable
        {
            { "PlayerAction", action }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        CheckAllPlayersActions();
    }
	  private void CheckAllPlayersActions()
    {
        if (currentState == GameState.Playing && AllPlayersHavePlayed())
        {
            currentState = GameState.BettingRound;
        }
        else
        {
            
        }
    }
	[PunRPC]
	private void firsurn()
	{
		firsturn = true;
	}
public void CallButton()
{
    if (isPlayerTurn)
    {
        // Determine the amount to call (currentBetAmount)
        int callAmount = currentBetAmount;

        // Adjust call amount if the player is the small blind
        if (currentPlayerIndex == smallBlindIndex &&!firsturn)
        {
			Debug.Log("First call BB-SB");
firsturn = true;
            callAmount = BIG_BLIND_AMOUNT - SMALL_BLIND_AMOUNT;
		
        }

        // Deduct chips from the player's chip count
        PhotonView localPhotonView = FindLocalPlayerPhotonView();
        if (localPhotonView != null)
        {
            // Get the PlayerCardHandler component from the local player
            PlayerCardHandler playerCardHandler = localPhotonView.GetComponent<PlayerCardHandler>();
            if (playerCardHandler != null)
            {
            playerCardHandler.photonView.RPC("DeductChipsRPC", RpcTarget.All, callAmount);

            // Update pot amount on all clients
            potAmount += callAmount;
            photonView.RPC("UpdatePotAmountRPC", RpcTarget.All, potAmount);
        }

        photonView.RPC("PlayerFinishedTurn", RpcTarget.MasterClient);
        EndTurn();
	SetPlayerAction(PlayerAction.Called);
		photonView.RPC("UpdateCallAmountText", RpcTarget.Others, currentRaiseAmount);
    }
}}

public void RaiseClick()
{
    if (isPlayerTurn)
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
                }
                else
                {
                    Debug.LogError("Player does not have enough chips to make this raise.");
                }
            }
            else
            {
                Debug.LogError("PlayerCardHandler component not found on local PhotonView.");
            }
        }
        else
        {
            Debug.LogError("Local PhotonView not found.");
        }
		ResetOtherPlayersTurnStates();
		photonView.RPC("PlayerFinishedTurn", RpcTarget.MasterClient);
	SetPlayerAction(PlayerAction.Raised);
    }
}
[PunRPC]
private void AmountToCall(int valeur)
{
	currentBetAmount = valeur;
}

    [PunRPC]
    private void ResetTurnStatesForOthers()
    {
        playersFinished = 0;
        // Handle any additional state reset logic for other players
    }
private void ResetOtherPlayersTurnStates()
    {
        // Reset playersFinished for all players except the one who raised
        photonView.RPC("ResetTurnStatesForOthers", RpcTarget.Others);
    }
	private void HandlePlayerCheck()
{
    Debug.Log("Player is checking.");

    // Perform any necessary game state adjustments for a check
    EndTurn(); // Assuming ending the turn after a check in your game logic
}
private PhotonView FindLocalPlayerPhotonView()
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
private void UpdateCallAmountText(int displayedAmount)
{
    if (callAmountText != null)
    {
        if (currentPlayerIndex == smallBlindIndex)
        {
            displayedAmount -= SMALL_BLIND_AMOUNT;
        }
		if (currentPlayerIndex == bigBlindIndex)
        {
            displayedAmount -= BIG_BLIND_AMOUNT;
        }

        callAmountText.text = $"Call: {displayedAmount}";
    }
}

  public void OnSliderValueChange(float value)
    {
        currentRaiseAmount = Mathf.RoundToInt(value);
        UpdateRaiseAmountText();
    }
     

	 private void UpdateRaiseAmountText()
    {
        if (raiseAmountText != null && raiseSlider != null)
        {
            raiseAmountText.text = $"Raise: {Mathf.RoundToInt(raiseSlider.value)}";
        }
        else
        {
            Debug.LogError("TextMeshProUGUI or Slider reference is not assigned.");
        }
    }
    [PunRPC]
    private void UpdatePotAmountRPC(int newPotAmount)
    {
        potAmount = newPotAmount;
        potAmountText.text = $"Pot: {potAmount}";
        Debug.Log($"Updated pot amount: {potAmount}"); // Debug to verify the update
    }
[PunRPC]
public void RequestHandRanks()
{
    Debug.Log("Requesting hand ranks from all players.");
    photonView.RPC("SendHandRanks", RpcTarget.All);
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
                Debug.LogError("PlayerCardHandler component not found.");
            }
        }
        else
        {
            Debug.LogError("Local PhotonView not found.");
        }
    }

[PunRPC]
public void ReceiveHandRank(int playerID, HandRank handRank)
{
    playerHandRanks[playerID] = handRank;
    Debug.Log($"Received hand rank for player {playerID}: {handRank}");

    if (playerHandRanks.Count == PhotonNetwork.PlayerList.Length)
    {
        Debug.Log("All hand ranks received. Evaluating winner...");
		Debug.Log("Evaluating winner...");

    // Debug log for hand ranks
    foreach (var entry in playerHandRanks)
    {
        Debug.Log($"Player {entry.Key} hand rank: {entry.Value}");
    }

    if (playerHandRanks.Count == PhotonNetwork.PlayerList.Length)
    {
        var winnerID = playerHandRanks.OrderByDescending(entry => entry.Value).FirstOrDefault().Key;
        Debug.Log($"Winner ID: {winnerID}");
        photonView.RPC("AnnounceWinner", RpcTarget.All, winnerID);

        // Transfer pot to the winner
       photonView.RPC("TransferPotToWinner", RpcTarget.MasterClient, winnerID);

        // Reset the pot
        photonView.RPC("ResetPot", RpcTarget.All);
    }
    else
    {
        Debug.LogWarning("Not all players have reported their hand ranks yet.");
    }
      
    }
}
[PunRPC]
private void AnnounceWinner(int winnerID)
{
    Debug.Log($"The winner is player {winnerID}");
    // Implement your UI update logic to show the winner to all players
}
[PunRPC]
private void EvaluateWinner()
{
    
}
[PunRPC]
private void TransferPotToWinner(int winnerID)
{
    Debug.Log($"Transferring pot to player {winnerID}. Pot amount: {potAmount}");

    PhotonView winnerPhotonView = FindPhotonViewByActorNumber(winnerID);
    if (winnerPhotonView != null)
    {
        PlayerCardHandler playerCardHandler = winnerPhotonView.GetComponent<PlayerCardHandler>();
        if (playerCardHandler != null)
        {
            playerCardHandler.AddChips(potAmount);
            Debug.Log($"Added {potAmount} chips to player {winnerID}.");
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

}
