using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class AnvilUI : MonoBehaviour
{
    [Header("Refs")]
    public InventorySystem inventory;
    public AnvilSystem anvilSystem;
    public PlayerStats player;
    public ItemPopup popup;

    [Header("UI")]
    public Button craftButton;
    public TMP_Text essenceText;

    [Header("Craft Timing")]
    public float minCraftTime = 0.5f;
    public float maxCraftTime = 1.0f;

    bool crafting = false;

    void Awake()
    {
        ResolveRefs();
    }

    void Start()
    {
        if (craftButton != null)
        {
            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(OnCraftPressed);
        }

        RefreshUI();
    }

    void Update()
    {
        RefreshUI();
    }

    void ResolveRefs()
    {
        if (player == null) player = FindObjectOfType<PlayerStats>();
        if (inventory == null) inventory = FindObjectOfType<InventorySystem>();
        if (anvilSystem == null) anvilSystem = FindObjectOfType<AnvilSystem>();
        if (popup == null) popup = FindInactivePopup();
    }

    void RefreshUI()
    {
        // Refs nachziehen falls Szene neu geladen / Objekte neu instanziiert
        if (player == null || inventory == null || anvilSystem == null || popup == null)
            ResolveRefs();

        if (essenceText != null && player != null)
            essenceText.text = $"{player.shadowEssence}";

        if (craftButton != null && player != null)
            craftButton.interactable = !crafting && player.shadowEssence >= 1;
    }

    public void OnCraftPressed()
    {
        if (crafting) return;
        StartCoroutine(CraftFlow());
    }

    IEnumerator CraftFlow()
    {
        crafting = true;

        // Safety refs
        ResolveRefs();

        // Wenn etwas Grundlegendes fehlt -> abbrechen ohne Nebenwirkungen
        if (player == null || inventory == null || anvilSystem == null || popup == null)
        {
            Debug.LogError("AnvilUI: Missing refs! (player/inventory/anvilSystem/popup)");
            crafting = false;
            yield break;
        }

        // Wenn nicht genug Essenz -> abbrechen
        if (player.shadowEssence < 1)
        {
            crafting = false;
            yield break;
        }

        // Craft Dauer (nur “magisches Laden”)
        float duration = Random.Range(minCraftTime, maxCraftTime);
        yield return new WaitForSeconds(duration);

        // Item craften (AnvilSystem zieht Essenz intern ab)
        ItemData crafted = anvilSystem.CraftItemInstant();


        crafting = false;

        if (crafted == null)
        {
            Debug.Log("AnvilUI: crafted item is null (maybe not enough essence?)");
            yield break;
        }

        // Ins Inventar
        bool added = inventory.Add(crafted);
        if (!added)
        {
            Debug.Log("Inventory full → crafted item auto-sold.");
            player.gold += crafted.sellPrice;
            yield break;
        }

        // Popup: letztes Item anzeigen
        int newIndex = inventory.Count - 1;
        popup.ShowInventoryIndex(newIndex);
    }

    ItemPopup FindInactivePopup()
    {
        // Findet auch disabled Objects
        var all = Resources.FindObjectsOfTypeAll<ItemPopup>();
        if (all == null || all.Length == 0) return null;

        // Nimm das erste, das wirklich in einer Scene liegt (nicht Prefab-Asset)
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.scene.IsValid())
                return all[i];
        }

        return all[0];
    }
}