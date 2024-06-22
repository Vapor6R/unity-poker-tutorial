using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using System;
using TMPro;
public class PlayerCardHandler : MonoBehaviourPun
{
	public Dictionary<Player, HandRank> playerHands = new Dictionary<Player, HandRank>();
public List<Card> playerCards = new List<Card>();
public Transform cardParent;
[SerializeField]
private TextMeshProUGUI resultText;
private int playerID;
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
                // Ensure cards are not duplicated
                if (!playerCards.Contains(card))
                {
                    playerCards.Add(card);
                }
               

                Debug.Log($"Added community card {card.rank} of {card.suit} to {PhotonNetwork.LocalPlayer.NickName}'s hand.");
            }
        }
    }
}
void Start()
{
	
	playerID = PhotonNetwork.LocalPlayer.ActorNumber;
	if (photonView.IsMine)
        {
resultText.gameObject.SetActive(false);			// Disable the script
        }
}
void Update()
{
if (playerCards.Count >= 7)
        {
HandRank bestHand = HandEvaluator.EvaluateBestHand(playerCards);
string result = $"Player {playerID} - Best Hand: {bestHand}";
            UpdateResultText(result);

        }
        
        else
        {
            string result = $"Player {playerID} - Not enough cards to evaluate.";
            
        }
}
    private void UpdateResultText(string text)
    {
        resultText.text = text;
    }
	
[PunRPC]
public void AddCardToPlayerHandRPC(int cardViewID)
{
PhotonView cardView = PhotonView.Find(cardViewID);
if (cardView != null)
{
Card card = cardView.GetComponent<Card>();
if (card != null)
{
playerCards.Add(card);
card.transform.SetParent(cardParent);
// Optionally, adjust card positions in the player's hand
TransformCardPositions();

            Debug.Log($"Added card {card.rank} of {card.suit} to {PhotonNetwork.LocalPlayer.NickName}'s hand.");
        }
    }
}


private void TransformCardPositions()
{
    for (int i = 0; i < playerCards.Count; i++)
    {
        Card card = playerCards[i];
        // Set desired position for each card
        Vector3 desiredPosition = new Vector3(i * 130f, 0f, 0f);
        card.transform.localPosition = desiredPosition;
    }
}

}