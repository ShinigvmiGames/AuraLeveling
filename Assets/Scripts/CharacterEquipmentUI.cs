using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterEquipmentUI : MonoBehaviour
{
    [System.Serializable]
    public class EquipSlotUI
    {
        public EquipmentSlot slot;
        public Button button;
        public TMP_Text label;
        public Image icon;        // shows equipped item icon (auto-created if null)
        public Image placeholder;  // shows slot placeholder (e.g. sword silhouette)
        public Image glowEffect;  // quality glow behind icon (auto-created if null)
    }

    public EquipmentSystem equipment;
    public InventorySystem inventory;
    public ItemPopup popup;
    public EquipSlotUI[] slotUIs;

    void Start()
    {
        if (equipment == null) equipment = FindObjectOfType<EquipmentSystem>();
        if (inventory == null) inventory = FindObjectOfType<InventorySystem>();
        if (popup == null) popup = FindInactivePopup();
        if (equipment != null) equipment.onChanged += Refresh;
        if (inventory != null) inventory.onChanged += Refresh;

        EnsureSlotImages();
        SetupDragDrop();
        SetupClickHandlers();
        Refresh();
    }

    void OnDestroy()
    {
        if (equipment != null) equipment.onChanged -= Refresh;
        if (inventory != null) inventory.onChanged -= Refresh;
    }

    /// <summary>
    /// Auto-create icon and glow Images for slots that don't have them wired in Inspector.
    /// Also auto-find placeholder images (Img_Item children in the scene).
    /// </summary>
    void EnsureSlotImages()
    {
        if (slotUIs == null) return;
        foreach (var ui in slotUIs)
        {
            if (ui == null || ui.button == null) continue;

            // Auto-find placeholder image (Img_Item child) if not wired in Inspector
            if (ui.placeholder == null)
            {
                var phTF = ui.button.transform.Find("Img_Item");
                if (phTF != null)
                    ui.placeholder = phTF.GetComponent<Image>();
            }

            // Auto-create icon Image if not assigned
            if (ui.icon == null)
                ui.icon = CreateChildImage(ui.button.transform, "ItemIcon", false);

            // Auto-create glow Image if not assigned
            if (ui.glowEffect == null)
                ui.glowEffect = CreateGlowImage(ui.button.transform);
        }
    }

    void SetupDragDrop()
    {
        if (slotUIs == null) return;

        foreach (var ui in slotUIs)
        {
            if (ui == null || ui.button == null) continue;

            var go = ui.button.gameObject;

            var drag = go.GetComponent<DraggableItem>();
            if (drag == null) drag = go.AddComponent<DraggableItem>();
            drag.isEquipmentSlot = true;
            drag.equipSlot = ui.slot;

            var drop = go.GetComponent<DroppableSlot>();
            if (drop == null) drop = go.AddComponent<DroppableSlot>();
            drop.isEquipmentSlot = true;
            drop.equipSlot = ui.slot;
            drop.equipment = equipment;
            drop.inventory = inventory;
        }
    }

    /// <summary>
    /// When clicking an equipped slot that has an item, open the ItemPopup to show its details.
    /// </summary>
    void SetupClickHandlers()
    {
        if (slotUIs == null) return;

        foreach (var ui in slotUIs)
        {
            if (ui == null || ui.button == null) continue;

            var capturedSlot = ui.slot; // capture for lambda
            ui.button.onClick.RemoveAllListeners();
            ui.button.onClick.AddListener(() => OnSlotClicked(capturedSlot));
        }
    }

    void OnSlotClicked(EquipmentSlot slot)
    {
        if (equipment == null) return;

        ItemData item = equipment.GetEquipped(slot);
        if (item == null) return; // empty slot, nothing to show

        if (popup == null) popup = FindInactivePopup();
        if (popup == null) return;

        popup.ShowEquippedItem(slot);
    }

    ItemPopup FindInactivePopup()
    {
        var all = Resources.FindObjectsOfTypeAll<ItemPopup>();
        if (all == null || all.Length == 0) return null;

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.scene.IsValid())
                return all[i];
        }

        return all.Length > 0 ? all[0] : null;
    }

    public void Refresh()
    {
        if (equipment == null || slotUIs == null) return;

        foreach (var ui in slotUIs)
        {
            if (ui == null) continue;

            ItemData it = equipment.GetEquipped(ui.slot);
            bool hasItem = it != null;

            bool hasIcon = hasItem && it.icon != null;

            // Hide label text when item icon is visible
            if (ui.label != null)
            {
                if (hasIcon)
                    ui.label.text = "";
                else
                    ui.label.text = hasItem ? $"{ui.slot}\n{it.rarity}" : $"{ui.slot}\nleer";
            }

            if (ui.icon != null)
            {
                ui.icon.enabled = hasIcon;
                if (hasIcon)
                    ui.icon.sprite = it.icon;
            }

            // Disable placeholder completely when an item is equipped
            if (ui.placeholder != null)
            {
                ui.placeholder.enabled = !hasItem;
                ui.placeholder.gameObject.SetActive(!hasItem);
            }

            if (ui.glowEffect != null)
                QualityGlow.Apply(ui.glowEffect, hasItem ? it.quality : ItemQuality.Normal);

            var drag = ui.button != null ? ui.button.GetComponent<DraggableItem>() : null;
            if (drag != null)
                drag.hasItem = hasItem;
        }
    }

    Image CreateChildImage(Transform parent, string name, bool behindSiblings)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (behindSiblings)
            go.transform.SetAsFirstSibling();
        else
            go.transform.SetAsLastSibling();

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.1f);
        rt.anchorMax = new Vector2(0.9f, 0.9f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;
        img.enabled = false;
        return img;
    }

    Image CreateGlowImage(Transform parent)
    {
        var go = new GameObject("QualityGlow");
        go.transform.SetParent(parent, false);
        go.transform.SetAsFirstSibling();

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-10, -10);
        rt.offsetMax = new Vector2(10, 10);

        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.enabled = false;
        return img;
    }
}
