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

    [Header("Battle System")]
    public BattleManager battleManager;
    public EnemyDatabase enemyDatabase;

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

    /// <summary>
    /// Generate enemy stats based on player level and gate rank.
    /// Enemies are always the same level as the player, with a random class.
    /// Stats are derived from what a player at this level would roughly have,
    /// scaled by rank difficulty (E-C: easy, B-A: challenging, S: very hard).
    /// Enemies are intentionally slightly weaker to keep the game fun.
    /// </summary>
    void GenerateEnemyStats(GateData gate)
    {
        if (player == null) player = FindObjectOfType<PlayerStats>();

        int lvl = player != null ? player.level : 1;

        // Random enemy class
        var classes = System.Enum.GetValues(typeof(PlayerClass));
        gate.enemyClass = (PlayerClass)classes.GetValue(Random.Range(0, classes.Length));

        // Rank power scaling:
        //   E-C rank: below average gear → easy for well-geared players
        //   B-A rank: average to strong → need decent gear
        //   S rank: strong → need near-best gear, but still beatable
        float rankPower;
        switch (gate.rank)
        {
            case GateRank.ERank: rankPower = Random.Range(0.40f, 0.55f); break;
            case GateRank.DRank: rankPower = Random.Range(0.55f, 0.70f); break;
            case GateRank.CRank: rankPower = Random.Range(0.70f, 0.85f); break;
            case GateRank.BRank: rankPower = Random.Range(0.85f, 1.00f); break;
            case GateRank.ARank: rankPower = Random.Range(1.00f, 1.15f); break;
            case GateRank.SRank: rankPower = Random.Range(1.10f, 1.25f); break;
            default: rankPower = 0.5f; break;
        }

        // === Base stats from level (simulates stat point allocation) ===
        // A character gets 3 points per level, distributed:
        //   ~55% primary, ~25% VIT, ~10% each to two others
        int totalPoints = 5 + (lvl - 1) * 3;
        int basePrimary = 5 + Mathf.RoundToInt((lvl - 1) * 3f * 0.55f);
        int baseVIT = 5 + Mathf.RoundToInt((lvl - 1) * 3f * 0.25f);
        int baseOther = 5 + Mathf.RoundToInt((lvl - 1) * 3f * 0.10f);

        // Virtual gear bonus: scales with level and rank
        // Represents equipment bonuses an enemy of this difficulty would have
        float gearScale = lvl * 2f * rankPower;
        int gearPrimary = Mathf.RoundToInt(gearScale * 0.5f);
        int gearVIT = Mathf.RoundToInt(gearScale * 0.3f);
        int gearOther = Mathf.RoundToInt(gearScale * 0.2f);

        int effPrimary = basePrimary + gearPrimary;
        int effVIT = baseVIT + gearVIT;
        int effOther = baseOther + gearOther;

        // Assign stats based on class
        switch (gate.enemyClass)
        {
            case PlayerClass.Assassin:
            case PlayerClass.Archer:
                gate.enemyDEX = effPrimary;
                gate.enemySTR = effOther;
                gate.enemyINT = effOther;
                break;
            case PlayerClass.Warrior:
                gate.enemySTR = effPrimary;
                gate.enemyDEX = effOther;
                gate.enemyINT = effOther;
                break;
            case PlayerClass.Mage:
            case PlayerClass.Necromancer:
                gate.enemyINT = effPrimary;
                gate.enemySTR = effOther;
                gate.enemyDEX = effOther;
                break;
        }
        gate.enemyVIT = effVIT;

        // === Derived stats (same formulas as PlayerStats) ===

        // HP: VIT * 15 * (1 + level * 0.02)
        float rawHP = effVIT * 15f * (1f + lvl * 0.02f);
        if (gate.enemyClass == PlayerClass.Warrior || gate.enemyClass == PlayerClass.Necromancer)
            rawHP *= 1.15f; // class HP passive
        gate.enemyHP = System.Math.Max(1L, (long)rawHP);

        // Damage: mainStat * virtualWeaponAvg * (1 + level * 0.03)
        // Virtual weapon damage simulates what an enemy at this gear level would deal
        float virtualWeaponAvg = Mathf.Max(1f, lvl * 4.5f * rankPower + Random.Range(-2f, 2f));
        float rawDmg = effPrimary * virtualWeaponAvg * (1f + lvl * 0.03f);
        if (gate.enemyClass == PlayerClass.Mage) rawDmg *= 1.25f; // class damage passive
        gate.enemyDamage = System.Math.Max(1L, (long)rawDmg);

        // Armor: scales with level and rank
        gate.enemyArmor = Mathf.Max(0, Mathf.RoundToInt(lvl * 3.5f * rankPower));

        // Crit Rate: base 15% + class bonus + level scaling
        float baseCritRate = 15f;
        if (gate.enemyClass == PlayerClass.Assassin) baseCritRate += 15f;
        gate.enemyCritRate = Mathf.Clamp(baseCritRate + lvl * 0.15f * rankPower, 0f, 100f);

        // Crit Damage: base 50% + class bonus + level scaling
        float baseCritDmg = 50f;
        if (gate.enemyClass == PlayerClass.Archer) baseCritDmg += 25f;
        gate.enemyCritDamage = Mathf.Max(30f, baseCritDmg + lvl * 0.3f * rankPower);

        // Speed: base 100 + DEX*0.5 + class bonus
        int speedDEX = (gate.enemyClass == PlayerClass.Assassin || gate.enemyClass == PlayerClass.Archer)
            ? effPrimary : effOther;
        float rawSpeed = 100f + speedDEX * 0.5f;
        if (gate.enemyClass == PlayerClass.Assassin) rawSpeed *= 1.20f;
        else if (gate.enemyClass == PlayerClass.Archer) rawSpeed *= 1.10f;
        gate.enemySpeed = Mathf.Max(50f, rawSpeed);

        // Aura for display/cross-stat
        gate.enemyAura = (int)Mathf.Min(
            (gate.enemySTR + gate.enemyDEX + gate.enemyINT + gate.enemyVIT) * 100f, int.MaxValue);
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
            Debug.Log("Not enough energy!");
            return;
        }

        activeGate = selected;
        gateReadyToResolve = false;
        activeGateStartTime = Time.time;
        activeGateEndTime = Time.time + activeGate.durationSeconds;


        activeGateStartUnix = UnixNow();
        activeGateEndUnix = activeGateStartUnix + activeGate.durationSeconds;
        readyEventFired = false;

        // Once a gate is active, the selection list should be cleared (so we don't accept another)
        availableGates.Clear();

        if (gateRunningPanel != null)
            gateRunningPanel.SetActive(true);

        OnGateAccepted?.Invoke(activeGate);
        OnGateStateChanged?.Invoke();
