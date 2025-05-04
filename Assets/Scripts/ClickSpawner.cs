using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System;
using System.Collections;
public class ClickSpawner : MonoBehaviourPun
{public int seatNumber;
    public GameObject objectPrefab;  // Assign the prefab in Inspector
    public Button[] seatButtons;     // Assign all buttons in the Inspector
    private PhotonView photonView;
    private int buttonIndex; // Stores the button's index
private PlayerManager playerManager;
    void Start()
    {
        photonView = GetComponent<PhotonView>(); // Get PhotonView component

        // Get the index of this button in the array
        buttonIndex = System.Array.IndexOf(seatButtons, GetComponent<Button>());
if (buttonIndex != -1)
        {
            GetComponent<Button>().onClick.AddListener(OnButtonClick);
        }
        else
        {
            Debug.LogError("Button not found in seatButtons array!");
        }
    }

   void OnButtonClick()
    {
        Debug.Log($"[CLICKED] Button Index: {buttonIndex}, Seat Number: {seatNumber}");

        if (PhotonNetwork.IsConnectedAndReady)
        {
            GameObject playerObj = PhotonNetwork.Instantiate(objectPrefab.name, transform.position, Quaternion.identity);

            // 🔥 Key part: Get PlayerManager from spawned object
            PlayerManager playerManager = playerObj.GetComponent<PlayerManager>();
            if (playerManager != null)
            {
               playerManager.photonView.RPC("SetCurrentSeat", RpcTarget.AllBuffered, seatNumber);
                Debug.Log("[OK] Assigned seat number: " + seatNumber);
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

        photonView.RPC("HideSeatForAll", RpcTarget.AllBuffered, buttonIndex);
        HideOtherSeatsForLocal();

    }
public IEnumerator AssignSeat()
{ yield return new WaitForSeconds(2f); // Adjust the delay time as needed
	{
}
}
    [PunRPC]
    void HideSeatForAll(int index)
    {
        if (index >= 0 && index < seatButtons.Length)
        {
            seatButtons[index].gameObject.SetActive(false);
        }
    }

    void HideOtherSeatsForLocal()
    {
        foreach (var button in seatButtons)
        {
            if (buttonIndex != System.Array.IndexOf(seatButtons, button)) // Don't hide the clicked button twice
            {
                button.gameObject.SetActive(false);
            }
        }
    }
}
