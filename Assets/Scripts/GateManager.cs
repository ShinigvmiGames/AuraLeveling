using System;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections.Generic;
/// <summary>
/// Creates 3 gate missions when none exist and no gate is currently active.
/// Gate rewards are ONLY granted after the timer ends AND the player opens the Gates UI (battle resolves there),
/// not instantly when the timer hits 0.
/// </summary>
public class GateManager : MonoBehaviour
{
    public event Action OnGatesGenerated;
    public event Action<GateData> OnGateAccepted;
    public event Action<GateData> OnGateBecameReady;
    public event Action<bool, GateData> OnGateResolved;
    public event Action OnGateStateChanged;

[Header("State")]
    public List<GateData> availableGates = new List<GateData>();
    public GateData activeGate;

    [Header("Running Screen (optional - GateUI panels can handle this too)")]
    public GameObject gateRunningPanel; // optional

    PlayerStats player;
    EnergySystem energy;
    AnvilSystem anvil;
    InventorySystem inventory;
    ItemInbox inbox;
float activeGateStartTime;
    float activeGateEndTime;

    long activeGateStartUnix;
    long activeGateEndUnix;
// When true: timer finished, waiting for player to enter Gate panel to resolve combat.
    bool gateReadyToResolve = false;
    bool readyEventFired = false;
void Awake()
    {
        EnsureRefs();
    }

    void Start()
    {
        // Safety: make sure gates exist when the scene loads (GateUI also calls EnsureGates on enable)
        EnsureGates();
    }

    

    void EnsureRefs()
    {
        if (player == null) player = FindObjectOfType<PlayerStats>();
        if (energy == null) energy = FindObjectOfType<EnergySystem>();
        if (anvil == null) anvil = FindObjectOfType<AnvilSystem>(true);
        if (inventory == null) inventory = FindObjectOfType<InventorySystem>(true);
        if (inbox == null) inbox = FindObjectOfType<ItemInbox>(true);
    }

    static long UnixNow()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
// ---------- Public Helpers for UI ----------
    public float GetRemainingGateSeconds()
    {
        if (activeGate == null) return 0f;
        return Mathf.Max(0f, activeGateEndTime - Time.time);
    }

    public float GetGateProgress01()
    {
        if (activeGate == null) return 0f;

        float total = Mathf.Max(1f, activeGate.durationSeconds);
        float elapsed = Mathf.Clamp(Time.time - activeGateStartTime, 0f, total);
        return elapsed / total;
    }

    public bool IsActiveGateReadyToResolve()
    {
        // Timer reached 0 AND we haven't resolved yet
        return activeGate != null && gateReadyToResolve;
    }

    void Update()
    {
        // Mark ready when timer finishes (do NOT auto-resolve here)
        if (activeGate != null && !gateReadyToResolve && Time.time >= activeGateEndTime)
        {
            gateReadyToResolve = true;

            if (!readyEventFired)
            {
                readyEventFired = true;
                OnGateBecameReady?.Invoke(activeGate);
            }

            OnGateStateChanged?.Invoke();
        }
    }

    // ---------- Gate Generation ----------
    public void EnsureGates()
    {
        // Only generate if no active gate and list empty
        if (activeGate != null) return;
        if (availableGates != null && availableGates.Count > 0) return;
        GenerateGates();
    }

    public void GenerateGates()
    {
        if (activeGate != null) return;

        if (availableGates == null) availableGates = new List<GateData>();
        availableGates.Clear();

        for (int i = 0; i < 3; i++)
            availableGates.Add(CreateGate());

        // Tell UI to refresh (views subscribe to events)
        OnGatesGenerated?.Invoke();
        OnGateStateChanged?.Invoke();
}

    GateData CreateGate()
    {
        if (player == null) player = FindObjectOfType<PlayerStats>();

        GateData gate = new GateData();
        gate.rank = RollRankWeighted();

        // Duration: 1–20 minutes
        int minutes = Random.Range(1, 21);
        gate.durationSeconds = minutes * 60;

        // Energy cost = minutes
        gate.energyCost = minutes;

        // Rewards: scale with minutes, rank and player level
        int lvl = player != null ? player.level : 1;
        float rankMult = GetRankRewardMultiplier(gate.rank);

        gate.rewardXP = Mathf.RoundToInt(minutes * 12f * rankMult * (1f + (lvl - 1) * 0.04f));
        gate.rewardGold = Mathf.RoundToInt(minutes * 8f * rankMult * (1f + (lvl - 1) * 0.03f));
        gate.rewardEssence = Mathf.RoundToInt(minutes * 2.5f * rankMult); // shadow essence

        SetDropChances(gate);
        GenerateEnemyStats(gate);

        return gate;
    }

    float GetRankRewardMultiplier(GateRank rank)
    {
        switch (rank)
        {
            case GateRank.ERank: return 0.9f;
            case GateRank.DRank: return 1.0f;
            case GateRank.CRank: return 1.15f;
            case GateRank.BRank: return 1.35f;
            case GateRank.ARank: return 1.6f;
            case GateRank.SRank: return 2.1f;
            default: return 1f;
        }
    }

