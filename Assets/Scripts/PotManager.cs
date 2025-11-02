using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using System.Collections;
using TMPro;
using System;

[Serializable]
public class PlayerContributionEntry
{
    public string playerName;
    public long amount;
}
public class PotManager : MonoBehaviourPunCallbacks
{
    public class PlayerContribution
    {
        public string playerName;
        public long bet;
        public bool folded;
    }

    public class SidePot
    {
        public long amount;
        public List<string> eligiblePlayers = new List<string>();
    }
	public TMP_Text potText;
	    public long TotalPot;
public Dictionary<string, long> playerContributions = new Dictionary<string, long>();
[SerializeField] 
    private List<PlayerContributionEntry> playerContributionsDebug = new List<PlayerContributionEntry>();
    
    public Dictionary<string, long> PlayerContributions
    {
        get { return playerContributions; }
    }
    
    // Call this in Update() or LateUpdate() to sync dictionary to inspector list
public static PotManager Instance;
    public List<SidePot> sidePots = new List<SidePot>();
private GameManager gameManager;
private bool isDistributing = false;
public Deck DeckInstance;
    
private void Update()
{
	LateUpdate();
}
            private void LateUpdate()
    {
        // Update inspector list with current dictionary values
        playerContributionsDebug.Clear();
        foreach (var kvp in playerContributions)
        {
            playerContributionsDebug.Add(new PlayerContributionEntry 
            { 
                playerName = kvp.Key, 
                amount = kvp.Value 
            });
        }
    }

		 void Awake() {gameManager = FindObjectOfType<GameManager>();
    if (Instance != null && Instance != this) {
      Debug.LogWarning("Duplicate GameManager found, destroying it: " + gameObject.name);
      Destroy(gameObject);
      return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);  // Optional: use only if you load scenes and want to keep GameManager
    Debug.Log("GameManager initialized: " + gameObject.name);
  }
    
