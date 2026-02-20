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

    // ===== Combat Substats =====
    public int weaponDamage;    // primarily on MainHand/OffHand
    public int armor;           // primarily on armor pieces
    public float critRate;      // %, max ~5% per item
    public float critDamage;    // %, max ~20% per item
    public float speed;         // flat, max ~15 per item

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