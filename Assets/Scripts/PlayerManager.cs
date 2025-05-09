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
using System.Globalization;
public class PlayerManager : MonoBehaviourPunCallbacks, IOnEventCallback


{public long currentRaiseAmount = 0;
private ClickSpawner clickSpawner;
	public List<PlayerPosition> playerPositions = new List<PlayerPosition>();
    private int dealerIndex = 0; // Assume the first player in the list is the dealer at the start
	private long newChipCount = 0;
	public int currentSeat;
	public HandRank bestHandRank;
	public TMP_Text smallBlindText;
    public TMP_Text bigBlindText;
	public TMP_Text nicknameText;
	 public string Name;
	 [SerializeField] private Slider betSlider;
public TMP_Text betAmountText;
public TextMeshProUGUI callAmountText;
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
public long callAmount=0;
public bool Raise = false;

    public List<Card> playerHand = new List<Card>();
	public long PlayersBet = 0;

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
 private ClickSpawner FindClickSpawner(int seat)
{
    ClickSpawner[] spawners = FindObjectsOfType<ClickSpawner>();
    Debug.Log("Found " + spawners.Length + " ClickSpawner(s) in the scene.");
    foreach (ClickSpawner spawner in spawners)
    {
        // Log if the ClickSpawner is active
        if (spawner.gameObject.activeSelf)
        {
            Debug.Log("Found active ClickSpawner with seat number: " + spawner.seatNumber);
            if (spawner.seatNumber == seat)
            {
                return spawner;
            }
        }
    }
    return null;
}

   public void StandUp()
    {
        if (clickSpawner != null)
        {
            clickSpawner.OnStandUpClick();
        }
    }
[PunRPC]
    public void SetCurrentSeat(int seat)
    {
        currentSeat = seat;
    }
public void ResetBettingRound()
{
    PlayersBet = 0;
}

[PunRPC]
void SetPlayerName(string name)
{
    nicknameText.text = name;
	photonView.RPC("UpdateBlindUI", RpcTarget.AllBuffered);
}
[PunRPC]
public void CallAmountReset()
{
callAmount=0;
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
		
		
    }    }
	

	void Start()
    { GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            clickSpawner = gameManager.GetClickSpawnerBySeat(currentSeat);
            if (clickSpawner != null)
            {
                Debug.Log("Found ClickSpawner for seat " + currentSeat);
            }
            else
            {
                Debug.LogWarning("ClickSpawner not found for seat " + currentSeat);
            }
        }
      betSlider.onValueChanged.AddListener(OnSliderValueChange);
	  
	  betSlider.wholeNumbers = true;

// Example: Set minimum raise to big blind and max to available chip count
long minRaise = GameManager.BIG_BLIND_AMOUNT;
long maxRaise = chipCount;

betSlider.minValue = minRaise;
betSlider.maxValue = maxRaise;
betSlider.value = minRaise;  // Default to minimum

OnSliderValueChange(betSlider.value); 

    }
	public void CheckAndStandUpIfBroke()
{Debug.Log($"[{PhotonNetwork.NickName}] Called check if broke.");
    if (chipCount <= 0 && photonView.IsMine )
    {
        Debug.Log($"[{PhotonNetwork.NickName}] has 0 chips. Auto-standing up.");


        StandUp();

    
}}
private void OnSliderValueChange(float value)
{
    PlayersBet = (long)value;
    photonView.RPC("UpdateRaiseAmountText", RpcTarget.All);
}
[PunRPC]
private void RaiseChipCount(long raiseAmount)
{
    if (PhotonNetwork.IsMasterClient)
    {
        GameManager.Instance.pot += raiseAmount;

        // Update global raise amount
        GameManager.Instance.globalRaise = raiseAmount;

        // Sync the call amount for all players based on the new global raise
        photonView.RPC("SyncCallAmount", RpcTarget.AllBuffered, GameManager.Instance.globalRaise);

        // Broadcast updated pot
        GameManager.Instance.photonView.RPC("UpdatePotUI", RpcTarget.AllBuffered, GameManager.Instance.pot);
    }

    // These updates can still be done for all clients
    photonView.RPC("UpdateBet", RpcTarget.AllBuffered, raiseAmount);
    photonView.RPC("UpdateUI", RpcTarget.AllBuffered);
    photonView.RPC("UpdateChip", RpcTarget.AllBuffered, raiseAmount);
}

