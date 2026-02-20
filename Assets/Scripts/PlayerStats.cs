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

    [Header("Equipment Bonuses — Combat Substats")]
    public int bonusWeaponDamage;
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
        Debug.Log("Player Stats initialisiert");
    }

    /// <summary>
    /// Recalculates all derived stats from base + equipment bonuses.
    /// Fixes the old double-calculation bug.
    /// </summary>
    public void RecalculateStats()
    {
        // Effective main stats (base + equipment)
        int effSTR = STR + bonusSTR;
        int effDEX = DEX + bonusDEX;
        int effINT = INT + bonusINT;
        int effVIT = VIT + bonusVIT;

        // Aura bonus from level
        auraBonusPercent = (level - 1) * 1.5f;
        float auraMultiplier = 1f + (auraBonusPercent + bonusAuraPercent) / 100f;

        // ===== Class multipliers =====
        float classDmgMult, classHPMult, classSpeedBonus;
        GetClassMultipliers(out classDmgMult, out classHPMult, out classSpeedBonus);

        // ===== HP =====
        // effVIT * 15 * (1 + level * 0.02) * auraMultiplier * classHPBonus
        float rawHP = effVIT * 15f * (1f + level * 0.02f) * auraMultiplier * classHPMult;
        maxHP = System.Math.Max(1L, (long)rawHP);

        // ===== Damage =====
        // classMainStat * max(1, bonusWeaponDamage) * (1 + level*0.03) * classDmgMult * auraMultiplier
        int classMainStat = GetClassMainStatValue(effSTR, effDEX, effINT, effVIT);
        float rawDamage = classMainStat * Mathf.Max(1f, bonusWeaponDamage)
                        * (1f + level * 0.03f) * classDmgMult * auraMultiplier;
        damage = System.Math.Max(1L, (long)rawDamage);

        // ===== Armor =====
        // Directly from equipped items
        armor = bonusArmor;

        // ===== Crit Rate =====
        // 5% base + bonusCritRate, cap 75%
        critRate = Mathf.Clamp(5f + bonusCritRate, 0f, 75f);

        // ===== Crit Damage =====
        // 150% base + bonusCritDamage
        critDamage = 150f + bonusCritDamage;

        // ===== Speed =====
        // 100 + effDEX*0.5 + bonusSpeed + classSpeedBonus
        speed = 100f + effDEX * 0.5f + bonusSpeed + classSpeedBonus;

        onStatsChanged?.Invoke();
    }

    /// <summary>
    /// Returns the class's primary damage stat value.
    /// </summary>
    int GetClassMainStatValue(int effSTR, int effDEX, int effINT, int effVIT)
    {
        switch (playerClass)
        {
            case PlayerClass.Assassine:     return effDEX;
            case PlayerClass.Tank:          return effSTR;   // was VIT, now STR
            case PlayerClass.Bogenschuetze: return effDEX;
            case PlayerClass.Krieger:       return effSTR;
            case PlayerClass.Magier:        return effINT;
            case PlayerClass.Nekromant:     return effINT;
            default:                        return effSTR;
        }
    }

    /// <summary>
    /// Returns the class's primary stat type for cross-stat damage reduction.
    /// </summary>
    public StatType GetClassMainStatType()
    {
        switch (playerClass)
        {
            case PlayerClass.Assassine:     return StatType.DEX;
            case PlayerClass.Tank:          return StatType.STR;
            case PlayerClass.Bogenschuetze: return StatType.DEX;
            case PlayerClass.Krieger:       return StatType.STR;
            case PlayerClass.Magier:        return StatType.INT;
            case PlayerClass.Nekromant:     return StatType.INT;
            default:                        return StatType.STR;
        }
    }

    /// <summary>
    /// Returns the effective value of the class's primary damage stat.
    /// Used for cross-stat damage reduction calculations.
    /// </summary>
    public int GetEffectiveMainStat()
    {
        return GetClassMainStatValue(STR + bonusSTR, DEX + bonusDEX, INT + bonusINT, VIT + bonusVIT);
    }

    void GetClassMultipliers(out float dmgMult, out float hpMult, out float speedBonus)
    {
        switch (playerClass)
        {
            case PlayerClass.Krieger:
                dmgMult = 1.05f; hpMult = 1.2f; speedBonus = 0f; break;
            case PlayerClass.Tank:
                dmgMult = 0.9f; hpMult = 1.4f; speedBonus = -10f; break;
            case PlayerClass.Assassine:
                dmgMult = 1.0f; hpMult = 1.0f; speedBonus = 15f; break;
            case PlayerClass.Bogenschuetze:
                dmgMult = 1.0f; hpMult = 1.0f; speedBonus = 10f; break;
            case PlayerClass.Magier:
                dmgMult = 1.0f; hpMult = 1.0f; speedBonus = 0f; break;
            case PlayerClass.Nekromant:
                dmgMult = 0.95f; hpMult = 1.1f; speedBonus = 0f; break;
            default:
                dmgMult = 1.0f; hpMult = 1.0f; speedBonus = 0f; break;
        }
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

        Debug.Log("LEVEL UP! Neues Level: " + level);
        Debug.Log("Du hast 3 Statuspunkte zu vergeben!");

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
