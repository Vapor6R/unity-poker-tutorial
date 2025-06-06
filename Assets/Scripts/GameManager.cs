using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System;
using System.Globalization;

using System.Linq;
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
	[System.Serializable]
public class Pot
{
    public long Amount;
    public List<string> EligiblePlayers = new();
    public Dictionary<string, long> Contributions = new();

	public object[] Serialize()
    {
        string[] players = EligiblePlayers.ToArray();

        string[] contribPlayers = Contributions.Keys.ToArray();
        long[] contribAmounts = Contributions.Values.ToArray();

        return new object[]
        {
            Amount,
            players,
            contribPlayers,
            contribAmounts
        };
    }

    // Deserialize object array back to Pot instance
    public static Pot Deserialize(object[] data)
    {
        Pot pot = new Pot();
        pot.Amount = (long)data[0];
        pot.EligiblePlayers = new List<string>((string[])data[1]);

        string[] contribPlayers = (string[])data[2];
        long[] contribAmounts = (long[])data[3];
        pot.Contributions = new Dictionary<string, long>();

        for (int i = 0; i < contribPlayers.Length; i++)
        {
            pot.Contributions[contribPlayers[i]] = contribAmounts[i];
        }

        return pot;
    }
}
[System.Serializable]
public class PlayerResultEntry
{
    public string playerName;
    public HandRank rank;
    public List<int> rankValues;
}
public class GameManager : MonoBehaviourPunCallbacks
{
private bool winnerDetermined = false;
	public Transform potDisplayPanel; // Assign in Inspector
public TMP_Text potEntryTemplate;
private const byte EVENT_CODE_SYNC_POTS = 200;
public Dictionary<string, long> playerContributions = new();
public List<PlayerResultEntry> playerResultsList = new List<PlayerResultEntry>();
public Dictionary<string, (HandRank rank, List<int> rankValues)> playerResults = new();
public List<PlayerManager> players = new List<PlayerManager>();
public List<Pot> pots = new List<Pot>();
  private Dictionary<string, long> playerBets = new();
private List<HandResult> results = new List<HandResult>();
 public List<Player> PlayersInGame = new List<Player>();
[SerializeField] private List<string> playerNamesInGame = new List<string>();
	public int PlayerCount => PlayersInGame.Count;
	private int handResultsReceived = 0;
private bool hasEvaluated = false;
	private ClickSpawner  clickSpawner ;
	private bool positionsAlreadyAssigned = false;
	public List<PlayerManager> allPlayers;
	public TMP_Text potText;
	    public const long SMALL_BLIND_AMOUNT = 100;
    public const long BIG_BLIND_AMOUNT = 200;
		public long potAmount = 0;
public HashSet<PlayerPosition> AssignedPositions = new HashSet<PlayerPosition>();
public static GameManager Instance { get; private set; }
    public bool Progress = false;
		public Deck DeckInstance;
			public int playersFinished = 0;
				 public int totalPlayers;
				 public bool turn = false;
	    public bool flop = false;
    public bool river = false;
	public bool FirstTurn = true;
	public long callAmount = 0;

public long CallAmount 
{ 
    get => callAmount; 
    set => callAmount = value; 
}
public void SyncPots()
{
    List<object> serializedPots = new List<object>();

    foreach (Pot pot in pots)
    {
        serializedPots.Add(pot.Serialize());
    }

    // Send as a RaiseEvent or Photon Custom Room Property
    PhotonNetwork.RaiseEvent(
        EVENT_CODE_SYNC_POTS,
        serializedPots.ToArray(),
        new RaiseEventOptions { Receivers = ReceiverGroup.All },
        new SendOptions { Reliability = true }
    );
}
public void AddPlayerContribution(string playerName, long amount)
{
    if (!playerContributions.ContainsKey(playerName))
        playerContributions[playerName] = 0;

    playerContributions[playerName] += amount;
}
public void UpdatePotDisplayUI()
{
    // Remove all children except the template
    foreach (Transform child in potDisplayPanel)
    {
        if (child != potEntryTemplate.transform)
            Destroy(child.gameObject);
    }

    for (int i = 0; i < pots.Count; i++)
    {
        Pot pot = pots[i];
        TMP_Text entry = Instantiate(potEntryTemplate, potDisplayPanel);
        entry.gameObject.SetActive(true);

        string eligible = string.Join(", ", pot.EligiblePlayers);
        entry.text = (pots.Count == 1)
            ? $"Main Pot: ${pot.Amount:N0}"
            : $"Side Pot {i + 1}: ${pot.Amount:N0} (Eligible: {eligible})";
    }
}
public void AddChipsToPot(string playerName, long amount)
{
    photonView.RPC("RPC_AddChipsToPot", RpcTarget.MasterClient, playerName, amount);
}
[PunRPC]
public void RPC_AddChipsToPot(string playerName, long amount)
{
    if (!PhotonNetwork.IsMasterClient) return;

    Debug.Log($"[Master] AddChipsToPot: player={playerName}, amount={amount}");

    if (!playerContributions.ContainsKey(playerName))
        playerContributions[playerName] = 0;

    playerContributions[playerName] += amount;
    potAmount += amount;

    photonView.RPC("UpdatePotUI", RpcTarget.AllBuffered, potAmount);
	Debug.Log($"[RPC_AddChipsToPot] player: {playerName}, amount: {amount}, new pot: {potAmount}");
 
}
public List<Pot> CreateSidePotsFromContributions()
{
    List<Pot> createdPots = new();

    var contributions = playerContributions.OrderBy(kvp => kvp.Value).ToList();
    long lastAmount = 0;
    List<string> remainingPlayers = contributions.Select(c => c.Key).ToList();

    foreach (var (playerName, contribution) in contributions)
    {
        long diff = contribution - lastAmount;
        if (diff > 0)
        {
            Pot pot = new Pot
            {
                Amount = diff * remainingPlayers.Count,
                EligiblePlayers = new List<string>(remainingPlayers)
            };

            foreach (string p in remainingPlayers)
            {
                pot.Contributions[p] = diff;
            }

            createdPots.Add(pot);
            lastAmount = contribution;
        }

        remainingPlayers.Remove(playerName);
    }

    return createdPots;
}



