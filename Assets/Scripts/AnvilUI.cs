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

    [Header("Upgrade Button Images")]
    [Tooltip("Sprite shown on the upgrade button in normal state (optional)")]
    public Sprite upgradeButtonSprite;
    [Tooltip("Sprite shown on the upgrade button when in skip mode (optional)")]
    public Sprite skipButtonSprite;

    [Header("Confirmation Dialog Sprites")]
    [Tooltip("Background sprite for the confirm dialog (e.g. 1000122033)")]
    public Sprite confirmDialogSprite;
    [Tooltip("Gold coin icon sprite (auto-loaded from 1000120671)")]
    public Sprite goldIconSprite;
    [Tooltip("Duration/timer icon sprite (auto-loaded from 1000120670)")]
    public Sprite durationIconSprite;

    [Header("Craft Timing")]
    public float minCraftTime = 0.5f;
    public float maxCraftTime = 1.0f;

    bool crafting = false;

    // === Upgrade Confirmation Popup ===
    GameObject confirmPopupRoot;
    TMP_Text confirmCostText;
    TMP_Text confirmDurationText;
    Button confirmYesBtn;
    Button confirmNoBtn;
    bool confirmPopupOpen = false;

    // === Skip Popup ===
    GameObject skipPopupRoot;
    Image skipPopupOverlay;
    GameObject skipPopupPanel;
    TMP_Text skipPopupCostText;
    TMP_Text skipPopupTimeText;
    Button skipPopupSkipBtn;
    Button skipPopupCloseBtn;
    bool skipPopupOpen = false;

    // === Timer text below skip button (created at runtime) ===
    TMP_Text skipTimerBelowText;

    // === Screen lock ===
    ScreenManager screenManager;

    // Track upgrade state for button switching
    bool wasUpgrading = false;
    Sprite originalUpgradeSprite; // stored at Awake to restore after skip mode
    // matAdditiveGlow removed - confirm windows no longer use it

    void Awake()
    {
        ResolveRefs();
        AutoWireAll();

        // Store the original upgrade button sprite so we can restore it after skip mode
        if (upgradeButton != null)
        {
            Image btnImg = upgradeButton.GetComponent<Image>();
            if (btnImg != null)
                originalUpgradeSprite = btnImg.sprite;
        }

        CreateConfirmPopup();
        CreateSkipPopup();
        CreateSkipTimerText();
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

        // Load dialog background sprite from Resources folder (guaranteed to be loadable)
        if (confirmDialogSprite == null)
        {
            confirmDialogSprite = Resources.Load<Sprite>("Sprites/1000122033");
            if (confirmDialogSprite != null)
                Debug.Log("AnvilUI: Loaded dialog sprite 1000122033 from Resources");
            else
                Debug.LogWarning("AnvilUI: Could not load Sprites/1000122033 from Resources folder!");
        }

        // Load currency icons from Resources (reliable)
        if (goldIconSprite == null)
            goldIconSprite = CurrencyIcons.Gold;

        // Duration icon not in Resources yet, try FindObjectsOfTypeAll
        if (durationIconSprite == null)
        {
            var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (var s in sprites)
            {
                if (durationIconSprite == null && s.name.Contains("1000120670"))
                    durationIconSprite = s;
            }
        }
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

    /// <summary>
    /// Create a timer text below the upgrade button (shown during upgrade).
    /// </summary>
    void CreateSkipTimerText()
    {
        if (upgradeButton == null) return;

        var go = new GameObject("Txt_SkipTimerBelow");
        go.transform.SetParent(upgradeButton.transform.parent, false);
        go.layer = 5;

        RectTransform rt = go.AddComponent<RectTransform>();

        // Position below the upgrade button
        RectTransform btnRT = upgradeButton.GetComponent<RectTransform>();
        if (btnRT != null)
        {
            rt.anchorMin = btnRT.anchorMin;
            rt.anchorMax = btnRT.anchorMax;
            rt.anchoredPosition = btnRT.anchoredPosition + new Vector2(0, -45);
            rt.sizeDelta = new Vector2(btnRT.sizeDelta.x + 40, 35);
        }
        else
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, -60);
            rt.sizeDelta = new Vector2(200, 35);
        }

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(1f, 0.85f, 0.3f, 1f); // gold-yellow
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.text = "";

        skipTimerBelowText = tmp;
        go.SetActive(false);
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
    void CreateConfirmPopup()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // --- Root overlay (full-screen, dims background, blocks clicks) ---
        confirmPopupRoot = new GameObject("UpgradeConfirm_Overlay");
        confirmPopupRoot.transform.SetParent(canvas.transform, false);
        confirmPopupRoot.layer = 5;

        RectTransform rootRT = confirmPopupRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        Image overlayImg = confirmPopupRoot.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.7f);
        overlayImg.raycastTarget = true; // blocks all clicks behind

        // --- Panel: large, nearly full-screen with small margins ---
        GameObject panel = new GameObject("ConfirmPanel");
        panel.transform.SetParent(confirmPopupRoot.transform, false);
        panel.layer = 5;

        RectTransform panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.05f, 0.2f);
        panelRT.anchorMax = new Vector2(0.95f, 0.8f);
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        Image panelBg = panel.AddComponent<Image>();
        if (confirmDialogSprite != null)
        {
            panelBg.sprite = confirmDialogSprite;
            panelBg.type = Image.Type.Simple;
            panelBg.preserveAspect = false;
            // No MAT material on window background - keep it clean
        }
        else
        {
            panelBg.color = new Color(0.08f, 0.04f, 0.18f, 0.95f);
        }

        // --- Title ---
        CreateTMPChild(panel.transform, "Txt_ConfirmTitle",
            new Vector2(0, 0), new Vector2(520, 80), 44, "Upgrade Anvil?",
            false, new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.95f));

        // --- Cost line: [Gold Icon] 1000 Gold ---
        GameObject costLine = new GameObject("CostLine");
        costLine.transform.SetParent(panel.transform, false);
        costLine.layer = 5;
        RectTransform costLineRT = costLine.AddComponent<RectTransform>();
        costLineRT.anchorMin = new Vector2(0.1f, 0.48f);
        costLineRT.anchorMax = new Vector2(0.9f, 0.7f);
        costLineRT.offsetMin = Vector2.zero;
        costLineRT.offsetMax = Vector2.zero;

        // Gold icon (large)
        if (goldIconSprite != null)
            CreateIconImage(costLine.transform, "Img_GoldIcon", goldIconSprite,
                new Vector2(-110, 0), new Vector2(70, 70));

        GameObject costTxtGO = CreateTMPChild(costLine.transform, "Txt_ConfirmCost",
            new Vector2(20, 0), new Vector2(350, 65), 38, "0");
        confirmCostText = costTxtGO.GetComponent<TMP_Text>();

        // --- Duration line: [Clock Icon] 00:05:00 ---
        GameObject durLine = new GameObject("DurationLine");
        durLine.transform.SetParent(panel.transform, false);
        durLine.layer = 5;
        RectTransform durLineRT = durLine.AddComponent<RectTransform>();
        durLineRT.anchorMin = new Vector2(0.1f, 0.28f);
        durLineRT.anchorMax = new Vector2(0.9f, 0.48f);
        durLineRT.offsetMin = Vector2.zero;
        durLineRT.offsetMax = Vector2.zero;

        // Duration icon (large)
        if (durationIconSprite != null)
            CreateIconImage(durLine.transform, "Img_DurationIcon", durationIconSprite,
                new Vector2(-110, 0), new Vector2(70, 70));

        GameObject durTxtGO = CreateTMPChild(durLine.transform, "Txt_ConfirmDuration",
            new Vector2(20, 0), new Vector2(350, 65), 36, "00:00");
        confirmDurationText = durTxtGO.GetComponent<TMP_Text>();
        confirmDurationText.color = new Color(0.8f, 0.8f, 1f, 1f);

        // --- YES button ---
        confirmYesBtn = CreatePopupButton(panel.transform, "Btn_ConfirmYes",
            new Vector2(-90, 0), new Vector2(200, 75),
            new Color(0.1f, 0.5f, 0.15f, 1f), "UPGRADE", 34,
            new Vector2(0.06f, 0.04f), new Vector2(0.48f, 0.24f));
        confirmYesBtn.onClick.AddListener(OnConfirmYes);

        // --- NO button ---
        confirmNoBtn = CreatePopupButton(panel.transform, "Btn_ConfirmNo",
            new Vector2(90, 0), new Vector2(200, 75),
            new Color(0.6f, 0.12f, 0.12f, 1f), "CANCEL", 34,
            new Vector2(0.52f, 0.04f), new Vector2(0.94f, 0.24f));
        confirmNoBtn.onClick.AddListener(OnConfirmNo);

        confirmPopupRoot.SetActive(false);
    }

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
    // ========= Skip Popup (created at runtime) ===================
    // =============================================================
    void CreateSkipPopup()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // --- Root overlay ---
        skipPopupRoot = new GameObject("SkipPopup_Overlay");
        skipPopupRoot.transform.SetParent(canvas.transform, false);
        skipPopupRoot.layer = 5;

        RectTransform rootRT = skipPopupRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        skipPopupOverlay = skipPopupRoot.AddComponent<Image>();
        skipPopupOverlay.color = new Color(0f, 0f, 0f, 0.6f);
        skipPopupOverlay.raycastTarget = true;

        // --- Panel: large, nearly full-screen with small margins ---
        skipPopupPanel = new GameObject("SkipPopup_Panel");
        skipPopupPanel.transform.SetParent(skipPopupRoot.transform, false);
        skipPopupPanel.layer = 5;

        RectTransform panelRT = skipPopupPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.05f, 0.2f);
        panelRT.anchorMax = new Vector2(0.95f, 0.8f);
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        Image panelBg = skipPopupPanel.AddComponent<Image>();
        if (confirmDialogSprite != null)
        {
            panelBg.sprite = confirmDialogSprite;
            panelBg.type = Image.Type.Simple;
            panelBg.preserveAspect = false;
            // No MAT material on window background - keep it clean
        }
        else
        {
            panelBg.color = new Color(0.12f, 0.06f, 0.22f, 0.95f);
        }

        // --- Title text ---
        CreateTMPChild(skipPopupPanel.transform, "Txt_SkipTitle",
            new Vector2(0, 0), new Vector2(460, 80), 44, "Skip Upgrade?",
            false, new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.95f));

        // --- Time remaining text ---
        GameObject timeGO = CreateTMPChild(skipPopupPanel.transform, "Txt_SkipTime",
            new Vector2(0, 0), new Vector2(460, 65), 36, "00:00",
            false, new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.7f));
        skipPopupTimeText = timeGO.GetComponent<TMP_Text>();

        // --- Cost line: [MC Icon] cost  (You have: [MC Icon] amount) ---
        GameObject costLineGO = new GameObject("CostLine_Skip");
        costLineGO.transform.SetParent(skipPopupPanel.transform, false);
        costLineGO.layer = 5;
        RectTransform costLineRT = costLineGO.AddComponent<RectTransform>();
        costLineRT.anchorMin = new Vector2(0.05f, 0.28f);
        costLineRT.anchorMax = new Vector2(0.95f, 0.48f);
        costLineRT.offsetMin = Vector2.zero;
        costLineRT.offsetMax = Vector2.zero;

        // MC icon in skip popup (same size as Gold icon in confirm popup: 70x70)
        if (CurrencyIcons.ManaCrystal != null)
            CurrencyIcons.CreateIcon(costLineGO.transform, "Img_MCIcon_Skip",
                CurrencyIcons.ManaCrystal, new Vector2(-140, 0), new Vector2(70, 70));

        GameObject costGO = CreateTMPChild(costLineGO.transform, "Txt_SkipPopupCost",
            new Vector2(30, 0), new Vector2(350, 60), 34, "0",
            false, new Vector2(0.1f, 0f), new Vector2(0.95f, 1f));
        skipPopupCostText = costGO.GetComponent<TMP_Text>();

        // --- SKIP button ---
        skipPopupSkipBtn = CreatePopupButton(skipPopupPanel.transform, "Btn_SkipPopup",
            new Vector2(0, 0), new Vector2(250, 75),
            new Color(0.2f, 0.1f, 0.35f, 0.85f), "SKIP", 38,
            new Vector2(0.15f, 0.04f), new Vector2(0.85f, 0.24f));
        skipPopupSkipBtn.onClick.AddListener(OnPopupSkipPressed);

        // --- Close button (X, top-right) ---
        GameObject closeBtnGO = new GameObject("Btn_CloseSkipPopup");
        closeBtnGO.transform.SetParent(skipPopupPanel.transform, false);
        closeBtnGO.layer = 5;

        RectTransform closeBtnRT = closeBtnGO.AddComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(1f, 1f);
        closeBtnRT.anchorMax = new Vector2(1f, 1f);
        closeBtnRT.anchoredPosition = new Vector2(-15, -15);
        closeBtnRT.sizeDelta = new Vector2(55, 55);

        Image closeBtnImg = closeBtnGO.AddComponent<Image>();
        closeBtnImg.color = new Color(0.8f, 0.15f, 0.15f, 1f);

        skipPopupCloseBtn = closeBtnGO.AddComponent<Button>();
        skipPopupCloseBtn.targetGraphic = closeBtnImg;
        skipPopupCloseBtn.onClick.AddListener(CloseSkipPopup);

        CreateTMPChild(closeBtnGO.transform, "Txt_X",
            Vector2.zero, Vector2.zero, 32, "X", true);

        skipPopupRoot.SetActive(false);
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

    /// <summary>
    /// Create a small icon Image (for gold coin / clock / etc.) inside a line row.
    /// </summary>
    void CreateIconImage(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.layer = 5;

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
    }

    Button CreatePopupButton(Transform parent, string name, Vector2 pos, Vector2 size,
        Color bgColor, string label, float fontSize,
        Vector2 anchorMin = default, Vector2 anchorMax = default)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        btnGO.layer = 5;

        RectTransform btnRT = btnGO.AddComponent<RectTransform>();
        // Use anchors if provided (non-zero), otherwise use fixed position
        if (anchorMin != Vector2.zero || anchorMax != Vector2.zero)
        {
            btnRT.anchorMin = anchorMin;
            btnRT.anchorMax = anchorMax;
            btnRT.offsetMin = Vector2.zero;
            btnRT.offsetMax = Vector2.zero;
        }
        else
        {
            btnRT.anchorMin = new Vector2(0.5f, 0.5f);
            btnRT.anchorMax = new Vector2(0.5f, 0.5f);
            btnRT.anchoredPosition = pos;
            btnRT.sizeDelta = size;
        }

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = bgColor;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        cb.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        cb.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
        btn.colors = cb;

        CreateTMPChild(btnGO.transform, "Txt_" + name,
            Vector2.zero, Vector2.zero, fontSize, label, true);

        return btn;
    }

    GameObject CreateTMPChild(Transform parent, string name, Vector2 pos, Vector2 size,
        float fontSize, string text, bool stretch = false,
        Vector2 anchorMin = default, Vector2 anchorMax = default)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.layer = 5;

        RectTransform rt = go.AddComponent<RectTransform>();

        // Use anchor-based positioning if anchors are provided
        if (anchorMin != Vector2.zero || anchorMax != Vector2.zero)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        else if (stretch)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        else
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.richText = true;

        return go;
    }

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
