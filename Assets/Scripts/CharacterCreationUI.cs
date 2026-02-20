using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public class RaceModels
{
    public RaceType race;
    public Sprite[] maleModels;
    public Sprite[] femaleModels;
}

public class CharacterCreationUI : MonoBehaviour
{
    [Header("UI Steps (Assign in Inspector)")]
    public GameObject CC_Header;     // Step 1
    public GameObject CC_Portrait;   // Step 1
    public GameObject CC_Class;      // Step 2

    [Header("Preview")]
    public Image portraitImage;
    public TMP_Text txtModelIndex;

    [Header("Race Buttons")]
    public Button btnRaceHuman;
    public Button btnRaceOrc;
    public Button btnRaceElf;
    public Button btnRaceDemon;

    [Header("Gender Buttons")]
    public Button btnMale;
    public Button btnFemale;

    [Header("Model Picker")]
    public Button btnPrev;
    public Button btnNext;

    [Header("Continue / Back")]
    public Button btnContinue;       // Step1 -> Step2
    public Button btnBackToSelect;   // Step1 -> CharacterSelect scene
    public Button btnBackToStep1;    // Step2 -> Step1

    [Header("Scenes")]
    public string sceneCharacterSelect = "CharacterSelect";
    public string sceneMain = "Main";

    [Header("Name")]
    public TMP_InputField inputName;
    public TMP_Text txtNameStatus;
    public int minNameLength = 3;
    public int maxNameLength = 15;

    [Header("Confirm")]
    public Button btnConfirm;

    [Header("Data")]
    public RaceModels[] raceModels;

    [Header("Selection State")]
    public RaceType selectedRace = RaceType.Human;
    public GenderType selectedGender = GenderType.Male;
    public int selectedModelIndex = 0;

    [Header("Class Selection")]
    public PlayerClass? selectedClass = null; // ✅ no default selected

    [Header("Class Buttons")]
    public Button btnClassAssassin;
    public Button btnClassTank;
    public Button btnClassArcher;
    public Button btnClassWarrior;
    public Button btnClassMage;
    public Button btnClassNecromancer;

    [Header("Info Texts")]
    public TMP_Text txtSelectedClassMainStat; // main stat + passive skill (hidden until class picked)
    public TMP_Text txtRaceFlavor;            // funny line when selecting race

    // ----------------------------
    // Name auto-check gating
    // ----------------------------
    bool nameCheckedOk = false;
    string lastCheckedName = "";
    Coroutine nameCheckRoutine;

    void Start()
    {
        // Force Step 1 as default
        SetStep1Active();

        // Hide class info until class selected
        if (txtSelectedClassMainStat != null)
            txtSelectedClassMainStat.gameObject.SetActive(false);

        // Hide name status by default (show only on error)
        if (txtNameStatus != null)
            txtNameStatus.gameObject.SetActive(false);

        BindButtons();

        RefreshPreview();
        ApplySelectedVisuals();
        UpdateRaceFlavorText();

        // Kick first check state
        OnNameChanged();
        UpdateConfirmState();
    }