 public void SyncDictToList()
    {

        foreach(var kvp in playerResults)
        {
            playerResultsList.Add(new PlayerResultEntry()
            {
                playerName = kvp.Key,
                rank = kvp.Value.rank,
                rankValues = kvp.Value.rankValues
            });
        }
    }
private List<Pot> CreateSidePots()
{
    var pots = new List<Pot>();

    var contributions = playerBets
        .OrderBy(entry => entry.Value)
        .ToList();

    long previous = 0;

    while (contributions.Count > 0)
    {
        long current = contributions[0].Value;
        long betLevel = current - previous;

        var eligible = contributions.Select(entry => entry.Key).ToList();
        long potAmount = betLevel * eligible.Count;

        pots.Add(new Pot
        {
            Amount = potAmount,
            EligiblePlayers = new List<string>(eligible)
        });

        previous = current;
        contributions.RemoveAll(entry => entry.Value == current);
    }

    // Return excess chips
    var totalPerPlayer = new Dictionary<string, long>();
    foreach (var pot in pots)
    {
        foreach (string player in pot.EligiblePlayers)
        {
            if (!totalPerPlayer.ContainsKey(player))
                totalPerPlayer[player] = 0;

            totalPerPlayer[player] += pot.Amount / pot.EligiblePlayers.Count;
        }
    }

    foreach (var entry in playerBets)
    {
        long excess = entry.Value - totalPerPlayer.GetValueOrDefault(entry.Key, 0);
        if (excess > 0)
        {
            Debug.Log($"💸 Returning excess {excess} to {entry.Key}");
            AwardChips(entry.Key, excess);
        }
    }

    return pots;
}
private int GetExpectedPlayerCount()
{
return FindObjectsOfType<PlayerManager>().Length;
}
private PlayerManager FindPlayerManagerByPhotonPlayer(Player photonPlayer)
{
    foreach (var pm in FindObjectsOfType<PlayerManager>())
    {
        if (pm.photonView.Owner == photonPlayer)
            return pm;
    }
    return null;
}



private PlayerManager GetPlayerByName(string playerName)
{
    foreach (var playerObj in FindObjectsOfType<PlayerManager>())
    {
        if (playerObj.photonView != null && playerObj.photonView.Owner != null)
        {
            if (playerObj.photonView.Owner.NickName == playerName)
            {
                return playerObj;
            }
        }
    }

    Debug.LogWarning($"Player with name {playerName} not found.");
    return null;
}
    void Start()
    {
		clickSpawner = FindObjectOfType<ClickSpawner>();
        if(clickSpawner == null)


           photonView.RPC("first", RpcTarget.All);
    }
	public void LogPlayersInGame()
{
    Debug.Log("==== Players In Game ====");
    foreach (var p in PlayersInGame)
    {
        Debug.Log($"Player: {p.NickName}, ActorNumber: {p.ActorNumber}");
    }
}
   [PunRPC]
    void AddPlayerToList(int actorNumber)
    {
        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (player != null && !GameManager.Instance.PlayersInGame.Contains(player))
        {
            GameManager.Instance.PlayersInGame.Add(player);

        }
    }
	private void UpdatePlayerNamesInInspector()
    {
        playerNamesInGame.Clear();
        foreach (var player in PlayersInGame)
        {
            playerNamesInGame.Add(player.NickName);
        }
    }

