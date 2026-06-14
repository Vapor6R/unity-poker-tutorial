using UnityEngine;
using Photon.Pun;
using System.Collections;
using UnityEngine.SceneManagement;

public class Loader : MonoBehaviourPunCallbacks
{
    void Awake()
    {
    //    PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void Load()
    {
        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        PhotonNetwork.LoadLevel("Room");

        // Wait until new scene is fully loaded
        yield return new WaitUntil(() =>
            SceneManager.GetActiveScene().name == "Room");

        yield return null; // extra frame so new scene objects initialize

        // NOW destroy OLD tagged objects
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("OLD"))
        {
            Debug.Log($"[Loader] Destroying OLD: {obj.name}");
            Destroy(obj);
        }
    }
}