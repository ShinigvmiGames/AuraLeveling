using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to any slot that can receive a dragged item.
/// Handles all swap logic between inventory and equipment.
/// </summary>
public class DroppableSlot : MonoBehaviour, IDropHandler
{
    [HideInInspector] public bool isEquipmentSlot = false;
    [HideInInspector] public EquipmentSlot equipSlot;
    [HideInInspector] public int inventoryIndex = -1;

    // Systems (set by the owning UI)
    [HideInInspector] public EquipmentSystem equipment;
    [HideInInspector] public InventorySystem inventory;

    public void OnDrop(PointerEventData eventData)
    {
        var dragged = DraggableItem.GetCurrentlyDragging();
        if (dragged == null || !dragged.hasItem) return;
        if (equipment == null || inventory == null) return;

        // Determine source
        bool srcIsEquip = dragged.isEquipmentSlot;
        EquipmentSlot srcEquipSlot = dragged.equipSlot;
        int srcInvIndex = dragged.inventoryIndex;

        // Determine target
        bool dstIsEquip = isEquipmentSlot;
        EquipmentSlot dstEquipSlot = equipSlot;
        int dstInvIndex = inventoryIndex;

        // === Inventory -> Equipment ===
        if (!srcIsEquip && dstIsEquip)
        {
            ItemData item = inventory.Get(srcInvIndex);
            if (item == null) return;

            // Item must match the equipment slot
            if (item.slot != dstEquipSlot) return;

            inventory.RemoveAt(srcInvIndex);
            ItemData replaced = equipment.Equip(item);

            if (replaced != null)
                inventory.Add(replaced);

            return;
        }

        // === Equipment -> Inventory ===
        if (srcIsEquip && !dstIsEquip)
        {
            if (!inventory.HasSpace()) return;

            ItemData removed = equipment.Unequip(srcEquipSlot);
            if (removed == null) return;

            // If target inventory slot has an item and it fits the source equip slot, swap
            ItemData targetItem = inventory.Get(dstInvIndex);
            if (targetItem != null && targetItem.slot == srcEquipSlot)
            {
                inventory.RemoveAt(dstInvIndex);
                inventory.Add(removed);
                equipment.Equip(targetItem);
            }
            else
            {
                inventory.Add(removed);
            }

            return;
        }

        // === Inventory -> Inventory (swap positions) ===
        if (!srcIsEquip && !dstIsEquip)
        {
            if (srcInvIndex == dstInvIndex) return;
            inventory.Swap(srcInvIndex, dstInvIndex);
            return;
        }

        // === Equipment -> Equipment ===
        // Only swap if both slots have items and they fit each other's slot type
        if (srcIsEquip && dstIsEquip)
        {
            if (srcEquipSlot == dstEquipSlot) return; // same slot, nothing to do

            ItemData srcItem = equipment.GetEquipped(srcEquipSlot);
            ItemData dstItem = equipment.GetEquipped(dstEquipSlot);
            if (srcItem == null) return;

            // Both items must fit the other's slot for a swap
            if (dstItem != null && srcItem.slot == dstEquipSlot && dstItem.slot == srcEquipSlot)
            {
                equipment.Unequip(srcEquipSlot);
                equipment.Unequip(dstEquipSlot);
                equipment.Equip(srcItem); // goes to dstEquipSlot because item.slot matches
                equipment.Equip(dstItem); // goes to srcEquipSlot because item.slot matches
            }
        }
    }
}
