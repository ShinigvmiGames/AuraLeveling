using TMPro;
using UnityEngine;

/// <summary>
/// Floating damage/heal number that rises and fades out.
/// Normal: white 36pt "-1.423.213"
/// Crit: red 48pt "-1.423.213!"
/// Heal: green 36pt "+500"
/// Numbers always show full value with dot separators.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    [Header("Settings")]
    public float riseDuration = 0.8f;
    public float riseDistance = 60f;
    public float fadeStartAt = 0.5f;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color critColor = Color.red;
    public Color healColor = new Color(0.3f, 1f, 0.3f);

    [Header("Sizes")]
    public float normalFontSize = 36f;
    public float critFontSize = 48f;
    public float healFontSize = 36f;

    TMP_Text label;
    RectTransform rt;
    Vector2 startPos;
    float timer;
    float startAlpha;
    bool active;

    void Awake()
    {
        label = GetComponentInChildren<TMP_Text>();
        rt = GetComponent<RectTransform>();
    }

    public void Show(long amount, PopupType type)
    {
        if (label == null) return;

        switch (type)
        {
            case PopupType.Normal:
                label.text = "-" + FormatNumber(amount);
                label.color = normalColor;
                label.fontSize = normalFontSize;
                break;
            case PopupType.Crit:
                label.text = "-" + FormatNumber(amount) + "!";
                label.color = critColor;
                label.fontSize = critFontSize;
                break;
            case PopupType.Heal:
                label.text = "+" + FormatNumber(amount);
                label.color = healColor;
                label.fontSize = healFontSize;
                break;
        }

        startPos = rt.anchoredPosition;
        startAlpha = label.color.a;
        timer = 0f;
        active = true;
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (!active) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / riseDuration);

        // Rise
        rt.anchoredPosition = startPos + Vector2.up * (riseDistance * t);

        // Fade
        if (t >= fadeStartAt)
        {
            float fadeT = (t - fadeStartAt) / (1f - fadeStartAt);
            Color c = label.color;
            c.a = Mathf.Lerp(startAlpha, 0f, fadeT);
            label.color = c;
        }

        if (t >= 1f)
        {
            active = false;
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Format number with dots as thousands separators.
    /// e.g. 1423213 → "1.423.213"
    /// </summary>
    static string FormatNumber(long value)
    {
        return value.ToString("#,0").Replace(',', '.');
    }
}

public enum PopupType
{
    Normal,
    Crit,
    Heal
}