    void BindButtons()
    {
        // Race
        if (btnRaceHuman) btnRaceHuman.onClick.AddListener(() => SetRace(RaceType.Human));
        if (btnRaceOrc) btnRaceOrc.onClick.AddListener(() => SetRace(RaceType.Orc));
        if (btnRaceElf) btnRaceElf.onClick.AddListener(() => SetRace(RaceType.Elf));
        if (btnRaceDemon) btnRaceDemon.onClick.AddListener(() => SetRace(RaceType.Demon));

        // Gender
        if (btnMale) btnMale.onClick.AddListener(() => SetGender(GenderType.Male));
        if (btnFemale) btnFemale.onClick.AddListener(() => SetGender(GenderType.Female));

        // Model arrows
        if (btnPrev) btnPrev.onClick.AddListener(PrevModel);
        if (btnNext) btnNext.onClick.AddListener(NextModel);

        // Continue (Step1 -> Step2)
        if (btnContinue)
        {
            btnContinue.onClick.RemoveAllListeners();
            btnContinue.onClick.AddListener(OnContinuePressed);
        }

        // Back Step2 -> Step1
        if (btnBackToStep1)
        {
            btnBackToStep1.onClick.RemoveAllListeners();
            btnBackToStep1.onClick.AddListener(OnBackToStep1Pressed);
        }

        // Back Step1 -> CharacterSelect scene
        if (btnBackToSelect)
        {
            btnBackToSelect.onClick.RemoveAllListeners();
            btnBackToSelect.onClick.AddListener(OnBackToSelectPressed);
        }

        // Name live -> auto check
        if (inputName != null)
        {
            inputName.onValueChanged.RemoveAllListeners();
            inputName.onValueChanged.AddListener(_ => OnNameChanged());
        }

        // Confirm
        if (btnConfirm)
        {
            btnConfirm.onClick.RemoveAllListeners();
            btnConfirm.onClick.AddListener(ConfirmCreate);
        }

        // Class buttons
        if (btnClassAssassin) btnClassAssassin.onClick.AddListener(() => SelectClass(PlayerClass.Assassine));
        if (btnClassTank) btnClassTank.onClick.AddListener(() => SelectClass(PlayerClass.Tank));
        if (btnClassArcher) btnClassArcher.onClick.AddListener(() => SelectClass(PlayerClass.Bogenschuetze));
        if (btnClassWarrior) btnClassWarrior.onClick.AddListener(() => SelectClass(PlayerClass.Krieger));
        if (btnClassMage) btnClassMage.onClick.AddListener(() => SelectClass(PlayerClass.Magier));
        if (btnClassNecromancer) btnClassNecromancer.onClick.AddListener(() => SelectClass(PlayerClass.Nekromant));
    }

    // ----------------------------
    // Step control
    // ----------------------------
    void SetStep1Active()
    {
        if (CC_Header) CC_Header.SetActive(true);
        if (CC_Portrait) CC_Portrait.SetActive(true);
        if (CC_Class) CC_Class.SetActive(false);
    }

    void SetStep2Active()
    {
        if (CC_Header) CC_Header.SetActive(false);
        if (CC_Portrait) CC_Portrait.SetActive(false);
        if (CC_Class) CC_Class.SetActive(true);
    }

    void OnContinuePressed()
    {
        var models = GetCurrentModelList();
        if (models == null || models.Length == 0)
            return;

        SetStep2Active();
        UpdateConfirmState();
    }

    void OnBackToStep1Pressed()
    {
        SetStep1Active();
    }

    void OnBackToSelectPressed()
    {
        SceneManager.LoadScene(sceneCharacterSelect);
    }

    // ----------------------------
    // Race / Gender / Model
    // ----------------------------
    void SetRace(RaceType race)
    {
        selectedRace = race;
        selectedModelIndex = 0;
        RefreshPreview();
        ApplySelectedVisuals();
        UpdateRaceFlavorText();
    }

    void SetGender(GenderType gender)
    {
        selectedGender = gender;
        selectedModelIndex = 0;
        RefreshPreview();
        ApplySelectedVisuals();
    }

    void PrevModel()
    {
        var models = GetCurrentModelList();
        if (models == null || models.Length == 0) return;

        selectedModelIndex--;
        if (selectedModelIndex < 0) selectedModelIndex = models.Length - 1;

        RefreshPreview();
    }

    void NextModel()
    {
        var models = GetCurrentModelList();
        if (models == null || models.Length == 0) return;

        selectedModelIndex++;
        if (selectedModelIndex >= models.Length) selectedModelIndex = 0;

        RefreshPreview();
    }

    Sprite[] GetCurrentModelList()
    {
        if (raceModels == null) return null;

        foreach (var rm in raceModels)
        {
            if (rm != null && rm.race == selectedRace)
                return selectedGender == GenderType.Male ? rm.maleModels : rm.femaleModels;
        }
        return null;
    }

