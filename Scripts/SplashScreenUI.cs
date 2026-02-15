using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SplashScreenUI : MonoBehaviour
{
    [Header("Scene")]
    public string authSceneName = "Auth";

    [Header("Refs")]
    public CanvasGroup tapToStartGroup;   // CanvasGroup auf Img_TapToStart
    public CanvasGroup fadeOverlayGroup;  // optional: CanvasGroup auf FlashOverlay (kann null sein)

    [Header("Timing")]
    public float inputDelay = 1f;         // 1 Sekunde Sperre
    public float blinkSpeed = 1.2f;       // höher = schneller blinken (1.0–1.6 passt meist gut)
    public float minAlpha = 0.25f;        // wie “aus” soll es werden
    public float maxAlpha = 1f;           // wie “an” soll es werden
    public float fadeOutTime = 0.6f;      // Übergang in nächste Szene

    bool canTap = false;
    Coroutine blinkRoutine;

    void Start()
    {
        // Startzustand
        if (tapToStartGroup != null)
        {
            tapToStartGroup.alpha = 0f;
            tapToStartGroup.interactable = false;
            tapToStartGroup.blocksRaycasts = false;
        }

        if (fadeOverlayGroup != null)
            fadeOverlayGroup.alpha = 0f;

        StartCoroutine(EnableTapAfterDelay());
    }

    IEnumerator EnableTapAfterDelay()
    {
        yield return new WaitForSeconds(inputDelay);

        canTap = true;

        // TapToStart sichtbar machen und blink starten
        if (tapToStartGroup != null)
        {
            tapToStartGroup.alpha = maxAlpha;
            blinkRoutine = StartCoroutine(BlinkTapToStart());
        }
    }

    IEnumerator BlinkTapToStart()
    {
        // Smooth PingPong Alpha
        while (true)
        {
            float t = Mathf.PingPong(Time.time * blinkSpeed, 1f); // 0..1..0
            float a = Mathf.Lerp(minAlpha, maxAlpha, t);
            tapToStartGroup.alpha = a;
            yield return null;
        }
    }

    void Update()
    {
        if (!canTap) return;

        // Tippen/Klicken irgendwo
        if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(GoToAuth());
        }
    }

    IEnumerator GoToAuth()
    {
        canTap = false;

        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);

        // TapToStart ausblenden (optional)
        if (tapToStartGroup != null)
            tapToStartGroup.alpha = 0f;

        // Optionaler Fade-Out über Overlay
        if (fadeOverlayGroup != null)
        {
            float t = 0f;
            while (t < fadeOutTime)
            {
                t += Time.deltaTime;
                fadeOverlayGroup.alpha = Mathf.Clamp01(t / fadeOutTime);
                yield return null;
            }
        }

        SceneManager.LoadScene(authSceneName);
    }
}