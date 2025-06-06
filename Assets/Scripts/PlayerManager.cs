using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using System;
using TMPro;
using System.Collections;
using ExitGames.Client.Photon;
using UnityEngine.UI;
using System.Globalization;
using System.Collections.Concurrent;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
public static class RaiseEventCodes
{
    public const byte RaiseMade = 2;
    public const byte AssignPositionsEventCode = 4;

}
public class HandResult
{
    public string PlayerName;
    public HandRank Rank;
    public List<int> RankValues;
}
public class PlayerManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
private const byte EVENT_CODE_SYNC_POTS = 200;
public static bool blindsPosted = false;
public bool dealerAssigned = false;
public static Dictionary<int, PlayerPosition> playerPositions = new Dictionary<int, PlayerPosition>();
public bool positionReceived = false;
public HandRank bestHandRank;
public string playerID; 
public long bet=0;
public long callAmount=0;
	public long PlayersBet = 0;
public bool InGame = false;
public ClickSpawner clickSpawner;
    public Transform cardHandPosition;
public Statue statue;
 public long chipCount;
 	public int currentSeat;
     public List<Card> playerHand = new List<Card>();
public PlayerPosition playerPosition = PlayerPosition.None;

      public GameObject UI; 
	  public TextMeshProUGUI callAmountText;
	  	 public string Name;
	 [SerializeField] private Slider betSlider;
	 	public TMP_Text nicknameText;
		[SerializeField] private TMP_Text chipCountText;
		public TMP_Text betAmountText;
		private HandEvaluator handEvaluator = new HandEvaluator();
		public HandEvaluator.EvaluatedHand bestHand;  
 private void OnDestroy()
{
 if (playerPosition != PlayerPosition.None)
    {
        GameManager.Instance.UnregisterPosition(playerPosition);
        PhotonNetwork.RemoveCallbackTarget(this); 
    }
}

int CountPlayersInGame()
{
    int count = 0;

    foreach (Player player in PhotonNetwork.PlayerList)
    {
        if (player.CustomProperties.ContainsKey("InGame") && (bool)player.CustomProperties["InGame"] == true)
        {
            
            count++;
        }
        else
        {
            Debug.Log($"Player {player.NickName} is NOT marked as InGame");
        }
    }

    Debug.Log("Total InGame players: " + count);
    return count;
}
public void AddChips(long amount)
    {
        chipCount += amount;
     
		photonView.RPC("UpdateChipCount", RpcTarget.AllBuffered, chipCount);
    }
[PunRPC]
public void EvaluateHandRPC()
{
    if (playerHand.Count != 7)
    {
    
        return;
    }

    var bestHand = handEvaluator.EvaluateHand(playerHand);

    if (bestHand != null)
    {
        string kickerString = string.Join(", ", bestHand.RankValues);
        string cardString = string.Join(" | ", bestHand.CardsInHand.Select(card => $"{card.rank} of {card.suit}"));

        Debug.Log($"🃏 [EvaluateHandRPC] Player: {photonView.Owner.NickName}\n" +
                  $"Hand Rank: {bestHand.Rank}\n" +
                  $"Kickers: {kickerString}\n" +
                  $"Best 5 Cards: {cardString}");

        // Only send result to MasterClient from the owner
        if (photonView.IsMine)
        {
photonView.RPC("SendHandResultToMaster", RpcTarget.MasterClient,
    photonView.Owner.NickName, (int)bestHand.Rank, bestHand.RankValues.ToArray());
        }
    }
    else
    {
        Debug.LogError($"[{photonView.Owner.NickName}] Hand evaluation failed.");
    }
	bestHand = null;
  bestHandRank = HandRank.None;
}
public int GetExpectedPlayerCount()
{
    // Basic version: all connected players
    return PhotonNetwork.PlayerList.Length;

    // Or, better: return only players who didn't fold, if you track that
    // return activePlayersInHand.Count;
}
[PunRPC]
public void ShareHandWithMaster()
{
    if (!photonView.IsMine) return;

    List<int> cardViewIDs = new List<int>();

    foreach (Card card in playerHand)
    {
        if (card != null && card.TryGetComponent(out PhotonView view))
        {
            cardViewIDs.Add(view.ViewID);
        }
    }

    
    photonView.RPC("ReceiveHandForDebug", RpcTarget.MasterClient, photonView.Owner.NickName, cardViewIDs.ToArray());
}
[PunRPC]
public void ReceiveHandForDebug(string playerName, int[] cardIDs)
{
    List<Card> cards = new List<Card>();

    foreach (int viewID in cardIDs)
    {
        PhotonView view = PhotonView.Find(viewID);
        if (view != null && view.TryGetComponent(out Card card))
        {
            cards.Add(card);
        }
    }

  
    HandEvaluator evaluator = new HandEvaluator();
    var bestHand = evaluator.EvaluateHand(cards);

    if (bestHand != null)
    {
        string kickerString = string.Join(", ", bestHand.RankValues);
        string cardString = string.Join(" | ", bestHand.CardsInHand.Select(c => $"{c.rank} of {c.suit}"));

        Debug.Log($"🧠 [MasterClient Debug]\n" +
                  $"Player: {playerName}\n" +
                  $"Rank: {bestHand.Rank}\n" +
                  $"Kickers: {kickerString}\n" +
                  $"Best 5 Cards: {cardString}");
    }
    else
    {
        Debug.LogWarning($"[MasterClient Debug] Failed to evaluate hand for {playerName}");
    }
}

