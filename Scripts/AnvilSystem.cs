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

    public event Action<ItemData> OnCrafted;
void Start()
    {
        EnsureRefs();
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

    List<RarityWeight> GetRarityWeightsForAnvilLevel(int lvl)
    {
        // Smooth scaling 1–100 based on the rarities defined in ItemRarity.cs.
        // Design goals:
        // - Level 1: basically only ERank
        // - Higher rarities gradually become possible, but remain rare (100 levels = slow progression)
        lvl = Mathf.Clamp(lvl, 1, maxAnvilLevel);

        float t = (maxAnvilLevel <= 1) ? 1f : (lvl - 1f) / (maxAnvilLevel - 1f); // 0..1

        // Helper: lerp with curve (power > 1 pushes growth to late game)
        int W(int start, int end, float power)
        {
            float k = Mathf.Pow(t, Mathf.Max(0.01f, power));
            return Mathf.Max(0, Mathf.RoundToInt(Mathf.Lerp(start, end, k)));
        }

        // Weights roughly tuned for "Aura Farming": lots of trash early, very slow ramp.
        // You can tweak numbers without touching any other system.
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

        // Level 1 should be ONLY ERank (hard rule)
        if (lvl == 1)
        {
            list.Clear();
            list.Add(new RarityWeight(ItemRarity.ERank, 1000));
            return list;
        }

        // Remove 0-weight entries (early levels)
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].weight <= 0) list.RemoveAt(i);
        }

        // Safety: never return empty list
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

        OnCrafted?.Invoke(item);

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
    float GetRarityMultiplier(ItemRarity rarity)
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