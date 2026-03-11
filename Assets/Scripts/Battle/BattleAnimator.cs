using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles attack animations in the battle screen.
/// Each attack: weapon swings at the defender (S&F style), defender flashes red and shakes.
/// No lunge — weapon appears at the defender's position with a swing animation.
/// </summary>
public class BattleAnimator : MonoBehaviour
{
    [Header("Weapon Effect Sprites (assign in Inspector)")]
    public Sprite slashSprite;     // Sword (Warrior)
    public Sprite arrowSprite;     // Bow (Archer)
    public Sprite grimoireSprite;  // Grimoire (Mage)
    public Sprite daggerSprite;    // Dagger (Assassin)
    public Sprite scytheSprite;    // Scythe (Necromancer)

    [Header("Animation Settings")]
    public float weaponSwingDuration = 0.25f;
    public float hitFlashDuration = 0.15f;
    public float hitShakeDuration = 0.15f;
    public float hitShakeIntensity = 8f;

    [Header("Effect Pool")]
    public RectTransform effectContainer;

    /// <summary>
    /// Play full attack animation sequence.
    /// 1. Weapon swing at defender position
    /// 2. Flash defender red + shake
    /// </summary>
    public IEnumerator PlayAttack(RectTransform attackerRT, RectTransform defenderRT,
        Image defenderImage, WeaponType weaponType, bool isPlayerAttack)
    {
        // 1. Swing weapon at defender
        yield return StartCoroutine(SwingWeapon(defenderRT, weaponType, isPlayerAttack));

        // 2. Flash defender red + shake (parallel)
        StartCoroutine(FlashRed(defenderImage));
        yield return StartCoroutine(Shake(defenderRT));
    }

    /// <summary>
    /// Weapon sprite appears at the defender, swings with rotation and scale,
    /// then fades out. Similar to Shakes & Fidget style.
    /// </summary>
    IEnumerator SwingWeapon(RectTransform defenderRT, WeaponType weaponType, bool isPlayerAttack)
    {
        Sprite effectSprite = GetEffectSprite(weaponType);
        if (effectSprite == null || effectContainer == null)
            yield break;

        // Create temporary weapon effect
        var go = new GameObject("WeaponSwing");
        go.transform.SetParent(effectContainer, false);
        var effectRT = go.AddComponent<RectTransform>();
        effectRT.sizeDelta = new Vector2(64, 64);

        var img = go.AddComponent<Image>();
        img.sprite = effectSprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        // Position at defender
        Vector2 defPos = GetWorldToLocal(defenderRT, effectContainer);
        effectRT.anchoredPosition = defPos;

        // Swing direction: player attacks from left, enemy from right
        float swingDir = isPlayerAttack ? 1f : -1f;
        float t = 0f;

        while (t < weaponSwingDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / weaponSwingDuration);

            // Rotation: swing arc from -45° to +45°
            float angle = Mathf.Lerp(-45f * swingDir, 45f * swingDir, EaseOutQuad(p));
            effectRT.localRotation = Quaternion.Euler(0, 0, angle);

            // Scale: quick pop (0.5→1.2) then settle (1.2→0.8)
            float scale;
            if (p < 0.3f)
                scale = Mathf.Lerp(0.5f, 1.2f, p / 0.3f);
            else
                scale = Mathf.Lerp(1.2f, 0.8f, (p - 0.3f) / 0.7f);
            effectRT.localScale = Vector3.one * scale;

            // Fade out in last 30%
            if (p > 0.7f)
            {
                float fade = 1f - (p - 0.7f) / 0.3f;
                img.color = new Color(1f, 1f, 1f, fade);
            }

            yield return null;
        }

        Destroy(go);
    }

    IEnumerator FlashRed(Image img)
    {
        if (img == null) yield break;

        Color original = img.color;
        img.color = new Color(1f, 0.3f, 0.3f);

        float t = 0f;
        while (t < hitFlashDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / hitFlashDuration);
            img.color = Color.Lerp(new Color(1f, 0.3f, 0.3f), original, p);
            yield return null;
        }

        img.color = original;
    }

    IEnumerator Shake(RectTransform rt)
    {
        Vector2 original = rt.anchoredPosition;

        float t = 0f;
        while (t < hitShakeDuration)
        {
            t += Time.deltaTime;
            float decay = 1f - Mathf.Clamp01(t / hitShakeDuration);
            float offsetX = Random.Range(-hitShakeIntensity, hitShakeIntensity) * decay;
            float offsetY = Random.Range(-hitShakeIntensity, hitShakeIntensity) * decay;
            rt.anchoredPosition = original + new Vector2(offsetX, offsetY);
            yield return null;
        }

        rt.anchoredPosition = original;
    }

    Sprite GetEffectSprite(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Sword:    return slashSprite;
            case WeaponType.Bow:      return arrowSprite;
            case WeaponType.Grimoire: return grimoireSprite;
            case WeaponType.Dagger:   return daggerSprite;
            case WeaponType.Scythe:   return scytheSprite;
            default:                  return slashSprite;
        }
    }

    Vector2 GetWorldToLocal(RectTransform source, RectTransform container)
    {
        Vector3 worldPos = source.position;
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            container, RectTransformUtility.WorldToScreenPoint(null, worldPos),
            null, out localPos);
        return localPos;
    }

    static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
}