public void OnRaiseButtonClicked()
{
	  
     if (photonView.IsMine)
    {
        object[] content = new object[] { PlayersBet, PhotonNetwork.LocalPlayer.ActorNumber };
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(3, content, options, SendOptions.SendReliable);

        if (UI != null)
            UI.SetActive(false);
RaiseNextPlayerEvent();
		BroadcastCallAmount(PlayersBet);
        GameManager.Instance.photonView.RPC("PlayerFinishedTurn", RpcTarget.MasterClient);
		photonView.RPC("UpdateChip", RpcTarget.AllBuffered, PlayersBet);
		
		
    }
        
}
[PunRPC]
public void SyncCallAmount(long globalRaise)
{
    callAmount = Math.Max(0, globalRaise);

    if (callAmountText != null)
        callAmountText.text = $"{FormatChipsWithSuffix(callAmount)}";

    Debug.Log($"[SyncCallAmount] Set callAmount = {callAmount}");
}
public void BroadcastCallAmount(long amount)
{
    if (PhotonNetwork.IsMasterClient)
    {
        photonView.RPC("SyncCallAmount", RpcTarget.AllBuffered, amount);
    }
}
[PunRPC]
private void UpdateBet(long M)
{GameManager.Instance.CurrentBet += M;
}

[PunRPC]
private void UpdatePot(long M)
{GameManager.Instance.potText.text = $"Pot: {FormatChipsWithSuffix(GameManager.Instance.pot)}";
}

