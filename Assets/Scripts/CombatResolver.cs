using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turn-based combat resolver for Gates.
///
/// Key mechanics:
///   - Speed determines turn order AND multi-turns (2x speed = 2 attacks per round)
///   - Armor reduces damage, cap 50% (Warrior passive: 60%)
///   - Crit Rate capped at 100%, determines chance of critical hit
///   - Crit Damage multiplier applied on critical hits
///   - MainHand weapon rolls min-max per attack, OffHand is flat
///   - Cross-Stat damage reduction when attacker/defender use different main stats
///   - Necromancer passive: 15% lifesteal on all damage dealt
///   - Max 200 turns safety limit
///   - Aura bonus is already baked into player stats (no separate multiplier here)
/// </summary>
public static class CombatResolver
{
    public struct CombatResult
    {
        public bool win;
        public int turnsPlayed;
        public long playerRemainingHP;
        public long enemyRemainingHP;
    }

    struct Fighter
    {
        public long hp;
        public long maxHP;
        public bool isPlayer;
        public int weaponDmgMin;
        public int weaponDmgMax;
        public int mainStatValue;
        public float levelDmgMult;   // (1 + level * 0.03) * classDmgMult — NO aura
        public long flatDamage;
        public int armor;
        public float armorCap;
        public float critRate;
        public float critDamage;
        public float speed;
        public StatType mainStatType;
        public int crossStatValue;
        public PlayerClass fighterClass;
        public float lifestealRate;
    }

    public static CombatResult Resolve(PlayerStats player, GateData gate, long seed = 0)
    {
        Random.State oldState = Random.state;
        if (seed != 0)
            Random.InitState((int)(seed % int.MaxValue));

        Fighter pFighter = BuildPlayerFighter(player);
        Fighter eFighter = BuildEnemyFighter(gate);

        var result = new CombatResult();
        int maxTurns = 200;
        bool combatOver = false;

        for (int turn = 0; turn < maxTurns && !combatOver; turn++)
        {
            result.turnsPlayed = turn + 1;

            float minSpeed = Mathf.Max(1f, Mathf.Min(pFighter.speed, eFighter.speed));
            int pAttacks = Mathf.Clamp(Mathf.FloorToInt(pFighter.speed / minSpeed), 1, 5);
            int eAttacks = Mathf.Clamp(Mathf.FloorToInt(eFighter.speed / minSpeed), 1, 5);

            bool playerFirst = pFighter.speed >= eFighter.speed;

            if (playerFirst)
            {
                for (int a = 0; a < pAttacks && !combatOver; a++)
                {
                    long dmg = CalculateDamage(pFighter, eFighter);
                    eFighter.hp -= dmg;
                    ApplyLifesteal(ref pFighter, dmg);
                    if (eFighter.hp <= 0) { result.win = true; combatOver = true; }
                }
                if (!combatOver)
                {
                    for (int a = 0; a < eAttacks && !combatOver; a++)
                    {
                        long dmg = CalculateDamage(eFighter, pFighter);
                        pFighter.hp -= dmg;
                        ApplyLifesteal(ref eFighter, dmg);
                        if (pFighter.hp <= 0) { result.win = false; combatOver = true; }
                    }
                }
            }
            else
            {
                for (int a = 0; a < eAttacks && !combatOver; a++)
                {
                    long dmg = CalculateDamage(eFighter, pFighter);
                    pFighter.hp -= dmg;
                    ApplyLifesteal(ref eFighter, dmg);
                    if (pFighter.hp <= 0) { result.win = false; combatOver = true; }
                }
                if (!combatOver)
                {
                    for (int a = 0; a < pAttacks && !combatOver; a++)
                    {
                        long dmg = CalculateDamage(pFighter, eFighter);
                        eFighter.hp -= dmg;
                        ApplyLifesteal(ref pFighter, dmg);
                        if (eFighter.hp <= 0) { result.win = true; combatOver = true; }
                    }
                }
            }

            if (!combatOver && turn == maxTurns - 1)
            {
                float pHPpct = pFighter.maxHP > 0 ? (float)pFighter.hp / pFighter.maxHP : 0f;
                float eHPpct = eFighter.maxHP > 0 ? (float)eFighter.hp / eFighter.maxHP : 0f;
                result.win = pHPpct >= eHPpct;
                combatOver = true;
            }
        }

        result.playerRemainingHP = System.Math.Max(0L, pFighter.hp);
        result.enemyRemainingHP = System.Math.Max(0L, eFighter.hp);

        Random.state = oldState;
        return result;
    }

