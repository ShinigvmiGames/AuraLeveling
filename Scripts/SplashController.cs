using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashController : MonoBehaviour
{
    public ScreenFader fader;
    public string nextScene = "Auth";
    private bool canTap = false;

    IEnumerator Start()
    {
        fader.FadeIn();
        yield return new WaitForSeconds(1f);
        canTap = true;
    }

    void Update()
    {
        if (!canTap) return;

        if (Input.GetMouseButtonDown(0))
        {
            fader.FadeOut();
            Invoke(nameof(LoadNext), 1f);
        }
    }

    void LoadNext()
    {
        SceneManager.LoadScene(nextScene);
    }
}