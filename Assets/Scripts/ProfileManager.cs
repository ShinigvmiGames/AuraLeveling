using UnityEngine;
public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance;

    [Header("Runtime")]
    public AccountSaveData current = new AccountSaveData();

    [Header("Flow")]
    public int pendingCreateSlotIndex = -1;

    const string KEY_LAST_ACCOUNT = "PM_LAST_ACCOUNT";
    const string KEY_LAST_GUEST = "PM_LAST_GUEST";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadLastAccountIfAny();
    }

    // =========================================================
    // ACCOUNT
    // =========================================================

    void LoadLastAccountIfAny()
    {
        if (!PlayerPrefs.HasKey(KEY_LAST_ACCOUNT))
            return;

        string acc = PlayerPrefs.GetString(KEY_LAST_ACCOUNT, "");
        bool guest = PlayerPrefs.GetInt(KEY_LAST_GUEST, 0) == 1;

        if (!string.IsNullOrEmpty(acc))
            SetAccount(acc, guest);
    }

    public void SetAccount(string accountId, bool isGuest)
    {
        current.accountId = accountId;
        current.isGuest = isGuest;

        PlayerPrefs.SetString(KEY_LAST_ACCOUNT, accountId);
        PlayerPrefs.SetInt(KEY_LAST_GUEST, isGuest ? 1 : 0);
        PlayerPrefs.Save();

        Load();
    }

    string SaveKey()
    {
        string id = string.IsNullOrEmpty(current.accountId)
            ? "UNKNOWN"
            : current.accountId.ToLowerInvariant();

        return "ACC_SAVE_" + id;
    }

    public void Load()
    {
        string key = SaveKey();

        if (!PlayerPrefs.HasKey(key))
        {
            // neuer Account
            current.activeSlotIndex = -1;
            current.slot1 = null;
            current.slot2 = null;
            current.slot3 = null;
            Save();
            return;
        }

        string json = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(json))
            return;

        current = JsonUtility.FromJson<AccountSaveData>(json);
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(current);
        PlayerPrefs.SetString(SaveKey(), json);
        PlayerPrefs.Save();
    }

    // =========================================================
    // SLOT ACCESS
    // =========================================================

    public CharacterData GetSlot(int index)
    {
        return current.GetSlot(index);
    }

    public void CreateOrReplaceCharacter(int slotIndex, CharacterData data)
    {
        current.SetSlot(slotIndex, data);
        current.activeSlotIndex = slotIndex;
        Save();
    }

    public void DeleteCharacter(int slotIndex)
    {
        current.ClearSlot(slotIndex);

        if (current.activeSlotIndex == slotIndex)
            current.activeSlotIndex = -1;

        Save();
    }

    public void SetActiveSlot(int slotIndex)
    {
        current.activeSlotIndex = slotIndex;
        Save();
    }

    public CharacterData GetActiveCharacter()
    {
        if (current.activeSlotIndex < 0)
            return null;

        return GetSlot(current.activeSlotIndex);
    }

    public bool HasCharacterInSlot(int slotIndex)
    {
        return GetSlot(slotIndex) != null;
    }

    // =========================================================
    // NAME CHECK
    // =========================================================

    public bool IsNameAvailable(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        name = name.Trim().ToLowerInvariant();

        for (int i = 0; i < 3; i++)
        {
            var slot = GetSlot(i);

            if (slot != null &&
                slot.name.Trim().ToLowerInvariant() == name)
            {
                return false;
            }
        }

        return true;
    }

    // =========================================================
    // SLOT FINDING
    // =========================================================

    public int GetHighestAvailableSlotIndex()
    {
        // 2 → 1 → 0
        for (int i = 2; i >= 0; i--)
        {
            if (GetSlot(i) == null)
                return i;
        }

        return -1; // all full
    }
}