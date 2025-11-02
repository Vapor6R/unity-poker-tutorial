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
public enum GameState
{
    Waiting,
    Preflop,
    Flop,
    Turn,
    River,
    Showdown,
	Newround
}

public class GameManager : MonoBehaviourPunCallbacks {
	public int inGameCount = 0;
public bool roundInProgress = false;
	public int playersFinished = 0;
	private bool blindsPostedThisRound = false;
	public bool NewState = false;
		public bool winnerDetermined = false;
	public GameState currentState = GameState.Waiting;
public Deck DeckInstance;
  public SpawnButtonManager Spawn;
    public static GameManager Instance;
	public long Small = 5;
public static List<PlayerManager> activePlayers = new List<PlayerManager>();
	public long Big = 10;
	public bool FirstTurn = true;
	 public long callAmount = 0;
	 public bool Finished = false;
	 public bool RoundInProgress = false; 
	   public bool turn = false;
  public bool flop = false;
  public bool river = false;
	 public bool ShowDown = false;

	  public bool blindtrue=false;
	 public void CallMasterClientCheck()
{
    int masterActorNumber = PhotonNetwork.MasterClient.ActorNumber;

    PlayerManager targetPlayer = FindObjectsOfType<PlayerManager>()
        .FirstOrDefault(p => p != null 
            && p.photonView != null 
            && p.photonView.OwnerActorNr == masterActorNumber);

    if (targetPlayer == null)
    {
        Debug.LogWarning("⚠️ Could not find MasterClient's PlayerManager!");
        return;
    }

    Debug.Log($"✅ Found MasterClient PlayerManager: {targetPlayer.PlayerName}");
    
    // Execute function only once, directly for MasterClient
    if (PhotonNetwork.IsMasterClient)
    {
        targetPlayer.CheckPlayerCountAndAssignRoles();
    }
    else
    {
        targetPlayer.photonView.RPC("CheckPlayerCountAndAssignRoles", RpcTarget.MasterClient);
    }
}
public int ActivePlayerCount()
{
    return activePlayers.Count;
}public void OnShowdown()
{
    if (!PhotonNetwork.IsMasterClient)
    {
        Debug.Log("⏩ Skipping OnShowdown() – only MasterClient handles showdown logic.");
        return;
    }

    if (PotManager.Instance == null)
    {
        PotManager.Instance = FindObjectOfType<PotManager>();
        if (PotManager.Instance == null)
        {
            Debug.LogError("❌ PotManager reference missing – cannot process showdown!");
            return;
        }
    }

    PotManager.Instance.CalculateSidePots();
StartCoroutine(NextStep());
}
[PunRPC]
private void Progress(){
roundInProgress = true;}

[PunRPC]
private void ProgressF(){
roundInProgress = false;}
public IEnumerator NextStep()
{
    yield return new WaitForSeconds(1.9f);
    
    // ✅ Step 1 – Evaluate all hands and prepare best cards
    foreach (var p in FindObjectsOfType<PlayerManager>())
    {
        if (p != null && p.InGame && !p.isFolded && p.playerHand.Count == 7)
        {
            p.EvaluateMyHand(); // Force evaluation
            
            // Ensure we have 5 best cards
            if (p.currentBestCards == null || p.currentBestCards.Count != 5)
            {
                Debug.LogWarning($"⚠️ {p.PlayerName} doesn't have 5 best cards after evaluation!");
                continue;
            }
            
            // Sort the best cards (highest to lowest)
            p.currentBestCards = p.currentBestCards
                .OrderByDescending(c => (int)c.rank)
                .ToList();
        }
    }
    
    // ✅ Step 2 – Get all valid players for showdown
    List<PlayerManager> validPlayers = FindObjectsOfType<PlayerManager>()
        .Where(p => p != null 
            && p.InGame 
            && !p.isFolded 
            && p.currentBestCards != null 
            && p.currentBestCards.Count == 5)
        .ToList();
    
    if (validPlayers.Count == 0)
    {
        Debug.LogError("❌ No valid players for showdown!");
        yield break;
    }
    
    // ✅ Step 3 – Sort players using custom comparison
    List<PlayerManager> showdownOrder = validPlayers
        .OrderByDescending(p => p, new HandComparer())
        .ToList();
    
    Debug.Log("==== 🃏 SHOWDOWN ORDER ====");
    for (int i = 0; i < showdownOrder.Count; i++)
    {
        var player = showdownOrder[i];
        Debug.Log($"#{i + 1} → {player.PlayerName} | Hand: {player.currentHandRank} | Best Cards: {string.Join(", ", player.currentBestCards.Select(c => $"{c.rank}{c.suit}"))}");
    }
    
    // ✅ Convert players to names for AwardPots()
    List<string> playerNames = showdownOrder.Select(p => p.PlayerName).ToList();
    yield return StartCoroutine(PotManager.Instance.AwardPots(playerNames));
}

// ✅ Custom comparer class for proper hand comparison

void Update()
{
    inGameCount = FindObjectsOfType<PlayerManager>()
        .Count(p => p != null && p.InGame);

    // Debug display
    Debug.Log($"Players InGame: {inGameCount}");
}
[PunRPC]
public void BroadcastCallAmount(long newCallAmount)
{
    Debug.Log($"📣 [BroadcastCallAmount] Broadcasting call amount: {newCallAmount}");
    
    callAmount = newCallAmount;  // ✅ Update GameManager's callAmount
    
    foreach (var player in FindObjectsOfType<PlayerManager>())
    {
        if (player == null)
        {
            continue;
        }

        if (player.isFolded)
        {
            Debug.Log($"⏭️  Skipping {player.PlayerName} - folded");
            continue;
        }

        if (!player.InGame)
        {
            Debug.Log($"⏭️  Skipping {player.PlayerName} - not in game");
            continue;
        }

        long amountToCall = newCallAmount;
        if (amountToCall < 0)
        {
            amountToCall = 0;
        }
        
        player.callAmount = amountToCall;
        player.UpdateCallAmountUI(amountToCall);
        
        Debug.Log($"  ✅ {player.PlayerName}: callAmount = {amountToCall}");
    }
}
[PunRPC]
private void RestartGame()
{
    

		photonView.RPC("Reset", RpcTarget.AllBuffered);
        DeckInstance.photonView.RPC("RPC_ClearCommunityAndDeck", RpcTarget.MasterClient);
photonView.RPC("SetGameState", RpcTarget.MasterClient, GameState.Newround);
  if (!DeckInstance.roundResetInProgress)
        {
            DeckInstance.photonView.RPC("ResetInProgressT", RpcTarget.AllBuffered);
	
}
}

[PunRPC]
public void CheckAndResetIfSinglePlayer()
{
    if (inGameCount <= 1)
    {
PotManager.Instance.TotalPot = 0;
        photonView.RPC("Reset", RpcTarget.AllBuffered);
        photonView.RPC("ResetTurnStatesForOthers", RpcTarget.AllBuffered);
photonView.RPC("RoundInProgressF", RpcTarget.AllBuffered);
 photonView.RPC("ProgressF", RpcTarget.AllBuffered);
photonView.RPC("SetGameState", RpcTarget.MasterClient, GameState.Waiting);

    }
    else
    {
        Debug.Log($"👥 {activePlayers.Count} active players remain — no reset needed.");
    }
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
public void PlayerFinishedTurn()
{
    Debug.Log($"[PlayerFinishedTurn] Called - playersFinished: {playersFinished}, activePlayers.Count: {ActivePlayerCount()}, flop: {flop}, turn: {turn}, river: {river}");

    playersFinished++;
	NewState = false;
    Debug.Log($"[PlayerFinishedTurn] Incremented - playersFinished: {playersFinished}");
 List<PlayerManager> activeP = FindObjectsOfType<PlayerManager>()
        .Where(p => p != null
            && p.InGame
            && !p.isFolded
            && p.seatIndex >= 0
            && p.chipCount > 0
            && p.statue != Statue.AllIn
            && p.statue != Statue.Folded)
        .OrderBy(p => p.seatIndex)
        .ToList();

    if (playersFinished >= inGameCount && !flop && activeP.Count <=1)
    {StartCoroutine(WaitAndShow());
photonView.RPC("ResetTurnStatesForOthers", RpcTarget.AllBuffered);
 foreach (var pm in FindObjectsOfType<PlayerManager>())
if (pm != null){
 pm.photonView.RPC("RPC_SetActedF", RpcTarget.AllBuffered);}
return;
	}
	else if(playersFinished >= inGameCount && !flop){
        Debug.Log("[PlayerFinishedTurn] Transitioning to FLOP phase");
        photonView.RPC("firstfalse", RpcTarget.AllBuffered);
        photonView.RPC("floptrue", RpcTarget.AllBuffered);
SetGameState(GameState.Flop);
 foreach (var pm in FindObjectsOfType<PlayerManager>())
if (pm != null){
 pm.photonView.RPC("RPC_SetActedF", RpcTarget.AllBuffered);}
        photonView.RPC("ResetTurnStatesForOthers", RpcTarget.AllBuffered);
    }
    else if (playersFinished >= inGameCount && flop && !turn)
    {
		
 foreach (var pm in FindObjectsOfType<PlayerManager>())
if (pm != null){
 pm.photonView.RPC("RPC_SetActedF", RpcTarget.AllBuffered);}
        Debug.Log("[PlayerFinishedTurn] Transitioning to TURN phase");
        photonView.RPC("ResetTurnStatesForOthers", RpcTarget.AllBuffered);
        SetGameState(GameState.Turn);
        photonView.RPC("turntrue", RpcTarget.AllBuffered);
    }
    else if (playersFinished >= inGameCount && flop && turn && !river)
    {
	 foreach (var pm in FindObjectsOfType<PlayerManager>())
if (pm != null){
 pm.photonView.RPC("RPC_SetActedF", RpcTarget.AllBuffered);}
        Debug.Log("[PlayerFinishedTurn] Transitioning to RIVER phase");
        photonView.RPC("ResetTurnStatesForOthers", RpcTarget.AllBuffered);
        SetGameState(GameState.River);
        photonView.RPC("rivertrue", RpcTarget.AllBuffered);
    }
    else if (playersFinished >= inGameCount && flop && turn && river &&!Finished)
    {
SetGameState(GameState.Showdown);
ShowDown=true;
         foreach (var pm in FindObjectsOfType<PlayerManager>())
if (pm != null){
 pm.photonView.RPC("RPC_SetActedF", RpcTarget.AllBuffered);}
        Debug.Log("END! round");
    }    }
	[PunRPC]
private void ResetTurnStatesForOthers()
{
    playersFinished = 0;

}
[PunRPC]
private void Reset()
{ 
    PotManager.Instance.TotalPot = 0L;
	PotManager.Instance.photonView.RPC("UpdatePotUI", RpcTarget.AllBuffered, PotManager.Instance.TotalPot);
    PotManager.Instance.playerContributions.Clear();
photonView.RPC("ResetTurnStatesForOthers", RpcTarget.AllBuffered);
    callAmount = 0;
    flop = false;
    turn = false;
    river = false;
	 photonView.RPC("BlindF", RpcTarget.AllBuffered);
	  photonView.RPC("FinishedF", RpcTarget.AllBuffered);
    playersFinished = 0;
    winnerDetermined = false;
    
    foreach (var pm in FindObjectsOfType<PlayerManager>())
    {
        if (pm != null && pm.chipCount > 0)
        {
			pm.isSMALL=false;
            pm.isFolded = false;
            pm.InGame = true;
            pm.Acted = false;
        }
    }

}
	
[PunRPC]
public void SetGameState(GameState newState)
{
    if (!PhotonNetwork.IsMasterClient)
        return;

    Debug.Log($"🎯 [SetGameState] Changing: {currentState} ➜ {newState}");
    currentState = newState;

    photonView.RPC("RPC_SyncGameState", RpcTarget.AllBuffered, (int)newState);

    switch (newState)
    {
        case GameState.Waiting:
            Debug.Log("⏳ Waiting for players...");

photonView.RPC("Reset", RpcTarget.AllBuffered);

		if(!DeckInstance.DeckSpawned && inGameCount>1 &&!RoundInProgress){
		CallMasterClientCheck();}

            break;

case GameState.Preflop:
   

if (!PhotonNetwork.IsMasterClient)
    return;
   if (activePlayers.Count < 2)
    {
        return;
    }
    RoundInProgress = true;
	NewState = true;
    Finished = false;
    winnerDetermined = false;
 photonView.RPC("RoundInProgressT", RpcTarget.AllBuffered);
Deck.Instance.photonView.RPC("isGame", RpcTarget.AllBuffered);
PostBlinds();
    
StartCoroutine(DeckInstance.Next());
    break;
        case GameState.Flop:
            Debug.Log("🌼 Flop phase started – dealing community cards.");
            DeckInstance.photonView.RPC("DistributeAndAddCommunityCards", RpcTarget.AllViaServer);
			 photonView.RPC("FirstFalse", RpcTarget.AllViaServer);
            NewState = true;
            break;

        case GameState.Turn:
            Debug.Log("💫 Turn phase started – dealing next card.");
            DeckInstance.photonView.RPC("DealTurnCardRPC", RpcTarget.AllViaServer);
            NewState = true;
            break;

        case GameState.River:
            Debug.Log("🌊 River phase started – dealing final card.");
            DeckInstance.photonView.RPC("DealTurnCardRPC", RpcTarget.AllViaServer);
            NewState = true;
            break;

        case GameState.Showdown:
            Debug.Log("🏆 Showdown – revealing hands!");
            foreach (PlayerManager pm in FindObjectsOfType<PlayerManager>())
            {
                if (pm != null && pm.photonView != null)
                    pm.photonView.RPC("RevealLocalHand", RpcTarget.AllBuffered);
            }
            if (PhotonNetwork.IsMasterClient)
            {
                OnShowdown();
				
            }
            break;
			
			case GameState.Newround:
			Debug.Log("🏆 Newround preflop – revealing hands!");
           RoundInProgress = true;
	
	NewState = true;
    Finished = false;
    winnerDetermined = false;
 photonView.RPC("RoundInProgressT", RpcTarget.AllBuffered);
 Deck.Instance.photonView.RPC("isGame", RpcTarget.AllBuffered);
            break;
    }
}

[PunRPC]
public void RPC_ApplySeatMapping(string[] playerNames, int[] newSeats, PhotonMessageInfo info)
{
    Debug.Log($"[RPC_ApplySeatMapping] Received mapping count={playerNames?.Length ?? 0}");

    if (playerNames == null || newSeats == null || playerNames.Length != newSeats.Length)
    {
        Debug.LogError("[RPC_ApplySeatMapping] Invalid mapping payload");
        return;
    }

    for (int i = 0; i < playerNames.Length; i++)
    {
        string name = playerNames[i];
        int seat = newSeats[i];

        // Find the local PlayerManager for that name
        PlayerManager pm = FindObjectsOfType<PlayerManager>().FirstOrDefault(p => p != null && p.PlayerName == name);

        if (pm == null)
        {
            Debug.LogWarning($"[RPC_ApplySeatMapping] Could not find PlayerManager for '{name}' on this client");
            continue;
        }

        // Set local seatIndex and optionally call RPC_SetSeatIndex locally
        pm.seatIndex = seat;
        Debug.Log($"[RPC_ApplySeatMapping] {name} -> seat {seat} (applied local)");

        // If you also want to notify other systems via the existing player-level RPC:
        // (call locally - it will broadcast again if you use RPC there; avoid double-broadcasting)
        pm.photonView.RPC("RPC_SetSeatIndex", RpcTarget.AllBuffered, seat);
    }
}

[PunRPC]
private void RoundInProgressF()
{
    RoundInProgress = false;
}

[PunRPC]
private void BlindF()
{
    blindtrue = false;
}
public void DelayedSetGameState(GameState nextState, float delay)
{
    StartCoroutine(DelayedGameStateRoutine(nextState, delay));
}
[PunRPC]
public void RPC_PassTurnToNextBySeat(int currentSeat)
{
    if (!PhotonNetwork.IsMasterClient)
        return;
    
    // ✅ Gather all active players who can TAKE ACTION
    List<PlayerManager> activeP = FindObjectsOfType<PlayerManager>()
        .Where(p => p != null
            && p.InGame
            && !p.isFolded
            && p.seatIndex >= 0
            && p.chipCount > 0
            && p.statue != Statue.AllIn
            && p.statue != Statue.Folded
            && p.IsPlaying
			&& !p.Acted)
        .OrderBy(p => p.seatIndex)
        .ToList();
    
    if (activeP.Count == 0)
    {
        photonView.RPC("ResetTurnStatesForOthers", RpcTarget.AllBuffered);
        StartCoroutine(WaitAndShow());
        return;
    }
    else if (activeP.Count == 1)
    {
        // ✅ Count players who are NOT folded (includes all-in players)
        int nonFoldedCount = FindObjectsOfType<PlayerManager>()
            .Count(pm => pm != null && pm.InGame && pm.IsPlaying && !pm.isFolded);
        
        Debug.Log($"Active players who can act = 1, Total non-folded = {nonFoldedCount}");
        
        // ✅ If there's only 1 non-folded player total (everyone else folded)
        if (nonFoldedCount == 1)
        {
            PotManager.Instance.CalculateSidePots();
            StartCoroutine(
                PotManager.Instance.AwardPots(
                    activeP.Select(p => p.PlayerName).ToList()
                )
            );
            return;
        }
        
        // ✅ If there are 2+ non-folded players but only 1 can act (heads-up, one all-in)
        // Give the remaining player their turn to call/fold
        PlayerManager lastActivePlayer = activeP[0];
        
        // Check if this player has already acted this round
        if (lastActivePlayer.seatIndex == currentSeat)
        {
            // They've already acted, move to showdown
            Debug.Log("✅ Last active player already acted. Moving to showdown.");
            photonView.RPC("ResetTurnStatesForOthers", RpcTarget.AllBuffered);
            StartCoroutine(WaitAndShow());
            return;
        }
        
        // ✅ Give them their turn
        Debug.Log($"✅ Heads-up with all-in: giving turn to {lastActivePlayer.PlayerName}");
        lastActivePlayer.photonView.RPC("RPC_SetTurn", RpcTarget.All, lastActivePlayer.seatIndex);
        return;
    }
    
    Debug.Log($"Active players: {string.Join(", ", activeP.Select(p => $"{p.PlayerName}(seat{p.seatIndex})"))}");
    
    // ✅ Find current player index INSIDE activeP
    int currentIndex = -1;
    for (int i = 0; i < activeP.Count; i++)
    {
        if (activeP[i].seatIndex == currentSeat)
        {
            currentIndex = i;
            break;
        }
    }
    
    // ✅ If current seat not found in active list, find the next seat in order
    if (currentIndex == -1)
    {
        Debug.LogWarning($"⚠️ Seat {currentSeat} not in active list (likely went all-in). Finding next available seat...");
        
        // Find the first active player with a seat index GREATER than currentSeat
        currentIndex = activeP.FindIndex(p => p.seatIndex > currentSeat);
        
        // If no one after currentSeat, wrap around to the first player
        if (currentIndex == -1)
        {
            currentIndex = 0;
        }
        else
        {
            // We found the next player, so don't increment again
            currentIndex--;
        }
    }
    
    // ✅ Get next index
    int nextIndex = (currentIndex + 1) % activeP.Count;
    
    // ✅ Get the next player
    PlayerManager nextPlayer = activeP[nextIndex];
    
    if (nextPlayer.seatIndex == currentSeat)
    {
        Debug.Log(
            $"✅ Turn would return to same seat ({currentSeat}). " +
            $"Ending betting round."
        );
        photonView.RPC("ResetTurnStatesForOthers", RpcTarget.AllBuffered);
        if (nextPlayer.UI != null)
            nextPlayer.UI.SetActive(false);
        StartCoroutine(WaitAndShow());
        return;
    }
    
    Debug.Log($"✅ Passing turn → {nextPlayer.PlayerName} (seat {nextPlayer.seatIndex})");
    
    // ✅ Tell *all clients* that this player now has the turn
    nextPlayer.photonView.RPC("RPC_SetTurn", RpcTarget.All, nextPlayer.seatIndex);
}
[PunRPC]
private void FinishedF()
{
	Finished = false;
}

public IEnumerator WaitAndShow()
{yield return new WaitForSeconds(0.1f);
	if (!flop &&!Finished)
        {
			Finished = true;
			foreach (PlayerManager pm in FindObjectsOfType<PlayerManager>())
            {
                if (pm != null && pm.photonView != null)
                    pm.photonView.RPC("RevealLocalHand", pm.photonView.Owner);
				
				Debug.Log("revela?");
            }
            DelayedSetGameState(GameState.Flop, 0.1f);
			photonView.RPC("floptrue", RpcTarget.All);
			DelayedSetGameState(GameState.Turn, 0.5f);
			photonView.RPC("turntrue", RpcTarget.All);
			DelayedSetGameState(GameState.River, 1f);
			photonView.RPC("rivertrue", RpcTarget.All);
			DelayedSetGameState(GameState.Showdown, 1.5f);
			 foreach (var pm in FindObjectsOfType<PlayerManager>())
        {
            if (pm != null && pm.photonView != null)
            {
 pm.photonView.RPC("ActedFalse", RpcTarget.AllBuffered);
            }
        }

				Debug.Log("revela?"); yield break;
            }
           
        
        else if (!GameManager.Instance.turn)
        {
           DelayedSetGameState(GameState.Turn, 0.0f);
            photonView.RPC("turntrue", RpcTarget.All);
            DelayedSetGameState(GameState.River, 0.5f);
            photonView.RPC("rivertrue", RpcTarget.All);
			Debug.Log("testriver");
			DelayedSetGameState(GameState.Showdown, 1f);
							 foreach (var pm in FindObjectsOfType<PlayerManager>())
        {
            if (pm != null && pm.photonView != null)
            {
 pm.photonView.RPC("ActedFalse", RpcTarget.AllBuffered);
            }
        }

			yield break;
        }
        else if (!GameManager.Instance.river)
        {
						 foreach (var pm in FindObjectsOfType<PlayerManager>())
        {
            if (pm != null && pm.photonView != null)
            {
 pm.photonView.RPC("ActedFalse", RpcTarget.AllBuffered);
            }
     }
		DelayedSetGameState(GameState.River, 0f);}
            photonView.RPC("rivertrue", RpcTarget.All);
			DelayedSetGameState(GameState.Showdown,0.5f);
     }
	 [PunRPC]
public void RPC_SetPlayerWaiting(string targetPlayerName)
{
    PlayerManager target = activePlayers.Find(p => p.PlayerName == targetPlayerName);
    if (target != null)
    {
        target.statue = Statue.Waiting;
        //target.UpdateStatusUI("Waiting");
        Debug.Log($"🔁 RPC: {targetPlayerName} set to Waiting");
    }
}
	 public void SetPlayersWaitingAfterRaise(string raiserName)
{
    foreach (PlayerManager pm in activePlayers)
    {
        if (pm == null || !pm.InGame)
            continue;

        if (pm.PlayerName == raiserName || pm.isFolded || pm.statue ==Statue.Folded)
            continue;

        photonView.RPC("RPC_SetPlayerWaiting", RpcTarget.AllBuffered, pm.PlayerName);
    }
}
	
[PunRPC]
private void StartSit()
{
	  if (!DeckInstance.roundResetInProgress &&ActivePlayerCount()>1 &&!roundInProgress)
        {
        DeckInstance.photonView.RPC("RPC_ClearCommunityAndDeck", RpcTarget.AllBufferedViaServer);
photonView.RPC("SetGameState", RpcTarget.MasterClient, GameState.Waiting);

            DeckInstance.photonView.RPC("ResetInProgressT", RpcTarget.AllBuffered);
			 photonView.RPC("Progress", RpcTarget.AllBuffered);
			StartCoroutine(DeckInstance.Next());
	
}
	
 
}

private void OnEnable()
{

}

private void blindt()
{
blindtrue=true;
}
private void blindf()
{
blindtrue=false;
}

[PunRPC]
private void PostBlinds()
{
	if(blindtrue)
	return;
	
	 photonView.RPC("blindt", RpcTarget.AllBuffered);
    PlayerManager[] allPlayers = FindObjectsOfType<PlayerManager>();
    
    if (allPlayers.Length < 2)
    {
        Debug.LogWarning("Not enough players to post blinds");
        return;
    }
    
    // Find player with lowest seat index (Small Blind)
    PlayerManager smallBlindPlayer = allPlayers
        .Where(p => p != null)
        .OrderBy(p => p.seatIndex)
        .FirstOrDefault();
    
    if (smallBlindPlayer == null)
    {
        Debug.LogError("Could not find small blind player");
        return;
    }
    
    // Find next player for Big Blind
    var sortedPlayers = allPlayers
        .Where(p => p != null)
        .OrderBy(p => p.seatIndex)
        .ToList();
    
    int smallBlindIndex = sortedPlayers.IndexOf(smallBlindPlayer);
    int bigBlindIndex = (smallBlindIndex + 1) % sortedPlayers.Count;
    PlayerManager bigBlindPlayer = sortedPlayers[bigBlindIndex];
    
    Debug.Log($"🎲 Small Blind: {smallBlindPlayer.PlayerName} (Seat {smallBlindPlayer.seatIndex})");
    Debug.Log($"🎲 Big Blind: {bigBlindPlayer.PlayerName} (Seat {bigBlindPlayer.seatIndex})");
    
    if (FirstTurn)
    {
        // ✅ FIXED: Call RPC to the owner of each player
        smallBlindPlayer.photonView.RPC("PostBlind", RpcTarget.All, Big);
		smallBlindPlayer.isSMALL=true;
        bigBlindPlayer.photonView.RPC("PostBlind", RpcTarget.All, Big);
        FirstTurn = false;
        return;
    }
    else 
    {
        // ✅ FIXED: Call RPC to the owner of each player
        smallBlindPlayer.photonView.RPC("PostBlind", RpcTarget.All, Small);
		smallBlindPlayer.isSMALL=true;
        bigBlindPlayer.photonView.RPC("PostBlind", RpcTarget.All, Big);

    }
}


public PlayerManager FindPlayerByName(string playerName)
{
    return activePlayers.FirstOrDefault(p => p != null && p.PlayerName == playerName);
}

  void Awake() {
    if (Instance != null && Instance != this) {
      Debug.LogWarning("Duplicate GameManager found, destroying it: " + gameObject.name);
      Destroy(gameObject);
      return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);  // Optional: use only if you load scenes and want to keep GameManager
    Debug.Log("GameManager initialized: " + gameObject.name);
  }
  private IEnumerator DelayedGameStateRoutine(GameState nextState, float delay)
{
    yield return new WaitForSeconds(delay);
    
    if (PhotonNetwork.IsMasterClient)
    {
        SetGameState(nextState);
    }
}
}
public class HandComparer : IComparer<PlayerManager>
{
    public int Compare(PlayerManager x, PlayerManager y)
    {
        if (x == null || y == null)
            return 0;
        
        // ✅ First compare HandRank (higher is better)
        int handRankComparison = x.currentHandRank.CompareTo(y.currentHandRank);
        if (handRankComparison != 0)
            return handRankComparison;
        
        // ✅ If HandRank is equal, compare each card from highest to lowest
        for (int i = 0; i < 5; i++)
        {
            if (x.currentBestCards.Count <= i || y.currentBestCards.Count <= i)
                break;
            
            int cardComparison = ((int)x.currentBestCards[i].rank).CompareTo((int)y.currentBestCards[i].rank);
            
            if (cardComparison != 0)
                return cardComparison;
        }
        
        // ✅ Hands are completely equal
        return 0;
    }
}
