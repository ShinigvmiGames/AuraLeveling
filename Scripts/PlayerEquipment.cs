using UnityEngine;
using System.Collections.Generic;

public class PlayerEquipment : MonoBehaviour
{
    public Dictionary<EquipmentSlot, ItemData> equippedItems =
        new Dictionary<EquipmentSlot, ItemData>();

    PlayerStats playerStats;

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
    }

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
