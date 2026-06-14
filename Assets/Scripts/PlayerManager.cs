using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using System.Linq;
using System.Globalization;
public class PlayerManager : MonoBehaviourPun
{
    public static PlayerManager LocalInstance { get; private set; }
    private long _raiseAmount = 0L;   // current raise amount
    private long _raiseMax = 0L;   // max raise = chips at turn start
    private int _cachedActorNumber = -1;
    private int _cachedSeatIndex = -1;
    [Header("UI References")]
    public TMP_Text actionText;
    public TMP_Text chipsText;
    public TMP_Text raiseAmountText;
    public TMP_Text callAmountText;
    public Slider raiseSlider;
    public Button raiseUpButton;    // + button  (assign in Inspector)
    public Button raiseDownButton;  // - button  (assign in Inspector)
    public GameObject UI;
    public bool InGame = false;
[SerializeField] TMP_Text nameText;
    [Header("Card / Hand")]
    public List<string> hand = new List<string>();
    public TextMeshProUGUI strengthText;
    public GameObject cardPrefab;
    public Transform hand1;
    public Transform hand2;
    public SeatManager currentSeat;

private long _raiseMin;
    // ── Player state ──────────────────────────────────────────────────────────
    public int seatIndex;
    public bool isFolded;
    public bool isAllIn;
    public long chips = 1000;
    public long currentBet = 0;
    public bool hasActed = false;
    public int handCount => hand.Count;

    [System.Flags]
    public enum PlayerRole { None = 0, Dealer = 1, SmallBlind = 2, BigBlind = 4 }
    public PlayerRole role;
    public bool IsLocalPlayer => photonView.IsMine;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (photonView.IsMine)
            LocalInstance = this;
    }
