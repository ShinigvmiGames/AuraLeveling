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
        public Image icon; // shows item icon (auto-created if null)
        public Image glowEffect; // quality glow behind icon (auto-created if null)
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

        EnsureSlotImages();
        SetupDragDrop();
        Refresh();
    }

    void OnDestroy()
    {
        if (inventory != null) inventory.onChanged -= Refresh;
    }

    /// <summary>
    /// Auto-create icon and glow Images for slots that don't have them wired in Inspector.
    /// </summary>
    void EnsureSlotImages()
    {
        if (slots == null) return;
        foreach (var ui in slots)
        {
            if (ui == null || ui.button == null) continue;

            if (ui.icon == null)
                ui.icon = CreateChildImage(ui.button.transform, "ItemIcon");

            if (ui.glowEffect == null)
                ui.glowEffect = CreateGlowImage(ui.button.transform);
        }
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

    Image CreateChildImage(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
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
