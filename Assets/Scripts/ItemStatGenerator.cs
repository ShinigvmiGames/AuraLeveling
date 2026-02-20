using UnityEngine;

/// <summary>
/// Shared stat generation logic used by both AnvilSystem and GateManager.
/// Normal quality: random stat distribution.
/// Epic quality: class-focused distribution.
/// Legendary quality: strongly class-focused + guaranteed aura bonus.
/// Also generates combat substats (weaponDamage, armor, critRate, critDamage, speed)
/// based on equipment slot.
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
        switch (rarity)
        {
            case ItemRarity.ERank:       return 0.8f;
            case ItemRarity.Common:      return 1.0f;
            case ItemRarity.DRank:       return 1.2f;
            case ItemRarity.CRank:       return 1.4f;
            case ItemRarity.Rare:        return 1.7f;
            case ItemRarity.BRank:       return 2.0f;
            case ItemRarity.Hero:        return 2.4f;
            case ItemRarity.ARank:       return 3.0f;
            case ItemRarity.SRank:       return 3.8f;
            case ItemRarity.Monarch:     return 5.0f;
            case ItemRarity.Godlike:     return 7.0f;
            case ItemRarity.AURAFARMING: return 10.0f;
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
        // Now includes combat substats contribution
        int statSum = item.bonusSTR + item.bonusDEX + item.bonusINT + item.bonusVIT;
        float combatValue = item.weaponDamage * 2f + item.armor * 0.5f
                          + item.critRate * 100f + item.critDamage * 50f + item.speed * 30f;
        item.itemAura = Mathf.RoundToInt((statSum * 100f + combatValue) * (1f + item.auraBonusPercent / 100f));

        // Sell price: based on itemAura, scaled down so gold doesn't explode
        item.sellPrice = Mathf.Clamp(Mathf.RoundToInt(item.itemAura * 0.006f + 5f), 1, 999999);
    }

    /// <summary>
    /// Generate combat substats based on the item's equipment slot.
    /// Each slot has different substat priorities.
    /// </summary>
    static void GenerateCombatSubstats(ItemData item, int budget)
    {
        // Reset combat substats
        item.weaponDamage = 0;
        item.armor = 0;
        item.critRate = 0f;
        item.critDamage = 0f;
        item.speed = 0f;

        float combatBudget = budget; // same scale as main stat budget

        switch (item.slot)
        {
            case EquipmentSlot.MainHand:
                // Primary weapon damage source
                item.weaponDamage = Mathf.Max(1, Mathf.RoundToInt(combatBudget * 2.5f) + Random.Range(-3, 5));
                // Small chance for minor substats
                if (Random.value < 0.30f) item.critRate = RollCritRate(combatBudget, 0.4f);
                if (Random.value < 0.25f) item.critDamage = RollCritDamage(combatBudget, 0.4f);
                break;

            case EquipmentSlot.OffHand:
                // Secondary weapon damage + moderate substats
                item.weaponDamage = Mathf.Max(1, Mathf.RoundToInt(combatBudget * 1.0f) + Random.Range(-2, 4));
                item.armor = Mathf.Max(0, Mathf.RoundToInt(combatBudget * 0.4f) + Random.Range(-1, 3));
                if (Random.value < 0.35f) item.critRate = RollCritRate(combatBudget, 0.5f);
                break;

            case EquipmentSlot.Chest:
                // Highest armor piece
                item.armor = Mathf.Max(1, Mathf.RoundToInt(combatBudget * 1.2f) + Random.Range(-2, 5));
                if (Random.value < 0.20f) item.critDamage = RollCritDamage(combatBudget, 0.3f);
                break;

            case EquipmentSlot.Head:
                item.armor = Mathf.Max(1, Mathf.RoundToInt(combatBudget * 0.9f) + Random.Range(-2, 4));
                if (Random.value < 0.25f) item.critRate = RollCritRate(combatBudget, 0.4f);
                break;

            case EquipmentSlot.Legs:
                item.armor = Mathf.Max(1, Mathf.RoundToInt(combatBudget * 1.0f) + Random.Range(-2, 4));
                if (Random.value < 0.20f) item.speed = RollSpeed(combatBudget, 0.3f);
                break;

            case EquipmentSlot.Boots:
                // Armor + high speed chance
                item.armor = Mathf.Max(1, Mathf.RoundToInt(combatBudget * 0.7f) + Random.Range(-2, 3));
                if (Random.value < 0.80f) item.speed = RollSpeed(combatBudget, 0.7f);
                break;

            case EquipmentSlot.Belt:
                item.armor = Mathf.Max(0, Mathf.RoundToInt(combatBudget * 0.6f) + Random.Range(-2, 3));
                if (Random.value < 0.30f) item.critDamage = RollCritDamage(combatBudget, 0.3f);
                if (Random.value < 0.25f) item.speed = RollSpeed(combatBudget, 0.3f);
                break;

            case EquipmentSlot.Ring:
                // Crit-focused, little armor
                item.armor = Mathf.Max(0, Mathf.RoundToInt(combatBudget * 0.15f));
                if (Random.value < 0.60f) item.critRate = RollCritRate(combatBudget, 0.8f);
                if (Random.value < 0.60f) item.critDamage = RollCritDamage(combatBudget, 0.8f);
                break;

            case EquipmentSlot.Amulet:
                // Crit-focused, little armor
                item.armor = Mathf.Max(0, Mathf.RoundToInt(combatBudget * 0.15f));
                if (Random.value < 0.50f) item.critRate = RollCritRate(combatBudget, 0.7f);
                if (Random.value < 0.60f) item.critDamage = RollCritDamage(combatBudget, 0.8f);
                if (Random.value < 0.30f) item.speed = RollSpeed(combatBudget, 0.4f);
                break;

            case EquipmentSlot.Artifact:
                // All substats possible
                item.armor = Mathf.Max(0, Mathf.RoundToInt(combatBudget * 0.4f) + Random.Range(-2, 3));
                if (Random.value < 0.40f) item.weaponDamage = Mathf.Max(0, Mathf.RoundToInt(combatBudget * 0.5f));
                if (Random.value < 0.40f) item.critRate = RollCritRate(combatBudget, 0.6f);
                if (Random.value < 0.40f) item.critDamage = RollCritDamage(combatBudget, 0.6f);
                if (Random.value < 0.40f) item.speed = RollSpeed(combatBudget, 0.5f);
                break;
        }
    }

    /// <summary>Roll crit rate %, capped at 5% per item.</summary>
    static float RollCritRate(float budget, float intensity)
    {
        float raw = budget * 0.015f * intensity + Random.Range(0f, 0.5f);
        return Mathf.Clamp(Mathf.Round(raw * 10f) / 10f, 0.1f, 5.0f);
    }

    /// <summary>Roll crit damage %, capped at 20% per item.</summary>
    static float RollCritDamage(float budget, float intensity)
    {
        float raw = budget * 0.06f * intensity + Random.Range(0f, 2f);
        return Mathf.Clamp(Mathf.Round(raw * 10f) / 10f, 0.5f, 20.0f);
    }

    /// <summary>Roll flat speed, capped at 15 per item.</summary>
    static float RollSpeed(float budget, float intensity)
    {
        float raw = budget * 0.04f * intensity + Random.Range(0f, 1.5f);
        return Mathf.Clamp(Mathf.Round(raw * 10f) / 10f, 0.5f, 15.0f);
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
