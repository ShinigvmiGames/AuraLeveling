using System;
[Serializable]
public class GateData
{
    public GateRank rank;

    public int durationSeconds;
    public int energyCost;

    public int rewardXP;
    public int rewardGold;
    public int rewardEssence;

    // Enemy Stats (legacy main stats kept for compatibility)
    public int enemySTR;
    public int enemyVIT;
    public int enemyDEX;
    public int enemyINT;
    public int enemyAura;

    // New Combat Stats
    public PlayerClass enemyClass;
    public long enemyHP;
    public long enemyDamage;
    public int enemyArmor;
    public float enemyCritRate;
    public float enemyCritDamage;
    public float enemySpeed;

    // Drop Chances
    public float randomItemChance;
    public float epicItemChance;
}