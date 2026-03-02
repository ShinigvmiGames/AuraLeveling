using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a layered character portrait from base + 5 feature sprites.
/// Attach to a parent GameObject that contains 6 child Images (bottom to top):
///   0: Base, 1: Marks, 2: Mouth, 3: Eyes, 4: Hair, 5: Headgear
///
/// Call Build() with a CharacterData or individual params to set all layers.
/// </summary>
public class PortraitBuilder : MonoBehaviour
{
    [Header("Portrait Layers (assign 6 Images, bottom to top)")]
    public Image imgBase;
    public Image imgHair;
    public Image imgEyes;
    public Image imgMouth;
    public Image imgClothing;
    public Image imgSpecial;

    [Header("Sprite Data (assign in Inspector)")]
    public PortraitSpriteSet spriteSet;

    public void Build(CharacterData data)
    {
        if (data == null || spriteSet == null) return;
        Build(data.gender, data.hairVariant, data.eyeVariant,
              data.mouthVariant, data.clothingVariant, data.specialVariant);
    }

    public void Build(GenderType gender, int hair, int eyes, int mouth, int clothing, int special)
    {
        if (spriteSet == null) return;

        SetLayer(imgBase, spriteSet.GetBase(gender));
        SetLayer(imgHair, spriteSet.GetFeature(gender, PortraitFeature.Hair, hair));
        SetLayer(imgEyes, spriteSet.GetFeature(gender, PortraitFeature.Eyes, eyes));
        SetLayer(imgMouth, spriteSet.GetFeature(gender, PortraitFeature.Mouth, mouth));
        SetLayer(imgClothing, spriteSet.GetFeature(gender, PortraitFeature.Clothing, clothing));
        SetLayer(imgSpecial, spriteSet.GetFeature(gender, PortraitFeature.Special, special));
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
            case PortraitFeature.Hair:     SetLayer(imgHair, sprite); break;
            case PortraitFeature.Eyes:     SetLayer(imgEyes, sprite); break;
            case PortraitFeature.Mouth:    SetLayer(imgMouth, sprite); break;
            case PortraitFeature.Clothing: SetLayer(imgClothing, sprite); break;
            case PortraitFeature.Special:  SetLayer(imgSpecial, sprite); break;
        }
    }

    public void SetBase(GenderType gender)
    {
        if (spriteSet == null) return;
        SetLayer(imgBase, spriteSet.GetBase(gender));
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
    Hair,
    Eyes,
    Mouth,
    Clothing,
    Special
}
