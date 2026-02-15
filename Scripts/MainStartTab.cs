using UnityEngine;

public class MainStartTab : MonoBehaviour
{
    [Header("Assign your screens")]
    public GameObject screenAnvil;
    public GameObject screenCharacter;
    public GameObject screenGates;

    void Start()
    {
        string tab = PlayerPrefs.GetString("START_TAB", "");

        if (tab == "ANVIL")
        {
            if (screenAnvil) screenAnvil.SetActive(true);
            if (screenCharacter) screenCharacter.SetActive(false);
            if (screenGates) screenGates.SetActive(false);

            PlayerPrefs.DeleteKey("START_TAB"); // one-time
            PlayerPrefs.Save();
        }
    }
}