	[PunRPC]
public void AddToPot(string playerName, long betAmount)
{

    Debug.Log($"💰 [AddToPot] Adding {betAmount} from {playerName} to pot");

    if (playerContributions.ContainsKey(playerName))
        playerContributions[playerName] += betAmount;
    else
        playerContributions[playerName] = betAmount;

    TotalPot += betAmount;
    
    Debug.Log($"💰 [AddToPot] TotalPot is now: {TotalPot}");
    
    photonView.RPC("UpdatePotUI", RpcTarget.AllBuffered, TotalPot);
    
    Debug.Log("==== PLAYER CONTRIBUTIONS ====");
    var contributions = PlayerContributions;
    foreach (var kv in contributions)
        Debug.Log($"{kv.Key}: {kv.Value}");
}

[PunRPC]
    public void UpdatePotUI(long x)
    {
        if (potText != null)
            potText.text = $"Pot: {x}";
    }
  public void CalculateSidePots()
{
    sidePots.Clear();

    var contributions = PlayerContributions;

    if (contributions.Count == 0)
    {
        Debug.LogWarning("⚠️ No player contributions found.");
        return;
    }

    // 1️⃣ Sort by contribution ascending
    var sorted = contributions
        .OrderBy(c => c.Value)
        .ToList();

    // 2️⃣ Collect distinct contribution levels
    var distinctTiers = sorted
        .Select(c => c.Value)
        .Distinct()
        .OrderBy(v => v)
        .ToList();

    long previousTier = 0;

    foreach (var tier in distinctTiers)
    {
        long potShare = tier - previousTier;
        if (potShare <= 0) continue;

        // ✅ Only players who contributed >= current tier are eligible
        var eligible = sorted
            .Where(c => c.Value >= tier)
            .Select(c => c.Key)
            .ToList();

        if (eligible.Count == 0) continue;

        long potAmount = potShare * eligible.Count;

        SidePot pot = new SidePot
        {
            amount = potAmount,
            eligiblePlayers = eligible
        };

        sidePots.Add(pot);

        previousTier = tier;
    }

    // 🧩 Debug Output
    Debug.Log("==== 🪙 SIDE POTS CREATED (FIXED) ====");
    for (int i = 0; i < sidePots.Count; i++)
    {
        var pot = sidePots[i];
        Debug.Log($"Pot #{i + 1}: {pot.amount} chips | Eligible: {string.Join(", ", pot.eligiblePlayers)}");
    }

    // ✅ Summary Total
    long total = sidePots.Sum(p => p.amount);
    Debug.Log($"💰 Total Pots Combined: {total}");
}
 public IEnumerator AwardPots(List<string> showdownOrder)
{
    isDistributing = true;

    
    if (sidePots.Count == 0)
    {
        Debug.LogWarning("⚠️ No side pots to award!");
        isDistributing = false;
        yield break;
    }

    Debug.Log("==== 🏆 AWARDING SIDE POTS ====");

    foreach (var pot in sidePots)
    {
        // ✅ Restrict to eligible players only
        var eligiblePlayers = showdownOrder
            .Where(name => pot.eligiblePlayers.Contains(name))
            .ToList();

        if (eligiblePlayers.Count == 0)
        {
            Debug.LogWarning("⚠️ No eligible players for this pot!");
            continue;
        }

        // ✅ The first eligible player in showdown order wins this pot
        string winnerName = eligiblePlayers.First();

        PlayerManager winner = GameManager.Instance.FindPlayerByName(winnerName);
        if (winner == null)
        {
            Debug.LogWarning($"⚠️ Could not find PlayerManager for {winnerName}");
            continue;
        }

        // ✅ Award the full pot to that player via Photon RPC
        winner.photonView.RPC("AddChipsRPC", winner.photonView.Owner, pot.amount);

        // Optional delay for animation / pacing
        yield return new WaitForSeconds(2);

        Debug.Log($"🏆 {winnerName} wins {pot.amount} chips from pot (Eligible: {string.Join(", ", pot.eligiblePlayers)})");
		Deck.Instance.photonView.RPC("Dfalse", RpcTarget.AllBuffered);
    }

    // ✅ Optional: summary total
    long totalAwarded = sidePots.Sum(p => p.amount);
    Debug.Log($"💰 Total Chips Awarded: {totalAwarded}");

    isDistributing = false;

    // ✅ All done — trigger restart
DeckInstance.photonView.RPC("ResetInProgressF", RpcTarget.AllBuffered);
    GameManager.Instance.photonView.RPC("RoundInProgressF", RpcTarget.AllBuffered);
	 GameManager.Instance.photonView.RPC("BlindF", RpcTarget.AllBuffered);
	 SpawnButtonManager spawn = FindObjectOfType<SpawnButtonManager>();
if (spawn != null && spawn.photonView != null)
{
    spawn.photonView.RPC("RPC_RotateSeats", RpcTarget.AllBuffered);
    Debug.Log("🔄 [PotManager] Seats rotated successfully after awarding chips.");
}
else
{
    Debug.LogError("❌ SpawnButtonManager not found! Cannot rotate seats.");
}
	  DeckInstance.photonView.RPC("DeckFalse", RpcTarget.AllBuffered);
	  foreach (PlayerManager pm in FindObjectsOfType<PlayerManager>())
    {
        if (pm != null && pm.photonView != null)
        {
            pm.photonView.RPC("Check", RpcTarget.AllViaServer);
			pm.IsPlaying=false ;       }
    }
    int activeCount = FindObjectsOfType<PlayerManager>()
        .Count(pm => pm != null && pm.InGame && pm.chipCount > 0);

    if (activeCount <= 1)
    {

        yield break;
    }
		  DeckInstance.photonView.RPC("DeckFalse", RpcTarget.AllBuffered);
DeckInstance.photonView.RPC("Dfalse", RpcTarget.AllBuffered);
	 GameManager.Instance.photonView.RPC("Reset", RpcTarget.AllBuffered);
StartCoroutine(NextStage());
    yield break;
}

 public IEnumerator NextStage()
{
	yield return new WaitForSeconds(1f);
	 GameManager.Instance.photonView.RPC("ProgressF", RpcTarget.AllBuffered);
        GameManager.Instance.photonView.RPC("StartSit", RpcTarget.MasterClient);
 GameManager.Instance.photonView.RPC("ResetTurnStatesForOthers", RpcTarget.AllBuffered);
    GameManager.Instance.photonView.RPC("blindf", RpcTarget.AllBuffered);

    yield break;

}}
