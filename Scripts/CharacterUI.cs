using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterUI : MonoBehaviour
{
    public PlayerStats player;

    public TMP_Text txtPoints;

    public TMP_Text txtSTR;
    public TMP_Text txtDEX;
    public TMP_Text txtINT;
    public TMP_Text txtVIT;

    public Button btnSTR;
    public Button btnDEX;
    public Button btnINT;
    public Button btnVIT;

    void Start()
    {
        if (player == null) player = FindObjectOfType<PlayerStats>();

        if (btnSTR != null) btnSTR.onClick.AddListener(() => Spend("STR"));
        if (btnDEX != null) btnDEX.onClick.AddListener(() => Spend("DEX"));
        if (btnINT != null) btnINT.onClick.AddListener(() => Spend("INT"));
        if (btnVIT != null) btnVIT.onClick.AddListener(() => Spend("VIT"));

        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Spend(string stat)
    {
        if (player == null) return;
        player.TrySpendPoint(stat);
    }

    void Refresh()
    {
        if (player == null) return;

        if (txtPoints != null) txtPoints.text = $"Punkte verfügbar: {player.unspentPoints}";

        if (txtSTR != null) txtSTR.text = $"STR: {player.STR}";
        if (txtDEX != null) txtDEX.text = $"DEX: {player.DEX}";
        if (txtINT != null) txtINT.text = $"INT: {player.INT}";
        if (txtVIT != null) txtVIT.text = $"VIT: {player.VIT}";

        bool canSpend = player.unspentPoints > 0;

        if (btnSTR != null) btnSTR.interactable = canSpend;
        if (btnDEX != null) btnDEX.interactable = canSpend;
        if (btnINT != null) btnINT.interactable = canSpend;
        if (btnVIT != null) btnVIT.interactable = canSpend;
    }
}