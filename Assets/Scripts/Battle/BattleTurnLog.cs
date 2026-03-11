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
    public bool isKill;

    // Skill flags
    public bool isDodge;         // Assassin Shadow: attack was dodged (damage = 0)
    public bool isExtraAttack;   // Warrior Berserk: this was a chain extra attack
    public bool isStun;          // Archer Stun: this attack stunned the defender
    public bool isDoubleDamage;  // Mage Arcane Surge: damage was doubled
    public bool isRevive;        // Necromancer Undying: fighter revived after this action
    public long reviveHP;        // HP after revive (for HP bar animation)
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