[PunRPC]
public void DebugShowHand()
{
    if (playerHand == null || playerHand.Count == 0)
    {
        Debug.LogWarning($"[DebugShowHand] Player {photonView.Owner.NickName} has no cards.");
        return;
    }

    string handDescription = string.Join(" | ", playerHand.Select(card => $"{card.rank} of {card.suit}"));
    Debug.Log($"🃏 [ShowHand] Player: {photonView.Owner.NickName} | Hand ({playerHand.Count} cards): {handDescription}");
}
[PunRPC]
void SendHandResultToMaster(string playerName, int handRank, int[] rankValues)
{
    GameManager.Instance.ReceivePlayerResult(playerName, (HandRank)handRank, rankValues.ToList());
}

	[PunRPC]
public void RevealAllCards()
{
    foreach (var player in PhotonNetwork.PlayerList)
    {
        if (player.TagObject is GameObject obj && obj.TryGetComponent(out PlayerManager pm))
        {
            pm.photonView.RPC("ShowOff", RpcTarget.AllBuffered);
        }
    }
}
private void TransformCardPositions()
    {
        for (int i = 0; i < playerHand.Count; i++)
        {
            Card card = playerHand[i];
            if (card != null)
            {
                Vector3 desiredPosition = new Vector3(i * 100f, 0f, 0f);

              
                card.transform.localPosition = desiredPosition;
                card.transform.localRotation = Quaternion.identity; // Reset rotation

            
                card.transform.localScale = Vector3.one;

               
            }    
            else
            {
                Debug.LogError("Card is null in TransformCardPositions().");
            }
        }
    }
	    [PunRPC]
    public void AddCommunityCardsRPC(int[] cardViewIDs)
    {
        
        foreach (int cardViewID in cardViewIDs)
        {
            PhotonView cardView = PhotonView.Find(cardViewID);
            if (cardView != null)
            {
                Card card = cardView.GetComponent<Card>();
                if (card != null)
                {
                    if (!playerHand.Contains(card))
                    {
                        playerHand.Add(card);
                        
                    }
                }
            }
        }
    }
