using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviourPun
{
    public List<PlayerManager> players = new List<PlayerManager>();
    public static GameManager Instance;
    public int minPlayers = 2;
    public bool gameStarted = false;

    // True between EndHandAndRestart and the next hand starting.
    // Prevents PlayerManager.Start() from calling AssignSeat mid-resit.
    [HideInInspector] public bool waitingForResit = false;

    private int nextSeat = 0;

    void Awake() => Instance = this;

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
            StartCoroutine(CheckPlayersRoutine());
    }
    public void AssignSeat(PlayerManager player)
    {
        // ✅ Seat already assigned at spawn — just register
        if (player.seatIndex >= 0)
        {
            RegisterPlayer(player);
            Debug.Log($"[AssignSeat] Player already has seat {player.seatIndex}, registering only");
            return;
        }

        // Fallback
        int seat = nextSeat++;
        player.photonView.RPC("SetSeat", RpcTarget.AllBuffered, seat);
        RegisterPlayer(player);
    }
    // ── Unified watcher: counts PlayerManagers present in scene ──────────────
    // Both initial start AND resit use the same count — just PlayerManager
    // objects existing in the scene. InGame is NOT used as a gate here because
    // InGame is only set AFTER cards are dealt, creating a deadlock.

    public IEnumerator CheckPlayersRoutine()
    {
        while (!gameStarted)
        {
            yield return new WaitForSeconds(0.5f);
            if (!PhotonNetwork.IsMasterClient) continue;

            int count = FindObjectsOfType<PlayerManager>().Length;
            if (count < minPlayers) continue;

            gameStarted = true;
            CleanPlayerLists();

            nextSeat = 0;
            foreach (var p in FindObjectsOfType<PlayerManager>())
                AssignSeat(p);

            // ✅ Wait until ALL players have received their seatIndex (no zeros)
            float timeout = 3f;
            while (timeout > 0f)
            {
                yield return new WaitForSeconds(0.1f);
                timeout -= 0.1f;

                bool allSeated = true;
                var allPlayers = FindObjectsOfType<PlayerManager>();

                // Check for duplicate seat indices (sign that RPCs haven't all landed)
                var seats = new System.Collections.Generic.HashSet<int>();
                foreach (var p in allPlayers)
                {
                    if (!seats.Add(p.seatIndex))
                    {
                        allSeated = false; // duplicate found
                        break;
                    }
                }
                if (allSeated) break;
            }

            Debug.Log("[GameManager] All seats confirmed unique — starting hand");

            if (waitingForResit)
            {
                waitingForResit = false;
                StartNewHand();
                photonView.RPC("RPC_ClientNewHand", RpcTarget.Others);
            }
            else
            {
                OnGameStart();
                photonView.RPC("RPC_ClientGameStart", RpcTarget.Others);
            }
        }
    }

    // ── Removes destroyed/null players from all manager lists ────────────────
    void CleanPlayerLists()
    {
        players.RemoveAll(p => p == null);

        // Clear and re-populate TurnManager and GameFlowManager from scene
        var live = new List<PlayerManager>(FindObjectsOfType<PlayerManager>());

        TurnManager.Instance.ClearPlayers();
        GameFlowManager.Instance.ClearPlayers();

        foreach (var p in live)
        {
            TurnManager.Instance.AddPlayer(p);
            GameFlowManager.Instance.RegisterPlayer(p);
        }

        Debug.Log($"[GameManager] CleanPlayerLists: {live.Count} live players");
    }

    // ── Initial start (first hand) ────────────────────────────────────────────
    void OnGameStart()
    {
        TurnManager.Instance.SortPlayers();
        GameFlowManager.Instance.currentPhase = GameFlowManager.GamePhase.Preflop;
       // DeckManager.Instance.DealCards();
        TurnManager.Instance.StartTurnSystem();
        GameFlowManager.Instance.roundinprogress = true;
    }

    [PunRPC]
    void RPC_ClientGameStart()
    {
        TurnManager.Instance.SortPlayers();
        GameFlowManager.Instance.currentPhase = GameFlowManager.GamePhase.Preflop;
    }

    // ── Subsequent hands (after resit) ────────────────────────────────────────
    void StartNewHand()
    {
        TurnManager.Instance.SortPlayers();
        GameFlowManager.Instance.currentPhase = GameFlowManager.GamePhase.Preflop;
        TurnManager.Instance.StartTurnSystem(); // ✅ handles roles, blinds, deal, turn
    }

    [PunRPC]
    void RPC_ClientNewHand()
    {
        TurnManager.Instance.SortPlayers();
        GameFlowManager.Instance.currentPhase = GameFlowManager.GamePhase.Preflop;
    }

    // ── Called by GameFlowManager.EndHandAndRestart ───────────────────────────
    public void WaitForResitAndRestart()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        gameStarted = false;
        waitingForResit = true;
        StartCoroutine(CheckPlayersRoutine());
    }


    public void UnregisterPlayer(PlayerManager p)
    {
        if (players.Contains(p))
        {
            players.Remove(p);
            Debug.Log($"[TurnManager] Player removed. Count = {players.Count}");
        }
    }
    public void RegisterPlayer(PlayerManager p)
    {
        if (!players.Contains(p))
            players.Add(p);
    }
}