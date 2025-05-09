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
public enum PokerSeat
{
    Dealer,
    UTG,
    UTG1,  // Using UTG1 instead of "UTG+1" since "+" is invalid in enum names
    Seat4,
    Seat5,
    Seat6,
    Seat7,
    Seat8,
    Seat9
}
public enum Statue
    {
        None = -1,
        Waiting = 0,    // Dealer position
        Playing = 1,       // Under the Gun (first player to act after the blinds)
        Checked = 2, // UTG+1 (second player to act)
        Raise = 3, // 3rd player to act
        Folded = 4, // 4th player to act
        AllIn = 5, // 5th player to act
    }
public class GameManager : MonoBehaviourPunCallbacks
{
	private Dictionary<int, ClickSpawner> spawnersBySeat = new Dictionary<int, ClickSpawner>();
	public bool FirstTurn = true;
public TMP_Text potText;
    public const long SMALL_BLIND_AMOUNT = 100000000000;
    public const long BIG_BLIND_AMOUNT = 200000000000;
private bool turn = false;
	    private bool flop = false;
    private bool river = false;
	private int playersFinished = 0;
	public long currentRaiseAmount = 0;
public long CurrentBet = 0;
public long globalRaise = 0; 
	public long pot = 0;
	 private int totalPlayers;
	    public Dictionary<int, PokerSeat> playerSeats = new Dictionary<int, PokerSeat>();
	public Deck DeckInstance;
    public GameObject UIPanel;  // The single UIPanel for all players
public List<Player> PlayersInGame = new List<Player>();
[Header("Debug - View in Inspector")]
    [SerializeField] private List<string> playerNamesInGame = new List<string>();
public Statue statue;
    [System.Serializable]
    public class PlayerSeatEntry
    {
        public int actorNumber;
        public PokerSeat seat;
		public int statue;
    }
public bool Progress = false;
    public List<PlayerSeatEntry> playerSeatList = new List<PlayerSeatEntry>();
    public static GameManager Instance { get; private set; }
public PlayerPosition playerPosition;
public Statue[] StatueNow = new Statue[]
{
    Statue.Waiting,
    Statue.Playing,
    Statue.Checked,
    Statue.Raise,
    Statue.Folded,
    Statue.AllIn,
};
    [System.Serializable]
    public class PlayerPosition
    {
        public string playerName;
        public string position;
        public string role;
    }
	private PokerSeat[] seatOrder = new PokerSeat[]
{
    PokerSeat.Dealer,
    PokerSeat.UTG,
    PokerSeat.UTG1,
    PokerSeat.Seat4,
    PokerSeat.Seat5,
    PokerSeat.Seat6,
    PokerSeat.Seat7,
    PokerSeat.Seat8,
    PokerSeat.Seat9
};
[Header("Debug Only")]
    [SerializeField] private long currentBetInspectorView;
[PunRPC]
public void CurrentBetSync(long syncedBet)
{
currentRaiseAmount = syncedBet;
Debug.Log($"currentRaiseAmount synced to: {syncedBet}");
// Optionally update pot display or trigger player UI refresh
}

[PunRPC]
public void ResetBetsAfterFlop()
{
    CurrentBet = 0;

    // Reset each player's current bet for this round
    foreach (var player in FindObjectsOfType<PlayerManager>())
    {
        player.PlayersBet = 0;
    }
}
public void Check()
{
    PlayerManager[] activePlayers = FindObjectsOfType<PlayerManager>();

    if (PhotonNetwork.IsMasterClient && activePlayers.Length == 1)
    {
        photonView.RPC("ProgressF", RpcTarget.All);
    }

    photonView.RPC("ProgressF", RpcTarget.All); }


public override void OnMasterClientSwitched(Player newMasterClient)
{
    Debug.Log($"New Master Client is: {newMasterClient.NickName}");
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
 [PunRPC]
    private void ResetTurnStatesForOthers()
    {
        playersFinished = 0;
        // Handle any additional state reset logic for other players
    }
[PunRPC]
public void UpdatePotUI(long pot)
{
    GameManager.Instance.pot = pot;
    GameManager.Instance.potText.text = $"Pot: {FormatChipsWithSuffix(pot)}";
}



[PunRPC]
public void AddToPotRPC(long amount, string senderName)
{
pot += amount;
Debug.Log($"Pot increased by {amount} from {senderName}. Total pot: {pot}");
photonView.RPC("UpdatePotUI", RpcTarget.AllBuffered, pot);
}
[PunRPC]
private void Reset()
{
	flop = false;
	turn = false;
	river = false;
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
	   [PunRPC]
	private void ResetAmount()
    {
        currentRaiseAmount = 0;
        CurrentBet = 0;
		photonView.RPC("UpdateCallAmountText", RpcTarget.All, CurrentBet);
    }
	[PunRPC]
private void UpdateCallAmountText(long CurrentBet)
{
	foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
{
    if (player.TagObject is GameObject obj && obj.TryGetComponent(out PlayerManager pm))
    {
        pm.photonView.RPC("SyncCallAmount", player,CurrentBet );
    }
}
}
[PunRPC]
    public void PlayerFinishedTurn()
    {
        playersFinished++;
        if (playersFinished >= totalPlayers && !flop)
        {photonView.RPC("firstfalse", RpcTarget.AllBuffered);
            photonView.RPC("floptrue", RpcTarget.All);
            DeckInstance.photonView.RPC("DistributeAndAddCommunityCards", RpcTarget.AllViaServer);
            photonView.RPC("ResetTurnStatesForOthers", RpcTarget.All);
            photonView.RPC("ResetAmount", RpcTarget.All);
			photonView.RPC("ResetBetsAfterFlop", RpcTarget.All);
        }
        else if (playersFinished >= totalPlayers && flop && !turn)
        {
            photonView.RPC("ResetTurnStatesForOthers", RpcTarget.All);
            DeckInstance.photonView.RPC("DealTurnCardRPC", RpcTarget.AllViaServer);
            photonView.RPC("turntrue", RpcTarget.All);
            photonView.RPC("ResetAmount", RpcTarget.All);
        }
        else if (playersFinished >= totalPlayers && flop && turn && !river)
        {
            photonView.RPC("ResetTurnStatesForOthers", RpcTarget.All);
            DeckInstance.photonView.RPC("DealRiverCardRPC", RpcTarget.AllViaServer);
			 Invoke("A", 0.5f); 
			
            photonView.RPC("rivertrue", RpcTarget.All);
			
        }
        else if (playersFinished >= totalPlayers && flop && turn && river)
        {  
			Invoke("Final", 2.5f);
			 Invoke("RestartD", 4f);
Debug.Log("?");
        }
    }
	
	
	public void EvaluateHand()
{
        foreach (PlayerManager pm in FindObjectsOfType<PlayerManager>())
    {
    pm.photonView.RPC("EvaluateHand", RpcTarget.All);
    }

 
}
private IEnumerator WaitAndDetermineWinner()
{
    yield return new WaitForSeconds(1f);
    DetermineWinner(); // Now it's in the same script
}
private IEnumerator ResetRound()
{
    yield return new WaitForSeconds(2f);
   
photonView.RPC("Reset", RpcTarget.All);
photonView.RPC("ResetTurnStatesForOthers", RpcTarget.All);
photonView.RPC("New", RpcTarget.MasterClient);

}

public void DetermineWinner()
{
    PlayerManager[] allPlayers = FindObjectsOfType<PlayerManager>();

    PlayerManager bestPlayer = null;
    HandRank bestRank = HandRank.HighCard;

    foreach (PlayerManager pm in allPlayers)
    {
        Debug.Log($"Player {pm.photonView.Owner.NickName} has {pm.bestHandRank}");

        if (bestPlayer == null || pm.bestHandRank > bestRank)
        {
            bestPlayer = pm;
            bestRank = pm.bestHandRank;
			pm.photonView.RPC("ResetHand", RpcTarget.All);
        }
    }

    if (bestPlayer != null)
    {
        Debug.Log($"🏆 Winner: {bestPlayer.photonView.Owner.NickName} with {bestRank}");

        // Award pot to winner
        bestPlayer.photonView.RPC("ReceiveWinnings", bestPlayer.photonView.Owner, pot);

        // Reset pot for next round
        pot = 0;
		 photonView.RPC("UpdatePotUI", RpcTarget.AllBuffered,pot);
    }
}
[PunRPC]
private void A()
	{A2();
    

}

private void A2()
	{
if (photonView.IsMine)
    {
        EvaluateHand(); // Local player evaluates their own hand
    }

}
	private void Final()
	{
	   StartCoroutine(WaitAndDetermineWinner());
            photonView.RPC("ResetTurnStatesForOthers", RpcTarget.All);

            photonView.RPC("Reset", RpcTarget.All);
            photonView.RPC("ResetAmount", RpcTarget.All);
            
			photonView.RPC("lastf", RpcTarget.All);
			StartCoroutine(ResetRound());
			
			Rotate();
			}
private void UpdatePlayerNamesInInspector()
    {
        playerNamesInGame.Clear();
        foreach (var player in PlayersInGame)
        {
            playerNamesInGame.Add(player.NickName);
        }
    }
	private void Update()
    {currentBetInspectorView = CurrentBet;
        UpdatePlayerNamesInInspector();
		if (Input.GetKeyDown(KeyCode.P)) // for testing
{
    LogPlayersInGame();
}
    }
	[PunRPC]
    public void RotatePlayerPositions()
{
    // Create a new dictionary to hold updated seat assignments
    Dictionary<int, PokerSeat> newSeatAssignments = new Dictionary<int, PokerSeat>();

    foreach (var kvp in playerSeats)
    {
        int actorNumber = kvp.Key;
        PokerSeat currentSeat = kvp.Value;

        // Find index of the current seat in the order
        int currentIndex = Array.IndexOf(seatOrder, currentSeat);
        int nextIndex = (currentIndex + 1) % seatOrder.Length;  // Wrap around

        PokerSeat newSeat = seatOrder[nextIndex];
        newSeatAssignments[actorNumber] = newSeat;

        Debug.Log($"Player {actorNumber} moves from {currentSeat} to {newSeat}");
    }

    // Apply updated seat assignments
    playerSeats = newSeatAssignments;

    // Optionally update playerSeatList if you're using that as well
    playerSeatList.Clear();
    foreach (var kvp in playerSeats)
    {
        playerSeatList.Add(new PlayerSeatEntry
        {
            actorNumber = kvp.Key,
            seat = kvp.Value,
			statue = (int)Statue.Waiting
        });
    }
}

public void AssignPlayerSeats()
{
    playerSeats.Clear();

    for (int i = 0; i < PlayersInGame.Count && i < seatOrder.Length; i++)
    {
        Player player = PlayersInGame[i];
        PokerSeat seat = seatOrder[i];

        playerSeats[player.ActorNumber] = seat;
    }
}
   [PunRPC]
    void AddPlayerToList(int actorNumber)
    {
        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (player != null && !GameManager.Instance.PlayersInGame.Contains(player))
        {
            GameManager.Instance.PlayersInGame.Add(player);
            Debug.Log($"Player {player.NickName} added to PlayersInGame.");
        }
    }

    public int PlayerCount => PlayersInGame.Count;
    void Start()
    { ClickSpawner[] spawners = FindObjectsOfType<ClickSpawner>();
        foreach (ClickSpawner spawner in spawners)
        {
            spawnersBySeat[spawner.seatNumber] = spawner;
        }

        Debug.Log("Stored " + spawnersBySeat.Count + " ClickSpawner(s) in the dictionary.");
        if (PhotonNetwork.IsMasterClient)
        {
            // Master client setup (if needed)
        }
    }
	public ClickSpawner GetClickSpawnerBySeat(int seatNumber)
    {
        if (spawnersBySeat.ContainsKey(seatNumber))
        {
            return spawnersBySeat[seatNumber];
        }
        return null;  // If not found, return null
    }
public override void OnPlayerEnteredRoom(Player newPlayer)
    {
	totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;}
public override void OnPlayerLeftRoom(Player otherPlayer)
{
    Debug.Log($"Player left: {otherPlayer.NickName}");

    // Count how many PlayerManager instances are still in the scene
    PlayerManager[] activePlayers = FindObjectsOfType<PlayerManager>();

    if (PhotonNetwork.IsMasterClient && activePlayers.Length == 1)
    {
        photonView.RPC("ProgressF", RpcTarget.All);
    }
}



public void LogPlayersInGame()
{
    Debug.Log("==== Players In Game ====");
    foreach (var p in PlayersInGame)
    {
        Debug.Log($"Player: {p.NickName}, ActorNumber: {p.ActorNumber}");
    }
}

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    [PunRPC]
    public void StartSit()
    {
        if (!Progress && IsAnyPlayerInstantiated())
        {
photonView.RPC("RestartGameRPC", RpcTarget.MasterClient);
            photonView.RPC("ProgressTrue", RpcTarget.AllBuffered);
			GameObject[] pokerPlayerObjects = GameObject.FindGameObjectsWithTag("Player");
     if (pokerPlayerObjects.Length > 0)
    {
        foreach (GameObject pokerPlayerObject in pokerPlayerObjects)
        {
            PhotonView photonView = pokerPlayerObject.GetComponent<PhotonView>();

            if (photonView != null)
            {
				photonView.RPC("IsPlaying", RpcTarget.AllBuffered, (int)Statue.Playing);

			}
        }
    }   }   
	foreach (PlayerManager player in FindObjectsOfType<PlayerManager>())
    {
        player.CheckAndStandUpIfBroke();
    }}
	[PunRPC]
	private void New()
	{ photonView.RPC("ProgressF", RpcTarget.All);
	if(PhotonNetwork.IsMasterClient)
		{
	StartSit();
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
    private void RestartGameRPC()
    {if(PhotonNetwork.IsMasterClient)
		{
		                StartCoroutine(DeckInstance.DelayedRestart());				
    }
	       
	}
    private bool IsAnyPlayerInstantiated()
    {
        return GameObject.FindGameObjectsWithTag("Player").Length > 1;
    }
	
	
 
public void StartNewRound()
{
    photonView.RPC("StartTurnUT", RpcTarget.MasterClient);

}
public void Rotate()
{
GameObject[] pokerPlayerObjects = GameObject.FindGameObjectsWithTag("Player");
     if (pokerPlayerObjects.Length > 0)
    {
        // Loop through each GameObject in the array
        foreach (GameObject pokerPlayerObject in pokerPlayerObjects)
        {
            // Get the PhotonView component from the GameObject
            PhotonView photonView = pokerPlayerObject.GetComponent<PhotonView>();

            if (photonView != null)
            {
            photonView.RPC("RotatePlayerPositions", RpcTarget.All);
        }
    }
}}

}
