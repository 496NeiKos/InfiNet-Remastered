/*
 * ================================================================
 *  UNITY SETUP GUIDE — ChromePanelManager
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to the "Chrome" GameObject under Windows Desktop.
 *
 *  HIERARCHY
 *    Chrome  (this script here)
 *      ├── Nav                              ← always active, never toggled
 *      │     ├── Back                       → backBtn
 *      │     ├── Chrome Search - Input Field → searchField
 *      │     └── Exit                       → exitBtn
 *      ├── Default Page                     → defaultPage  (active by default)
 *      │     └── [TMP_Text child]           → defaultPageTMP
 *      └── TP Link Page                     → tpLinkPage  (inactive by default)
 *            ├── Page 1 - Login             → loginPage  (active inside TP Link Page)
 *            │     └── Login Body
 *            │           ├── [Username TMP_InputField] → usernameField
 *            │           ├── [Password TMP_InputField] → passwordField
 *            │           ├── Login Button              → loginBtn
 *            │           └── ErrorTMP                  → errorTMP  (inactive by default)
 *            └── Page 2 - Main              → mainPage  (inactive by default)
 *
 *  INSPECTOR ASSIGNMENTS
 *    backBtn           → Back Button under Nav
 *    searchField       → Chrome Search - Input Field under Nav
 *    exitBtn           → Exit Button under Nav
 *    defaultPage       → Default Page GameObject
 *    defaultPageTMP    → TMP_Text child inside Default Page
 *    tpLinkPage        → TP Link Page GameObject
 *    loginPage         → Page 1 - Login GameObject
 *    mainPage          → Page 2 - Main GameObject
 *    usernameField     → Username TMP_InputField inside Login Body
 *    passwordField     → Password TMP_InputField inside Login Body
 *    loginBtn          → Login Button inside Login Body
 *    errorTMP          → ErrorTMP GameObject inside Login Body
 *    defaultWelcomeText → text displayed on Default Page when search is empty or on Back
 *
 *  CHROME BUTTON (Windows Desktop)
 *    Wire ChromeBtn's OnClick → ChromePanelManager.OpenChrome() in the Inspector.
 *    No script reference needed on the desktop side.
 *
 *  BUTTON OnClick — auto-wired in Awake. Do NOT wire loginBtn/exitBtn/backBtn manually.
 *
 *  NAVIGATION FLOW
 *    Search submit "192.168.1.1"  → TP Link Page
 *                                     not yet logged in → Page 1 - Login
 *                                     already logged in → Page 2 - Main  (skips login)
 *    Search submit (anything else) → stay on / return to Default Page
 *                                     TMP shows: "No result found, you searched: '...'"
 *    Login (admin / admin)         → Page 2 - Main; login held for this Chrome session
 *    Login (wrong credentials)     → ErrorTMP activates; hides on next keystroke
 *    Back button                   → pops browser history stack; disabled when no history
 *    Exit button                   → closes Chrome; resets navigation flow to Default Page;
 *                                     clears login session; Page 2 config values PRESERVED
 *
 *  HISTORY STACK LOGIC
 *    Every NavigateTo() call pushes the current page before switching.
 *    NavigateBack() pops the previous page off the stack.
 *    Exit/Reset clears the stack entirely.
 *    Back is interactable only when the stack is non-empty.
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChromePanelManager : MonoBehaviour
{
    private enum ChromePage { Default, TPLinkLogin, TPLinkMain }

    [Header("Nav")]
    [SerializeField] private Button backBtn;
    [SerializeField] private TMP_InputField searchField;
    [SerializeField] private Button exitBtn;

    [Header("Pages")]
    [SerializeField] private GameObject defaultPage;
    [SerializeField] private TMP_Text   defaultPageTMP;
    [SerializeField] private GameObject tpLinkPage;
    [SerializeField] private GameObject loginPage;
    [SerializeField] private GameObject mainPage;

    [Header("Login")]
    [SerializeField] private TMP_InputField usernameField;
    [SerializeField] private TMP_InputField passwordField;
    [SerializeField] private Button         loginBtn;
    [SerializeField] private GameObject     errorTMP;

    [Header("Settings")]
    [SerializeField] [TextArea(2, 4)] private string defaultWelcomeText =
        "Welcome!\nType an IP address in the address bar to navigate to a device.";

    private const string RouterIP  = "192.168.1.1";
    private const string AdminCred = "admin";

    private readonly Stack<ChromePage> _history = new Stack<ChromePage>();
    private ChromePage _currentPage = ChromePage.Default;
    private bool       _isLoggedIn;

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        if (backBtn  != null) backBtn.onClick.AddListener(NavigateBack);
        if (exitBtn  != null) exitBtn.onClick.AddListener(ExitChrome);
        if (loginBtn != null) loginBtn.onClick.AddListener(TryLogin);

        // Submit fires on Enter key or submit event — not on every keystroke
        if (searchField != null) searchField.onSubmit.AddListener(OnSearchSubmit);

        // ErrorTMP hides the moment the user starts correcting either field
        if (usernameField != null) usernameField.onValueChanged.AddListener(_ => HideError());
        if (passwordField != null) passwordField.onValueChanged.AddListener(_ => HideError());

        ResetFlow();
    }

    // ----------------------------------------------------------------
    //  Public API
    // ----------------------------------------------------------------

    public void OpenChrome() => gameObject.SetActive(true);

    public void ExitChrome()
    {
        ResetFlow();
        gameObject.SetActive(false);
        Debug.Log("[ChromePanelManager] Exited. Navigation reset; Page 2 config preserved.");
    }

    // ----------------------------------------------------------------
    //  Search
    // ----------------------------------------------------------------

    private void OnSearchSubmit(string input)
    {
        string trimmed = input.Trim();

        if (trimmed == RouterIP)
        {
            NavigateTo(_isLoggedIn ? ChromePage.TPLinkMain : ChromePage.TPLinkLogin);
        }
        else
        {
            // Update Default Page text to reflect the failed search
            if (defaultPageTMP != null)
            {
                defaultPageTMP.text = string.IsNullOrEmpty(trimmed)
                    ? defaultWelcomeText
                    : $"No result found, you searched: '{trimmed}'\nTry typing your IP address.";
            }

            // Only push history and switch if we're not already on Default
            if (_currentPage != ChromePage.Default)
                NavigateTo(ChromePage.Default);
        }
    }

    // ----------------------------------------------------------------
    //  Login
    // ----------------------------------------------------------------

    private void TryLogin()
    {
        string user = usernameField != null ? usernameField.text : "";
        string pass = passwordField != null ? passwordField.text : "";

        if (user == AdminCred && pass == AdminCred)
        {
            _isLoggedIn = true;
            HideError();
            NavigateTo(ChromePage.TPLinkMain);
            Debug.Log("[ChromePanelManager] Login successful.");
        }
        else
        {
            ShowError();
            Debug.Log("[ChromePanelManager] Login failed — wrong credentials.");
        }
    }

    private void ShowError() { if (errorTMP != null) errorTMP.SetActive(true); }
    private void HideError() { if (errorTMP != null) errorTMP.SetActive(false); }

    // ----------------------------------------------------------------
    //  Navigation
    // ----------------------------------------------------------------

    private void NavigateTo(ChromePage page)
    {
        if (page == _currentPage) return;

        _history.Push(_currentPage);
        ShowPage(page);
        Debug.Log($"[ChromePanelManager] Navigate → {page}. History depth: {_history.Count}.");
    }

    private void NavigateBack()
    {
        if (_history.Count == 0) return;

        ChromePage previous = _history.Pop();
        ShowPage(previous);
        Debug.Log($"[ChromePanelManager] Back → {previous}. History depth: {_history.Count}.");
    }

    // ----------------------------------------------------------------
    //  Page display
    // ----------------------------------------------------------------

    private void ShowPage(ChromePage page)
    {
        _currentPage = page;

        bool isDefault = page == ChromePage.Default;
        bool isTPLink  = page == ChromePage.TPLinkLogin || page == ChromePage.TPLinkMain;
        bool isLogin   = page == ChromePage.TPLinkLogin;
        bool isMain    = page == ChromePage.TPLinkMain;

        if (defaultPage != null) defaultPage.SetActive(isDefault);
        if (tpLinkPage  != null) tpLinkPage.SetActive(isTPLink);
        if (loginPage   != null) loginPage.SetActive(isLogin);
        if (mainPage    != null) mainPage.SetActive(isMain);

        // When landing on Default (Back or initial), always show the welcome text
        if (isDefault && defaultPageTMP != null)
            defaultPageTMP.text = defaultWelcomeText;

        RefreshBackButton();
    }

    private void RefreshBackButton()
    {
        if (backBtn != null) backBtn.interactable = _history.Count > 0;
    }

    // ----------------------------------------------------------------
    //  Reset
    // ----------------------------------------------------------------

    private void ResetFlow()
    {
        _history.Clear();
        _isLoggedIn = false;

        if (searchField   != null) searchField.text = "";
        if (errorTMP      != null) errorTMP.SetActive(false);

        ShowPage(ChromePage.Default);
    }
}
