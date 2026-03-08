using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Utility class for quality-based glow effects on item slots.
/// Generates a procedural radial gradient sprite and applies quality colors.
/// </summary>
public static class QualityGlow
{
    static Sprite _glowSprite;

    // Quality colors
    static readonly Color EpicColor      = new Color(0.6f, 0.2f, 0.9f, 0.6f);    // lila / purple
    static readonly Color LegendaryColor = new Color(1.0f, 0.7f, 0.1f, 0.7f);    // orange-gold
    static readonly Color MythicColor    = new Color(0.7f, 0.0f, 0.1f, 0.8f);    // kaiserliches rot / imperial red

    /// <summary>
    /// Get or create the shared radial gradient glow sprite.
    /// </summary>
    public static Sprite GetGlowSprite()
    {
        if (_glowSprite != null) return _glowSprite;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = size / 2f;
        float maxDist = center;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / maxDist;

                // Smooth radial falloff
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha; // quadratic falloff for softer glow

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();

        _glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        _glowSprite.name = "QualityGlow_Procedural";

        return _glowSprite;
    }

    /// <summary>
    /// Apply quality glow to an Image component.
    /// Creates the glow image if glowImage is null (pass the parent transform).
    /// </summary>
    public static void Apply(Image glowImage, ItemQuality quality)
    {
        if (glowImage == null) return;

        switch (quality)
        {
            case ItemQuality.Epic:
                glowImage.enabled = true;
                glowImage.sprite = GetGlowSprite();
                glowImage.color = EpicColor;
                break;

            case ItemQuality.Legendary:
                glowImage.enabled = true;
                glowImage.sprite = GetGlowSprite();
                glowImage.color = LegendaryColor;
                break;

            case ItemQuality.Mythic:
                glowImage.enabled = true;
                glowImage.sprite = GetGlowSprite();
                glowImage.color = MythicColor;
                break;

            default: // Normal
                glowImage.enabled = false;
                break;
        }
    }
}
