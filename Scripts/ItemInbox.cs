using System;
using System.Collections.Generic;
using UnityEngine;
public class ItemInbox : MonoBehaviour
{
    public List<ItemData> items = new List<ItemData>();
    public event Action OnInboxChanged;

    public void Add(ItemData item, string source = "")
    {
        if (item == null) return;
        items.Add(item);
        OnInboxChanged?.Invoke();
        if (!string.IsNullOrEmpty(source))
            Debug.Log($"ItemInbox: Added '{item.itemName}' from {source}");
    }

    public bool TryClaimToInventory(InventorySystem inv, int index)
    {
        if (inv == null) return false;
        if (index < 0 || index >= items.Count) return false;
        if (!inv.HasSpace(1)) return false;
        var item = items[index];
        items.RemoveAt(index);
        inv.TryAdd(item);
        OnInboxChanged?.Invoke();
        return true;
    }

    public void ClaimAllToInventory(InventorySystem inv)
    {
        if (inv == null) return;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (!inv.HasSpace(1)) break;
            var item = items[i];
            items.RemoveAt(i);
            inv.TryAdd(item);
        }
        OnInboxChanged?.Invoke();
    }
}