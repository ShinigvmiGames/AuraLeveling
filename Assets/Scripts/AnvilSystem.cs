using System;
using System.Collections.Generic;
using UnityEngine;

public class AnvilSystem : MonoBehaviour
{
    [Header("Refs")]
    public ItemDatabase itemDatabase;
    public PlayerStats player;

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

    // ========= Level-Up Cost (Gold) =========
    public int GetUpgradeCost()
    {
        if (anvilLevel >= maxAnvilLevel) return -1;
        // Exponential curve: 50g base, grows ~9.5% per level
        // Lv1->2: 55g, Lv5->6: 79g, Lv10->11: 125g,
        // Lv20->21: 313g, Lv50->51: 4950g, Lv99->100: ~480000g
        int cost = Mathf.RoundToInt(50f * Mathf.Pow(1.095f, anvilLevel));
        return cost;
    }

    // ========= Level-Up Duration (Seconds) =========
    /// <summary>
    /// Timer duration for upgrading from current level to next.
    /// Lvl 1->2: 60s (1min), Lvl 5->6: 2.5min, Lvl 10->11: 6min,
    /// Lvl 20->21: 18min, Lvl 30->31: 38min, Lvl 50->51: 2.5h,
    /// Lvl 70->71: 10h, Lvl 99->100: 72h
    /// Sweet-spot: fast enough early to feel rewarding, but later levels
    /// create anticipation. MC skip keeps whales happy.
    /// </summary>
    public float GetUpgradeDuration()
    {
        if (anvilLevel >= maxAnvilLevel) return 0f;
        // 60s base, grows ~5.8% per level (less aggressive than gold cost)
        // This creates: 1min -> 2.5min -> 6min -> 18min -> 38min -> 2.5h -> 10h -> 72h
        float seconds = 60f * Mathf.Pow(1.058f, anvilLevel - 1);
        return seconds;
    }

    /// <summary>
    /// How many ManaCrystals to skip the remaining upgrade time.
    /// 1 MC = 5 minutes. Minimum 1 MC even if less than 5 min remain.
    /// </summary>
    public int GetSkipCostMC()
    {
        if (!isUpgrading) return 0;
        float remaining = GetUpgradeRemainingSeconds();
        if (remaining <= 0f) return 0;
        int mc = Mathf.CeilToInt(remaining / 300f); // 300s = 5 min
        return Mathf.Max(1, mc);
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

    // ========= Can / Start / Skip / Complete Upgrade =========
    public bool CanStartUpgrade()
    {
        if (anvilLevel >= maxAnvilLevel) return false;
        if (isUpgrading) return false;
        if (player == null) return false;
        return player.gold >= GetUpgradeCost();
    }

    /// <summary>
    /// Starts the timed upgrade. Spends gold immediately, timer begins.
    /// </summary>
    public bool TryStartUpgrade()
    {
        if (!CanStartUpgrade()) return false;
        int cost = GetUpgradeCost();
        if (!player.SpendGold(cost)) return false;

        isUpgrading = true;
        upgradeDuration = GetUpgradeDuration();
        upgradeStartTime = Time.time;

        OnUpgradeStarted?.Invoke();
        return true;
    }

    /// <summary>
    /// Skip the remaining timer with ManaCrystals.
    /// </summary>
    public bool TrySkipUpgrade()
    {
        if (!isUpgrading) return false;
        if (player == null) return false;

        int mcCost = GetSkipCostMC();
        if (mcCost <= 0) return false;
        if (!player.SpendManaCrystals(mcCost)) return false;

        CompleteUpgrade();
        OnUpgradeSkipped?.Invoke();
        return true;
    }

    void CompleteUpgrade()
    {
        if (!isUpgrading) return;
        isUpgrading = false;
        anvilLevel++;
        OnAnvilLevelChanged?.Invoke(anvilLevel);
        OnUpgradeCompleted?.Invoke();
    }

    // ========= Crafting =========
    public ItemData CraftItemInstant()
    {
        EnsureRefs();
        if (!HasEnoughEssence())
            return null;

        SpendEssence();

        ItemData item = GenerateItemFromDatabase();

        if (item != null)
            OnCrafted?.Invoke(item);

        return item;
    }

    // ========= Internals =========
    void EnsureRefs()
    {
        if (player == null) player = FindObjectOfType<PlayerStats>();
    }

    bool HasEnoughEssence()
    {
        if (player == null) return false;
        return player.shadowEssence >= essenceCostPerCraft;
    }

    void SpendEssence()
    {
        player.SpendEssence(essenceCostPerCraft);
    }

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

        return ItemRarity.ERank;
    }

