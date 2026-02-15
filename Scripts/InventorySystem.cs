using System.Collections.Generic;
using UnityEngine;
public class InventorySystem : MonoBehaviour
{
public int capacity = 10;

public List<ItemData> items = new List<ItemData>();

public System.Action onChanged;

public int Count => items.Count;

public ItemData Get(int index)
{
if (index < 0 || index >= items.Count) return null;
return items[index];
}

public bool Add(ItemData item)
{
if (item == null) return false;
if (items.Count >= capacity)
{
Debug.Log("Inventar voll!");
return false;
}

items.Add(item);
onChanged?.Invoke();
return true;
}

public ItemData RemoveAt(int index)
{
if (index < 0 || index >= items.Count) return null;
ItemData it = items[index];
items.RemoveAt(index);
onChanged?.Invoke();
return it;
}

public bool Remove(ItemData item)
{
if (item == null) return false;
bool ok = items.Remove(item);
if (ok) onChanged?.Invoke();
return ok;
}

public bool HasSpace(int amount = 1)
{
    return items.Count + amount <= capacity;
}

public bool TryAdd(ItemData item)
{
    return Add(item);
}
}