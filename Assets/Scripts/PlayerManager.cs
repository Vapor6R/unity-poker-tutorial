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
public class PlayerManager : MonoBehaviourPunCallbacks, IOnEventCallback


{
	public List<PlayerPosition> playerPositions = new List<PlayerPosition>();
    private int dealerIndex = 0; // Assume the first player in the list is the dealer at the start
	
	public int currentSeat;
	public HandRank bestHandRank;
	public TMP_Text smallBlindText;
    public TMP_Text bigBlindText;
	public TMP_Text nicknameText;
	 public string Name;
	 [SerializeField] private Slider betSlider;
public TMP_Text betAmountText;
public Button confirmBetButton;
	public GameObject smallBlindLogo;
public GameObject bigBlindLogo;
public GameObject dealerLogo;
    public GameObject UI; 
    public Transform cardHandPosition;
 public string playerName;
 public PlayerPosition playerPosition;
[SerializeField] private TMP_Text chipCountText;
public long chipCount;
public bool InGame = false;
public bool Blind = false;
public bool FirstTurn = true;

    public List<Card> playerHand = new List<Card>();
	public long currentBetThisRound = 0;
    public enum PlayerPosition
    {
        None = -1,
        DEALER = 0,    // Dealer position
        UTG = 1,       // Under the Gun (first player to act after the blinds)
        UTG_PLUS_1 = 2, // UTG+1 (second player to act)
        POSITION_3 = 3, // 3rd player to act
        POSITION_4 = 4, // 4th player to act
        POSITION_5 = 5, // 5th player to act
        POSITION_6 = 6, // 6th player to act
        POSITION_7 = 7, // 7th player to act
        POSITION_8 = 8  // 8th player to act
    }
public enum Statue
    {
        None = -1,
        Waiting = 0,    // Dealer position
        Playing = 1,       // Under the Gun (first player to act after the blinds)
        Checked = 2, // UTG+1 (second player to act)
        Raise = 3, // 3rd player to act
        Folded = 4, // 4th player to act
        AllIn = 5, // 5th player to act
    }
	
