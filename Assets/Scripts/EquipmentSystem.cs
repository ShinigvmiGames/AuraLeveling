using System.Collections.Generic;
using UnityEngine;

public class EquipmentSystem : MonoBehaviour
{
    public PlayerStats player;

    // equipped items by slot
    private Dictionary<EquipmentSlot, ItemData> equipped = new Dictionary<EquipmentSlot, ItemData>();

    public System.Action onChanged;

    void Start()
    {
        if (player == null) player = FindObjectOfType<PlayerStats>();
        RebuildBonuses();
    }

    public ItemData GetEquipped(EquipmentSlot slot)
    {
        if (equipped.TryGetValue(slot, out var item)) return item;
        return null;
    }

    /// <summary>
    /// Equip item and return the previously equipped item (if any).
    /// </summary>
    public ItemData Equip(ItemData item)
    {
        if (item == null || player == null) return null;

        ItemData replaced = null;
        if (equipped.ContainsKey(item.slot))
            replaced = equipped[item.slot];

        equipped[item.slot] = item;

        RebuildBonuses();
        onChanged?.Invoke();

        Debug.Log($"Equipped: {item.itemName} ({item.rarity}) in {item.slot}");
        return replaced;
    }

    public ItemData Unequip(EquipmentSlot slot)
    {
        if (!equipped.ContainsKey(slot)) return null;

        var removed = equipped[slot];
        equipped.Remove(slot);

        RebuildBonuses();
        onChanged?.Invoke();
        return removed;
    }

    void RebuildBonuses()
    {
        // Reset all bonuses — main stats
        player.bonusSTR = 0;
        player.bonusDEX = 0;
        player.bonusINT = 0;
        player.bonusVIT = 0;
        player.bonusAuraPercent = 0f;

        // Reset all bonuses — weapon damage
        player.bonusWeaponDmgMin = 0;
        player.bonusWeaponDmgMax = 0;

        // Reset all bonuses — combat substats
        player.bonusArmor = 0;
        player.bonusCritRate = 0f;
        player.bonusCritDamage = 0f;
        player.bonusSpeed = 0f;

        foreach (var kv in equipped)
        {
            var it = kv.Value;

            // Main stats
            player.bonusSTR += it.bonusSTR;
            player.bonusDEX += it.bonusDEX;
            player.bonusINT += it.bonusINT;
            player.bonusVIT += it.bonusVIT;
            player.bonusAuraPercent += it.auraBonusPercent;

            // Weapon damage (only MainHand/OffHand will have non-zero values)
            player.bonusWeaponDmgMin += it.weaponDamageMin;
            player.bonusWeaponDmgMax += it.weaponDamageMax;

            // Combat substats
            player.bonusArmor += it.armor;
            player.bonusCritRate += it.critRate;
            player.bonusCritDamage += it.critDamage;
            player.bonusSpeed += it.speed;
        }

        player.RecalculateStats();
    }
}
