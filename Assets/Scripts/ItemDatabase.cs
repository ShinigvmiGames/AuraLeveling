using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "AuraLeveling/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemDefinition> allItems = new List<ItemDefinition>();

    /// <summary>
    /// Gibt alle Items zurück, die die Klasse tragen darf (alle Qualities).
    /// Waffen/OffHand: nur wenn allowedClasses die Klasse enthält.
    /// Alles andere: immer erlaubt (egal was in allowedClasses steht).
    /// </summary>
    public List<ItemDefinition> GetFor(PlayerClass pc)
    {
        List<ItemDefinition> result = new List<ItemDefinition>();
        foreach (var it in allItems)
        {
            if (it == null) continue;
            if (!IsClassAllowed(it, pc)) continue;
            result.Add(it);
        }
        return result;
    }

    /// <summary>
    /// Gibt alle Items zurück, die die Klasse tragen darf UND die gewünschte Quality haben.
    /// Ablauf: Erst Quality rollen, dann aus diesem Pool ein Item wählen.
    /// </summary>
    public List<ItemDefinition> GetFor(PlayerClass pc, ItemQuality quality)
    {
        List<ItemDefinition> result = new List<ItemDefinition>();
        foreach (var it in allItems)
        {
            if (it == null) continue;
            if (it.quality != quality) continue;
            if (!IsClassAllowed(it, pc)) continue;
            result.Add(it);
        }
        return result;
    }

    static bool IsClassAllowed(ItemDefinition it, PlayerClass pc)
    {
        bool isWeapon = (it.slot == EquipmentSlot.MainHand ||
                         it.slot == EquipmentSlot.OffHand);

        if (!isWeapon) return true;

        if (it.allowedClasses == null || it.allowedClasses.Length == 0)
            return false;

        for (int i = 0; i < it.allowedClasses.Length; i++)
            if (it.allowedClasses[i] == pc) return true;

        return false;
    }
}