public void OnCallButtonClicked()
{
    if (!photonView.IsMine)
        return;

    if (callAmount <= 0)
    {
        UI?.SetActive(false);
        RaiseNextPlayerEvent();
        GameManager.Instance.photonView.RPC("PlayerFinishedTurn", RpcTarget.MasterClient);
        Debug.Log("You already matched the current bet.");
        return;
    }

if (callAmount >= chipCount)
    {callAmount=GetChipCount();
        UI?.SetActive(false);
        RaiseNextPlayerEvent();
        GameManager.Instance.photonView.RPC("PlayerFinishedTurn", RpcTarget.MasterClient);
        Debug.Log("You already matched the current bet.");
    }
   
    chipCount -= callAmount;
    PlayersBet += callAmount;
	GameManager.Instance.AddChipsToPot(photonView.Owner.NickName, callAmount);
    photonView.RPC("UpdateChipCount", RpcTarget.AllBuffered, chipCount);
	Debug.Log($"[Call] {photonView.Owner.NickName} calling {callAmount}, chipCount: {chipCount}");
    //UpdatePot(callAmount);
  //GameManager.Instance.AddPlayerContribution(photonView.Owner.NickName, callAmount);
    bool isAllIn = chipCount <= 0;


    callAmount = 0;
    if (isAllIn)
        InGame = false;

    
    //GameManager.Instance.potAmount += callAmount;
    GameManager.Instance.photonView.RPC("UpdatePotUI", RpcTarget.AllBuffered, GameManager.Instance.potAmount);
    photonView.RPC("CallAmountReset", RpcTarget.AllBuffered);

    UI?.SetActive(false);
    GameManager.Instance.photonView.RPC("PlayerFinishedTurn", RpcTarget.MasterClient);

    
    int inGameCount = CountPlayersInGame();
    if (inGameCount <= 1)
    {
        photonView.RPC("RevealAllCards", RpcTarget.AllBuffered);

        
        if (!GameManager.Instance.flop)
        {
            GameManager.Instance.Invoke("Comm", 0.5f);
            statue = Statue.AllIn;
        }
        else if (!GameManager.Instance.turn)
        {
            GameManager.Instance.Invoke("Turn", 0.5f);
            GameManager.Instance.photonView.RPC("turntrue", RpcTarget.All);
            GameManager.Instance.Invoke("River", 0.8f);
            GameManager.Instance.photonView.RPC("rivertrue", RpcTarget.All);
			Debug.Log("testriver");
        }
        else if (!GameManager.Instance.river)
        {
            StartCoroutine(GameManager.Instance.DelayedRiver());

        }

        RaiseNextPlayerEvent();
        return;
    }

   
    RaiseNextPlayerEvent();
}

[PunRPC]
public void gametrue()
{
	InGame=true;
}
	public void StandUp()
    {
        if (clickSpawner != null)
        {
            clickSpawner.OnStandUpClick();
        }
		 Debug.Log($"{photonView.Owner.NickName} stood up from {playerPosition}");
    playerPosition = PlayerPosition.None;
    }	


[PunRPC]
public void AddCommunityCardToHand(int cardViewID)
{
    PhotonView cardView = PhotonView.Find(cardViewID);
    if (cardView == null) return;

    Card card = cardView.GetComponent<Card>();
    if (card == null) return;

    
    if (!playerHand.Contains(card))
    {
        playerHand.Add(card);
       
    }
}
[PunRPC]
private void UpdateUI()
{
    if (GameManager.Instance.potAmount != null)
        GameManager.Instance.potText.text = $"Pot: {FormatChipsWithSuffix(GameManager.Instance.potAmount)}";
}
[PunRPC]
public void ShowOff()
{
    
    foreach (Card card in playerHand)
    {
       
        card.gameObject.SetActive(true);
    }
}

public long GetChipCount()
    {
        
        return chipCount;
    }
