using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public Image imgCharacterPortrait; // Img_CharacterPortrait
        public TMP_Text txtNameLvl;        // Txt_NameLvl
        public GameObject imgAddPlus;      // Img_AddPlus (GameObject)
        public Button btnClickArea;        // Btn_ClickArea
        public Button btnDelete;           // Btn_Delete
    }

    [Header("Slots (size = 3)")]
    public SlotUI[] slots = new SlotUI[3];

    [Header("Scene Flow")]
    public string sceneAuth = "Auth";
    public string sceneCreateCharacter = "CreateCharacter";
    public string sceneMain = "Main";

    [Header("Buttons")]
    public Button btnBack;

    [Header("Delete Confirm")]
    public GameObject imgDeleteConfirm; // Img_DeleteConfirm (Panel)
    public Button btnNo;
    public Button btnYes;

    int pendingDeleteIndex = -1;

    void Start()
    {
        // Safety
        if (ProfileManager.Instance == null)
        {
            var go = new GameObject("ProfileManager");
            go.AddComponent<ProfileManager>();
        }

        if (btnBack) btnBack.onClick.AddListener(BackToAuth);

        if (btnNo) btnNo.onClick.AddListener(HideDeleteConfirm);
        if (btnYes) btnYes.onClick.AddListener(ConfirmDelete);

        // Bind slot buttons
        for (int i = 0; i < slots.Length; i++)
        {
            int idx = i;

            if (slots[idx].btnClickArea != null)
            {
                slots[idx].btnClickArea.onClick.RemoveAllListeners();
                slots[idx].btnClickArea.onClick.AddListener(() => OnSlotClicked(idx));
            }

            if (slots[idx].btnDelete != null)
            {
                slots[idx].btnDelete.onClick.RemoveAllListeners();
                slots[idx].btnDelete.onClick.AddListener(() => OnDeleteClicked(idx));
            }
        }

        HideDeleteConfirm();
        RefreshAll();
    }

    void RefreshAll()
    {
        for (int i = 0; i < slots.Length; i++)
            RefreshSlot(i);
    }

    void RefreshSlot(int index)
{
    var pm = ProfileManager.Instance;
    var data = (pm != null) ? pm.GetSlot(index) : null;

    bool occupied = (data != null);

    // 1) Plus: nur wenn leer
    if (slots[index].imgAddPlus != null)
        slots[index].imgAddPlus.SetActive(!occupied);

    // 2) Portrait: nur wenn belegt
    if (slots[index].imgCharacterPortrait != null)
    {
        slots[index].imgCharacterPortrait.gameObject.SetActive(occupied);

        // Optional: Sprite setzen (wenn du später Portraits lösen willst)
        // slots[index].imgCharacterPortrait.sprite = occupied ? ... : null;
    }

    // 3) Delete-Button: nur wenn belegt
    if (slots[index].btnDelete != null)
        slots[index].btnDelete.gameObject.SetActive(occupied);

    // 4) Name(Level): nur wenn belegt
    if (slots[index].txtNameLvl != null)
    {
        slots[index].txtNameLvl.gameObject.SetActive(occupied);
        slots[index].txtNameLvl.text = occupied ? $"{data.name} ({data.level})" : "";
    }
}

    void OnSlotClicked(int index)
    {
        var pm = ProfileManager.Instance;
        if (pm == null) return;

        var data = pm.GetSlot(index);

        // Empty -> CreateCharacter
        if (data == null)
        {
            pm.pendingCreateSlotIndex = index;
            SceneManager.LoadScene(sceneCreateCharacter);
            return;
        }

        // Occupied -> go Main with this character
        pm.SetActiveSlot(index);
        SceneManager.LoadScene(sceneMain);
    }

    void OnDeleteClicked(int index)
    {
        var pm = ProfileManager.Instance;
        if (pm == null) return;

        var data = pm.GetSlot(index);
        if (data == null) return;

        pendingDeleteIndex = index;
        ShowDeleteConfirm();
    }

    void ConfirmDelete()
    {
        if (pendingDeleteIndex < 0) { HideDeleteConfirm(); return; }

        var pm = ProfileManager.Instance;
        if (pm != null)
        {
            pm.DeleteCharacter(pendingDeleteIndex);
        }

        pendingDeleteIndex = -1;
        HideDeleteConfirm();
        RefreshAll();
    }

    void ShowDeleteConfirm()
    {
        if (imgDeleteConfirm) imgDeleteConfirm.SetActive(true);
    }

    void HideDeleteConfirm()
    {
        if (imgDeleteConfirm) imgDeleteConfirm.SetActive(false);
        pendingDeleteIndex = -1;
    }

    void BackToAuth()
    {
        SceneManager.LoadScene(sceneAuth);
    }
}