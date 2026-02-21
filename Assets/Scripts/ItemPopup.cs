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

    [Header("Worth (eigenes TMP + Coin Icon als Children)")]
    public TMP_Text worthText; // zeigt "Worth: 123" — eigenes GO damit Coin-Icon daneben passt

    [Header("Rarity / Quality Images")]
    public Image rarityImage;   // zeigt das Rarity-Icon (ERank, Common, Rare, etc.)
    public Image qualityImage;  // zeigt das Quality-Icon (Normal, Epic, Legendary)

    [Header("Rarity Sprites (Inspector: ERank..AURAFARMING, 12 Stück)")]
    public Sprite[] raritySprites; // Index = (int)ItemRarity

    [Header("Quality Sprites (Inspector: Normal, Epic, Legendary, 3 Stück)")]
    public Sprite[] qualitySprites; // Index = (int)ItemQuality

    [Header("Glow Effect")]
    public Image glowImage; // Glow-Effekt Image (wie bei Equipment/Inventory)

    [Header("Glow Colors per Rarity")]
    public Color glowERank      = new Color(0.6f, 0.6f, 0.6f, 0.4f);
    public Color glowCommon     = new Color(0.8f, 0.8f, 0.8f, 0.5f);
    public Color glowDRank      = new Color(0.4f, 0.8f, 0.4f, 0.5f);
    public Color glowCRank      = new Color(0.3f, 0.7f, 1.0f, 0.5f);
    public Color glowRare       = new Color(0.3f, 0.4f, 1.0f, 0.6f);
    public Color glowBRank      = new Color(0.7f, 0.3f, 1.0f, 0.6f);
    public Color glowHero       = new Color(1.0f, 0.5f, 0.0f, 0.6f);
    public Color glowARank      = new Color(1.0f, 0.3f, 0.3f, 0.7f);
    public Color glowSRank      = new Color(1.0f, 0.85f, 0.0f, 0.7f);
    public Color glowMonarch    = new Color(1.0f, 0.0f, 0.5f, 0.8f);
    public Color glowGodlike    = new Color(0.9f, 0.0f, 0.9f, 0.8f);
    public Color glowAURAFARMING = new Color(1.0f, 1.0f, 1.0f, 0.9f);

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

        // Title: nur Item-Name + Slot + Level (Rarity/Quality werden jetzt als Images angezeigt)
        if (titleText != null)
        {
            string name = !string.IsNullOrEmpty(item.itemName) ? item.itemName : item.slot.ToString();
            titleText.text = $"{name}\n<size=70%>{item.slot}  Lv {item.itemLevel}</size>";
        }

        // Rarity Image
        if (rarityImage != null)
        {
            int rarityIdx = (int)item.rarity;
            if (raritySprites != null && rarityIdx >= 0 && rarityIdx < raritySprites.Length && raritySprites[rarityIdx] != null)
            {
                rarityImage.sprite = raritySprites[rarityIdx];
                rarityImage.enabled = true;
            }
            else
            {
                rarityImage.enabled = false;
            }
        }

        // Quality Image
        if (qualityImage != null)
        {
            int qualityIdx = (int)item.quality;
            if (qualitySprites != null && qualityIdx >= 0 && qualityIdx < qualitySprites.Length && qualitySprites[qualityIdx] != null)
            {
                qualityImage.sprite = qualitySprites[qualityIdx];
                qualityImage.enabled = true;
            }
            else
            {
                qualityImage.enabled = false;
            }
        }

        // Glow Effect (wie bei Equipment/Inventory)
        if (glowImage != null)
        {
            glowImage.enabled = true;
            glowImage.color = GetGlowColor(item.rarity);
        }

        // Stats
        if (statsText != null)
        {
            string stats = "";

            // Main stats
            if (item.bonusSTR > 0) stats += $"STR +{item.bonusSTR}\n";
            if (item.bonusDEX > 0) stats += $"DEX +{item.bonusDEX}\n";
            if (item.bonusINT > 0) stats += $"INT +{item.bonusINT}\n";
            if (item.bonusVIT > 0) stats += $"VIT +{item.bonusVIT}\n";

            // Weapon Damage (colored red)
            if (item.weaponDamageMin > 0 || item.weaponDamageMax > 0)
            {
                if (item.weaponDamageMin == item.weaponDamageMax)
                    stats += $"<color=#FF6B6B>DMG +{item.weaponDamageMin}</color>\n";
                else
                    stats += $"<color=#FF6B6B>DMG +{item.weaponDamageMin}-{item.weaponDamageMax}</color>\n";
            }
            if (item.armor > 0)
                stats += $"<color=#6BAFFF>Armor +{item.armor}</color>\n";
            if (item.critRate > 0f)
                stats += $"<color=#FFD700>Crit Rate +{item.critRate:0.0}%</color>\n";
            if (item.critDamage > 0f)
                stats += $"<color=#FF9500>Crit DMG +{item.critDamage:0.0}%</color>\n";
            if (item.speed > 0f)
                stats += $"<color=#4CFF4C>Speed +{item.speed:0.0}</color>\n";

            if (item.auraBonusPercent > 0f)
                stats += $"Aura Bonus: +{item.auraBonusPercent:0.0}%\n";

            stats += $"\nItem Aura: {item.itemAura}";

            statsText.text = stats;
        }

        // Worth (eigenes TMP Feld, damit Coin Icon als Child daneben steht)
        if (worthText != null)
        {
            worthText.text = $"Worth: {item.sellPrice}";
        }
    }

    Color GetGlowColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.ERank:       return glowERank;
            case ItemRarity.Common:      return glowCommon;
            case ItemRarity.DRank:       return glowDRank;
            case ItemRarity.CRank:       return glowCRank;
            case ItemRarity.Rare:        return glowRare;
            case ItemRarity.BRank:       return glowBRank;
            case ItemRarity.Hero:        return glowHero;
            case ItemRarity.ARank:       return glowARank;
            case ItemRarity.SRank:       return glowSRank;
            case ItemRarity.Monarch:     return glowMonarch;
            case ItemRarity.Godlike:     return glowGodlike;
            case ItemRarity.AURAFARMING: return glowAURAFARMING;
            default:                     return glowCommon;
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
