using UnityEngine;

/// <summary>
/// Turn-based combat resolver for Gates.
/// Uses Armor mitigation, Crit, Speed (turn order), and Cross-Stat damage reduction.
/// Max 100 turns safety limit.
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
        public long damage;
        public int armor;
        public float critRate;
        public float critDamage;
        public float speed;
        public StatType mainStatType;
        public int mainStatValue;
    }

    public static CombatResult Resolve(PlayerStats player, GateData gate, long seed = 0)
    {
        // Seeded randomness for deterministic outcome
        Random.State oldState = Random.state;
        if (seed != 0)
            Random.InitState((int)(seed % int.MaxValue));

        Fighter pFighter = new Fighter
        {
            hp = player.maxHP,
            damage = player.damage,
            armor = player.armor,
            critRate = player.critRate,
            critDamage = player.critDamage,
            speed = player.speed,
            mainStatType = player.GetClassMainStatType(),
            mainStatValue = player.GetEffectiveMainStat()
        };

        Fighter eFighter = new Fighter
        {
            hp = gate.enemyHP,
            damage = gate.enemyDamage,
            armor = gate.enemyArmor,
            critRate = gate.enemyCritRate,
            critDamage = gate.enemyCritDamage,
            speed = gate.enemySpeed,
            mainStatType = GetClassMainStatType(gate.enemyClass),
            mainStatValue = GetEnemyMainStatValue(gate)
        };

        var result = new CombatResult();
        int maxTurns = 100;

        // Turn-based: higher speed goes first
        bool playerFirst = pFighter.speed >= eFighter.speed;

        for (int turn = 0; turn < maxTurns; turn++)
        {
            result.turnsPlayed = turn + 1;

            if (playerFirst)
            {
                // Player attacks enemy
                long dmg = CalculateDamage(pFighter, eFighter);
                eFighter.hp -= dmg;
                if (eFighter.hp <= 0) { result.win = true; break; }

                // Enemy attacks player
                dmg = CalculateDamage(eFighter, pFighter);
                pFighter.hp -= dmg;
                if (pFighter.hp <= 0) { result.win = false; break; }
            }
            else
            {
                // Enemy attacks player
                long dmg = CalculateDamage(eFighter, pFighter);
                pFighter.hp -= dmg;
                if (pFighter.hp <= 0) { result.win = false; break; }

                // Player attacks enemy
                dmg = CalculateDamage(pFighter, eFighter);
                eFighter.hp -= dmg;
                if (eFighter.hp <= 0) { result.win = true; break; }
            }

            // Safety: if we hit max turns, whoever has more HP wins
            if (turn == maxTurns - 1)
            {
                result.win = pFighter.hp >= eFighter.hp;
            }
        }

        result.playerRemainingHP = Mathf.Max(0, (int)Mathf.Min(pFighter.hp, int.MaxValue));
        result.enemyRemainingHP = Mathf.Max(0, (int)Mathf.Min(eFighter.hp, int.MaxValue));

        Random.state = oldState;
        return result;
    }

    /// <summary>
    /// Calculate damage for one attack.
    /// armorReduction + crossStatReduction → totalReduction (cap 75%).
    /// Then crit check, then ±10% variance.
    /// </summary>
    static long CalculateDamage(Fighter attacker, Fighter defender)
    {
        float baseDamage = attacker.damage;

        // Armor reduction: armor / (armor + 200), cap 60%
        float armorRed = defender.armor / (defender.armor + 200f);
        armorRed = Mathf.Min(armorRed, 0.60f);

        // Cross-stat damage reduction: if attacker uses different main stat type
        float crossStatRed = 0f;
        if (attacker.mainStatType != defender.mainStatType)
        {
            crossStatRed = defender.mainStatValue / (defender.mainStatValue + 500f) * 0.40f;
            crossStatRed = Mathf.Min(crossStatRed, 0.35f);
        }

        // Total reduction cap 75%
        float totalReduction = Mathf.Min(0.75f, armorRed + crossStatRed);

        float dmgAfterArmor = baseDamage * (1f - totalReduction);

        // Crit check
        if (Random.value * 100f < attacker.critRate)
        {
            dmgAfterArmor *= attacker.critDamage / 100f;
        }

        // ±10% variance
        dmgAfterArmor *= Random.Range(0.90f, 1.10f);

        return Mathf.Max(1, (long)dmgAfterArmor);
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
    /// Uses legacy enemy main stats from GateData for cross-stat reduction.
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
