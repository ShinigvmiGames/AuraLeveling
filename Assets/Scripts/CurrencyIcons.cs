using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Central helper for loading and placing currency icons.
/// All icons are loaded from Resources/Sprites/.
/// </summary>
public static class CurrencyIcons
{
    static Sprite _gold;
    static Sprite _manaCrystal;
    static Sprite _shadowEssence;
    static Sprite _energy;

    public static Sprite Gold => _gold ?? (_gold = Resources.Load<Sprite>("Sprites/Gold"));
    public static Sprite ManaCrystal => _manaCrystal ?? (_manaCrystal = Resources.Load<Sprite>("Sprites/ManaCrystal"));
    public static Sprite ShadowEssence => _shadowEssence ?? (_shadowEssence = Resources.Load<Sprite>("Sprites/ShadowEssence"));
    public static Sprite Energy => _energy ?? (_energy = Resources.Load<Sprite>("Sprites/Energy"));

    /// <summary>
    /// Create a small icon Image as a child of the given parent.
    /// Position is relative to the parent's center (anchored to 0.5/0.5).
    /// </summary>
    public static Image CreateIcon(Transform parent, string name, Sprite sprite, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.layer = 5; // UI

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>
    /// Create a small inline icon next to a TMP_Text component.
    /// The icon is placed to the left of the text.
    /// </summary>
    public static Image CreateIconLeftOfText(RectTransform textRT, string name, Sprite sprite, float iconSize = 36f, float gap = 4f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(textRT.parent, false);
        go.layer = 5;

        var rt = go.AddComponent<RectTransform>();
        // Match the text's anchors/position but shift left
        rt.anchorMin = textRT.anchorMin;
        rt.anchorMax = textRT.anchorMax;
        rt.anchoredPosition = textRT.anchoredPosition;
        rt.sizeDelta = new Vector2(iconSize, iconSize);

        // Place icon to the left of text area
        rt.anchoredPosition += new Vector2(-(textRT.sizeDelta.x * 0.5f + gap + iconSize * 0.5f), 0);

        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }
}
