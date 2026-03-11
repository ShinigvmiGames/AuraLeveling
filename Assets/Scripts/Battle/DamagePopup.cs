using TMPro;
using UnityEngine;

/// <summary>
/// Floating damage/heal/skill number that rises and fades out.
/// Normal:       white 36pt  "-1.423.213"
/// Crit:         red 48pt    "-1.423.213!"
/// Heal:         green 36pt  "+500"
/// Dodge:        cyan 42pt   "DODGE!"
/// Stun:         yellow 42pt "STUNNED!"
/// Berserk:      orange 42pt "BERSERK!"
/// DoubleDamage: purple 48pt "-2.846.426 ×2"
/// Revive:       green 48pt  "UNDYING!"
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
    public Color dodgeColor = new Color(0.4f, 0.9f, 1f);         // cyan
    public Color stunColor = new Color(1f, 0.85f, 0.2f);         // yellow-orange
    public Color berserkColor = new Color(1f, 0.55f, 0.1f);      // orange
    public Color doubleDamageColor = new Color(0.8f, 0.3f, 1f);  // purple
    public Color reviveColor = new Color(0.2f, 1f, 0.4f);        // bright green

    [Header("Sizes")]
    public float normalFontSize = 36f;
    public float critFontSize = 48f;
    public float healFontSize = 36f;
    public float skillFontSize = 42f;
    public float bigSkillFontSize = 48f;

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
            case PopupType.Dodge:
                label.text = "DODGE!";
                label.color = dodgeColor;
                label.fontSize = skillFontSize;
                break;
            case PopupType.Stun:
                label.text = "STUNNED!";
                label.color = stunColor;
                label.fontSize = skillFontSize;
                break;
            case PopupType.Berserk:
                label.text = "BERSERK!";
                label.color = berserkColor;
                label.fontSize = skillFontSize;
                break;
            case PopupType.DoubleDamage:
                label.text = "-" + FormatNumber(amount) + " ×2";
                label.color = doubleDamageColor;
                label.fontSize = bigSkillFontSize;
                break;
            case PopupType.Revive:
                label.text = "UNDYING!";
                label.color = reviveColor;
                label.fontSize = bigSkillFontSize;
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
    Heal,
    Dodge,
    Stun,
    Berserk,
    DoubleDamage,
    Revive
}
