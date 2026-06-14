using UnityEngine;
using UnityEngine.UI;

public class SeatButton : MonoBehaviour
{
    [Tooltip("0-based seat index for this button")]
    public int seatIndex;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
            SeatManager.Instance.OnSeatClicked(seatIndex));
    }
}