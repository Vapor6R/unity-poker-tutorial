using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class SpawnButtonManager : MonoBehaviourPunCallbacks
{
    [Header("Setup")]
    public Button[] spawnButtons; // Assign 9 buttons in inspector
    public Button standUpButton; // Stand up button
    public GameObject playerPrefab; // Your player prefab (must be in Resources folder)
    public int[] seatIndices = new int[3];
    
    private bool hasSpawned = false;
    private int myOccupiedButtonIndex = -1;
    private GameObject myPlayerInstance;
    
    // Track which buttons are occupied (synced across network)
    private HashSet<int> occupiedButtons = new HashSet<int>();
    
    // Track which player occupies which button
    private Dictionary<int, int> buttonToPlayerID = new Dictionary<int, int>();
    
    // Track which button index each player instance is sitting at
    private Dictionary<GameObject, int> playerInstanceToButton = new Dictionary<GameObject, int>();
    
    private const string SEAT_INDICES_KEY = "SeatIndices";

    void Start()
    {
        // Initialize default seat indices
        if (seatIndices.Length != spawnButtons.Length)
        {
            seatIndices = new int[spawnButtons.Length];
            for (int i = 0; i < seatIndices.Length; i++)
            {
                seatIndices[i] = i;
            }
        }
        
        // Load synced seat indices from room properties (for late joiners)
        LoadSeatIndicesFromRoom();
        
        for (int i = 0; i < spawnButtons.Length; i++)
        {
            int buttonIndex = i; // Capture index for closure
            spawnButtons[i].onClick.AddListener(() => OnButtonClicked(buttonIndex));
        }
        
        // Setup stand up button
        standUpButton.gameObject.SetActive(false);
        standUpButton.onClick.AddListener(OnStandUpClicked);
    }

    void LoadSeatIndicesFromRoom()
    {
        if (PhotonNetwork.CurrentRoom != null && 
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(SEAT_INDICES_KEY))
        {
            int[] savedIndices = PhotonNetwork.CurrentRoom.CustomProperties[SEAT_INDICES_KEY] as int[];
            if (savedIndices != null && savedIndices.Length == seatIndices.Length)
            {
                seatIndices = savedIndices;
                Debug.Log("✅ Loaded seatIndices from room: " + string.Join(",", seatIndices));
            }
        }
        else
        {
            // First player in room - initialize the room property
            SaveSeatIndicesToRoom();
        }
    }

    void SaveSeatIndicesToRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable roomProps = new Hashtable();
            roomProps[SEAT_INDICES_KEY] = seatIndices;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            Debug.Log("💾 Saved seatIndices to room: " + string.Join(",", seatIndices));
        }
    }

    void OnButtonClicked(int buttonIndex)
    {
        // Prevent multiple spawns
        if (hasSpawned) return;
        
        hasSpawned = true;
        myOccupiedButtonIndex = buttonIndex;
        
        // Get button position
        Vector3 spawnPosition = spawnButtons[buttonIndex].transform.position;
        
        // Instantiate player at button position
        myPlayerInstance = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
        
        // Track which button this player instance is sitting at
        playerInstanceToButton[myPlayerInstance] = buttonIndex;
        
        // ✅ Use the current (possibly rotated) seatIndices value
        int seatIndex = seatIndices[buttonIndex];
        
        PhotonView playerPhotonView = myPlayerInstance.GetComponent<PhotonView>();
        if (playerPhotonView != null)
        {
            playerPhotonView.RPC("RPC_SetSeatIndex", RpcTarget.AllBuffered, seatIndex);
            // Also store the button index in the player for rotation purposes
            playerPhotonView.RPC("RPC_SetButtonIndex", RpcTarget.AllBuffered, buttonIndex);
        }
        
        Debug.Log($"🪑 Player spawned at buttonIndex {buttonIndex} with seatIndex {seatIndex}");
        
        // Mark button as occupied for all players via RPC
        photonView.RPC("OccupyButton", RpcTarget.AllBuffered, buttonIndex, PhotonNetwork.LocalPlayer.ActorNumber);
        
        // Hide all buttons locally and show stand up button
        HideAllButtonsLocally();
        standUpButton.gameObject.SetActive(true);
    }

    public void OnStandUpClicked()
    {
        if (!hasSpawned) return;
        
        // Remove tracking
        if (myPlayerInstance != null)
        {
            playerInstanceToButton.Remove(myPlayerInstance);
            PhotonNetwork.Destroy(myPlayerInstance);
            myPlayerInstance = null;
        }
        
        // Free the button for all players via RPC
        FreeButtonForAll(myOccupiedButtonIndex);
        
        // Reset local state
        hasSpawned = false;
        myOccupiedButtonIndex = -1;
        
        // Hide stand up button and show available buttons
        standUpButton.gameObject.SetActive(false);
        ShowAvailableButtons();
    }
    
    void FreeButtonForAll(int buttonIndex)
    {
        // Send RPC to free button
        photonView.RPC("FreeButton", RpcTarget.AllBuffered, buttonIndex);
    }

    [PunRPC]
    void OccupyButton(int buttonIndex, int playerID)
    {
        // Mark button as occupied
        occupiedButtons.Add(buttonIndex);
        buttonToPlayerID[buttonIndex] = playerID;
        
        // Hide the occupied button for all players who haven't spawned
        if (!hasSpawned && buttonIndex >= 0 && buttonIndex < spawnButtons.Length)
        {
            spawnButtons[buttonIndex].gameObject.SetActive(false);
        }
    }

    [PunRPC]
    void FreeButton(int buttonIndex)
    {
        // Mark button as free
        occupiedButtons.Remove(buttonIndex);
        buttonToPlayerID.Remove(buttonIndex);
        
        // Show the freed button for all players who haven't spawned
        if (!hasSpawned && buttonIndex >= 0 && buttonIndex < spawnButtons.Length)
        {
            spawnButtons[buttonIndex].gameObject.SetActive(true);
        }
    }

    void HideAllButtonsLocally()
    {
        // Hide all buttons only for this player
        foreach (Button btn in spawnButtons)
        {
            btn.gameObject.SetActive(false);
        }
    }

    void ShowAvailableButtons()
    {
        // Show only unoccupied buttons
        for (int i = 0; i < spawnButtons.Length; i++)
        {
            if (!occupiedButtons.Contains(i))
            {
                spawnButtons[i].gameObject.SetActive(true);
            }
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        HandlePlayerDisconnection(otherPlayer.ActorNumber);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        // When local player disconnects, free their button
        if (hasSpawned)
        {
            HandlePlayerDisconnection(PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }
    
    private void OnDestroy()
    {
        HandlePlayerDisconnection(PhotonNetwork.LocalPlayer.ActorNumber);
    }
    
    void HandlePlayerDisconnection(int disconnectedPlayerID)
    {
        // Find which button the disconnected player was occupying
        int buttonToFree = -1;
        
        foreach (var kvp in buttonToPlayerID)
        {
            if (kvp.Value == disconnectedPlayerID)
            {
                buttonToFree = kvp.Key;
                break;
            }
        }
        
        // If the disconnected player was sitting, free their button
        if (buttonToFree != -1)
        {
            FreeButton(buttonToFree); // Call directly
        }
    }

    [PunRPC]
    public void RPC_RotateSeats()
    {
        Debug.Log("🔄 RPC_RotateSeats called");
        
        // ✅ Get all existing players
        PlayerManager[] players = FindObjectsOfType<PlayerManager>();

        // ✅ Rotate seatIndices RIGHT (inverse)
        int last = seatIndices[seatIndices.Length - 1];
        for (int i = seatIndices.Length - 1; i > 0; i--)
            seatIndices[i] = seatIndices[i - 1];
        seatIndices[0] = last;

        Debug.Log("✅ New seatIndices = " + string.Join(",", seatIndices));
        
        // ✅ Save the rotated array to room properties for late joiners
        SaveSeatIndicesToRoom();

        // ✅ Update all players with their NEW seat index based on their stored button index
        foreach (var pm in players)
        {
            // Each PlayerManager should store its buttonIndex
            int buttonIdx = pm.buttonIndex; // This needs to be added to PlayerManager
            int newSeatIndex = seatIndices[buttonIdx];
            
            pm.photonView.RPC(
                "RPC_SetSeatIndex",
                RpcTarget.AllBuffered,
                newSeatIndex
            );

            Debug.Log($"✅ {pm.PlayerName} at buttonIndex {buttonIdx} -> newSeatIndex {newSeatIndex}");
        }
    }
    
    // ✅ Handle room property updates (when another client rotates seats)
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(SEAT_INDICES_KEY))
        {
            int[] newIndices = propertiesThatChanged[SEAT_INDICES_KEY] as int[];
            if (newIndices != null && newIndices.Length == seatIndices.Length)
            {
                seatIndices = newIndices;
                Debug.Log("🔄 Received updated seatIndices: " + string.Join(",", seatIndices));
            }
        }
    }
}