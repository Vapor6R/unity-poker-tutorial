using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine.UI;
public class GameManager : MonoBehaviourPunCallbacks
{
    public List<int> playerActorNumbers = new List<int>();
    public GameObject UI; // Reference to your UI GameObject
    private int currentPlayerIndex = 0;
    private bool isPlayerTurn = false;
	public InputField raiseInputField;
	public int potAmount = 0;
void Start()
{
	 if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
	 {		 playerActorNumbers.Clear(); // Clear the list first
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                playerActorNumbers.Add(player.ActorNumber);
            }
            StartGame();
}}
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2 && PhotonNetwork.IsMasterClient)
        {
            playerActorNumbers.Clear(); // Clear the list first
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                playerActorNumbers.Add(player.ActorNumber);
            }
            StartGame();
        }
    }

    void StartGame()
    {
        currentPlayerIndex = 0;
        StartTurn(currentPlayerIndex);
    }
 [PunRPC]
    void RestartGameRPC()
    {
        Debug.Log("Restarting game...");
        RestartGame();
    }
	  void RestartGame()
    {
        // Reset game state logic
        currentPlayerIndex = 0;
        StartGame();
    }
    void StartTurn(int playerIndex)
    {
        int currentActorNumber = playerActorNumbers[playerIndex];
		 Debug.Log($"Starting turn for player with ActorNumber {currentActorNumber}");
        if (PhotonNetwork.LocalPlayer.ActorNumber == currentActorNumber)
        {
            // It's the local player's turn
            isPlayerTurn = true;
            // Show UI elements relevant to the player's turn (e.g., buttons for actions)
            Debug.Log($"Starting turn for player {currentActorNumber}");
            UI.SetActive(true);
        }
        else
        {
            // It's not the local player's turn
            isPlayerTurn = false;
            // Hide UI elements not relevant to the current player's turn
            Debug.Log($"It's not your turn (Player {PhotonNetwork.LocalPlayer.ActorNumber}), current turn is for player {currentActorNumber}");
            UI.SetActive(false);
        }
    }

    void EndTurn()
    {
        isPlayerTurn = false;
        UI.SetActive(false); // Hide UI elements for actions

        // Move to the next player's turn
        currentPlayerIndex = (currentPlayerIndex + 1) % playerActorNumbers.Count;
        photonView.RPC("StartTurnRPC", RpcTarget.All, currentPlayerIndex); // RPC to start the next player's turn
    }

    [PunRPC]
    void StartTurnRPC(int newPlayerIndex)
    {
        currentPlayerIndex = newPlayerIndex;
        StartTurn(currentPlayerIndex);
    }


    // Example method for handling player actions (e.g., End Turn button click)
    public void FOLDButtonClick()
    {
        if (isPlayerTurn)
        {
            EndTurn();
if(PhotonNetwork.CurrentRoom.PlayerCount <= 2)
{
Debug.Log("Player folded. Restarting game...");
            photonView.RPC("RestartGameRPC", RpcTarget.All);
}
        }
    }
	 public void CALLButtonClick()
    {
        if (isPlayerTurn)
        {
            EndTurn(); // Call this method when the player ends their turn
        }
    }
	 public void RAISEButtonClick()
    {
        if (isPlayerTurn)
        {
            if (int.TryParse(raiseInputField.text, out int raiseAmount))
            {
                Debug.Log($"Player {PhotonNetwork.LocalPlayer.ActorNumber} raised {raiseAmount}.");

                // Update the pot amount (this can be more complex depending on game rules)
                potAmount += raiseAmount;

                // Synchronize the raised amount with all players
                photonView.RPC("UpdatePotAmountRPC", RpcTarget.All, potAmount);

                // End the current player's turn
                EndTurn();
            }
            else
            {
                Debug.Log("Invalid raise amount entered.");
            }
        }
        else
        {
            Debug.Log("Cannot raise because it's not the local player's turn.");
        }
    }
}
