using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Collections;

public class DeckManager : MonoBehaviourPun
{
    public static DeckManager Instance;

    public Sprite cardBackSprite;
    public Transform communityCardsAnchor;
    public GameObject cardPrefab;
    public float cardSpacing = 1.2f;

    public List<string> communityCards = new List<string>();
    public List<GameObject> communityCardObjects = new List<GameObject>();
    public List<string> deck = new List<string>();

    // ONE sprite cache only
    private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // ← destroy duplicate
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSprites();
        BuildDeck();
    }
    // ── Sprite lookup ────────────────────────────────────────────────────────
    void LoadSprites()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("Cards");
        foreach (var s in sprites)
        {
            spriteCache[s.name] = s;
            Debug.Log($"Loaded sprite: '{s.name}'");
        }
        Debug.Log("Total loaded: " + spriteCache.Count);
    }
    public Sprite GetSprite(string card)
    {
        // Try exact match first
        if (spriteCache.TryGetValue(card, out var s)) return s;

        // Try with .png extension
        if (spriteCache.TryGetValue(card + ".png", out s)) return s;

        // Try lowercase
        if (spriteCache.TryGetValue(card.ToLower(), out s)) return s;

        Debug.LogError($"Sprite not found for: '{card}' — cache has {spriteCache.Count} entries");
        return cardBackSprite;
    }

    

    // ── Deck ─────────────────────────────────────────────────────────────────

    public void BuildDeck()
    {
        deck.Clear();

        string[] suits = { "S", "H", "D", "C" };
        string[] ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

        foreach (var s in suits)
            foreach (var r in ranks)
                deck.Add(r + s);

        Shuffle();
    }

    void Shuffle()
    {
        // Fisher-Yates with a cryptographic random seed for true randomness
        byte[] seedBytes = new byte[4];
        new System.Security.Cryptography.RNGCryptoServiceProvider().GetBytes(seedBytes);
        System.Random rng = new System.Random(System.BitConverter.ToInt32(seedBytes, 0));

        int n = deck.Count;
        while (n > 1)
        {
            int k = rng.Next(n--);
            (deck[n], deck[k]) = (deck[k], deck[n]);
        }
    }

    void BurnCard()
    {
        if (deck.Count == 0) return;
        deck.RemoveAt(0);
    }

    // ── Deal hole cards ───────────────────────────────────────────────────────
    public void DealCards()
{
    if (!PhotonNetwork.IsMasterClient) return;

    PlayerManager[] players = FindObjectsOfType<PlayerManager>();

    // Sort by seatIndex for consistent deal order across all clients
    System.Array.Sort(players, (a, b) => a.seatIndex.CompareTo(b.seatIndex));

    foreach (var p in players)
    {
        if (deck.Count < 2)
        {
            Debug.LogError("Not enough cards in deck to deal!");
            return;
        }

        string c1 = deck[0]; deck.RemoveAt(0);
        string c2 = deck[0]; deck.RemoveAt(0);

        Debug.Log($"Dealing to seat {p.seatIndex}: {c1} {c2} — deck remaining: {deck.Count}");

        p.photonView.RPC("ReceiveCards", RpcTarget.All, c1, c2);
        p.photonView.RPC("RPC_SetInGame", RpcTarget.All, true);
    }
}
    // ── Deal community cards ──────────────────────────────────────────────────

    public void DealFlop()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        BurnCard();
        DealCommunityCards(3);
    }

    public void DealTurn()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        BurnCard();
        DealCommunityCards(1);
    }

    public void DealRiver()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        BurnCard();
        DealCommunityCards(1);
    }

    void DealCommunityCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0) return;
            string card = deck[0];
            deck.RemoveAt(0);
            communityCards.Add(card);
        }

        int startIndex = communityCards.Count - count;
        for (int i = 0; i < count; i++)
        {
            string card = communityCards[startIndex + i];
            int slotIndex = startIndex + i;

            // master spawns locally
            SpawnCommunityCard(slotIndex, card);

            // client spawns via RPC
            photonView.RPC("RPC_SetCommunityCard", RpcTarget.Others, slotIndex, card);
        }
    }

    [PunRPC]
    void RPC_SetCommunityCard(int slotIndex, string card)
    {
        SpawnCommunityCard(slotIndex, card);
    }

    void SpawnCommunityCard(int slotIndex, string card)
    {
        Vector3 pos = communityCardsAnchor.position + Vector3.right * slotIndex * cardSpacing;
        GameObject obj = Instantiate(cardPrefab, pos, Quaternion.identity);
        communityCardObjects.Add(obj);
        StartCoroutine(SetCardSprite(obj, card));

    }

    // ── Shared sprite setter ──────────────────────────────────────────────────

    public static IEnumerator SetCardSprite(GameObject obj, string card)
    {
        SpriteRenderer sr = null;
        int frames = 0;

        while (sr == null)
        {
            yield return null;
            sr = obj.GetComponentInChildren<SpriteRenderer>();
            if (++frames > 100) { Debug.LogError("SpriteRenderer never found!"); yield break; }
        }

        Sprite sprite = Instance.GetSprite(card);
        if (sprite == null) { Debug.LogError($"No sprite for {card}"); yield break; }

        sr.sprite = sprite;
        Debug.Log($"Card set: {card} → {sprite.name}");
    }

    // ── Winning-card highlight ────────────────────────────────────────────────

    /// <summary>
    /// Called by ShowdownManager immediately after evaluation.
    /// Pops community cards that are part of the winning 5-card hand;
    /// dims the ones that aren't.
    /// </summary>
    [PunRPC]
    public void RPC_HighlightCommunityCards(string[] bestCards)
    {
        var bestSet = new HashSet<string>(bestCards);

        for (int i = 0; i < communityCards.Count && i < communityCardObjects.Count; i++)
        {
            GameObject obj = communityCardObjects[i];
            if (obj == null) continue;

            bool inBest = bestSet.Contains(communityCards[i]);
            StartCoroutine(PopCommunityCard(obj, inBest));
        }
    }

    private IEnumerator PopCommunityCard(GameObject cardObj, bool isWinner)
    {
        if (cardObj == null) yield break;

        SpriteRenderer sr = cardObj.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) sr = cardObj.GetComponent<SpriteRenderer>();

        Vector3 originalScale = cardObj.transform.localScale;
        Vector3 originalPos   = cardObj.transform.localPosition;

        if (isWinner)
        {
            Vector3 targetScale = originalScale * 1.35f;
            Vector3 targetPos   = originalPos + new Vector3(0f, 0.25f, 0f);
            Color   goldTint    = new Color(1f, 0.88f, 0.2f, 1f);

            float duration = 0.25f, elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                cardObj.transform.localScale    = Vector3.Lerp(originalScale, targetScale, t);
                cardObj.transform.localPosition = Vector3.Lerp(originalPos,   targetPos,   t);
                if (sr != null) sr.color = Color.Lerp(Color.white, goldTint, t);
                yield return null;
            }

            yield return new WaitForSeconds(3f);

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                cardObj.transform.localScale    = Vector3.Lerp(targetScale, originalScale, t);
                cardObj.transform.localPosition = Vector3.Lerp(targetPos,   originalPos,   t);
                yield return null;
            }

            cardObj.transform.localScale    = originalScale;
            cardObj.transform.localPosition = originalPos;
        }
        else
        {
            // Not part of the best hand — dim it
            if (sr != null) sr.color = new Color(0.55f, 0.55f, 0.55f, 1f);
        }
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    public void ResetCommunityCards()
    {
        foreach (var obj in communityCardObjects)
            Destroy(obj);
        communityCardObjects.Clear();
        communityCards.Clear();
    }
}