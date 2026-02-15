using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject screenGates;
    public GameObject screenAnvil;
    public GameObject screenCharacter;

    public void ShowGates()
    {
        screenGates.SetActive(true);
        screenAnvil.SetActive(false);
        screenCharacter.SetActive(false);
    }

    public void ShowAnvil()
    {
        screenGates.SetActive(false);
        screenAnvil.SetActive(true);
        screenCharacter.SetActive(false);
    }

    public void ShowCharacter()
    {
        screenGates.SetActive(false);
        screenAnvil.SetActive(false);
        screenCharacter.SetActive(true);
    }
}
