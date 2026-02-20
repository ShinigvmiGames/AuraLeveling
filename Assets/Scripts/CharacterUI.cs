using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterUI : MonoBehaviour
{
    public PlayerStats player;

    [Header("Stat Points")]
    public TMP_Text txtPoints;

    [Header("Main Stats")]
    public TMP_Text txtSTR;
    public TMP_Text txtDEX;
    public TMP_Text txtINT;
    public TMP_Text txtVIT;

    [Header("Main Stat Buttons")]
    public Button btnSTR;
    public Button btnDEX;
    public Button btnINT;
    public Button btnVIT;

    [Header("Derived Stats")]
    public TMP_Text txtMaxHP;
    public TMP_Text txtAttack;
    public TMP_Text txtMaxMana;
    public TMP_Text txtDefense;

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
        Refresh();
    }

    void Refresh()
    {
        if (player == null) return;

        if (txtPoints != null)
        {
            if (player.unspentPoints > 0)
                txtPoints.text = $"Verfügbare Punkte: <color=#FFD700>{player.unspentPoints}</color>";
            else
                txtPoints.text = "Verfügbare Punkte: 0";
        }

        if (txtSTR != null) txtSTR.text = FormatStat("STR", player.STR, player.bonusSTR);
        if (txtDEX != null) txtDEX.text = FormatStat("DEX", player.DEX, player.bonusDEX);
        if (txtINT != null) txtINT.text = FormatStat("INT", player.INT, player.bonusINT);
        if (txtVIT != null) txtVIT.text = FormatStat("VIT", player.VIT, player.bonusVIT);

        if (txtMaxHP != null) txtMaxHP.text = $"HP: {player.maxHP}";
        if (txtAttack != null) txtAttack.text = $"ATK: {player.attack}";
        if (txtMaxMana != null) txtMaxMana.text = $"Mana: {player.maxMana}";
        if (txtDefense != null) txtDefense.text = $"DEF: {player.defense}";

        bool canSpend = player.unspentPoints > 0;
        if (btnSTR != null) btnSTR.interactable = canSpend;
        if (btnDEX != null) btnDEX.interactable = canSpend;
        if (btnINT != null) btnINT.interactable = canSpend;
        if (btnVIT != null) btnVIT.interactable = canSpend;
    }

    string FormatStat(string name, int baseVal, int bonus)
    {
        if (bonus > 0)
            return $"{name}: {baseVal} <color=#4CFF4C>(+{bonus})</color>";
        return $"{name}: {baseVal}";
    }
}