// let UI switch panels immediately
    }

    /// <summary>
    /// Skip the active gate for 1 MC. Instantly resolves combat.
    /// </summary>
    public bool SkipGate(PlayerStats playerRef)
    {
        if (activeGate == null) return false;
        if (playerRef == null) return false;

        if (!playerRef.SpendManaCrystals(1))
        {
            Debug.Log("Not enough Mana Crystals to skip!");
            return false;
        }

        activeGateEndTime = Time.time - 1f;
        gateReadyToResolve = true;
        ResolveGateInternal();
        return true;
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

        // Try to use BattleManager for visual combat
        if (battleManager == null)
            battleManager = FindObjectOfType<BattleManager>();

        if (battleManager != null)
        {
            // Build battle setup
            var setup = new BattleSetupData
            {
                playerStats = player,
                playerCharData = GetPlayerCharacterData(),
                enemyDefinition = GetEnemyForGate(gate),
                gateData = gate,
                context = BattleContext.Gate,
                seed = activeGateEndUnix,
                rewards = new BattleRewards
                {
                    xp = gate.rewardXP,
                    gold = gate.rewardGold,
                    essence = gate.rewardEssence,
                    droppedItem = RollDropItem(gate)
                }
            };

            // Subscribe to battle completion (one-shot)
            battleManager.OnBattleFinished -= OnBattleComplete;
            battleManager.OnBattleFinished += OnBattleComplete;

            // Hide running panel, battle screen takes over
            if (gateRunningPanel != null)
                gateRunningPanel.SetActive(false);

            battleManager.StartBattle(setup);
        }
        else
        {
            // Fallback: instant resolve (no BattleManager in scene)
            ResolveGateInstant(gate);
        }
    }

    /// <summary>
    /// Called when BattleManager finishes (player pressed Continue).
    /// </summary>
    void OnBattleComplete(bool won, BattleSetupData setup)
    {
        if (battleManager != null)
            battleManager.OnBattleFinished -= OnBattleComplete;

        // Grant dropped item on win (BattleManager already gave XP/Gold/Essence)
        if (won && setup.rewards != null && setup.rewards.droppedItem != null)
            GrantItem(setup.rewards.droppedItem, "Gate");

        // Clear gate state
        ClearActiveGate();
        OnGateResolved?.Invoke(won, setup.gateData);
        OnGateStateChanged?.Invoke();

        // Generate new gates
        EnsureGates();
    }

    /// <summary>
    /// Fallback: resolve gate instantly without battle screen.
    /// Used when BattleManager is not present in the scene.
    /// </summary>
    void ResolveGateInstant(GateData gate)
    {
        var combat = CombatResolver.Resolve(player, gate, activeGateEndUnix);
        bool win = combat.win;

        if (win)
        {
            player.GainXP(gate.rewardXP);
            player.AddGold(gate.rewardGold);
            player.AddEssence(gate.rewardEssence);
            HandleDrops(gate);
        }

        ClearActiveGate();

        if (gateRunningPanel != null)
            gateRunningPanel.SetActive(false);

        OnGateResolved?.Invoke(win, gate);
        OnGateStateChanged?.Invoke();
    }

    void ClearActiveGate()
    {
        activeGate = null;
        gateReadyToResolve = false;
        readyEventFired = false;
        activeGateStartTime = 0f;
        activeGateEndTime = 0f;
        activeGateStartUnix = 0;
        activeGateEndUnix = 0;
    }

    /// <summary>
    /// Get the player's CharacterData for portrait display.
    /// </summary>
    CharacterData GetPlayerCharacterData()
    {
        var pm = ProfileManager.Instance;
        if (pm != null) return pm.GetActiveCharacter();
        return null;
    }

    /// <summary>
    /// Get an enemy definition for the gate rank from the database.
    /// </summary>
    EnemyDefinition GetEnemyForGate(GateData gate)
    {
        if (enemyDatabase == null) return null;
        return enemyDatabase.GetRandomEnemy(EnemyPool.Gate, gate.rank);
    }

    /// <summary>
    /// Pre-roll the item drop for battle reward display.
    /// Returns null if no drop.
    /// Epic drops on S-Rank use the top 3 rarities from the player's anvil,
    /// weighted by their actual drop rates.
    /// </summary>
    ItemData RollDropItem(GateData gate)
    {
        EnsureRefs();
        if (gate == null) return null;

        // Epic has priority (S-Rank only)
        float rollEpic = UnityEngine.Random.Range(0f, 100f);
        if (rollEpic <= gate.epicItemChance)
        {
            // Use the top 3 rarities from the anvil, weighted by actual drop rates
            ItemRarity rarity = ItemRarity.ERank;
            if (anvil != null)
                rarity = anvil.RollTopRarities(3);

            return GenerateDropItem(rarity, ItemQuality.Epic);
        }

        float rollRandom = UnityEngine.Random.Range(0f, 100f);
        if (rollRandom <= gate.randomItemChance)
        {
            var rarity = RollDropRarity(forceMinimum: ItemRarity.ERank);
            ItemQuality quality = AnvilSystem.RollQuality(rarity);
            return GenerateDropItem(rarity, quality);
        }

        return null;
    }

    ItemData GenerateDropItem(ItemRarity rarity, ItemQuality quality)
    {
        EnsureRefs();
        if (anvil == null || anvil.itemDatabase == null) return null;
        return GenerateItemFromDatabase(anvil.itemDatabase, player.playerClass, rarity, quality, player.level);
    }


    void HandleDrops(GateData gate)
    {
        EnsureRefs();
        if (gate == null) return;

        // Epic has priority over random item (S-Rank only)
        float rollEpic = UnityEngine.Random.Range(0f, 100f);
        if (rollEpic <= gate.epicItemChance)
        {
            // Use top 3 rarities from anvil, weighted by actual drop rates
            ItemRarity rarity = ItemRarity.ERank;
            if (anvil != null)
                rarity = anvil.RollTopRarities(3);

            TryGrantDropItem(rarity, ItemQuality.Epic, source: "Gate(Epic)");
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

    /// <summary>
    /// Normal gate drop: roll quality first, then pick item from quality pool.
    /// </summary>
    void TryGrantDropItem(ItemRarity rarity, string source)
    {
        EnsureRefs();
        if (anvil == null || anvil.itemDatabase == null)
        {
            Debug.LogWarning("Gate drop failed: ItemDatabase not found.");
            return;
        }

        PlayerClass pc = player.playerClass;
        ItemQuality quality = AnvilSystem.RollQuality(rarity);

        var item = GenerateItemFromDatabase(anvil.itemDatabase, pc, rarity, quality, player.level);
        if (item != null)
            GrantItem(item, source);
    }

    /// <summary>
    /// Forced quality gate drop (e.g. epic drop guarantee).
    /// Picks item from the forced quality pool directly.
    /// </summary>
    void TryGrantDropItem(ItemRarity rarity, ItemQuality forcedQuality, string source)
    {
        EnsureRefs();
        if (anvil == null || anvil.itemDatabase == null)
        {
            Debug.LogWarning("Gate drop failed: ItemDatabase not found.");
            return;
        }

        PlayerClass pc = player.playerClass;

        var item = GenerateItemFromDatabase(anvil.itemDatabase, pc, rarity, forcedQuality, player.level);
        if (item != null)
            GrantItem(item, source);
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

    /// <summary>
    /// Quality-first item generation: quality is already decided, pick item from that pool.
    /// Fallback to Normal if no items exist for the rolled quality.
    /// </summary>
    static ItemData GenerateItemFromDatabase(ItemDatabase db, PlayerClass pc, ItemRarity rarity, ItemQuality quality, int playerLevel)
    {
        if (db == null) return null;

        // Pool = items for this class AND quality
        var pool = db.GetFor(pc, quality);

        // Fallback: if no Epic/Legendary item exists -> Normal
        if ((pool == null || pool.Count == 0) && quality != ItemQuality.Normal)
        {
            quality = ItemQuality.Normal;
            pool = db.GetFor(pc, quality);
        }

        if (pool == null || pool.Count == 0)
        {
            Debug.LogWarning($"Gate drop: no items for {pc} (Quality: {quality})");
            return null;
        }

        var chosen = pool[UnityEngine.Random.Range(0, pool.Count)];

        var item = new ItemData();
        item.definition = chosen;
        item.icon = chosen.icon;
        item.slot = chosen.slot;
        item.itemName = chosen.itemName;
        item.itemLevel = Mathf.Max(1, playerLevel);
        item.rarity = rarity;
        item.quality = quality;

        ItemStatGenerator.GenerateStats(item, playerLevel, pc);

        return item;
    }

}