	public void Check()
{
    PlayerManager[] activePlayers = FindObjectsOfType<PlayerManager>();

    if (PhotonNetwork.IsMasterClient && activePlayers.Length == 1)
    {
        photonView.RPC("ProgressF", RpcTarget.All);
    }

    }
	public void UnregisterPosition(PlayerPosition position)
{
    if (AssignedPositions.Contains(position))
    {
        AssignedPositions.Remove(position);
    }


}
public void RegisterPosition(PlayerPosition position)
    {
        AssignedPositions.Add(position);
    }
public bool IsPositionAvailable(PlayerPosition position)
    {
        return !AssignedPositions.Contains(position);
    }
public void ReceivePlayerResult(string playerName, HandRank handRank, List<int> rankValues)
{
    if (winnerDetermined)
    {
        Debug.LogWarning($"[Ignored] Result from {playerName} received after winner was determined.");
        return;
    }

    // Avoid duplicate results
    if (playerResults.ContainsKey(playerName))
    {
        Debug.LogWarning($"[Duplicate] Result from {playerName} already received, ignoring.");
        return;
    }

    // Store the result
    playerResults[playerName] = (handRank, rankValues);

    // ✅ Log the result
    Debug.Log($"[HAND RECEIVED] {playerName}: Rank = {handRank}, RankValues = {string.Join(",", rankValues)}");

    int received = playerResults.Count;
    int expected = GetExpectedPlayerCount();
    Debug.Log($"[Progress] Hand results: {received}/{expected}");

    // ✅ All results are in, determine winner
    if (received == expected)
    {
        Debug.Log("[ALL HANDS RECEIVED] Calling DetermineWinner...");
        SyncDictToList();
        DetermineWinner();
        winnerDetermined = true;
    }
}
private void DetermineWinner()
{
	Debug.Log($"Player Contributions count: {playerContributions.Count}");
foreach (var kvp in playerContributions)
{
    Debug.Log($"Player: {kvp.Key}, Contribution: {kvp.Value}");
}
    pots = CreateSidePotsFromContributions();
	Debug.Log($"Total pots created: {pots.Count}");
foreach(var pot in pots)
{
    Debug.Log($"Pot Amount: {pot.Amount}, Eligible Players: {string.Join(", ", pot.EligiblePlayers)}");
}
SyncPots();
UpdatePotDisplayUI(); 
    var sortedResults = playerResults
        .OrderByDescending(entry => entry.Value.rank)
        .ThenByDescending(entry => entry.Value.rankValues, new LexicographicComparer())
        .ToList();

    Debug.Log("DetermineWinner called.");
    Debug.Log($"Total pots: {pots.Count}");
    Debug.Log("Player results:");
    foreach (var pr in playerResults)
        Debug.Log($"{pr.Key}: Rank={pr.Value.rank}, Kickers={string.Join(",", pr.Value.rankValues)}");

    HashSet<string> awardedPlayers = new();

foreach (var pot in pots)
{
    Debug.Log($"Pot: {pot.Amount}, Eligible: {string.Join(", ", pot.EligiblePlayers)}");

    foreach (var result in sortedResults)
    {
        if (pot.EligiblePlayers.Contains(result.Key) && !awardedPlayers.Contains(result.Key))
        {
            AwardChips(result.Key, pot.Amount);
            awardedPlayers.Add(result.Key);
            break;
        }
    }
}

    playerResults.Clear();
}
void AwardChips(string playerName, long amount)
{
    Debug.Log($"AwardChips called for {playerName} with amount {amount}");

    var player = GetPlayerByName(playerName);
    if (player == null)
    {
        Debug.LogError($"Player {playerName} not found.");
        return;
    }

    player.chipCount += amount;
    Debug.Log($"Player {playerName} now has {player.chipCount} chips");

    player.photonView.RPC("UpdateChipCount", RpcTarget.AllBuffered, player.chipCount);
}
[PunRPC]
    public void AddToPot(long amount)
    {
        potAmount += amount;
        UpdatePotUI(potAmount);
AddChipsToPot(photonView.Owner.NickName, amount);
    }
[PunRPC]
    public void UpdatePotUI(long x)
    {
        if (potText != null)
            potText.text = $"Pot: {x}";
    }
public override void OnPlayerEnteredRoom(Player newPlayer)
    {
	totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;}
	public IEnumerator DelayedRiver()
{
    yield return new WaitForSeconds(2f);
if (flop && turn && !river){

           PhotonView localPhotonView = FindLocalPlayerPhotonView();
        if (localPhotonView != null)
        {
            PlayerManager playerManager = localPhotonView.GetComponent<PlayerManager>();
            if (playerManager != null)
            { River();
    photonView.RPC("rivertrue", RpcTarget.All);
    playerManager.photonView.RPC("DeductChipsRPC", RpcTarget.All);

    photonView.RPC("UpdateCallAmountText", RpcTarget.All);
}}}
StartCoroutine(DelayedDW()); }

[PunRPC]
    private void UpdatePotAmountRPC(long newPotAmount)
    {
        potAmount = newPotAmount;
        potText.text = $"Pot: {potAmount}";
       
    }

