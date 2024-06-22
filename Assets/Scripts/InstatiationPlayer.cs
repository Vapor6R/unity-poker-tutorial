using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class InstatiationPlayer : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;  // Declare the playerPrefab
    public Transform[] spawnPoints;  // Ensure this matches the array name in Start()

    void Start()
    {if (PhotonNetwork.IsMasterClient)
		{
       if (GameObject.FindWithTag("Canvas") == null)
            {
                // Determine spawn index based on the actor number
                int spawnIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
                if (spawnIndex >= spawnPoints.Length)
                {
                    spawnIndex = 0;  // If actor number exceeds spawnPoints array length, default to the first spawn point
                }

                // Instantiate the playerPrefab at the chosen spawn point
                PhotonNetwork.Instantiate(playerPrefab.name, spawnPoints[spawnIndex].position, spawnPoints[spawnIndex].rotation, 0);
            }
    }
        }

}
