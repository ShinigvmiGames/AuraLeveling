using UnityEngine;
[CreateAssetMenu(menuName = "AuraLeveling/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    // ✅ Rarity ist NICHT mehr hier – wird beim Craften/Droppen dynamisch bestimmt.

    [Header("Identity")]
    public string itemName;
    public Sprite icon;

    [Header("Rules")]
    public EquipmentSlot slot;
    public PlayerClass[] allowedClasses; // leer = alle Klassen, gesetzt = nur diese

    [Header("Quality")]
    public ItemQuality[] allowedQualities; // welche Qualities dieses Item haben darf (MUSS gesetzt werden)
}