	public PhotonView FindLocalPlayerPhotonView()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            PhotonView photonView = player.GetComponent<PhotonView>();
            if (photonView != null && photonView.Owner == PhotonNetwork.LocalPlayer)
            {
                return photonView;
            }
        }

        return null; 
    }
	
	private void River()
	{
				DeckInstance.photonView.RPC("DealRiverCardRPC", RpcTarget.AllViaServer);

                        photonView.RPC("rivertrue", RpcTarget.All);
						StartCoroutine(DelayedDW());
	}
void Awake()
    {
if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate GameManager found, destroying it: " + gameObject.name);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: use only if you load scenes and want to keep GameManager
        Debug.Log("GameManager initialized: " + gameObject.name);
    }
	    private bool IsAnyPlayerInstantiated()
    {
        return GameObject.FindGameObjectsWithTag("Player").Length > 1;
    }
	 [PunRPC]
    private void RestartGameRPC()
    {if(PhotonNetwork.IsMasterClient)
		{
		                StartCoroutine(DeckInstance.DelayedRestart());				
    }
	       
	}
	[PunRPC]
	private void ProgressF()
	{Progress = false;
	}
	[PunRPC]
	private void ProgressTrue()
	{Progress = true;
	}
	[PunRPC]
	public void first()
    {
        FirstTurn = true;
	  }

	[PunRPC]
	public void firstfalse()
    {
        FirstTurn = false;
	  }
	[PunRPC]
	public void floptrue()
    {
        flop = true;
	  }
	  [PunRPC]
	public void turntrue()
    {
        turn = true;
	  }
	  [PunRPC]
	public void rivertrue()
    {
        river = true;
	  }
	  public void Comm()
	{
	photonView.RPC("floptrue", RpcTarget.All);
                        StartCoroutine(DelayedRPC());
                        StartCoroutine(DelayedRPC1());
						
                        photonView.RPC("turntrue", RpcTarget.All);
                        StartCoroutine(DelayedRPC2());
						
                        photonView.RPC("rivertrue", RpcTarget.All);	
				
						StartCoroutine(DelayedDW()); 
                
			   Debug.Log("Comm called");
	}
