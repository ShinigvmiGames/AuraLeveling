using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Class")]
    public PlayerClass playerClass;
    public System.Action onStatsChanged;

    [Header("Level")]
    public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;

    [Header("Main Stats")]
    public int STR = 5;
    public int DEX = 5;
    public int INT = 5;
    public int VIT = 5;

    [Header("Unspent Stat Points")]
    public int unspentPoints = 0;

    [Header("Derived Stats")]
    public long maxHP;
    public long damage;
    public int armor;
    public float critRate;
    public float critDamage;
    public float speed;

    [Header("Aura")]
    public float auraBonusPercent = 0f;

    [Header("Money")]
    public int gold = 0;

    [Header("Crafting Currency")]
    public int shadowEssence = 10;

    [Header("Premium Currency")]
    public int manaCrystals = 0;

    [Header("Equipment Bonuses — Main Stats")]
    public int bonusSTR;
    public int bonusDEX;
    public int bonusINT;
    public int bonusVIT;
    public float bonusAuraPercent;

    [Header("Equipment Bonuses — Weapon Damage")]
    // Sum of all equipped weapon min/max damage
    public int bonusWeaponDmgMin;
    public int bonusWeaponDmgMax;

    [Header("Equipment Bonuses — Combat Substats")]
    public int bonusArmor;
    public float bonusCritRate;
    public float bonusCritDamage;
    public float bonusSpeed;

    public bool TrySpendPoint(string statName)
    {
        if (unspentPoints <= 0) return false;

        switch (statName)
        {
            case "STR": STR++; break;
            case "DEX": DEX++; break;
            case "INT": INT++; break;
            case "VIT": VIT++; break;
            default: return false;
        }

        unspentPoints--;
        RecalculateStats();
        return true;
    }

    /// <summary>
    /// Returns the player's total Aura as a long (big numbers!).
    /// statBase + damageContrib + hpContrib + combatContrib, scaled by aura bonus%.
    /// </summary>
    public long GetAura()
    {
        int effSTR = STR + bonusSTR;
        int effDEX = DEX + bonusDEX;
        int effINT = INT + bonusINT;
        int effVIT = VIT + bonusVIT;

        long statBase = (long)(effSTR + effDEX + effINT + effVIT) * 100L;
        long damageContrib = damage * 2L;
        long hpContrib = maxHP;
        long combatContrib = (long)(armor * 50f + critRate * 100f + critDamage * 50f + speed * 30f);

        long rawAura = statBase + damageContrib + hpContrib + combatContrib;

        float totalAuraBonus = (auraBonusPercent + bonusAuraPercent) / 100f;
        long finalAura = (long)(rawAura * (1f + totalAuraBonus));

        return System.Math.Max(0L, finalAura);
    }

    void Start()
    {
        RecalculateStats();
        Debug.Log("Player Stats initialized");
    }

    /// <summary>
    /// Returns the aura multiplier: 1 + (levelAuraBonus + equipAuraBonus) / 100.
    /// This multiplier is applied to ALL stats at the end of RecalculateStats.
    /// </summary>
    public float GetAuraMultiplier()
    {
        return 1f + (auraBonusPercent + bonusAuraPercent) / 100f;
    }

    /// <summary>
    /// Recalculates all derived stats from base + equipment bonuses + class passives.
    /// Aura Bonus is applied as a final multiplier to ALL stats (no double-dipping).
    ///
    /// Class Passives:
    ///   Assassin:     +20% Speed, +15% Crit Rate
    ///   Warrior:      +15% Max HP, Armor cap raised from 50% to 60%
    ///   Archer:       +25% Crit Damage, +10% Speed
    ///   Mage:         +25% Damage
    ///   Necromancer:  +15% Max HP (lifesteal 15% handled in CombatResolver)
    /// </summary>
    public void RecalculateStats()
    {
        // Effective main stats (base + equipment) — no aura yet
        int effSTR = STR + bonusSTR;
        int effDEX = DEX + bonusDEX;
        int effINT = INT + bonusINT;
        int effVIT = VIT + bonusVIT;

        // Aura bonus from level progression
        auraBonusPercent = (level - 1) * 1.5f;
        float auraMultiplier = GetAuraMultiplier();

        // ===== HP = VIT * 15 * (1 + level * 0.02) =====
        float rawHP = effVIT * 15f * (1f + level * 0.02f);
        switch (playerClass)
        {
            case PlayerClass.Warrior:     rawHP *= 1.15f; break;
            case PlayerClass.Necromancer: rawHP *= 1.15f; break;
        }

        // ===== Damage = mainStat * weaponDmg * (1 + level*0.03) =====
        int classMainStat = GetClassMainStatValue(effSTR, effDEX, effINT, effVIT);
        float avgWeaponDmg = Mathf.Max(1f, (bonusWeaponDmgMin + bonusWeaponDmgMax) / 2f);
        float rawDamage = classMainStat * avgWeaponDmg * (1f + level * 0.03f);
        if (playerClass == PlayerClass.Mage)
            rawDamage *= 1.25f;

        // ===== Armor = directly from equipped armor items =====
        int rawArmor = bonusArmor;

        // ===== Crit Rate = 15% base + bonusCritRate =====
        float rawCritRate = 15f + bonusCritRate;
        if (playerClass == PlayerClass.Assassin)
            rawCritRate += 15f;

        // ===== Crit Damage = 50% base + bonusCritDamage =====
        float rawCritDamage = 50f + bonusCritDamage;
        if (playerClass == PlayerClass.Archer)
            rawCritDamage += 25f;

        // ===== Speed = 100 + DEX*0.5 + bonusSpeed =====
        float rawSpeed = 100f + effDEX * 0.5f + bonusSpeed;
        switch (playerClass)
        {
            case PlayerClass.Assassin: rawSpeed *= 1.20f; break;
            case PlayerClass.Archer:   rawSpeed *= 1.10f; break;
        }

        // ===== Apply Aura Bonus to ALL stats =====
        maxHP = System.Math.Max(1L, (long)(rawHP * auraMultiplier));
        damage = System.Math.Max(1L, (long)(rawDamage * auraMultiplier));
        armor = Mathf.Max(0, Mathf.RoundToInt(rawArmor * auraMultiplier));
        critRate = Mathf.Clamp(rawCritRate * auraMultiplier, 0f, 100f);
        critDamage = rawCritDamage * auraMultiplier;
        speed = rawSpeed * auraMultiplier;

        onStatsChanged?.Invoke();
    }

    /// <summary>
    /// Returns the class's primary damage stat value.
    /// </summary>
    int GetClassMainStatValue(int effSTR, int effDEX, int effINT, int effVIT)
    {
        switch (playerClass)
        {
            case PlayerClass.Assassin:    return effDEX;
            case PlayerClass.Warrior:     return effSTR;
            case PlayerClass.Archer:      return effDEX;
            case PlayerClass.Mage:        return effINT;
            case PlayerClass.Necromancer: return effINT;
            default:                      return effSTR;
        }
    }

    /// <summary>
    /// Returns the class's primary stat type for cross-stat damage reduction.
    /// </summary>
    public StatType GetClassMainStatType()
    {
        switch (playerClass)
        {
            case PlayerClass.Assassin:    return StatType.DEX;
            case PlayerClass.Warrior:     return StatType.STR;
            case PlayerClass.Archer:      return StatType.DEX;
            case PlayerClass.Mage:        return StatType.INT;
            case PlayerClass.Necromancer: return StatType.INT;
            default:                      return StatType.STR;
        }
    }

    /// <summary>
    /// Returns the effective value of the class's primary damage stat,
    /// including aura bonus (so CombatResolver can use it directly).
    /// </summary>
    public int GetEffectiveMainStat()
    {
        int raw = GetClassMainStatValue(STR + bonusSTR, DEX + bonusDEX, INT + bonusINT, VIT + bonusVIT);
        return Mathf.RoundToInt(raw * GetAuraMultiplier());
    }

    /// <summary>
    /// Returns the armor damage reduction cap for this class.
    /// Warrior has 60% cap (passive), all others 50%.
    /// </summary>
    public float GetArmorCap()
    {
        return playerClass == PlayerClass.Warrior ? 0.60f : 0.50f;
    }

    // ========= Economy helpers =========
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        gold += amount;
        onStatsChanged?.Invoke();
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (gold < amount) return false;
        gold -= amount;
        onStatsChanged?.Invoke();
        return true;
    }

    public void AddEssence(int amount)
    {
        if (amount <= 0) return;
        shadowEssence += amount;
        onStatsChanged?.Invoke();
    }

    public bool SpendEssence(int amount)
    {
        if (amount <= 0) return true;
        if (shadowEssence < amount) return false;
        shadowEssence -= amount;
        onStatsChanged?.Invoke();
        return true;
    }

    // ========= ManaCrystal helpers =========
    public void AddManaCrystals(int amount)
    {
        if (amount <= 0) return;
        manaCrystals += amount;
        onStatsChanged?.Invoke();
    }

    public bool SpendManaCrystals(int amount)
    {
        if (amount <= 0) return true;
        if (manaCrystals < amount) return false;
        manaCrystals -= amount;
        onStatsChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Returns XP progress as 0..1 for UI bars.
    /// </summary>
    public float GetXPProgress01()
    {
        if (xpToNextLevel <= 0) return 1f;
        return Mathf.Clamp01((float)currentXP / xpToNextLevel);
    }

    public void GainXP(int amount)
    {
        if (amount <= 0) return;
        currentXP += amount;

        while (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }
        onStatsChanged?.Invoke();
    }

    void LevelUp()
    {
        currentXP -= xpToNextLevel;
        level++;
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.25f);
        unspentPoints += 3;

        Debug.Log("LEVEL UP! New level: " + level);
        Debug.Log("You have 3 stat points to spend!");

        RecalculateStats();
    }
}

/// <summary>
/// Stat type enum for cross-stat damage reduction system.
/// </summary>
public enum StatType
{
    STR,
    DEX,
    INT,
    VIT
}
