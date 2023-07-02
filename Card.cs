using UnityEngine;

public class Card : MonoBehaviour
{
    public int rank;
    private int suit;
    private SpriteRenderer spriteRenderer;

    public void SetRankSuit(int rank, int suit)
    {
        this.rank = rank;
        this.suit = suit;
    }

    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = sprite;
    }

    public int GetRank()
    {
        return rank;
    }

    public int GetSuit()
    {
        return suit;
    }
}
