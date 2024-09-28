using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    public float moveSpeed = 5f; // Player movement speed

    private Vector3 networkPosition; // Used for position synchronization
    private Quaternion networkRotation; // Used for rotation synchronization

    void Start()
    {
        if (photonView.IsMine)
        {
            // Initialize the player if it's controlled by the local player
            InitializePlayer();
        }
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            // Handle player input and movement if it's controlled by the local player
            HandleMovement();
        }
        else
        {
            // Smoothly interpolate the position and rotation for remote players
            SmoothNetworkMovement();
        }
    }

    void InitializePlayer()
    {
        // Additional initialization for the local player
        // For example, you might want to set up a camera follow script
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;

        transform.Translate(movement, Space.World);
        
        // Optionally update the position across the network
        photonView.RPC("UpdatePosition", RpcTarget.Others, transform.position);
    }

    void SmoothNetworkMovement()
    {
        // Smoothly interpolate the position and rotation of remote players
        transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
        transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, Time.deltaTime * 10f);
    }

    [PunRPC]
    void UpdatePosition(Vector3 newPosition)
    {
        networkPosition = newPosition;
    }

    [PunRPC]
    void UpdateRotation(Quaternion newRotation)
    {
        networkRotation = newRotation;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send position and rotation data to the network
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // Receive position and rotation data from the network
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
        }
    }
}
