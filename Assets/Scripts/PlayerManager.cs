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
using System.Collections.Concurrent;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
public enum Statue
    {
        None = -1,
        Waiting = 0,  
        Playing = 1,    
        Checked = 2,
        Raise = 3,
        Folded = 4,
        AllIn = 5,
    }
public class PlayerManager : MonoBehaviourPunCallbacks
{  
public int buttonIndex = -1; // Which button the player is sitting at
public bool IsPlayingIN = false;
public SpawnButtonManager currentSeat;
 public List<Card> currentBestCards = new List<Card>();
[SerializeField] private Slider betSlider;
public TMP_Text callAmountText;
public List<Card> highCards = new List<Card>();
public HandRank currentHandRank = HandRank.HighCard;
public bool IsPlaying = false;
public Statue statue;
public long PlayersBet;
    public bool isDealer = false;
    public bool isUTG = false;
    public bool isUTGPlus1 = false;
public bool isFolded = false;
public bool isSMALL= false;
public long chipCount;
    public GameObject UI;
 public int seatIndex = -1;

public bool InGame = false;
private long currentBet;
public TMP_Text chipCountText;
  public TMP_Text playerNameText;
  public string PlayerName { get; private set; }
   public List<Card> playerHand = new List<Card>();
   public Transform cardHandPosition;
   public bool Acted = false;
   public long callAmount;
   private bool FirstSit = true;
   	public TMP_Text betValueText;
	private long sliderUnit = 1000L;   
	
	
	private long ClampLong(long value, long min, long max)
{
    if (value < min) return min;
    if (value > max) return max;
    return value;
}

[PunRPC]
public void RPC_SetButtonIndex(int btnIdx)
{
    buttonIndex = btnIdx;
    Debug.Log($"🔘 {PlayerName} buttonIndex set to {buttonIndex}");
}

[PunRPC]
public void RPC_SetFolded(bool folded)
{
    isFolded = folded;
    Debug.Log($"[RPC_SetFolded] {PlayerName} isFolded = {isFolded}");
}

[PunRPC]
public void RPC_Setplaying(bool playing)
{
    IsPlaying = playing;
    Debug.Log($"[RPC_SetFolded] {PlayerName} isFolded = {isFolded}");
}

[PunRPC]
public void RPC_SetActed(bool acted)
{
    Acted = acted;
    Debug.Log($"[RPC_SetFolded] {PlayerName} isFolded = {isFolded}");
}

[PunRPC]
public void RPC_SetActedF()
{
    Acted = false;
    Debug.Log($"[RPC_SetFolded] {PlayerName} isFolded = {isFolded}");
}
private void TransferMasterClientToPlayerWithChips()
{
    Debug.Log("🔄 Current MasterClient is leaving - searching for new MasterClient...");
    
    // Get all active players with chips, excluding the current player
    var eligiblePlayers = FindObjectsOfType<PlayerManager>()
        .Where(p => p != null 
            && p != this 
            && p.chipCount > 0 
            && p.photonView != null 
            && p.photonView.Owner != null)
        .OrderByDescending(p => p.chipCount) // Prioritize player with most chips
        .ToList();
    
    if (eligiblePlayers.Count == 0)
    {
        Debug.LogWarning("⚠️ No eligible players found to transfer MasterClient.");
        return;
    }
    
    // Transfer to the player with the most chips
    PlayerManager newMaster = eligiblePlayers[0];
    Player newMasterPhotonPlayer = newMaster.photonView.Owner;
    
    Debug.Log($"✅ Transferring MasterClient to: {newMasterPhotonPlayer.NickName} (Chips: {newMaster.chipCount})");
    
    PhotonNetwork.SetMasterClient(newMasterPhotonPlayer);
}
private void OnDestroy()
{
if (PhotonNetwork.IsMasterClient && photonView.IsMine)
    {
        TransferMasterClientToPlayerWithChips();
    }
	isSMALL= false;
if (GameManager.activePlayers.Contains(this))
    {
        GameManager.activePlayers.Remove(this);
        Debug.Log($"❌ Removed {PlayerName} from activePlayers");
    }
 GameManager.Instance.photonView.RPC("CheckAndResetIfSinglePlayer", RpcTarget.All);
}
public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
{
    if (stream.IsWriting)
    {
        // ✅ Send values in THIS ORDER
        stream.SendNext(IsPlaying);
        stream.SendNext(isFolded);
    }
    else
    {
        // ✅ Receive values in the SAME ORDER
        IsPlaying = (bool)stream.ReceiveNext();
        isFolded = (bool)stream.ReceiveNext();
    }
}

[PunRPC]
	private void IsPlayingT(){
	IsPlayingIN = true;}
	 void Start()
    {
        currentSeat = FindObjectOfType<SpawnButtonManager>();
    }
[PunRPC]
public void AddChipsRPC(long amount)
{


    chipCount += amount;
    photonView.RPC("UpdateChipCount", RpcTarget.AllBuffered, chipCount);
	
}
[PunRPC]
public void Check()
{
    Debug.Log($"[Check] Called on: {photonView.Owner.NickName}, chipCount = {chipCount}");
    Debug.Log($"[Check] photonView.IsMine = {photonView.IsMine}, IsLocalPlayer = {PhotonNetwork.LocalPlayer.NickName}, Owner = {photonView.Owner.NickName}");

    if (chipCount == 0)
    {
        Debug.Log($"[Check] {photonView.Owner.NickName} is standing up.");
        if (photonView.IsMine)
        {
            if (currentSeat != null)
            {
                currentSeat.OnStandUpClicked();
                // Don't set to null here, OnStandUp will destroy this object
            }
            else
            {
                Debug.LogWarning("⚠️ No currentSeat reference found!");
            }
        }
    }
    else
    {
        Debug.Log($"[Check] {photonView.Owner.NickName} has chips, not standing up.");
    }
}
	public static string FormatChipsWithSuffix(long amount)
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

public void EvaluateMyHand()
{
    if (playerHand == null || playerHand.Count != 7)
    {
        Debug.LogWarning($"⚠️ {PlayerName} cannot evaluate hand - has {(playerHand == null ? "null" : playerHand.Count.ToString())} cards instead of 7");
        return;
    }

    try
    {
        var result = HandEvaluator.Evaluate(playerHand);
        currentHandRank = result.rank;
        currentBestCards = result.bestCards;

        if (currentBestCards == null || currentBestCards.Count != 5)
        {
            Debug.LogError($"❌ {PlayerName} - HandEvaluator returned invalid best cards!");
            return;
        }

        Debug.Log($"✅ {photonView.Owner.NickName} → {currentHandRank} | Best: {string.Join(", ", currentBestCards.Select(c => $"{c.rank}{c.suit}"))}");
    }
    catch (Exception e)
    {
        Debug.LogError($"❌ Error evaluating hand for {PlayerName}: {e.Message}");
    }
}
private void Update()
{
    // Only evaluate if we have exactly 7 cards (2 hole cards + 5 community cards)
    if (playerHand != null && playerHand.Count == 7)
    {
        EvaluateMyHand();
    }
    
    if (GameManager.Instance != null && GameManager.Instance.currentState == GameState.Showdown)
    {
        if (UI != null)
            UI.SetActive(false);
    }
	if(chipCount <0)
		photonView.RPC("RPC_SetActed", RpcTarget.AllBuffered, true);
}
[PunRPC]
public void UpdateRaiseAmountText(long amount)
{
    if (betValueText != null)
betValueText.text = $"Bet: {FormatChipsWithSuffix(amount)}";
}

