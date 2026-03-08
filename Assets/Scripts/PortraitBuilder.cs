using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a layered character portrait from 4 feature sprites.
/// Attach to a parent GameObject that contains 4 child Images (bottom to top):
///   0: SkinColor, 1: Face, 2: Hair, 3: Clothing
///
/// Call Build() with a CharacterData or individual params to set all layers.
/// </summary>
public class PortraitBuilder : MonoBehaviour
{
    [Header("Portrait Layers (assign 4 Images, bottom to top)")]
    public Image imgSkinColor;
    public Image imgFace;
    public Image imgHair;
    public Image imgClothing;

    [Header("Sprite Data (assign in Inspector)")]
    public PortraitSpriteSet spriteSet;

    public void Build(CharacterData data)
    {
        if (data == null || spriteSet == null) return;
        Build(data.gender, data.skinColorVariant, data.faceVariant,
              data.hairVariant, data.clothingVariant);
    }

    public void Build(GenderType gender, int skinColor, int face, int hair, int clothing)
    {
        if (spriteSet == null) return;

        SetLayer(imgSkinColor, spriteSet.GetFeature(gender, PortraitFeature.SkinColor, skinColor));
        SetLayer(imgFace, spriteSet.GetFeature(gender, PortraitFeature.Face, face));
        SetLayer(imgHair, spriteSet.GetFeature(gender, PortraitFeature.Hair, hair));
        SetLayer(imgClothing, spriteSet.GetFeature(gender, PortraitFeature.Clothing, clothing));
    }

    /// <summary>
    /// Update a single feature layer (used during creation when player picks variants).
    /// </summary>
    public void SetFeature(PortraitFeature feature, GenderType gender, int variant)
    {
        if (spriteSet == null) return;

        Sprite sprite = spriteSet.GetFeature(gender, feature, variant);
        switch (feature)
        {
            case PortraitFeature.SkinColor: SetLayer(imgSkinColor, sprite); break;
            case PortraitFeature.Face:      SetLayer(imgFace, sprite); break;
            case PortraitFeature.Hair:      SetLayer(imgHair, sprite); break;
            case PortraitFeature.Clothing:  SetLayer(imgClothing, sprite); break;
        }
    }

    static void SetLayer(Image img, Sprite sprite)
    {
        if (img == null) return;
        img.sprite = sprite;
        img.enabled = (sprite != null);
        if (sprite != null) img.color = Color.white;
    }
}

public enum PortraitFeature
{
    SkinColor,
    Face,
    Hair,
    Clothing
}
