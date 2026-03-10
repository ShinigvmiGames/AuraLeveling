using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles attack animations in the battle screen.
/// Each attack: attacker lunges forward, weapon effect flies to defender,
/// defender flashes red and shakes.
/// </summary>
public class BattleAnimator : MonoBehaviour
{
    [Header("Weapon Effect Sprites (assign in Inspector)")]
    public Sprite slashSprite;   // Sword
    public Sprite arrowSprite;   // Bow
    public Sprite orbSprite;     // Staff
    public Sprite daggerSprite;  // Dagger
    public Sprite scytheSprite;  // Scythe

    [Header("Animation Settings")]
    public float lungeDuration = 0.12f;
    public float lungeDistance = 30f;
    public float effectFlyDuration = 0.2f;
    public float hitFlashDuration = 0.15f;
    public float hitShakeDuration = 0.15f;
    public float hitShakeIntensity = 8f;

    [Header("Effect Pool")]
    public RectTransform effectContainer; // parent for spawned effect sprites

    /// <summary>
    /// Play full attack animation sequence.
    /// attackerRT: the portrait RectTransform of the attacker
    /// defenderRT: the portrait RectTransform of the defender
    /// defenderImage: Image component to flash red
    /// weaponType: determines which sprite to use
    /// isPlayerAttack: player is on the left, enemy on the right
    /// </summary>
    public IEnumerator PlayAttack(RectTransform attackerRT, RectTransform defenderRT,
        Image defenderImage, WeaponType weaponType, bool isPlayerAttack)
    {
        // 1. Lunge attacker toward defender
        float lungeDir = isPlayerAttack ? 1f : -1f;
        yield return StartCoroutine(Lunge(attackerRT, lungeDir));

        // 2. Fly weapon effect from attacker to defender
        yield return StartCoroutine(FlyEffect(attackerRT, defenderRT, weaponType));

        // 3. Flash defender red + shake
        StartCoroutine(FlashRed(defenderImage));
        yield return StartCoroutine(Shake(defenderRT));
    }

    IEnumerator Lunge(RectTransform rt, float direction)
    {
        Vector2 original = rt.anchoredPosition;
        Vector2 target = original + Vector2.right * (lungeDistance * direction);

        // Move forward
        float t = 0f;
        while (t < lungeDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / lungeDuration);
            rt.anchoredPosition = Vector2.Lerp(original, target, EaseOutQuad(p));
            yield return null;
        }

        // Move back
        t = 0f;
        while (t < lungeDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / lungeDuration);
            rt.anchoredPosition = Vector2.Lerp(target, original, EaseInQuad(p));
            yield return null;
        }

        rt.anchoredPosition = original;
    }

    IEnumerator FlyEffect(RectTransform from, RectTransform to, WeaponType weaponType)
    {
        Sprite effectSprite = GetEffectSprite(weaponType);
        if (effectSprite == null || effectContainer == null)
            yield break;

        // Create temporary effect
        var go = new GameObject("AttackEffect");
        go.transform.SetParent(effectContainer, false);
        var effectRT = go.AddComponent<RectTransform>();
        effectRT.sizeDelta = new Vector2(48, 48);

        var img = go.AddComponent<Image>();
        img.sprite = effectSprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        Vector2 startPos = GetWorldToLocal(from, effectContainer);
        Vector2 endPos = GetWorldToLocal(to, effectContainer);

        float t = 0f;
        while (t < effectFlyDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / effectFlyDuration);
            effectRT.anchoredPosition = Vector2.Lerp(startPos, endPos, p);
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
            case WeaponType.Sword:   return slashSprite;
            case WeaponType.Bow:     return arrowSprite;
            case WeaponType.Staff:   return orbSprite;
            case WeaponType.Dagger:  return daggerSprite;
            case WeaponType.Scythe:  return scytheSprite;
            default:                 return slashSprite;
        }
    }

    /// <summary>
    /// Convert a RectTransform's world position into local anchored position
    /// relative to another RectTransform (the container).
    /// </summary>
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
    static float EaseInQuad(float t) => t * t;
}
