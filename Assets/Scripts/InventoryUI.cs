using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [System.Serializable]
    public class InventorySlotUI
    {
        public Button button;
        public TMP_Text label;
        public Image icon; // shows item icon (assign in Inspector)
        public Image glowEffect; // quality glow behind icon (assign in Inspector)
    }

    public InventorySystem inventory;
    public EquipmentSystem equipment;
    public ItemPopup popup;

    public InventorySlotUI[] slots; // size 10

    void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<InventorySystem>();
        if (equipment == null) equipment = FindObjectOfType<EquipmentSystem>();
        if (popup == null) popup = FindObjectOfType<ItemPopup>();

        if (inventory != null)
            inventory.onChanged += Refresh;

        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;
            if (slots[i].button != null)
                slots[i].button.onClick.AddListener(() => OnClickSlot(index));
        }

        SetupDragDrop();
        Refresh();
    }

    void OnDestroy()
    {
        if (inventory != null) inventory.onChanged -= Refresh;
    }

    void SetupDragDrop()
    {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            var ui = slots[i];
            if (ui == null || ui.button == null) continue;

            var go = ui.button.gameObject;

            var drag = go.GetComponent<DraggableItem>();
            if (drag == null) drag = go.AddComponent<DraggableItem>();
            drag.isEquipmentSlot = false;
            drag.inventoryIndex = i;

            var drop = go.GetComponent<DroppableSlot>();
            if (drop == null) drop = go.AddComponent<DroppableSlot>();
            drop.isEquipmentSlot = false;
            drop.inventoryIndex = i;
            drop.equipment = equipment;
            drop.inventory = inventory;
        }
    }

    void OnClickSlot(int index)
    {
        if (inventory == null || popup == null) return;
        if (index >= inventory.Count) return;

        popup.ShowInventoryIndex(index);
    }

    public void Refresh()
    {
        if (inventory == null || slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            var ui = slots[i];
            bool has = (i < inventory.Count);
            var item = has ? inventory.Get(i) : null;
            bool hasIcon = has && item != null && item.icon != null;

            if (ui.label != null)
            {
                if (hasIcon)
                    ui.label.text = "";
                else if (has && item != null)
                    ui.label.text = $"{item.rarity}\n{item.slot}";
                else
                    ui.label.text = "";
            }

            if (ui.icon != null)
            {
                ui.icon.enabled = hasIcon;
                if (hasIcon)
                    ui.icon.sprite = item.icon;
            }

            if (ui.glowEffect != null)
                QualityGlow.Apply(ui.glowEffect, has && item != null ? item.quality : ItemQuality.Normal);

            if (ui.button != null)
                ui.button.interactable = has;

            var drag = ui.button != null ? ui.button.GetComponent<DraggableItem>() : null;
            if (drag != null)
            {
                drag.hasItem = has;
                drag.inventoryIndex = i;
            }
        }
    }

}
