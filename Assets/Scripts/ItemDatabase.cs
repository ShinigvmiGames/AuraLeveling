using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "AuraLeveling/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemDefinition> allItems = new List<ItemDefinition>();

    /// <summary>
    /// Returns all items the class is allowed to equip (all qualities).
    /// Weapons/OffHand: only if allowedClasses contains the class.
    /// Everything else: always allowed (regardless of allowedClasses).
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
    /// Returns all items the class is allowed to equip AND that match the desired quality.
    /// Flow: Roll quality first, then pick an item from this pool.
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
