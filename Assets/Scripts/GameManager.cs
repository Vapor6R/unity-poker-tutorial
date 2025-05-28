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
	
public class GameManager : MonoBehaviourPunCallbacks
{   
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
	private Dictionary<string, (HandRank rank, List<int> rankValues)> playerResults =
    new Dictionary<string, (HandRank, List<int>)>();
public long CallAmount 
{ 
    get => callAmount; 
    set => callAmount = value; 
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
public void ReceivePlayerResult(string playerName, HandRank rank, List<int> rankValues)
{
    playerResults[playerName] = (rank, rankValues);
    handResultsReceived++;

    Debug.Log($"Got result from {playerName}: {rank} ({string.Join(", ", rankValues)})");

    if (handResultsReceived >= totalPlayers)
    {
        DetermineWinner();

		playerResults.Clear();
        handResultsReceived = 0;
    }
}
   private void DetermineWinner()
{
    var winner = playerResults
        .OrderByDescending(entry => entry.Value.rank)
        .ThenByDescending(entry => entry.Value.rankValues, new LexicographicComparer())
        .First();

    string winnerName = winner.Key;
    var (rank, rankValues) = winner.Value;

    Debug.Log($"🏆 Winner: {winnerName} | Rank: {rank} | Kickers: {string.Join(", ", rankValues)}");
	    AwardPotToWinner(winner.Key);
    playerResults.Clear();
}

    private void AwardPotToWinner(string winnerName)
    {
        
        PlayerManager winnerPM = FindObjectsOfType<PlayerManager>()
            .FirstOrDefault(pm => pm.photonView.Owner.NickName == winnerName);

        if (winnerPM != null)
        {
            winnerPM.AddChips(potAmount);

            Debug.Log($"{winnerName} awarded {potAmount} chips!");
            potAmount = 0;
        }
        else
        {
            Debug.LogError("Winner player not found to award pot.");
        }

    }
[PunRPC]
    public void AddToPot(long amount)
    {
        potAmount += amount;
        UpdatePotUI(potAmount);
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
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
