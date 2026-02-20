using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// OBSOLETE: This is a legacy duplicate of EquipmentSystem.
/// It directly modifies base STR/DEX/INT/VIT which corrupts stat points.
/// Use EquipmentSystem instead, which properly uses bonus fields.
/// This script should be removed from all GameObjects in the scene.
/// </summary>
[System.Obsolete("Use EquipmentSystem instead. This legacy script corrupts base stats.")]
public class PlayerEquipment : MonoBehaviour
{
    public Dictionary<EquipmentSlot, ItemData> equippedItems =
        new Dictionary<EquipmentSlot, ItemData>();

    PlayerStats playerStats;

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        Debug.LogError("PlayerEquipment is OBSOLETE and should be removed! Use EquipmentSystem instead.");
    }

    [System.Obsolete("Use EquipmentSystem.Equip() instead")]
    public void EquipItem(ItemData item)
    {
        if (equippedItems.ContainsKey(item.slot))
        {
            UnequipItem(item.slot);
        }

        equippedItems[item.slot] = item;
        ApplyItemStats(item);

        Debug.Log("Item ausgerüstet: " + item.itemName);
    }

    void UnequipItem(EquipmentSlot slot)
    {
        ItemData oldItem = equippedItems[slot];
        RemoveItemStats(oldItem);
        equippedItems.Remove(slot);
    }

    void ApplyItemStats(ItemData item)
    {
        playerStats.STR += item.bonusSTR;
        playerStats.DEX += item.bonusDEX;
        playerStats.INT += item.bonusINT;
        playerStats.VIT += item.bonusVIT;

        playerStats.RecalculateStats();
    }

    void RemoveItemStats(ItemData item)
    {
        playerStats.STR -= item.bonusSTR;
        playerStats.DEX -= item.bonusDEX;
        playerStats.INT -= item.bonusINT;
        playerStats.VIT -= item.bonusVIT;

        playerStats.RecalculateStats();
    }
}
