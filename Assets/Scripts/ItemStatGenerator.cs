using UnityEngine;

/// <summary>
/// Shared stat generation logic used by both AnvilSystem and GateManager.
/// Normal quality: random stat distribution.
/// Epic quality: class-focused distribution.
/// Legendary quality: strongly class-focused + guaranteed aura bonus.
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

        // Aura bonus percent
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

        // Aura calculation (x100 scaling)
        int statSum = item.bonusSTR + item.bonusDEX + item.bonusINT + item.bonusVIT;
        item.itemAura = Mathf.RoundToInt(statSum * 100f * (1f + item.auraBonusPercent / 100f));

        // Sell price: based on itemAura, scaled down so gold doesn't explode
        item.sellPrice = Mathf.Clamp(Mathf.RoundToInt(item.itemAura * 0.006f + 5f), 1, 999999);
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
                primaryIdx = 3;  // VIT
                secondaryIdx = 0; // STR
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