	 public Statue statue;

[PunRPC]
    public void SetCurrentSeat(int seat)
    {
        currentSeat = seat;
    }
public void ResetBettingRound()
{
    currentBetThisRound = 0;
}

[PunRPC]
void SetPlayerName(string name)
{
    nicknameText.text = name;
	photonView.RPC("UpdateBlindUI", RpcTarget.AllBuffered);
}
public void EnableBettingUI()
{

}
public void OnSliderValueChanged(long value)
{
    photonView.RPC("UpdateBetTextRPC", RpcTarget.All, (long)value);
}
[PunRPC]
private void UpdateBetTextRPC(float value)
{
    betAmountText.text = $"Bet: {value:N0}"; // adds commas
}
public void OnConfirmBetClicked()
{
    
    
}
void Awake()
    {
		chipCount = 10000000000000;
		Debug.Log("ChipCount at start: " + chipCount);

        photonView.RPC("UpdateChipCount", RpcTarget.AllBuffered, chipCount);
		if (photonView.IsMine)
    {
		photonView.RPC("SetPlayerName", RpcTarget.AllBuffered, PhotonNetwork.NickName);
		
		
    }}
	void Start()
    {
      
    }
[PunRPC]
public void UpdateBlindUI()
{
    smallBlindText.text = $"SB: {FormatChipsWithSuffix(GameManager.SMALL_BLIND_AMOUNT)}";
    bigBlindText.text = $"BB: {FormatChipsWithSuffix(GameManager.BIG_BLIND_AMOUNT)}";
}

[PunRPC]
public void EvaluateHand()
{
    if (playerHand.Count != 7)
    {
        Debug.LogWarning($"[{PhotonNetwork.NickName}] Cannot evaluate hand — needs 7 cards.");
        return;
    }

    HandValue handValue = HandEvaluator.EvaluateHand(playerHand);
bestHandRank = handValue.Rank;
    Debug.Log($"[{PhotonNetwork.NickName}] Hand Rank: {bestHandRank}");
}

[PunRPC]
private void ResetHand()
{
    bestHandRank = HandRank.HighCard; // Reset to the lowest/default hand rank
}
public void DetermineWinner()
{
    PlayerManager[] allPlayers = FindObjectsOfType<PlayerManager>();

    PlayerManager bestPlayer = null;
    HandRank bestRank = HandRank.HighCard;

    foreach (PlayerManager pm in allPlayers)
    {
        Debug.Log($"Player {pm.photonView.Owner.NickName} has {pm.bestHandRank}");

        if (bestPlayer == null || pm.bestHandRank > bestRank)
        {
            bestPlayer = pm;
            bestRank = pm.bestHandRank;
        }
    }

    if (bestPlayer != null)
    {
        Debug.Log($"🏆 Winner is: {bestPlayer.photonView.Owner.NickName} with {bestRank}");
    }
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
    private void OnEnable()
    {
		ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
        customProperties.Add("joinTime", PhotonNetwork.Time);  // Store the join time when the player joins
        PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);

        // You can also log it to verify if it's set correctly
        Debug.Log($"Player {PhotonNetwork.LocalPlayer.NickName} joined at {PhotonNetwork.Time}");
    
		
        if (photonView.IsMine)
        {
          GameManager.Instance.photonView.RPC("AddPlayerToList", RpcTarget.AllBuffered,PhotonNetwork.LocalPlayer.ActorNumber);
            AssignPlayerPosition();
            photonView.Owner.TagObject = this.gameObject;
            Debug.Log($"{photonView.Owner.NickName} TagObject assigned: {this.gameObject}");
			PhotonNetwork.AddCallbackTarget(this); 
			photonView.RPC("IsPlaying", RpcTarget.AllBuffered, (int)Statue.Waiting);
			 
			
			GameManager.Instance.photonView.RPC("StartSit", RpcTarget.MasterClient);

        }  
		 
    }
[PunRPC]
public void IsPlaying(int statueValue)
{
   statue = (Statue)statueValue;
if (statue == Statue.Playing)
    {
        photonView.RPC("Roles", RpcTarget.AllBuffered);
    }
}

[PunRPC]
public void AddCardToPlayerHandRPC(int cardViewID, PhotonMessageInfo info)
{
    // Get the player who sent the RPC call
   
    // Find the PhotonView for the card and set it in the player's hand
    PhotonView cardView = PhotonView.Find(cardViewID);
    if (cardView != null)
    {
        Card card = cardView.GetComponent<Card>();
        if (card != null)
        {
            playerHand.Add(card);
            card.transform.SetParent(cardHandPosition);

            // Set card active only for the owner of the photonView
            card.gameObject.SetActive(photonView.IsMine);

            // Ensure the cards are correctly positioned
            TransformCardPositions();
			StartCoroutine(delayedRole());
				if(PhotonNetwork.IsMasterClient)
				{
photonView.RPC("PostBlinds", RpcTarget.All);
} 


        }
    }

    // Mark that the player is now in the game
    InGame = true;
}
public static string FormatChipsWithSuffix(long amount)
{
    if (amount >= 1_000_000_000_000)
        return (amount / 1_000_000_000_000D).ToString("0.#") + "T";
    if (amount >= 1_000_000_000)
        return (amount / 1_000_000_000D).ToString("0.#") + "B";
    if (amount >= 1_000_000)
        return (amount / 1_000_000D).ToString("0.#") + "M";
    if (amount >= 1_000)
        return (amount / 1_000D).ToString("0.#") + "K";

    return amount.ToString("N0");
}

[PunRPC]
private void BlindTrue()
{
Blind=true;
	
}

[PunRPC]
private void BlindF()
{
Blind=false;
	
}
 private void TransformCardPositions()
    {
        for (int i = 0; i < playerHand.Count; i++)
        {
            Card card = playerHand[i];
            if (card != null)
            {
                Vector3 desiredPosition = new Vector3(i * 100f, 0f, 0f);

                // Ensure the card is positioned relative to the parent
                card.transform.localPosition = desiredPosition;
                card.transform.localRotation = Quaternion.identity; // Reset rotation

                // Optionally, adjust the scale if needed
                card.transform.localScale = Vector3.one;

                Debug.Log($"Card {card.rank} of {card.suit} moved to position {desiredPosition}.");
            }    
            else
            {
                //Debug.LogError("Card is null in TransformCardPositions().");
            }
        }
    }
[PunRPC]
private void BlindFalse()
{
    Blind = false;
    Debug.Log($"BlindFalse RPC called for player: {photonView.Owner.NickName}, Blind set to: {Blind}");
}
[PunRPC]
public void PostBlinds()
{
if (FirstTurn && playerPosition == PlayerPosition.DEALER && !Blind)
    {currentBetThisRound = GameManager.BIG_BLIND_AMOUNT;
        PostBlind(GameManager.BIG_BLIND_AMOUNT);
		 smallBlindText.gameObject.SetActive(true);
        bigBlindText.gameObject.SetActive(false);
		photonView.RPC("BlindTrue", RpcTarget.All);  
        photonView.RPC("UpdateChipCountUI", RpcTarget.AllBuffered, chipCount);
			 photonView.RPC("RaiseAmount", RpcTarget.All, 0);
    }
    else if (playerPosition == PlayerPosition.DEALER && !Blind){
        Debug.Log("Posting SMALL blind for DEALER");

        PostBlind(GameManager.SMALL_BLIND_AMOUNT);
        smallBlindText.gameObject.SetActive(true);
        bigBlindText.gameObject.SetActive(false);
photonView.RPC("BlindTrue", RpcTarget.All);  
        photonView.RPC("UpdateChipCountUI", RpcTarget.AllBuffered, chipCount);
        
    }
    else if (playerPosition == PlayerPosition.UTG && !Blind)
{
    Debug.Log("Posting BIG blind for UTG");

    // Set CurrentBet to the big blind amount before posting
    GameManager.CurrentBet = GameManager.BIG_BLIND_AMOUNT;

    // Sync the current bet across all clients
    photonView.RPC("CurrentBetSync", RpcTarget.All, GameManager.CurrentBet);

    // Set the current player's bet this round
    currentBetThisRound = GameManager.BIG_BLIND_AMOUNT;

    // Deduct chips and post blind
    PostBlind(GameManager.BIG_BLIND_AMOUNT);

    smallBlindText.gameObject.SetActive(false);
    bigBlindText.gameObject.SetActive(true);

    photonView.RPC("BlindTrue", RpcTarget.All);
    photonView.RPC("UpdateChipCountUI", RpcTarget.AllBuffered, chipCount);
}

    else
    {
        Debug.Log("This player does not post a blind.");
    }
	
}
	public void PostBlind(long amount)
{
    if (chipCount < amount)
    {
        Debug.LogWarning($"{PhotonNetwork.NickName} doesn't have enough chips to post blind.");
        return;
    }

    chipCount -= amount;
	 GameManager.CurrentBet = amount;
        Debug.Log($"{Name} posts blind of {amount}");
    photonView.RPC("UpdateChipCountUI", RpcTarget.AllBuffered, chipCount);

    // Only MasterClient updates the pot to avoid duplication
    if (PhotonNetwork.IsMasterClient)
    {
        GameManager gm = FindObjectOfType<GameManager>();
       gm.photonView.RPC("AddToPotRPC", RpcTarget.MasterClient, (long)amount);// You must define this method in GameManager
    }

    Debug.Log($"{PhotonNetwork.NickName} posted blind of {amount}");
}
[PunRPC]
public void CurrentBetSync(long syncedBet)
{
    GameManager.CurrentBet = syncedBet;
    Debug.Log($"CurrentBet synced to: {syncedBet}");
}
public void Bet(long amount)
{
    if (chipCount < amount) return;

    chipCount -= amount;
  photonView.RPC("UpdateChipCountUI", RpcTarget.AllBuffered, chipCount);

    if (PhotonNetwork.IsMasterClient)
    {
        GameManager gm = FindObjectOfType<GameManager>();
gm.photonView.RPC("AddToPotRPC", RpcTarget.MasterClient, (long)amount);    }
}

[PunRPC]
public void UpdateChipCountUI(long newChipCount)
{
    this.chipCount = newChipCount;
    chipCountText.text = chipCount.ToString("N0"); // or FormatChipsWithSuffix(chipCount)
}
[PunRPC]
private void Roles()
{
    if (PhotonNetwork.PlayerList.Length == 2)
    {
        if (playerPosition == PlayerPosition.DEALER && statue == Statue.Playing)
        {
            dealerLogo.SetActive(true);
            smallBlindLogo.SetActive(true);
			bigBlindLogo.SetActive(false);
        }
        else if (playerPosition == PlayerPosition.UTG && statue == Statue.Playing)
        {
            bigBlindLogo.SetActive(true);
dealerLogo.SetActive(false);
            smallBlindLogo.SetActive(false);
        }
    }

    if (PhotonNetwork.PlayerList.Length >= 3)
    {
        if (playerPosition == PlayerPosition.DEALER && statue == Statue.Playing)
        {
            dealerLogo.SetActive(true);
			 smallBlindLogo.SetActive(false);
			bigBlindLogo.SetActive(false);
        }
        else if (playerPosition == PlayerPosition.UTG && statue == Statue.Playing)
        {
            smallBlindLogo.SetActive(true);
			bigBlindLogo.SetActive(false);
			 dealerLogo.SetActive(false);
        }
        else if (playerPosition == PlayerPosition.UTG_PLUS_1 && statue == Statue.Playing)
        {
            bigBlindLogo.SetActive(true);
			smallBlindLogo.SetActive(true);
			dealerLogo.SetActive(false);
        }
    }
}



