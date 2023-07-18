using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class PokerSeatButtonScript : MonoBehaviourPunCallbacks
{
    [SerializeField] private int seatIndex; // The index of the seat or button
    private bool isSeatOccupied = false; // Flag to track if the seat is occupied

    private void Update()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("Not connected to Photon network.");
            return;
        }

        // Disable the button for other players if the seat is occupied
        if (isSeatOccupied && !photonView.IsMine)
        {
            GetComponent<Button>().interactable = false;
        }
    }

    // Called when the button is clicked
    public void OnButtonClicked()
    {
        // Raise an event to claim the seat and hide buttons for the local player
        photonView.RPC("ClaimSeatAndHideButtons", RpcTarget.All, seatIndex, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    // RPC method to claim a seat and hide buttons for the local player
    [PunRPC]
    private void ClaimSeatAndHideButtons(int seatIndex, int claimingPlayerActorNumber, PhotonMessageInfo info)
    {
        // Check if the seat is already occupied
        if (isSeatOccupied)
        {
            // Hide the occupied seat for all players
            photonView.RPC("HideOccupiedSeat", RpcTarget.All, seatIndex);
            return;
        }

        // Assign the seat to the player who clicked the button
        // You can use the seat index to determine the player's position

        // Example: Assign the seat to the player with the Photon ID
        if (PhotonNetwork.LocalPlayer.ActorNumber == claimingPlayerActorNumber)
        {
            Debug.Log("Player " + info.Sender.NickName + " claimed seat " + seatIndex);
            // Assign the seat to the player locally
            isSeatOccupied = true;

            // Hide the buttons for the local player
            GetComponent<Button>().interactable = false;
        }
    }

    // RPC method to hide the occupied seat for all players
    [PunRPC]
    private void HideOccupiedSeat(int seatIndex)
    {
        // Hide the seat object for all players
        // You can implement your specific logic to hide the occupied seat
    }
}
