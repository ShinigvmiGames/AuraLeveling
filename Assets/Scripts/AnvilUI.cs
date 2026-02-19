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

    [Header("Craft Timing")]
    public float minCraftTime = 0.5f;
    public float maxCraftTime = 1.0f;

    bool crafting = false;

    // === Skip Popup (created at runtime) ===
    GameObject skipPopupRoot;
    Image skipPopupOverlay;
    GameObject skipPopupPanel;
    TMP_Text skipPopupCostText;
    TMP_Text skipPopupTimeText;
    Button skipPopupSkipBtn;
    Button skipPopupCloseBtn;
    bool skipPopupOpen = false;

    // === Screen lock ===
    ScreenManager screenManager;

    void Awake()
    {
        ResolveRefs();
        AutoWireAll();
        CreateSkipPopup();
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
        UpdateSkipPopup();
    }

    // ========= Auto-Wire =========
    void AutoWireAll()
    {
        if (anvilLevelText == null)
        {
            var go = GameObject.Find("Txt_AnvilLevel");
            if (go != null)
                anvilLevelText = go.GetComponent<TMP_Text>();
        }

        if (upgradeButton == null)
        {
            var go = GameObject.Find("Btn_Upgrade");
            if (go != null)
                upgradeButton = go.GetComponent<Button>();
        }

        if (upgradeButtonText == null)
        {
            var go = GameObject.Find("Txt_Upgrade");
            if (go != null)
                upgradeButtonText = go.GetComponent<TMP_Text>();
        }

        if (skipButton == null)
        {
            var go = GameObject.Find("Btn_Skip");
            if (go != null)
                skipButton = go.GetComponent<Button>();
        }

        if (skipCostText == null)
        {
            var go = GameObject.Find("Txt_SkipCost");
            if (go != null)
                skipCostText = go.GetComponent<TMP_Text>();
        }

        if (screenManager == null)
            screenManager = FindObjectOfType<ScreenManager>();
    }

    void ResolveRefs()
    {
        if (player == null) player = FindObjectOfType<PlayerStats>();
        if (inventory == null) inventory = FindObjectOfType<InventorySystem>();
        if (anvilSystem == null) anvilSystem = FindObjectOfType<AnvilSystem>();
        if (popup == null) popup = FindInactivePopup();
    }

    void RefreshUI()
    {
        if (player == null || inventory == null || anvilSystem == null)
            ResolveRefs();
        if (anvilSystem == null || player == null) return;

        // === Essence display ===
        if (essenceText != null)
            essenceText.text = $"{player.shadowEssence}";

        // === Craft button: greyed out when no essence, crafting, upgrading, or inventory full ===
        if (craftButton != null)
        {
            bool canCraft = !crafting
                && !anvilSystem.isUpgrading
                && player.shadowEssence >= anvilSystem.essenceCostPerCraft
                && inventory != null && inventory.HasSpace();
            craftButton.interactable = canCraft;
        }

        // === Anvil level display ===
        if (anvilLevelText != null)
            anvilLevelText.text = $"{anvilSystem.anvilLevel}";

        // === Upgrade state ===
        bool upgrading = anvilSystem.isUpgrading;

        // Upgrade button: always visible, text changes based on state
        if (upgradeButton != null)
        {
            int cost = anvilSystem.GetUpgradeCost();
            bool isMaxLevel = cost < 0;

            if (upgrading)
            {
                // While upgrading: show remaining time, clickable to open skip popup
                upgradeButton.interactable = true;

                if (upgradeButtonText != null)
                {
                    float remaining = anvilSystem.GetUpgradeRemainingSeconds();
                    upgradeButtonText.text = FormatTime(Mathf.CeilToInt(remaining));
                }
            }
            else
            {
                // Not upgrading: show "UPGRADE" text, interactable if affordable
                upgradeButton.interactable = !isMaxLevel && anvilSystem.CanStartUpgrade();

                if (upgradeButtonText != null)
                    upgradeButtonText.text = isMaxLevel ? "MAX" : "UPGRADE";
            }

            if (upgradeCostText != null)
                upgradeCostText.text = isMaxLevel ? "MAX" : $"{cost} Gold";
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

        // Skip button: visible only when upgrading
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(upgrading);
            if (upgrading)
            {
                int mcCost = anvilSystem.GetSkipCostMC();
                skipButton.interactable = player.manaCrystals >= mcCost;

                if (skipCostText != null)
                    skipCostText.text = $"{mcCost} MC";
            }
        }

        // === Screen lock during crafting ===
        if (screenManager != null)
            screenManager.lockScreenSwitch = crafting;
    }

    // ========= Craft =========
    public void OnCraftPressed()
    {
        if (crafting) return;
        if (player == null || anvilSystem == null) return;
        if (anvilSystem.isUpgrading) return;
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

        bool added = inventory.Add(crafted);
        if (!added)
        {
            player.AddGold(crafted.sellPrice);
            crafting = false;
            yield break;
        }

        if (popup != null)
        {
            int newIndex = inventory.Count - 1;
            popup.ShowInventoryIndex(newIndex);
        }

        // Unlock screen switch after popup is shown
        crafting = false;
    }

    // ========= Upgrade =========
    public void OnUpgradePressed()
    {
        if (anvilSystem == null) return;

        // If currently upgrading, open the skip popup instead
        if (anvilSystem.isUpgrading)
        {
            OpenSkipPopup();
            return;
        }

        anvilSystem.TryStartUpgrade();
    }

    // ========= Skip (legacy direct button) =========
    public void OnSkipPressed()
    {
        if (anvilSystem == null) return;
        anvilSystem.TrySkipUpgrade();
    }

    // =============================================================
    // ========= Skip Popup (created at runtime) ===================
    // =============================================================
    void CreateSkipPopup()
    {
        // Find a Canvas to parent this popup to
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // --- Root overlay (full-screen, dims background) ---
        skipPopupRoot = new GameObject("SkipPopup_Overlay");
        skipPopupRoot.transform.SetParent(canvas.transform, false);
        skipPopupRoot.layer = 5; // UI

        RectTransform rootRT = skipPopupRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        skipPopupOverlay = skipPopupRoot.AddComponent<Image>();
        skipPopupOverlay.color = new Color(0f, 0f, 0f, 0.6f); // dim
        skipPopupOverlay.raycastTarget = true; // blocks clicks behind

        // --- Panel (centered box) ---
        skipPopupPanel = new GameObject("SkipPopup_Panel");
        skipPopupPanel.transform.SetParent(skipPopupRoot.transform, false);
        skipPopupPanel.layer = 5;

        RectTransform panelRT = skipPopupPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(520, 320);

        Image panelBg = skipPopupPanel.AddComponent<Image>();
        panelBg.color = new Color(0.12f, 0.06f, 0.22f, 0.95f); // dark purple

        // --- Title text ---
        GameObject titleGO = CreateTMPChild(skipPopupPanel.transform, "Txt_SkipTitle",
            new Vector2(0, 100), new Vector2(460, 60), 36, "Skip Upgrade?");

        // --- Time remaining text ---
        GameObject timeGO = CreateTMPChild(skipPopupPanel.transform, "Txt_SkipTime",
            new Vector2(0, 40), new Vector2(460, 50), 28, "00:00");
        skipPopupTimeText = timeGO.GetComponent<TMP_Text>();

        // --- Cost text ---
        GameObject costGO = CreateTMPChild(skipPopupPanel.transform, "Txt_SkipPopupCost",
            new Vector2(0, -15), new Vector2(460, 50), 28, "0 MC");
        skipPopupCostText = costGO.GetComponent<TMP_Text>();

        // --- SKIP button ---
        GameObject skipBtnGO = new GameObject("Btn_SkipPopup");
        skipBtnGO.transform.SetParent(skipPopupPanel.transform, false);
        skipBtnGO.layer = 5;

        RectTransform skipBtnRT = skipBtnGO.AddComponent<RectTransform>();
        skipBtnRT.anchorMin = new Vector2(0.5f, 0.5f);
        skipBtnRT.anchorMax = new Vector2(0.5f, 0.5f);
        skipBtnRT.anchoredPosition = new Vector2(0, -85);
        skipBtnRT.sizeDelta = new Vector2(250, 70);

        Image skipBtnImg = skipBtnGO.AddComponent<Image>();
        skipBtnImg.color = new Color(0.2f, 0.1f, 0.35f, 0.85f);

        skipPopupSkipBtn = skipBtnGO.AddComponent<Button>();
        skipPopupSkipBtn.targetGraphic = skipBtnImg;
        skipPopupSkipBtn.onClick.AddListener(OnPopupSkipPressed);

        // Button color transitions
        ColorBlock cb = skipPopupSkipBtn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        cb.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        cb.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
        skipPopupSkipBtn.colors = cb;

        // Skip button text
        CreateTMPChild(skipBtnGO.transform, "Txt_SkipBtnLabel",
            Vector2.zero, Vector2.zero, 32, "SKIP", true);

        // --- Close button (red X, top-right) ---
        GameObject closeBtnGO = new GameObject("Btn_CloseSkipPopup");
        closeBtnGO.transform.SetParent(skipPopupPanel.transform, false);
        closeBtnGO.layer = 5;

        RectTransform closeBtnRT = closeBtnGO.AddComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(1f, 1f);
        closeBtnRT.anchorMax = new Vector2(1f, 1f);
        closeBtnRT.anchoredPosition = new Vector2(-25, -25);
        closeBtnRT.sizeDelta = new Vector2(50, 50);

        Image closeBtnImg = closeBtnGO.AddComponent<Image>();
        closeBtnImg.color = new Color(0.8f, 0.15f, 0.15f, 1f); // red

        skipPopupCloseBtn = closeBtnGO.AddComponent<Button>();
        skipPopupCloseBtn.targetGraphic = closeBtnImg;
        skipPopupCloseBtn.onClick.AddListener(CloseSkipPopup);

        // X text on close button
        CreateTMPChild(closeBtnGO.transform, "Txt_X",
            Vector2.zero, Vector2.zero, 30, "X", true);

        // Hide at start
        skipPopupRoot.SetActive(false);
    }

    GameObject CreateTMPChild(Transform parent, string name, Vector2 pos, Vector2 size,
        float fontSize, string text, bool stretch = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.layer = 5;

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        if (stretch)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return go;
    }

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

        // Auto-close if upgrade completed while popup was open
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
            skipPopupCostText.text = $"Cost: {mcCost} MC  (You have: {player.manaCrystals} MC)";

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