    GateRank RollRankWeighted()
    {
        // E 30%, D 25%, C 18%, B 12%, A 10%, S 5%
        int roll = Random.Range(1, 101);
        if (roll <= 30) return GateRank.ERank;      // 30
        if (roll <= 55) return GateRank.DRank;      // +25
        if (roll <= 73) return GateRank.CRank;      // +18
        if (roll <= 85) return GateRank.BRank;      // +12
        if (roll <= 95) return GateRank.ARank;      // +10
        return GateRank.SRank;                      // +5
    }

    void SetDropChances(GateData gate)
    {
        // Random item chance:
        // E 3, D 5, C 8, B 12, A 16, S 32
        // Epic item chance:
        // E 1, D 2, C 3, B 5, A 7, S 15
        switch (gate.rank)
        {
            case GateRank.ERank: gate.randomItemChance = 3f;  gate.epicItemChance = 1f;  break;
            case GateRank.DRank: gate.randomItemChance = 5f;  gate.epicItemChance = 2f;  break;
            case GateRank.CRank: gate.randomItemChance = 8f;  gate.epicItemChance = 3f;  break;
            case GateRank.BRank: gate.randomItemChance = 12f; gate.epicItemChance = 5f;  break;
            case GateRank.ARank: gate.randomItemChance = 16f; gate.epicItemChance = 7f;  break;
            case GateRank.SRank: gate.randomItemChance = 32f; gate.epicItemChance = 15f; break;
        }
    }

    void GenerateEnemyStats(GateData gate)
    {
        if (player == null) player = FindObjectOfType<PlayerStats>();

        int baseAura = player != null ? player.GetAura() : 10;

        // Enemy gear scaling (placeholder): higher rank means better "equipment"
        float mult = 1f;
        switch (gate.rank)
        {
            case GateRank.ERank: mult = Random.Range(0.80f, 0.95f); break;
            case GateRank.DRank: mult = Random.Range(0.95f, 1.05f); break;
            case GateRank.CRank: mult = Random.Range(1.05f, 1.20f); break;
            case GateRank.BRank: mult = Random.Range(1.20f, 1.40f); break;
            case GateRank.ARank: mult = Random.Range(1.40f, 1.65f); break;
            case GateRank.SRank: mult = Random.Range(1.65f, 2.05f); break;
        }

        gate.enemyAura = Mathf.RoundToInt(baseAura * mult);

        int points = Mathf.Max(4, gate.enemyAura / 2);
        gate.enemySTR = Random.Range(points / 5, points / 3);
        gate.enemyVIT = Random.Range(points / 5, points / 3);
        gate.enemyDEX = Random.Range(points / 5, points / 3);
        gate.enemyINT = Mathf.Max(0, points - (gate.enemySTR + gate.enemyVIT + gate.enemyDEX));
    }

    // ---------- Accept / Run / Resolve ----------
    public void AcceptGate(int index)
    {
        if (activeGate != null) return;
        if (availableGates == null || availableGates.Count == 0) return;
        if (index < 0 || index >= availableGates.Count) return;

        if (energy == null) energy = FindObjectOfType<EnergySystem>();
        if (player == null) player = FindObjectOfType<PlayerStats>();

        GateData selected = availableGates[index];

        if (energy != null && !energy.UseEnergy(selected.energyCost))
        {
            Debug.Log("Nicht genug Energie!");
            return;
        }

        activeGate = selected;
        gateReadyToResolve = false;
        activeGateStartTime = Time.time;
        activeGateEndTime = Time.time + activeGate.durationSeconds;


        activeGateStartUnix = UnixNow();
        activeGateEndUnix = activeGateStartUnix + activeGate.durationSeconds;
        readyEventFired = false;

        // Once a gate is active, the selection list should be cleared (so we don\'t accept another)
        availableGates.Clear();

        if (gateRunningPanel != null)
            gateRunningPanel.SetActive(true);

        OnGateAccepted?.Invoke(activeGate);
        OnGateStateChanged?.Invoke();
// let UI switch panels immediately
    }

    /// <summary>
    /// Call this from Gate UI when the player enters the Gates screen AFTER timer ended.
    /// Returns true if a resolve happened.
    /// </summary>
    public bool ResolveActiveGateIfReady()
    {
        if (!IsActiveGateReadyToResolve())
            return false;

        ResolveGateInternal();
        return true;
    }

