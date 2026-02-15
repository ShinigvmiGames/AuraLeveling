using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AuthUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelLogin;
    public GameObject panelRegister;

    [Header("Login Fields")]
    public TMP_InputField loginEmail;
    public TMP_InputField loginPassword;
    public Button btnLogin;
    public Button btnGotoRegister;

    [Header("Register Fields")]
    public TMP_InputField regEmail;
    public TMP_InputField regPassword;
    public TMP_InputField regConfirm;
    public Button btnRegister;
    public Button btnGotoLogin;

    [Header("Guest")]
    public Button btnGuest; // <- NEU Button im UI
    public string nextSceneGuest = "CharacterSelect";
    public string keyIsGuest = "IS_GUEST";

    [Header("Status")]
    public TMP_Text txtStatus;

    [Header("Optional Loading Overlay")]
    public CanvasGroup loadingOverlay;

    [Header("Flow")]
    public string sceneCharacterSelect = "CharacterSelect";

    const string KEY_LAST_EMAIL = "LAST_EMAIL";
    const string KEY_PASS_PREFIX = "ACC_PASS_";

    bool isBusy = false;

    void Start()
{
    // default view
    ShowRegister(); // 👈 statt ShowLogin()

    // bind buttons
    if (btnLogin) btnLogin.onClick.AddListener(OnLoginPressed);
    if (btnGotoRegister) btnGotoRegister.onClick.AddListener(ShowRegister);

    if (btnRegister) btnRegister.onClick.AddListener(OnRegisterPressed);
    if (btnGotoLogin) btnGotoLogin.onClick.AddListener(ShowLogin);

    if (btnGuest) btnGuest.onClick.AddListener(OnGuestPressed);

    // Guest bleibt immer sichtbar
    SetStatus("");
    SetLoading(false);
}

    void EnsureProfileManager()
    {
        if (ProfileManager.Instance != null) return;

        var go = new GameObject("ProfileManager");
        go.AddComponent<ProfileManager>();
    }

    public void ShowLogin()
    {
        if (panelLogin) panelLogin.SetActive(true);
        if (panelRegister) panelRegister.SetActive(false);
        SetStatus("");
    }

    public void ShowRegister()
    {
        if (panelLogin) panelLogin.SetActive(false);
        if (panelRegister) panelRegister.SetActive(true);
        SetStatus("");
    }

    void OnGuestPressed()
    {
        if (isBusy) return;

        PlayerPrefs.SetInt(keyIsGuest, 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene(nextSceneGuest);
    }

    void OnRegisterPressed()
    {
        if (isBusy) return;

        string email = regEmail ? regEmail.text.Trim() : "";
        string pass = regPassword ? regPassword.text : "";
        string conf = regConfirm ? regConfirm.text : "";

        if (!IsValidEmail(email)) { SetStatus("Please enter a valid email."); return; }
        if (pass.Length < 6) { SetStatus("Password must be at least 6 characters."); return; }
        if (pass != conf) { SetStatus("Passwords do not match."); return; }

        string keyPass = KEY_PASS_PREFIX + email.ToLowerInvariant();
        if (PlayerPrefs.HasKey(keyPass))
        {
            SetStatus("Account already exists. Please log in.");
            return;
        }

        PlayerPrefs.SetString(keyPass, pass);
        PlayerPrefs.SetString(KEY_LAST_EMAIL, email);
        PlayerPrefs.Save();

        SetStatus("Account created! Logging in...");
        StartCoroutine(FakeNetworkThenGoCharacterSelect(email.ToLowerInvariant(), false));
    }

    void OnLoginPressed()
    {
        if (isBusy) return;

        string email = loginEmail ? loginEmail.text.Trim() : "";
        string pass = loginPassword ? loginPassword.text : "";

        if (!IsValidEmail(email)) { SetStatus("Please enter a valid email."); return; }
        if (pass.Length < 1) { SetStatus("Please enter your password."); return; }

        string keyPass = KEY_PASS_PREFIX + email.ToLowerInvariant();
        if (!PlayerPrefs.HasKey(keyPass))
        {
            SetStatus("Account not found. Please register.");
            return;
        }

        string stored = PlayerPrefs.GetString(keyPass, "");
        if (stored != pass)
        {
            SetStatus("Wrong password.");
            return;
        }

        PlayerPrefs.SetString(KEY_LAST_EMAIL, email);
        PlayerPrefs.Save();

        SetStatus("Login successful!");
        StartCoroutine(FakeNetworkThenGoCharacterSelect(email.ToLowerInvariant(), false));
    }

    IEnumerator FakeNetworkThenGoCharacterSelect(string accountId, bool isGuest)
    {
        isBusy = true;
        SetLoading(true);

        yield return new WaitForSeconds(0.6f);

        SetLoading(false);
        isBusy = false;

        // ✅ Set account in ProfileManager and load CharacterSelect
        if (ProfileManager.Instance != null)
            ProfileManager.Instance.SetAccount(accountId, isGuest);

        SceneManager.LoadScene(sceneCharacterSelect);
    }

    void SetStatus(string msg)
    {
        if (txtStatus) txtStatus.text = msg;
    }

    void SetLoading(bool on)
    {
        if (!loadingOverlay) return;
        loadingOverlay.alpha = on ? 1f : 0f;
        loadingOverlay.blocksRaycasts = on;
        loadingOverlay.interactable = on;
    }

    bool IsValidEmail(string email)
    {
        return !string.IsNullOrEmpty(email) && email.Contains("@") && email.Contains(".");
    }
}