private ClickSpawner FindClickSpawner(int seat)
{
    ClickSpawner[] spawners = FindObjectsOfType<ClickSpawner>();
    
    foreach (ClickSpawner spawner in spawners)
    {
        
        if (spawner.gameObject.activeSelf)
        {
            
            if (spawner.seatNumber == seat)
            {
                return spawner;
            }
        }
    }
    return null;
}
public void OnEvent(EventData photonEvent)
{
    switch (photonEvent.Code)
    {
        case 1:
        {
            object[] nextPlayerData = (object[])photonEvent.CustomData;
            int nextPlayerIndex = (int)nextPlayerData[0];

            if ((int)playerPosition == nextPlayerIndex)
                HandleUIForPlayer();
            else if (UI != null)
                UI.SetActive(false);
            break;
        }

        case 2:
        {
            object[] raiseData = (object[])photonEvent.CustomData;
            int actorId = (int)raiseData[0];
            long raiseAmount = (long)raiseData[1];

            GameManager.Instance.callAmount = raiseAmount;

            if (PhotonNetwork.IsMasterClient)
            {
                //UpdatePot(raiseAmount);
				
            }

            foreach (var player in FindObjectsOfType<PlayerManager>())
            {
                player.callAmount = raiseAmount;
                player.UpdateCallAmount(raiseAmount);
                player.SetSliderToCallAmount(raiseAmount);
            }
            break;
        }

        case 3:
        {
            object[] data = (object[])photonEvent.CustomData;

            int dealerViewID = (int)data[0];
            long smallBlind = (long)data[1];
            int utgViewID = (int)data[2];
            long bigBlind = (long)data[3];

            PhotonView dealerView = PhotonView.Find(dealerViewID);
            PhotonView utgView = PhotonView.Find(utgViewID);

                        if (PhotonNetwork.IsMasterClient)
            {
                if (GameManager.Instance.FirstTurn)
                {
                    if (dealerView != null && dealerView.TryGetComponent(out PlayerManager dealer))
                    {
                        dealer.PostBlind(bigBlind); // Dealer = small blind on first round
                        
						GameManager.Instance.AddChipsToPot(dealer.photonView.Owner.NickName, bigBlind);
						//GameManager.Instance.AddPlayerContribution(photonView.Owner.NickName, bigBlind);
                    }

                    if (utgView != null && utgView.TryGetComponent(out PlayerManager utg))
                    {
                        utg.PostBlind(bigBlind); // UTG = big blind on first round
                    
						GameManager.Instance.AddChipsToPot(utg.photonView.Owner.NickName, bigBlind);
						//	GameManager.Instance.AddPlayerContribution(photonView.Owner.NickName, bigBlind);
                    }
                }
                else
                {
                    if (dealerView != null && dealerView.TryGetComponent(out PlayerManager dealer))
                    {
                        dealer.PostBlind(smallBlind);
                        
						GameManager.Instance.AddChipsToPot(dealer.photonView.Owner.NickName, smallBlind);
							//GameManager.Instance.AddPlayerContribution(photonView.Owner.NickName, smallBlind);
                    }

                    if (utgView != null && utgView.TryGetComponent(out PlayerManager utg))
                    {
                        utg.PostBlind(bigBlind);
                       
						GameManager.Instance.AddChipsToPot(utg.photonView.Owner.NickName, bigBlind);
						//GameManager.Instance.AddPlayerContribution(photonView.Owner.NickName, bigBlind);
                    }
                }
            }

            break;
        }

        default:
            Debug.LogWarning($"Unhandled event code: {photonEvent.Code}");
            break;
    }
	 if (photonEvent.Code == EVENT_CODE_SYNC_POTS)
    {
        object[] serializedPots = (object[])photonEvent.CustomData;
        GameManager.Instance.pots.Clear();

        foreach (object potDataObj in serializedPots)
        {
            object[] potData = (object[])potDataObj;
            Pot pot = Pot.Deserialize(potData);
            GameManager.Instance.pots.Add(pot);
        }

        Debug.Log($"Received {GameManager.Instance.pots.Count} pots synced from master.");
    }
}


private void UpdatePot(long raiseAmount)
{
	
	GameManager.Instance.callAmount = raiseAmount;
    GameManager.Instance.photonView.RPC("AddToPot", RpcTarget.AllBuffered, raiseAmount);
}
[PunRPC]
	public void PostBlinds()
{
    if (!PhotonNetwork.IsMasterClient) return;

    PlayerManager[] players = FindObjectsOfType<PlayerManager>();
    if (players.Length != 2) return;

    PlayerManager dealer = null;
    PlayerManager utg = null;

    foreach (var player in players)
    {
        if (player.playerPosition == PlayerPosition.DEALER)
            dealer = player;
        else if (player.playerPosition == PlayerPosition.UTG)
            utg = player;
}

    if (dealer == null || utg == null)
    {
        Debug.LogWarning("Cannot assign blinds - Dealer or UTG not found.");
        return;
    }

    
    object[] content = new object[]
    {
        dealer.photonView.ViewID, GameManager.SMALL_BLIND_AMOUNT,
        utg.photonView.ViewID, GameManager.BIG_BLIND_AMOUNT
    };

    RaiseEventOptions options = new RaiseEventOptions
    {
        Receivers = ReceiverGroup.All
    };

    SendOptions sendOptions = new SendOptions
    {
        Reliability = true
    };

    PhotonNetwork.RaiseEvent(3, content, options, sendOptions);

    Debug.Log($"Posted SMALL_BLIND for Dealer (ViewID {dealer.photonView.ViewID}) and BIG_BLIND for UTG (ViewID {utg.photonView.ViewID})");
}
public void PostBlind(long blindAmount)
{
    chipCount -= blindAmount;
    photonView.RPC("UpdateChipCount", RpcTarget.AllBuffered, chipCount);
if (photonView.IsMine && betSlider != null)
    {
        betSlider.maxValue = GetChipCount(); // Update slider max after deduction
        betSlider.value = Mathf.Min(betSlider.value, betSlider.maxValue);
    }
   
}

