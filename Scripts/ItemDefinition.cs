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

    [Header("Stat Style (weights)")]
    [Range(0f, 2f)] public float wSTR = 1f;
    [Range(0f, 2f)] public float wDEX = 1f;
    [Range(0f, 2f)] public float wINT = 1f;
    [Range(0f, 2f)] public float wVIT = 1f;
}