    void RefreshPreview()
    {
        var models = GetCurrentModelList();

        if (portraitImage != null)
        {
            if (models != null && models.Length > 0)
            {
                selectedModelIndex = Mathf.Clamp(selectedModelIndex, 0, models.Length - 1);
                portraitImage.sprite = models[selectedModelIndex];
                portraitImage.color = Color.white;
            }
            else
            {
                portraitImage.sprite = null;
                portraitImage.color = new Color(1, 1, 1, 0.15f);
            }
        }

        if (txtModelIndex != null)
        {
            int count = (models == null) ? 0 : models.Length;
            txtModelIndex.text = (count > 0) ? $"Model {selectedModelIndex + 1}/{count}" : "No Models";
        }
    }

    // ----------------------------
    // Visual highlights
    // ----------------------------
    void ApplySelectedVisuals()
    {
        SetBtnSelected(btnRaceHuman, selectedRace == RaceType.Human);
        SetBtnSelected(btnRaceOrc, selectedRace == RaceType.Orc);
        SetBtnSelected(btnRaceElf, selectedRace == RaceType.Elf);
        SetBtnSelected(btnRaceDemon, selectedRace == RaceType.Demon);

        SetBtnSelected(btnMale, selectedGender == GenderType.Male);
        SetBtnSelected(btnFemale, selectedGender == GenderType.Female);
    }

    void SetBtnSelected(Button b, bool selected)
    {
        if (b == null) return;

        var colors = b.colors;
        colors.normalColor = selected ? new Color(0.70f, 0.88f, 1f, 1f) : Color.white;
        colors.highlightedColor = colors.normalColor;
        b.colors = colors;
    }

    // ----------------------------
    // Race funny line
    // ----------------------------
    void UpdateRaceFlavorText()
    {
        if (txtRaceFlavor == null) return;

        txtRaceFlavor.text = selectedRace switch
        {
            RaceType.Human => "Humans: somehow always confident… even at level 1.",
            RaceType.Orc => "Orcs: peace was never an option.",
            RaceType.Elf => "Frost Elves: elegant, cold, and silently judging you.",
            RaceType.Demon => "Demons: charming smile, questionable decisions.",
            _ => "Choose wisely."
        };
    }

    // ----------------------------
    // Class selection + text (HIDDEN until selected)
    // ----------------------------
    void SelectClass(PlayerClass pc)
    {
        selectedClass = pc;

        if (txtSelectedClassMainStat != null)
            txtSelectedClassMainStat.gameObject.SetActive(true);

        UpdateSelectedClassText();
        UpdateConfirmState();
    }

    void UpdateSelectedClassText()
    {
        if (selectedClass == null) return;
        if (txtSelectedClassMainStat == null) return;

        PlayerClass pc = selectedClass.Value;

        string mainStat = GetMainStatForClass(pc);
        string passiveName = GetPassiveName(pc);
        string passiveDesc = GetPassiveDescription(pc);

        // Main Stat + Passive (replaces the old joke)
        txtSelectedClassMainStat.text =
            $"<b>Main Stat:</b> {mainStat}\n" +
            $"<b>{passiveName}</b>\n" +
            $"<size=85%>{passiveDesc}</size>";
    }

    string GetMainStatForClass(PlayerClass pc)
    {
        switch (pc)
        {
            case PlayerClass.Assassine:     return "DEX";
            case PlayerClass.Tank:          return "STR";
            case PlayerClass.Bogenschuetze: return "DEX";
            case PlayerClass.Krieger:       return "STR";
            case PlayerClass.Magier:        return "INT";
            case PlayerClass.Nekromant:     return "INT";
            default:                        return "-";
        }
    }

    string GetPassiveName(PlayerClass pc)
    {
        switch (pc)
        {
            case PlayerClass.Assassine:     return "Shadow Step";
            case PlayerClass.Tank:          return "Iron Fortress";
            case PlayerClass.Bogenschuetze: return "Eagle Eye";
            case PlayerClass.Krieger:       return "Battle Fury";
            case PlayerClass.Magier:        return "Arcane Power";
            case PlayerClass.Nekromant:     return "Soul Drain";
            default:                        return "Unknown";
        }
    }