private void OnSliderMoved(float value)
{
    if (!photonView.IsMine) return;

    if (value >= 1f)
        _raiseAmount = _raiseMax;
    else if (value <= 0f)
        _raiseAmount = _raiseMin; // or your minimum raise
    else
        _raiseAmount = _raiseMin + (long)((double)(value) * (_raiseMax - _raiseMin));

    if (_raiseAmount > chips) _raiseAmount = chips;
    UpdateRaiseLabel(_raiseAmount);
}

    // ── Raise step helpers ────────────────────────────────────────────────────

    /// <summary>Step size = max(1000, 10% of chips).</summary>
    long GetRaiseStep()
    {
        long toCall = BettingManager.Instance != null
            ? BettingManager.Instance.currentBet > currentBet ? BettingManager.Instance.currentBet - currentBet : 0L
            : 0;
        long available = chips - toCall;
        long tenPercent = (long)(available * 0.1);  // double literal avoids float precision loss
        return tenPercent > 1000L ? tenPercent : 1000L;
    }

    public void OnRaiseUp()
    {
        if (!photonView.IsMine) return;
        long next = _raiseAmount + GetRaiseStep();
        _raiseAmount = next < chips ? next : chips;
        SyncSliderVisual();
        UpdateRaiseLabel(_raiseAmount);
    }

    public void OnRaiseDown()
    {
        if (!photonView.IsMine) return;
        long toCall = BettingManager.Instance != null
            ? BettingManager.Instance.currentBet > currentBet ? BettingManager.Instance.currentBet - currentBet : 0L
            : 0;
        long next = _raiseAmount - GetRaiseStep();
        _raiseAmount = next > 0L ? next : 0L;
        SyncSliderVisual();
        UpdateRaiseLabel(_raiseAmount);
    }

    /// <summary>Push _raiseAmount into the slider without triggering OnSliderMoved.</summary>
    void SyncSliderVisual()
    {
        if (raiseSlider == null || _raiseMax <= 0) return;
        raiseSlider.onValueChanged.RemoveListener(OnSliderMoved);
        raiseSlider.value = (float)_raiseAmount / (float)_raiseMax; // normalized 0..1
        raiseSlider.onValueChanged.AddListener(OnSliderMoved);
    }
    IEnumerator Start()
    {
        // Slider configured properly in SetTurnUI when turn begins
        raiseSlider.wholeNumbers = false;
        raiseSlider.value = 0f;
        raiseSlider.onValueChanged.AddListener(OnSliderMoved);

        if (raiseUpButton != null) raiseUpButton.onClick.AddListener(OnRaiseUp);
        if (raiseDownButton != null) raiseDownButton.onClick.AddListener(OnRaiseDown);
        _cachedActorNumber = photonView.Owner?.ActorNumber ?? -1;
        _cachedSeatIndex = seatIndex;

        UI.SetActive(false);
        photonView.RPC("RefreshChipsUI", RpcTarget.All);
        currentSeat = FindObjectOfType<SeatManager>();


        if (PhotonNetwork.IsMasterClient && !GameManager.Instance.waitingForResit)
        {
            GameManager.Instance.AssignSeat(this);
        }

        GameFlowManager.Instance.RegisterPlayer(this);
        TurnManager.Instance.AddPlayer(this);
		
		 yield return new WaitUntil(() => PlayerProfileManager.Instance != null
                                      && PlayerProfileManager.Instance.IsReady);

        nameText.text = PlayerProfileManager.Instance.Profile.displayName;
    }

    // ── OnEnable ──────────────────────────────────────────────────────────────
    void OnEnable()
    {
        if (photonView == null) return;
        photonView.RPC(handCount == 2 ? "Undarken" : "RealDarken", RpcTarget.All);
    }
    void OnDestroy()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_DestroyMyCards", RpcTarget.All);

            if (_cachedSeatIndex >= 0)
                SeatManager.Instance?.photonView.RPC(
                    "RPC_OnPlayerDestroyed", RpcTarget.All, _cachedSeatIndex
                );
        }

        OnFoldButtonPressed();

        TurnManager.Instance?.RemovePlayer(this);
        GameFlowManager.Instance?.UnregisterPlayer(this);
        GameManager.Instance?.UnregisterPlayer(this);
    }
    [PunRPC]
    public void RPC_DestroyMyCards()
    {
        if (hand1 != null)
            foreach (Transform child in hand1)
                Destroy(child.gameObject);
        if (hand2 != null)
            foreach (Transform child in hand2)
                Destroy(child.gameObject);

        hand.Clear();
    }
    // ── Card dealing ──────────────────────────────────────────────────────────

    [PunRPC]
    public void RPC_DealCard(string card, int position)
    {
        if (position < 2)
        {
            hand.Add(card);
            Transform pos = position == 0 ? hand1 : hand2;
            SpawnCard(card, pos);
        }
    }

    void SpawnCard(string card, Transform pos)
    {
        GameObject cardObj = Instantiate(cardPrefab, pos);
        StartCoroutine(DeckManager.SetCardSprite(cardObj, card));
    }

    // ── Chip award ────────────────────────────────────────────────────────────

    [PunRPC]
    public void RPC_AwardChips(long newChipTotal, long amountWon, string handName)
    {
        chips = newChipTotal;   // authoritative total from MasterClient
        Debug.Log($"[RPC_AwardChips] Seat {seatIndex} won {amountWon} ({handName}) | chips now: {chips}");
        RefreshChipsUI();
        ShowAction($"Won {amountWon}!\n{handName}");
    }

    // ── Check (bust detection) ────────────────────────────────────────────────

   [PunRPC]
public void Check()
{
    Debug.Log($"[Check] {photonView.Owner.NickName} chips={chips}");
    foreach (var p in FindObjectsOfType<PlayerManager>())
    {
        if (p.chips == 0 && p.photonView.IsMine)
        {
            if (currentSeat != null)
                currentSeat.OnStandUpClicked(); // ← bust path, seat stays hidden
            else
                Debug.LogWarning("⚠️ No currentSeat reference found!");
        }
    }
}

    // ── Chips UI ──────────────────────────────────────────────────────────────

    [PunRPC]
    void RefreshChipsUI()
    {
        if (chipsText != null)
            chipsText.text = chips.ToString("N0", new CultureInfo("en-US"));
        if (raiseSlider != null && photonView.IsMine)
        {
            raiseSlider.maxValue = chips;
            if (raiseSlider.value > chips)
                raiseSlider.value = chips;
        }
    }

