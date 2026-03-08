using UnityEngine;

/// <summary>
/// ScriptableObject holding all portrait sprites for both genders and 4 features.
/// Create via Assets > Create > AuraLeveling > Portrait Sprite Set.
/// </summary>
[CreateAssetMenu(menuName = "AuraLeveling/Portrait Sprite Set")]
public class PortraitSpriteSet : ScriptableObject
{
    [System.Serializable]
    public class GenderSprites
    {
        public GenderType gender;

        [Header("Skin Color (4 variants)")]
        public Sprite[] skinColor = new Sprite[4];

        [Header("Face (4 variants)")]
        public Sprite[] face = new Sprite[4];

        [Header("Hair (4 variants)")]
        public Sprite[] hair = new Sprite[4];

        [Header("Clothing (4 variants)")]
        public Sprite[] clothing = new Sprite[4];
    }

    [Header("Gender Entries (2: Male, Female)")]
    public GenderSprites[] entries;

    public Sprite GetFeature(GenderType gender, PortraitFeature feature, int variant)
    {
        var entry = Find(gender);
        if (entry == null) return null;

        variant = Mathf.Clamp(variant, 0, 3);

        Sprite[] arr = feature switch
        {
            PortraitFeature.SkinColor => entry.skinColor,
            PortraitFeature.Face      => entry.face,
            PortraitFeature.Hair      => entry.hair,
            PortraitFeature.Clothing  => entry.clothing,
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