   private void OnSliderChipValueChange(float sliderStepValue)
{
    if (betSlider == null)
        return;

    // Calculate selected amount
    long chipsSelected = (long)(sliderStepValue * sliderUnit);
    chipsSelected = ClampLong(chipsSelected, 1, chipCount);

    PlayersBet = chipsSelected;
photonView.RPC("UpdateChipCount", RpcTarget.AllBuffered, chipCount);
    photonView.RPC("UpdateRaiseAmountText", RpcTarget.AllBuffered, PlayersBet);

    Debug.Log($"[Slider] {PlayerName} slider={sliderStepValue}, unit={sliderUnit}, bet={PlayersBet}");
}
   private void ConfigureSliderForChips(long chips)
{
    if (betSlider == null) return;

    // Remove old listeners (to avoid duplicates)
    betSlider.onValueChanged.RemoveAllListeners();

    // Add the listener dynamically
    betSlider.onValueChanged.AddListener(OnSliderChipValueChange);

    // Now configure the rest
    betSlider.wholeNumbers = true;
    betSlider.minValue = 1f;

    sliderUnit = 1L;
    while (chips / sliderUnit > 1000L)
        sliderUnit *= 10L;

    betSlider.maxValue = Mathf.Max(1f, chips / (float)sliderUnit);
    betSlider.value = betSlider.minValue;

    Debug.Log($"🎚️ Slider configured: min={betSlider.minValue}, max={betSlider.maxValue}, unit={sliderUnit}");
}

private void OnEnable()
{

	if (!GameManager.activePlayers.Contains(this))
    {
        GameManager.activePlayers.Add(this);
        Debug.Log($"✅ Added {PlayerName} to activePlayers");
    }
	if(PhotonNetwork.IsMasterClient)
		GameManager.Instance.photonView.RPC("StartSit", RpcTarget.MasterClient);
}
public void Activate()
{if (!photonView.IsMine)
	return;
		  UI.SetActive(true);

}

[PunRPC]
   public void CheckPlayerCountAndAssignRoles()
{
    if (GameManager.Instance == null)
    {
        Debug.LogError("[PlayerManager] ❌ GameManager.Instance is null — cannot assign roles.");
        return;
    }
	
	GameManager.Instance.photonView.RPC("UpdateActivePlayersList", RpcTarget.AllBuffered);
    photonView.RPC("SetGameState", RpcTarget.MasterClient, GameState.Preflop);
	
	
}
[PunRPC]
public void RPC_SetTurn(int seatIndex)
{
    bool isCurrentTurnSeat = this.seatIndex == seatIndex && InGame && !isFolded;
    bool isMyTurn = isCurrentTurnSeat && photonView.IsMine;

    Debug.Log($"🎯 [RPC_SetTurn] Seat {seatIndex} | This player ({PlayerName}) isMyTurn={isMyTurn}");
    
    // ✅ CRITICAL: Get the current callAmount from GameManager before showing UI
    long currentCallAmount = GameManager.Instance.callAmount;
    
    Debug.Log($"   Current callAmount from GameManager: {currentCallAmount}");
    Debug.Log($"   This player's chipCount: {chipCount}");

    if (UI != null)
        UI.SetActive(isMyTurn);
    
    photonView.RPC("IsPlaying", RpcTarget.AllBuffered, (int)Statue.Playing);
    photonView.RPC("WaitingF", RpcTarget.AllBuffered);
    
    if (isMyTurn)
    {
        // ✅ Update local callAmount from GameManager
        this.callAmount = currentCallAmount;
        UpdateCallAmountUI(currentCallAmount);
        
        Debug.Log($"✅ {PlayerName}'s UI activated - callAmount synced to {currentCallAmount}");
        ConfigureSliderForChips(chipCount);
    }
    else
    {
        Debug.Log($"⏳ {PlayerName} waiting for their turn");
    }
}
[PunRPC]
public void UpdateCallAmountUI(long amount)
{
    callAmount = amount;
    if (callAmountText != null)
        callAmountText.text = $"Call: {amount}";
}

[PunRPC]
private void HandleUIForPlayerUTG()
{
    // Find the player with the lowest seat index
    PlayerManager utgPlayer = FindObjectsOfType<PlayerManager>()
        .Where(p => p != null && p.InGame && !p.isFolded && p.chipCount > 0)
        .OrderBy(p => p.seatIndex)
        .FirstOrDefault();
    
    if (utgPlayer != null)
    {
        Debug.Log($"✅ UTG Player: {utgPlayer.PlayerName} (Seat {utgPlayer.seatIndex})");
        
        // Check if this is the local player
        PlayerManager localPlayer = FindObjectsOfType<PlayerManager>()
            .FirstOrDefault(p => p != null && p.photonView != null && p.photonView.IsMine);
        
        if (localPlayer != null && localPlayer.seatIndex == utgPlayer.seatIndex)
        {
            // This is the local player and they are UTG
            if (localPlayer.UI != null)
            {
                localPlayer.UI.SetActive(true);
                localPlayer.ConfigureSliderForChips(chipCount);
            }
        }
    }
}

[PunRPC]
public void RevealLocalHand()
{
foreach (var card in playerHand)
    {
        if (card?.cardObject != null)
            card.cardObject.SetActive(true);
    } }
[PunRPC]
    public void AddCardToPlayerHandRPC(int cardViewID, PhotonMessageInfo info)
    {
        // 🔁 Forcefully clear existing cards from playerHand BEFORE adding the new one

        // 🔄 Add new card
        PhotonView cardView = PhotonView.Find(cardViewID);
        if (cardView != null)
        {
            Card card = cardView.GetComponent<Card>();
            if (card != null)
            {
                playerHand.Add(card);
photonView.RPC("HandleUIForPlayerUTG", RpcTarget.AllBuffered); 
                card.transform.SetParent(cardHandPosition, false);
                card.gameObject.SetActive(photonView.IsMine);
 photonView.RPC("RPC_Setplaying", RpcTarget.AllBuffered, true); 
                // --- Reposition after adding ---
                TransformCardPositions();

                
				GameManager.Instance.photonView.RPC("SetGameState", RpcTarget.MasterClient, GameState.Newround);
            }
        }
    }
	public void OnFold()
	{
		if(!photonView.IsMine)
			return;
photonView.RPC("RPC_SetFolded", RpcTarget.AllBuffered, true);
		if (UI != null)
        UI.SetActive(false);
    photonView.RPC("ActedFalse", RpcTarget.Others);
		  GameManager.Instance.photonView.RPC("RPC_PassTurnToNextBySeat", RpcTarget.MasterClient, seatIndex);
		
	}
	private void TransformCardPositions()
    {
        for (int i = 0; i < playerHand.Count; i++)
        {
            Card card = playerHand[i];
            if (card != null)
            {
                Vector3 desiredPosition = new Vector3(i * 100f, 0f, 0f);

                card.transform.localPosition = desiredPosition;
                card.transform.localRotation = Quaternion.identity; // Reset rotation

                card.transform.localScale = Vector3.one;
            }
            else
            {
                Debug.LogError("Card is null in TransformCardPositions().");
            }
        }
    }
private void Awake()
{
	 if (photonView != null && photonView.Owner != null)
	{ PlayerName = photonView.Owner.NickName;
//photonView.RPC("UpdateChipCount", RpcTarget.AllBuffered, chipCount);
}
    else
        PlayerName = "Unknown_Player";

    if (playerNameText != null)
        playerNameText.text = PlayerName;
		  InGame = true;
}[PunRPC]
public void RPC_SetSeatIndex(int seat)
{
    seatIndex = seat;
    Debug.Log($"🪑 {PlayerName} seatIndex set to {seatIndex}");
    // ... rest of your code
}
[PunRPC]
public void PostBlind(long Amount)
{
    // ✅ Only execute on the owner's client
    if (!photonView.IsMine)
    {
        Debug.Log($"⏭️ Skipping PostBlind for {PlayerName} - not my player");
        return;
    }
    
    Debug.Log($"💰 {PlayerName} posting blind: {Amount}");
    
    if (chipCount < Amount)
    {
        // Player is all-in
        Debug.Log($"💰 {gameObject.name} posts Blind ALL-IN: {chipCount}");
        long allInAmount = chipCount;
        chipCount = 0;
        
        if (PotManager.Instance != null)
        {
            PotManager.Instance.photonView.RPC("AddToPot", RpcTarget.AllBuffered, PhotonNetwork.NickName, allInAmount);
        }
        
        currentBet = allInAmount;
    }
    else
    {
        // Normal blind post
        Debug.Log($"💰 {gameObject.name} posts Blind: {Amount}");
        chipCount -= Amount;
        currentBet = Amount;
        
        if (PotManager.Instance != null)
        {
            PotManager.Instance.photonView.RPC("AddToPot", RpcTarget.AllBuffered, PhotonNetwork.NickName, Amount);           
        }
    }
    
    // Update UI for all clients
    photonView.RPC("UpdateChipCount", RpcTarget.AllBuffered, chipCount);
	
}

[PunRPC]
public void UpdateChipCount(long newChipCount)
{
    chipCount = newChipCount;

    if (chipCountText != null)
        chipCountText.text = $"${chipCount}";
    else
        Debug.LogWarning($"⚠️ chipCountText is null on {gameObject.name}");

    // 🟢 Only update slider for local player

       
}

public void OnCall()
{
	   if (!photonView.IsMine)
        return;
    Acted = true;
    UI?.SetActive(false);
    Debug.Log($"\n===== [OnCall] {PlayerName} START =====");
    Debug.Log($"Local callAmount: {callAmount}");
    Debug.Log($"chipCount: {chipCount}");

    if (callAmount <= 0)
    {

        FinishTurn(Statue.Checked);
        return;
    }
   
    if (!GameManager.Instance.FirstTurn && FirstSit && isDealer)
    {
        callAmount += GameManager.Instance.Small;
    }
    if (callAmount > chipCount)
    {
        callAmount = chipCount;

    }
    if (FirstSit)
    {
        FirstSit = false;
    }
    photonView.RPC("RPC_SetActed", RpcTarget.AllBuffered, true);
    chipCount -= callAmount;
	photonView.RPC("UpdateChipCount", RpcTarget.AllBuffered, chipCount);
   if (PotManager.Instance != null)
        {
            PotManager.Instance.photonView.RPC("AddToPot", RpcTarget.AllBuffered, PhotonNetwork.NickName, callAmount);
        }
		 bool isAllIn = chipCount <= 0;

    if (isAllIn)
    {
	//photonView.RPC("RPC_Setplaying", RpcTarget.AllBuffered, false);
	}
    Statue newStatue = (chipCount <= 0) ? Statue.AllIn : Statue.Checked;
    FinishTurn(newStatue);
}
public long GetchipCount()
    {
        
        return chipCount;
    }
private void FinishTurn(Statue newStatue)
{
    photonView.RPC("IsPlaying", RpcTarget.AllBuffered, (int)newStatue);
    GameManager.Instance.photonView.RPC("UpdatePotUI", RpcTarget.AllBuffered, PotManager.Instance.TotalPot);
    

    
    GameManager.Instance.photonView.RPC("PlayerFinishedTurn", RpcTarget.MasterClient);
    GameManager.Instance.photonView.RPC("RPC_PassTurnToNextBySeat", RpcTarget.MasterClient, seatIndex);
}public void OnRaiseButtonClicked()
{
    if (!photonView.IsMine)
        return;
 if (UI != null)
        UI.SetActive(false);
    photonView.RPC("ActedFalse", RpcTarget.Others);

    // ✅ ALL-IN check before deducting
    if (chipCount <= PlayersBet)
    {
        PlayersBet = chipCount;
        photonView.RPC("IsPlaying", RpcTarget.AllBuffered, (int)Statue.AllIn);
    }
    else
    {
        photonView.RPC("IsPlaying", RpcTarget.AllBuffered, (int)Statue.Raise);
    }

    photonView.RPC("RPC_SetActed", RpcTarget.AllBuffered, true);
    chipCount -= PlayersBet;

    // ✅ Broadcast NEW chip count to all
    photonView.RPC("UpdateChipCount", RpcTarget.AllBuffered, chipCount);

    // ✅ Add to pot
    if (PotManager.Instance != null)
    {
        PotManager.Instance.photonView.RPC("AddToPot", RpcTarget.AllBuffered, PhotonNetwork.NickName, PlayersBet);
    }

    Debug.Log($"🎲 [OnRaise] {PlayerName} raising {PlayersBet}, chipCount now {chipCount}");

    GameManager.Instance.callAmount = PlayersBet;
    GameManager.Instance.photonView.RPC("BroadcastCallAmount", RpcTarget.AllBuffered, PlayersBet);

    GameManager.Instance.SetPlayersWaitingAfterRaise(PlayerName);

   

    GameManager.Instance.photonView.RPC("RPC_PassTurnToNextBySeat", RpcTarget.MasterClient, seatIndex);

    if (FirstSit)
        photonView.RPC("SitSync", RpcTarget.AllBuffered, false);

    bool isAllIn = chipCount <= 0;

    if (!isAllIn)
    {
		
        GameManager.Instance.photonView.RPC("ResetTurnStatesRaise", RpcTarget.MasterClient);
        GameManager.Instance.photonView.RPC("PlayerFinishedTurn", RpcTarget.MasterClient);
    }
	else if(isAllIn){//IsPlaying=false;
	//photonView.RPC("RPC_Setplaying", RpcTarget.AllBuffered, false); 
	}
}

[PunRPC]
    public void AddCommunityCardToHand(int cardViewID)
    {
        PhotonView cardView = PhotonView.Find(cardViewID);
        if (cardView == null)
            return;

        Card card = cardView.GetComponent<Card>();
        if (card == null)
            return;

        if (!playerHand.Contains(card))
        {
            playerHand.Add(card);
        }
    }
}
