using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class CharacterCreationUI : MonoBehaviour
{
    [Header("UI Steps")]
    public GameObject CC_Header;     // Step 1: Gender/Features
    public GameObject CC_Portrait;   // Step 1: Portrait preview area
    public GameObject CC_Class;      // Step 2: Class + Name

    [Header("Portrait Builder")]
    public PortraitBuilder portraitBuilder;

    [Header("Gender Buttons")]
    public Button btnMale;
    public Button btnFemale;

    [Header("Feature Pickers (4 categories, each has Prev/Next + label)")]
    public Button btnSkinColorPrev;
    public Button btnSkinColorNext;
    public TMP_Text txtSkinColorLabel;

    public Button btnFacePrev;
    public Button btnFaceNext;
    public TMP_Text txtFaceLabel;

    public Button btnHairPrev;
    public Button btnHairNext;
    public TMP_Text txtHairLabel;

    public Button btnClothingPrev;
    public Button btnClothingNext;
    public TMP_Text txtClothingLabel;

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

    [Header("Class Selection")]
    public PlayerClass? selectedClass = null;

    [Header("Class Buttons")]
    public Button btnClassAssassin;
    public Button btnClassWarrior;
    public Button btnClassArcher;
    public Button btnClassMage;
    public Button btnClassNecromancer;

    [Header("Info Text")]
    public TMP_Text txtSelectedClassMainStat;

    // --- Selection State ---
    GenderType selectedGender = GenderType.Male;
    int[] featureVariants = new int[4]; // SkinColor, Face, Hair, Clothing (0-3)
    const int VARIANTS_PER_FEATURE = 4;

    // --- Name check ---
    bool nameCheckedOk = false;
    string lastCheckedName = "";
    Coroutine nameCheckRoutine;

    void Start()
    {
        SetStep1Active();

        if (txtSelectedClassMainStat != null)
            txtSelectedClassMainStat.gameObject.SetActive(false);

        if (txtNameStatus != null)
            txtNameStatus.gameObject.SetActive(false);

        BindButtons();
        RefreshPortrait();
        ApplySelectedVisuals();
        RefreshFeatureLabels();
        OnNameChanged();
        UpdateConfirmState();
    }

    void BindButtons()
    {
        // Gender
        if (btnMale) btnMale.onClick.AddListener(() => SetGender(GenderType.Male));
        if (btnFemale) btnFemale.onClick.AddListener(() => SetGender(GenderType.Female));

        // Feature pickers
        if (btnSkinColorPrev) btnSkinColorPrev.onClick.AddListener(() => CycleFeature(0, -1));
        if (btnSkinColorNext) btnSkinColorNext.onClick.AddListener(() => CycleFeature(0, 1));
        if (btnFacePrev) btnFacePrev.onClick.AddListener(() => CycleFeature(1, -1));
        if (btnFaceNext) btnFaceNext.onClick.AddListener(() => CycleFeature(1, 1));
        if (btnHairPrev) btnHairPrev.onClick.AddListener(() => CycleFeature(2, -1));
        if (btnHairNext) btnHairNext.onClick.AddListener(() => CycleFeature(2, 1));
        if (btnClothingPrev) btnClothingPrev.onClick.AddListener(() => CycleFeature(3, -1));
        if (btnClothingNext) btnClothingNext.onClick.AddListener(() => CycleFeature(3, 1));

        // Continue / Back
        if (btnContinue)
        {
            btnContinue.onClick.RemoveAllListeners();
            btnContinue.onClick.AddListener(OnContinuePressed);
        }
        if (btnBackToStep1)
        {
            btnBackToStep1.onClick.RemoveAllListeners();
            btnBackToStep1.onClick.AddListener(OnBackToStep1Pressed);
        }
        if (btnBackToSelect)
        {
            btnBackToSelect.onClick.RemoveAllListeners();
            btnBackToSelect.onClick.AddListener(OnBackToSelectPressed);
        }

        // Name
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
        if (btnClassAssassin) btnClassAssassin.onClick.AddListener(() => SelectClass(PlayerClass.Assassin));
        if (btnClassWarrior) btnClassWarrior.onClick.AddListener(() => SelectClass(PlayerClass.Warrior));
        if (btnClassArcher) btnClassArcher.onClick.AddListener(() => SelectClass(PlayerClass.Archer));
        if (btnClassMage) btnClassMage.onClick.AddListener(() => SelectClass(PlayerClass.Mage));
        if (btnClassNecromancer) btnClassNecromancer.onClick.AddListener(() => SelectClass(PlayerClass.Necromancer));
    }

    // ==================== Step Control ====================
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
        SetStep2Active();
        UpdateConfirmState();
    }

    void OnBackToStep1Pressed() => SetStep1Active();
    void OnBackToSelectPressed() => SceneManager.LoadScene(sceneCharacterSelect);

    // ==================== Gender ====================
    void SetGender(GenderType gender)
    {
        selectedGender = gender;
        ResetFeatures();
        RefreshPortrait();
        ApplySelectedVisuals();
        RefreshFeatureLabels();
    }

    void ResetFeatures()
    {
        for (int i = 0; i < featureVariants.Length; i++)
            featureVariants[i] = 0;
    }

    // ==================== Feature Picker ====================
    void CycleFeature(int featureIndex, int direction)
    {
        featureVariants[featureIndex] += direction;
        if (featureVariants[featureIndex] < 0) featureVariants[featureIndex] = VARIANTS_PER_FEATURE - 1;
        if (featureVariants[featureIndex] >= VARIANTS_PER_FEATURE) featureVariants[featureIndex] = 0;

        // Update only the changed layer for performance
        PortraitFeature feature = (PortraitFeature)featureIndex;
        if (portraitBuilder != null)
            portraitBuilder.SetFeature(feature, selectedGender, featureVariants[featureIndex]);

        RefreshFeatureLabels();
    }

    void RefreshFeatureLabels()
    {
        if (txtSkinColorLabel) txtSkinColorLabel.text = $"Skin {featureVariants[0] + 1}/{VARIANTS_PER_FEATURE}";
        if (txtFaceLabel) txtFaceLabel.text = $"Face {featureVariants[1] + 1}/{VARIANTS_PER_FEATURE}";
        if (txtHairLabel) txtHairLabel.text = $"Hair {featureVariants[2] + 1}/{VARIANTS_PER_FEATURE}";
        if (txtClothingLabel) txtClothingLabel.text = $"Clothing {featureVariants[3] + 1}/{VARIANTS_PER_FEATURE}";
    }

    // ==================== Portrait ====================
    void RefreshPortrait()
    {
        if (portraitBuilder == null) return;

        portraitBuilder.Build(selectedGender,
            featureVariants[0], featureVariants[1], featureVariants[2],
            featureVariants[3]);
    }

    // ==================== Visual Highlights ====================
    void ApplySelectedVisuals()
    {
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

    // ==================== Class Selection ====================
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
        if (selectedClass == null || txtSelectedClassMainStat == null) return;

        PlayerClass pc = selectedClass.Value;
        string mainStat = GetMainStatForClass(pc);
        string skillName = GetSkillName(pc);
        string skillDesc = GetSkillDescription(pc);

        txtSelectedClassMainStat.text =
            $"<b>Main Stat:</b> {mainStat}\n" +
            $"<b>{skillName}</b>\n" +
            $"<size=85%>{skillDesc}</size>";
    }

    string GetMainStatForClass(PlayerClass pc)
    {
        return pc switch
        {
            PlayerClass.Assassin => "DEX",
            PlayerClass.Warrior => "STR",
            PlayerClass.Archer => "DEX",
            PlayerClass.Mage => "INT",
            PlayerClass.Necromancer => "INT",
            _ => "-"
        };
    }

    string GetSkillName(PlayerClass pc)
    {
        return pc switch
        {
            PlayerClass.Assassin => "Shadow",
            PlayerClass.Warrior => "Berserk",
            PlayerClass.Archer => "Stun",
            PlayerClass.Mage => "Arcane Surge",
            PlayerClass.Necromancer => "Undying",
            _ => "Unknown"
        };
    }

    string GetSkillDescription(PlayerClass pc)
    {
        return pc switch
        {
            PlayerClass.Assassin =>
                "20% Dodge Chance\nSilent as a shadow — no blade can touch you.",
            PlayerClass.Warrior =>
                "20% Extra Attack Chance\nPure rage. Every strike can chain into another.",
            PlayerClass.Archer =>
                "15% Stun Chance\nA precise shot paralyzes the enemy for one round.",
            PlayerClass.Mage =>
                "25% Double Damage Chance\nRaw arcane power — uncontrollable, devastating.",
            PlayerClass.Necromancer =>
                "Revive once at 30% HP\nDeath is only the beginning.",
            _ => ""
        };
    }

    // ==================== Name Validation ====================
    void OnNameChanged()
    {
        nameCheckedOk = false;
        lastCheckedName = "";

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

        nameCheckedOk = true;
        lastCheckedName = name;

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

    // ==================== Confirm ====================
    void UpdateConfirmState()
    {
        bool classOk = (selectedClass != null);
        string currentName = (inputName != null) ? inputName.text.Trim() : "";
        bool nameOk = nameCheckedOk && currentName == lastCheckedName;

        if (btnConfirm != null)
            btnConfirm.interactable = classOk && nameOk;
    }

    void ConfirmCreate()
    {
        if (btnConfirm == null || !btnConfirm.interactable) return;
        if (ProfileManager.Instance == null) { ShowNameError("ProfileManager missing."); return; }
        if (selectedClass == null) return;

        var pm = ProfileManager.Instance;
        int slot = pm.GetHighestAvailableSlotIndex();
        if (slot < 0) { ShowNameError("All character slots are full."); return; }

        string name = inputName.text.Trim();

        CharacterData cd = new CharacterData(
            name, 1,
            selectedGender,
            featureVariants[0], featureVariants[1], featureVariants[2],
            featureVariants[3],
            selectedClass.Value
        );

        pm.CreateOrReplaceCharacter(slot, cd);

        PlayerPrefs.SetInt("START_TAB", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene(sceneMain);
    }
}
