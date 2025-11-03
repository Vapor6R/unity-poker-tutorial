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
private Coroutine timerRoutine;
private bool timerStopped = false;
public TMP_Text timerText;
public TMP_Text actionText;
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
	public GameObject D;
		public GameObject SB;
			public GameObject BB;
	private long ClampLong(long value, long min, long max)
{
    if (value < min) return min;
    if (value > max) return max;
    return value;
}
public void StartCountdown(float duration)
{

    timerStopped = false;

    if (timerRoutine != null)
        StopCoroutine(timerRoutine);

    timerRoutine = StartCoroutine(TimerCoroutine(duration));
}
public void StopTimer()
{
    if (timerRoutine != null)
        StopCoroutine(timerRoutine);

    timerStopped = true;

    photonView.RPC("RPC_OnTimerFinished", RpcTarget.All);
}private IEnumerator TimerCoroutine(float duration)
{
    float timer = duration;
    timerStopped = false;

    while (timer > 0 && !timerStopped)
    {
        timer -= Time.deltaTime;
        photonView.RPC("RPC_UpdateTimerUI", RpcTarget.All, timer);
        yield return null;
    }

    // ✅ Only Master calls finish once
    if (!timerStopped && PhotonNetwork.IsMasterClient)
    {
        photonView.RPC("RPC_OnTimerFinished", RpcTarget.All);
    }
}