    public static BattleResult ResolveWithLog(PlayerStats player, GateData gate, long seed = 0)
    {
        Random.State oldState = Random.state;
        if (seed != 0)
            Random.InitState((int)(seed % int.MaxValue));

        Fighter pFighter = BuildPlayerFighter(player);
        Fighter eFighter = BuildEnemyFighter(gate);

        var battleResult = new BattleResult
        {
            playerMaxHP = pFighter.maxHP,
            enemyMaxHP = eFighter.maxHP
        };

        int maxTurns = 200;
        bool combatOver = false;

        for (int turn = 0; turn < maxTurns && !combatOver; turn++)
        {
            battleResult.turnsPlayed = turn + 1;

            float minSpeed = Mathf.Max(1f, Mathf.Min(pFighter.speed, eFighter.speed));
            int pAttacks = Mathf.Clamp(Mathf.FloorToInt(pFighter.speed / minSpeed), 1, 5);
            int eAttacks = Mathf.Clamp(Mathf.FloorToInt(eFighter.speed / minSpeed), 1, 5);
            bool playerFirst = pFighter.speed >= eFighter.speed;

            if (playerFirst)
            {
                for (int a = 0; a < pAttacks && !combatOver; a++)
                    combatOver = LogAttack(ref pFighter, ref eFighter, true, battleResult);
                if (!combatOver)
                    for (int a = 0; a < eAttacks && !combatOver; a++)
                        combatOver = LogAttack(ref eFighter, ref pFighter, false, battleResult);
            }
            else
            {
                for (int a = 0; a < eAttacks && !combatOver; a++)
                    combatOver = LogAttack(ref eFighter, ref pFighter, false, battleResult);
                if (!combatOver)
                    for (int a = 0; a < pAttacks && !combatOver; a++)
                        combatOver = LogAttack(ref pFighter, ref eFighter, true, battleResult);
            }

            if (!combatOver && turn == maxTurns - 1)
            {
                float pHPpct = pFighter.maxHP > 0 ? (float)pFighter.hp / pFighter.maxHP : 0f;
                float eHPpct = eFighter.maxHP > 0 ? (float)eFighter.hp / eFighter.maxHP : 0f;
                battleResult.playerWon = pHPpct >= eHPpct;
                combatOver = true;
            }
        }

        Random.state = oldState;
        return battleResult;
    }

    static bool LogAttack(ref Fighter attacker, ref Fighter defender, bool isPlayerAttack, BattleResult log)
    {
        long dmg = CalculateDamage(attacker, defender, out bool isCrit);
        defender.hp -= dmg;

        long healAmount = 0;
        if (attacker.lifestealRate > 0f)
        {
            healAmount = (long)(dmg * attacker.lifestealRate);
            attacker.hp = System.Math.Min(attacker.maxHP, attacker.hp + healAmount);
        }

        bool isKill = defender.hp <= 0;

        log.actions.Add(new BattleTurnAction
        {
            isPlayerAttack = isPlayerAttack,
            damage = dmg,
            isCrit = isCrit,
            attackerHPAfter = System.Math.Max(0L, attacker.hp),
            defenderHPAfter = System.Math.Max(0L, defender.hp),
            lifestealAmount = healAmount,
            isKill = isKill
        });

        if (isKill)
        {
            log.playerWon = isPlayerAttack;
            return true;
        }
        return false;
    }

