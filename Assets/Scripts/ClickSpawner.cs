using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Realtime;
public class ClickSpawner : MonoBehaviourPun
{

	public int seatNumber;
    public GameObject objectPrefab;  // Assign the prefab in Inspector
    public Button[] seatButtons;
public Button spawnButton;	// Assign all buttons in the Inspector
    private PhotonView photonView;
    private int buttonIndex;
	public Button standUpButton;
	private GameObject playerInstance;
private PlayerManager playerManager;
private int localSeatIndex = -1; // track local player's seat
private Dictionary<int, Button> locallyHiddenButtons = new Dictionary<int, Button>();
private HashSet<int> rpcHiddenSeatIndices = new HashSet<int>(); // track global hides
    void Start()
    { 
	 if (spawnButton != null)
        {
            spawnButton.onClick.AddListener(OnButtonClick);
        }
	if (standUpButton != null)
        {
            standUpButton.onClick.AddListener(OnStandUpClick);
            standUpButton.gameObject.SetActive(true); // Initially hidden
        }
        photonView = GetComponent<PhotonView>(); // Get PhotonView component

    }

   void OnButtonClick()
    {
        Debug.Log($"[CLICKED] Button Index: {buttonIndex}, Seat Number: {seatNumber}");

        if (PhotonNetwork.IsConnectedAndReady)
        {
			        Vector3 spawnPosition = spawnButton.transform.position;
        Quaternion spawnRotation = Quaternion.Euler(0, 0, 0);
			
             playerInstance = PhotonNetwork.Instantiate(objectPrefab.name, spawnPosition, spawnRotation, 0);

	if (standUpButton != null)
        {
            standUpButton.onClick.AddListener(OnStandUpClick);
            standUpButton.gameObject.SetActive(true); // Initially hidden
        }
            // 🔥 Key part: Get PlayerManager from spawned object
            PlayerManager playerManager = playerInstance.GetComponent<PlayerManager>();
            if (playerManager != null)
            {
               playerManager.photonView.RPC("SetCurrentSeat", RpcTarget.AllBuffered, seatNumber);
                Debug.Log("[OK] Assigned seat number: " + seatNumber);
				playerManager.currentSeat = seatNumber;
            }
            else
            {
                Debug.LogWarning("[ERROR] PlayerManager not found on instantiated object!");
            }
        }
        else
        {
            Debug.LogWarning("Photon not connected!");
        }

       OnButton1Click();

    }
public IEnumerator AssignSeat()
{ yield return new WaitForSeconds(2f); // Adjust the delay time as needed
	{
}}
void OnStandUpClick()
{
    if (playerInstance != null)
    {
        playerManager = playerInstance.GetComponent<PlayerManager>();
        PhotonNetwork.Destroy(playerInstance);

        playerInstance = null;
		if (PhotonNetwork.IsMasterClient)
{
    // Step 3: Get the oldest player (the one who joined first)
    Player oldestPlayer = null;
    foreach (var player in PhotonNetwork.PlayerList)
    {
        // Ensure the joinTime property exists and cast it to double for comparison
        if (player.CustomProperties.ContainsKey("joinTime"))
        {
            double playerJoinTime = (double)player.CustomProperties["joinTime"];
            if (oldestPlayer == null || playerJoinTime < (double)oldestPlayer.CustomProperties["joinTime"])
            {
                oldestPlayer = player;
            }
        }
    }

    // Step 4: Assign the oldest player as the new master client
    if (oldestPlayer != null)
    {
        PhotonNetwork.SetMasterClient(oldestPlayer);
		 
    }
}
    }

    // ✅ Re-enable this player's seat
    if (localSeatIndex >= 0 && localSeatIndex < seatButtons.Length)
    {
        seatButtons[localSeatIndex].gameObject.SetActive(true);
        Debug.Log($"[StandUp] Re-enabled local seat: {localSeatIndex}");
        rpcHiddenSeatIndices.Remove(localSeatIndex);
    }

    locallyHiddenButtons.Clear();
    standUpButton.gameObject.SetActive(false);

    // 🧠 Check if any players remain
    GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
    if (allPlayers.Length == 0)
    {
        Debug.Log("[StandUp] No players left. Re-enabling all seat buttons.");
        foreach (Button button in seatButtons)
        {
            button.gameObject.SetActive(true);
        }
        rpcHiddenSeatIndices.Clear();
    }
    else
    {
        // ✅ Reactivate other non-occupied seats
        HashSet<int> occupiedSeats = new HashSet<int>();
        foreach (GameObject obj in allPlayers)
        {
            PlayerManager pm = obj.GetComponent<PlayerManager>();
            if (pm != null)
            {
                occupiedSeats.Add(pm.currentSeat); // Assumes PlayerManager tracks this
            }
        }

        for (int i = 0; i < seatButtons.Length; i++)
        {
            if (!occupiedSeats.Contains(i) && i != localSeatIndex)
            {
                seatButtons[i].gameObject.SetActive(true);
                Debug.Log($"[StandUp] Re-enabled unoccupied seat: {i}");
            }
        }
    }

    localSeatIndex = -1;
	   GameManager.Instance.Check();
}


[PunRPC]
public void HideButtonRPC(int seatIndex)
{
    rpcHiddenSeatIndices.Add(seatIndex); // record all RPC-hidden indices

    if (seatIndex >= 0 && seatIndex < seatButtons.Length)
    {
        seatButtons[seatIndex].gameObject.SetActive(false);
        Debug.Log($"[RPC] Hiding seat index {seatIndex}");
    }
}
void HideOtherSeatsForLocal()
{
    locallyHiddenButtons.Clear();

    for (int i = 0; i < seatButtons.Length; i++)
    {
        if (i != seatNumber && seatButtons[i].gameObject.activeSelf)
        {
            seatButtons[i].gameObject.SetActive(false);
            locallyHiddenButtons[i] = seatButtons[i];
            Debug.Log($"Locally hiding: {seatButtons[i].name}");
        }
    }
}
	 void OnButton1Click()
{
    HideOtherSeatsForLocal();
    
    localSeatIndex = seatNumber; // Store the seat this player clicked
    photonView.RPC("HideButtonRPC", RpcTarget.AllBuffered, seatNumber);

    Debug.Log("Buttons have been swapped and hidden.");
}
}
