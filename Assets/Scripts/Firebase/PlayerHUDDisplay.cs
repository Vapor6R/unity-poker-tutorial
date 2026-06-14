using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// PlayerHUDDisplay
/// Attach to any GameObject in MainMenu that has your profile UI elements.
/// Waits for PlayerProfileManager to finish loading, then populates the UI.
///
/// XP Bar setup in Unity:
///   - Create an Image (background bar) — e.g. dark grey
///   - Inside it, create a child Image (fill bar) — e.g. gold/green
///   - Set the fill Image's RectTransform anchor: left-stretch (anchorMin.x=0, anchorMax.x=0)
///   - Pivot: (0, 0.5)
///   - Assign the fill Image's RectTransform to xpBarFill in the Inspector
///
/// Edit Name Modal setup in Unity:
///   - Create a Panel (editNameModal) — hidden by default
///     - Inside: TMP_InputField (editNameInput)
///     - Button "Confirm" → assign to confirmNameButton
///     - Button "Cancel"  → assign to cancelNameButton
///   - Create a Button next to displayNameText → assign to editNameButton
///   - Optionally assign a loadingSpinner (Image) shown while saving
/// </summary>
public class PlayerHUDDisplay : MonoBehaviourPun
{
    // ─────────────────────────────────────────────────────────────
    //  Inspector fields
    // ─────────────────────────────────────────────────────────────

    [Header("Profile UI")]
    [SerializeField] private TextMeshProUGUI displayNameText;
    [SerializeField] private TextMeshProUGUI referralCodeText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI xpText;

    [Header("XP Bar")]
    [SerializeField] private RectTransform xpBarFill;
    [SerializeField] private RectTransform xpBarBackground;
    [SerializeField] private TextMeshProUGUI xpProgressLabel;

    [Header("Panels")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject profilePanel;

    [Header("Edit Name")]
    [SerializeField] private Button          editNameButton;      // pencil / "Edit" button next to the name
    [SerializeField] private GameObject      editNameModal;       // the popup panel
    [SerializeField] private TMP_InputField  editNameInput;       // text field inside the modal
    [SerializeField] private Button          confirmNameButton;   // "Save" button
    [SerializeField] private Button          cancelNameButton;    // "Cancel" button
    [SerializeField] private TextMeshProUGUI editFeedbackText;    // optional status label ("Saving…", "Saved!", error)
    [SerializeField] private GameObject      editLoadingSpinner;  // optional spinner while Firebase saves

    // Runtime state
    private Coroutine _saveCoroutine;
    private bool      _isSaving;

    // ─────────────────────────────────────────────────────────────
    //  XP per level — must match PlayerProfileManager formula
    // ─────────────────────────────────────────────────────────────

    private const long XP_PER_LEVEL = 1000;

    // ─────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SetPanelVisibility(profileReady: false);
        SetEditModalVisible(false);

        // Wire up edit-name buttons
        if (editNameButton)    editNameButton.onClick.AddListener(OpenEditNameModal);
        if (confirmNameButton) confirmNameButton.onClick.AddListener(OnConfirmNameClicked);
        if (cancelNameButton)  cancelNameButton.onClick.AddListener(CloseEditNameModal);

        StartCoroutine(WaitAndDisplay());
    }

    // ─────────────────────────────────────────────────────────────
    //  Wait for profile then populate
    // ─────────────────────────────────────────────────────────────

    private IEnumerator WaitAndDisplay()
    {
        yield return new WaitUntil(() => PlayerProfileManager.Instance != null);
        yield return new WaitUntil(() => PlayerProfileManager.Instance.IsReady);

        PopulateUI();
    }

    // ─────────────────────────────────────────────────────────────
    //  Populate UI
    // ─────────────────────────────────────────────────────────────

