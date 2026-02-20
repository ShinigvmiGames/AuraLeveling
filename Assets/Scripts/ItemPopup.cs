using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ItemPopup : MonoBehaviour
{
    [Header("UI")]
    public Image itemIcon;
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
    ItemData currentItem = null; // reference to the displayed item (for safe removal)
    EquipmentSlot currentEquipSlot;
    bool showingEquipped = false;
    bool bound = false;
    bool actionInProgress = false; // prevents double-tap duplication

    // Auto-created sell price icon
    Image sellGoldIcon;

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

        // Auto-create itemIcon if not wired in Inspector
        // Large centered icon in the popup (positioned towards center, bigger)
        if (itemIcon == null && titleText != null)
        {
            var go = new GameObject("ItemIcon_Auto");
            go.transform.SetParent(titleText.transform.parent, false);
            go.transform.SetAsFirstSibling();

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 20); // more centered (lower)
            rt.sizeDelta = new Vector2(260, 260); // bigger icon

            itemIcon = go.AddComponent<Image>();
            itemIcon.raycastTarget = false;
            itemIcon.preserveAspect = true;
            itemIcon.enabled = false;

            // Apply MAT_AdditiveGlow material for better visuals
            var mats = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var m in mats)
            {
                if (m.name == "MAT_AdditiveGlow")
                {
                    itemIcon.material = m;
                    break;
                }
            }
        }

        // Auto-create sell price gold icon next to statsText
        if (sellGoldIcon == null && statsText != null)
        {
            Sprite goldSprite = CurrencyIcons.Gold;
            if (goldSprite != null)
            {
                var go = new GameObject("SellGoldIcon");
                go.transform.SetParent(statsText.transform, false);
                go.layer = 5;

                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(30, 12);
                rt.sizeDelta = new Vector2(28, 28);

                sellGoldIcon = go.AddComponent<Image>();
                sellGoldIcon.sprite = goldSprite;
                sellGoldIcon.preserveAspect = true;
                sellGoldIcon.raycastTarget = false;
                sellGoldIcon.enabled = false;
            }
        }
    }

    void BindButtonsOnce()
    {
        if (bound) return;

        if (equipButton != null) equipButton.onClick.AddListener(OnEquipButtonPressed);
        if (sellButton != null) sellButton.onClick.AddListener(OnSellButtonPressed);
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

        showingEquipped = false;
        currentInventoryIndex = index;
        currentItem = item;
        actionInProgress = false;
        ApplyText(item);

        // Restore button labels for inventory mode
        if (equipButton != null)
        {
            var btnText = equipButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = "Equip";
        }
        if (sellButton != null)
            sellButton.gameObject.SetActive(true);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    /// <summary>
    /// Show an equipped item's details. Equip button becomes "Unequip".
    /// </summary>
    public void ShowEquippedItem(EquipmentSlot slot)
    {
        EnsureRefs();

        if (equipment == null)
        {
            Debug.LogError("ItemPopup: EquipmentSystem not found.");
            return;
        }

        ItemData item = equipment.GetEquipped(slot);
        if (item == null)
        {
            Debug.LogWarning($"ItemPopup: No item equipped in {slot}.");
            return;
        }

        showingEquipped = true;
        currentEquipSlot = slot;
        currentInventoryIndex = -1;
        currentItem = item;
        actionInProgress = false;
        ApplyText(item);

        // Change button labels for equipped item mode
        if (equipButton != null)
        {
            var btnText = equipButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = "Unequip";
        }

        // Hide sell button for equipped items (must unequip first)
        if (sellButton != null)
            sellButton.gameObject.SetActive(false);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    void ApplyText(ItemData item)
    {
        // Icon
        if (itemIcon != null)
        {
            itemIcon.enabled = item.icon != null;
            if (item.icon != null)
                itemIcon.sprite = item.icon;
        }

        // Title: "Shadowfang" or fallback to slot name
        // Subtitle line: Quality + Rarity + Slot + Level
        if (titleText != null)
        {
            string name = !string.IsNullOrEmpty(item.itemName) ? item.itemName : item.slot.ToString();
            string qualityTag = item.quality != ItemQuality.Normal ? $"[{item.quality}] " : "";
            titleText.text = $"{qualityTag}{name}\n<size=70%>{item.rarity} {item.slot}  Lv {item.itemLevel}</size>";
        }

        // Stats
        if (statsText != null)
        {
            string stats = "";

            if (item.bonusSTR > 0) stats += $"STR +{item.bonusSTR}\n";
            if (item.bonusDEX > 0) stats += $"DEX +{item.bonusDEX}\n";
            if (item.bonusINT > 0) stats += $"INT +{item.bonusINT}\n";
            if (item.bonusVIT > 0) stats += $"VIT +{item.bonusVIT}\n";

            if (item.auraBonusPercent > 0f)
                stats += $"Aura Bonus: +{item.auraBonusPercent:0.0}%\n";

            stats += $"\nItem Aura: {item.itemAura}";
            stats += $"\nSell:       {item.sellPrice}";

            statsText.text = stats;

            // Show gold icon next to sell price
            if (sellGoldIcon != null)
                sellGoldIcon.enabled = true;
        }
    }

    void OnEquipButtonPressed()
    {
        if (actionInProgress) return; // prevent double-tap
        actionInProgress = true;

        if (showingEquipped)
            UnequipItem();
        else
            EquipFromInventory();
    }

    void OnSellButtonPressed()
    {
        if (actionInProgress) return; // prevent double-tap
        if (showingEquipped) return; // can't sell equipped items directly
        actionInProgress = true;
        SellFromInventory();
    }

    void EquipFromInventory()
    {
        EnsureRefs();
        if (inventory == null || equipment == null) return;
        if (currentItem == null) return;

        // Use reference-based removal to prevent stale-index duplication.
        // The index might be invalid if inventory changed since popup opened.
        bool removed = inventory.Remove(currentItem);
        if (!removed)
        {
            Debug.LogWarning("ItemPopup: Item not found in inventory (already removed?).");
            Close();
            return;
        }

        ItemData replaced = equipment.Equip(currentItem);

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

    void UnequipItem()
    {
        EnsureRefs();
        if (equipment == null || inventory == null) return;

        ItemData removed = equipment.Unequip(currentEquipSlot);
        if (removed == null) return;

        bool added = inventory.Add(removed);
        if (!added)
        {
            // Inventory full - re-equip it
            equipment.Equip(removed);
            Debug.LogWarning("Inventory full - cannot unequip.");
            return;
        }

        Close();
    }

    void SellFromInventory()
    {
        EnsureRefs();
        if (inventory == null || player == null) return;
        if (currentItem == null) return;

        // Use reference-based removal to prevent stale-index issues
        bool removed = inventory.Remove(currentItem);
        if (!removed)
        {
            Debug.LogWarning("ItemPopup: Item not found in inventory for sell (already removed?).");
            Close();
            return;
        }

        player.gold += currentItem.sellPrice;
        Close();
    }

    void Close()
    {
        currentInventoryIndex = -1;
        currentItem = null;
        actionInProgress = false;
        gameObject.SetActive(false);
    }
}