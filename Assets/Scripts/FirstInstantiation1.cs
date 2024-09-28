using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
public class FirstInstantiation1 : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab; // Reference to the player prefab
    public Button spawnButton; // Reference to the button
public List<Button> buttons;
 public GameManager deckInstance;

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
            
       
	   Vector3 buttonPosition = spawnButton.transform.position;
        
        // Instantiate the player at the button's position
         PhotonNetwork.Instantiate(playerPrefab.name, buttonPosition, Quaternion.identity);
		 
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