[PunRPC]
public void SetSliderToCallAmount(long raiseAmount)
{
    if (betSlider != null)
    {
        
        float clampedValue = Mathf.Clamp(raiseAmount, betSlider.minValue, betSlider.maxValue);
        betSlider.value = clampedValue;
    }
}
[PunRPC]
public void UpdateCallAmount(long newCallAmount)
{
    callAmount = newCallAmount;

    if (callAmountText != null)
    {
        callAmountText.text = $"Call: {callAmount}";
    }
}
public void OnRaiseButtonClicked()
{
    if (!photonView.IsMine)
        return;

    if (chipCount < PlayersBet)
    {
        PlayersBet=chipCount;

    }

   
    chipCount -= PlayersBet;
	Debug.Log($"Adding {PlayersBet} chips to pot for player {photonView.Owner.NickName}");
  GameManager.Instance.AddChipsToPot(photonView.Owner.NickName, PlayersBet);
    //GameManager.Instance.AddPlayerContribution(photonView.Owner.NickName, PlayersBet);
    object[] content = new object[]
    {
        PhotonNetwork.LocalPlayer.ActorNumber,
        PlayersBet
    };

    PhotonNetwork.RaiseEvent(
        2,  
        content,
        new RaiseEventOptions { Receivers = ReceiverGroup.All },
        new SendOptions { Reliability = true }
    );

    
    photonView.RPC("UpdateChipCount", RpcTarget.AllBuffered, chipCount);
betSlider.maxValue = GetChipCount(); // Update slider max after deduction
        betSlider.value = 0;
   
    if (UI != null)
        UI.SetActive(false);

   
    RaiseNextPlayerEvent();
if(GameManager.Instance.FirstTurn)
{
	  GameManager.Instance.photonView.RPC("firstfalse", RpcTarget.MasterClient);  
GameManager.Instance.photonView.RPC("PlayerFinishedTurn", RpcTarget.MasterClient);
bool isAllIn = GetChipCount() <= 0;
    if (isAllIn)
InGame = false;
return;}
else{
GameManager.Instance.photonView.RPC("ResetTurnStatesRaise", RpcTarget.MasterClient);}


}




[PunRPC]
public void UpdateChip(long updatedChips)
{
    chipCount = updatedChips;
    chipCountText.text = chipCount.ToString(); // update UI if needed
}

private void RaiseNextPlayerEvent()
{
    PlayerManager[] allPlayers = FindObjectsOfType<PlayerManager>();

    var sorted = allPlayers
        .Where(p => p.playerPosition != PlayerPosition.None)
        .OrderBy(p => (int)p.playerPosition)
        .ToList();

    int currentIndex = sorted.FindIndex(p => p == this);
    if (currentIndex == -1)
    {
        Debug.LogWarning("Current player not found in sorted list.");
        return;
    }

    for (int i = 1; i <= sorted.Count; i++)
    {
        int nextIndex = (currentIndex + i) % sorted.Count;
        PlayerManager candidate = sorted[nextIndex];

        if (candidate != null && candidate.InGame && candidate.chipCount > 0)
        {
           
            object[] eventContent = new object[] { (int)candidate.playerPosition };
            PhotonNetwork.RaiseEvent(1, eventContent, RaiseEventOptions.Default, SendOptions.SendReliable);
            Debug.Log($"[RaiseNextPlayerEvent] Passing turn to: {candidate.photonView.Owner.NickName} (Position: {candidate.playerPosition})");
            return;
        }
    }

    Debug.LogWarning("[RaiseNextPlayerEvent] No eligible player found.");
}

