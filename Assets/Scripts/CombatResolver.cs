using UnityEngine;

/// <summary>
/// Turn-based combat resolver for Gates.
///
/// Key mechanics:
///   - Speed determines turn order AND multi-turns (2x speed = 2 attacks per round)
///   - Armor reduces damage, cap 50% (Tank passive: 60%)
///   - Crit Rate capped at 100%, determines chance of critical hit
///   - Crit Damage multiplier applied on critical hits
///   - MainHand weapon rolls min-max per attack, OffHand is flat
///   - Cross-Stat damage reduction when attacker/defender use different main stats
///   - Nekromant passive: 15% lifesteal on all damage dealt
///   - Max 200 turns safety limit
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

    /// <summary>
    /// Fighter data used internally for the turn simulation.
    /// </summary>
    struct Fighter
    {
        public long hp;
        public long maxHP;
        public bool isPlayer;        // true = use weapon rolls, false = use flatDamage
        // Weapon damage range (for per-attack RNG) — player only
        public int weaponDmgMin;
        public int weaponDmgMax;
        // Main stat that scales weapon damage — player only
        public int mainStatValue;
        public float levelDmgMult;   // (1 + level * 0.03) * auraMultiplier * classDmgMult
        // Pre-calculated flat damage — enemy only
        public long flatDamage;
        public int armor;
        public float armorCap;       // 0.50 normally, 0.60 for Tank
        public float critRate;       // capped at 100
        public float critDamage;     // e.g. 150 = 1.5x
        public float speed;
        public StatType mainStatType;
        public int crossStatValue;   // main stat value for cross-stat defense
        public PlayerClass fighterClass;
        public float lifestealRate;  // 0.15 for Nekromant, 0 otherwise
    }

    public static CombatResult Resolve(PlayerStats player, GateData gate, long seed = 0)
    {
        // Seeded randomness for deterministic outcome
        Random.State oldState = Random.state;
        if (seed != 0)
            Random.InitState((int)(seed % int.MaxValue));

        // Build player fighter
        float playerAuraMult = 1f + ((player.level - 1) * 1.5f + player.bonusAuraPercent) / 100f;
        float playerClassDmgMult = 1f;
        switch (player.playerClass)
        {
            case PlayerClass.Magier:  playerClassDmgMult = 1.25f; break;
            case PlayerClass.Krieger: playerClassDmgMult = 1.15f; break;
        }

        Fighter pFighter = new Fighter
        {
            hp = player.maxHP,
            maxHP = player.maxHP,
            isPlayer = true,
            weaponDmgMin = Mathf.Max(1, player.bonusWeaponDmgMin),
            weaponDmgMax = Mathf.Max(1, player.bonusWeaponDmgMax),
            mainStatValue = player.GetEffectiveMainStat(),
            levelDmgMult = (1f + player.level * 0.03f) * playerAuraMult * playerClassDmgMult,
            flatDamage = 0,
            armor = player.armor,
            armorCap = player.GetArmorCap(),
            critRate = player.critRate,
            critDamage = player.critDamage,
            speed = player.speed,
            mainStatType = player.GetClassMainStatType(),
            crossStatValue = player.GetEffectiveMainStat(),
            fighterClass = player.playerClass,
            lifestealRate = player.playerClass == PlayerClass.Nekromant ? 0.15f : 0f
        };

        // Build enemy fighter — uses pre-calculated flat damage
        Fighter eFighter = new Fighter
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
            armorCap = 0.50f, // enemies always 50% cap
            critRate = gate.enemyCritRate,
            critDamage = gate.enemyCritDamage,
            speed = gate.enemySpeed,
            mainStatType = GetClassMainStatType(gate.enemyClass),
            crossStatValue = GetEnemyMainStatValue(gate),
            fighterClass = gate.enemyClass,
            lifestealRate = gate.enemyClass == PlayerClass.Nekromant ? 0.15f : 0f
        };

        var result = new CombatResult();
        int maxTurns = 200;
        bool combatOver = false;

        // ===== Speed-based turn system =====
        // Each round: each fighter gets (their speed / min speed) attacks
        // e.g. 200 speed vs 99 speed → fast fighter gets 2 attacks, slow gets 1
        for (int turn = 0; turn < maxTurns && !combatOver; turn++)
        {
            result.turnsPlayed = turn + 1;

            // Calculate attacks per round based on speed ratio
            float minSpeed = Mathf.Max(1f, Mathf.Min(pFighter.speed, eFighter.speed));
            int pAttacks = Mathf.Max(1, Mathf.FloorToInt(pFighter.speed / minSpeed));
            int eAttacks = Mathf.Max(1, Mathf.FloorToInt(eFighter.speed / minSpeed));

            // Cap to prevent infinite loops from extreme speed differences
            pAttacks = Mathf.Min(pAttacks, 5);
            eAttacks = Mathf.Min(eAttacks, 5);

            // Faster fighter attacks first
            bool playerFirst = pFighter.speed >= eFighter.speed;

            if (playerFirst)
            {
                // Player attacks
                for (int a = 0; a < pAttacks && !combatOver; a++)
                {
                    long dmg = CalculateDamage(pFighter, eFighter);
                    eFighter.hp -= dmg;
                    ApplyLifesteal(ref pFighter, dmg);
                    if (eFighter.hp <= 0) { result.win = true; combatOver = true; }
                }

                // Enemy attacks
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
                // Enemy attacks first
                for (int a = 0; a < eAttacks && !combatOver; a++)
                {
                    long dmg = CalculateDamage(eFighter, pFighter);
                    pFighter.hp -= dmg;
                    ApplyLifesteal(ref eFighter, dmg);
                    if (pFighter.hp <= 0) { result.win = false; combatOver = true; }
                }

                // Player attacks
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

            // Safety: if we hit max turns, whoever has more HP% wins
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

    /// <summary>
    /// Apply lifesteal: heal attacker for lifestealRate * damage dealt.
    /// Cannot exceed maxHP.
    /// </summary>
    static void ApplyLifesteal(ref Fighter attacker, long damageDealt)
    {
        if (attacker.lifestealRate <= 0f) return;
        long heal = (long)(damageDealt * attacker.lifestealRate);
        attacker.hp = System.Math.Min(attacker.maxHP, attacker.hp + heal);
    }

    /// <summary>
    /// Calculate damage for one attack.
    /// Player: mainStat * roll(weaponMin..weaponMax) * levelDmgMult
    /// Enemy: flatDamage (pre-calculated by GateManager)
    /// Then: armor reduction (cap 50%, Tank 60%) + cross-stat reduction (cap 35%)
    /// Then: crit check, ±5% variance. Min 1 damage.
    /// </summary>
    static long CalculateDamage(Fighter attacker, Fighter defender)
    {
        float baseDamage;

        if (attacker.isPlayer)
        {
            // Player: mainStat * roll(weaponMin, weaponMax) * levelDmgMult
            int weaponRoll = Random.Range(attacker.weaponDmgMin, attacker.weaponDmgMax + 1);
            baseDamage = attacker.mainStatValue * weaponRoll * attacker.levelDmgMult;
        }
        else
        {
            // Enemy: pre-calculated flat damage
            baseDamage = attacker.flatDamage;
        }

        // Armor reduction: armor / (armor + 300), cap at defender's armor cap
        float armorRed = defender.armor / (defender.armor + 300f);
        armorRed = Mathf.Min(armorRed, defender.armorCap);

        // Cross-stat damage reduction: if attacker uses different main stat type
        float crossStatRed = 0f;
        if (attacker.mainStatType != defender.mainStatType)
        {
            crossStatRed = defender.crossStatValue / (defender.crossStatValue + 500f) * 0.40f;
            crossStatRed = Mathf.Min(crossStatRed, 0.35f);
        }

        // Total reduction cap 70%
        float totalReduction = Mathf.Min(0.70f, armorRed + crossStatRed);

        float dmgAfterArmor = baseDamage * (1f - totalReduction);

        // Crit check (critRate already capped at 100 by PlayerStats)
        if (Random.value * 100f < attacker.critRate)
        {
            dmgAfterArmor *= attacker.critDamage / 100f;
        }

        // ±5% variance
        dmgAfterArmor *= Random.Range(0.95f, 1.05f);

        return System.Math.Max(1L, (long)dmgAfterArmor);
    }

    /// <summary>
    /// Get the StatType for an enemy's class.
    /// </summary>
    static StatType GetClassMainStatType(PlayerClass pc)
    {
        switch (pc)
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
    /// Get the enemy's main stat value based on their class.
    /// </summary>
    static int GetEnemyMainStatValue(GateData gate)
    {
        switch (gate.enemyClass)
        {
            case PlayerClass.Assassine:     return gate.enemyDEX;
            case PlayerClass.Tank:          return gate.enemySTR;
            case PlayerClass.Bogenschuetze: return gate.enemyDEX;
            case PlayerClass.Krieger:       return gate.enemySTR;
            case PlayerClass.Magier:        return gate.enemyINT;
            case PlayerClass.Nekromant:     return gate.enemyINT;
            default:                        return gate.enemySTR;
        }
    }
}
