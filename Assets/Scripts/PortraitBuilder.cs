using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a layered character portrait from base + 5 feature sprites.
/// Attach to a parent GameObject that contains 6 child Images (bottom to top):
///   0: Base, 1: Marks, 2: Mouth, 3: Eyes, 4: Hair, 5: Headgear
///
/// Call Build() with a CharacterData or individual params to set all layers.
/// Also used as a static utility via BuildInto() for external Image arrays.
/// </summary>
public class PortraitBuilder : MonoBehaviour
{
    [Header("Portrait Layers (assign 6 Images, bottom to top)")]
    public Image imgBase;
    public Image imgMarks;
    public Image imgMouth;
    public Image imgEyes;
    public Image imgHair;
    public Image imgHeadgear;

    [Header("Sprite Data (assign in Inspector)")]
    public PortraitSpriteSet spriteSet;

    public void Build(CharacterData data)
    {
        if (data == null || spriteSet == null) return;
        Build(data.race, data.gender, data.eyeVariant, data.hairVariant,
              data.marksVariant, data.mouthVariant, data.headgearVariant);
    }

    public void Build(RaceType race, GenderType gender, int eyes, int hair, int marks, int mouth, int headgear)
    {
        if (spriteSet == null) return;

        SetLayer(imgBase, spriteSet.GetBase(race, gender));
        SetLayer(imgEyes, spriteSet.GetFeature(race, gender, PortraitFeature.Eyes, eyes));
        SetLayer(imgHair, spriteSet.GetFeature(race, gender, PortraitFeature.Hair, hair));
        SetLayer(imgMarks, spriteSet.GetFeature(race, gender, PortraitFeature.Marks, marks));
        SetLayer(imgMouth, spriteSet.GetFeature(race, gender, PortraitFeature.Mouth, mouth));
        SetLayer(imgHeadgear, spriteSet.GetFeature(race, gender, PortraitFeature.Headgear, headgear));
    }

    /// <summary>
    /// Update a single feature layer (used during creation when player picks variants).
    /// </summary>
    public void SetFeature(PortraitFeature feature, RaceType race, GenderType gender, int variant)
    {
        if (spriteSet == null) return;

        Sprite sprite = spriteSet.GetFeature(race, gender, feature, variant);
        switch (feature)
        {
            case PortraitFeature.Eyes:     SetLayer(imgEyes, sprite); break;
            case PortraitFeature.Hair:     SetLayer(imgHair, sprite); break;
            case PortraitFeature.Marks:    SetLayer(imgMarks, sprite); break;
            case PortraitFeature.Mouth:    SetLayer(imgMouth, sprite); break;
            case PortraitFeature.Headgear: SetLayer(imgHeadgear, sprite); break;
        }
    }

    public void SetBase(RaceType race, GenderType gender)
    {
        if (spriteSet == null) return;
        SetLayer(imgBase, spriteSet.GetBase(race, gender));
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
    Eyes,
    Hair,
    Marks,
    Mouth,
    Headgear
}
