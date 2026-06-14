using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    [Header("UI OR SPRITE")]
    public Image uiImage;
    public SpriteRenderer spriteRenderer;

    private string cardValue;

    public void SetCard(string card)
    {
        cardValue = card;
        UpdateVisual();
    }

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (uiImage == null)
            uiImage = GetComponentInChildren<Image>();
    }

    void UpdateVisual()
    {
        SetSprite(DeckManager.Instance.GetSprite(cardValue));
    }

    void SetSprite(Sprite s)
    {
        if (s == null) Debug.LogError("Sprite is NULL for card: " + cardValue);

        if (spriteRenderer != null)
            spriteRenderer.sprite = s;
        else
            Debug.LogError("SpriteRenderer is NULL on: " + gameObject.name);

        if (uiImage != null)
            uiImage.sprite = s;
        else
            Debug.LogError("uiImage is NULL on: " + gameObject.name);
    }
}