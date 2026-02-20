using UnityEngine;

/// <summary>
/// Shared stat generation logic used by both AnvilSystem and GateManager.
/// Normal quality: random stat distribution.
/// Epic quality: class-focused distribution.
/// Legendary quality: strongly class-focused + guaranteed aura bonus.
///
/// Combat substats rules:
///   - Weapon Damage: ONLY MainHand (range min-max) and OffHand (flat)
///   - Armor: ONLY Head, Chest, Legs, Boots
///   - Crit Rate, Crit Damage, Speed: ALL slots
/// </summary>
public static class ItemStatGenerator
{
    // Quality multipliers for stat budget
    static float GetQualityMultiplier(ItemQuality quality)
    {
        switch (quality)
        {
            case ItemQuality.Normal:    return 1.0f;
            case ItemQuality.Epic:      return 1.2f;
            case ItemQuality.Legendary: return 1.5f;
            default:                    return 1.0f;
        }
    }

    static float GetRarityMultiplier(ItemRarity rarity)
    {
        // Rarer items scale SIGNIFICANTLY stronger
        // ERank → AURAFARMING = 0.8x → 15x (massive jump at top tiers)
        switch (rarity)
        {
            case ItemRarity.ERank:       return 0.8f;
            case ItemRarity.Common:      return 1.0f;
            case ItemRarity.DRank:       return 1.3f;
            case ItemRarity.CRank:       return 1.7f;
            case ItemRarity.Rare:        return 2.2f;
            case ItemRarity.BRank:       return 3.0f;
            case ItemRarity.Hero:        return 4.0f;
            case ItemRarity.ARank:       return 5.5f;
            case ItemRarity.SRank:       return 7.5f;
            case ItemRarity.Monarch:     return 10.0f;
            case ItemRarity.Godlike:     return 13.0f;
            case ItemRarity.AURAFARMING: return 18.0f;
            default:                     return 1.0f;
        }
    }

    /// <summary>
    /// Generate stats for an item based on level, rarity, quality, and player class.
    /// </summary>
    public static void GenerateStats(ItemData item, int playerLevel, PlayerClass playerClass)
    {
        float rarityMult = GetRarityMultiplier(item.rarity);
        float qualityMult = GetQualityMultiplier(item.quality);
        int budget = Mathf.RoundToInt(playerLevel * 2.2f * rarityMult * qualityMult);

        // ===== Main Stat Distribution =====
        float[] splits;

        switch (item.quality)
        {
            case ItemQuality.Epic:
                splits = GetClassFocusedSplits(playerClass, primaryRange: new Vector2(0.50f, 0.60f), secondaryRange: new Vector2(0.20f, 0.30f));
                break;
            case ItemQuality.Legendary:
                splits = GetClassFocusedSplits(playerClass, primaryRange: new Vector2(0.60f, 0.70f), secondaryRange: new Vector2(0.15f, 0.25f));
                break;
            default: // Normal
                splits = RandomSplit(4);
                break;
        }

        // STR=0, DEX=1, INT=2, VIT=3
        item.bonusSTR = Mathf.Max(0, Mathf.RoundToInt(budget * splits[0]) + Random.Range(-2, 4));
        item.bonusDEX = Mathf.Max(0, Mathf.RoundToInt(budget * splits[1]) + Random.Range(-2, 4));
        item.bonusINT = Mathf.Max(0, Mathf.RoundToInt(budget * splits[2]) + Random.Range(-2, 4));
        item.bonusVIT = Mathf.Max(0, Mathf.RoundToInt(budget * splits[3]) + Random.Range(-2, 4));

        // ===== Combat Substats (slot-aware) =====
        GenerateCombatSubstats(item, budget);

        // ===== Aura bonus percent =====
        if (item.quality == ItemQuality.Legendary)
        {
            item.auraBonusPercent = Random.Range(10f, 20f);
        }
        else if (item.rarity >= ItemRarity.ARank)
        {
            item.auraBonusPercent = Random.Range(5f, 15f);
        }
        else if (item.rarity >= ItemRarity.Hero && Random.value < 0.15f)
        {
            item.auraBonusPercent = Random.Range(1f, 6f);
        }
        else
        {
            item.auraBonusPercent = 0f;
        }

        // ===== Item Aura calculation =====
        int statSum = item.bonusSTR + item.bonusDEX + item.bonusINT + item.bonusVIT;
        float avgWeaponDmg = (item.weaponDamageMin + item.weaponDamageMax) / 2f;
        float combatValue = avgWeaponDmg * 2f + item.armor * 0.5f
                          + item.critRate * 100f + item.critDamage * 50f + item.speed * 30f;
        item.itemAura = Mathf.RoundToInt((statSum * 100f + combatValue) * (1f + item.auraBonusPercent / 100f));

        // Sell price: based on itemAura, scaled down so gold doesn't explode
        item.sellPrice = Mathf.Clamp(Mathf.RoundToInt(item.itemAura * 0.006f + 5f), 1, 999999);
    }

