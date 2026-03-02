using System;
using UnityEngine;

//
// ProfileData.cs
// Contains only data classes (Serializable) for saves.
// NO MonoBehaviours in here.
//

[Serializable]
public class CharacterData
{
    public string name;
    public int level;

    public GenderType gender;

    // Portrait feature variants (0-4 each)
    public int hairVariant;
    public int eyeVariant;
    public int mouthVariant;
    public int clothingVariant;
    public int specialVariant;

    public PlayerClass playerClass;

    public CharacterData(string name, int level, GenderType gender,
        int hairVariant, int eyeVariant, int mouthVariant, int clothingVariant, int specialVariant,
        PlayerClass pc)
    {
        this.name = name;
        this.level = level;
        this.gender = gender;
        this.hairVariant = hairVariant;
        this.eyeVariant = eyeVariant;
        this.mouthVariant = mouthVariant;
        this.clothingVariant = clothingVariant;
        this.specialVariant = specialVariant;
        this.playerClass = pc;
    }
}

[Serializable]
public class AccountSaveData
{
    // email or "GUEST"
    public string accountId = "";
    public bool isGuest = false;

    // Which slot is currently active/logged in
    public int activeSlotIndex = -1;

    // Exactly 3 slots (JsonUtility friendly)
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

        // If active slot was deleted -> reset
        if (activeSlotIndex == index)
            activeSlotIndex = -1;
    }

    public bool HasAnyCharacter()
    {
        return slot1 != null || slot2 != null || slot3 != null;
    }
}