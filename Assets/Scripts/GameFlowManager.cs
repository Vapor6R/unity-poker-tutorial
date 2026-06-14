using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class GameFlowManager : MonoBehaviourPun
{
    public bool gameInProgress = false;
    private bool _showdownStarted = false;
    public static GameFlowManager Instance;
    public bool roundinprogress = false;
    private List<PlayerManager> players = new List<PlayerManager>();
    public bool BettingRoundFinished;
    public enum GamePhase { Preflop, Flop, Turn, River, Showdown }
    public GamePhase currentPhase = GamePhase.Preflop;

    void Awake() => Instance = this;

    // ── All-in check ──────────────────────────────────────────────────────────

    bool ShouldSkipToShowdown()
    {
        int canAct = 0;
        foreach (var p in FindObjectsOfType<PlayerManager>())
        {
            Debug.Log($"[ShouldSkipToShowdown] Seat={p.seatIndex} InGame={p.InGame} isFolded={p.isFolded} isAllIn={p.isAllIn}");
            if (p == null || p.isFolded || !p.InGame) continue;
            if (!p.isAllIn) canAct++;
        }
        Debug.Log($"[ShouldSkipToShowdown] canAct={canAct} skip={canAct <= 1}");
        return canAct <= 1;
    }

    // ── Phase progression ─────────────────────────────────────────────────────

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!BettingRoundFinished) return;

        // ✅ FIX: Double guard against in-progress flag
        // This prevents Update from starting a new round if the game
        // was just halted by RemovePlayer()
        if (gameInProgress)
        {
            Debug.LogWarning("[Update] gameInProgress=true, consuming BettingRoundFinished but not starting new round");
            BettingRoundFinished = false;
            return;
        }

        BettingRoundFinished = false;

        // ✅ If no one can bet, deal remaining streets then showdown
        if (ShouldSkipToShowdown())
        {
            Debug.Log($"[Update] All-in at phase {currentPhase} — running AllInShowdown");
            StartCoroutine(AllInShowdownSequence());
            return;
        }

        switch (currentPhase)
        {
            case GamePhase.Preflop:
                currentPhase = GamePhase.Flop;
                ResetActed();
                gameInProgress = true;
                DeckManager.Instance.DealFlop();
                // ✅ KEY FIX: Start betting round after dealing flop!
                TurnManager.Instance.StartBettingRound();
                break;

            case GamePhase.Flop:
                currentPhase = GamePhase.Turn;
                ResetActed();
                gameInProgress = true;
                DeckManager.Instance.DealTurn();
                TurnManager.Instance.StartBettingRound();
                break;

            case GamePhase.Turn:
                currentPhase = GamePhase.River;
                ResetActed();
                gameInProgress = true;
                DeckManager.Instance.DealRiver();
                TurnManager.Instance.StartBettingRound();
                break;

            case GamePhase.River:
                currentPhase = GamePhase.Showdown;
                gameInProgress = true;
                StartCoroutine(ShowdownSequence());
                break;
        }
    }

    // ── All-in runout: deal missing streets then showdown ─────────────────────

    IEnumerator AllInShowdownSequence()
    {
        if (_showdownStarted) yield break; // ✅ prevent double execution
        _showdownStarted = true;
        foreach (var p in FindObjectsOfType<PlayerManager>())
        {
            if (p != null && p.InGame && !p.isFolded)
                p.photonView.RPC("RPC_RevealCards", RpcTarget.All);
        }
        if (currentPhase == GamePhase.Preflop)
        {
            DeckManager.Instance.DealFlop();
            yield return new WaitForSeconds(1f);
            DeckManager.Instance.DealTurn();
            yield return new WaitForSeconds(1f);
            DeckManager.Instance.DealRiver();
            yield return new WaitForSeconds(1f);
        }
        else if (currentPhase == GamePhase.Flop)
        {
            DeckManager.Instance.DealTurn();
            yield return new WaitForSeconds(1f);
            DeckManager.Instance.DealRiver();
            yield return new WaitForSeconds(1f);
        }
        else if (currentPhase == GamePhase.Turn)
        {
            DeckManager.Instance.DealRiver();
            yield return new WaitForSeconds(1f);
        }
        // River — nothing left to deal, fall through

        currentPhase = GamePhase.Showdown;

        // Reveal all active non-folded hands, then evaluate immediately
        StartCoroutine(ShowdownManager.Instance.RunShowdown());
    }

    // ── Normal showdown (after river betting) ─────────────────────────────────

    IEnumerator ShowdownSequence()
    {
        if (_showdownStarted) yield break;
        _showdownStarted = true;

        foreach (var p in FindObjectsOfType<PlayerManager>())
        {
            if (p != null && p.InGame && !p.isFolded)
                p.photonView.RPC("RPC_RevealCards", RpcTarget.All);
        }

        // ✅ FIX: was missing StartCoroutine
        StartCoroutine(ShowdownManager.Instance.RunShowdown());
        yield break;
    }
    void ResetActed()
    {
        foreach (var p in TurnManager.Instance.players)
        {
            if (p == null) continue;
            p.hasActed = false;
            p.currentBet = 0;
            // ✅ Reset call button text to "Check" for new street
            p.photonView.RPC("RPC_UpdateCallButton", RpcTarget.All);
        }

        // ✅ Reset table bet to 0 for new street
        BettingManager.Instance.photonView.RPC("RPC_SyncCurrentBet", RpcTarget.All, 0);

        Debug.Log("[GameFlowManager.ResetActed] Reset all players' hasActed, currentBet and call buttons");
    }
    /// <summary>
    /// Reset per-round flags at the start of a new betting round.
    /// ✅ KEY FIX: Use TurnManager.Instance.players, not the empty local list
    /// </summary>


    public IEnumerator Reset()
    {
        if (gameInProgress)
            yield break;
        
        gameInProgress = false;
        _showdownStarted = false;

        TurnManager.Instance.ResetGameStarted(); 
        photonView.RPC("RPC_ClearAllCards", RpcTarget.All);
        yield return new WaitForSeconds(0.3f);

        // ✅ Destroy all cards on ALL clients, even if PlayerManager is already destroyed
        photonView.RPC("RPC_DestroyAllCards", RpcTarget.All);
        yield return new WaitForSeconds(0.1f);
       
        foreach (var p in FindObjectsOfType<PlayerManager>())
        {
            p.photonView.RPC("RPC_SetInGame", RpcTarget.All, false);
            p.hand.Clear();
            p.hasActed = false;
            p.isFolded = false;
            p.isAllIn = false;
            p.currentBet = 0;
            p.InGame = false;
            p.photonView.RPC("RPC_SyncRoundReset", RpcTarget.All);
            p.photonView.RPC("RPC_SetInGame", RpcTarget.All, false);
            p.photonView.RPC("Undarken", RpcTarget.All);
            p.photonView.RPC("RPC_SetRole", RpcTarget.All, (int)PlayerManager.PlayerRole.None);
        }

        DeckManager.Instance.ResetCommunityCards();
        BettingManager.Instance.ResetForNewHand();
        PotManager.Instance.ResetPot();
        currentPhase = GamePhase.Preflop;
        GameManager.Instance.WaitForResitAndRestart();
    }

    [PunRPC]
    void RPC_DestroyAllCards()
    {
        foreach (GameObject card in GameObject.FindGameObjectsWithTag("Card"))
            Destroy(card);
    }

    public IEnumerator EndHandAndRestart()
    {
        gameInProgress = false;
        _showdownStarted = false;
        if (!PhotonNetwork.IsMasterClient) yield break;
        roundinprogress = false;
        foreach (var p in FindObjectsOfType<PlayerManager>())
            p.photonView.RPC("Check", RpcTarget.AllViaServer);

        yield return new WaitForSeconds(2f);

        photonView.RPC("RPC_ClearAllCards", RpcTarget.All);
        yield return new WaitForSeconds(0.3f);

        foreach (GameObject card in GameObject.FindGameObjectsWithTag("Card"))
            Destroy(card);

        foreach (var p in FindObjectsOfType<PlayerManager>())
        {
            p.photonView.RPC("RPC_SetInGame", RpcTarget.All, false);
            p.hand.Clear();
            p.hasActed = false;
            p.isFolded = false;
            p.isAllIn = false;
            p.currentBet = 0;
            p.InGame = false;
            p.photonView.RPC("RPC_SyncRoundReset", RpcTarget.All);
            p.photonView.RPC("RPC_SetInGame", RpcTarget.All, false);
            p.photonView.RPC("Undarken", RpcTarget.All);
            p.photonView.RPC("RPC_SetRole", RpcTarget.All, (int)PlayerManager.PlayerRole.None);
        }

        BettingManager.Instance.ResetForNewHand();
        PotManager.Instance.ResetPot();
        currentPhase = GamePhase.Preflop;

        // ✅ Rotate dealer at the END of the hand, before next hand starts
        TurnManager.Instance.RotateDealer();

        GameManager.Instance.WaitForResitAndRestart();
    }

    // ── Card clear RPC ────────────────────────────────────────────────────────

    [PunRPC]
    void RPC_ClearAllCards()
    {
        foreach (var p in FindObjectsOfType<PlayerManager>())
        {
            if (p.hand1 != null)
                foreach (Transform child in p.hand1) Destroy(child.gameObject);
            if (p.hand2 != null)
                foreach (Transform child in p.hand2) Destroy(child.gameObject);

            p.hand.Clear();

            if (p.strengthText != null) p.strengthText.gameObject.SetActive(false);
            if (p.actionText != null) p.actionText.gameObject.SetActive(false);
        }
        DeckManager.Instance.ResetCommunityCards();
    }

    // ── Betting round end ─────────────────────────────────────────────────────

    public void EndBettingRound()
    {
        Debug.Log($"[EndBettingRound] isMaster:{PhotonNetwork.IsMasterClient}");
        if (!PhotonNetwork.IsMasterClient) return;

        TurnManager.Instance.currentIndex = 0;

        foreach (var p in FindObjectsOfType<PlayerManager>())
        {
            p.currentBet = 0;
            p.hasActed = false;

            if (p.handCount == 2)
                p.photonView.RPC("Undarken", RpcTarget.All);
            else
                p.photonView.RPC("RealDarken", RpcTarget.All);
        }

        gameInProgress = false;   // ✅ FIX: clear BEFORE setting the flag
        BettingRoundFinished = true;
    }

    // ── Player registry ───────────────────────────────────────────────────────

    public void RegisterPlayer(PlayerManager player)
    {
        if (!players.Contains(player))
            players.Add(player);
    }

    public void UnregisterPlayer(PlayerManager player)
    {
        players.Remove(player);
    }

    public void ClearPlayers()
    {
        players.Clear();
    }
}