    /// <summary>
    /// Generate combat substats based on the item's equipment slot.
    /// Weapon Damage: ONLY MainHand (range) and OffHand (flat).
    /// Armor: ONLY Head, Chest, Legs, Boots.
    /// Crit Rate, Crit Damage, Speed: ALL slots.
    /// </summary>
    static void GenerateCombatSubstats(ItemData item, int budget)
    {
        // Reset all combat substats
        item.weaponDamageMin = 0;
        item.weaponDamageMax = 0;
        item.armor = 0;
        item.critRate = 0f;
        item.critDamage = 0f;
        item.speed = 0f;

        float cb = budget; // combat budget

        switch (item.slot)
        {
            // ====== WEAPONS (damage only here) ======
            case EquipmentSlot.MainHand:
            {
                // MainHand: damage RANGE (min–max), RNG per attack
                int baseDmg = Mathf.Max(1, Mathf.RoundToInt(cb * 2.5f));
                int spread = Mathf.Max(1, Mathf.RoundToInt(baseDmg * Random.Range(0.15f, 0.30f)));
                item.weaponDamageMin = Mathf.Max(1, baseDmg - spread + Random.Range(-2, 3));
                item.weaponDamageMax = Mathf.Max(item.weaponDamageMin + 1, baseDmg + spread + Random.Range(-2, 3));
                // Crit/speed possible on weapons too
                if (Random.value < 0.30f) item.critRate = RollCritRate(cb, 0.4f);
                if (Random.value < 0.25f) item.critDamage = RollCritDamage(cb, 0.4f);
                if (Random.value < 0.15f) item.speed = RollSpeed(cb, 0.3f);
                break;
            }

            case EquipmentSlot.OffHand:
            {
                // OffHand: FIXED damage (no range), min == max
                int flatDmg = Mathf.Max(1, Mathf.RoundToInt(cb * 1.0f) + Random.Range(-2, 4));
                item.weaponDamageMin = flatDmg;
                item.weaponDamageMax = flatDmg; // same = fixed
                // More substats than MainHand
                if (Random.value < 0.35f) item.critRate = RollCritRate(cb, 0.5f);
                if (Random.value < 0.30f) item.critDamage = RollCritDamage(cb, 0.5f);
                if (Random.value < 0.20f) item.speed = RollSpeed(cb, 0.3f);
                break;
            }

            // ====== ARMOR PIECES (armor only here) ======
            case EquipmentSlot.Chest:
                item.armor = Mathf.Max(1, Mathf.RoundToInt(cb * 1.2f) + Random.Range(-2, 5));
                if (Random.value < 0.25f) item.critDamage = RollCritDamage(cb, 0.3f);
                if (Random.value < 0.15f) item.speed = RollSpeed(cb, 0.2f);
                break;

            case EquipmentSlot.Head:
                item.armor = Mathf.Max(1, Mathf.RoundToInt(cb * 0.9f) + Random.Range(-2, 4));
                if (Random.value < 0.25f) item.critRate = RollCritRate(cb, 0.4f);
                if (Random.value < 0.20f) item.critDamage = RollCritDamage(cb, 0.3f);
                break;

            case EquipmentSlot.Legs:
                item.armor = Mathf.Max(1, Mathf.RoundToInt(cb * 1.0f) + Random.Range(-2, 4));
                if (Random.value < 0.20f) item.critRate = RollCritRate(cb, 0.3f);
                if (Random.value < 0.20f) item.speed = RollSpeed(cb, 0.3f);
                break;

            case EquipmentSlot.Boots:
                item.armor = Mathf.Max(1, Mathf.RoundToInt(cb * 0.7f) + Random.Range(-2, 3));
                if (Random.value < 0.80f) item.speed = RollSpeed(cb, 0.7f);
                if (Random.value < 0.15f) item.critRate = RollCritRate(cb, 0.3f);
                break;

            // ====== ACCESSORIES (no weapon damage, no armor) ======
            case EquipmentSlot.Belt:
                // Belt: no armor, no weapon damage — purely crit/speed
                if (Random.value < 0.40f) item.critRate = RollCritRate(cb, 0.5f);
                if (Random.value < 0.40f) item.critDamage = RollCritDamage(cb, 0.4f);
                if (Random.value < 0.30f) item.speed = RollSpeed(cb, 0.3f);
                break;

            case EquipmentSlot.Ring:
                // Crit-focused
                if (Random.value < 0.60f) item.critRate = RollCritRate(cb, 0.8f);
                if (Random.value < 0.60f) item.critDamage = RollCritDamage(cb, 0.8f);
                if (Random.value < 0.25f) item.speed = RollSpeed(cb, 0.3f);
                break;

            case EquipmentSlot.Amulet:
                // Crit-focused + speed
                if (Random.value < 0.50f) item.critRate = RollCritRate(cb, 0.7f);
                if (Random.value < 0.60f) item.critDamage = RollCritDamage(cb, 0.8f);
                if (Random.value < 0.35f) item.speed = RollSpeed(cb, 0.4f);
                break;

            case EquipmentSlot.Artifact:
                // All non-weapon, non-armor substats
                if (Random.value < 0.45f) item.critRate = RollCritRate(cb, 0.6f);
                if (Random.value < 0.45f) item.critDamage = RollCritDamage(cb, 0.6f);
                if (Random.value < 0.40f) item.speed = RollSpeed(cb, 0.5f);
                break;
        }
    }