private PlayerManager FindPlayerByID(string id)
{
    return allPlayers.FirstOrDefault(p => p.playerID == id);
}	
	private IEnumerator DelayedRPC()
{
    yield return new WaitForSeconds(1f);
    DeckInstance.photonView.RPC("DistributeAndAddCommunityCards", RpcTarget.AllViaServer);
}
private IEnumerator DelayedRPC1()
{
    yield return new WaitForSeconds(1.5f);
    DeckInstance.photonView.RPC("DealTurnCardRPC", RpcTarget.AllViaServer);
}
private IEnumerator DelayedRPC2()
{
    yield return new WaitForSeconds(2f);
DeckInstance.photonView.RPC("DealRiverCardRPC", RpcTarget.AllViaServer);

	
}
	
 private IEnumerator DelayedDW()
{
yield return new WaitForSeconds(1f);
	photonView.RPC("ResetTurnStatesForOthers", RpcTarget.AllBuffered); 
	Invoke("Final", 2.5f);
    

}

   [PunRPC]
public void StartSit()
{
  winnerDetermined = false;
    playerResults.Clear();
pots.Clear(); // Remove all existing pots

    // If you have any other pot-related state, reset here
    // For example, clear player contributions if needed:
    playerContributions.Clear();

    Debug.Log("Pots and player contributions reset for new hand.");
    PlayerManager[] playerManagers = FindObjectsOfType<PlayerManager>();
    totalPlayers = playerManagers.Length;

    if (PhotonNetwork.IsMasterClient && !Progress && IsAnyPlayerInstantiated())
    {photonView.RPC("RestartGameRPC", RpcTarget.MasterClient);
        GameObject[] pokerPlayerObjects = GameObject.FindGameObjectsWithTag("Player");
        if (pokerPlayerObjects.Length > 0)
        {foreach (GameObject pokerPlayerObject in pokerPlayerObjects)
            {PhotonView pv = pokerPlayerObject.GetComponent<PhotonView>();
                if (pv != null)
                {pv.RPC("IsPlaying", RpcTarget.AllBuffered, (int)Statue.Playing);
                }
            }
        }
        photonView.RPC("first", RpcTarget.AllBuffered);
        foreach (PlayerManager playerManager in playerManagers)
        {
            playerManager.photonView.RPC("gametrue", RpcTarget.All);
        }

        photonView.RPC("hasEvaluatedf", RpcTarget.All);
        photonView.RPC("Resetstate", RpcTarget.All);
    }
}


	
    
	
    void Update()
    {
                UpdatePlayerNamesInInspector();
		if (Input.GetKeyDown(KeyCode.P)) // for testing
{
    LogPlayersInGame();
}
    }
	
	 [PunRPC]
    private void ResetTurnStatesForOthers()
    {
        playersFinished = 0;
    }
	private void Turn()
	{
		   DeckInstance.photonView.RPC("DealTurnCardRPC", RpcTarget.AllViaServer); 
                        photonView.RPC("turntrue", RpcTarget.All); 
						
	}[PunRPC]
	public void  hasEvaluatedf()
	{
		 hasEvaluated = false;
	}
public void EvaluateHand()
{
 if (!PhotonNetwork.IsMasterClient || hasEvaluated)
        return;

    hasEvaluated = true;

    PlayerManager[] players = FindObjectsOfType<PlayerManager>();
    foreach (PlayerManager playerManager in players)
    {

        playerManager.photonView.RPC("EvaluateHandRPC", RpcTarget.All);   
        playerManager.photonView.RPC("ShareHandWithMaster", RpcTarget.All);
    }
}
[PunRPC]
private void EvalForAll()
	{
if (photonView.IsMine)
    {
        EvaluateHand();
    }

}
public void Rotate()
{
GameObject[] pokerPlayerObjects = GameObject.FindGameObjectsWithTag("Player");
     if (pokerPlayerObjects.Length > 0)
    {

        foreach (GameObject pokerPlayerObject in pokerPlayerObjects)
        {

            PhotonView photonView = pokerPlayerObject.GetComponent<PhotonView>();

            if (photonView != null)
            {
            photonView.RPC("RotatePlayerPositions", RpcTarget.All);
        }
    }
}}
[PunRPC]
private void Reset()
{
	flop = false;
	turn = false;
	river = false;
}
[PunRPC]
private void ResetTurnStatesRaise()
{
	playersFinished--;
}
private void Final()
{


photonView.RPC("EvalForAll", RpcTarget.AllBuffered);
    photonView.RPC("Reset", RpcTarget.AllBuffered);
Debug.Log("test");

    StartCoroutine(ResetRound());
    Rotate();
}
[PunRPC]
	private void ResetAmount()
    {
        potAmount = 0;
        
		photonView.RPC("UpdatePotAmountRPC", RpcTarget.All, potAmount);
    }

	[PunRPC]
