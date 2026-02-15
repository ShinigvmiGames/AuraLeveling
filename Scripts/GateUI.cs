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
    public Sprite[] cardBackgrounds; // size 3 empfohlen

    [Header("Panels")]
    public GameObject panelSelectGate;
    public GameObject panelGateRunning;

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

        gateManager?.EnsureGates();
        Refresh();
    }

    void Update()
    {
        if (gateManager == null) return;

        bool running = gateManager.activeGate != null &&
                       gateManager.GetRemainingGateSeconds() > 0.01f;

        if (panelSelectGate != null) panelSelectGate.SetActive(!running);
        if (panelGateRunning != null) panelGateRunning.SetActive(running);

        if (!running) return;

        int sec = Mathf.CeilToInt(gateManager.GetRemainingGateSeconds());
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

                // Background sprite wählen: prefer index i, fallback random
                Sprite bg = null;
                if (cardBackgrounds != null && cardBackgrounds.Length > 0)
                {
                    int idx = i < cardBackgrounds.Length ? i : Random.Range(0, cardBackgrounds.Length);
                    bg = cardBackgrounds[idx];
                }

                cards[i].Setup(list[i], i, gateManager);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }
    }

    string FormatTime(int seconds)
    {
        int m = seconds / 60;
        int s = seconds % 60;
        return $"{m:00}:{s:00}";
    }
}