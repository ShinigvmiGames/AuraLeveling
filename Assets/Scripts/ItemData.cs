using UnityEngine;

[System.Serializable]
public class ItemData
{
    // ===== Equip =====
    public EquipmentSlot slot;

    // ===== Basis =====
    public string itemName;
    public ItemRarity rarity;
    public ItemQuality quality;

    // ===== Level =====
    // Immer gleich dem Spielerlevel beim Craften
    public int itemLevel;

    // ===== Main Stat Boni =====
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
    // Crit Rate, Crit Damage, Speed: from ALL item slots
    public float critRate;      // %, hard cap 100% total
    public float critDamage;    // %
    public float speed;         // flat

    // ===== Aura =====
    // Prozentualer Bonus auf ALLE Stats (z.B. +10%)
    public float auraBonusPercent;

    // Gesamtstärke des Items (für Vergleich, Balance, Gates)
    public int itemAura;

    // ===== Economy =====
    public int sellPrice;

    public ItemDefinition definition;
    public Sprite icon;
}
