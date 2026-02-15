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

    // Enemy Stats
    public int enemySTR;
    public int enemyVIT;
    public int enemyDEX;
    public int enemyINT;
    public int enemyAura;

    // Drop Chances
    public float randomItemChance;
    public float epicItemChance;
}