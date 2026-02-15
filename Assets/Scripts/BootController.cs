using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootController : MonoBehaviour
{
    public ScreenFader fader;
    public float logoHoldTime = 1.5f;
    public string nextScene = "Splash";

    IEnumerator Start()
    {
        // Start schwarz → Logo reinfaden
        fader.FadeIn();

        yield return new WaitForSeconds(logoHoldTime);

        // Ausfaden
        fader.FadeOut();

        yield return new WaitForSeconds(fader.fadeDuration);

        SceneManager.LoadScene(nextScene);
    }
}