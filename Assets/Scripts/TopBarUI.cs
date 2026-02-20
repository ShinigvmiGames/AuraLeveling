using TMPro;
using UnityEngine;

public class TopBarUI : MonoBehaviour
{
    public PlayerStats player;

    // TopBar shows exactly 4 values: Level, Aura, Gold, ManaCrystals
    TMP_Text txtLevel;
    TMP_Text txtAura;
    TMP_Text txtGold;
    TMP_Text txtManaCrystals;

    void Start()
    {
        if (player == null) player = FindObjectOfType<PlayerStats>();

        AutoWire();

        if (player != null)
            player.onStatsChanged += Refresh;

        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void AutoWire()
    {
        // Scene layout (old names): Txt_Level, Txt_Aura, Txt_Energy, Txt_Gold, Txt_Essence
        // Target:                    Txt_Level, Txt_Aura, Txt_Gold,   Txt_ManaCrystals, (destroyed)
        //
        // Rename order matters: rename Txt_Gold → Txt_ManaCrystals FIRST,
        // then Txt_Energy → Txt_Gold, so there's no name collision.

        // Slot 1: Level — already named correctly
        if (txtLevel == null)
        {
            var go = GameObject.Find("Txt_Level");
            if (go != null) txtLevel = go.GetComponent<TMP_Text>();
        }

        // Slot 2: Aura — already named correctly
        if (txtAura == null)
        {
            var go = GameObject.Find("Txt_Aura");
            if (go != null) txtAura = go.GetComponent<TMP_Text>();
        }

        // Slot 4 first: rename Txt_Gold → Txt_ManaCrystals (before Slot 3 claims the name)
        if (txtManaCrystals == null)
        {
            var go = GameObject.Find("Txt_Gold");
            if (go != null)
            {
                go.name = "Txt_ManaCrystals";
                txtManaCrystals = go.GetComponent<TMP_Text>();
            }
        }
        if (txtManaCrystals == null)
        {
            var go = GameObject.Find("Txt_ManaCrystals");
            if (go != null) txtManaCrystals = go.GetComponent<TMP_Text>();
        }

        // Slot 3: rename Txt_Energy → Txt_Gold
        if (txtGold == null)
        {
            var go = GameObject.Find("Txt_Energy");
            if (go != null)
            {
                go.name = "Txt_Gold";
                txtGold = go.GetComponent<TMP_Text>();
            }
        }
        if (txtGold == null)
        {
            var go = GameObject.Find("Txt_Gold");
            if (go != null) txtGold = go.GetComponent<TMP_Text>();
        }

        // Destroy slot 5 (Txt_Essence) — completely remove it
        var essence = GameObject.Find("Txt_Essence");
        if (essence != null) Destroy(essence);
    }

    void DestroyLegacySlot(string goName)
    {
        var go = GameObject.Find(goName);
        if (go != null) Destroy(go);
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