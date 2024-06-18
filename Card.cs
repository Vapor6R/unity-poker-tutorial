using UnityEngine;
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
    Ace = 1,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King
}

public class Card : MonoBehaviourPunCallbacks, IPunObservable
{
    public Rank rank;
    public Suit suit;
    private SpriteRenderer spriteRenderer;

    // Method to initialize the card with a rank and suit
    public void InitializeCard(Rank rank, Suit suit)
    {
        this.rank = rank;
        this.suit = suit;

        string spriteName = rank.ToString() + suit.ToString();
        Sprite cardSprite = Resources.Load<Sprite>("Cards/" + spriteName);

        if (cardSprite != null)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = cardSprite;
        }
        else
        {
            Debug.LogError("Sprite not found for card: " + spriteName);
        }
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // If this is the owner of the card (you), send the data
            stream.SendNext(rank);
            stream.SendNext(suit);
            stream.SendNext(spriteRenderer.sprite != null ? spriteRenderer.sprite.name : "");
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
                    if (spriteRenderer == null)
                    {
                        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                    }

                    spriteRenderer.sprite = cardSprite;
                }
                else
                {
                    Debug.LogError("Sprite not found for card: " + spriteName);
                }
            }
        }
    }

    public void SynchronizeCardInitialization(Rank syncedRank, Suit syncedSuit)
    {
        rank = syncedRank;
        suit = syncedSuit;
        InitializeCard(rank, suit);
    }
}