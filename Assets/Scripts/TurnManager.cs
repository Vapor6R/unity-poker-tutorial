using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TurnManager : MonoBehaviourPunCallbacks
{
    public static TurnManager Instance;
    public int currentTurnActor = -1;
    public List<PlayerManager> players = new List<PlayerManager>();
    public int currentIndex = 0;
    public long smallBlind = 50;
    public long bigBlind = 100;

    // ✅ Tracks which player index is currently the Dealer — rotates each hand
    public int dealerIndex = 0;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>Called by GameManager.CleanPlayerLists before each new hand.</summary>
    public void ClearPlayers()
    {
        players.Clear();
        currentIndex = 0;
        // NOTE: do NOT reset dealerIndex here — it must persist across hands to rotate
    }

    public void AddPlayer(PlayerManager player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
            Debug.Log("➕ Player added to turn system: " + player.seatIndex);
        }
    }

    // ── Rotation ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Call once per new hand (before AssignRoles) to advance the dealer button.
    /// </summary>
    public void RotateDealer()
    {
        if (players.Count < 2) return;

        // ✅ Rotate by seatIndex reference, not list index
        // Store current dealer's seatIndex, find next player by seatIndex order
        int currentDealerSeat = players[dealerIndex].seatIndex;

        // Sort by seatIndex to find next dealer in seat order
        List<PlayerManager> sorted = new List<PlayerManager>(players);
        sorted.Sort((a, b) => a.seatIndex.CompareTo(b.seatIndex));

        // Find current dealer in sorted list and advance
        int sortedIndex = sorted.FindIndex(p => p.seatIndex == currentDealerSeat);
        int nextSortedIndex = (sortedIndex + 1) % sorted.Count;
        int nextDealerSeat = sorted[nextSortedIndex].seatIndex;

        // Find that player's index in the actual players list
        dealerIndex = players.FindIndex(p => p.seatIndex == nextDealerSeat);

        Debug.Log($"[RotateDealer] Dealer moved from seat {currentDealerSeat} to seat {nextDealerSeat} (list index {dealerIndex})");
    }

    public void ResetGameStarted()
    {
        int count = players.Count;
        if (count <= 1)
            GameManager.Instance.gameStarted = false;
    }

    void GiveTurn(PlayerManager p)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int actor = p.photonView.Owner.ActorNumber;

        // ✅ Remove the early-return guard — currentTurnActor is now always
        // reset to -1 before NextTurn() is called, so this can never be a
        // legitimate duplicate. If it fires, something upstream is broken.
        if (currentTurnActor == actor)
        {
            Debug.LogWarning($"⚠️ [GiveTurn] Duplicate turn attempt for actor {actor} (seat {p.seatIndex}) — forcing through");
            // Don't return — force the turn through so the game doesn't freeze
        }

        currentTurnActor = actor;

        Debug.Log($"GiveTurn seat:{p.seatIndex} actor:{actor}");

        foreach (var player in FindObjectsOfType<PlayerManager>())
            player.photonView.RPC("SetTurnUI", RpcTarget.All, false);

        p.photonView.RPC("SetTurnUI", p.photonView.Owner, true);
    }

    [PunRPC]
    void RPC_GiveTurn(int actorNumber) { } // kept for compatibility, unused


    /// <summary>Called only on preflop — rotates dealer, assigns roles, posts blinds.</summary>
    public void StartTurnSystem()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        AssignRolesAndDeal();
        StartCoroutine(StartTurnRoutine(true));
    }
    void PostBlind(PlayerManager player, long amount, string type)
    {
        player.photonView.RPC("RPC_ApplyBlind", RpcTarget.All, amount, type);

        if (amount > BettingManager.Instance.currentBet)
            BettingManager.Instance.currentBet = amount;

        // ✅ DO NOT call AddContribution here — RPC_ApplyBlind already does it
    }
    void AssignRolesAndDeal()
    {
        players.RemoveAll(p => p == null);
        int count = players.Count;
        if (count < 2) return;

        // ── Clear all roles ──────────────────────────────────────────────
        foreach (var p in players)
        {
            p.role = PlayerManager.PlayerRole.None;
            p.photonView.RPC("RPC_SetRole", RpcTarget.All, (int)PlayerManager.PlayerRole.None);
        }

        if (dealerIndex >= count) dealerIndex = dealerIndex % count;

        // ── Assign roles ─────────────────────────────────────────────────
        PlayerManager sbPlayer = null, bbPlayer = null;

        if (count == 2)
        {
            int sbIndex = dealerIndex;
            int bbIndex = (dealerIndex + 1) % count;

            // ✅ Set role locally BEFORE posting so p.role is readable immediately
            players[sbIndex].role = PlayerManager.PlayerRole.Dealer | PlayerManager.PlayerRole.SmallBlind;
            players[bbIndex].role = PlayerManager.PlayerRole.BigBlind;

            players[sbIndex].photonView.RPC("RPC_SetRole", RpcTarget.All,
                (int)(PlayerManager.PlayerRole.Dealer | PlayerManager.PlayerRole.SmallBlind));
            players[bbIndex].photonView.RPC("RPC_SetRole", RpcTarget.All,
                (int)PlayerManager.PlayerRole.BigBlind);

            sbPlayer = players[sbIndex];
            bbPlayer = players[bbIndex];
        }
        else
        {
            int sbListIndex = (dealerIndex + 1) % count;
            int bbListIndex = (dealerIndex + 2) % count;

            // ✅ Set role locally BEFORE posting so p.role is readable immediately
            players[dealerIndex].role = PlayerManager.PlayerRole.Dealer;
            players[sbListIndex].role = PlayerManager.PlayerRole.SmallBlind;
            players[bbListIndex].role = PlayerManager.PlayerRole.BigBlind;

            players[dealerIndex].photonView.RPC("RPC_SetRole", RpcTarget.All,
                (int)PlayerManager.PlayerRole.Dealer);
            players[sbListIndex].photonView.RPC("RPC_SetRole", RpcTarget.All,
                (int)PlayerManager.PlayerRole.SmallBlind);
            players[bbListIndex].photonView.RPC("RPC_SetRole", RpcTarget.All,
                (int)PlayerManager.PlayerRole.BigBlind);

            sbPlayer = players[sbListIndex];
            bbPlayer = players[bbListIndex];

            Debug.Log($"[AssignRolesAndDeal] D:{players[dealerIndex].seatIndex} SB:{players[sbListIndex].seatIndex} BB:{players[bbListIndex].seatIndex}");
        }

        // ── Post blinds (single unified block — no duplication) ──────────
        if (sbPlayer != null) PostBlind(sbPlayer, smallBlind, "SB");
        if (bbPlayer != null) PostBlind(bbPlayer, bigBlind, "BB");

        BettingManager.Instance.currentBet = bigBlind;

        // ── Deal cards ───────────────────────────────────────────────────
        DeckManager.Instance.BuildDeck();
        DeckManager.Instance.DealCards();
    }
    /// <summary>
    /// Called on flop/turn/river — no blinds, first active player acts first.
    /// ✅ FIX: Reset currentIndex to players.Count so first NextTurn() 
    /// decrements to Count-1 (highest index)
    /// </summary>
    public void StartBettingRound()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        foreach (var p in players)
        {
            if (p == null) continue;
            if (p.isFolded || p.isAllIn) continue;
            p.hasActed = false;
            p.currentBet = 0;
            if (p.handCount == 2)
                p.photonView.RPC("Undarken", RpcTarget.All);
            else
                p.photonView.RPC("RealDarken", RpcTarget.All);
        }

        BettingManager.Instance.currentBet = 0;
        currentIndex = -1;
        StartCoroutine(StartTurnRoutine(false));
    }

    IEnumerator StartTurnRoutine(bool isPreflop)
    {
        Debug.Log($"StartTurnRoutine isPreflop:{isPreflop}");

        int count = players.Count;

        if (isPreflop)
        {
            if (count == 2)
            {
                // HU preflop: Dealer/SB acts first
                currentIndex = dealerIndex;
            }
            else
            {
                // 3+: UTG acts first (left of BB)
                currentIndex = (dealerIndex + 3) % count;
            }
        }
        else
        {
            // Post-flop: first active player left of dealer
            currentIndex = -1;
        }

        yield return new WaitForSeconds(0.5f);

        if (isPreflop)
        {
            if (currentIndex < count && players[currentIndex] != null)
                GiveTurn(players[currentIndex]);
            else
                Debug.LogError($"[StartTurnRoutine] No valid player at index {currentIndex}");
        }
        else
        {
            NextTurn();
        }
    }



    // ── Player management ─────────────────────────────────────────────────────

    /// <summary>
    /// Remove a player from the turn list. Properly removes the entry,
    /// adjusts indices, and halts the game if it's mid-round to prevent
    /// the double-start bug when kicking during HU + 3rd player.
    /// 
    /// ✅ KEY FIX: Halt gameInProgress if player kicked mid-round.
    /// </summary>
    public void RemovePlayer(PlayerManager player)
    {
        int index = players.IndexOf(player);

        if (index == -1) return;

        // ✅ FIX: If game is actively running, halt it to prevent double-start
        bool wasGameInProgress = GameFlowManager.Instance.gameInProgress;
        if (wasGameInProgress)
        {
            Debug.LogWarning($"[RemovePlayer] Player kicked mid-round — halting game to prevent double-start");
            GameFlowManager.Instance.gameInProgress = false;
            GameFlowManager.Instance.BettingRoundFinished = true;
        }

        players.RemoveAt(index);

        Debug.Log($"[RemovePlayer] Player {player.seatIndex} removed. Remaining: {players.Count}");

        if (players.Count == 0) return;

        // ✅ FIX: Adjust currentIndex first
        if (index < currentIndex)
        {
            currentIndex--;
        }
        else if (index == currentIndex)
        {
            if (currentIndex >= players.Count && players.Count > 0)
                currentIndex = currentIndex % players.Count;
        }

        // ✅ FIX: Adjust dealerIndex second
        if (index < dealerIndex)
        {
            dealerIndex--;
        }
        else if (index == dealerIndex)
        {
            if (players.Count > 0)
                dealerIndex = dealerIndex % players.Count;
        }

        Debug.Log($"[RemovePlayer] After adjustment: currentIndex={currentIndex}, dealerIndex={dealerIndex}, total={players.Count}");
    }

    int GetNextValidPlayerIndex(int startIndex)
    {
        for (int i = 0; i < players.Count; i++)
        {
            int index = (startIndex + i + 1) % players.Count;
            if (players[index] != null)
                return index;
        }
        return -1; // no players left
    }

    /// <summary>
    /// Sort players by seat index. Removes any null entries first
    /// to prevent NullReferenceException during comparison.
    /// 
    /// ✅ KEY FIX: Skip sort if game is in progress to prevent
    /// index confusion during active play.
    /// </summary>
    public void SortPlayers()
    {
        players.Sort((a, b) => a.seatIndex.CompareTo(b.seatIndex));
        Debug.Log("[TurnManager] SortPlayers: " + string.Join(", ", players.ConvertAll(p => $"seat{p.seatIndex}")));
    }

    public PlayerManager GetCurrentPlayer()
    {
        if (currentIndex >= 0 && currentIndex < players.Count)
            return players[currentIndex];
        return null;
    }

    public void InitializeTurnOrder()
    {
        players.RemoveAll(p => p == null);
        players.Sort((a, b) => a.seatIndex.CompareTo(b.seatIndex));
    }

    // ── Next turn ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Advance to the next player who can act (not folded, not all-in, has 2 cards).
    /// If no valid player found, the betting round is over — call EndBettingRound().
    /// 
    /// ✅ KEY FIX: DECREMENT instead of increment to go in reverse order (8→7→6→...→0)
    /// This is now the SINGLE entry point for ending a betting round.
    public void NextTurn()
    {
        int safety = 0;
        while (safety < players.Count)
        {
            // ✅ INCREMENT instead of decrement
            currentIndex = (currentIndex + 1) % players.Count;
            PlayerManager p = players[currentIndex];
            safety++;

            if (p == null || p.isFolded || !p.InGame) continue;
            if (p.isAllIn) continue;

            if (!p.hasActed)
            {
                GiveTurn(p);
                return;
            }
        }

        Debug.Log("[NextTurn] no valid player → EndBettingRound");
        GameFlowManager.Instance.EndBettingRound();
    }
    // ── Betting settled check ─────────────────────────────────────────────────

    bool IsBettingSettled()
    {
        long highestBet = 0;

        foreach (var p in players)
        {
            if (p == null || p.isFolded || p.isAllIn) continue;
            if (p.currentBet > highestBet) highestBet = p.currentBet;
        }

        foreach (var p in players)
        {
            if (p == null || p.isFolded || p.isAllIn) continue;
            if (!p.hasActed) return false;
            if (p.currentBet != highestBet) return false;
        }

        return true;
    }
}