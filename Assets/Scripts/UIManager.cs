using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject screenGates;
    public GameObject screenAnvil;
    public GameObject screenCharacter;

    /// <summary>
    /// Set to true by AnvilUI while crafting is in progress.
    /// Prevents screen switching until the craft + popup is done.
    /// </summary>
    [HideInInspector] public bool lockScreenSwitch = false;

    public void ShowGates()
    {
        if (lockScreenSwitch) return;
        screenGates.SetActive(true);
        screenAnvil.SetActive(false);
        screenCharacter.SetActive(false);
    }

    public void ShowAnvil()
    {
        if (lockScreenSwitch) return;
        screenGates.SetActive(false);
        screenAnvil.SetActive(true);
        screenCharacter.SetActive(false);
    }

    public void ShowCharacter()
    {
        if (lockScreenSwitch) return;
        screenGates.SetActive(false);
        screenAnvil.SetActive(false);
        screenCharacter.SetActive(true);
    }
}
