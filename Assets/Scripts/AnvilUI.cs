using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnvilUI : MonoBehaviour
{
    [Header("Refs (auto-found if empty)")]
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

    [Header("Upgrade Timer")]
    public TMP_Text skipTimerBelowText;
    public Image upgradeProgressFill;

    [Header("Upgrade Button Sprites (optional)")]
    public Sprite upgradeButtonSprite;
    public Sprite skipButtonSprite;

    [Header("Upgrade Confirm Popup")]
    public GameObject confirmPopupRoot;
    public TMP_Text confirmTitleText;
    public Image confirmGoldIcon;
    public TMP_Text confirmCostText;
    public Image confirmDurationIcon;
    public TMP_Text confirmDurationText;
    public Button confirmYesBtn;
    public Button confirmNoBtn;

    [Header("Skip Popup")]
    public GameObject skipPopupRoot;
    public TMP_Text skipTitleText;
    public Image skipMCIcon;
    public TMP_Text skipPopupCostText;
    public Image skipDurationIcon;
    public TMP_Text skipPopupTimeText;
    public Button skipPopupSkipBtn;
    public Button skipPopupCloseBtn;

    [Header("Craft Timing")]
    public float minCraftTime = 0.5f;
    public float maxCraftTime = 1.0f;

    bool crafting = false;
    bool confirmPopupOpen = false;
    bool skipPopupOpen = false;

    ScreenManager screenManager;
    bool wasUpgrading = false;
    Sprite originalUpgradeSprite;

    void Awake()
    {
        ResolveRefs();

        if (upgradeButton != null)
        {
            Image btnImg = upgradeButton.GetComponent<Image>();
            if (btnImg != null)
                originalUpgradeSprite = btnImg.sprite;
        }

        if (confirmYesBtn != null) confirmYesBtn.onClick.AddListener(OnConfirmYes);
        if (confirmNoBtn != null) confirmNoBtn.onClick.AddListener(OnConfirmNo);
        if (skipPopupSkipBtn != null) skipPopupSkipBtn.onClick.AddListener(OnPopupSkipPressed);
        if (skipPopupCloseBtn != null) skipPopupCloseBtn.onClick.AddListener(CloseSkipPopup);

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
                upgradeButton.interactable = true;
                if (upgradeButtonText != null)
                    upgradeButtonText.text = "SKIP";
            }
            else
            {
                upgradeButton.interactable = !isMaxLevel && anvilSystem.CanStartUpgrade();
                if (upgradeButtonText != null)
                    upgradeButtonText.text = isMaxLevel ? "MAX" : "UPGRADE";
            }

            if (upgradeCostText != null)
                upgradeCostText.text = isMaxLevel ? "MAX" : $"{cost}";
        }

        // Timer below the upgrade button
        if (skipTimerBelowText != null)
        {
            skipTimerBelowText.gameObject.SetActive(upgrading);
            if (upgrading)
            {
                float remaining = anvilSystem.GetUpgradeRemainingSeconds();
                skipTimerBelowText.text = FormatTime(Mathf.CeilToInt(remaining));
            }
        }

        // Progress fill
        if (upgradeProgressFill != null)
        {
            upgradeProgressFill.gameObject.SetActive(upgrading);
            if (upgrading)
                upgradeProgressFill.fillAmount = anvilSystem.GetUpgradeProgress01();
        }

        // === Screen lock during crafting ===
        if (screenManager != null)
            screenManager.lockScreenSwitch = crafting;
    }

    void UpdateUpgradeButtonAppearance(bool isSkipMode)
    {
        if (upgradeButton == null) return;
        Image btnImage = upgradeButton.GetComponent<Image>();
        if (btnImage == null) return;

        if (isSkipMode)
        {
            if (skipButtonSprite != null)
                btnImage.sprite = skipButtonSprite;
        }
        else
        {
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

        if (anvilSystem.isUpgrading)
        {
            OpenSkipPopup();
            return;
        }

        OpenConfirmPopup();
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

        // Title
        if (confirmTitleText != null)
            confirmTitleText.text = "Are you sure?";

        // Gold cost line: icon is always visible, text shows the number
        if (confirmGoldIcon != null)
            confirmGoldIcon.enabled = true;
        if (confirmCostText != null)
            confirmCostText.text = $"{cost:N0}";

        // Duration line: icon is always visible, text shows formatted time
        if (confirmDurationIcon != null)
            confirmDurationIcon.enabled = true;
        if (confirmDurationText != null)
            confirmDurationText.text = FormatTime(Mathf.CeilToInt(duration));

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

        // Title
        if (skipTitleText != null)
            skipTitleText.text = "Skip upgrade?";

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

        // MC cost line: icon + "5 / 12" (cost / your balance)
        if (skipMCIcon != null)
            skipMCIcon.enabled = true;
        if (skipPopupCostText != null)
            skipPopupCostText.text = $"{mcCost:N0}  /  {player.manaCrystals:N0}";

        // Time remaining line: icon + formatted time
        if (skipDurationIcon != null)
            skipDurationIcon.enabled = true;
        if (skipPopupTimeText != null)
            skipPopupTimeText.text = FormatTime(Mathf.CeilToInt(remaining));

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
