using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main Battle Screen UI controller.
/// Displays player vs enemy with turn-by-turn replay of pre-resolved combat.
///
/// Layout (portrait mode):
///   Top area: Player portrait (left) vs Enemy portrait (right) with HP bars
///   Bottom area: Result panel (hidden during combat) + Skip/Continue button
///
/// Setup from BattleManager:
///   1. Call Initialize(setup, result) with battle data
///   2. Call StartPlayback() to begin animating turns
///   3. OnBattleUIFinished fires when player presses Continue
/// </summary>
public class BattleUI : MonoBehaviour
{
    [Header("Player Side (Left)")]
    public PortraitBuilder playerPortrait;
    public Image playerPortraitBg;       // background/frame image for flash effect
    public RectTransform playerPortraitRT;
    public Image playerHPBarFill;
    public TMP_Text playerNameText;
    public TMP_Text playerHPText;

    [Header("Enemy Side (Right)")]
    public Image enemyPortraitImage;     // single sprite for PvE enemies
    public PortraitBuilder enemyPvPPortrait; // 4-layer portrait for PvP
    public Image enemyPortraitBg;
    public RectTransform enemyPortraitRT;
    public Image enemyHPBarFill;
    public TMP_Text enemyNameText;
    public TMP_Text enemyHPText;

    [Header("Damage Popup")]
    public DamagePopup playerDamagePopup;  // popup positioned above player portrait
    public DamagePopup enemyDamagePopup;   // popup positioned above enemy portrait

    [Header("Result Panel")]
    public GameObject resultPanel;
    public TMP_Text resultTitleText;      // "You've won the battle!" / "You've lost the battle."

    [Header("Reward Display (inside Result Panel)")]
    public GameObject rewardRow;          // parent for reward icons+text
    public TMP_Text rewardXPText;
    public Image rewardXPIcon;
    public TMP_Text rewardGoldText;
    public Image rewardGoldIcon;
    public TMP_Text rewardEssenceText;
    public Image rewardEssenceIcon;

    [Header("Item Drop Display (inside Result Panel)")]
    public GameObject itemDropDisplay;    // container for dropped item
    public Image itemDropIcon;
    public Image itemDropGlow;            // quality glow
    public TMP_Text itemDropNameText;

    [Header("Skip / Continue Button")]
    public Button actionButton;
    public TMP_Text actionButtonText;

    [Header("Battle Animator")]
    public BattleAnimator animator;

    [Header("Playback Settings")]
    public float delayBetweenActions = 0.6f;
    public float resultShowDelay = 0.5f;

    // Events
    public event Action OnBattleUIFinished;

    // State
    BattleSetupData setup;
    BattleResult result;
    WeaponType playerWeaponType;
    WeaponType enemyWeaponType;
    Coroutine playbackCoroutine;
    bool battleFinished;
    bool skipped;
    long displayedPlayerHP;
    long displayedEnemyHP;

    void Awake()
    {
        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionButtonPressed);

