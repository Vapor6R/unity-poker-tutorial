using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
public class PhotonConnect : MonoBehaviourPunCallbacks
{
    [Header("Scene Names")]
    public string lobbyScene = "Room";

    public void StartManually()
    {
       PhotonNetwork.AutomaticallySyncScene = true;

        // 🔥 Force EU region
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "eu";

        Debug.Log("Connecting to Photon (EU)...");
        PhotonNetwork.ConnectUsingSettings();
    }

    // ---------------- CONNECT ----------------

    public override void OnConnectedToMaster()
    {
        Debug.Log("✅ Connected to Master");

        PhotonNetwork.NickName = "Player_" + Random.Range(1000, 9999);

        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("✅ Joined Lobby");

        PhotonNetwork.JoinRandomRoom();
    }

    // ---------------- ROOM LOGIC ----------------

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No room found → Creating new room");

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 9;

        PhotonNetwork.CreateRoom(null, options);
    }

   public override void OnJoinedRoom()
{
    Debug.Log("✅ Joined Room");
    if (PhotonNetwork.IsMasterClient)
    {
        Debug.Log("Master loading Lobby scene...");
        PhotonNetwork.LoadLevel(lobbyScene);
        StartCoroutine(DestroyOldAfterLoad());
    }
}

private IEnumerator DestroyOldAfterLoad()
{
    yield return new WaitUntil(() =>
        SceneManager.GetActiveScene().name == lobbyScene);

    yield return null; // extra frame so new scene objects initialize

    foreach (GameObject obj in GameObject.FindGameObjectsWithTag("OLD"))
    {
		if (obj.CompareTag("DD"))
        {
            DontDestroyOnLoad(obj);
            continue;
        }
        Debug.Log($"[Loader] Destroying OLD: {obj.name}");
        Destroy(obj);
    }
}
    // ---------------- DISCONNECT ----------------

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("❌ Disconnected: " + cause);
    }
}