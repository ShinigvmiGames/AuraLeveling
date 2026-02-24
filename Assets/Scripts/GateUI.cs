using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GateUI : MonoBehaviour
{
    [Header("Core")]
    public GateManager gateManager;
    public PlayerStats player;

    [Header("Cards (3 in Hierarchy)")]
    public GateCardUI[] cards = new GateCardUI[3];

    [Header("Card Backgrounds (drag 3 sprites here)")]
    public Sprite[] cardBackgrounds;

    [Header("Panels")]
    public GameObject panelSelectGate;
    public GameObject panelGateRunning;

    [Header("Energy Display (SelectGate Panel)")]
    public TMP_Text energyText;
    public EnergySystem energySystem;

    [Header("Energy Recharge Button (SelectGate Panel)")]
    public Button btnRechargeEnergy;       // der "+" Button neben der Energy-Anzeige
    public TMP_Text rechargeCountText;     // optional: zeigt "7/10" verbleibende Aufladungen

    [Header("Recharge Confirm Popup")]
    public GameObject panelRechargeConfirm;   // das Popup Panel (anfangs disabled)
    public TMP_Text rechargeTitleText;        // "[3/10 recharged today]"
    public TMP_Text rechargeBodyText;         // optional: Fragetext (kann auch statisch in Unity sein)
    public Button btnConfirmRecharge;         // Confirm Button im Popup
    public Button btnCloseRecharge;           // Close/X Button im Popup

    [Header("Running UI")]
    public TMP_Text countdownText;
    public Image runningRankImage;         // Rank als Bild (ersetzt runningRankText)
    public RankSpriteSet rankSprites;      // gleiche ScriptableObject wie bei GateCardUI
    public Image runningGateImage;
    public Image runningBgImage;
    public Sprite[] runningGateBackgrounds;

    [Header("Gate Progress Bar (Running Panel)")]
    public Image gateProgressBarBg;        // Background-Image der Bar
    public Image gateProgressBarFill;      // Fill-Image (Filled, Left-to-Right)
    public Sprite barBgNormal;             // normale Bar Background Sprite
    public Sprite barFillNormal;           // normale Bar Fill Sprite
    public Sprite barBgSRank;             // S-Rank Bar Background Sprite (rot/schwarz)
    public Sprite barFillSRank;           // S-Rank Bar Fill Sprite (rot/schwarz)

    [Header("Gate Sprites (Running Panel)")]
    public Sprite gateNormalSprite;  // blaues Gate (A-E Rank)
    public Sprite gateSRankSprite;   // rotes Gate (S-Rank)
    public float gateRotateSpeed = 90f;

    [Header("Skip Button (Running Panel)")]
    public Button btnSkipGate;             // "SKIP 1 (MC Icon)" Button
    public TMP_Text skipButtonText;        // optional: falls du den Text per Code setzen willst

    bool skipBound = false;
    bool rechargeBound = false;
    bool confirmBound = false;

    void OnEnable()
    {
        if (gateManager == null) gateManager = FindObjectOfType<GateManager>();
        if (energySystem == null) energySystem = FindObjectOfType<EnergySystem>();
        if (player == null) player = FindObjectOfType<PlayerStats>();

        BindButtons();

        if (gateManager != null)
        {
            gateManager.ResolveActiveGateIfReady();
            gateManager.EnsureGates();
        }

        ApplyRandomBackground();
        Refresh();
        RefreshEnergy();
    }

    void BindButtons()
    {
        if (!skipBound && btnSkipGate != null)
        {
            btnSkipGate.onClick.AddListener(OnSkipPressed);
            skipBound = true;
        }
        if (!rechargeBound && btnRechargeEnergy != null)
        {
            btnRechargeEnergy.onClick.AddListener(OnRechargePressed);
            rechargeBound = true;
        }
        if (!confirmBound)
        {
            if (btnConfirmRecharge != null)
                btnConfirmRecharge.onClick.AddListener(OnConfirmRecharge);
            if (btnCloseRecharge != null)
                btnCloseRecharge.onClick.AddListener(CloseRechargePopup);
            confirmBound = true;
        }

        // Popup anfangs ausblenden
        if (panelRechargeConfirm != null)
            panelRechargeConfirm.SetActive(false);
    }

    void Update()
    {
        if (gateManager == null) return;

        bool hasActiveGate = gateManager.activeGate != null;
        float remaining = gateManager.GetRemainingGateSeconds();
        bool running = hasActiveGate && remaining > 0.01f;
        bool readyToResolve = gateManager.IsActiveGateReadyToResolve();

        if (readyToResolve)
        {
            gateManager.ResolveActiveGateIfReady();
            gateManager.EnsureGates();
            Refresh();
            return;
        }

        if (panelSelectGate != null) panelSelectGate.SetActive(!running);
        if (panelGateRunning != null) panelGateRunning.SetActive(running);

        if (!running && !hasActiveGate)
        {
            if (gateManager.availableGates == null || gateManager.availableGates.Count == 0)
            {
                gateManager.EnsureGates();
                Refresh();
            }
        }

        RefreshEnergy();

        if (!running) return;

        // --- Running Panel Updates ---
        int sec = Mathf.CeilToInt(remaining);
        if (countdownText != null) countdownText.text = FormatTime(sec);

        bool isSRank = gateManager.activeGate != null && gateManager.activeGate.rank == GateRank.SRank;

        // Progress Bar — Sprites tauschen je nach Rank
        if (gateProgressBarFill != null)
        {
            gateProgressBarFill.fillAmount = gateManager.GetGateProgress01();
            Sprite targetFill = isSRank ? barFillSRank : barFillNormal;
            if (targetFill != null) gateProgressBarFill.sprite = targetFill;
        }
        if (gateProgressBarBg != null)
        {
            Sprite targetBg = isSRank ? barBgSRank : barBgNormal;
            if (targetBg != null) gateProgressBarBg.sprite = targetBg;
        }

        // Rank Image (statt Text)
        if (runningRankImage != null && rankSprites != null && gateManager.activeGate != null)
        {
            Sprite rankSprite = rankSprites.Get(gateManager.activeGate.rank);
            if (rankSprite != null) runningRankImage.sprite = rankSprite;
        }

        // Gate Sprite + Rotation
        if (runningGateImage != null)
        {
            runningGateImage.type = Image.Type.Simple;

            Sprite targetSprite = isSRank ? gateSRankSprite : gateNormalSprite;
            if (targetSprite != null && runningGateImage.sprite != targetSprite)
                runningGateImage.sprite = targetSprite;

            runningGateImage.transform.Rotate(0, 0, -gateRotateSpeed * Time.deltaTime);
        }

    }

    // ==================== Skip Gate ====================
    void OnSkipPressed()
    {
        if (gateManager == null || player == null) return;

        if (gateManager.SkipGate(player))
        {
            gateManager.EnsureGates();
            Refresh();
        }
    }

    // ==================== Energy Recharge ====================
    void OnRechargePressed()
    {
        if (energySystem == null || player == null) return;
        if (panelRechargeConfirm == null)
        {
            // Kein Popup vorhanden → direkt kaufen (fallback)
            energySystem.BuyEnergyWithMC(player);
            RefreshEnergy();
            return;
        }

        // Popup öffnen und Texte setzen
        int used = energySystem.premiumEnergyBoughtToday;
        int max = energySystem.maxRechargesPerDay;

        if (rechargeTitleText != null)
            rechargeTitleText.text = $"{used}/{max} recharged today";

        if (rechargeBodyText != null)
            rechargeBodyText.text = $"Recharge {energySystem.energyPerRecharge} Energy for 1   ?";

        panelRechargeConfirm.SetActive(true);
        panelRechargeConfirm.transform.SetAsLastSibling();
    }

    void OnConfirmRecharge()
    {
        if (energySystem == null || player == null) return;

        energySystem.BuyEnergyWithMC(player);
        RefreshEnergy();
        CloseRechargePopup();
    }

    void CloseRechargePopup()
    {
        if (panelRechargeConfirm != null)
            panelRechargeConfirm.SetActive(false);
    }

    // ==================== Refresh ====================
    public void Refresh()
    {
        if (gateManager == null) return;

        var list = gateManager.availableGates;
        if (list == null) return;

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null) continue;

            if (i < list.Count)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].Setup(list[i], i, gateManager);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }
    }

    void ApplyRandomBackground()
    {
        if (runningBgImage == null) return;
        if (runningGateBackgrounds == null || runningGateBackgrounds.Length == 0) return;

        int idx = Random.Range(0, runningGateBackgrounds.Length);
        runningBgImage.sprite = runningGateBackgrounds[idx];
        runningBgImage.color = Color.white;
    }

    void RefreshEnergy()
    {
        if (energyText == null) return;
        if (energySystem == null) energySystem = FindObjectOfType<EnergySystem>();
        if (energySystem == null) return;

        energyText.text = $"{energySystem.currentEnergy}/{energySystem.maxEnergy}";

        // Recharge count anzeigen (optional)
        if (rechargeCountText != null)
            rechargeCountText.text = $"{energySystem.GetRemainingRecharges()}/{energySystem.maxRechargesPerDay}";

        // Recharge Button deaktivieren wenn Limit erreicht
        if (btnRechargeEnergy != null)
            btnRechargeEnergy.interactable = energySystem.GetRemainingRecharges() > 0;
    }

    string FormatTime(int seconds)
    {
        int m = seconds / 60;
        int s = seconds % 60;
        return $"{m:00}:{s:00}";
    }
}
