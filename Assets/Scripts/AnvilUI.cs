using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnvilUI : MonoBehaviour
{
    [Header("Refs")]
    public InventorySystem inventory;
    public AnvilSystem anvilSystem;
    public PlayerStats player;
    public ItemPopup popup;

    [Header("Craft UI")]
    public Button craftButton;
    public TMP_Text essenceText;

    [Header("Level-Up UI")]
    public Button upgradeButton;
    public TMP_Text anvilLevelText;
    public TMP_Text upgradeCostText;
    public TMP_Text upgradeButtonText;

    [Header("Upgrade Timer UI")]
    public TMP_Text upgradeTimerText;
    public Image upgradeProgressFill;
    public Button skipButton;
    public TMP_Text skipCostText;

    [Header("Skip Timer Below Button (assign in Inspector)")]
    public TMP_Text skipTimerBelowText;

    [Header("Upgrade Button Images")]
    [Tooltip("Sprite shown on the upgrade button in normal state (optional)")]
    public Sprite upgradeButtonSprite;
    [Tooltip("Sprite shown on the upgrade button when in skip mode (optional)")]
    public Sprite skipButtonSprite;

    [Header("Confirmation Dialog Sprites")]
    [Tooltip("Background sprite for the confirm dialog")]
    public Sprite confirmDialogSprite;
    [Tooltip("Gold coin icon sprite")]
    public Sprite goldIconSprite;
    [Tooltip("Duration/timer icon sprite")]
    public Sprite durationIconSprite;

    [Header("Upgrade Confirm Popup (assign in Inspector)")]
    public GameObject confirmPopupRoot;
    public TMP_Text confirmCostText;
    public TMP_Text confirmDurationText;
    public Button confirmYesBtn;
    public Button confirmNoBtn;

    [Header("Skip Popup (assign in Inspector)")]
    public GameObject skipPopupRoot;
    public TMP_Text skipPopupCostText;
    public TMP_Text skipPopupTimeText;
    public Button skipPopupSkipBtn;
    public Button skipPopupCloseBtn;

    [Header("Craft Timing")]
    public float minCraftTime = 0.5f;
    public float maxCraftTime = 1.0f;

    bool crafting = false;
    bool confirmPopupOpen = false;
    bool skipPopupOpen = false;

    // === Screen lock ===
    ScreenManager screenManager;

    // Track upgrade state for button switching
    bool wasUpgrading = false;
    Sprite originalUpgradeSprite; // stored at Awake to restore after skip mode

    void Awake()
    {
        ResolveRefs();

        // Store the original upgrade button sprite so we can restore it after skip mode
        if (upgradeButton != null)
        {
            Image btnImg = upgradeButton.GetComponent<Image>();
            if (btnImg != null)
                originalUpgradeSprite = btnImg.sprite;
        }

        // Bind confirm popup buttons
        if (confirmYesBtn != null) confirmYesBtn.onClick.AddListener(OnConfirmYes);
        if (confirmNoBtn != null) confirmNoBtn.onClick.AddListener(OnConfirmNo);

        // Bind skip popup buttons
        if (skipPopupSkipBtn != null) skipPopupSkipBtn.onClick.AddListener(OnPopupSkipPressed);
        if (skipPopupCloseBtn != null) skipPopupCloseBtn.onClick.AddListener(CloseSkipPopup);

        // Ensure popups start hidden
        if (confirmPopupRoot != null) confirmPopupRoot.SetActive(false);
        if (skipPopupRoot != null) skipPopupRoot.SetActive(false);
    }

    void Start()
    {
        if (craftButton != null)
        {
            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(OnCraftPressed);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradePressed);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkipPressed);
        }

        RefreshUI();
    }

    void Update()
    {
        RefreshUI();
        UpdateConfirmPopup();
        UpdateSkipPopup();
    }

    void ResolveRefs()
    {
        if (player == null) player = FindObjectOfType<PlayerStats>();
        if (inventory == null) inventory = FindObjectOfType<InventorySystem>();
        if (anvilSystem == null) anvilSystem = FindObjectOfType<AnvilSystem>();
        if (popup == null) popup = FindInactivePopup();
        if (screenManager == null) screenManager = FindObjectOfType<ScreenManager>();
    }

    void RefreshUI()
    {
        if (player == null || inventory == null || anvilSystem == null)
            ResolveRefs();
        if (anvilSystem == null || player == null) return;

        // === Essence display ===
        if (essenceText != null)
            essenceText.text = $"{player.shadowEssence}";

        // === Craft button ===
        if (craftButton != null)
        {
            // Block crafting while popup is open or during craft animation
            bool popupOpen = popup != null && popup.gameObject.activeSelf;
            bool canCraft = !crafting
                && !popupOpen
                && player.shadowEssence >= anvilSystem.essenceCostPerCraft
                && inventory != null && inventory.HasSpace();
            craftButton.interactable = canCraft;
        }

        // === Anvil level display ===
        if (anvilLevelText != null)
            anvilLevelText.text = $"{anvilSystem.anvilLevel}";

        // === Upgrade/Skip state ===
        bool upgrading = anvilSystem.isUpgrading;

        // Switch button appearance when upgrade state changes
        if (upgrading != wasUpgrading)
        {
            wasUpgrading = upgrading;
            UpdateUpgradeButtonAppearance(upgrading);
        }

        if (upgradeButton != null)
        {
            int cost = anvilSystem.GetUpgradeCost();
            bool isMaxLevel = cost < 0;

            if (upgrading)
            {
                // While upgrading: button is "SKIP", always clickable to open skip popup
                upgradeButton.interactable = true;

                if (upgradeButtonText != null)
                    upgradeButtonText.text = "SKIP";
            }
            else
            {
                // Not upgrading: show "UPGRADE", interactable if affordable
                upgradeButton.interactable = !isMaxLevel && anvilSystem.CanStartUpgrade();

                if (upgradeButtonText != null)
                    upgradeButtonText.text = isMaxLevel ? "MAX" : "UPGRADE";
            }

            if (upgradeCostText != null)
                upgradeCostText.text = isMaxLevel ? "MAX" : $"{cost}";
        }

        // Timer text below the button (visible only during upgrade)
        if (skipTimerBelowText != null)
        {
            skipTimerBelowText.gameObject.SetActive(upgrading);
            if (upgrading)
            {
                float remaining = anvilSystem.GetUpgradeRemainingSeconds();
                skipTimerBelowText.text = FormatTime(Mathf.CeilToInt(remaining));
            }
        }

        // Legacy timer text (if still wired)
        if (upgradeTimerText != null)
        {
            upgradeTimerText.gameObject.SetActive(upgrading);
            if (upgrading)
            {
                float remaining = anvilSystem.GetUpgradeRemainingSeconds();
                upgradeTimerText.text = FormatTime(Mathf.CeilToInt(remaining));
            }
        }

        // Progress fill
        if (upgradeProgressFill != null)
        {
            upgradeProgressFill.gameObject.SetActive(upgrading);
            if (upgrading)
                upgradeProgressFill.fillAmount = anvilSystem.GetUpgradeProgress01();
        }

        // Legacy skip button: hide it (we now use the upgrade button as skip)
        if (skipButton != null)
            skipButton.gameObject.SetActive(false);

        // === Screen lock during crafting ===
        if (screenManager != null)
            screenManager.lockScreenSwitch = crafting;
    }

    /// <summary>
    /// Switch the upgrade button's image between upgrade and skip sprites.
    /// </summary>
    void UpdateUpgradeButtonAppearance(bool isSkipMode)
    {
        if (upgradeButton == null) return;

        Image btnImage = upgradeButton.GetComponent<Image>();
        if (btnImage == null) return;

        if (isSkipMode)
        {
            // Use skipButtonSprite if assigned, otherwise keep the current image
            if (skipButtonSprite != null)
                btnImage.sprite = skipButtonSprite;
        }
        else
        {
            // Restore original sprite
            Sprite restoreSprite = upgradeButtonSprite != null ? upgradeButtonSprite : originalUpgradeSprite;
            if (restoreSprite != null)
                btnImage.sprite = restoreSprite;
        }
    }

    // ========= Craft =========
    public void OnCraftPressed()
    {
        if (crafting) return;
        if (player == null || anvilSystem == null) return;
        // Crafting is allowed during upgrades (uses current level rates)
        if (player.shadowEssence < anvilSystem.essenceCostPerCraft) return;
        if (inventory != null && !inventory.HasSpace()) return;

        StartCoroutine(CraftFlow());
    }

    IEnumerator CraftFlow()
    {
        crafting = true;
        ResolveRefs();

        if (player == null || inventory == null || anvilSystem == null)
        {
            crafting = false;
            yield break;
        }

        if (player.shadowEssence < anvilSystem.essenceCostPerCraft)
        {
            crafting = false;
            yield break;
        }

        float duration = Random.Range(minCraftTime, maxCraftTime);
        yield return new WaitForSeconds(duration);

        // CraftItemInstant() already handles:
        // 1. Spending essence
        // 2. Generating the item
        // 3. Adding to inventory (via TryAddToInventoryAndPopup)
        // 4. Showing the ItemPopup
        // Do NOT add to inventory again here - that would duplicate the item!
        ItemData crafted = anvilSystem.CraftItemInstant();

        if (crafted == null)
        {
            crafting = false;
            Debug.LogWarning("AnvilUI: Craft returned null - check ItemDatabase_Main!");
            yield break;
        }

        crafting = false;
    }

    // ========= Upgrade =========
    public void OnUpgradePressed()
    {
        if (anvilSystem == null) return;

        // If currently upgrading, clicking the button opens skip popup
        if (anvilSystem.isUpgrading)
        {
            OpenSkipPopup();
            return;
        }

        // Not upgrading: open confirmation dialog
        OpenConfirmPopup();
    }

    // ========= Skip (legacy direct button) =========
    public void OnSkipPressed()
    {
        if (anvilSystem == null) return;
        anvilSystem.TrySkipUpgrade();
    }

    // =============================================================
    // ========= Upgrade Confirmation Popup ========================
    // =============================================================

    void OpenConfirmPopup()
    {
        if (confirmPopupRoot == null) return;
        if (anvilSystem == null) return;
        if (anvilSystem.isUpgrading) return;

        int cost = anvilSystem.GetUpgradeCost();
        if (cost < 0) return; // max level

        float duration = AnvilSystem.GetUpgradeDurationSeconds(anvilSystem.anvilLevel);

        if (confirmCostText != null)
            confirmCostText.text = $"<b>{cost}</b>";

        if (confirmDurationText != null)
            confirmDurationText.text = $"<b>{FormatTime(Mathf.CeilToInt(duration))}</b>";

        confirmPopupOpen = true;
        confirmPopupRoot.SetActive(true);
        confirmPopupRoot.transform.SetAsLastSibling();
    }

    void CloseConfirmPopup()
    {
        if (confirmPopupRoot == null) return;
        confirmPopupOpen = false;
        confirmPopupRoot.SetActive(false);
    }

    void UpdateConfirmPopup()
    {
        // Close if upgrade started somehow while popup was open
        if (confirmPopupOpen && anvilSystem != null && anvilSystem.isUpgrading)
            CloseConfirmPopup();
    }

    void OnConfirmYes()
    {
        if (anvilSystem == null) return;
        CloseConfirmPopup();
        anvilSystem.TryStartUpgrade();
    }

    void OnConfirmNo()
    {
        CloseConfirmPopup();
    }

    // =============================================================
    // ========= Skip Popup ========================================
    // =============================================================

    void OpenSkipPopup()
    {
        if (skipPopupRoot == null) return;
        if (!anvilSystem.isUpgrading) return;

        skipPopupOpen = true;
        skipPopupRoot.SetActive(true);
        skipPopupRoot.transform.SetAsLastSibling();

        UpdateSkipPopupContent();
    }

    public void CloseSkipPopup()
    {
        if (skipPopupRoot == null) return;
        skipPopupOpen = false;
        skipPopupRoot.SetActive(false);
    }

    void UpdateSkipPopup()
    {
        if (!skipPopupOpen || skipPopupRoot == null) return;
        if (anvilSystem == null || player == null) return;

        if (!anvilSystem.isUpgrading)
        {
            CloseSkipPopup();
            return;
        }

        UpdateSkipPopupContent();
    }

    void UpdateSkipPopupContent()
    {
        float remaining = anvilSystem.GetUpgradeRemainingSeconds();
        int mcCost = anvilSystem.GetSkipCostMC();

        if (skipPopupTimeText != null)
            skipPopupTimeText.text = $"Time left: {FormatTime(Mathf.CeilToInt(remaining))}";

        if (skipPopupCostText != null)
            skipPopupCostText.text = $"{mcCost}  (You have: {player.manaCrystals})";

        if (skipPopupSkipBtn != null)
            skipPopupSkipBtn.interactable = player.manaCrystals >= mcCost;
    }

    void OnPopupSkipPressed()
    {
        if (anvilSystem == null) return;
        if (anvilSystem.TrySkipUpgrade())
        {
            CloseSkipPopup();
        }
    }

    // ========= Helpers =========

    string FormatTime(int totalSeconds)
    {
        if (totalSeconds >= 86400)
        {
            int d = totalSeconds / 86400;
            int h = (totalSeconds % 86400) / 3600;
            return $"{d}d {h}h";
        }
        if (totalSeconds >= 3600)
        {
            int h = totalSeconds / 3600;
            int m = (totalSeconds % 3600) / 60;
            int s = totalSeconds % 60;
            return $"{h:0}h {m:00}m {s:00}s";
        }
        else
        {
            int m = totalSeconds / 60;
            int s = totalSeconds % 60;
            return $"{m:00}:{s:00}";
        }
    }

    ItemPopup FindInactivePopup()
    {
        var all = Resources.FindObjectsOfTypeAll<ItemPopup>();
        if (all == null || all.Length == 0) return null;

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.scene.IsValid())
                return all[i];
        }

        return all[0];
    }
}
