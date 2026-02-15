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
}

public InventorySystem inventory;
public ItemPopup popup;

public InventorySlotUI[] slots; // size 10

void Start()
{
if (inventory == null) inventory = FindObjectOfType<InventorySystem>();
if (popup == null) popup = FindObjectOfType<ItemPopup>();

if (inventory != null)
inventory.onChanged += Refresh;

// bind buttons
for (int i = 0; i < slots.Length; i++)
{
int index = i;
if (slots[i].button != null)
slots[i].button.onClick.AddListener(() => OnClickSlot(index));
}

Refresh();
}

void OnDestroy()
{
if (inventory != null) inventory.onChanged -= Refresh;
}

void OnClickSlot(int index)
{
if (inventory == null || popup == null) return;
if (index >= inventory.Count) return; // empty slot

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

if (ui.label != null)
ui.label.text = has ? $"{item.rarity}n{item.slot}" : "leer";

if (ui.button != null)
ui.button.interactable = has;
}
}
}