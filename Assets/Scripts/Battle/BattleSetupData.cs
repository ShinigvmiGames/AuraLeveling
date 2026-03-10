using System;

[Serializable]
public class BattleRewards
{
    public int xp;
    public int gold;
    public int essence;
    public ItemData droppedItem; // null if no drop
}

[Serializable]
public class BattleSetupData
{
    public PlayerStats playerStats;
    public CharacterData playerCharData;

    // PvE
    public EnemyDefinition enemyDefinition;
    public GateData gateData;

    // PvP
    public bool isVsPlayer;
    public CharacterData opponentCharData;

    public BattleRewards rewards;
    public BattleContext context;
    public long seed;
}
