using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "AuraLeveling/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemDefinition> allItems = new List<ItemDefinition>();

    /// <summary>
    /// Gibt alle Items zurück, die die Klasse tragen darf.
    /// Waffen/OffHand: nur wenn allowedClasses die Klasse enthält.
    /// Alles andere: immer erlaubt (egal was in allowedClasses steht).
    /// </summary>
    public List<ItemDefinition> GetFor(PlayerClass pc)
    {
        List<ItemDefinition> result = new List<ItemDefinition>();
        foreach (var it in allItems)
        {
            if (it == null) continue;

            bool isWeapon = (it.slot == EquipmentSlot.MainHand ||
                             it.slot == EquipmentSlot.OffHand);

            if (isWeapon)
            {
                if (it.allowedClasses == null || it.allowedClasses.Length == 0)
                    continue;
                bool found = false;
                for (int i = 0; i < it.allowedClasses.Length; i++)
                {
                    if (it.allowedClasses[i] == pc) { found = true; break; }
                }
                if (!found) continue;
            }

            result.Add(it);
        }
        return result;
    }
}