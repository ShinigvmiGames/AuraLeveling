using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main Battle Screen UI controller.
/// Displays player vs enemy with turn-by-turn replay of pre-resolved combat.
/// HP bars and numbers animate smoothly toward their target values (like S&F).
/// </summary>
public class BattleUI : MonoBehaviour
{
    [Header("Player Side (Left)")]
    public PortraitBuilder playerPortrait;
    public Image playerPortraitBg;
    public RectTransform playerPortraitRT;
    public Image playerHPBarFill;
    public TMP_Text playerNameText;
    public TMP_Text playerHPText;

    [Header("Enemy Side (Right)")]
    public Image enemyPortraitImage;
    public PortraitBuilder enemyPvPPortrait;
    public Image enemyPortraitBg;
    public RectTransform enemyPortraitRT;
    public Image enemyHPBarFill;
    public TMP_Text enemyNameText;
    public TMP_Text enemyHPText;

    [Header("Damage Popup")]
    public DamagePopup playerDamagePopup;
    public DamagePopup enemyDamagePopup;

    [Header("Result Panel")]
    public GameObject resultPanel;
    public TMP_Text resultTitleText;

    [Header("Reward Display (inside Result Panel)")]
    public GameObject rewardRow;
    public TMP_Text rewardXPText;
    public Image rewardXPIcon;
    public TMP_Text rewardGoldText;
    public Image rewardGoldIcon;
    public TMP_Text rewardEssenceText;
    public Image rewardEssenceIcon;

    [Header("Item Drop Display (inside Result Panel)")]
    public GameObject itemDropDisplay;
    public Image itemDropIcon;
    public Image itemDropGlow;
    public TMP_Text itemDropNameText;

    [Header("Skip / Continue Button")]
    public Button actionButton;
    public TMP_Text actionButtonText;

    [Header("Battle Animator")]
    public BattleAnimator animator;

    [Header("Playback Settings")]
    public float delayBetweenActions = 0.6f;
    public float resultShowDelay = 0.5f;

    [Header("HP Animation")]
    public float hpAnimDuration = 0.4f;

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

    // Smooth HP animation state
    long displayedPlayerHP;
    long displayedEnemyHP;
    long targetPlayerHP;
    long targetEnemyHP;
    long playerHPAnimStart;
    long enemyHPAnimStart;
    float playerHPAnimTimer;
    float enemyHPAnimTimer;
    bool playerHPAnimating;
    bool enemyHPAnimating;

    void Awake()
    {
        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionButtonPressed);

