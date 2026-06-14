using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using TMPro;

/// <summary>
/// Tracks every chip that enters the pot this hand.
/// Calculates main pot + side pots when all-in players are involved.
/// Lives on MasterClient; results are broadcast via RPC for UI updates.
/// </summary>
public class PotManager : MonoBehaviourPun
{
    public static PotManager Instance;

    [Header("UI")]
    public TMP_Text potText;   // optional — shows "Pot: 320"

    // ── Data ──────────────────────────────────────────────────────────────────

    /// <summary>Total chips contributed this hand per seatIndex.</summary>
    public Dictionary<int, long> contributions = new Dictionary<int, long>();

    // ── Structs ───────────────────────────────────────────────────────────────

    public struct Pot
    {
        public long amount;
        public List<int> eligibleSeats; // seats that can win this pot

        public override string ToString() =>
            $"Pot {amount} — eligible: [{string.Join(", ", eligibleSeats)}]";
    }

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API (MasterClient only) ────────────────────────────────────────

    /// <summary>
    /// Record chips going into the pot.  Call for blinds, calls, raises, all-ins.
    /// </summary>
    public void AddContribution(int seatIndex, long amount)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!contributions.ContainsKey(seatIndex))
            contributions[seatIndex] = 0;
        contributions[seatIndex] += amount;

        long total = TotalPot();
        string allContribs = "";
        foreach (var kv in contributions)
            allContribs += $"Seat{kv.Key}={kv.Value} ";
        Debug.Log($"[PotManager] AddContribution → Seat {seatIndex} +{amount} | Contributions: {allContribs.Trim()} | TotalPot={total}");

        BroadcastTotal();
    }

    /// <summary>
    /// Returns the raw total (sum of all contributions). Useful for quick display.
    /// </summary>
    public long TotalPot()
    {
        long total = 0;
        foreach (var v in contributions.Values) total += v;
        return total;
    }

    /// <summary>
    /// Calculate main pot + side pots.
    /// Pass the list of active (non-folded) players so eligibility is correct.
    /// Returns ordered list: index 0 = main pot, rest = side pots.
    /// </summary>
    public List<Pot> CalculatePots(List<PlayerManager> allPlayers)
    {
        // Build a working copy: only players who contributed > 0
        // Key = seatIndex, Value = total contributed
        var contribs = new Dictionary<int, long>(contributions);

        // Seats of non-folded players (eligible to win)
        var activeSeatSet = new HashSet<int>(
            allPlayers.Where(p => !p.isFolded).Select(p => p.seatIndex));

        // All seats that put chips in (including folded — their chips stay)
        var allSeats = contribs.Keys.ToList();

        List<Pot> pots = new List<Pot>();

        // Iterative side-pot algorithm
        // Each pass: find the smallest all-in stack, create a pot capped at that level
        while (contribs.Count > 0 && contribs.Values.Any(v => v > 0))
        {
            // Minimum non-zero contribution this pass
            long cap = contribs.Values.Where(v => v > 0).Min();

            long potAmount = 0;
            var eligible = new List<int>();

            foreach (var seat in allSeats.ToList())
            {
                if (!contribs.ContainsKey(seat)) continue;

                long take = Math.Min(contribs[seat], cap);
                potAmount += take;
                contribs[seat] -= take;

                if (contribs[seat] == 0)
                    contribs.Remove(seat);

                // Eligible = contributed to this level AND not folded
                if (activeSeatSet.Contains(seat))
                    eligible.Add(seat);
            }

            if (potAmount > 0)
                pots.Add(new Pot { amount = potAmount, eligibleSeats = eligible });
        }

        return pots;
    }

    /// <summary>
    /// Evaluate hands, award each pot to the correct winner(s), and return
    /// the main-pot result so ShowdownManager can announce it.
    /// MasterClient only.
    /// </summary>
    public HandEvaluator.ShowdownResult AwardPots(List<PlayerManager> allPlayers)
    {
        if (!PhotonNetwork.IsMasterClient) return null;

        List<string> community = DeckManager.Instance.communityCards;
        List<Pot> pots = CalculatePots(allPlayers);

        HandEvaluator.ShowdownResult mainResult = null;

        foreach (var pot in pots)
        {
            var eligible = allPlayers
                .Where(p => pot.eligibleSeats.Contains(p.seatIndex) && !p.isFolded && p.InGame)
                .ToList();

            if (eligible.Count == 0) continue;

            var result = HandEvaluator.DetermineWinner(eligible, community);
            if (result == null) continue;

            // Keep the main pot (first/largest) result for the announcement
            if (mainResult == null) mainResult = result;

            long share = pot.amount / result.winners.Count;
            long remainder = pot.amount % result.winners.Count;

            // In PotManager.cs — replace the award block inside AwardPots()
            foreach (var winner in result.winners)
            {
                long award = share + (remainder > 0 ? 1 : 0);
                remainder = Math.Max(0L, remainder - 1);

                // Award on master first so chips value is correct before sync
                winner.chips += award;

                // Then broadcast the authoritative new chip count to ALL clients
                winner.photonView.RPC("RPC_AwardChips", RpcTarget.All, winner.chips, award, result.winningHand.RankName);

                Debug.Log($"[Pot] Seat {winner.seatIndex} wins {award} ({result.winningHand.RankName})");
            }
        }

        ResetPot();
        return mainResult;
    }

    /// <summary>Clear all contributions. Call at the start of each new hand.</summary>
    public void ResetPot()
    {
        contributions.Clear();
        BroadcastTotal();
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    void BroadcastTotal()
    {
        photonView.RPC("RPC_UpdatePotUI", RpcTarget.All, TotalPot());
    }


	[PunRPC]
void RPC_UpdatePotUI(long total)
{
    if (potText != null)
        potText.text = $"Pot: {ChipFormatter.Format(total)}";
}
}