private void HandleUIForPlayer()
{
    
    if (photonView.IsMine)  
    {
        if (UI != null)
            UI.SetActive(true);
		betSlider.minValue = 0;
			betSlider.maxValue = GetChipCount();
        betSlider.value = 0;
    }
	
}
[PunRPC]
public void AddCardToPlayerHandRPC(int cardViewID, PhotonMessageInfo info)
{

    PhotonView cardView = PhotonView.Find(cardViewID);
    if (cardView != null)
    {
        Card card = cardView.GetComponent<Card>();
        if (card != null)
        {
            playerHand.Add(card);
            card.transform.SetParent(cardHandPosition);

           
            card.gameObject.SetActive(photonView.IsMine);

           
            TransformCardPositions();
photonView.RPC("HandleUIForPlayerUTG", RpcTarget.AllBuffered);
     }
    }

   
    InGame = true;
}
[PunRPC]
private void HandleUIForPlayerUTG()
{
    
    if (playerPosition == PlayerPosition.UTG)
    {
  if (photonView.IsMine)  
        {

            if (UI != null)
                UI.SetActive(true); 
        }
    }
    else
    {
        if (UI != null)
            UI.SetActive(false); 
    }
}
[PunRPC]
    public void SetCurrentSeat(int seat)
    {
        currentSeat = seat;
    }

  [PunRPC]
	private void ResetAmount()
    {
        callAmount = 0;
		photonView.RPC("UpdateCallAmount", RpcTarget.All, callAmount);
    }
[PunRPC]
public void IsPlaying(int statueValue)
{
   statue = (Statue)statueValue;
if (statue == Statue.Playing)
    {if (PhotonNetwork.IsMasterClient)
        {
        photonView.RPC("Roles", RpcTarget.AllBuffered);
    }}
}
[PunRPC]
public void AssignPositionRPC(PlayerPosition assignedPosition)
{
    if (playerPosition != PlayerPosition.None)
    {
        Debug.LogWarning($"{photonView.Owner.NickName} already assigned position {playerPosition}, ignoring new assignment {assignedPosition}");
        return;
    }

    playerPosition = assignedPosition;
    Debug.Log($"Assigned position {assignedPosition} to {photonView.Owner.NickName}");
}



    
    void UpdatePositionFromProperties()
    {
        if (photonView.Owner.CustomProperties.TryGetValue("PlayerPosition", out object pos))
        {
            playerPosition = (PlayerPosition)(int)pos;
            Debug.Log($"Player {photonView.Owner.NickName} got assigned position {playerPosition}");
        }
        else
        {
            Debug.LogWarning($"Player {photonView.Owner.NickName} has no position assigned yet.");
        }
    }

    
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (photonView.Owner == targetPlayer && changedProps.ContainsKey("PlayerPosition"))
        {
            playerPosition = (PlayerPosition)(int)changedProps["PlayerPosition"];
            Debug.Log($"Player {photonView.Owner.NickName} updated position to {playerPosition}");
        }
    }
 private void OnEnable()
    {
		ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
        customProperties.Add("joinTime", PhotonNetwork.Time);  // Store the join time when the player joins
        PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);
		clickSpawner = FindObjectOfType<ClickSpawner>();
        if(clickSpawner == null)
            Debug.LogWarning("No ClickSpawner found in scene.");	
AssignPositions();

		
 
        if (photonView.IsMine)
        {
          
GameManager.Instance.photonView.RPC("AddPlayerToList", RpcTarget.AllBuffered,PhotonNetwork.LocalPlayer.ActorNumber);
            photonView.Owner.TagObject = this.gameObject;
           
			PhotonNetwork.AddCallbackTarget(this); 
			photonView.RPC("IsPlaying", RpcTarget.AllBuffered, (int)Statue.Waiting);
			  
			GameManager.Instance.Resetstate();
			GameManager.Instance.photonView.RPC("StartSit", RpcTarget.MasterClient);
     photonView.RPC("PostBlinds", RpcTarget.All);
        }  
		 
    }