void UpdateRaiseLabel(long amount)
{
    Debug.Log($"[Raise] raw amount = {amount}");  // ADD THIS
    if (raiseAmountText == null) return;
    raiseAmountText.text = $"Raise: {FormatChips(amount)}";
}
public static string FormatChips(long amount)
{
    var ic = System.Globalization.CultureInfo.InvariantCulture;
    if (amount >= 1_000_000_000_000L) return (amount / 1_000_000_000_000.0).ToString("0.##", ic) + "T";
    if (amount >= 1_000_000_000L)     return (amount / 1_000_000_000.0).ToString("0.##", ic)     + "B";
    if (amount >= 1_000_000L)         return (amount / 1_000_000.0).ToString("0.##", ic)          + "M";
    if (amount >= 1_000L)             return (amount / 1_000.0).ToString("0.##", ic)              + "K";
    return amount.ToString();
}
    // ── Action text ───────────────────────────────────────────────────────────

    [PunRPC]
    public void ShowBlindUI(string type, int amount) => ShowAction($"{type}: {amount}");

    public void ShowAction(string message)
    {
        if (actionText == null) return;
        actionText.text = message;
        actionText.gameObject.SetActive(true);
        StopCoroutine("HideAction");
    }

    IEnumerator HideAction()
    {
        yield return new WaitForSeconds(2f);
        if (actionText != null)
            actionText.gameObject.SetActive(false);
    }

    // ── Call button ───────────────────────────────────────────────────────────

    /// <summary>
    /// Update call button text based on current table bet and this player's bet.
    /// Shows "Check" if toCall is 0, otherwise shows "Call {amount}".
    /// 
    /// ✅ KEY FIX: This is called both in SetTurnUI() and RPC_UpdateCallButton()
    /// to ensure the button always shows the correct amount.
    /// </summary>
    public void RefreshCallButton()
    {
        if (BettingManager.Instance == null)
        {
            Debug.LogError($"[RefreshCallButton] Seat {seatIndex}: BettingManager.Instance is NULL!");
            return;
        }

        long tableBet = BettingManager.Instance.currentBet;
        long toCallAmount = tableBet > currentBet ? tableBet - currentBet : 0L;

        if (callAmountText != null)
        {
            callAmountText.text = toCallAmount > 0 ? $"Call {toCallAmount}" : "Check";
            Debug.Log($"[RefreshCallButton] Seat {seatIndex}: tableBet={tableBet}, myBet={currentBet}, toCall={toCallAmount}");
        }
    }

    /// <summary>
    /// Called via RPC when betting round state changes (e.g., after a raise).
    /// Ensures call button shows correct amount for all players.
    /// 
    /// ✅ KEY FIX: When another player raises, this updates everyone's call button.
    /// </summary>
    [PunRPC]
    public void RPC_UpdateCallButton()
    {
        RefreshCallButton();
        Debug.Log($"[RPC_UpdateCallButton] Seat {seatIndex}: Updated call button");
    }

    // ── Turn ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// ✅ KEY FIX: When this player gets their turn, refresh the call button!
    /// This ensures the button shows the correct "Call {amount}" or "Check".
    /// </summary>
    [PunRPC]
    public void SetTurnUI(bool state)
    {
        if (!photonView.IsMine) return;
        UI.SetActive(state);

        if (state)
        {
            RefreshCallButton();

            // ✅ Reset raise amount to 0 at turn start
            if (raiseSlider != null)
            {
                _raiseAmount = 0;
                _raiseMax = chips;
                raiseSlider.wholeNumbers = false;
                raiseSlider.minValue = 0f;
                raiseSlider.maxValue = 1f;
                raiseSlider.value = 0f;
                UpdateRaiseLabel(0);
            }

            Debug.Log($"[SetTurnUI] Seat {seatIndex}: Enabled UI, chips={chips}, sliderMax={raiseSlider?.maxValue}");
        }
    }
    [PunRPC]
    public void SetSeat(int index)
    {
        seatIndex = index;
        Debug.Log($"[SetSeat] Seat assigned: {seatIndex}");
    }
    [PunRPC]
    public void EndMyTurn()
    {
        Debug.Log($"[EndMyTurn] Seat {seatIndex}: Disabling UI");
        UI.SetActive(false);
    }

    // ── Blind ─────────────────────────────────────────────────────────────────
    [PunRPC]
    void RPC_SyncAllIn()
    {
        isAllIn = true;
    }
    [PunRPC]
    public void RPC_ApplyBlind(long amount, string type)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        long actual = amount < chips ? amount : chips;
        chips -= actual;
        currentBet += actual;

        if (chips == 0)
        {
            isAllIn = true;
            photonView.RPC("RPC_SyncAllIn", RpcTarget.All);
        }

        Debug.Log($"[RPC_ApplyBlind] Seat {seatIndex}: {type} {actual} (wanted {amount}) | chips={chips}, currentBet={currentBet}, allIn={isAllIn}");

        // Sync result to all clients
        photonView.RPC("RPC_SyncBlind", RpcTarget.All, chips, currentBet, actual, type);

        PotManager.Instance.AddContribution(seatIndex, actual);
    }

    [PunRPC]
    void RPC_SyncBlind(long newChips, long newBet, long actual, string type)
    {
        chips = newChips;
        currentBet = newBet;
        RefreshChipsUI();
        ShowAction($"{type}: {actual}");
    }
    // ── Actions ───────────────────────────────────────────────────────────────

    public void OnCheckButtonPressed()
    {
        if (!photonView.IsMine) return;
        photonView.RPC("RPC_Check", RpcTarget.AllBuffered);
    }

    public void OnFoldButtonPressed()
    {
        if (!photonView.IsMine) return;
        photonView.RPC("RPC_Fold", RpcTarget.AllBuffered);
    }

    public void OnCallButtonPressed()
    {
        if (!photonView.IsMine) return;
        photonView.RPC("RPC_Call", RpcTarget.AllBuffered);
    }
    public void OnRaiseButtonPressed()
    {
        if (!photonView.IsMine) return;

        long finalAmount = _raiseAmount < chips ? _raiseAmount : chips;

        if (finalAmount <= 0)
        {
            Debug.LogWarning("⚠️ Invalid raise amount");
            return;
        }

        bool goingAllIn = finalAmount >= chips;

        Debug.Log($"[Raise] total={finalAmount} allIn={goingAllIn}");
        photonView.RPC("RPC_Raise", RpcTarget.All, finalAmount, goingAllIn);
    }

    // ── RPC Actions (MasterClient only) ───────────────────────────────────────

    [PunRPC]
    void RPC_Check()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        BettingManager.Instance.ProcessCheck(this);
    }

    [PunRPC]
    void RPC_Fold()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        BettingManager.Instance.ProcessFold(this);
    }

    [PunRPC]
    void RPC_Call()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        long toCall = BettingManager.Instance.currentBet > currentBet ? BettingManager.Instance.currentBet - currentBet : 0L;

        if (toCall == 0)
        {
            photonView.RPC("RPC_SyncCall", RpcTarget.All, chips, currentBet, 0);
            BettingManager.Instance.ProcessCheck(this);
            return;
        }

        long actual = toCall < chips ? toCall : chips;
        chips -= actual;
        currentBet += actual;

        if (chips == 0) isAllIn = true;

        photonView.RPC("RPC_SyncCall", RpcTarget.All, chips, currentBet, actual);
        PotManager.Instance.AddContribution(seatIndex, actual);
        BettingManager.Instance.ProcessCall(this);
    }

    [PunRPC]
    public void RPC_Raise(long amount, bool goingAllIn)
    {
        // ── Only MasterClient mutates authoritative state ─────────────────────
        if (!PhotonNetwork.IsMasterClient) return;

        // ✅ Safety clamp
        long actualAmount = amount < chips ? amount : chips;

        if (actualAmount <= 0)
        {
            Debug.LogWarning("⚠️ Invalid raise received");
            return;
        }

        // ✅ Force ALL-IN: spend every chip, leave zero remainder
        if (goingAllIn || actualAmount >= chips)
        {
            actualAmount = chips;
            chips = 0;
            currentBet += actualAmount;
            goingAllIn = true;
        }
        else
        {
            chips -= actualAmount;
            currentBet += actualAmount;
        }

        // ✅ Sync authoritative state to ALL clients
        photonView.RPC("RPC_SyncRaise", RpcTarget.All, chips, currentBet, actualAmount, goingAllIn);

        if (goingAllIn)
        {
            Debug.Log("🔥 ALL-IN");
            this.isAllIn = true;
            photonView.RPC("RPC_SyncAllIn", RpcTarget.All);
        }

        // ✅ Record raiser's contribution into the pot
        PotManager.Instance.AddContribution(seatIndex, actualAmount);

        // ✅ Tell BettingManager to process the raise
        BettingManager.Instance.ProcessRaise(this, actualAmount);
    }

    [PunRPC]
    void RPC_SyncCall(long newChips, long newBet, long amountActuallyDeducted)
    {
        chips = newChips;
        currentBet = newBet;

        RefreshChipsUI();
        if (photonView.IsMine)
            ShowAction($"Call {ChipFormatter.Format(amountActuallyDeducted)}");
    }
    [PunRPC]
    void RPC_SyncRaise(long newChips, long newBet, long amountDeducted, bool allIn)
    {
        chips = newChips;
        currentBet = newBet;

        RefreshChipsUI();
        ShowAction(allIn ? "ALL-IN" : $"Raise {amountDeducted}");

        // ✅ Force ALL players to refresh their call button
        if (BettingManager.Instance != null)
        {
            foreach (var p in FindObjectsOfType<PlayerManager>())
            {
                p.RefreshCallButton();
            }
        }
    }

    // ── Card reveal ────────────────────────────────────────────────────────────



    // ── Darken / Undarken ─────────────────────────────────────────────────────

    [PunRPC]
    public void Darken()
    {
        if (hand1 != null)
            foreach (Transform child in hand1)
            {
                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = Color.gray;
            }
        if (hand2 != null)
            foreach (Transform child in hand2)
            {
                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = Color.gray;
            }
    }

    [PunRPC]
    public void RealDarken()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers)
            sr.color = Color.gray;
    }

    [PunRPC]
    public void Undarken()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers)
            sr.color = Color.white;
    }

    // ── RPC disable UI ────────────────────────────────────────────────────────

    [PunRPC]
    public void RPC_DisableUI()
    {
        UI.SetActive(false);
    }

    // ── RPC set role ──────────────────────────────────────────────────────────

    [PunRPC]
    public void RPC_SetRole(int roleFlags)
    {
        role = (PlayerRole)roleFlags;
    }

    // ── RPC sync round reset ──────────────────────────────────────────────────

    [PunRPC]
    public void RPC_SyncRoundReset()
    {
        currentBet = 0;
        hasActed = false;
    }

    // ── RPC set InGame ────────────────────────────────────────────────────────

    [PunRPC]
    public void RPC_SetInGame(bool state)
    {
        InGame = state;
    }
    [PunRPC]
    public void ReceiveCards(string card1, string card2)
    {

        hand.Clear();
        hand.Add(card1);
        hand.Add(card2);
        if (!photonView.IsMine) return;
        SpawnCard(card1, hand1);
        SpawnCard(card2, hand2);

        Debug.Log($"[ReceiveCards] Seat {seatIndex} received {card1} {card2}");
    }
    // ── RPC highlight winning cards ───────────────────────────────────────────

    [PunRPC]
    public void RPC_HighlightWinningCards(string[] bestCards)
    {
        foreach (string card in bestCards)
        {
            if (hand.Contains(card))
            {
                foreach (Transform hand_transform in new[] { hand1, hand2 })
                {
                    if (hand_transform == null) continue;
                    foreach (Transform child in hand_transform)
                    {
                        SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.color = Color.green;
                    }
                }
            }
        }
    }
    [PunRPC]
    public void RPC_RevealCards()
    {
        // Tell DeckManager to respawn this player's cards visibly on all clients
        if (hand.Count == 2)
        {
            SpawnCard(hand[0], hand1);
            SpawnCard(hand[1], hand2);
        }
    }
}