    void ResolveGateInternal()
    {
        EnsureRefs();
        if (player == null) return;
        if (activeGate == null) return;

        var gate = activeGate;

        // Deterministic combat (seeded)
        var combat = CombatResolver.Resolve(player, gate, activeGateEndUnix);

        bool win = combat.win;

        if (win)
        {
            player.GainXP(gate.rewardXP);
            player.AddGold(gate.rewardGold);
            player.AddEssence(gate.rewardEssence);

            HandleDrops(gate);
        }
        else
        {
            Debug.Log("Gate verloren → keine Belohnung.");
        }

        // Clear state
        activeGate = null;
        gateReadyToResolve = false;
        readyEventFired = false;
        activeGateStartTime = 0f;
        activeGateEndTime = 0f;
        activeGateStartUnix = 0;
        activeGateEndUnix = 0;

        if (gateRunningPanel != null)
            gateRunningPanel.SetActive(false);

        OnGateResolved?.Invoke(win, gate);
        OnGateStateChanged?.Invoke();
    }

    
    void HandleDrops(GateData gate)
    {
        EnsureRefs();
        if (gate == null) return;

        // Epic has priority over random item
        float rollEpic = UnityEngine.Random.Range(0f, 100f);
        if (rollEpic <= gate.epicItemChance)
        {
            // Epic -> at least Hero rarity
            var rarity = RollDropRarity(forceMinimum: ItemRarity.Hero);
            TryGrantDropItem(rarity, source: "Gate(Epic)");
            return;
        }

        float rollRandom = UnityEngine.Random.Range(0f, 100f);
        if (rollRandom <= gate.randomItemChance)
        {
            var rarity = RollDropRarity(forceMinimum: ItemRarity.ERank);
            TryGrantDropItem(rarity, source: "Gate");
        }
    }

    ItemRarity RollDropRarity(ItemRarity forceMinimum)
    {
        EnsureRefs();

        ItemRarity r = ItemRarity.ERank;
        if (anvil != null)
            r = anvil.RollRarity();

        if (r < forceMinimum)
            r = forceMinimum;

        return r;
    }

    void TryGrantDropItem(ItemRarity rarity, string source)
    {
        EnsureRefs();
        if (player == null || player.playerClass == null) { }

        if (anvil != null && anvil.itemDatabase != null)
        {
            // Use same generation logic as Anvil (best match), but with forced rarity
            var item = GenerateItemFromDatabaseForcedRarity(anvil.itemDatabase, player, rarity);
            if (item != null)
            {
                GrantItem(item, source);
                return;
            }
        }

        // Fallback: cannot roll item
        Debug.LogWarning("Gate drop failed: ItemDatabase not found or no item matched.");
    }

    void GrantItem(ItemData item, string source)
    {
        if (item == null) return;

        EnsureRefs();
        if (inventory != null && inventory.TryAdd(item))
            return;

        if (inbox != null)
        {
            inbox.Add(item, source);
            return;
        }

        Debug.LogWarning("Item granted but no Inventory/Inbox to store it.");
    }

    static ItemData GenerateItemFromDatabaseForcedRarity(ItemDatabase db, PlayerStats player, ItemRarity rarity)
    {
        if (db == null || player == null) return null;

        // ✅ Pool = alle Items die zur Klasse passen (keine Rarity auf Definition)
        var pool = db.GetFor(player.playerClass);

        if (pool == null || pool.Count == 0) return null;

        var chosen = pool[UnityEngine.Random.Range(0, pool.Count)];

        var item = new ItemData();
        item.definition = chosen;
        item.icon = chosen.icon;
        item.slot = chosen.slot;
        item.itemName = chosen.itemName;
        item.itemLevel = Mathf.Max(1, player.level);
        item.rarity = rarity;

        float mult = GetRarityMultiplier(rarity);
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

        if (rarity >= ItemRarity.ARank)
            item.auraBonusPercent = UnityEngine.Random.Range(5f, 15f);
        else if (rarity >= ItemRarity.Hero && UnityEngine.Random.value < 0.15f)
            item.auraBonusPercent = UnityEngine.Random.Range(1f, 6f);
        else
            item.auraBonusPercent = 0f;

        int sum = item.bonusSTR + item.bonusDEX + item.bonusINT + item.bonusVIT;
        item.itemAura = Mathf.RoundToInt(sum * (1f + item.auraBonusPercent / 100f));
        item.sellPrice = Mathf.RoundToInt((10 + sum) * mult);

        return item;
    }

    // ✅ IsClassAllowed ist jetzt in ItemDatabase.GetFor() integriert

    static float GetRarityMultiplier(ItemRarity r)
    {
        // Roughly aligned with crafting scaling
        switch (r)
        {
            case ItemRarity.ERank: return 0.8f;
            case ItemRarity.Common: return 0.9f;
            case ItemRarity.DRank: return 1.0f;
            case ItemRarity.CRank: return 1.15f;
            case ItemRarity.Rare: return 1.3f;
            case ItemRarity.BRank: return 1.5f;
            case ItemRarity.Hero: return 1.8f;
            case ItemRarity.ARank: return 2.2f;
            case ItemRarity.SRank: return 2.8f;
            case ItemRarity.Monarch: return 3.4f;
            case ItemRarity.Godlike: return 4.2f;
            default: return 1.0f;
        }
    }
}