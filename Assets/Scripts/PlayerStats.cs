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
        long combatContrib = (long)(armor * 50f + critRate * 100f + critDamage * 50f);

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
    /// Recalculates all derived stats from base + equipment bonuses.
    /// Aura Bonus is applied as a final multiplier to ALL stats (no double-dipping).
    ///
    /// Damage uses sqrt compression: sqrt(mainStat * weaponAvg) * 3.0
    /// This keeps damage/HP ratio stable across all levels (~6 turns to kill).
    ///
    /// HP uses VIT floor (+ level*2) to prevent HP starvation.
    ///
    /// Class Skills (handled in CombatResolver, NOT here):
    ///   Assassin:     Shadow — 20% dodge chance
    ///   Warrior:      Berserk — 20% extra attack (chainable)
    ///   Archer:       Stun — 15% stun for 1 round
    ///   Mage:         Arcane Surge — 25% double damage
    ///   Necromancer:  Undying — revive once at 30% HP
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

        // ===== HP = (VIT + level*2) * 20 * (1 + level * 0.025) =====
        // VIT floor (+ level*2) prevents HP starvation when player dumps points into damage
        int effectiveVIT = effVIT + level * 2;
        float rawHP = effectiveVIT * 20f * (1f + level * 0.025f);

        // ===== Damage = sqrt(mainStat * weaponAvg) * 3.0 * (1 + level * 0.025) =====
        // sqrt compresses quadratic growth → linear, keeping TTK stable across levels
        int classMainStat = GetClassMainStatValue(effSTR, effDEX, effINT, effVIT);
        float avgWeaponDmg = Mathf.Max(1f, (bonusWeaponDmgMin + bonusWeaponDmgMax) / 2f);
        float compressedBase = Mathf.Sqrt(classMainStat * avgWeaponDmg) * 3.0f;
        float rawDamage = compressedBase * (1f + level * 0.025f);

        // ===== Armor = directly from equipped armor items =====
        int rawArmor = bonusArmor;

        // ===== Crit Rate = 15% base + bonusCritRate =====
        float rawCritRate = 15f + bonusCritRate;

        // ===== Crit Damage = 50% base + bonusCritDamage =====
        float rawCritDamage = 50f + bonusCritDamage;

        // ===== Apply Aura Bonus to ALL stats =====
        maxHP = System.Math.Max(1L, (long)(rawHP * auraMultiplier));
        damage = System.Math.Max(1L, (long)(rawDamage * auraMultiplier));
        armor = Mathf.Max(0, Mathf.RoundToInt(rawArmor * auraMultiplier));
        critRate = Mathf.Clamp(rawCritRate * auraMultiplier, 0f, 100f);
        critDamage = rawCritDamage * auraMultiplier;

        onStatsChanged?.Invoke();
    }

    /// <summary>
    /// Returns the class's primary damage stat value (raw, without aura).
    /// </summary>
    public int GetClassMainStatValue(int effSTR, int effDEX, int effINT, int effVIT)
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
    /// Returns the raw value of the class's primary damage stat (base + equipment, NO aura).
    /// Used by CombatResolver for the sqrt damage formula.
    /// </summary>
    public int GetRawMainStat()
    {
        return GetClassMainStatValue(STR + bonusSTR, DEX + bonusDEX, INT + bonusINT, VIT + bonusVIT);
    }

    /// <summary>
    /// Returns the effective value of the class's primary damage stat,
    /// including aura bonus (used for cross-stat defense).
    /// </summary>
    public int GetEffectiveMainStat()
    {
        int raw = GetClassMainStatValue(STR + bonusSTR, DEX + bonusDEX, INT + bonusINT, VIT + bonusVIT);
        return Mathf.RoundToInt(raw * GetAuraMultiplier());
    }

    /// <summary>
    /// Returns the armor damage reduction cap (50% for all classes).
    /// </summary>
    public float GetArmorCap()
    {
        return 0.50f;
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