    static Fighter BuildPlayerFighter(PlayerStats player)
    {
        // Aura is already baked into all player stats and GetEffectiveMainStat().
        float classDmgMult = player.playerClass == PlayerClass.Mage ? 1.25f : 1f;

        return new Fighter
        {
            hp = player.maxHP,
            maxHP = player.maxHP,
            isPlayer = true,
            weaponDmgMin = Mathf.Max(1, player.bonusWeaponDmgMin),
            weaponDmgMax = Mathf.Max(1, player.bonusWeaponDmgMax),
            mainStatValue = player.GetEffectiveMainStat(),
            levelDmgMult = (1f + player.level * 0.03f) * classDmgMult, // NO aura
            flatDamage = 0,
            armor = player.armor,
            armorCap = player.GetArmorCap(),
            critRate = player.critRate,
            critDamage = player.critDamage,
            speed = player.speed,
            mainStatType = player.GetClassMainStatType(),
            crossStatValue = player.GetEffectiveMainStat(),
            fighterClass = player.playerClass,
            lifestealRate = player.playerClass == PlayerClass.Necromancer ? 0.15f : 0f
        };
    }

    static Fighter BuildEnemyFighter(GateData gate)
    {
        return new Fighter
        {
            hp = gate.enemyHP,
            maxHP = gate.enemyHP,
            isPlayer = false,
            weaponDmgMin = 0,
            weaponDmgMax = 0,
            mainStatValue = 0,
            levelDmgMult = 0f,
            flatDamage = gate.enemyDamage,
            armor = gate.enemyArmor,
            armorCap = 0.50f,
            critRate = gate.enemyCritRate,
            critDamage = gate.enemyCritDamage,
            speed = gate.enemySpeed,
            mainStatType = GetClassMainStatType(gate.enemyClass),
            crossStatValue = GetEnemyMainStatValue(gate),
            fighterClass = gate.enemyClass,
            lifestealRate = gate.enemyClass == PlayerClass.Necromancer ? 0.15f : 0f
        };
    }

    static void ApplyLifesteal(ref Fighter attacker, long damageDealt)
    {
        if (attacker.lifestealRate <= 0f) return;
        long heal = (long)(damageDealt * attacker.lifestealRate);
        attacker.hp = System.Math.Min(attacker.maxHP, attacker.hp + heal);
    }

    static long CalculateDamage(Fighter attacker, Fighter defender)
    {
        return CalculateDamage(attacker, defender, out _);
    }

    static long CalculateDamage(Fighter attacker, Fighter defender, out bool isCrit)
    {
        float baseDamage;

        if (attacker.isPlayer)
        {
            int weaponRoll = Random.Range(attacker.weaponDmgMin, attacker.weaponDmgMax + 1);
            baseDamage = attacker.mainStatValue * weaponRoll * attacker.levelDmgMult;
        }
        else
        {
            baseDamage = attacker.flatDamage;
        }

        float armorRed = defender.armor / (defender.armor + 300f);
        armorRed = Mathf.Min(armorRed, defender.armorCap);

        float crossStatRed = 0f;
        if (attacker.mainStatType != defender.mainStatType)
        {
            crossStatRed = defender.crossStatValue / (defender.crossStatValue + 500f) * 0.40f;
            crossStatRed = Mathf.Min(crossStatRed, 0.35f);
        }

        float totalReduction = Mathf.Min(0.70f, armorRed + crossStatRed);
        float dmgAfterArmor = baseDamage * (1f - totalReduction);

        isCrit = Random.value * 100f < attacker.critRate;
        if (isCrit)
            dmgAfterArmor *= (1f + attacker.critDamage / 100f);

        dmgAfterArmor *= Random.Range(0.95f, 1.05f);

        return System.Math.Max(1L, (long)dmgAfterArmor);
    }

    static StatType GetClassMainStatType(PlayerClass pc)
    {
        switch (pc)
        {
            case PlayerClass.Assassin:    return StatType.DEX;
            case PlayerClass.Warrior:     return StatType.STR;
            case PlayerClass.Archer:      return StatType.DEX;
            case PlayerClass.Mage:        return StatType.INT;
            case PlayerClass.Necromancer: return StatType.INT;
            default:                      return StatType.STR;
        }
    }

    static int GetEnemyMainStatValue(GateData gate)
    {
        switch (gate.enemyClass)
        {
            case PlayerClass.Assassin:    return gate.enemyDEX;
            case PlayerClass.Warrior:     return gate.enemySTR;
            case PlayerClass.Archer:      return gate.enemyDEX;
            case PlayerClass.Mage:        return gate.enemyINT;
            case PlayerClass.Necromancer: return gate.enemyINT;
            default:                      return gate.enemySTR;
        }
    }
}
