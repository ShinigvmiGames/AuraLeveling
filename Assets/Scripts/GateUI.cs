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

    [Header("Running UI")]
    public TMP_Text countdownText;
    public TMP_Text runningRankText;
    public Image progressFill;
    public Image runningGateImage;
    public Image runningBgImage;
    public Sprite[] runningGateBackgrounds;

    [Header("Gate Sprites (Running Panel)")]
    public Sprite gateNormalSprite;  // blaues Gate (A-E Rank)
    public Sprite gateSRankSprite;   // rotes Gate (S-Rank)
    public float gateRotateSpeed = 90f;

    [Header("Pulsing Glow (Running Panel)")]
    public Image glowImage;               // Glow-Image hinter dem Gate
    public Color glowNormalColor = new Color(0.3f, 0.5f, 1.0f, 0.4f);  // blau
    public Color glowSRankColor  = new Color(1.0f, 0.15f, 0.1f, 0.5f); // rot
    [Range(0.5f, 3f)]
    public float glowPulseSpeed = 1.5f;   // wie schnell der Glow pulsiert
    [Range(0.1f, 0.5f)]
    public float glowMinAlpha = 0.2f;     // niedrigster Alpha
    [Range(0.6f, 1.0f)]
    public float glowMaxAlphaNormal = 0.7f;
    [Range(0.8f, 1.0f)]
    public float glowMaxAlphaSRank = 1.0f; // S-Rank pulsiert heftiger
    [Range(0.9f, 1.3f)]
    public float glowScaleMin = 0.95f;
    [Range(1.0f, 1.5f)]
    public float glowScaleMaxNormal = 1.1f;
    [Range(1.1f, 1.6f)]
    public float glowScaleMaxSRank = 1.25f; // S-Rank skaliert mehr

    [Header("Skip Button (Running Panel)")]
    public Button btnSkipGate;             // "SKIP 1 (MC Icon)" Button
    public TMP_Text skipButtonText;        // optional: falls du den Text per Code setzen willst

    bool skipBound = false;
    bool rechargeBound = false;

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
        if (progressFill != null) progressFill.fillAmount = gateManager.GetGateProgress01();

        if (runningRankText != null && gateManager.activeGate != null)
            runningRankText.text = $"{gateManager.activeGate.rank}";

        bool isSRank = gateManager.activeGate != null && gateManager.activeGate.rank == GateRank.SRank;

        // Gate Sprite + Rotation
        if (runningGateImage != null)
        {
            Sprite targetSprite = isSRank ? gateSRankSprite : gateNormalSprite;
            if (targetSprite != null && runningGateImage.sprite != targetSprite)
                runningGateImage.sprite = targetSprite;

            runningGateImage.transform.Rotate(0, 0, -gateRotateSpeed * Time.deltaTime);
        }

        // Pulsing Glow
        UpdateGlow(isSRank);
    }

    // ==================== Pulsing Glow ====================
    void UpdateGlow(bool isSRank)
    {
        if (glowImage == null) return;

        glowImage.enabled = true;

        float t = (Mathf.Sin(Time.time * glowPulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f; // 0..1 sine wave

        // Alpha pulsing
        float maxAlpha = isSRank ? glowMaxAlphaSRank : glowMaxAlphaNormal;
        float alpha = Mathf.Lerp(glowMinAlpha, maxAlpha, t);

        // Color
        Color baseColor = isSRank ? glowSRankColor : glowNormalColor;
        glowImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

        // Scale pulsing (S-Rank skaliert mehr)
        float scaleMax = isSRank ? glowScaleMaxSRank : glowScaleMaxNormal;
        float scale = Mathf.Lerp(glowScaleMin, scaleMax, t);
        glowImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
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

        energySystem.BuyEnergyWithMC(player);
        RefreshEnergy();
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
