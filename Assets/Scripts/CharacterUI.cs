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

    [Header("Derived Stats (auto-wired from scene if null)")]
    public TMP_Text txtMaxHP;
    public TMP_Text txtAttack;
    public TMP_Text txtMaxMana;
    public TMP_Text txtDefense;

    void Start()
    {
        if (player == null) player = FindObjectOfType<PlayerStats>();

        AutoWireAll();

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

    /// <summary>
    /// Auto-find all UI fields from scene GameObjects if not wired in Inspector.
    /// </summary>
    void AutoWireAll()
    {
        // Stat points text
        if (txtPoints == null)
        {
            var go = GameObject.Find("Txt_Points");
            if (go != null) txtPoints = go.GetComponent<TMP_Text>();
        }

        // Main stat texts
        if (txtSTR == null)
        {
            var go = GameObject.Find("Txt_STR");
            if (go != null) txtSTR = go.GetComponent<TMP_Text>();
        }
        if (txtDEX == null)
        {
            var go = GameObject.Find("Txt_DEX");
            if (go != null) txtDEX = go.GetComponent<TMP_Text>();
        }
        if (txtINT == null)
        {
            var go = GameObject.Find("Txt_INT");
            if (go != null) txtINT = go.GetComponent<TMP_Text>();
        }
        if (txtVIT == null)
        {
            var go = GameObject.Find("Txt_VIT");
            if (go != null) txtVIT = go.GetComponent<TMP_Text>();
        }

        // Stat plus buttons
        if (btnSTR == null)
        {
            var go = GameObject.Find("Btn_STR_Plus");
            if (go != null) btnSTR = go.GetComponent<Button>();
        }
        if (btnDEX == null)
        {
            var go = GameObject.Find("Btn_DEX_Plus");
            if (go != null) btnDEX = go.GetComponent<Button>();
        }
        if (btnINT == null)
        {
            var go = GameObject.Find("Btn_INT_Plus");
            if (go != null) btnINT = go.GetComponent<Button>();
        }
        if (btnVIT == null)
        {
            var go = GameObject.Find("Btn_VIT_Plus");
            if (go != null) btnVIT = go.GetComponent<Button>();
        }

        // Derived stats
        if (txtMaxHP == null)
        {
            var go = GameObject.Find("Txt_MaxHP");
            if (go != null) txtMaxHP = go.GetComponent<TMP_Text>();
        }
        if (txtAttack == null)
        {
            var go = GameObject.Find("Txt_Attack");
            if (go != null) txtAttack = go.GetComponent<TMP_Text>();
        }
        if (txtMaxMana == null)
        {
            var go = GameObject.Find("Txt_MaxMana");
            if (go != null) txtMaxMana = go.GetComponent<TMP_Text>();
        }
        if (txtDefense == null)
        {
            var go = GameObject.Find("Txt_Defense");
            if (go != null) txtDefense = go.GetComponent<TMP_Text>();
        }
    }

    void Spend(string stat)
    {
        if (player == null) return;
        player.TrySpendPoint(stat);
        Refresh(); // immediate visual feedback
    }

    void Refresh()
    {
        if (player == null) return;

        // Remaining stat points
        if (txtPoints != null)
        {
            if (player.unspentPoints > 0)
                txtPoints.text = $"Verfügbare Punkte: <color=#FFD700>{player.unspentPoints}</color>";
            else
                txtPoints.text = "Verfügbare Punkte: 0";
        }

        // Main stats: show base + equipment bonus in green parentheses if bonus > 0
        if (txtSTR != null) txtSTR.text = FormatStat("STR", player.STR, player.bonusSTR);
        if (txtDEX != null) txtDEX.text = FormatStat("DEX", player.DEX, player.bonusDEX);
        if (txtINT != null) txtINT.text = FormatStat("INT", player.INT, player.bonusINT);
        if (txtVIT != null) txtVIT.text = FormatStat("VIT", player.VIT, player.bonusVIT);

        // Derived stats (include totals from equipment)
        if (txtMaxHP != null) txtMaxHP.text = $"HP: {player.maxHP}";
        if (txtAttack != null) txtAttack.text = $"ATK: {player.attack}";
        if (txtMaxMana != null) txtMaxMana.text = $"Mana: {player.maxMana}";
        if (txtDefense != null) txtDefense.text = $"DEF: {player.defense}";

        // Enable/disable plus buttons based on available points
        bool canSpend = player.unspentPoints > 0;

        if (btnSTR != null) btnSTR.interactable = canSpend;
        if (btnDEX != null) btnDEX.interactable = canSpend;
        if (btnINT != null) btnINT.interactable = canSpend;
        if (btnVIT != null) btnVIT.interactable = canSpend;
    }

    /// <summary>
    /// Format a main stat line. Shows equipment bonus in green parentheses if > 0.
    /// e.g. "STR: 5" or "STR: 5 <color=#4CFF4C>(+3)</color>"
    /// </summary>
    string FormatStat(string name, int baseVal, int bonus)
    {
        if (bonus > 0)
            return $"{name}: {baseVal} <color=#4CFF4C>(+{bonus})</color>";
        return $"{name}: {baseVal}";
    }
}