[PunRPC]
private void UpdateUI()
{
    if (GameManager.Instance.pot != null)
        GameManager.Instance.potText.text = $"Pot: {FormatChipsWithSuffix(GameManager.Instance.pot)}";
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
    {if (PhotonNetwork.IsMasterClient)
        {
        photonView.RPC("Roles", RpcTarget.AllBuffered);
    }}
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

     }
    }

    // Mark that the player is now in the game
    InGame = true;
}public static string FormatChipsWithSuffix(long amount)
{
    if (amount >= 1_000_000_000_000)
        return (amount / 1_000_000_000_000d).ToString("0.#", CultureInfo.InvariantCulture) + "T";
    if (amount >= 1_000_000_000)
        return (amount / 1_000_000_000d).ToString("0.#", CultureInfo.InvariantCulture) + "B";
    if (amount >= 1_000_000)
        return (amount / 1_000_000d).ToString("0.#", CultureInfo.InvariantCulture) + "M";
    if (amount >= 1_000)
        return (amount / 1_000d).ToString("0.#", CultureInfo.InvariantCulture) + "K";

    return amount.ToString("N0", CultureInfo.InvariantCulture);
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
[PunRPC]
private void RaiseT()
{
Raise=true;
	
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
private void UpdateChip(long Raise)
{
    chipCount -= Raise; // Deduct the chips from the actual value
    chipCountText.text = $"${chipCount.ToString("N0", CultureInfo.InvariantCulture)}";
	Start();
		
}
[PunRPC]
public void PostBlinds()
{
if (GameManager.Instance.FirstTurn && playerPosition == PlayerPosition.DEALER && !Blind)
    {PlayersBet = GameManager.BIG_BLIND_AMOUNT;
        PostBlind(GameManager.BIG_BLIND_AMOUNT);
		 smallBlindText.gameObject.SetActive(false);
        bigBlindText.gameObject.SetActive(true);
 Blind = true;
        photonView.RPC("UpdateChipCountUI", RpcTarget.AllBuffered, chipCount);
			photonView.RPC("RaiseAmount", RpcTarget.All, (long)0);
 GameManager.Instance.photonView.RPC("first", RpcTarget.AllBuffered);
	}
    else if (GameManager.Instance.FirstTurn  && playerPosition == PlayerPosition.UTG && !Blind){
        Debug.Log("Posting SMALL blind for UTG");
PlayersBet = GameManager.BIG_BLIND_AMOUNT;
        PostBlind(GameManager.BIG_BLIND_AMOUNT);
        smallBlindText.gameObject.SetActive(false);
        bigBlindText.gameObject.SetActive(true);
 Blind = true;
        photonView.RPC("UpdateChipCountUI", RpcTarget.AllBuffered, chipCount);
        	photonView.RPC("RaiseAmount", RpcTarget.All, (long)0);

   }
    else if (playerPosition == PlayerPosition.DEALER && !Blind)
{GameManager.Instance.currentRaiseAmount = GameManager.SMALL_BLIND_AMOUNT;   
    Debug.Log("Posting BIG blind for UTG");
 Blind = true;
    // Set CurrentBet to the big blind amount before posting
    GameManager.Instance.CurrentBet = GameManager.SMALL_BLIND_AMOUNT;

    // Sync the current bet across all clients
GameManager.Instance.photonView.RPC("CurrentBetSync", RpcTarget.All, GameManager.Instance.CurrentBet);

    // Set the current player's bet this round
    PlayersBet = GameManager.SMALL_BLIND_AMOUNT;

    // Deduct chips and post blind
    PostBlind(GameManager.SMALL_BLIND_AMOUNT);

    smallBlindText.gameObject.SetActive(true);
    bigBlindText.gameObject.SetActive(false);
    photonView.RPC("UpdateChipCountUI", RpcTarget.AllBuffered, chipCount);

		GameManager.Instance.currentRaiseAmount = GameManager.BIG_BLIND_AMOUNT;
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
	 GameManager.Instance.CurrentBet = amount;
        Debug.Log($"{Name} posts blind of {amount}");
    photonView.RPC("UpdateChipCountUI", RpcTarget.AllBuffered, chipCount);
	if (PhotonNetwork.IsMasterClient)
{
       GameManager.Instance.photonView.RPC("AddToPotRPC", RpcTarget.MasterClient, (long)amount, PhotonNetwork.NickName);// You must define this method in GameManager
}}



[PunRPC]
public void UpdateChipCountUI(long newChipCount)
{
    this.chipCount = newChipCount;
  chipCountText.text = $"${chipCount.ToString("N0", CultureInfo.InvariantCulture)}";
  long maxRaise = newChipCount;
betSlider.maxValue = maxRaise;
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
	if (PhotonNetwork.IsMasterClient)
    {
photonView.RPC("PostBlinds", RpcTarget.All);



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
public void CallButton()
{
    // Log the current callAmount
    Debug.Log($"[CallButton] Player {photonView.Owner.NickName} is calling {callAmount}");

    // Deduct chips and apply the callAmount
    photonView.RPC("RaiseChipCount", RpcTarget.MasterClient, callAmount);

    // Turn off UI and notify GameManager if it's the local player
    if (photonView.IsMine && UI.activeSelf)
    {
        UI.SetActive(false);
        GameManager.Instance.photonView.RPC("PlayerFinishedTurn", RpcTarget.MasterClient);
        RaiseNextPlayerEvent();

        Debug.Log("[CallButton] Player finished turn after calling.");
    }

    // Reset callAmount for the next player
    photonView.RPC("CallAmountReset", RpcTarget.AllBuffered);

    // Optional: sync again to make sure all players see 0 call amount (for UI)
    photonView.RPC("SyncCallAmount", RpcTarget.AllBuffered, 0L);
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
{
}
public void CheckButtonClicked()
{
        
		if(callAmount==GameManager.BIG_BLIND_AMOUNT)
		{
			if (photonView.IsMine && UI.activeSelf)
    {
        if (UI != null)
            UI.SetActive(false);
        GameManager.Instance.photonView.RPC("PlayerFinishedTurn", RpcTarget.MasterClient);
        RaiseNextPlayerEvent();
		Debug.Log("Player checked.");
    }
		}
		else {photonView.RPC("RaiseChipCount", RpcTarget.AllBuffered, callAmount);
		if (photonView.IsMine && UI.activeSelf)
    {
        if (UI != null)
            UI.SetActive(false);
        GameManager.Instance.photonView.RPC("PlayerFinishedTurn", RpcTarget.MasterClient);
        RaiseNextPlayerEvent();
    }}
		
		
        
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
private void UpdateRaiseAmountText()
{
if (betAmountText != null)
betAmountText.text = $"{FormatChipsWithSuffix(PlayersBet)}";
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
else if (photonEvent.Code == 3) // Raise action
{
    object[] data = (object[])photonEvent.CustomData;
    long raiseAmount = (long)data[0];
    int actorNumber = (int)data[1];

    GameManager.Instance.pot += raiseAmount;
    GameManager.Instance.globalRaise = raiseAmount;

    // Update UI for all clients
    GameManager.Instance.photonView.RPC("UpdatePotUI", RpcTarget.AllBuffered, GameManager.Instance.pot);
    photonView.RPC("SyncCallAmount", RpcTarget.AllBuffered, raiseAmount);

    Debug.Log($"[RaiseEvent] Player {actorNumber} raised {raiseAmount}. Pot is now {GameManager.Instance.pot}");
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