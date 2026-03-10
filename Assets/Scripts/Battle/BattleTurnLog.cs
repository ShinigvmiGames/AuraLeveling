using System;
using System.Collections.Generic;

[Serializable]
public struct BattleTurnAction
{
    public bool isPlayerAttack;
    public long damage;
    public bool isCrit;
    public long attackerHPAfter;
    public long defenderHPAfter;
    public long lifestealAmount;
    public bool isKill;
}

[Serializable]
public class BattleResult
{
    public bool playerWon;
    public List<BattleTurnAction> actions = new List<BattleTurnAction>();
    public long playerMaxHP;
    public long enemyMaxHP;
    public int turnsPlayed;
}
