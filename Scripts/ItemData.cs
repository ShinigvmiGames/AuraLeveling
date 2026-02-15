using UnityEngine;

[System.Serializable]
public class ItemData
{
    // ===== Equip =====
    public EquipmentSlot slot;

    // ===== Basis =====
    public string itemName;
    public ItemRarity rarity;

    // ===== Level =====
    // Immer gleich dem Spielerlevel beim Craften
    public int itemLevel;

    // ===== Main Stat Boni =====
    public int bonusSTR;
    public int bonusDEX;
    public int bonusINT;
    public int bonusVIT;

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