    private void PopulateUI()
    {
        PlayerProfileManager.PlayerProfile p = PlayerProfileManager.Instance.Profile;

        if (p == null)
        {
            Debug.LogWarning("[HUD] Profile is null after IsReady.");
            return;
        }

        if (displayNameText)  displayNameText.text = p.displayName;
        if (referralCodeText) referralCodeText.text = $"Referal:{p.referralCode}";
        if (levelText)        levelText.text        = $"Level {p.level}";
        if (xpText)           xpText.text           = $"{p.xp} XP";

        UpdateXPBar(p.xp);

        SetPanelVisibility(profileReady: true);
        Debug.Log($"[HUD] UI populated — {p}");
    }

    // ─────────────────────────────────────────────────────────────
    //  XP bar — scales fill RectTransform width
    // ─────────────────────────────────────────────────────────────

    private void UpdateXPBar(long totalXp)
    {
        long  xpIntoLevel = totalXp % XP_PER_LEVEL;
        float progress    = Mathf.Clamp01((float)xpIntoLevel / XP_PER_LEVEL);

        if (xpProgressLabel)
            xpProgressLabel.text = $"{xpIntoLevel} / {XP_PER_LEVEL} XP";

        if (xpBarFill == null) return;

        float totalWidth = (xpBarBackground != null)
            ? xpBarBackground.rect.width
            : xpBarFill.parent.GetComponent<RectTransform>().rect.width;

        xpBarFill.sizeDelta = new Vector2(totalWidth * progress, xpBarFill.sizeDelta.y);
    }

    // ─────────────────────────────────────────────────────────────
    //  Public refresh — call after XP or name changes
    // ─────────────────────────────────────────────────────────────

    public void RefreshUI()
    {
        PopulateUI();
    }

    // ─────────────────────────────────────────────────────────────
    //  Edit name — modal open / close
    // ─────────────────────────────────────────────────────────────

    private void OpenEditNameModal()
    {
        // Kill any in-flight save coroutine (e.g. the 1-second "Saved!" delay)
        // so it can't close the modal we're about to open
        if (_saveCoroutine != null)
        {
            StopCoroutine(_saveCoroutine);
            _saveCoroutine = null;
        }
        _isSaving = false;

        // Pre-fill the input with the current display name
        if (editNameInput && PlayerProfileManager.Instance?.Profile != null)
            editNameInput.text = PlayerProfileManager.Instance.Profile.displayName;

        SetEditFeedback("", false);
        SetSpinner(false);
        SetEditButtons(interactable: true);
        SetEditModalVisible(true);

        // Focus the input field so the keyboard opens on mobile / desktop
        if (editNameInput)
            editNameInput.ActivateInputField();
    }

    private void CloseEditNameModal()
    {
        // Kill any pending delay coroutine before hiding
        if (_saveCoroutine != null)
        {
            StopCoroutine(_saveCoroutine);
            _saveCoroutine = null;
        }
        _isSaving = false;

        SetSpinner(false);
        SetEditButtons(interactable: true);
        SetEditModalVisible(false);
    }

    // ─────────────────────────────────────────────────────────────
    //  Edit name — confirm / save
    // ─────────────────────────────────────────────────────────────

    private void OnConfirmNameClicked()
    {
        Debug.Log("[HUD] OnConfirmNameClicked FIRED");

        // Guard: ignore extra clicks while a save is already in flight
        if (_isSaving)
        {
            Debug.Log("[HUD] Already saving, ignored.");
            return;
        }

        string newName = editNameInput ? editNameInput.text.Trim() : "";
        Debug.Log($"[HUD] newName='{newName}' editNameInput={(editNameInput == null ? "NULL" : "OK")}");

        if (string.IsNullOrEmpty(newName))
        {
            SetEditFeedback("Name cannot be empty.", false);
            return;
        }

        if (newName.Length > 20)
        {
            SetEditFeedback("Max 20 characters.", false);
            return;
        }

        // Direct synchronous update — no coroutine, no Firebase, just set the text now
        if (PlayerProfileManager.Instance?.Profile != null)
            PlayerProfileManager.Instance.Profile.displayName = newName;

        if (displayNameText != null)
        {
            displayNameText.text = newName;
            displayNameText.ForceMeshUpdate();
            Debug.Log($"[HUD] displayNameText set to '{newName}'");
        }
        else
        {
            Debug.LogError("[HUD] displayNameText is NULL!");
        }

        SetEditModalVisible(false);

        // Now fire Firebase in background
        _saveCoroutine = StartCoroutine(SaveDisplayName(newName));
    }

