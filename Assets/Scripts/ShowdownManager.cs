using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using System.Collections;
/// <summary>
/// Runs showdown on MasterClient, then broadcasts results to all clients.
/// Call ShowdownManager.Instance.RunShowdown() after the river betting ends.
/// </summary>
public class ShowdownManager : MonoBehaviourPun
{
    public static ShowdownManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Entry point ───────────────────────────────────────────────────────────

    /// <summary>
    /// Call on MasterClient only (e.g. from GameFlowManager after river betting).
    /// </summary>
    public IEnumerator RunShowdown()
    {
        if (!PhotonNetwork.IsMasterClient) yield break;

        PlayerManager[] allPlayers = FindObjectsOfType<PlayerManager>();
        List<PlayerManager> players = new List<PlayerManager>(allPlayers);
        List<string> community = DeckManager.Instance.communityCards;

        // Force-reveal all non-folded hands to every client before evaluating
        foreach (var p in players.Where(p => !p.isFolded))
            p.photonView.RPC("RPC_RevealCards", RpcTarget.All);

        // AwardPots evaluates hands per-pot internally and distributes chips.
        // It returns the main-pot result so we can announce the winner.
        HandEvaluator.ShowdownResult result = PotManager.Instance.AwardPots(players);
        if (result == null) yield break;

        string[] bestCards = result.winningHand.bestHand.ToArray();

        // ✅ Highlight immediately on ALL clients right after evaluation — no delay
        foreach (var winner in result.winners)
            winner.photonView.RPC("RPC_HighlightWinningCards", RpcTarget.All, bestCards);

        // ✅ Also highlight matching community cards on ALL clients
        DeckManager.Instance.photonView.RPC("RPC_HighlightCommunityCards", RpcTarget.All, bestCards);

        int[] winnerSeats = result.winners.Select(p => p.seatIndex).ToArray();
        string handName   = result.winningHand.RankName;

        photonView.RPC("RPC_AnnounceWinner", RpcTarget.All, winnerSeats, handName, bestCards);
yield return new WaitForSeconds(2f);
        if (PhotonNetwork.IsMasterClient)
            GameFlowManager.Instance.StartCoroutine(
                GameFlowManager.Instance.EndHandAndRestart()
            );
    }

    // ── RPC received by ALL clients ───────────────────────────────────────────

    [PunRPC]
    void RPC_AnnounceWinner(int[] winnerSeats, string handName, string[] bestCards)
    {
        PlayerManager[] all = FindObjectsOfType<PlayerManager>();

        // Highlight winners, darken losers
        foreach (var p in all)
        {
            if (winnerSeats.Contains(p.seatIndex))
            {
                p.ShowAction($"WINNER! {handName}");
                p.Undarken();
            }
            else if (!p.isFolded)
            {
                p.ShowAction("Lost");
                p.RealDarken();
            }
        }

        string names = string.Join(" & ", winnerSeats.Select(s => $"Seat {s}"));
        Debug.Log($"[Showdown] Winner(s): {names} with {handName} | Best: {string.Join(", ", bestCards)}");
    }
}