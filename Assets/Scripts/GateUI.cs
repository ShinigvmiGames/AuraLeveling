using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GateUI : MonoBehaviour
{
    [Header("Core")]
    public GateManager gateManager;

    [Header("Cards (3 in Hierarchy)")]
    public GateCardUI[] cards = new GateCardUI[3];

    [Header("Card Backgrounds (drag 3 sprites here)")]
    public Sprite[] cardBackgrounds;

    [Header("Panels")]
    public GameObject panelSelectGate;
    public GameObject panelGateRunning;

    [Header("Energy Display (SelectGate Panel)")]
    public TMP_Text energyText; // zeigt z.B. "57/100"
    public EnergySystem energySystem; // optional, wird per FindObjectOfType gesucht

    [Header("Running UI")]
    public TMP_Text countdownText;
    public TMP_Text runningRankText;
    public Image progressFill;
    public Image runningGateImage;
    public Image runningBgImage;
    public Sprite[] runningGateBackgrounds;

    void OnEnable()
    {
        if (gateManager == null) gateManager = FindObjectOfType<GateManager>();
        if (energySystem == null) energySystem = FindObjectOfType<EnergySystem>();

        // Try to resolve a finished gate when player opens this screen
        if (gateManager != null)
        {
            gateManager.ResolveActiveGateIfReady();
            gateManager.EnsureGates();
        }

        ApplyRandomBackground();
        Refresh();
        RefreshEnergy();
    }

    void Update()
    {
        if (gateManager == null) return;

        bool hasActiveGate = gateManager.activeGate != null;
        float remaining = gateManager.GetRemainingGateSeconds();
        bool running = hasActiveGate && remaining > 0.01f;
        bool readyToResolve = gateManager.IsActiveGateReadyToResolve();

        // If gate just finished, resolve it and regenerate
        if (readyToResolve)
        {
            gateManager.ResolveActiveGateIfReady();
            gateManager.EnsureGates();
            Refresh();
            return;
        }

        if (panelSelectGate != null) panelSelectGate.SetActive(!running);
        if (panelGateRunning != null) panelGateRunning.SetActive(running);

        // If not running and no gates available, regenerate
        if (!running && !hasActiveGate)
        {
            if (gateManager.availableGates == null || gateManager.availableGates.Count == 0)
            {
                gateManager.EnsureGates();
                Refresh();
            }
        }

        // Energy-Anzeige immer aktualisieren (auch im SelectGate Panel)
        RefreshEnergy();

        if (!running) return;

        int sec = Mathf.CeilToInt(remaining);
        if (countdownText != null) countdownText.text = FormatTime(sec);
        if (progressFill != null) progressFill.fillAmount = gateManager.GetGateProgress01();

        if (runningRankText != null && gateManager.activeGate != null)
            runningRankText.text = $"{gateManager.activeGate.rank}";

        if (runningGateImage != null)
            runningGateImage.transform.Rotate(0, 0, 90f * Time.deltaTime);
    }

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

    /// <summary>
    /// Picks a random sprite from runningGateBackgrounds and assigns it
    /// to the runningBgImage so the player sees an actual background
    /// instead of a plain white image.
    /// </summary>
    void ApplyRandomBackground()
    {
        if (runningBgImage == null) return;
        if (runningGateBackgrounds == null || runningGateBackgrounds.Length == 0) return;

        int idx = Random.Range(0, runningGateBackgrounds.Length);
        runningBgImage.sprite = runningGateBackgrounds[idx];
        runningBgImage.color = Color.white; // ensure full opacity
    }

    void RefreshEnergy()
    {
        if (energyText == null) return;
        if (energySystem == null) energySystem = FindObjectOfType<EnergySystem>();
        if (energySystem == null) return;

        energyText.text = $"{energySystem.currentEnergy}/{energySystem.maxEnergy}";
    }

    string FormatTime(int seconds)
    {
        int m = seconds / 60;
        int s = seconds % 60;
        return $"{m:00}:{s:00}";
    }
}
