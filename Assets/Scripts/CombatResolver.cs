using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turn-based combat resolver for Gates.
///
/// Key mechanics:
///   - Turn order: whoever has higher Aura (Kampfkraft) goes first
///   - Each fighter attacks once per round (Warrior Berserk can chain extra attacks)
///   - Damage uses sqrt compression: sqrt(mainStat * weaponRoll) * 3.0 * levelDmgMult
///   - Armor reduces damage: armor / (armor + 500), cap 50% for ALL classes
///   - Cross-Stat damage reduction when attacker/defender use different main stats
///   - Max 200 turns safety limit
///
/// Class Skills (proc per attack):
///   Assassin  — Shadow:       20% dodge chance (attacker misses, damage = 0)
///   Warrior   — Berserk:      20% extra attack chance (chainable)
///   Archer    — Stun:         15% stun for 1 round (defender skips next attack)
///   Mage      — Arcane Surge: 25% double damage on attack
///   Necromancer — Undying:    Revive once at 30% max HP when killed
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
        public int mainStatValue;      // Raw main stat (NO aura) — used in sqrt damage formula
        public float levelDmgMult;     // (1 + level * 0.025) * auraMultiplier
        public long flatDamage;        // Enemy pre-computed damage (already includes sqrt + aura)
        public int armor;
        public float armorCap;
        public float critRate;
        public float critDamage;
        public StatType mainStatType;
        public int crossStatValue;     // Effective main stat WITH aura (for cross-stat defense)
        public long aura;              // Total aura (for turn order)
        public PlayerClass fighterClass;
    }

    // ==================== Quick Resolve (no log) ====================
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

        // Turn order: whoever has higher Aura attacks first
        bool playerFirst = pFighter.aura >= eFighter.aura;

        // Skill state
        bool playerStunned = false;
        bool enemyStunned = false;
        bool playerReviveUsed = false;
        bool enemyReviveUsed = false;

        for (int turn = 0; turn < maxTurns && !combatOver; turn++)
        {
            result.turnsPlayed = turn + 1;

            Fighter first = playerFirst ? pFighter : eFighter;
            Fighter second = playerFirst ? eFighter : pFighter;
            bool firstStunned = playerFirst ? playerStunned : enemyStunned;
            bool secondStunned = playerFirst ? enemyStunned : playerStunned;

            // Reset stun flags at start of round (stun lasts 1 round)
            if (playerFirst) { playerStunned = false; enemyStunned = false; }
            else { enemyStunned = false; playerStunned = false; }

            // === First fighter attacks ===
            if (!firstStunned)
            {
                combatOver = DoAttackQuick(ref first, ref second, playerFirst,
                    ref playerStunned, ref enemyStunned,
                    ref playerReviveUsed, ref enemyReviveUsed,
                    ref result);

                // Warrior Berserk chain
                while (!combatOver && first.fighterClass == PlayerClass.Warrior && Random.value < 0.20f)
                {
                    combatOver = DoAttackQuick(ref first, ref second, playerFirst,
                        ref playerStunned, ref enemyStunned,
                        ref playerReviveUsed, ref enemyReviveUsed,
                        ref result);
                }
            }

            // === Second fighter attacks ===
            if (!combatOver && !secondStunned)
            {
                combatOver = DoAttackQuick(ref second, ref first, !playerFirst,
                    ref playerStunned, ref enemyStunned,
                    ref playerReviveUsed, ref enemyReviveUsed,
                    ref result);

                // Warrior Berserk chain
                while (!combatOver && second.fighterClass == PlayerClass.Warrior && Random.value < 0.20f)
                {
                    combatOver = DoAttackQuick(ref second, ref first, !playerFirst,
                        ref playerStunned, ref enemyStunned,
                        ref playerReviveUsed, ref enemyReviveUsed,
                        ref result);
                }
            }

            // Write back HP
            if (playerFirst) { pFighter = first; eFighter = second; }
            else { eFighter = first; pFighter = second; }

            // Timeout — highest HP% wins
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

    static bool DoAttackQuick(ref Fighter attacker, ref Fighter defender, bool isPlayerAttack,
        ref bool playerStunned, ref bool enemyStunned,
        ref bool playerReviveUsed, ref bool enemyReviveUsed,
        ref CombatResult result)
    {
        // Assassin Shadow: 20% dodge
        if (defender.fighterClass == PlayerClass.Assassin && Random.value < 0.20f)
            return false; // dodge — no damage

        long dmg = CalculateDamage(attacker, defender);

        // Mage Arcane Surge: 25% double damage
        if (attacker.fighterClass == PlayerClass.Mage && Random.value < 0.25f)
            dmg *= 2;

        defender.hp -= dmg;

        // Archer Stun: 15% chance
        if (attacker.fighterClass == PlayerClass.Archer && Random.value < 0.15f)
        {
            if (isPlayerAttack) enemyStunned = true;
            else playerStunned = true;
        }

        // Check kill
        if (defender.hp <= 0)
        {
            // Necromancer Undying: revive once at 30% max HP
            bool reviveAvailable = defender.fighterClass == PlayerClass.Necromancer &&
                (defender.isPlayer ? !playerReviveUsed : !enemyReviveUsed);

            if (reviveAvailable)
            {
                defender.hp = (long)(defender.maxHP * 0.30f);
                if (defender.isPlayer) playerReviveUsed = true;
                else enemyReviveUsed = true;
                return false; // not dead — revived
            }

            result.win = isPlayerAttack;
            return true; // combat over
        }

        return false;
    }

    // ==================== Logged Resolve (for BattleUI) ====================
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

        // Turn order: whoever has higher Aura attacks first
        bool playerFirst = pFighter.aura >= eFighter.aura;

        // Skill state
        bool playerStunned = false;
        bool enemyStunned = false;
        bool playerReviveUsed = false;
        bool enemyReviveUsed = false;

        for (int turn = 0; turn < maxTurns && !combatOver; turn++)
        {
            battleResult.turnsPlayed = turn + 1;

            bool firstStunned = playerFirst ? playerStunned : enemyStunned;
            bool secondStunned = playerFirst ? enemyStunned : playerStunned;

            // Reset stun for this round
            playerStunned = false;
            enemyStunned = false;

            // === First fighter attacks ===
            if (!firstStunned)
            {
                combatOver = LogAttack(ref pFighter, ref eFighter, playerFirst, false,
                    ref playerStunned, ref enemyStunned,
                    ref playerReviveUsed, ref enemyReviveUsed,
                    battleResult);

                // Warrior Berserk chain
                while (!combatOver)
                {
                    Fighter attacker = playerFirst ? pFighter : eFighter;
                    if (attacker.fighterClass != PlayerClass.Warrior || Random.value >= 0.20f)
                        break;

                    combatOver = LogAttack(ref pFighter, ref eFighter, playerFirst, true,
                        ref playerStunned, ref enemyStunned,
                        ref playerReviveUsed, ref enemyReviveUsed,
                        battleResult);
                }
            }

            // === Second fighter attacks ===
            if (!combatOver && !secondStunned)
            {
                combatOver = LogAttack(ref pFighter, ref eFighter, !playerFirst, false,
                    ref playerStunned, ref enemyStunned,
                    ref playerReviveUsed, ref enemyReviveUsed,
                    battleResult);

                // Warrior Berserk chain
                while (!combatOver)
                {
                    Fighter attacker = !playerFirst ? pFighter : eFighter;
                    if (attacker.fighterClass != PlayerClass.Warrior || Random.value >= 0.20f)
                        break;

                    combatOver = LogAttack(ref pFighter, ref eFighter, !playerFirst, true,
                        ref playerStunned, ref enemyStunned,
                        ref playerReviveUsed, ref enemyReviveUsed,
                        battleResult);
                }
            }

            // Timeout
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

    static bool LogAttack(ref Fighter pFighter, ref Fighter eFighter, bool isPlayerAttack, bool isExtraAttack,
        ref bool playerStunned, ref bool enemyStunned,
        ref bool playerReviveUsed, ref bool enemyReviveUsed,
        BattleResult log)
    {
        Fighter attacker = isPlayerAttack ? pFighter : eFighter;
        Fighter defender = isPlayerAttack ? eFighter : pFighter;

        var action = new BattleTurnAction
        {
            isPlayerAttack = isPlayerAttack,
            isExtraAttack = isExtraAttack
        };

        // Assassin Shadow: 20% dodge
        if (defender.fighterClass == PlayerClass.Assassin && Random.value < 0.20f)
        {
            action.isDodge = true;
            action.damage = 0;
            action.attackerHPAfter = System.Math.Max(0L, attacker.hp);
            action.defenderHPAfter = System.Math.Max(0L, defender.hp);
            log.actions.Add(action);

            // Write back
            if (isPlayerAttack) { pFighter = attacker; eFighter = defender; }
            else { eFighter = attacker; pFighter = defender; }
            return false;
        }

        // Calculate damage
        long dmg = CalculateDamage(attacker, defender, out bool isCrit);

        // Mage Arcane Surge: 25% double damage
        bool doubleDamage = attacker.fighterClass == PlayerClass.Mage && Random.value < 0.25f;
        if (doubleDamage) dmg *= 2;

        defender.hp -= dmg;

        action.damage = dmg;
        action.isCrit = isCrit;
        action.isDoubleDamage = doubleDamage;

        // Archer Stun: 15% chance
        if (attacker.fighterClass == PlayerClass.Archer && Random.value < 0.15f)
        {
            action.isStun = true;
            if (isPlayerAttack) enemyStunned = true;
            else playerStunned = true;
        }

        // Check kill
        bool isKill = defender.hp <= 0;
        if (isKill)
        {
            // Necromancer Undying: revive once at 30% max HP
            bool reviveAvailable = defender.fighterClass == PlayerClass.Necromancer &&
                (defender.isPlayer ? !playerReviveUsed : !enemyReviveUsed);

            if (reviveAvailable)
            {
                long reviveHP = (long)(defender.maxHP * 0.30f);
                defender.hp = reviveHP;
                if (defender.isPlayer) playerReviveUsed = true;
                else enemyReviveUsed = true;

                action.isRevive = true;
                action.reviveHP = reviveHP;
                isKill = false;
            }
        }

        action.isKill = isKill;
        action.attackerHPAfter = System.Math.Max(0L, attacker.hp);
        action.defenderHPAfter = System.Math.Max(0L, defender.hp);

        log.actions.Add(action);

        // Write back
        if (isPlayerAttack) { pFighter = attacker; eFighter = defender; }
        else { eFighter = attacker; pFighter = defender; }

        if (isKill)
        {
            log.playerWon = isPlayerAttack;
            return true;
        }
        return false;
    }

    // ==================== Fighter Builders ====================
    static Fighter BuildPlayerFighter(PlayerStats player)
    {
        int rawMainStat = player.GetRawMainStat();
        float auraMultiplier = player.GetAuraMultiplier();

        return new Fighter
        {
            hp = player.maxHP,
            maxHP = player.maxHP,
            isPlayer = true,
            weaponDmgMin = Mathf.Max(1, player.bonusWeaponDmgMin),
            weaponDmgMax = Mathf.Max(1, player.bonusWeaponDmgMax),
            mainStatValue = rawMainStat,
            levelDmgMult = (1f + player.level * 0.025f) * auraMultiplier,
            flatDamage = 0,
            armor = player.armor,
            armorCap = player.GetArmorCap(),
            critRate = player.critRate,
            critDamage = player.critDamage,
            mainStatType = player.GetClassMainStatType(),
            crossStatValue = player.GetEffectiveMainStat(),
            aura = player.GetAura(),
            fighterClass = player.playerClass
        };
    }

    static Fighter BuildEnemyFighter(GateData gate)
    {
        // Compute enemy aura using same formula as PlayerStats.GetAura()
        long statBase = (long)(gate.enemySTR + gate.enemyDEX + gate.enemyINT + gate.enemyVIT) * 100L;
        long dmgContrib = gate.enemyDamage * 2L;
        long hpContrib = gate.enemyHP;
        long combatContrib = (long)(gate.enemyArmor * 50f + gate.enemyCritRate * 100f + gate.enemyCritDamage * 50f);
        long enemyAura = statBase + dmgContrib + hpContrib + combatContrib;

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
            mainStatType = GetClassMainStatType(gate.enemyClass),
            crossStatValue = GetEnemyMainStatValue(gate),
            aura = enemyAura,
            fighterClass = gate.enemyClass
        };
    }

    // ==================== Damage Calculation ====================
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
            baseDamage = Mathf.Sqrt(attacker.mainStatValue * weaponRoll) * 3.0f * attacker.levelDmgMult;
        }
        else
        {
            baseDamage = attacker.flatDamage;
        }

        float armorRed = defender.armor / (defender.armor + 500f);
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

    // ==================== Helpers ====================
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
