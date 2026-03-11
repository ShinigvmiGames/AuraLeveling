using UnityEngine;

[System.Serializable]
public class ItemData
{
    // ===== Equip =====
    public EquipmentSlot slot;

    // ===== Base =====
    public string itemName;
    public ItemRarity rarity;
    public ItemQuality quality;

    // ===== Level =====
    // Always equals the player level when crafted
    public int itemLevel;

    // ===== Main Stat Bonuses =====
    public int bonusSTR;
    public int bonusDEX;
    public int bonusINT;
    public int bonusVIT;

    // ===== Weapon Damage =====
    // MainHand: range (minDamage–maxDamage), each attack rolls between them
    // OffHand: fixed value (weaponDamageMin only, weaponDamageMax == weaponDamageMin)
    // Other slots: 0
    public int weaponDamageMin;
    public int weaponDamageMax;

    // ===== Combat Substats =====
    // Armor: only from Head, Chest, Legs, Boots
    public int armor;
    // Crit Rate, Crit Damage: from ALL item slots
    public float critRate;      // %, hard cap 100% total
    public float critDamage;    // %

    // ===== Aura =====
    // Percentage bonus on ALL stats (e.g. +10%)
    public float auraBonusPercent;

    // Total item power (for comparison, balance, gates)
    public int itemAura;

    // ===== Economy =====
    public int sellPrice;

    public ItemDefinition definition;
    public Sprite icon;
}
