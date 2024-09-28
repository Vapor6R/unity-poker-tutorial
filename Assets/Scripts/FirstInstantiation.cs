using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
public class FirstInstantiation : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab; // Reference to the player prefab
    public Button spawnButton; // Reference to the button
public List<Button> buttons;
 public GameManager deckInstance;
    public Button button1; // Assign Button1 in the Inspector
    public Button button2;
	private bool hasInstantiated = false;
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
		
    }

	void Start()
    {
        if (spawnButton != null)
        {
            spawnButton.onClick.AddListener(SpawnPlayer);
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
           if (hasInstantiated)
        {
            Debug.Log("Button2 click already handled. Ignoring.");
            return;
        }

        // Get positions of buttons
        Vector3 buttonPosition = button1.transform.position;


        // Instantiate a new player at button1's position
        GameObject player = PhotonNetwork.Instantiate(playerPrefab.name, buttonPosition, Quaternion.identity);
        Debug.Log("Player instantiated at: " + buttonPosition);
 hasInstantiated = true;

   // Prevent multiple instantiations
        Debug.Log("Button positions swapped!"); 
		 
deckInstance.photonView.RPC("StartSit", RpcTarget.MasterClient);

           
		Vector3 button2Position = button2.transform.position;
        player.transform.position = button2Position;
		OnButton1Click();

	
    }
	[PunRPC]
    void HideButtonRPC()
    {
        if (spawnButton != null)
        {
            spawnButton.gameObject.SetActive(false);
			Debug.Log($"Hiding button: {spawnButton}");
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
       Vector3 button1Position = button1.transform.position;

    // Swap positions first
    button1.transform.position = button2.transform.position;
    button2.transform.position = button1Position;

    // Log the new positions
    Debug.Log($"Button1 new position: {button1.transform.position}");
    Debug.Log($"Button2 new position: {button2.transform.position}");

    // Hide buttons locally first
    HideButtons();

    // Now call the RPC to ensure all clients hide their buttons as well
    photonView.RPC("HideButtonRPC", RpcTarget.AllBuffered);

    Debug.Log("Buttons have been swapped and hidden.");
}
}