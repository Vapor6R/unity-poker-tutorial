using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections.Generic;
public class FirstInstantiation : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab; // Reference to the player prefab
    public Button spawnButton; // Reference to the button
public List<Button> buttons;
    void Start()
    {
        if (spawnButton != null)
        {
            spawnButton.onClick.AddListener(SpawnPlayer);
        }
    }

    void SpawnPlayer()
    {
        // Get the button's position
        Vector3 buttonPosition = spawnButton.transform.position;
        
        // Instantiate the player at the button's position
        PhotonNetwork.Instantiate(playerPrefab.name, buttonPosition, Quaternion.identity);
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