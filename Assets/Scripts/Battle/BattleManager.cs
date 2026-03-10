using System;
using UnityEngine;

/// <summary>
/// Orchestrates battle flow: receives setup data, runs CombatResolver,
/// displays the battle via BattleUI, and distributes rewards on completion.
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("References")]
    public BattleUI battleUI;
    public ScreenManager screenManager;

    /// <summary>
    /// Fired when the battle is fully complete (player pressed Continue).
    /// bool = playerWon, BattleSetupData = the setup for context.
    /// </summary>
    public event Action<bool, BattleSetupData> OnBattleFinished;

    BattleSetupData currentSetup;
    BattleResult currentResult;

    void Awake()
    {
        if (battleUI != null)
            battleUI.OnBattleUIFinished += OnUIFinished;
    }

    void OnDestroy()
    {
        if (battleUI != null)
            battleUI.OnBattleUIFinished -= OnUIFinished;
    }

    /// <summary>
    /// Start a battle. The battle screen will show and play back the combat.
    /// Call this from GateManager (or future Dungeon/Arena managers).
    /// </summary>
    public void StartBattle(BattleSetupData setup)
    {
        currentSetup = setup;

        // Pre-resolve the entire combat with turn logging
        currentResult = CombatResolver.ResolveWithLog(
            setup.playerStats, setup.gateData, setup.seed);

        // Show battle panel
        if (screenManager != null)
        {
            screenManager.lockScreenSwitch = true;
            screenManager.ShowBattle();
        }

        // Initialize and start playback
        if (battleUI != null)
        {
            battleUI.gameObject.SetActive(true);
            battleUI.Initialize(setup, currentResult);
            battleUI.StartPlayback();
        }
    }

    /// <summary>
    /// Called when player presses Continue after the battle ends.
    /// Distributes rewards and closes the battle screen.
    /// </summary>
    void OnUIFinished()
    {
        if (currentSetup == null) return;

        bool won = currentResult != null && currentResult.playerWon;

        // Distribute rewards on win
        if (won && currentSetup.rewards != null)
        {
            var player = currentSetup.playerStats;
            var rewards = currentSetup.rewards;

            if (rewards.xp > 0) player.GainXP(rewards.xp);
            if (rewards.gold > 0) player.AddGold(rewards.gold);
            if (rewards.essence > 0) player.AddEssence(rewards.essence);

            // Item drop handled by the caller (GateManager) since it needs
            // InventorySystem/ItemInbox references
        }

        // Hide battle panel
        if (screenManager != null)
        {
            screenManager.HideBattle();
            screenManager.lockScreenSwitch = false;
        }

        if (battleUI != null)
            battleUI.gameObject.SetActive(false);

        // Notify caller
        OnBattleFinished?.Invoke(won, currentSetup);

        currentSetup = null;
        currentResult = null;
    }
}
