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
        // Random item chance per rank, epic items ONLY from S-Rank (20%)
        switch (gate.rank)
        {
            case GateRank.ERank: gate.randomItemChance = 3f;  gate.epicItemChance = 0f;  break;
            case GateRank.DRank: gate.randomItemChance = 5f;  gate.epicItemChance = 0f;  break;
            case GateRank.CRank: gate.randomItemChance = 8f;  gate.epicItemChance = 0f;  break;
            case GateRank.BRank: gate.randomItemChance = 12f; gate.epicItemChance = 0f;  break;
            case GateRank.ARank: gate.randomItemChance = 16f; gate.epicItemChance = 0f;  break;
            case GateRank.SRank: gate.randomItemChance = 40f; gate.epicItemChance = 20f; break;
        }
    }

    void GenerateEnemyStats(GateData gate)
    {
        if (player == null) player = FindObjectOfType<PlayerStats>();

        // Rank difficulty multiplier
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

        // Random enemy class
        var classes = System.Enum.GetValues(typeof(PlayerClass));
        gate.enemyClass = (PlayerClass)classes.GetValue(Random.Range(0, classes.Length));

        if (player != null)
        {
            // Mirror player stats with rank multiplier
            gate.enemyHP = System.Math.Max(1L, (long)(player.maxHP * mult));
            gate.enemyDamage = System.Math.Max(1L, (long)(player.damage * mult));
            gate.enemyArmor = Mathf.Max(0, Mathf.RoundToInt(player.armor * mult));
            gate.enemyCritRate = Mathf.Clamp(player.critRate * mult * 0.8f, 0f, 100f);
            gate.enemyCritDamage = Mathf.Max(100f, player.critDamage * mult * 0.85f);
            gate.enemySpeed = Mathf.Max(50f, player.speed * mult * 0.95f);

            // Main stats for cross-stat reduction
            long baseAura = player.GetAura();
            gate.enemyAura = (int)Mathf.Min(baseAura * mult, int.MaxValue);
            int points = Mathf.Max(4, Mathf.RoundToInt(gate.enemyAura * 0.005f));
            gate.enemySTR = Random.Range(points / 5, Mathf.Max(points / 5 + 1, points / 3));
            gate.enemyVIT = Random.Range(points / 5, Mathf.Max(points / 5 + 1, points / 3));
            gate.enemyDEX = Random.Range(points / 5, Mathf.Max(points / 5 + 1, points / 3));
            gate.enemyINT = Mathf.Max(0, points - (gate.enemySTR + gate.enemyVIT + gate.enemyDEX));
        }
        else
        {
            // Fallback for when player not found
            gate.enemyHP = 76;
            gate.enemyDamage = 5;
            gate.enemyArmor = 0;
            gate.enemyCritRate = 5f;
            gate.enemyCritDamage = 150f;
            gate.enemySpeed = 100f;
            gate.enemyAura = 10;
            gate.enemySTR = 5;
            gate.enemyVIT = 5;
            gate.enemyDEX = 5;
            gate.enemyINT = 5;
        }
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
            // Epic drop -> at least Hero rarity, guaranteed Epic+ quality
            var rarity = RollDropRarity(forceMinimum: ItemRarity.Hero);
            TryGrantDropItem(rarity, ItemQuality.Epic, source: "Gate(Epic)");
            return;
        }

        float rollRandom = UnityEngine.Random.Range(0f, 100f);
        if (rollRandom <= gate.randomItemChance)
        {
            var rarity = RollDropRarity(forceMinimum: ItemRarity.ERank);
            TryGrantDropItem(rarity, ItemQuality.Normal, source: "Gate");
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

    void TryGrantDropItem(ItemRarity rarity, ItemQuality minQuality, string source)
    {
        EnsureRefs();

        if (anvil != null && anvil.itemDatabase != null)
        {
            // Roll quality first, guarantee at least minQuality
            ItemQuality rolled = AnvilSystem.RollQuality(rarity);
            ItemQuality quality = rolled > minQuality ? rolled : minQuality;

            var item = GenerateItemFromDatabaseForcedRarity(anvil.itemDatabase, player, rarity, quality);
            if (item != null)
            {
                GrantItem(item, source);
                return;
            }
        }

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

    static ItemData GenerateItemFromDatabaseForcedRarity(ItemDatabase db, PlayerStats player, ItemRarity rarity, ItemQuality quality)
    {
        if (db == null || player == null) return null;

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
        item.quality = quality;

        ItemStatGenerator.GenerateStats(item, player.level, player.playerClass);

        return item;
    }

}