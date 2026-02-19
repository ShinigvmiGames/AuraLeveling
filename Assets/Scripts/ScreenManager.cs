using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    [Header("Screens")]
    public GameObject screenGates;
    public GameObject screenAnvil;
    public GameObject screenCharacter;

    [Header("Optional Popups")]
    public GameObject itemPopup; // Root vom ItemPopup

    /// <summary>
    /// Set to true by AnvilUI while crafting is in progress.
    /// Prevents screen switching until the craft + popup is done.
    /// </summary>
    [HideInInspector] public bool lockScreenSwitch = false;

    void Start()
    {
        // 0 = Gates, 1 = Anvil, 2 = Character
        int startTab = PlayerPrefs.GetInt("START_TAB", 0);
        if (startTab == 1) ShowAnvil();
        else if (startTab == 2) ShowCharacter();
        else ShowGates();

        PlayerPrefs.DeleteKey("START_TAB"); // optional: reset
    }

    public void ShowGates()
    {
        if (lockScreenSwitch) return;
        SetOnly(screenGates);
        ClosePopups();
    }

    public void ShowAnvil()
    {
        if (lockScreenSwitch) return;
        SetOnly(screenAnvil);
        ClosePopups();
    }

    public void ShowCharacter()
    {
        if (lockScreenSwitch) return;
        SetOnly(screenCharacter);
        ClosePopups();
    }

    void SetOnly(GameObject target)
    {
        if (screenGates != null) screenGates.SetActive(target == screenGates);
        if (screenAnvil != null) screenAnvil.SetActive(target == screenAnvil);
        if (screenCharacter != null) screenCharacter.SetActive(target == screenCharacter);
    }

    void ClosePopups()
    {
        if (itemPopup != null) itemPopup.SetActive(false);
    }
}