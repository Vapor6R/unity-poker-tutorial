using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using System;
using TMPro;

using System.Collections;
using ExitGames.Client.Photon;

public class PlayerCardHandler : MonoBehaviourPun
{
   
    private PlayerAction action;

    public Dictionary<Player, HandRank> playerHands = new Dictionary<Player, HandRank>();
    public List<Card> playerCards = new List<Card>();
    public Transform cardParent;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI chipCountText;
    private int playerID;
    private int chipCount = 1000;
    private bool PlayerRaise = false;
 public HandRank playerHandRank = HandRank.None;
    public enum PlayerAction
    {
        Waiting,
        Called,
        Raised,
        Folded
    }

    public enum GameState
    {
        Waiting,
        Playing,
        BettingRound,
        DistributingCommunityCards
    }

  [PunRPC]
    public void ResetPlayerHandRank()
    {
        playerHandRank = HandRank.None;
        Debug.Log("Player hand rank reset to None.");
    }

    public HandRank GetPlayerHandRank()
{
    Debug.Log("Entering GetPlayerHandRank method.");
    
    if (playerCards == null || !playerCards.Any())
    {
        Debug.LogWarning("playerCards is null or empty.");
        return default; // Return a default value if needed
    }
    
    Debug.Log("playerCards is valid. Proceeding with evaluation.");
    
    Debug.Log($"Hand rank evaluated: {playerHandRank}");
    float delayInSeconds = 5.0f; // Set your desired delay time
    ResetPlayerHandRankOnAllClientsWithDelay(delayInSeconds);
    return playerHandRank;
	
}


    // Example method to set the player's hand rank
    public void SetPlayerHandRank(HandRank handRank)
    {
        playerHandRank = handRank;
		
    }

[PunRPC]
public void SetPlayerHandRankRPC(HandRank handRank)
{
    SetPlayerHandRank(handRank);
}public void CallSetPlayerHandRankRPC(HandRank handRank)
{
    photonView.RPC("SetPlayerHandRankRPC", RpcTarget.AllBuffered, handRank);
}

    [PunRPC]
    public void SetPlayerRaiseTrue()
    {
        PlayerRaise = true;
        Debug.Log("PlayerRaise set to true.");
    }

    public bool IsPlayerRaise()
    {
        return PlayerRaise;
    }

    [PunRPC]
    public void AddCommunityCardsRPC(int[] cardViewIDs)
    {
        Debug.Log("Adding community cards...");
        foreach (int cardViewID in cardViewIDs)
        {
            PhotonView cardView = PhotonView.Find(cardViewID);
            if (cardView != null)
            {
                Card card = cardView.GetComponent<Card>();
                if (card != null)
                {
                    if (!playerCards.Contains(card))
                    {
                        playerCards.Add(card);
                        Debug.Log($"Added community card {card.rank} of {card.suit}.");
                    }
                }
            }
        }
    }

    void Start()
    {
        Debug.Log("Start method called.");
		playerID = PhotonNetwork.LocalPlayer.ActorNumber;

      
      
        if (photonView.IsMine)
        {
            photonView.RPC("UpdateChipCountRPC", RpcTarget.AllBuffered, chipCount);
            resultText.gameObject.SetActive(false);
            chipCountText.gameObject.SetActive(true);
            Debug.Log($"Player ID {playerID} - Chip count initialized to {chipCount}");
        }
        else
        {
            chipCountText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerCards.Count >= 7)
        {
            HandRank playerHandRank = HandEvaluator.EvaluateBestHand(playerCards);
            Debug.Log($"Player {playerID} - Best Hand: {playerHandRank}");
            
            // Notify GameManager of this player's hand rank
            if (photonView.IsMine)
            {
                 CallSetPlayerHandRankRPC(playerHandRank);
				photonView.RPC("UpdatePlayerHandRankRPC", RpcTarget.All, playerID, playerHandRank);
            
			}
            
            StartCoroutine(ResetResultTextAfterDelay(2f));
        }
        else
        {
            string result = $"Player {playerID} - Not enough cards to evaluate.";
            StartCoroutine(ResetResultTextAfterDelay(2f));
        }
		
    }
public void ResetPlayerHandRankOnAllClientsWithDelay(float delay)
    {
        StartCoroutine(ResetPlayerHandRankCoroutine(delay));
    }
	private IEnumerator ResetPlayerHandRankCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        photonView.RPC("ResetPlayerHandRank", RpcTarget.All);
    }
    private void UpdateResultText(string text)
    {
        resultText.text = text;
    }

    [PunRPC]
    public void AddCardToPlayerHandRPC(int cardViewID)
    {
        Debug.Log($"Adding card with view ID {cardViewID} to player hand.");
        PhotonView cardView = PhotonView.Find(cardViewID);
        if (cardView != null)
        {
            Card card = cardView.GetComponent<Card>();
            if (card != null)
            {
                playerCards.Add(card);
                card.transform.SetParent(cardParent);
                card.gameObject.SetActive(photonView.IsMine);
                TransformCardPositions();
                Debug.Log($"Added card {card.rank} of {card.suit} to hand.");
            }
        }
    }

    public int GetChipCount()
    {
        return chipCount;
    }

    public void DeductBlind(int blindAmount)
    {
        if (blindAmount > 0 && chipCount >= blindAmount)
        {
            int newChipCount = chipCount - blindAmount;
            photonView.RPC("UpdateChipCountRPC", RpcTarget.All, newChipCount);
            Debug.Log($"Blind of {blindAmount} deducted. New chip count: {chipCount}");
        }
        else
        {
            Debug.LogWarning("Insufficient chips or invalid blind amount.");
        }
    }

    public void UpdateChipCount(int amount)
    {
        chipCount = Mathf.Max(0, amount); 
        Debug.Log($"Chip Count Updated: {chipCount}");
    }

    [PunRPC]
    public void UpdateChipCountRPC(int newChipCount)
    {
        chipCount = newChipCount;
        if (chipCountText != null)
        {
            chipCountText.text = $"Chips: {chipCount}";
            Debug.Log($"Chip count text updated to: {chipCount}");
        }
    }

    private void TransformCardPositions()
    {
        for (int i = 0; i < playerCards.Count; i++)
        {
            Card card = playerCards[i];
            Vector3 desiredPosition = new Vector3(i * 14f, 0f, 0f);
            card.transform.localPosition = desiredPosition;
        }
    }

    public void ClearHand()
    {
        foreach (Card card in playerCards)
        {
            PhotonNetwork.Destroy(card.gameObject);
        }
        playerCards.Clear();
        Debug.Log("Cleared player hand.");
    }

    public void DeductChips(int amount)
    {
        // You might want to add validation here to ensure chipCount doesn't go negative
    }

    [PunRPC]
    public void DeductChipsRPC(int amount)
    {
        if (photonView.IsMine)
        {
            chipCount -= amount;
            chipCountText.text = $"Chips: {chipCount}";
            Debug.Log($"Chips deducted by {amount}. New chip count: {chipCount}");
        }
    }

    private IEnumerator ResetResultTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        UpdateResultText(""); // Reset the result text
        Debug.Log("Result text reset after delay.");
    }
	public void AddChips(int amount)
    {
        // Ensure you have a field for chip count
        chipCount += amount;
        photonView.RPC("UpdateChipCountRPC", RpcTarget.All, chipCount);
    }

}
