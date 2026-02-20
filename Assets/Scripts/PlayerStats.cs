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
    public int maxHP;
    public int maxMana;
    public int attack;
    public int defense;

    [Header("Aura")]
    public float auraBonusPercent = 0f;

    [Header("Money")]
    public int gold = 0; // Gold / Mana-Kristalle

    [Header("Crafting Currency")]
    public int shadowEssence = 10;

    [Header("Premium Currency")]
    public int manaCrystals = 0;

[Header("Equipment Bonuses")]
public int bonusSTR;
public int bonusDEX;
public int bonusINT;
public int bonusVIT;
public float bonusAuraPercent;

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

    // Berechnet die Aura des Spielers (Stats + Items + Boni) mit x100 Skalierung
    public int GetAura()
    {
        int effSTR = STR + bonusSTR;
        int effDEX = DEX + bonusDEX;
        int effINT = INT + bonusINT;
        int effVIT = VIT + bonusVIT;

        int baseAura = effSTR + effDEX + effINT + effVIT;

        float totalAuraBonus = (auraBonusPercent + bonusAuraPercent) / 100f;
        int finalAura = Mathf.RoundToInt(baseAura * 100f * (1f + totalAuraBonus));

        return finalAura;
    }


    void Start()
    {
        RecalculateStats();
        Debug.Log("Player Stats initialisiert");
    }

    public void RecalculateStats()
{
    // Base derived stats from main stats
    int effSTR = STR + bonusSTR;
int effDEX = DEX + bonusDEX;
int effINT = INT + bonusINT;
int effVIT = VIT + bonusVIT;

maxHP = effVIT * 10;
maxMana = effINT * 10;
attack = effSTR * 2;
defense = effVIT * 2;

    ApplyClassBonus();

    // Aura-Bonus aus Level (wie du es schon hattest)
    auraBonusPercent = (level - 1) * 1.5f;

    // Equipment Boni auf Mainstats addieren
    int totalSTR = STR + bonusSTR;
    int totalDEX = DEX + bonusDEX;
    int totalINT = INT + bonusINT;
    int totalVIT = VIT + bonusVIT;

    // Derived Stats nochmal mit total stats neu berechnen
    maxHP = totalVIT * 10;
    maxMana = totalINT * 10;
    attack = totalSTR * 2;
    defense = totalVIT * 2;

    ApplyClassBonus(); // Klassenbonus nochmal auf neue derived stats

    // Aura Bonus: Level + Equipment-AuraBonus%
    float totalAuraBonusPercent = auraBonusPercent + bonusAuraPercent;

    ApplyAuraBonus(totalAuraBonusPercent);
    onStatsChanged?.Invoke();
}
    void ApplyClassBonus()
    {
    switch (playerClass)
        {
        case PlayerClass.Assassine:
            attack += Mathf.RoundToInt(DEX * 0.5f);
            break;

        case PlayerClass.Tank:
            maxHP += VIT * 20;
            defense += VIT;
            break;

        case PlayerClass.Bogenschuetze:
            attack += Mathf.RoundToInt(DEX * 0.7f);
            break;

        case PlayerClass.Krieger:
            attack += STR;
            defense += STR / 2;
            break;

        case PlayerClass.Magier:
            maxMana += INT * 20;
            break;

        case PlayerClass.Nekromant:
            maxMana += INT * 15;
            defense += INT / 3;
            break;
        }
    }



    void ApplyAuraBonus(float totalAuraBonusPercent)
{
    float multiplier = 1f + totalAuraBonusPercent / 100f;

    maxHP = Mathf.RoundToInt(maxHP * multiplier);
    maxMana = Mathf.RoundToInt(maxMana * multiplier);
    attack = Mathf.RoundToInt(attack * multiplier);
    defense = Mathf.RoundToInt(defense * multiplier);
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
