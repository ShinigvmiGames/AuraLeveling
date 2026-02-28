using UnityEngine;

/// <summary>
/// ScriptableObject holding all portrait sprites for both genders and 5 features.
/// Create via Assets > Create > AuraLeveling > Portrait Sprite Set.
/// </summary>
[CreateAssetMenu(menuName = "AuraLeveling/Portrait Sprite Set")]
public class PortraitSpriteSet : ScriptableObject
{
    [System.Serializable]
    public class GenderSprites
    {
        public GenderType gender;

        [Header("Base (blank face)")]
        public Sprite baseSprite;

        [Header("Eyes (5 variants)")]
        public Sprite[] eyes = new Sprite[5];

        [Header("Hair (5 variants)")]
        public Sprite[] hair = new Sprite[5];

        [Header("Marks / Scars (5 variants)")]
        public Sprite[] marks = new Sprite[5];

        [Header("Mouth (5 variants)")]
        public Sprite[] mouth = new Sprite[5];

        [Header("Headgear (5 variants)")]
        public Sprite[] headgear = new Sprite[5];
    }

    [Header("Gender Entries (2: Male, Female)")]
    public GenderSprites[] entries;

    public Sprite GetBase(GenderType gender)
    {
        var entry = Find(gender);
        return entry?.baseSprite;
    }

    public Sprite GetFeature(GenderType gender, PortraitFeature feature, int variant)
    {
        var entry = Find(gender);
        if (entry == null) return null;

        variant = Mathf.Clamp(variant, 0, 4);

        Sprite[] arr = feature switch
        {
            PortraitFeature.Eyes     => entry.eyes,
            PortraitFeature.Hair     => entry.hair,
            PortraitFeature.Marks    => entry.marks,
            PortraitFeature.Mouth    => entry.mouth,
            PortraitFeature.Headgear => entry.headgear,
            _ => null
        };

        if (arr == null || variant >= arr.Length) return null;
        return arr[variant];
    }

    GenderSprites Find(GenderType gender)
    {
        if (entries == null) return null;
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].gender == gender)
                return entries[i];
        }
        return null;
    }
}
