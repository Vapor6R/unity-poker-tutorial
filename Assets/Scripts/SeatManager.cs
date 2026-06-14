using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class SeatManager : MonoBehaviourPunCallbacks
{
    public static SeatManager Instance;

    [Header("Seats (assign in Inspector, index 0-8)")]
    public GameObject[] seatButtons;       // The clickable seat UI buttons
    public Transform[] seatSpawnPoints;    // Where the player prefab spawns per seat
[Header("Stand Up Button")]
public GameObject standUpButton;
    [Header("Player Prefab")]
    public string playerPrefabName = "PlayerAvatar"; // Must be in Resources/

    // seatIndex -> actorNumber of occupying player (synced via CustomProperties)
    private const string SEAT_PROP_PREFIX = "Seat_";

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        RefreshSeatsLocally();
    }
// ─── Stand Up: free the seat and destroy the local player object ──────────
public void StandUp()
{
    int seatIndex = GetLocalPlayerSeat();
    if (seatIndex == -1) return;

    // Destroy the local player's networked object
    if (PhotonNetwork.LocalPlayer.TagObject is GameObject playerObj && playerObj != null)
    {
        PhotonNetwork.Destroy(playerObj);
        PhotonNetwork.LocalPlayer.TagObject = null;
    }

    // Clear seat ownership from Room Properties
    string key = SEAT_PROP_PREFIX + seatIndex;
    ExitGames.Client.Photon.Hashtable props =
        new ExitGames.Client.Photon.Hashtable { { key, null } };
    PhotonNetwork.CurrentRoom.SetCustomProperties(props);

    // Clear player's own SeatIndex property
    ExitGames.Client.Photon.Hashtable playerProps =
        new ExitGames.Client.Photon.Hashtable { { "SeatIndex", -1 } };
    PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

    // Refresh locally immediately (don't wait for callback)
    RefreshSeatsLocally();
}

// ─── Call this from StandUpButton.cs ─────────────────────────────────────
public void OnStandUpClicked()
{
    StandUp();
}
    public void RefreshSeatsLocally()
{
    bool localIsSeated = GetLocalPlayerSeat() != -1;

    for (int i = 0; i < seatButtons.Length; i++)
    {
        if (seatButtons[i] == null) continue;

        if (localIsSeated)
        {
            seatButtons[i].SetActive(false);
        }
        else
        {
            seatButtons[i].SetActive(!IsSeatOccupied(i));
        }
    }

    // Show/hide the stand-up button
    if (standUpButton != null)
        standUpButton.SetActive(localIsSeated);
}
    public void OnSeatClicked(int seatIndex)
    {
        // Ignore if local player already seated
        if (GetLocalPlayerSeat() != -1) return;

        // Ignore if seat is already taken
        if (IsSeatOccupied(seatIndex)) return;

        // Claim the seat via Room Custom Properties
        ClaimSeat(seatIndex);
    }

    // ─── Claim seat: write to Room Properties ───────────────────────────────
    void ClaimSeat(int seatIndex)
    {
        string key = SEAT_PROP_PREFIX + seatIndex;

        // Optimistic lock: only set if key is currently empty/null
        ExitGames.Client.Photon.Hashtable expectedProps = new ExitGames.Client.Photon.Hashtable
        {
            { key, null }
        };
        ExitGames.Client.Photon.Hashtable newProps = new ExitGames.Client.Photon.Hashtable
        {
            { key, PhotonNetwork.LocalPlayer.ActorNumber }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(newProps, expectedProps);
        // Result handled in OnRoomPropertiesUpdate
    }

    // ─── Called on ALL clients when room properties change ──────────────────
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable changedProps)
    {
        foreach (var key in changedProps.Keys)
        {
            string k = key.ToString();
            if (!k.StartsWith(SEAT_PROP_PREFIX)) continue;

            int seatIndex = int.Parse(k.Replace(SEAT_PROP_PREFIX, ""));
            object val = changedProps[key];

            if (val != null)
            {
                int actorNumber = (int)val;

                // If this is MY seat claim being confirmed → spawn
                if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber
                    && GetLocalPlayerSeat() == seatIndex)
                {
                    SpawnLocalPlayer(seatIndex);
                }
            }
        }

        RefreshSeatsLocally();
    }

    // ─── Spawn the local player at the given seat ────────────────────────────
    void SpawnLocalPlayer(int seatIndex)
    {
        // Prevent double-spawn
        if (PhotonNetwork.LocalPlayer.TagObject != null) return;

        Vector3 pos = seatSpawnPoints[seatIndex].position;
        Quaternion rot = seatSpawnPoints[seatIndex].rotation;

        GameObject player = PhotonNetwork.Instantiate(
            playerPrefabName, pos, rot
        );

        // Tag so we don't spawn twice
        PhotonNetwork.LocalPlayer.TagObject = player;

        // Store which seat this player owns (for rejoin/refresh)
        ExitGames.Client.Photon.Hashtable playerProps =
            new ExitGames.Client.Photon.Hashtable { { "SeatIndex", seatIndex } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
    }

    

    // ─── Helpers ─────────────────────────────────────────────────────────────
    public bool IsSeatOccupied(int seatIndex)
    {
        string key = SEAT_PROP_PREFIX + seatIndex;
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        return props.ContainsKey(key) && props[key] != null;
    }

    /// Returns the seat index the local player owns, or -1 if none.
    public int GetLocalPlayerSeat()
    {
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        for (int i = 0; i < 9; i++)
        {
            string key = SEAT_PROP_PREFIX + i;
            if (props.ContainsKey(key) && props[key] is int actor
                && actor == PhotonNetwork.LocalPlayer.ActorNumber)
                return i;
        }
        return -1;
    }

    // ─── Free seat when player leaves ────────────────────────────────────────
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        FreeSeatOfActor(otherPlayer.ActorNumber);
    }

    void FreeSeatOfActor(int actorNumber)
    {
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        ExitGames.Client.Photon.Hashtable toSet =
            new ExitGames.Client.Photon.Hashtable();

        for (int i = 0; i < 9; i++)
        {
            string key = SEAT_PROP_PREFIX + i;
            if (props.ContainsKey(key) && props[key] is int a && a == actorNumber)
                toSet[key] = null;
        }

        if (toSet.Count > 0)
            PhotonNetwork.CurrentRoom.SetCustomProperties(toSet);

        RefreshSeatsLocally();
    }

    // ─── Also free own seat on disconnect ────────────────────────────────────
    public override void OnDisconnected(DisconnectCause cause)
    {
        // Room props are cleaned server-side when player leaves,
        // but OnPlayerLeftRoom handles it for others.
    }
}