    string GetPassiveDescription(PlayerClass pc)
    {
        switch (pc)
        {
            case PlayerClass.Assassine:
                return "+20% Speed, +15% Crit Rate\nStrike fast, strike deadly. Your enemies won't see it coming.";
            case PlayerClass.Tank:
                return "+30% Max HP, Armor cap raised to 60%\nAn unbreakable wall. Nothing gets past you.";
            case PlayerClass.Bogenschuetze:
                return "+25% Crit Damage, +10% Speed\nPrecision over power. One perfect shot is all you need.";
            case PlayerClass.Krieger:
                return "+15% Damage, +10% Max HP\nBorn for the battlefield. Hit hard. Survive harder.";
            case PlayerClass.Magier:
                return "+25% Damage\nPure arcane destruction. The strongest burst in the game.";
            case PlayerClass.Nekromant:
                return "+15% Max HP, 15% Lifesteal\nDrains the life force of your enemies to sustain yourself.";
            default:
                return "";
        }
    }


    // ----------------------------
    // Name auto-check (NO CHECK BUTTON)
    // ----------------------------
    void OnNameChanged()
    {
        // Invalidate old check immediately
        nameCheckedOk = false;
        lastCheckedName = "";

        // Hide status while typing (we only show errors)
        if (txtNameStatus != null)
            txtNameStatus.gameObject.SetActive(false);

        if (nameCheckRoutine != null)
            StopCoroutine(nameCheckRoutine);

        nameCheckRoutine = StartCoroutine(DelayedNameCheck());
        UpdateConfirmState();
    }

    IEnumerator DelayedNameCheck()
    {
        yield return new WaitForSeconds(0.25f);

        string name = (inputName != null) ? inputName.text.Trim() : "";

        if (name.Length < minNameLength || name.Length > maxNameLength)
        {
            ShowNameError($"Name must be {minNameLength}-{maxNameLength} characters.");
            nameCheckedOk = false;
            lastCheckedName = "";
            UpdateConfirmState();
            yield break;
        }

        if (ProfileManager.Instance == null)
        {
            ShowNameError("ProfileManager missing.");
            nameCheckedOk = false;
            lastCheckedName = "";
            UpdateConfirmState();
            yield break;
        }

        bool available = ProfileManager.Instance.IsNameAvailable(name);

        if (!available)
        {
            ShowNameError("Name is already in use.");
            nameCheckedOk = false;
            lastCheckedName = "";
            UpdateConfirmState();
            yield break;
        }

        // ✅ Valid
        nameCheckedOk = true;
        lastCheckedName = name;

        // Hide any error now
        if (txtNameStatus != null)
            txtNameStatus.gameObject.SetActive(false);

        UpdateConfirmState();
    }

    void ShowNameError(string msg)
    {
        if (txtNameStatus == null) return;
        txtNameStatus.gameObject.SetActive(true);
        txtNameStatus.text = msg;
    }

    // ----------------------------
    // Confirm gating
    // ----------------------------
    void UpdateConfirmState()
    {
        bool classOk = (selectedClass != null);

        string currentName = (inputName != null) ? inputName.text.Trim() : "";
        bool nameOk = nameCheckedOk && currentName == lastCheckedName;

        bool ok = classOk && nameOk;

        if (btnConfirm != null)
            btnConfirm.interactable = ok;
    }

    // ----------------------------
    // Confirm -> Save -> Main (Anvil tab)
    // ----------------------------
    void ConfirmCreate()
    {
        if (btnConfirm == null || !btnConfirm.interactable) return;

        if (ProfileManager.Instance == null)
        {
            ShowNameError("ProfileManager missing.");
            return;
        }

        if (selectedClass == null)
            return;

        var pm = ProfileManager.Instance;

        int slot = pm.GetHighestAvailableSlotIndex(); // highest free slot
        if (slot < 0)
        {
            ShowNameError("All character slots are full.");
            return;
        }

        string name = inputName.text.Trim();

        CharacterData cd = new CharacterData(
            name,
            1,
            selectedRace,
            selectedGender,
            selectedModelIndex,
            selectedClass.Value
        );

        pm.CreateOrReplaceCharacter(slot, cd);

        PlayerPrefs.SetInt("START_TAB", 1); // 1 = Anvil
        PlayerPrefs.Save();

        SceneManager.LoadScene(sceneMain);
    }
}