using TMPro;
using UnityEngine;

public class TopBarUI : MonoBehaviour
{
    public PlayerStats player;

    [Header("Drag these from the scene (4 slots)")]
    public TMP_Text txtLevel;
    public TMP_Text txtAura;
    public TMP_Text txtGold;
    public TMP_Text txtManaCrystals;

    void Start()
    {
        if (player == null) player = FindObjectOfType<PlayerStats>();

        if (player != null)
            player.onStatsChanged += Refresh;

        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        if (player == null) return;

        if (txtLevel != null) txtLevel.text = $"Lv{player.level}";
        if (txtAura != null) txtAura.text = $"{player.GetAura()}";
        if (txtGold != null) txtGold.text = $"{player.gold}";
        if (txtManaCrystals != null) txtManaCrystals.text = $"{player.manaCrystals}";
    }

    void OnDestroy()
    {
        if (player != null)
            player.onStatsChanged -= Refresh;
    }
}