[PunRPC]
void RPC_UpdateTimerUI(float timeLeft)
{
	if(!photonView.IsMine)
		return;
    timerText.text = Mathf.CeilToInt(timeLeft).ToString();
}
[PunRPC]
void RPC_OnTimerFinished()
{
    Debug.Log("Timer finished!");

    GameManager.Instance.photonView.RPC("RPC_PassTurnToNextBySeat", RpcTarget.MasterClient, seatIndex);
    // Example: auto-fold player, start next round, pass turn, etc.
}
 [PunRPC]
    public void UpdateActionText(string action, long amount)
    {
        if (actionText != null)
        {
            actionText.text = $"{action} {FormatChipsWithSuffix(amount)}";
            actionText.gameObject.SetActive(true);
        }
    }
    
    // Call this to clear the action text
    [PunRPC]
    public void ClearActionText()
    {
        if (actionText != null)
        {
            actionText.text = "";
            actionText.gameObject.SetActive(false);
        }
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
 OnFold();
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
[PunRPC]
private void Logos()
{


    List<PlayerManager> activePlayers = FindObjectsOfType<PlayerManager>()
        .Where(p => p != null 
            && p.InGame 
            && !p.isFolded 
            && p.chipCount > 0
            && p.seatIndex >= 0
            && p.IsPlaying)
        .OrderBy(p => p.seatIndex)
        .ToList();
    
    if (activePlayers.Count == 0)
        return;
    
    // Deactivate all blind indicators for all players first
    foreach (PlayerManager player in activePlayers)
    {
        if (player.D != null) player.D.SetActive(false);
        if (player.SB != null) player.SB.SetActive(false);
        if (player.BB != null) player.BB.SetActive(false);
    }
    
    int activeCount = activePlayers.Count;
    
    // ✅ Heads-up (2 players): Dealer = SB, other = BB
    if (activeCount <= 2)
    {
        // Lowest seat = Dealer + SB
        PlayerManager dealerPlayer = activePlayers[0];
        if (dealerPlayer.D != null) dealerPlayer.D.SetActive(true);
        if (dealerPlayer.SB != null) dealerPlayer.SB.SetActive(true);
        
        // 2nd lowest seat = BB
        if (activeCount == 2)
        {
            PlayerManager bbPlayer = activePlayers[1];
            if (bbPlayer.BB != null) bbPlayer.BB.SetActive(true);
        }
    }
    // ✅ 3+ players: Normal positions
    else if (activeCount > 2)
    {
        // Lowest seat = Dealer
        PlayerManager dealerPlayer = activePlayers[0];
        if (dealerPlayer.D != null) dealerPlayer.D.SetActive(true);
        
        // 2nd lowest seat = SB
        PlayerManager sbPlayer = activePlayers[1];
        if (sbPlayer.SB != null) sbPlayer.SB.SetActive(true);
        
        // 3rd lowest seat = BB
        PlayerManager bbPlayer = activePlayers[2];
        if (bbPlayer.BB != null) bbPlayer.BB.SetActive(true);
    }
}
private void Update()
{
	
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
	{UI.SetActive(isMyTurn);}
    StartCountdown(20f);
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
        callAmountText.text = $"Call: {FormatChipsWithSuffix(amount)}";
}
[PunRPC]
private void HandleUIForPlayerUTG()
{
    // ✅ Get all eligible players (can take action)
    List<PlayerManager> eligiblePlayers = FindObjectsOfType<PlayerManager>()
        .Where(p => p != null 
            && p.InGame 
            && !p.isFolded 
            && p.chipCount > 0
            && p.seatIndex >= 0
            && p.statue != Statue.Folded
            && p.statue != Statue.AllIn
            && p.IsPlaying)
        .OrderBy(p => p.seatIndex)
        .ToList();
    
    if (eligiblePlayers.Count == 0)
    {
        Debug.LogWarning("⚠️ No eligible players found for UTG");
        return;
    }
    
    // ✅ UTG is the first player in the ordered list (lowest seat index)
    PlayerManager utgPlayer = eligiblePlayers[0];
    
    Debug.Log($"✅ UTG Player: {utgPlayer.PlayerName} (Seat {utgPlayer.seatIndex})");
    
    // ✅ Find the local player
    PlayerManager localPlayer = FindObjectsOfType<PlayerManager>()
        .FirstOrDefault(p => p != null && p.photonView != null && p.photonView.IsMine);
    
    if (localPlayer == null)
    {
        Debug.LogWarning("⚠️ Local player not found");
        return;
    }
    
    // ✅ Check if local player is the UTG player
    if (localPlayer.seatIndex == utgPlayer.seatIndex)
    {
        Debug.Log($"✅ Local player IS UTG. Activating UI for {localPlayer.PlayerName}");
        
        // Activate UI for local player
        if (localPlayer.UI != null)
        {
            localPlayer.UI.SetActive(true);
			localPlayer.StartCountdown(20f);
            localPlayer.ConfigureSliderForChips(localPlayer.chipCount);
        }
        else
        {
            Debug.LogWarning("⚠️ Local player UI is null");
        }
    }
    else
    {
        Debug.Log($"ℹ️ Local player ({localPlayer.PlayerName}, Seat {localPlayer.seatIndex}) is NOT UTG");
        
        // Ensure local player's UI is hidden if they're not UTG
        if (localPlayer.UI != null && localPlayer.UI.activeSelf)
        {
            localPlayer.UI.SetActive(false);
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
				 photonView.RPC("RPC_Setplaying", RpcTarget.AllBuffered, true); 
photonView.RPC("HandleUIForPlayerUTG", RpcTarget.AllBuffered); 
                card.transform.SetParent(cardHandPosition, false);
                card.gameObject.SetActive(photonView.IsMine);
 photonView.RPC("Logos", RpcTarget.AllBuffered); 
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
		StopTimer();
photonView.RPC("RPC_SetFolded", RpcTarget.AllBuffered, true);
photonView.RPC("RPC_Setplaying", RpcTarget.AllBuffered, false); 
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
    
    if (chipCount < Amount)
    {
        // Player is all-in
        Debug.Log($"💰 {gameObject.name} posts Blind ALL-IN: {FormatChipsWithSuffix(chipCount)}");
        long allInAmount = chipCount;
        chipCount = 0;
        
        if (PotManager.Instance != null)
        {
            PotManager.Instance.photonView.RPC("AddToPot", RpcTarget.AllBuffered, PhotonNetwork.NickName, allInAmount);
         photonView.RPC("UpdateActionText", RpcTarget.AllBuffered, "Blind", allInAmount);
		
		}
        
        currentBet = allInAmount;
    }
    else
    {
        // Normal blind post
        Debug.Log($"💰 {gameObject.name} posts Blind: {FormatChipsWithSuffix(Amount)}");
        chipCount -= Amount;
        currentBet = Amount;
        
        if (PotManager.Instance != null)
        {
            PotManager.Instance.photonView.RPC("AddToPot", RpcTarget.AllBuffered, PhotonNetwork.NickName, Amount);           
        photonView.RPC("UpdateActionText", RpcTarget.AllBuffered, "Blind", Amount);
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
        chipCountText.text = $"${ FormatChipsWithSuffix(chipCount)}";
    else
        Debug.LogWarning($"⚠️ chipCountText is null on {gameObject.name}");

    // 🟢 Only update slider for local player

       
}

public void OnCall()
{
	   if (!photonView.IsMine)
	   {return;}


StopTimer();
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
	ConfigureSliderForChips(chipCount);
	photonView.RPC("UpdateActionText", RpcTarget.AllBuffered, "Call", callAmount);
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
	StopTimer();
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
 photonView.RPC("UpdateActionText", RpcTarget.AllBuffered, "Raise", PlayersBet);
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
	ConfigureSliderForChips(chipCount);
	  
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
