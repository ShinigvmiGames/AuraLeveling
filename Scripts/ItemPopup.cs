using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ItemPopup : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text statsText;
    public Button equipButton;
    public Button sellButton;
    public Button closeButton;

    [Header("Systems")]
    public PlayerStats player;
    public EquipmentSystem equipment;
    public InventorySystem inventory;

    int currentInventoryIndex = -1;
    bool bound = false;

    void Awake()
    {
        EnsureRefs();
        BindButtonsOnce();

        // WICHTIG:
        // NICHT hier oder in Start deaktivieren, wenn du später per Code öffnen willst.
        // Lass den Default-State im Inspector entscheiden (meist: disabled).
    }

    void OnEnable()
    {
        // falls Scene-Wechsel / Objects neu geladen werden
        EnsureRefs();
        BindButtonsOnce();
    }

    void EnsureRefs()
    {
        if (player == null) player = FindObjectOfType<PlayerStats>();
        if (equipment == null) equipment = FindObjectOfType<EquipmentSystem>();
        if (inventory == null) inventory = FindObjectOfType<InventorySystem>();
    }

    void BindButtonsOnce()
    {
        if (bound) return;

        if (equipButton != null) equipButton.onClick.AddListener(EquipFromInventory);
        if (sellButton != null) sellButton.onClick.AddListener(SellFromInventory);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        bound = true;
    }

    public void ShowInventoryIndex(int index)
    {
        EnsureRefs();

        if (inventory == null)
        {
            Debug.LogError("ItemPopup: InventorySystem not found.");
            return;
        }

        ItemData item = inventory.Get(index);
        if (item == null)
        {
            Debug.LogWarning($"ItemPopup: No item at index {index}.");
            return;
        }

        currentInventoryIndex = index;
        ApplyText(item);

        // Jetzt wirklich anzeigen:
        gameObject.SetActive(true);
        transform.SetAsLastSibling(); // falls es hinter anderen Panels hängt
    }

    void ApplyText(ItemData item)
    {
        if (titleText != null)
            titleText.text = $"{item.rarity} {item.slot} (Lv {item.itemLevel})";

        if (statsText != null)
        {
            statsText.text =
                $"STR +{item.bonusSTR}\n" +
                $"DEX +{item.bonusDEX}\n" +
                $"INT +{item.bonusINT}\n" +
                $"VIT +{item.bonusVIT}\n" +
                $"Aura Bonus: {item.auraBonusPercent:0.0}%\n" +
                $"Item Aura: {item.itemAura}\n" +
                $"Sell: {item.sellPrice} Gold";
        }
    }

    void EquipFromInventory()
    {
        EnsureRefs();
        if (inventory == null || equipment == null) return;
        if (currentInventoryIndex < 0) return;

        ItemData item = inventory.RemoveAt(currentInventoryIndex);
        if (item == null) return;

        ItemData replaced = equipment.Equip(item);

        if (replaced != null)
        {
            bool added = inventory.Add(replaced);
            if (!added)
            {
                if (player != null) player.gold += replaced.sellPrice;
                Debug.Log("Inventory full → replaced item auto-sold.");
            }
        }

        Close();
    }

    void SellFromInventory()
    {
        EnsureRefs();
        if (inventory == null || player == null) return;
        if (currentInventoryIndex < 0) return;

        ItemData item = inventory.RemoveAt(currentInventoryIndex);
        if (item == null) return;

        player.gold += item.sellPrice;
        Close();
    }

    void Close()
    {
        currentInventoryIndex = -1;
        gameObject.SetActive(false);
    }
}