    /// <summary>Roll crit rate %. Budget already includes rarity scaling.</summary>
    static float RollCritRate(float budget, float intensity)
    {
        float raw = budget * 0.012f * intensity + Random.Range(0f, 0.5f);
        // Cap scales with budget: low items ~3%, high items ~8%
        float cap = Mathf.Clamp(3f + budget * 0.008f, 3f, 8f);
        return Mathf.Clamp(Mathf.Round(raw * 10f) / 10f, 0.1f, cap);
    }

    /// <summary>Roll crit damage %. Budget already includes rarity scaling.</summary>
    static float RollCritDamage(float budget, float intensity)
    {
        float raw = budget * 0.05f * intensity + Random.Range(0f, 2f);
        // Cap scales with budget: low items ~15%, high items ~35%
        float cap = Mathf.Clamp(15f + budget * 0.03f, 15f, 35f);
        return Mathf.Clamp(Mathf.Round(raw * 10f) / 10f, 0.5f, cap);
    }

    /// <summary>Roll flat speed. Budget already includes rarity scaling.</summary>
    static float RollSpeed(float budget, float intensity)
    {
        float raw = budget * 0.035f * intensity + Random.Range(0f, 1.5f);
        // Cap scales with budget: low items ~10, high items ~25
        float cap = Mathf.Clamp(10f + budget * 0.02f, 10f, 25f);
        return Mathf.Clamp(Mathf.Round(raw * 10f) / 10f, 0.5f, cap);
    }

    /// <summary>
    /// Generate a random split of N portions that sum to 1.0.
    /// Uses Dirichlet-like distribution for variety.
    /// </summary>
    static float[] RandomSplit(int n)
    {
        float[] raw = new float[n];
        float total = 0f;
        for (int i = 0; i < n; i++)
        {
            // Exponential random for more variety than uniform
            raw[i] = -Mathf.Log(Mathf.Max(0.001f, Random.value));
            total += raw[i];
        }
        for (int i = 0; i < n; i++)
            raw[i] /= total;
        return raw;
    }

    /// <summary>
    /// Returns splits [STR, DEX, INT, VIT] focused on the class's primary/secondary stats.
    /// Tank uses STR as primary (was VIT before redesign).
    /// </summary>
    static float[] GetClassFocusedSplits(PlayerClass pc, Vector2 primaryRange, Vector2 secondaryRange)
    {
        // indices: STR=0, DEX=1, INT=2, VIT=3
        int primaryIdx, secondaryIdx;

        switch (pc)
        {
            case PlayerClass.Assassine:
                primaryIdx = 1;  // DEX
                secondaryIdx = 3; // VIT
                break;
            case PlayerClass.Tank:
                primaryIdx = 0;  // STR (was VIT — changed in stat redesign)
                secondaryIdx = 3; // VIT (was STR)
                break;
            case PlayerClass.Bogenschuetze:
                primaryIdx = 1;  // DEX
                secondaryIdx = 0; // STR
                break;
            case PlayerClass.Krieger:
                primaryIdx = 0;  // STR
                secondaryIdx = 3; // VIT
                break;
            case PlayerClass.Magier:
                primaryIdx = 2;  // INT
                secondaryIdx = 3; // VIT
                break;
            case PlayerClass.Nekromant:
                primaryIdx = 2;  // INT
                secondaryIdx = 3; // VIT
                break;
            default:
                return RandomSplit(4);
        }

        float primaryPct = Random.Range(primaryRange.x, primaryRange.y);
        float secondaryPct = Random.Range(secondaryRange.x, secondaryRange.y);

        // Clamp so total doesn't exceed 1
        if (primaryPct + secondaryPct > 0.90f)
            secondaryPct = 0.90f - primaryPct;

        float remaining = 1f - primaryPct - secondaryPct;

        float[] splits = new float[4];
        splits[primaryIdx] = primaryPct;
        splits[secondaryIdx] = secondaryPct;

        // Distribute remaining across the other two stats
        int[] others = new int[2];
        int oi = 0;
        for (int i = 0; i < 4; i++)
        {
            if (i != primaryIdx && i != secondaryIdx)
                others[oi++] = i;
        }

        float split = Random.Range(0.3f, 0.7f);
        splits[others[0]] = remaining * split;
        splits[others[1]] = remaining * (1f - split);

        return splits;
    }
}