    private void AssignPlayerPosition()
    {
        // Get the list of players in the room
        Player[] players = PhotonNetwork.PlayerList;

        // Assign a position based on the player's index in the list
        int playerIndex = System.Array.IndexOf(players, photonView.Owner);
        if (playerIndex >= 0 && playerIndex < System.Enum.GetValues(typeof(PlayerPosition)).Length)
        {
            playerPosition = (PlayerPosition)playerIndex;  // Assign the position based on the player's order
            Debug.Log($"Player {photonView.Owner.NickName} has been assigned position: {playerPosition}");

            // Set the player's TagObject to the PlayerManager GameObject
            photonView.Owner.TagObject = this.gameObject;
            Debug.Log($"{photonView.Owner.NickName} TagObject assigned: {this.gameObject}");

            // Call the RPC to set the position for this player on all clients
            photonView.RPC("SetPlayerPosition", RpcTarget.All, playerPosition);
        }
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
	photonView.RPC("BlindFalse", RpcTarget.All); 

    photonView.RPC("Logos", RpcTarget.All);
}
	private IEnumerator delayedRole()
{yield return new WaitForSeconds(1f); // Adjust the delay time as needed
	{
	photonView.RPC("Roles", RpcTarget.AllBuffered);
}}
[PunRPC]
public void SetPlayerPosition(PlayerPosition assignedPosition)
{
    playerPosition = assignedPosition;
    Debug.Log($"[SetPlayerPosition] {photonView.Owner.NickName} => {playerPosition}");

    photonView.Owner.TagObject = this.gameObject; // ✅ ADD THIS AGAIN
    Debug.Log($"[SetPlayerPosition] TagObject reassigned for {photonView.Owner.NickName}");

    HandleUIForPlayerUTG();

}
[PunRPC]
public void ReceiveWinnings(long amount)
{
    chipCount += amount;
    Debug.Log($"{PhotonNetwork.NickName} received {amount} chips! New balance: {chipCount}");
    
	photonView.RPC("UpdateChipCountUI", RpcTarget.AllBuffered, chipCount);
}


public void FoldButtonClicked()
{
    Debug.Log("FoldButtonClicked: Swapping roles triggered via RaiseEvent(2)");

    if (photonView.IsMine && UI.activeSelf)
    {
        UI.SetActive(false);

        if (GameManager.Instance.PlayersInGame.Count == 2)
        {
            // Raise event to tell MasterClient to swap

            object[] eventData = new object[] { 0 }; // No extra data needed
            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            PhotonNetwork.RaiseEvent(2, eventData, options, SendOptions.SendReliable);
photonView.RPC("InGameF", RpcTarget.AllBuffered);
statue = Statue.Folded;
photonView.RPC("BlindF", RpcTarget.All);
GameManager.Instance.photonView.RPC("Reset", RpcTarget.All);
GameManager.Instance.photonView.RPC("ResetTurnStatesForOthers", RpcTarget.All);
					GameManager.Instance.photonView.RPC("New", RpcTarget.MasterClient);
					
return;
        }
       if (GameManager.Instance.PlayersInGame.Count == 3)
        {
            RaiseNextPlayerEvent();
        }
    }
}
[PunRPC]
private void InGameF()
{InGame = false; 
}


[PunRPC]
private void RaiseAmount(long Amount)
{GameManager.RaiseAmount = Amount;
}
    public void CheckButtonClicked()
{
	
	if(GameManager.RaiseAmount> 0)
	{
		long betAmount = (long)betSlider.value;

    if (betAmount > chipCount)
    {
        Debug.LogWarning("Cannot bet more than you have!");
        return;
    }

    chipCount -= betAmount;
    currentBetThisRound += betAmount;
GameManager.CurrentBet = (long)Mathf.Max((float)GameManager.CurrentBet, betAmount);
GameManager.CurrentBet = (long)GameManager.CurrentBet;
    photonView.RPC("UpdateChipCountUI", RpcTarget.AllBuffered, chipCount);
    photonView.RPC("CurrentBetSync", RpcTarget.All, GameManager.CurrentBet);


    betSlider.gameObject.SetActive(false);
    betAmountText.gameObject.SetActive(false);
    confirmBetButton.gameObject.SetActive(false);

    GameManager.Instance.photonView.RPC("PlayerFinishedTurn", RpcTarget.MasterClient);
    RaiseNextPlayerEvent();
	}
	else{
	    betSlider.minValue = GameManager.CurrentBet; // can't bet less than current bet
    betSlider.maxValue = chipCount;
    betSlider.value = GameManager.CurrentBet;

    UpdateBetText(betSlider.value);
photonView.RPC("UpdateBetTextRPC", RpcTarget.AllBuffered, betSlider.value);
    betSlider.gameObject.SetActive(true);
    betAmountText.gameObject.SetActive(true);
    confirmBetButton.gameObject.SetActive(true);
	
    long amountToCall = GameManager.CurrentBet - currentBetThisRound;
	
    if (amountToCall > 0 && chipCount >= amountToCall)
    {
        chipCount -= amountToCall;
        currentBetThisRound += amountToCall;

        photonView.RPC("UpdateChipCountUI", RpcTarget.AllBuffered, chipCount);
        Debug.Log($"{PhotonNetwork.NickName} called {amountToCall}");
    }
    else
    {
        Debug.Log("Nothing to call or not enough chips");
    }

    if (photonView.IsMine && UI.activeSelf)
    {
        if (UI != null)
            UI.SetActive(false);
        GameManager.Instance.photonView.RPC("PlayerFinishedTurn", RpcTarget.MasterClient);
        RaiseNextPlayerEvent();
    }
}}
private void UpdateBetText(float value)
{
    betAmountText.text = $"Bet: {value:N0}";
}
   private void RaiseNextPlayerEvent()
{
    // Get the list of players in the room
    Player[] players = PhotonNetwork.PlayerList;

    // Ensure there's at least 1 player (room must not be empty)
    if (players.Length == 0)
    {
        Debug.LogError("No players in the room, cannot raise event.");
        return;
    }

    // Calculate the next player's index
    int nextPlayerIndex = (int)playerPosition + 1;

    // If there are only two players, we need to wrap around to the dealer after UTG
    if (players.Length == 2 && nextPlayerIndex > 1)
    {
        nextPlayerIndex = 0; // Go back to the dealer (index 0)
    }

    // If there are more than 2 players, just increment the player position,
    // and if we exceed the total number of players, wrap around to the dealer
    if (nextPlayerIndex >= players.Length)
    {
        nextPlayerIndex = 0; // Go back to the dealer after the last player
    }

    // Raise the event to notify all players about the next player
    byte eventCode = 1;  // Use a byte event code (1 in this case)
    object[] eventContent = new object[] { nextPlayerIndex };  // Send the next player index
    PhotonNetwork.RaiseEvent(eventCode, eventContent, RaiseEventOptions.Default, SendOptions.SendReliable);
    Debug.Log($"Raised event for next player: {nextPlayerIndex}");
}

public void OnEvent(EventData photonEvent)
{
    // Ensure that the event code is 1 (the event we're raising)
    if (photonEvent.Code == 1)
    {
        // Extract the next player index from the event data
        object[] data = (object[])photonEvent.CustomData;
        int nextPlayerIndex = (int)data[0];

        // Log the received event data and current player position
        Debug.Log($"Received event for next player. Next Player Index: {nextPlayerIndex}, Current Player Position: {playerPosition}");

        if ((int)playerPosition == nextPlayerIndex)
        {
            Debug.Log("Activating UI for the next player.");
            HandleUIForPlayer();  // Activate UI for the next player
        }
        else
        {
            Debug.Log("Deactivating UI for the current player.");
            // Deactivate the UI for other players who are not the next player
            if (UI != null)
                UI.SetActive(false);
        }
		 
    }
	else if (photonEvent.Code == 2)
{
    // Extract the next player index from the event data
    object[] data = (object[])photonEvent.CustomData;
    int nextPlayerIndex = (int)data[0];

    foreach (Player player in PhotonNetwork.PlayerList)
    {
        if (player.TagObject is GameObject obj && obj.TryGetComponent(out PlayerManager pm))
        {
            // Swap roles only if necessary
            if (pm.playerPosition == PlayerPosition.DEALER)
            {
                // Swap DEALER to UTG
                pm.photonView.RPC("SetPlayerPosition", RpcTarget.All, PlayerPosition.UTG);
            }
            else if (pm.playerPosition == PlayerPosition.UTG)
            {
                // Swap UTG to DEALER
                pm.photonView.RPC("SetPlayerPosition", RpcTarget.All, PlayerPosition.DEALER);
            }
			
        }
    }
}

}

private void HandleUIForPlayer()
{
    // Activate UI only for the next player
    if (photonView.IsMine)  // Ensure only the local player activates their own UI
    {
        if (UI != null)
            UI.SetActive(true);  // Activate UI for the next player
    }
	
}
[PunRPC]
private void HandleUIForPlayerUTG()
{
    // Activate UI only for UTG or Dealer
    if (playerPosition == PlayerPosition.UTG)
    {
        if (photonView.IsMine)  // Ensure only the local player activates their own UI
        {
            if (UI != null)
                UI.SetActive(true);  // Activate UI for UTG or Dealer position
        }
    }
    else
    {
        if (UI != null)
            UI.SetActive(false);  // Deactivate UI for non-UTG and non-Dealer positions
    }
}


private void OnDisable()
{
    PhotonNetwork.RemoveCallbackTarget(this);
 GameManager.Instance.photonView.RPC("Check", RpcTarget.MasterClient);
}

public long GetChipCount()
    {
        // Return the player's total chip count (assume this is already implemented)
        return chipCount;
    }
[PunRPC]
	public void ClearHand()
{
    // Iterate over the list to destroy each card's GameObject
    foreach (Card card in playerHand)
    {
        PhotonNetwork.Destroy(card.gameObject);  // Destroy the card's GameObject
    }
 playerHand.Clear();
   
}

 
}