private void AssignPositions()
{
    bool positionAssigned = false;

    for (int i = (int)PlayerPosition.DEALER; i <= (int)PlayerPosition.POSITION_8; i++)
    {
        PlayerPosition positionToAssign = (PlayerPosition)i;

        if (GameManager.Instance.IsPositionAvailable(positionToAssign))
        {
            playerPosition = positionToAssign;

            GameManager.Instance.RegisterPosition(positionToAssign);
            photonView.Owner.TagObject = this.gameObject;

            Debug.Log($"XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");

            positionAssigned = true;

           
Debug.Log($".");
            break;
        }
    }

    if (!positionAssigned)
    {
        Debug.LogWarning("[Client] No available position to assign.");
    }
}
 [PunRPC]
	public void ClearHand()
{
   
    foreach (Card card in playerHand)
    {
        PhotonNetwork.Destroy(card.gameObject); 
    }
 playerHand.Clear();
   
}
private void OnSliderValueChange(float value)
{
	PlayersBet = (long)value;
 photonView.RPC("UpdateRaiseAmountText", RpcTarget.All);
}
[PunRPC]
void SetPlayerName(string name)
{
    nicknameText.text = name;
	photonView.RPC("UpdateBlindUI", RpcTarget.AllBuffered);
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
private void UpdateRaiseAmountText()
{
if (betAmountText != null)
betAmountText.text = $"{FormatChipsWithSuffix(PlayersBet)}";
}

    void Start()
    { 
	
        betSlider.onValueChanged.AddListener(OnSliderValueChange);
	  



long minRaise = GameManager.BIG_BLIND_AMOUNT;
betSlider.minValue = minRaise;
betSlider.value = minRaise; 

OnSliderValueChange(betSlider.value); 
    if (callAmountText == null)
    {
        callAmountText = GameObject.Find("CallAmountText").GetComponent<TextMeshProUGUI >();
    }
    }

    void Update()
    {
        
    }
	[PunRPC]
public void UpdateChipCount(long newChipCount)
{
    this.chipCount = newChipCount;
  chipCountText.text = $"${chipCount.ToString("N0", CultureInfo.InvariantCulture)}";
  long maxRaise = newChipCount;
betSlider.maxValue = maxRaise;
}
	void Awake()
    {
		
        photonView.RPC("UpdateChipCount", RpcTarget.AllBuffered, chipCount);
		if (photonView.IsMine)
    {
		photonView.RPC("SetPlayerName", RpcTarget.AllBuffered, PhotonNetwork.NickName);
    }    
	  }
	  
	 [PunRPC]
public void Check()
{
    Debug.Log($"[Check] Called on: {photonView.Owner.NickName}, chipCount = {chipCount}");
Debug.Log($"[Check] photonView.IsMine = {photonView.IsMine}, IsLocalPlayer = {PhotonNetwork.LocalPlayer.NickName}, Owner = {photonView.Owner.NickName}");
    if (clickSpawner == null)
    {
        clickSpawner = FindObjectOfType<ClickSpawner>();
        if (clickSpawner == null)
        {
            Debug.LogWarning($"[Check] clickSpawner is NULL for {photonView.Owner.NickName}");
            return;
        }
    }

    if (chipCount == 0)
    {
        Debug.Log($"[Check] {photonView.Owner.NickName} is standing up.");
        if (photonView.IsMine)
        {
            clickSpawner.OnStandUpClick();
        }
    }
    else
    {
        Debug.Log($"[Check] {photonView.Owner.NickName} has chips, not standing up.");
    }
}


	
[PunRPC]
public void RotatePlayerPositions()
{

    List<PlayerPosition> positions = new List<PlayerPosition>
    {
        PlayerPosition.DEALER, 
        PlayerPosition.UTG, 
        PlayerPosition.UTG_PLUS_1, 
        PlayerPosition.POSITION_3, 
        PlayerPosition.POSITION_4, 
        PlayerPosition.POSITION_5, 
        PlayerPosition.POSITION_6, 
        PlayerPosition.POSITION_7, 
        PlayerPosition.POSITION_8, 
    };

    
    int currentPositionIndex = positions.IndexOf(playerPosition);


    int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;

    if (playerCount == 2)
    {
        if (currentPositionIndex == 0) 
        {
            playerPosition = PlayerPosition.UTG;  
        }
        else if (currentPositionIndex == 1) 
        {
            playerPosition = PlayerPosition.DEALER;  
        }
    }
    else if (playerCount == 3)
    {

        if (currentPositionIndex == 0)
        {
            playerPosition = PlayerPosition.UTG; 
        }
        else if (currentPositionIndex == 1) 
        {
            playerPosition = PlayerPosition.UTG_PLUS_1; 
        }
        else if (currentPositionIndex == 2)  
        {
            playerPosition = PlayerPosition.DEALER; 
        }
    }
    else if (playerCount == 4)
    {

        if (currentPositionIndex == 0) 
        {
            playerPosition = PlayerPosition.UTG; 
        }
        else if (currentPositionIndex == 1)  
        {
            playerPosition = PlayerPosition.UTG_PLUS_1;  
        }
        else if (currentPositionIndex == 2)  
        {
            playerPosition = PlayerPosition.POSITION_3;  
        }
        else if (currentPositionIndex == 3)  
        {
            playerPosition = PlayerPosition.DEALER;  
        }
    }
    else if (playerCount == 5)
    {

        if (currentPositionIndex == 0)  
        {
            playerPosition = PlayerPosition.UTG;  
        }
        else if (currentPositionIndex == 1)  
        {
            playerPosition = PlayerPosition.UTG_PLUS_1;  
        }
        else if (currentPositionIndex == 2) 
        {
            playerPosition = PlayerPosition.POSITION_3;  
        }
        else if (currentPositionIndex == 3) 
        {
            playerPosition = PlayerPosition.POSITION_4;  
        }
        else if (currentPositionIndex == 4)  
        {
            playerPosition = PlayerPosition.DEALER;  
        }
    }
    else if (playerCount == 6)
    {

        if (currentPositionIndex == 0) 
        {
            playerPosition = PlayerPosition.UTG; 
        }
        else if (currentPositionIndex == 1) 
        {
            playerPosition = PlayerPosition.UTG_PLUS_1; 
        }
        else if (currentPositionIndex == 2) 
        {
            playerPosition = PlayerPosition.POSITION_3;  
        }
        else if (currentPositionIndex == 3) 
        {
            playerPosition = PlayerPosition.POSITION_4;  
        }
        else if (currentPositionIndex == 4)  
        {
            playerPosition = PlayerPosition.POSITION_5;  
        }
        else if (currentPositionIndex == 5) 
        {
            playerPosition = PlayerPosition.DEALER; 
        }
    }
    else if (playerCount == 7)
    {

        if (currentPositionIndex == 0)  
        {
            playerPosition = PlayerPosition.UTG;
        }
        else if (currentPositionIndex == 1)  
        {
            playerPosition = PlayerPosition.UTG_PLUS_1; 
        }
        else if (currentPositionIndex == 2)  
        {
            playerPosition = PlayerPosition.POSITION_3;
        }
        else if (currentPositionIndex == 3)
        {
            playerPosition = PlayerPosition.POSITION_4; 
        }
        else if (currentPositionIndex == 4)
        {
            playerPosition = PlayerPosition.POSITION_5;
        }
        else if (currentPositionIndex == 5)
        {
            playerPosition = PlayerPosition.POSITION_6; 
        }
        else if (currentPositionIndex == 6) 
        {
            playerPosition = PlayerPosition.DEALER;  
        }
    }
    else if (playerCount == 8)
    {

        if (currentPositionIndex == 0)
        {
            playerPosition = PlayerPosition.UTG; 
        }
        else if (currentPositionIndex == 1)
        {
            playerPosition = PlayerPosition.UTG_PLUS_1; 
        }
        else if (currentPositionIndex == 2)
        {
            playerPosition = PlayerPosition.POSITION_3; 
        }
        else if (currentPositionIndex == 3) 
        {
            playerPosition = PlayerPosition.POSITION_4; 
        }
        else if (currentPositionIndex == 4)  
        {
            playerPosition = PlayerPosition.POSITION_5; 
        }
        else if (currentPositionIndex == 5)  
        {
            playerPosition = PlayerPosition.POSITION_6;  
        }
        else if (currentPositionIndex == 6)  
        {
            playerPosition = PlayerPosition.POSITION_7;  
        }
        else if (currentPositionIndex == 7)  
        {
            playerPosition = PlayerPosition.DEALER;  
        }
    }
    else if (playerCount == 9)
    {

        int nextPositionIndex = (currentPositionIndex + 1) % positions.Count;
        playerPosition = positions[nextPositionIndex];  
    } 
	photonView.RPC("BlindFalse", RpcTarget.All); 

    photonView.RPC("Logos", RpcTarget.All);
}
}
public class LexicographicComparer : IComparer<IEnumerable<int>>
{
    public int Compare(IEnumerable<int> x, IEnumerable<int> y)
    {
        var enumX = x.GetEnumerator();
        var enumY = y.GetEnumerator();

        while (enumX.MoveNext() && enumY.MoveNext())
        {
            int cmp = enumX.Current.CompareTo(enumY.Current);
            if (cmp != 0)
                return cmp;
        }

        if (enumX.MoveNext()) return 1;
        if (enumY.MoveNext()) return -1;
        return 0;
    }
}