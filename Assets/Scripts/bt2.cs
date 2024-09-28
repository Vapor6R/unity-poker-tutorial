using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
public class bt2 : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab; // Reference to the player prefab
    public Button spawnButton; // Reference to the button
public List<Button> buttons;
 public GameManager deckInstance;
public RectTransform button1;
    public RectTransform button2;
	public RectTransform button3;
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

    void OnButton2Click()
    {
        
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
        Vector3 button1Position = button1.position;
        Vector3 button2Position = button2.position;
Vector3 button3Position = button3.position;
        // Instantiate a new player at button1's position
        PhotonNetwork.Instantiate(playerPrefab.name, button3Position, Quaternion.identity);
        Debug.Log("Player instantiated at: " + button1Position);
 hasInstantiated = true;
        // Swap button positions
        button1.position = button2Position;
        button2.position = button1Position;

   // Prevent multiple instantiations
        Debug.Log("Button positions swapped!"); 
       
	   Vector3 buttonPosition = spawnButton.transform.position;
        
        // Instantiate the player at the button's position
         
		 
deckInstance.photonView.RPC("StartSit", RpcTarget.MasterClient);

           
       

		photonView.RPC("HideButtonRPC", RpcTarget.AllBuffered);
		
				HideButtons();

	
    }
	[PunRPC]
    void HideButtonRPC()
    {
        if (spawnButton != null)
        {
            spawnButton.gameObject.SetActive(false);
        }
    }

    void HideButtons()
    {
        foreach (Button button in buttons)
        {
            button.gameObject.SetActive(false);
        }
    }
	
}