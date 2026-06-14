using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class BettingManager : MonoBehaviourPun
{
    public static BettingManager Instance;

    public long currentBet = 0;
    public long pot = 0;

    void Awake()
    {
        Instance = this;
    }

    bool IsBettingRoundFinished()
    {
        foreach (var p in TurnManager.Instance.players)
        {
            if (p == null) continue;
            if (p.isFolded || p.isAllIn) continue;

            if (!p.hasActed)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Called when a player takes an action (check, fold, call, raise).
    /// Disables their UI and determines next action:
    /// - If only 1 player with chips remains, this will be caught by NextTurn()
    /// - If betting round is finished, NextTurn() will find no valid players and call EndBettingRound()
    /// - Otherwise, advance to next player's turn
    /// 
    /// ✅ KEY FIX: Do NOT call EndBettingRound() here — let NextTurn() handle it
    /// </summary>
    public void OnPlayerActed(PlayerManager p)
    {
        p.UI.SetActive(false);
        p.photonView.RPC("RPC_DisableUI", RpcTarget.All);
        p.photonView.RPC("Darken", RpcTarget.All);

        Debug.Log($"Player {p.seatIndex} acted - hasActed set to {p.hasActed}, UI disabled, darkening");

        // ✅ Check if only 1 player with chips — but let NextTurn() end the round
        if (OnlyOnePlayerWithChipsAndActed())
        {
            Debug.Log("[OnPlayerActed] Only 1 player with chips left and has acted — advancing turn");
            TurnManager.Instance.currentTurnActor = -1;
            TurnManager.Instance.NextTurn();  // NextTurn will find no valid players and end round
            return;
        }

        if (IsBettingRoundFinished())
        {
            Debug.Log("Round finished right after action — advancing turn");
            TurnManager.Instance.currentTurnActor = -1;
            TurnManager.Instance.NextTurn();  // NextTurn will find no valid players and end round
            return;
        }

        TurnManager.Instance.currentTurnActor = -1;
        TurnManager.Instance.NextTurn();
    }
    bool OnlyOnePlayerWithChipsAndActed()
    {
        int canAct = 0;
        int notActed = 0;

        foreach (var p in TurnManager.Instance.players)
        {
            if (p == null || p.isFolded || !p.InGame) continue;
            if (p.isAllIn) continue;

            canAct++;
            if (!p.hasActed) notActed++;
        }

        // ✅ Round is only over if everyone who CAN act HAS acted
        return canAct > 0 && notActed == 0;
    }

    public bool CheckRoundEnd()
    {
        Debug.Log("---- CHECK ROUND ----");

        foreach (var p in TurnManager.Instance.players)
        {
            if (p == null) continue;

            long required = currentBet - p.currentBet;

            Debug.Log($"Seat {p.seatIndex} | Acted:{p.hasActed} | Fold:{p.isFolded} | AllIn:{p.isAllIn} | ToCall:{required}");

            if (!p.isFolded && !p.isAllIn)
            {
                if (!p.hasActed || required > 0)
                {
                    Debug.Log("→ ROUND NOT FINISHED");
                    return false;
                }
            }
        }

        Debug.Log("→ ROUND FINISHED");
        return true;
    }

    // ✅ Master processes the action (called by PlayerManager's RPC)
    public void ProcessCheck(PlayerManager p)
    {
        p.hasActed = true;
        OnPlayerActed(p);
    }

    public void ProcessFold(PlayerManager player)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        player.isFolded = true;
        player.hasActed = true;
        player.photonView.RPC("RealDarken", RpcTarget.All);
        player.photonView.RPC("EndMyTurn", RpcTarget.All);
        player.photonView.RPC("SetTurnUI", RpcTarget.All, false);

        // ✅ FIX: Only count players who are InGame (have cards this hand).
        // A seated-but-not-InGame player is a spectator — they don't count
        // as an "active" contestant and should never trigger an early award.
        var activePlayers = System.Array.FindAll(
            TurnManager.Instance.players.ToArray(),
            p => p != null && !p.isFolded && p.InGame
        );

        if (activePlayers.Length == 1)
        {
            // Last InGame player standing wins the pot uncontested
            GameFlowManager.Instance.StartCoroutine(AwardFoldWin(activePlayers[0]));
            return;
        }

        // ✅ FIX: Reset currentTurnActor so GiveTurn's duplicate-actor guard
        // doesn't silently skip the next player.
        TurnManager.Instance.currentTurnActor = -1;
        TurnManager.Instance.NextTurn();
    }

    private IEnumerator AwardFoldWin(PlayerManager winner)
    {
        long pot = PotManager.Instance.TotalPot();
        winner.chips += pot;
        winner.photonView.RPC("RPC_AwardChips", RpcTarget.All, winner.chips, pot, "Opponent Folded");

        // Show result on all clients
        foreach (var p in GameObject.FindObjectsOfType<PlayerManager>())
        {
            if (p.seatIndex == winner.seatIndex)
                p.photonView.RPC("ShowBlindUI", RpcTarget.All, "WIN", pot);
            else
                p.photonView.RPC("ShowBlindUI", RpcTarget.All, "FOLD", 0);
        }

        PotManager.Instance.ResetPot();

        yield return new WaitForSeconds(2f);

        GameFlowManager.Instance.StartCoroutine(
            GameFlowManager.Instance.EndHandAndRestart()
        );
    }

    public void ProcessCall(PlayerManager p)
    {
        if (p.chips == 0 && !p.isAllIn)
            p.isAllIn = true;

        p.hasActed = true;

        Debug.Log($"Player {p.seatIndex} calls — currentBet now {p.currentBet}, chips left {p.chips}");

        // ✅ FIX: If this player called and went all-in, the opponent
        // has already acted (they were the one who raised/went all-in first),
        // so the round IS finished. Don't give turn to anyone.
        // If this player called normally (chips remain), proceed normally.
        OnPlayerActed(p);
    }
    public long GetCallAmount(PlayerManager player)
    {
        long highestBet = GetHighestBetAtTable();
        long diff = highestBet - player.currentBet;
        return diff > 0L ? diff : 0L;
    }

    public long GetHighestBetAtTable()
    {
        long max = 0;

        foreach (PlayerManager p in GameManager.Instance.players)
        {
            if (p.currentBet > max)
                max = p.currentBet;
        }

        return max;
    }

    public void ProcessRaise(PlayerManager p, long amount)
    {
        currentBet = p.currentBet;

        foreach (var player in TurnManager.Instance.players)
        {
            if (player == null) continue;
            if (player.isFolded || player.isAllIn) continue;
            if (player.handCount == 2)
            {
                player.photonView.RPC("Undarken", RpcTarget.All);
                player.hasActed = false;
            }
        }

        p.hasActed = true;
        p.ShowAction("Raise: " + ChipFormatter.Format(amount));

        // ✅ Sync new currentBet to all clients then refresh their call button
        photonView.RPC("RPC_SyncCurrentBet", RpcTarget.All, currentBet);

        OnPlayerActed(p);
    }
	private string FormatChips(long amount)
{
    if (amount >= 1_000_000_000)
        return (amount / 1_000_000_000f).ToString("0.##") + "B";
    if (amount >= 1_000_000)
        return (amount / 1_000_000f).ToString("0.##") + "M";
    if (amount >= 1_000)
        return (amount / 1_000f).ToString("0.##") + "K";
    return amount.ToString("N0"); // e.g. 850 → "850", with locale commas if < 1K
}
    [PunRPC]
    void RPC_SyncCurrentBet(long bet)
    {
        currentBet = bet;
        // Refresh call button for all local players
        foreach (var player in TurnManager.Instance.players)
        {
            if (player != null && player.photonView.IsMine)
                player.RefreshCallButton();
        }
    }
    /// <summary>
    /// Call between streets (flop, turn, river) — NOT before preflop.
    /// Resets hasActed and per-round bet tracking. currentBet on players
    /// resets to 0 because no chips have been committed this street yet.
    /// </summary>
    public void ResetForNewRound()
    {
        currentBet = 0;
        pot = 0;

        foreach (var p in TurnManager.Instance.players)
        {
            if (p == null) continue;
            p.currentBet = 0;
            p.hasActed = false;

            // Sync the reset to all clients
            p.photonView.RPC("RPC_SyncRoundReset", RpcTarget.All);
        }

        Debug.Log("[BettingManager] Street reset — currentBet=0, all hasActed=false, all p.currentBet=0");
    }

    /// <summary>
    /// Call at the very start of a new hand, BEFORE blinds are posted.
    /// Only resets hasActed — blind posting via RPC_SyncBlind will set currentBet correctly.
    /// </summary>
    public void ResetForNewHand()
    {
        currentBet = 0;
        pot = 0;

        foreach (var p in TurnManager.Instance.players)
        {
            if (p == null) continue;
            p.currentBet = 0;
            p.hasActed = false;
            p.isFolded = false;
            p.isAllIn = false;

            p.photonView.RPC("RPC_SyncRoundReset", RpcTarget.All);
        }

        Debug.Log("[BettingManager] Hand reset — all state cleared, awaiting blind posts");
    }
}