using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to your profile panel GameObject.
/// Call Open() from your avatar button's OnClick.
/// </summary>
public class PlayerProfilePanel : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button     closeButton;
    [SerializeField] private Button     avatarButton;       // the avatar on the HUD that opens this

    [Header("Identity")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI countryFlagText;  // emoji flag via TMP
    [SerializeField] private TextMeshProUGUI countryNameText;
    [SerializeField] private TextMeshProUGUI referralCodeText;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Gender")]
    [SerializeField] private Button          maleButton;
    [SerializeField] private Button          femaleButton;
    [SerializeField] private Image           maleButtonBg;
    [SerializeField] private Image           femaleButtonBg;
    [SerializeField] private Color           genderActiveColor   = new Color(0.12f, 0.62f, 0.46f, 1f);
    [SerializeField] private Color           genderInactiveColor = new Color(0.2f,  0.2f,  0.2f, 0.3f);

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI balanceText;
    [SerializeField] private TextMeshProUGUI handsText;
    [SerializeField] private TextMeshProUGUI biggestPotText;
    [SerializeField] private TextMeshProUGUI winPercentText;
    [SerializeField] private TextMeshProUGUI jackpotText;
    [SerializeField] private TextMeshProUGUI bjpText;

    [Header("Best Hand")]
    [SerializeField] private TextMeshProUGUI bestHandNameText;
    [SerializeField] private Transform       bestHandCardsParent;  // horizontal layout group
    [SerializeField] private GameObject      cardPrefab;           // small card UI prefab
    [SerializeField] private Sprite[]        cardSprites;          // 52 card sprites, named "AS","KH", etc.

    // ── Private ──────────────────────────────────────────────────

    private readonly List<GameObject> _spawnedCards = new();

    // ── Unity lifecycle ──────────────────────────────────────────

    private void Awake()
    {
        if (closeButton)  closeButton.onClick.AddListener(Close);
        if (avatarButton) avatarButton.onClick.AddListener(Open);
        if (maleButton)   maleButton.onClick.AddListener(OnMaleClicked);
        if (femaleButton) femaleButton.onClick.AddListener(OnFemaleClicked);

        panelRoot?.SetActive(false);
    }

    private void Start()
    {
        StartCoroutine(WaitAndBind());
    }

    private IEnumerator WaitAndBind()
    {
        yield return new WaitUntil(() =>
            PlayerProfileManager.Instance != null &&
            PlayerProfileManager.Instance.IsReady);
    }

    // ── Open / Close ─────────────────────────────────────────────

    public void Open()
    {
        if (PlayerProfileManager.Instance == null || !PlayerProfileManager.Instance.IsReady) return;
        Populate(PlayerProfileManager.Instance.Profile);
        panelRoot?.SetActive(true);
    }

    public void Close()
    {
        panelRoot?.SetActive(false);
    }

    // ── Populate ─────────────────────────────────────────────────

    private void Populate(PlayerProfileManager.PlayerProfile p)
    {
        if (p == null) return;

        // Identity
        if (nameText)        nameText.text        = p.displayName;
        if (referralCodeText)referralCodeText.text = $"# {p.referralCode}";
        if (levelText)       levelText.text        = $"Level  {p.level}";

        // Country
        string flag = CountryToFlag(p.country);
        string name = CountryToName(p.country);
        if (countryFlagText) countryFlagText.text = flag;
        if (countryNameText) countryNameText.text = name;

        // Gender buttons
        RefreshGenderButtons(p.gender);

        // Stats
        if (balanceText)    balanceText.text    = ChipFormatter.Format(p.balance);
        if (handsText)      handsText.text       = p.handsPlayed.ToString("N0");
        if (biggestPotText) biggestPotText.text  = ChipFormatter.Format(p.biggestPot);
        if (winPercentText) winPercentText.text  = $"{p.winPercent:F1}%";
        if (jackpotText)    jackpotText.text     = ChipFormatter.Format(p.jackpotWon);
        if (bjpText)        bjpText.text         = p.bjpWon.ToString();

        // Best hand
        if (bestHandNameText) bestHandNameText.text = p.bestHand;
        SpawnBestHandCards(p.bestHandCards);
    }

    // ── Gender ───────────────────────────────────────────────────

    private void OnMaleClicked()
    {
        PlayerProfileManager.Instance?.SetGender("male");
        RefreshGenderButtons("male");
    }

    private void OnFemaleClicked()
    {
        PlayerProfileManager.Instance?.SetGender("female");
        RefreshGenderButtons("female");
    }

    private void RefreshGenderButtons(string gender)
    {
        bool isMale = gender == "male";
        if (maleButtonBg)   maleButtonBg.color   = isMale  ? genderActiveColor : genderInactiveColor;
        if (femaleButtonBg) femaleButtonBg.color  = !isMale ? genderActiveColor : genderInactiveColor;
    }

    // ── Best hand cards ──────────────────────────────────────────

    private void SpawnBestHandCards(string cards)
    {
        foreach (var go in _spawnedCards) Destroy(go);
        _spawnedCards.Clear();

        if (bestHandCardsParent == null || cardPrefab == null) return;
        if (string.IsNullOrEmpty(cards)) return;

        string[] tokens = cards.Split(' ');
        foreach (string token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token)) continue;
            GameObject card = Instantiate(cardPrefab, bestHandCardsParent);
            Image img = card.GetComponent<Image>();
            if (img != null)
            {
                Sprite sp = FindCardSprite(token.Trim());
                if (sp) img.sprite = sp;
            }
            _spawnedCards.Add(card);
        }
    }

    private Sprite FindCardSprite(string code)
    {
        if (cardSprites == null) return null;
        foreach (Sprite sp in cardSprites)
            if (sp.name.Equals(code, System.StringComparison.OrdinalIgnoreCase))
                return sp;
        return null;
    }

    // ── Country helpers ──────────────────────────────────────────

    /// <summary>Converts ISO 3166-1 alpha-2 code to emoji flag.</summary>
private static string CountryToFlag(string iso2)
{
    if (string.IsNullOrEmpty(iso2) || iso2 == "XX") return "🏳";
    iso2 = iso2.ToUpper();
    int a = iso2[0] - 'A' + 0x1F1E6;
    int b = iso2[1] - 'A' + 0x1F1E6;
    return char.ConvertFromUtf32(a) + char.ConvertFromUtf32(b);
}

private static string CountryToName(string iso2)
{
    if (string.IsNullOrEmpty(iso2) || iso2 == "XX") return "Unknown";
    try
    {
        return new System.Globalization.RegionInfo(iso2).DisplayName;
    }
    catch
    {
        return iso2;
    }
}}

    /// <summary>Returns a readable country name