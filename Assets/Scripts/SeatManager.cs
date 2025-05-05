using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class SeatManager : MonoBehaviour
{
    public static SeatManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void HideSeat(int index)
    {
        // Globally hide the seat's button
        // This method is invoked via RPC from any player
        ClickSpawner spawner = FindObjectOfType<ClickSpawner>();
        if (spawner != null && index >= 0 && index < spawner.seatButtons.Length)
        {
            spawner.seatButtons[index].gameObject.SetActive(false);
            Debug.Log($"[RPC] Globally hiding seat: {index}");
        }
    }
}