    List<RarityWeight> GetRarityWeightsForAnvilLevel(int lvl)
    {
        lvl = Mathf.Clamp(lvl, 1, maxAnvilLevel);
        float t = (maxAnvilLevel <= 1) ? 1f : (lvl - 1f) / (maxAnvilLevel - 1f);

        int W(int start, int end, float power)
        {
            float k = Mathf.Pow(t, Mathf.Max(0.01f, power));
            return Mathf.Max(0, Mathf.RoundToInt(Mathf.Lerp(start, end, k)));
        }

        var list = new List<RarityWeight>
        {
            new RarityWeight(ItemRarity.ERank,       W(1000, 160, 0.60f)),
            new RarityWeight(ItemRarity.Common,      W(  0, 220, 0.85f)),
            new RarityWeight(ItemRarity.DRank,       W(  0, 240, 1.00f)),
            new RarityWeight(ItemRarity.CRank,       W(  0, 210, 1.15f)),
            new RarityWeight(ItemRarity.Rare,        W(  0, 140, 1.35f)),
            new RarityWeight(ItemRarity.BRank,       W(  0,  80, 1.55f)),
            new RarityWeight(ItemRarity.Hero,        W(  0,  45, 1.80f)),
            new RarityWeight(ItemRarity.ARank,       W(  0,  22, 2.05f)),
            new RarityWeight(ItemRarity.SRank,       W(  0,  10, 2.30f)),
            new RarityWeight(ItemRarity.Monarch,     W(  0,   5, 2.70f)),
            new RarityWeight(ItemRarity.Godlike,     W(  0,   2, 3.10f)),
            new RarityWeight(ItemRarity.AURAFARMING, W(  0,   1, 3.60f)),
        };

        if (lvl == 1)
        {
            list.Clear();
            list.Add(new RarityWeight(ItemRarity.ERank, 1000));
            return list;
        }

        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].weight <= 0) list.RemoveAt(i);
        }

        if (list.Count == 0)
            list.Add(new RarityWeight(ItemRarity.ERank, 1000));

        return list;
    }

    public ItemData GenerateItemFromDatabase()
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

        List<ItemDefinition> pool = itemDatabase.GetFor(pc);

        if (pool == null || pool.Count == 0)
        {
            Debug.LogError($"Keine Items in der Database fuer Klasse {pc}. Pruefe ItemDatabase_Main!");
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

        float mult = GetRarityMultiplier(item.rarity);
        int budget = Mathf.RoundToInt(item.itemLevel * 2.2f * mult);

        float wSTR = Mathf.Max(0f, chosen.wSTR);
        float wDEX = Mathf.Max(0f, chosen.wDEX);
        float wINT = Mathf.Max(0f, chosen.wINT);
        float wVIT = Mathf.Max(0f, chosen.wVIT);
        float wSum = wSTR + wDEX + wINT + wVIT;
        if (wSum < 0.001f) wSum = 1f;

        item.bonusSTR = Mathf.RoundToInt(budget * (wSTR / wSum));
        item.bonusDEX = Mathf.RoundToInt(budget * (wDEX / wSum));
        item.bonusINT = Mathf.RoundToInt(budget * (wINT / wSum));
        item.bonusVIT = Mathf.RoundToInt(budget * (wVIT / wSum));

        item.bonusSTR += UnityEngine.Random.Range(-2, 3);
        item.bonusDEX += UnityEngine.Random.Range(-2, 3);
        item.bonusINT += UnityEngine.Random.Range(-2, 3);
        item.bonusVIT += UnityEngine.Random.Range(-2, 3);

        item.bonusSTR = Mathf.Max(0, item.bonusSTR);
        item.bonusDEX = Mathf.Max(0, item.bonusDEX);
        item.bonusINT = Mathf.Max(0, item.bonusINT);
        item.bonusVIT = Mathf.Max(0, item.bonusVIT);

        if (item.rarity >= ItemRarity.ARank)
            item.auraBonusPercent = UnityEngine.Random.Range(5f, 15f);
        else if (item.rarity >= ItemRarity.Hero && UnityEngine.Random.value < 0.15f)
            item.auraBonusPercent = UnityEngine.Random.Range(1f, 6f);
        else
            item.auraBonusPercent = 0f;

        int sum = item.bonusSTR + item.bonusDEX + item.bonusINT + item.bonusVIT;
        item.itemAura = Mathf.RoundToInt(sum * (1f + item.auraBonusPercent / 100f));
        item.sellPrice = Mathf.Clamp(Mathf.RoundToInt(item.itemAura * 0.6f), 1, 999999);

        return item;
    }

    bool IsClassAllowed(ItemDefinition def, PlayerClass pc)
    {
        if (def == null) return false;
        bool isWeaponSlot = (def.slot == EquipmentSlot.MainHand ||
                             def.slot == EquipmentSlot.OffHand);
        if (!isWeaponSlot) return true;
        if (def.allowedClasses == null || def.allowedClasses.Length == 0) return false;
        for (int i = 0; i < def.allowedClasses.Length; i++)
            if (def.allowedClasses[i] == pc) return true;
        return false;
    }

    public float GetRarityMultiplier(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.ERank: return 0.8f;
            case ItemRarity.Common: return 1.0f;
            case ItemRarity.DRank: return 1.2f;
            case ItemRarity.CRank: return 1.4f;
            case ItemRarity.Rare: return 1.7f;
            case ItemRarity.BRank: return 2.0f;
            case ItemRarity.Hero: return 2.4f;
            case ItemRarity.ARank: return 3.0f;
            case ItemRarity.SRank: return 3.8f;
            case ItemRarity.Monarch: return 5.0f;
            case ItemRarity.Godlike: return 7.0f;
            case ItemRarity.AURAFARMING: return 10.0f;
        }
        return 1f;
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
}
