using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class AnvilSystem : MonoBehaviour
{
    [Header("Refs")]
    public ItemDatabase itemDatabase; // <- ItemDatabase_Main hier reinziehen
    public PlayerStats player;        // <- PlayerStats hier reinziehen (oder wird gefunden)
    [Header("Optional: Auto-add + Popup")]
    public InventorySystem inventory; // optional
    public ItemPopup itemPopup;       // optional

    [Header("Anvil Level")]
    [Range(1, 100)] public int anvilLevel = 1;
    public int maxAnvilLevel = 100;

    [Header("Craft Cost")]
    public int essenceCostPerCraft = 1;

    // ========= Events =========
    public event Action<int> OnAnvilLevelChanged;
    public event Action<ItemData> OnCrafted;
    public event Action OnUpgradeStarted;
    public event Action OnUpgradeCompleted;
    public event Action OnUpgradeSkipped;

    // ========= Upgrade Timer State =========
    [HideInInspector] public bool isUpgrading = false;
    float upgradeStartTime;
    float upgradeDuration;

    void Start()
    {
        EnsureRefs();
    }

    void Update()
    {
        if (isUpgrading && Time.time >= upgradeStartTime + upgradeDuration)
        {
            CompleteUpgrade();
        }
    }

    // ========= Upgrade API =========

    public int GetUpgradeCost()
    {
        if (anvilLevel >= maxAnvilLevel) return -1;
        // Gold cost scales with upgrade duration
        float duration = GetUpgradeDurationSeconds(anvilLevel);
        int gold = Mathf.RoundToInt(duration / 6f);
        return Mathf.Max(10, (gold / 5) * 5);
    }

    public bool CanStartUpgrade()
    {
        if (anvilLevel >= maxAnvilLevel) return false;
        if (isUpgrading) return false;
        if (player == null) return false;
        return player.gold >= GetUpgradeCost();
    }

    public bool TryStartUpgrade()
    {
        if (!CanStartUpgrade()) return false;

        int cost = GetUpgradeCost();
        player.SpendGold(cost);

        upgradeDuration = GetUpgradeDurationSeconds(anvilLevel);
        upgradeStartTime = Time.time;
        isUpgrading = true;

        OnUpgradeStarted?.Invoke();
        return true;
    }

    void CompleteUpgrade()
    {
        isUpgrading = false;
        anvilLevel = Mathf.Min(anvilLevel + 1, maxAnvilLevel);
        OnAnvilLevelChanged?.Invoke(anvilLevel);
        OnUpgradeCompleted?.Invoke();
    }

    public float GetUpgradeRemainingSeconds()
    {
        if (!isUpgrading) return 0f;
        return Mathf.Max(0f, (upgradeStartTime + upgradeDuration) - Time.time);
    }

    public float GetUpgradeProgress01()
    {
        if (!isUpgrading || upgradeDuration <= 0f) return 0f;
        float elapsed = Time.time - upgradeStartTime;
        return Mathf.Clamp01(elapsed / upgradeDuration);
    }

    /// <summary>
    /// Skip cost in ManaCrystals: 1 MC per 10 minutes remaining, minimum 1.
    /// </summary>
    public int GetSkipCostMC()
    {
        float remaining = GetUpgradeRemainingSeconds();
        return Mathf.Max(1, Mathf.CeilToInt(remaining / 600f));
    }

    public bool TrySkipUpgrade()
    {
        if (!isUpgrading) return false;
        if (player == null) return false;

        int cost = GetSkipCostMC();
        if (!player.SpendManaCrystals(cost)) return false;

        isUpgrading = false;
        anvilLevel = Mathf.Min(anvilLevel + 1, maxAnvilLevel);
        OnAnvilLevelChanged?.Invoke(anvilLevel);
        OnUpgradeSkipped?.Invoke();
        return true;
    }

    // ========= Public Craft API =========
    public ItemData CraftItemInstant()
    {
        EnsureRefs();
        if (!HasEnoughEssence())
            return null;

        SpendEssence();

        ItemData item = GenerateItemFromDatabase();
        // If someone uses Instant craft, we still support auto add + popup:
        TryAddToInventoryAndPopup(item);

        OnCrafted?.Invoke(item);

        return item;
    }

    public void CraftItem(Action<ItemData> onCrafted)
    {
        EnsureRefs();

        if (!HasEnoughEssence())
        {
            Debug.Log("Nicht genug Essenz der Schatten!");
            onCrafted?.Invoke(null);
            return;
        }

        SpendEssence();
        StartCoroutine(CraftRoutine(onCrafted));
    }

    IEnumerator CraftRoutine(Action<ItemData> onCrafted)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1f));

        ItemData item = GenerateItemFromDatabase();

        // ✅ IMPORTANT: auto add + popup BEFORE callback (so UI sees correct inventory state)
        TryAddToInventoryAndPopup(item);

        OnCrafted?.Invoke(item);
        onCrafted?.Invoke(item);
    }

    // ========= Internals =========
    void EnsureRefs()
    {
        if (player == null) player = FindObjectOfType<PlayerStats>();

        // these two can live anywhere in the scene; we search if not assigned
        if (inventory == null) inventory = FindObjectOfType<InventorySystem>(true);
        if (itemPopup == null) itemPopup = FindObjectOfType<ItemPopup>(true);
    }

    bool HasEnoughEssence()
    {
        if (player == null) return false;
        if (player.shadowEssence < essenceCostPerCraft)
        {
            Debug.Log("Nicht genug Essenz der Schatten!");
            return false;
        }
        return true;
    }

    void SpendEssence()
    {
        player.SpendEssence(essenceCostPerCraft);
    }

    // ✅ NEW: central place for "put item into inventory and show popup"
    void TryAddToInventoryAndPopup(ItemData item)
    {
        if (item == null) return;

        // If inventory not present, just do nothing (old behaviour).
        if (inventory == null) return;

        // Try add
        bool added = inventory.Add(item);
        if (!added)
        {
            Debug.Log("Inventar voll → Item konnte nicht hinzugefügt werden.");
            return;
        }

        // Find last added index (common patterns)
        int idx = FindIndexOfItem(item);

        // Show popup if possible
        if (itemPopup != null && idx >= 0)
        {
            itemPopup.ShowInventoryIndex(idx);
        }
    }

    // Best-effort helper: find index of the exact reference (works if inventory stores the same object reference)
    int FindIndexOfItem(ItemData item)
    {
        if (inventory == null || item == null) return -1;

        // If your InventorySystem has a Count + Get(i), this will work.
        // If not, tell me your InventorySystem code and I’ll adapt in 10 sec.
        for (int i = 0; i < 999; i++)
        {
            var it = inventory.Get(i);
            if (it == null) continue;
            if (ReferenceEquals(it, item)) return i;
        }
        return -1;
    }

    // IMPORTANT: muss public sein, weil GateManager es nutzt
    public ItemRarity RollRarity()
    {
        var weights = GetRarityWeightsForAnvilLevel(anvilLevel);

        int total = 0;
        for (int i = 0; i < weights.Count; i++)
            total += weights[i].weight;

        int roll = UnityEngine.Random.Range(1, total + 1);

        int acc = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            acc += weights[i].weight;
            if (roll <= acc)
                return weights[i].rarity;
        }

        return ItemRarity.ERank; // fallback
    }

    // ========= Upgrade Duration System =========

    /// <summary>
    /// Returns the duration in seconds to upgrade from currentLevel to currentLevel+1.
    /// Level 1→2: 1 min, Level 2→3: 2.5 min, ..., Level 99→100: 2 weeks.
    /// </summary>
    public static float GetUpgradeDurationSeconds(int currentLevel)
    {
        currentLevel = Mathf.Clamp(currentLevel, 1, 99);

        // Exact values for first 5 upgrades (each roughly doubles)
        switch (currentLevel)
        {
            case 1: return 60f;        // 1 minute
            case 2: return 150f;       // 2.5 minutes
            case 3: return 300f;       // 5 minutes
            case 4: return 600f;       // 10 minutes
            case 5: return 1200f;      // 20 minutes
        }

        // Upgrades 6–99: exponential curve in log-space from 20 min to 2 weeks
        int upgradeIndex = currentLevel - 1; // 4 = upgrade 5 (20 min), 98 = upgrade 99 (2 weeks)
        float t = (upgradeIndex - 4f) / (98f - 4f); // 0..1
        float curvedT = Mathf.Pow(t, 0.42f);
        float logMin = Mathf.Log(1200f);     // ln(20 min in seconds)
        float logMax = Mathf.Log(1209600f);  // ln(2 weeks in seconds)
        return Mathf.Exp(Mathf.Lerp(logMin, logMax, curvedT));
    }

    public static string FormatDuration(float seconds)
    {
        if (seconds < 60f) return $"{seconds:F0}s";
        float minutes = seconds / 60f;
        if (minutes < 60f) return $"{minutes:F1} Min";
        float hours = minutes / 60f;
        if (hours < 24f) return $"{hours:F1} Std";
        float days = hours / 24f;
        return $"{days:F1} Tage";
    }

    // ========= Drop Probability System =========
    // Sliding-window system across 12 rarities (ERank..AURAFARMING).
    //
    // Design goals:
    //   Level  1 : 100% ERank
    //   Level  2 : 80% ERank, 20% Common
    //   Level  3 : 65% ERank, 30% Common, 5% DRank
    //   ...window slides upward...
    //   Level 100: 5% AURAFARMING, 30% Godlike, 35% Monarch, 30% SRank
    //
    // The "focus tier" (the tier with peak probability) smoothly moves from
    // ERank (tier 0) at level 1 to Monarch (tier 9) at level 100.
    // At any level, only ~3-4 adjacent tiers have meaningful weight.

    static readonly ItemRarity[] AllRarities = {
        ItemRarity.ERank,       // 0
        ItemRarity.Common,      // 1
        ItemRarity.DRank,       // 2
        ItemRarity.CRank,       // 3
        ItemRarity.Rare,        // 4
        ItemRarity.BRank,       // 5
        ItemRarity.Hero,        // 6
        ItemRarity.ARank,       // 7
        ItemRarity.SRank,       // 8
        ItemRarity.Monarch,     // 9
        ItemRarity.Godlike,     // 10
        ItemRarity.AURAFARMING  // 11
    };
    const int RarityCount = 12;

    List<RarityWeight> GetRarityWeightsForAnvilLevel(int lvl)
    {
        lvl = Mathf.Clamp(lvl, 1, maxAnvilLevel);
        var list = new List<RarityWeight>();

        // Level 1: only ERank
        if (lvl == 1)
        {
            list.Add(new RarityWeight(ItemRarity.ERank, 1000));
            return list;
        }

        // Level 2: 80% ERank, 20% Common (hardcoded for exact match)
        if (lvl == 2)
        {
            list.Add(new RarityWeight(ItemRarity.ERank, 800));
            list.Add(new RarityWeight(ItemRarity.Common, 200));
            return list;
        }

        // Level 3: 65% ERank, 30% Common, 5% DRank
        if (lvl == 3)
        {
            list.Add(new RarityWeight(ItemRarity.ERank, 650));
            list.Add(new RarityWeight(ItemRarity.Common, 300));
            list.Add(new RarityWeight(ItemRarity.DRank, 50));
            return list;
        }

        // Levels 4–100: sliding window system
        // t goes from 0 (level 4) to 1 (level 100)
        float t = (lvl - 4f) / (maxAnvilLevel - 4f);

        // The "focus tier" is the tier with the highest probability.
        // It moves from tier 0.5 (between ERank/Common) at t=0 to tier 9 (Monarch) at t=1.
        float focusTier = Mathf.Lerp(0.5f, 9.0f, Mathf.Pow(t, 0.75f));

        // Width of the bell curve (how many tiers get meaningful probability)
        // Narrow early (2 tiers), widens mid-game (3-4 tiers), narrows at end
        float spread = Mathf.Lerp(1.2f, 2.0f, Mathf.Sin(t * Mathf.PI));

        // Calculate gaussian-like weights for each tier
        float[] weights = new float[RarityCount];
        float totalWeight = 0f;

        for (int i = 0; i < RarityCount; i++)
        {
            float dist = (i - focusTier) / spread;
            float w = Mathf.Exp(-0.5f * dist * dist);

            // Suppress tiers that are way above the focus (prevent premature high drops)
            if (i > focusTier + 2.5f) w *= 0.1f;

            weights[i] = w;
            totalWeight += w;
        }

        // Special handling for AURAFARMING (tier 11): only starts appearing around level 60+
        // and reaches exactly 5% at level 100
        float auraFarmingChance = 0f;
        if (lvl >= 60)
        {
            float auraT = (lvl - 60f) / (maxAnvilLevel - 60f); // 0..1 from level 60 to 100
            auraFarmingChance = Mathf.Lerp(0.001f, 0.05f, Mathf.Pow(auraT, 1.5f));
        }

        // Special handling for Godlike (tier 10): appears around level 40+
        float godlikeBonus = 0f;
        if (lvl >= 40)
        {
            float godT = (lvl - 40f) / (maxAnvilLevel - 40f);
            godlikeBonus = Mathf.Lerp(0f, 0.15f, Mathf.Pow(godT, 1.2f));
        }

        // Normalize base weights to (1 - auraFarmingChance - godlikeBonus)
        float basePool = 1f - auraFarmingChance - godlikeBonus;
        if (basePool < 0.1f) basePool = 0.1f;

        for (int i = 0; i < RarityCount; i++)
        {
            float normalizedWeight = (weights[i] / totalWeight) * basePool;
            int intWeight = Mathf.RoundToInt(normalizedWeight * 10000f);

            // Override AURAFARMING and Godlike with their special values
            if (i == 11) // AURAFARMING
                intWeight = Mathf.RoundToInt(auraFarmingChance * 10000f);
            else if (i == 10) // Godlike
                intWeight = Mathf.RoundToInt((normalizedWeight + godlikeBonus) * 10000f);

            if (intWeight > 0)
                list.Add(new RarityWeight(AllRarities[i], intWeight));
        }

        // Safety: never return empty
        if (list.Count == 0)
            list.Add(new RarityWeight(ItemRarity.ERank, 1000));

        return list;
    }

    ItemData GenerateItemFromDatabase()
    {
        if (itemDatabase == null)
        {
            Debug.LogError("ItemDatabase fehlt! Zieh ItemDatabase_Main ins AnvilSystem.");
            return null;
        }
        if (player == null)
        {
            Debug.LogError("PlayerStats fehlt!");
            return null;
        }

        ItemRarity rarity = RollRarity();
        PlayerClass pc = player.playerClass;

        // ✅ Pool = alle Items die zur Klasse passen (Rarity spielt keine Rolle mehr)
        List<ItemDefinition> pool = itemDatabase.GetFor(pc);

        if (pool == null || pool.Count == 0)
        {
            Debug.LogError($"Keine Items in der Database für Klasse {pc}. Items hinzufügen!");
            return null;
        }

        ItemDefinition chosen = pool[UnityEngine.Random.Range(0, pool.Count)];

        ItemData item = new ItemData();
        item.definition = chosen;
        item.icon = chosen.icon;
        item.slot = chosen.slot;
        item.itemName = chosen.itemName;
        item.itemLevel = player.level;
        item.rarity = rarity;
        item.quality = RollQuality(rarity, chosen);

        ItemStatGenerator.GenerateStats(item, player.level, player.playerClass);

        return item;
    }

