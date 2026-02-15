using TMPro;
using UnityEngine;

public class TopBarUI : MonoBehaviour
{
    public PlayerStats player;
    public EnergySystem energy;

    public TMP_Text txtLevel;
    public TMP_Text txtAura;
    public TMP_Text txtEnergy;
    public TMP_Text txtGold;

    // optional
    public TMP_Text txtEssence;

    void Start()
    {
        if (player == null) player = FindObjectOfType<PlayerStats>();
        if (energy == null) energy = FindObjectOfType<EnergySystem>();
        player = FindObjectOfType<PlayerStats>();
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

        if (txtLevel != null) txtLevel.text = $"Lv {player.level}";
        if (txtAura != null) txtAura.text = $"Aura: {player.GetAura()}";
        if (txtGold != null) txtGold.text = $"Gold: {player.gold}";

        if (energy != null && txtEnergy != null)
            txtEnergy.text = $"Energie: {energy.currentEnergy}";

        if (txtEssence != null)
            txtEssence.text = $"Essenz: {player.shadowEssence}";
    }

    void onDestroy()
    {
        if (player != null)
        player.onStatsChanged -= Refresh;
    }
}