public void PlayerFinishedTurn()
{
    Debug.Log($"[PlayerFinishedTurn] Called - playersFinished: {playersFinished}, totalPlayers: {totalPlayers}, flop: {flop}, turn: {turn}, river: {river}");

    if (playersFinished < totalPlayers)
    {
        playersFinished = 0;
    }

    playersFinished++;
    Debug.Log($"[PlayerFinishedTurn] Incremented - playersFinished: {playersFinished}");

    if (playersFinished >= totalPlayers && !flop)
    {
        Debug.Log("[PlayerFinishedTurn] Transitioning to FLOP phase");
        photonView.RPC("firstfalse", RpcTarget.AllBuffered);
        photonView.RPC("floptrue", RpcTarget.All);
        DeckInstance.photonView.RPC("DistributeAndAddCommunityCards", RpcTarget.AllViaServer);
        photonView.RPC("ResetTurnStatesForOthers", RpcTarget.AllBuffered);
    }
    else if (playersFinished >= totalPlayers && flop && !turn)
    {
        Debug.Log("[PlayerFinishedTurn] Transitioning to TURN phase");
        photonView.RPC("ResetTurnStatesForOthers", RpcTarget.AllBuffered);
        DeckInstance.photonView.RPC("DealTurnCardRPC", RpcTarget.AllViaServer);
        photonView.RPC("turntrue", RpcTarget.All);
    }
    else if (playersFinished >= totalPlayers && flop && turn && !river)
    {
        Debug.Log("[PlayerFinishedTurn] Transitioning to RIVER phase");
        photonView.RPC("ResetTurnStatesForOthers", RpcTarget.AllBuffered);
        DeckInstance.photonView.RPC("DealRiverCardRPC", RpcTarget.AllViaServer);
        photonView.RPC("rivertrue", RpcTarget.All);
    }
    else if (playersFinished >= totalPlayers && flop && turn && river)
    {
        Debug.Log("[PlayerFinishedTurn] Final phase - Revealing cards and evaluating");

        foreach (PlayerManager pm in FindObjectsOfType<PlayerManager>())
        {
            pm.photonView.RPC("RevealAllCards", RpcTarget.All);
        }

        photonView.RPC("EvalForAll", RpcTarget.All);

        Invoke("Final", 2.5f);
        Invoke("RestartD", 4f);

        Debug.Log("END! round");
    }
}
[PunRPC]
		public void Resetstate()
{
   
photonView.RPC("Reset", RpcTarget.All);
photonView.RPC("ResetTurnStatesForOthers", RpcTarget.All);}
	private IEnumerator ResetRound()
{
    yield return new WaitForSeconds(5f);
   	    foreach (PlayerManager pm in FindObjectsOfType<PlayerManager>())
    {
        pm.photonView.RPC("Check", RpcTarget.AllViaServer);
    }
	    photonView.RPC("ResetAmount", RpcTarget.AllBuffered);
photonView.RPC("Reset", RpcTarget.All);
photonView.RPC("ResetTurnStatesForOthers", RpcTarget.All);
photonView.RPC("New", RpcTarget.MasterClient);

}
[PunRPC]
	private void New()
	{ photonView.RPC("ProgressF", RpcTarget.All);
	if(PhotonNetwork.IsMasterClient)
		{
	StartSit();
	}
	 }
}
