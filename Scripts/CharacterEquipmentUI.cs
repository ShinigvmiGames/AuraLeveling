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
}

public EquipmentSystem equipment;
public EquipSlotUI[] slotUIs;

void Start()
{
if (equipment == null) equipment = FindObjectOfType<EquipmentSystem>();
if (equipment != null) equipment.onChanged += Refresh;

Refresh();
}

void OnDestroy()
{
if (equipment != null) equipment.onChanged -= Refresh;
}

public void Refresh()
{
if (equipment == null || slotUIs == null) return;

foreach (var ui in slotUIs)
{
if (ui == null) continue;

ItemData it = equipment.GetEquipped(ui.slot);
if (ui.label != null)
ui.label.text = it == null ? $"{ui.slot}nleer" : $"{ui.slot}n{it.rarity}";
}
}
}