    private IEnumerator SaveDisplayName(string newName)
    {
        _isSaving = true;

        // Hide the modal immediately on confirm — don't wait for Firebase
        SetEditModalVisible(false);

        Debug.Log($"[HUD] SaveDisplayName START — newName='{newName}'");
        Debug.Log($"[HUD] displayNameText is {(displayNameText == null ? "NULL" : "OK")}");
        Debug.Log($"[HUD] ProfileManager Instance is {(PlayerProfileManager.Instance == null ? "NULL" : "OK")}");

        // Update local profile and UI immediately — no Firebase wait
        if (PlayerProfileManager.Instance?.Profile != null)
        {
            PlayerProfileManager.Instance.Profile.displayName = newName;
            Debug.Log($"[HUD] Profile.displayName set to: {PlayerProfileManager.Instance.Profile.displayName}");
        }

        if (displayNameText != null)
        {
            displayNameText.text = newName;
            displayNameText.ForceMeshUpdate();
            Debug.Log($"[HUD] displayNameText.text = '{displayNameText.text}'");
        }
        else
        {
            Debug.LogError("[HUD] displayNameText is NULL — assign it in the Inspector!");
        }

        SetEditButtons(interactable: false);
        SetSpinner(true);

        bool done     = false;
        bool error    = false;
        string errorMsg = "";

        PlayerProfileManager.Instance?.UpdateDisplayName(newName, (success, msg) =>
        {
            Debug.Log($"[HUD] UpdateDisplayName callback fired — success={success} msg={msg}");
            done     = true;
            error    = !success;
            errorMsg = msg;
        });

        // Wait max 10 seconds before giving up
        float timeout = 0f;
        yield return new WaitUntil(() =>
        {
            timeout += Time.deltaTime;
            return done || timeout > 10f;
        });

        SetSpinner(false);
        SetEditButtons(interactable: true);
        _isSaving      = false;
        _saveCoroutine = null;

        if (!done)
        {
            Debug.LogError("[HUD] UpdateDisplayName timed out — callback never fired!");
            SetEditFeedback("Timeout — check Firebase/Manager", isError: true);
        }
        else if (error)
        {
            Debug.LogError($"[HUD] UpdateDisplayName error: {errorMsg}");
            SetEditFeedback($"Error: {errorMsg}", isError: true);
        }
        else
        {
            Debug.Log("[HUD] Save complete.");
            SetEditFeedback("Saved!", isError: false);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────

    private void SetEditModalVisible(bool visible)
    {
        if (editNameModal) editNameModal.SetActive(visible);
    }

    private void SetEditFeedback(string message, bool isError)
    {
        if (!editFeedbackText) return;
        editFeedbackText.text  = message;
        editFeedbackText.color = isError ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 1f, 0.5f);
    }

    private void SetEditButtons(bool interactable)
    {
        if (confirmNameButton) confirmNameButton.interactable = interactable;
        if (cancelNameButton)  cancelNameButton.interactable  = interactable;
    }

    private void SetSpinner(bool visible)
    {
        if (editLoadingSpinner) editLoadingSpinner.SetActive(visible);
    }

    // ─────────────────────────────────────────────────────────────
    //  Panel visibility
    // ─────────────────────────────────────────────────────────────

    private void SetPanelVisibility(bool profileReady)
    {
        if (loadingPanel) loadingPanel.SetActive(!profileReady);
        if (profilePanel) profilePanel.SetActive(profileReady);
    }
}
