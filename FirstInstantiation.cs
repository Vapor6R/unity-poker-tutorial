using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class FirstInstantiation : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab; // Reference to the player prefab
    public Button spawnButton; // Reference to the spawn button
    public List<Button> buttons; // List of buttons
    public GameManager deckInstance; // Reference to the GameManager
    public Button button2; // Reference to an additional button
    public int playerNumber; // Player number
    public Camera mainCamera; // Reference to the main camera
    public Canvas canvas; // Reference to the canvas
public Button standUpButton;
private GameObject playerInstance;
public PlayerPosition playerPosition;
private PlayerCardHandler playerCardHandler;
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // Add logic here for when a new player enters the room
    }

    void Start()
    {
        if (spawnButton != null)
        {
            spawnButton.onClick.AddListener(SpawnPlayer);
        }
		 if (standUpButton != null)
        {
            standUpButton.onClick.AddListener(OnStandUpClick);
            standUpButton.gameObject.SetActive(false); // Initially hidden
        }
    }

    public void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab is not assigned!");
            return;
        }

        if (deckInstance == null)
        {
            Debug.LogError("Deck Instance is not assigned!");
            return;
        }

        // Ensure canvas uses the correct camera
        canvas.worldCamera = mainCamera;

        // Use the correct position for the spawn button
        Vector3 spawnPosition = spawnButton.transform.position;
        Quaternion spawnRotation = Quaternion.Euler(0, 0, 180);

        // Instantiate the player
        playerInstance = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, spawnRotation, 0);
playerCardHandler = playerInstance.GetComponent<PlayerCardHandler>();
        if (playerCardHandler != null)
        {
            playerPosition = playerCardHandler.playerPosition;
            Debug.Log($"Player assigned to position: {playerPosition}");
        } 
        else
        {
            Debug.LogError("PlayerCardHandler component not found on player.");
        }
		playerInstance.transform.position = button2.transform.position;

        // Rotate the camera
        RotateCamera();

        if(PhotonNetwork.CurrentRoom.PlayerCount==2){
        deckInstance.photonView.RPC("StartSit", RpcTarget.MasterClient);}

        // Handle button visibility and logic
        OnButton1Click();
		standUpButton.gameObject.SetActive(true);
    }

    private void RotateCamera()
    {
        // Rotate the Camera by 180 degrees on the Z-axis
        mainCamera.transform.rotation = Quaternion.Euler(0, 0, 180);
    }
void OnStandUpClick()
    {
       if (playerInstance != null)
    {
        // Get the PlayerCardHandler component if it's not already assigned
        if (playerCardHandler == null)
        {
            playerCardHandler = playerInstance.GetComponent<PlayerCardHandler>();
        }

        // Check if playerCardHandler is still null after trying to assign it
        if (playerCardHandler != null)
        {
            // Store the previous position of the player
            playerCardHandler.StorePreviousPosition();

            // Destroy the player instance
            PhotonNetwork.Destroy(playerInstance);
            playerInstance = null;

            // Reinstate the player's position back into the available positions list
            playerCardHandler.ReinstatePosition();

            // Hide the stand-up button and show the spawn button again
            standUpButton.gameObject.SetActive(false);
            spawnButton.gameObject.SetActive(true);

            // Show all buttons again
            foreach (Button button in buttons)
            {
                button.gameObject.SetActive(true);
                Debug.Log($"Re-showing button: {button.name}");
            }
        }
        else
        {
            Debug.LogError("PlayerCardHandler component is missing from the player instance.");
        }
    }
    else
    {
        Debug.LogError("Player instance is null.");
    }
    }
    [PunRPC]
    void HideButtonRPC()
    {
        if (spawnButton != null)
        {
            spawnButton.gameObject.SetActive(false);
            Debug.Log($"Hiding button: {spawnButton.name}");
        }
    }

    void HideButtons()
    {
        foreach (Button button in buttons)
        {
            button.gameObject.SetActive(false);
            Debug.Log($"Hiding button: {button.name}");
        }
    }

    void OnButton1Click()
    {
        HideButtons();
        photonView.RPC("HideButtonRPC", RpcTarget.AllBuffered);

        Debug.Log("Buttons have been swapped and hidden.");
    }
}