bool IsClassAllowed(ItemDefinition def, PlayerClass pc)
{
    if (def == null) return false;
    // Nur Waffen / Offhand bleiben class-locked
    bool isWeaponSlot = (def.slot == EquipmentSlot.MainHand ||
                         def.slot == EquipmentSlot.OffHand);

    // Für Rüstung/Accessories: jede Klasse darf es bekommen
    if (!isWeaponSlot)
        return true;

    // Ab hier: Waffen/Offhand brauchen allowedClasses
    if (def.allowedClasses == null || def.allowedClasses.Length == 0)
        return false;

    for (int i = 0; i < def.allowedClasses.Length; i++)
        if (def.allowedClasses[i] == pc)
            return true;

    return false;
}
    /// <summary>
    /// Roll item quality based on rarity, filtered by the item's allowedQualities.
    /// If the rolled quality is not allowed, falls back to the best allowed quality.
    /// </summary>
    public static ItemQuality RollQuality(ItemRarity rarity, ItemDefinition def = null)
    {
        float roll = UnityEngine.Random.Range(0f, 100f);

        // Legendary chance: only from SRank+ (1-3%)
        // Epic chance: from Rare+ (3-15%)
        ItemQuality rolled;
        switch (rarity)
        {
            case ItemRarity.ERank:
            case ItemRarity.Common:
            case ItemRarity.DRank:
            case ItemRarity.CRank:
                rolled = ItemQuality.Normal;
                break;

            case ItemRarity.Rare:
                rolled = (roll < 3f) ? ItemQuality.Epic : ItemQuality.Normal;
                break;

            case ItemRarity.BRank:
                rolled = (roll < 5f) ? ItemQuality.Epic : ItemQuality.Normal;
                break;

            case ItemRarity.Hero:
                rolled = (roll < 8f) ? ItemQuality.Epic : ItemQuality.Normal;
                break;

            case ItemRarity.ARank:
                rolled = (roll < 12f) ? ItemQuality.Epic : ItemQuality.Normal;
                break;

            case ItemRarity.SRank:
                if (roll < 1f) rolled = ItemQuality.Legendary;
                else if (roll < 12f) rolled = ItemQuality.Epic;
                else rolled = ItemQuality.Normal;
                break;

            case ItemRarity.Monarch:
                if (roll < 2f) rolled = ItemQuality.Legendary;
                else if (roll < 15f) rolled = ItemQuality.Epic;
                else rolled = ItemQuality.Normal;
                break;

            case ItemRarity.Godlike:
                if (roll < 3f) rolled = ItemQuality.Legendary;
                else if (roll < 18f) rolled = ItemQuality.Epic;
                else rolled = ItemQuality.Normal;
                break;

            case ItemRarity.AURAFARMING:
                if (roll < 5f) rolled = ItemQuality.Legendary;
                else if (roll < 25f) rolled = ItemQuality.Epic;
                else rolled = ItemQuality.Normal;
                break;

            default:
                rolled = ItemQuality.Normal;
                break;
        }

        // Filter by allowedQualities from ItemDefinition
        return FilterQuality(rolled, def);
    }

    /// <summary>
    /// If the ItemDefinition has allowedQualities set, ensure the rolled quality
    /// is one of them. Falls back to the best allowed quality that is <= rolled.
    /// </summary>
    static ItemQuality FilterQuality(ItemQuality rolled, ItemDefinition def)
    {
        if (def == null) return rolled;
        if (def.allowedQualities == null || def.allowedQualities.Length == 0) return rolled;

        // Check if rolled quality is allowed
        for (int i = 0; i < def.allowedQualities.Length; i++)
            if (def.allowedQualities[i] == rolled) return rolled;

        // Not allowed — find the best allowed quality that is <= rolled
        ItemQuality best = def.allowedQualities[0];
        for (int i = 1; i < def.allowedQualities.Length; i++)
        {
            if (def.allowedQualities[i] <= rolled && def.allowedQualities[i] > best)
                best = def.allowedQualities[i];
        }

        return best;
    }


    struct RarityWeight
    {
        public ItemRarity rarity;
        public int weight;
        public RarityWeight(ItemRarity r, int w)
        {
            rarity = r;
            weight = w;
        }
    }

    // ========= Debug / Testing =========

    [ContextMenu("Debug: Print Drop Tables")]
    void DebugPrintDropTables()
    {
        int[] levels = { 1, 2, 3, 5, 10, 15, 20, 30, 40, 50, 60, 70, 80, 90, 95, 100 };
        foreach (int lvl in levels)
        {
            var weights = GetRarityWeightsForAnvilLevel(lvl);
            int total = 0;
            foreach (var w in weights) total += w.weight;

            string line = $"Level {lvl,3}: ";
            foreach (var w in weights)
            {
                float pct = (w.weight / (float)total) * 100f;
                if (pct >= 0.1f)
                    line += $"{w.rarity}={pct:F1}% ";
            }
            Debug.Log(line);
        }
    }

    [ContextMenu("Debug: Print Upgrade Times")]
    void DebugPrintUpgradeTimes()
    {
        Debug.Log("=== ANVIL UPGRADE TIMES ===");
        float totalSeconds = 0f;
        for (int i = 1; i < maxAnvilLevel; i++)
        {
            float sec = GetUpgradeDurationSeconds(i);
            totalSeconds += sec;
            if (i <= 10 || i % 10 == 0 || i == maxAnvilLevel - 1)
                Debug.Log($"Level {i,2} -> {i + 1,3}: {FormatDuration(sec),10}   (kumuliert: {FormatDuration(totalSeconds)})");
        }
        Debug.Log($"GESAMT Level 1 -> {maxAnvilLevel}: {FormatDuration(totalSeconds)}");
    }
}