using System;
using UnityEngine;

//
// ProfileData.cs
// Enthält nur Datenklassen (Serializable) für Saves.
// KEINE MonoBehaviours hier rein.
//

[Serializable]
public class CharacterData
{
    public string name;
    public int level;

    public RaceType race;
    public GenderType gender;
    public int modelIndex;

    public PlayerClass playerClass;

    public CharacterData(string name, int level, RaceType race, GenderType gender, int modelIndex, PlayerClass pc)
    {
        this.name = name;
        this.level = level;
        this.race = race;
        this.gender = gender;
        this.modelIndex = modelIndex;
        this.playerClass = pc;
    }
}

[Serializable]
public class AccountSaveData
{
    // email oder "GUEST"
    public string accountId = "";
    public bool isGuest = false;

    // Welcher Slot ist aktuell aktiv/eingeloggt
    public int activeSlotIndex = -1;

    // Genau 3 Slots (JsonUtility freundlich)
    public CharacterData slot1;
    public CharacterData slot2;
    public CharacterData slot3;

    public CharacterData GetSlot(int index)
    {
        switch (index)
        {
            case 0: return slot1;
            case 1: return slot2;
            case 2: return slot3;
            default: return null;
        }
    }

    public void SetSlot(int index, CharacterData data)
    {
        switch (index)
        {
            case 0: slot1 = data; break;
            case 1: slot2 = data; break;
            case 2: slot3 = data; break;
        }
    }

    public void ClearSlot(int index)
    {
        SetSlot(index, null);

        // Wenn aktiver Slot gelöscht wurde -> reset
        if (activeSlotIndex == index)
            activeSlotIndex = -1;
    }

    public bool HasAnyCharacter()
    {
        return slot1 != null || slot2 != null || slot3 != null;
    }
}