        if (resultPanel != null)
            resultPanel.SetActive(false);
        if (itemDropDisplay != null)
            itemDropDisplay.SetActive(false);
    }

    void Update()
    {
        bool needsUpdate = false;

        // Smooth player HP animation
        if (playerHPAnimating)
        {
            playerHPAnimTimer += Time.deltaTime;
            float t = Mathf.Clamp01(playerHPAnimTimer / hpAnimDuration);
            t = EaseOutQuad(t);
            displayedPlayerHP = (long)Mathf.Lerp(playerHPAnimStart, targetPlayerHP, t);
            if (playerHPAnimTimer >= hpAnimDuration)
            {
                displayedPlayerHP = targetPlayerHP;
                playerHPAnimating = false;
            }
            needsUpdate = true;
        }

        // Smooth enemy HP animation
        if (enemyHPAnimating)
        {
            enemyHPAnimTimer += Time.deltaTime;
            float t = Mathf.Clamp01(enemyHPAnimTimer / hpAnimDuration);
            t = EaseOutQuad(t);
            displayedEnemyHP = (long)Mathf.Lerp(enemyHPAnimStart, targetEnemyHP, t);
            if (enemyHPAnimTimer >= hpAnimDuration)
            {
                displayedEnemyHP = targetEnemyHP;
                enemyHPAnimating = false;
            }
            needsUpdate = true;
        }

        if (needsUpdate) UpdateHPDisplay();
    }

    public void Initialize(BattleSetupData battleSetup, BattleResult battleResult)
    {
        setup = battleSetup;
        result = battleResult;
        battleFinished = false;
        skipped = false;

        playerWeaponType = GetWeaponForClass(setup.playerStats.playerClass);
        enemyWeaponType = setup.enemyDefinition != null
            ? setup.enemyDefinition.weaponType
            : GetWeaponForClass(setup.gateData.enemyClass);

        SetupVisuals();
        SetupHPBars();

        if (resultPanel != null) resultPanel.SetActive(false);
        if (itemDropDisplay != null) itemDropDisplay.SetActive(false);

        if (actionButtonText != null) actionButtonText.text = "SKIP";
    }

    void SetupVisuals()
    {
        if (playerPortrait != null && setup.playerCharData != null)
            playerPortrait.Build(setup.playerCharData);

        if (playerNameText != null)
            playerNameText.text = setup.playerCharData != null
                ? $"{setup.playerCharData.name} Lv.{setup.playerStats.level}"
                : $"Player Lv.{setup.playerStats.level}";

        if (setup.isVsPlayer && enemyPvPPortrait != null && setup.opponentCharData != null)
        {
            if (enemyPortraitImage != null) enemyPortraitImage.gameObject.SetActive(false);
            if (enemyPvPPortrait != null) enemyPvPPortrait.gameObject.SetActive(true);
            enemyPvPPortrait.Build(setup.opponentCharData);
            if (enemyNameText != null) enemyNameText.text = setup.opponentCharData.name;
        }
        else
        {
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
        targetPlayerHP = result.playerMaxHP;
        targetEnemyHP = result.enemyMaxHP;
        playerHPAnimating = false;
        enemyHPAnimating = false;
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

    void AnimatePlayerHP(long newHP)
    {
        playerHPAnimStart = displayedPlayerHP;
        targetPlayerHP = newHP;
        playerHPAnimTimer = 0f;
        playerHPAnimating = true;
    }

    void AnimateEnemyHP(long newHP)
    {
        enemyHPAnimStart = displayedEnemyHP;
        targetEnemyHP = newHP;
        enemyHPAnimTimer = 0f;
        enemyHPAnimating = true;
    }

    public void StartPlayback()
    {
        if (playbackCoroutine != null) StopCoroutine(playbackCoroutine);
        playbackCoroutine = StartCoroutine(PlaybackRoutine());
    }

    IEnumerator PlaybackRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        foreach (var action in result.actions)
        {
            if (skipped) break;

            bool isPlayerAttack = action.isPlayerAttack;
            RectTransform attackerRT = isPlayerAttack ? playerPortraitRT : enemyPortraitRT;
            RectTransform defenderRT = isPlayerAttack ? enemyPortraitRT : playerPortraitRT;
            Image defenderImg = isPlayerAttack ? enemyPortraitBg : playerPortraitBg;
            WeaponType weapon = isPlayerAttack ? playerWeaponType : enemyWeaponType;
            DamagePopup defenderPopup = isPlayerAttack ? enemyDamagePopup : playerDamagePopup;
            DamagePopup attackerPopup = isPlayerAttack ? playerDamagePopup : enemyDamagePopup;

            // Show "BERSERK!" on attacker before extra attack animation
            if (action.isExtraAttack && attackerPopup != null)
            {
                attackerPopup.Show(0, PopupType.Berserk);
                yield return new WaitForSeconds(0.3f);
            }

            // Play attack animation
            if (animator != null)
                yield return StartCoroutine(animator.PlayAttack(
                    attackerRT, defenderRT, defenderImg, weapon, isPlayerAttack));

            // === DODGE: show "DODGE!" instead of damage ===
            if (action.isDodge)
            {
                if (defenderPopup != null)
                    defenderPopup.Show(0, PopupType.Dodge);

                yield return new WaitForSeconds(delayBetweenActions);
                continue;
            }

            // === DAMAGE POPUP ===
            if (defenderPopup != null)
            {
                if (action.isDoubleDamage)
                    defenderPopup.Show(action.damage, PopupType.DoubleDamage);
                else if (action.isCrit)
                    defenderPopup.Show(action.damage, PopupType.Crit);
                else
                    defenderPopup.Show(action.damage, PopupType.Normal);
            }

            // Animate HP smoothly toward target
            if (isPlayerAttack)
                AnimateEnemyHP(action.defenderHPAfter);
            else
                AnimatePlayerHP(action.defenderHPAfter);

            // === STUN: show "STUNNED!" on defender ===
            if (action.isStun && defenderPopup != null)
            {
                yield return new WaitForSeconds(0.3f);
                defenderPopup.Show(0, PopupType.Stun);
            }

            // === REVIVE: show "UNDYING!" and animate HP back up ===
            if (action.isRevive)
            {
                yield return new WaitForSeconds(0.4f);

                // Show revive popup on the defender (who just revived)
                if (defenderPopup != null)
                    defenderPopup.Show(0, PopupType.Revive);

                // Animate HP bar back up from 0 to revive HP
                if (isPlayerAttack)
                    AnimateEnemyHP(action.reviveHP);
                else
                    AnimatePlayerHP(action.reviveHP);
            }

            yield return new WaitForSeconds(delayBetweenActions);
        }

        // Wait for HP animation to complete
        yield return new WaitForSeconds(hpAnimDuration);

        yield return new WaitForSeconds(resultShowDelay);
        ShowResult();
    }

    void OnActionButtonPressed()
    {
        if (!battleFinished)
            SkipToEnd();
        else
            OnBattleUIFinished?.Invoke();
    }

    void SkipToEnd()
    {
        skipped = true;

        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

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

            // Handle revive: if the last action triggered a revive,
            // the defender's HP should show the revive amount
            if (lastAction.isRevive)
            {
                if (lastAction.isPlayerAttack)
                    displayedEnemyHP = lastAction.reviveHP;
                else
                    displayedPlayerHP = lastAction.reviveHP;
            }
        }

        targetPlayerHP = displayedPlayerHP;
        targetEnemyHP = displayedEnemyHP;
        playerHPAnimating = false;
        enemyHPAnimating = false;
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
            if (rewardRow != null) rewardRow.SetActive(false);
            if (itemDropDisplay != null) itemDropDisplay.SetActive(false);
        }
    }

    void ShowRewards()
    {
        if (setup.rewards == null) return;

        if (rewardRow != null) rewardRow.SetActive(true);

        if (rewardXPText != null)
            rewardXPText.text = FormatNumber(setup.rewards.xp);
        if (rewardXPIcon != null)
        {
            Sprite xpSprite = CurrencyIcons.Energy;
            if (xpSprite != null) rewardXPIcon.sprite = xpSprite;
        }

        if (rewardGoldText != null)
            rewardGoldText.text = FormatNumber(setup.rewards.gold);
        if (rewardGoldIcon != null)
        {
            Sprite goldSprite = CurrencyIcons.Gold;
            if (goldSprite != null) rewardGoldIcon.sprite = goldSprite;
        }

        if (rewardEssenceText != null)
            rewardEssenceText.text = FormatNumber(setup.rewards.essence);
        if (rewardEssenceIcon != null)
        {
            Sprite essenceSprite = CurrencyIcons.ShadowEssence;
            if (essenceSprite != null) rewardEssenceIcon.sprite = essenceSprite;
        }

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
            case PlayerClass.Warrior:     return WeaponType.Sword;
            case PlayerClass.Assassin:    return WeaponType.Dagger;
            case PlayerClass.Archer:      return WeaponType.Bow;
            case PlayerClass.Mage:        return WeaponType.Grimoire;
            case PlayerClass.Necromancer: return WeaponType.Scythe;
            default:                      return WeaponType.Sword;
        }
    }

    /// <summary>
    /// Format number with dots as thousands separators.
    /// e.g. 15000123 → "15.000.123"
    /// </summary>
    static string FormatNumber(long value)
    {
        return value.ToString("#,0").Replace(',', '.');
    }

    static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
}