        if (resultPanel != null)
            resultPanel.SetActive(false);
        if (itemDropDisplay != null)
            itemDropDisplay.SetActive(false);
    }

    /// <summary>
    /// Initialize the battle screen with setup data and pre-resolved result.
    /// </summary>
    public void Initialize(BattleSetupData battleSetup, BattleResult battleResult)
    {
        setup = battleSetup;
        result = battleResult;
        battleFinished = false;
        skipped = false;

        // Determine weapon types
        playerWeaponType = GetWeaponForClass(setup.playerStats.playerClass);
        enemyWeaponType = setup.enemyDefinition != null
            ? setup.enemyDefinition.weaponType
            : GetWeaponForClass(setup.gateData.enemyClass);

        SetupVisuals();
        SetupHPBars();

        // Hide result panel
        if (resultPanel != null) resultPanel.SetActive(false);
        if (itemDropDisplay != null) itemDropDisplay.SetActive(false);

        // Set button to SKIP mode
        if (actionButtonText != null) actionButtonText.text = "SKIP";
    }

    void SetupVisuals()
    {
        // Player portrait
        if (playerPortrait != null && setup.playerCharData != null)
            playerPortrait.Build(setup.playerCharData);

        if (playerNameText != null)
            playerNameText.text = setup.playerCharData != null
                ? $"{setup.playerCharData.name} Lv.{setup.playerStats.level}"
                : $"Player Lv.{setup.playerStats.level}";

        // Enemy portrait
        if (setup.isVsPlayer && enemyPvPPortrait != null && setup.opponentCharData != null)
        {
            // PvP: use portrait builder
            if (enemyPortraitImage != null) enemyPortraitImage.gameObject.SetActive(false);
            if (enemyPvPPortrait != null) enemyPvPPortrait.gameObject.SetActive(true);
            enemyPvPPortrait.Build(setup.opponentCharData);
            if (enemyNameText != null) enemyNameText.text = setup.opponentCharData.name;
        }
        else
        {
            // PvE: use enemy definition sprite
            if (enemyPvPPortrait != null) enemyPvPPortrait.gameObject.SetActive(false);
            if (enemyPortraitImage != null)
            {
                enemyPortraitImage.gameObject.SetActive(true);
                if (setup.enemyDefinition != null && setup.enemyDefinition.portrait != null)
                    enemyPortraitImage.sprite = setup.enemyDefinition.portrait;
            }
            if (enemyNameText != null)
                enemyNameText.text = setup.enemyDefinition != null
                    ? setup.enemyDefinition.enemyName
                    : setup.gateData.enemyClass.ToString();
        }
    }

    void SetupHPBars()
    {
        displayedPlayerHP = result.playerMaxHP;
        displayedEnemyHP = result.enemyMaxHP;
        UpdateHPDisplay();
    }

    void UpdateHPDisplay()
    {
        if (playerHPBarFill != null)
            playerHPBarFill.fillAmount = result.playerMaxHP > 0
                ? Mathf.Clamp01((float)displayedPlayerHP / result.playerMaxHP) : 0f;

        if (enemyHPBarFill != null)
            enemyHPBarFill.fillAmount = result.enemyMaxHP > 0
                ? Mathf.Clamp01((float)displayedEnemyHP / result.enemyMaxHP) : 0f;

        if (playerHPText != null)
            playerHPText.text = $"{FormatNumber(displayedPlayerHP)}/{FormatNumber(result.playerMaxHP)}";

        if (enemyHPText != null)
            enemyHPText.text = $"{FormatNumber(displayedEnemyHP)}/{FormatNumber(result.enemyMaxHP)}";
    }

    /// <summary>
    /// Start the turn-by-turn playback coroutine.
    /// </summary>
    public void StartPlayback()
    {
        if (playbackCoroutine != null) StopCoroutine(playbackCoroutine);
        playbackCoroutine = StartCoroutine(PlaybackRoutine());
    }

    IEnumerator PlaybackRoutine()
    {
        // Small delay before combat starts
        yield return new WaitForSeconds(0.5f);

        foreach (var action in result.actions)
        {
            if (skipped) break;

            // Determine attacker/defender
            bool isPlayerAttack = action.isPlayerAttack;
            RectTransform attackerRT = isPlayerAttack ? playerPortraitRT : enemyPortraitRT;
            RectTransform defenderRT = isPlayerAttack ? enemyPortraitRT : playerPortraitRT;
            Image defenderImg = isPlayerAttack ? enemyPortraitBg : playerPortraitBg;
            WeaponType weapon = isPlayerAttack ? playerWeaponType : enemyWeaponType;
            DamagePopup popup = isPlayerAttack ? enemyDamagePopup : playerDamagePopup;

            // Play attack animation
            if (animator != null)
                yield return StartCoroutine(animator.PlayAttack(
                    attackerRT, defenderRT, defenderImg, weapon, isPlayerAttack));

            // Show damage popup
            if (popup != null)
            {
                PopupType popupType = action.isCrit ? PopupType.Crit : PopupType.Normal;
                popup.Show(action.damage, popupType);
            }

            // Update HP
            if (isPlayerAttack)
                displayedEnemyHP = action.defenderHPAfter;
            else
                displayedPlayerHP = action.defenderHPAfter;

            // Show lifesteal heal popup
            if (action.lifestealAmount > 0)
            {
                DamagePopup healPopup = isPlayerAttack ? playerDamagePopup : enemyDamagePopup;
                if (healPopup != null)
                    healPopup.Show(action.lifestealAmount, PopupType.Heal);

                // Update attacker HP for lifesteal
                if (isPlayerAttack)
                    displayedPlayerHP = action.attackerHPAfter;
                else
                    displayedEnemyHP = action.attackerHPAfter;
            }

            UpdateHPDisplay();

            // Pause between actions
            yield return new WaitForSeconds(delayBetweenActions);
        }

        // Battle finished
        yield return new WaitForSeconds(resultShowDelay);
        ShowResult();
    }

    void OnActionButtonPressed()
    {
        if (!battleFinished)
        {
            // SKIP: jump to end
            SkipToEnd();
        }
        else
        {
            // CONTINUE: close battle screen
            OnBattleUIFinished?.Invoke();
        }
    }

    void SkipToEnd()
    {
        skipped = true;

        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

        // Jump HP to final values
        if (result.actions.Count > 0)
        {
            var lastAction = result.actions[result.actions.Count - 1];
            if (lastAction.isPlayerAttack)
            {
                displayedPlayerHP = lastAction.attackerHPAfter;
                displayedEnemyHP = lastAction.defenderHPAfter;
            }
            else
            {
                displayedEnemyHP = lastAction.attackerHPAfter;
                displayedPlayerHP = lastAction.defenderHPAfter;
            }
        }
        UpdateHPDisplay();

        ShowResult();
    }

    void ShowResult()
    {
        battleFinished = true;

        if (actionButtonText != null) actionButtonText.text = "Continue";

        if (resultPanel != null) resultPanel.SetActive(true);

        if (result.playerWon)
        {
            if (resultTitleText != null)
                resultTitleText.text = "You've won the battle! Rewards:";

            ShowRewards();
        }
        else
        {
            if (resultTitleText != null)
                resultTitleText.text = "You've lost the battle.";

            // Hide reward row on loss
            if (rewardRow != null) rewardRow.SetActive(false);
            if (itemDropDisplay != null) itemDropDisplay.SetActive(false);
        }
    }

    void ShowRewards()
    {
        if (setup.rewards == null) return;

        if (rewardRow != null) rewardRow.SetActive(true);

        // XP
        if (rewardXPText != null)
            rewardXPText.text = FormatNumber(setup.rewards.xp);
        if (rewardXPIcon != null)
        {
            Sprite xpSprite = CurrencyIcons.Energy; // XP uses energy-like icon or custom
            if (xpSprite != null) rewardXPIcon.sprite = xpSprite;
        }

        // Gold
        if (rewardGoldText != null)
            rewardGoldText.text = FormatNumber(setup.rewards.gold);
        if (rewardGoldIcon != null)
        {
            Sprite goldSprite = CurrencyIcons.Gold;
            if (goldSprite != null) rewardGoldIcon.sprite = goldSprite;
        }

        // Shadow Essence
        if (rewardEssenceText != null)
            rewardEssenceText.text = FormatNumber(setup.rewards.essence);
        if (rewardEssenceIcon != null)
        {
            Sprite essenceSprite = CurrencyIcons.ShadowEssence;
            if (essenceSprite != null) rewardEssenceIcon.sprite = essenceSprite;
        }

        // Item drop
        if (setup.rewards.droppedItem != null && itemDropDisplay != null)
        {
            itemDropDisplay.SetActive(true);
            var item = setup.rewards.droppedItem;

            if (itemDropIcon != null && item.icon != null)
                itemDropIcon.sprite = item.icon;

            if (itemDropGlow != null)
                QualityGlow.Apply(itemDropGlow, item.quality);

            if (itemDropNameText != null)
                itemDropNameText.text = item.itemName;
        }
        else if (itemDropDisplay != null)
        {
            itemDropDisplay.SetActive(false);
        }
    }

    static WeaponType GetWeaponForClass(PlayerClass pc)
    {
        switch (pc)
        {
            case PlayerClass.Warrior:       return WeaponType.Sword;
            case PlayerClass.Assassine:     return WeaponType.Dagger;
            case PlayerClass.Bogenschuetze: return WeaponType.Bow;
            case PlayerClass.Magier:        return WeaponType.Staff;
            case PlayerClass.Nekromant:     return WeaponType.Scythe;
            default:                        return WeaponType.Sword;
        }
    }

    static string FormatNumber(long value)
    {
        if (value >= 1_000_000_000L) return $"{value / 1_000_000_000f:0.0}B";
        if (value >= 1_000_000L) return $"{value / 1_000_000f:0.0}M";
        if (value >= 10_000L) return $"{value / 1_000f:0.0}K";
        return value.ToString();
    }
}
