using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public enum Suit
{
    Hearts,
    Diamonds,
    Clubs,
    Spades
}

public enum Rank
{
 
    Two =2,
    Three=3,
    Four=4,
    Five=5,
    Six=6,
    Seven=7,
    Eight=8,
    Nine=9,
    Ten=10,
    Jack=11,
    Queen=12,
    King=13,
	Ace = 14
}

public class Card : MonoBehaviourPunCallbacks, IPunObservable
{
    public Rank rank;
    public Suit suit;
    private Image imageComponent;

    // Method to initialize the card with a rank and suit
    public void InitializeCard(Rank rank, Suit suit)
    {
        this.rank = rank;
        this.suit = suit;

        string spriteName = rank.ToString() + suit.ToString();
        Sprite cardSprite = Resources.Load<Sprite>("Cards/" + spriteName);

        if (cardSprite != null)
        {
            if (imageComponent == null)
            {
                imageComponent = gameObject.AddComponent<Image>();
            }

            imageComponent.sprite = cardSprite;
        }
        else
        {
            Debug.LogError("Sprite not found for card: " + spriteName);
        }
    }

    private void Awake()
    {
        // Ensure the GameObject has a Canvas component
        if (GetComponent<Canvas>() == null)
        {
            gameObject.AddComponent<Canvas>();
        }

        // Ensure the GameObject has a RectTransform component
        if (GetComponent<RectTransform>() == null)
        {
            gameObject.AddComponent<RectTransform>();
        }

        imageComponent = GetComponent<Image>();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // If this is the owner of the card (you), send the data
            stream.SendNext(rank);
            stream.SendNext(suit);
            stream.SendNext(imageComponent.sprite != null ? imageComponent.sprite.name : "");
        }
        else
        {
            // If this is another client, receive the data
            rank = (Rank)stream.ReceiveNext();
            suit = (Suit)stream.ReceiveNext();
            string spriteName = (string)stream.ReceiveNext();

            if (!string.IsNullOrEmpty(spriteName))
            {
                Sprite cardSprite = Resources.Load<Sprite>("Cards/" + spriteName);
                if (cardSprite != null)
                {
                    if (imageComponent == null)
                    {
                        imageComponent = gameObject.AddComponent<Image>();
                    }

                    imageComponent.sprite = cardSprite;
                }
                else
                {
                    Debug.LogError("Sprite not found for card: " + spriteName);
                }
            }
        }
    }
}
