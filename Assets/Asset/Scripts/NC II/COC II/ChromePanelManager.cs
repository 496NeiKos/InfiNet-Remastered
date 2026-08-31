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
 *      ├── TP Link Page                     → tpLinkPage  (inactive by default)
 *      │     ├── Page 1 - Login             → loginPage  (active inside TP Link Page)
 *      │     │     └── Login Body
 *      │     │           ├── [Username TMP_InputField] → usernameField
 *      │     │           ├── [Password TMP_InputField] → passwordField
 *      │     │           ├── Login Button              → loginBtn
 *      │     │           └── ErrorTMP                  → errorTMP  (inactive by default)
 *      │     └── Page 2 - Main              → mainPage  (inactive by default)
 *      └── DLink AP Page                   → dLinkPage  (inactive by default)
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
 *    dLinkPage         → DLink AP Page GameObject
 *    usernameField     → Username TMP_InputField inside Login Body
 *    passwordField     → Password TMP_InputField inside Login Body
 *    loginBtn          → Login Button inside Login Body
 *    errorTMP          → ErrorTMP GameObject inside Login Body
 *    defaultWelcomeText → text displayed on Default Page when search is empty or on Back
 *    tpLinkTabManager  → TPLinkTabManager on Page 2 - Main (router IP read dynamically)
 *    apSearchKeyword   → keyword that navigates to the AP DLink page (default: "dlinkap")
 *    routerReset       → RouterResetController on the router reset button
 *    apReset           → AccessPointResetController on the AP reset button
 *
 *  CHROME BUTTON (Windows Desktop)
 *    Wire ChromeBtn's OnClick → ChromePanelManager.OpenChrome() in the Inspector.
 *
 *  BUTTON OnClick — auto-wired in Awake. Do NOT wire loginBtn/exitBtn/backBtn manually.
 *
 *  NAVIGATION FLOW
 *    Search submit router LAN IP  → TP Link Page  (IP read live from TPLinkTabManager)
 *                                     not yet logged in → Page 1 - Login
 *                                     already logged in → Page 2 - Main
 *    Search submit apSearchKeyword → DLink AP Page (no login — opens directly)
 *    Search submit (anything else) → Default Page; TMP shows "No result found"
 *    Login (admin / admin)         → Page 2 - Main; login held for this Chrome session
 *    Login (wrong credentials)     → ErrorTMP activates; hides on next keystroke
 *    Back button                   → pops browser history stack; disabled when empty
 *    Exit button                   → closes Chrome; resets navigation to Default Page;
 *                                     clears login session; TP-Link config PRESERVED
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
    private enum ChromePage { Default, TPLinkLogin, TPLinkMain, DLinkAP }

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
    [SerializeField] private GameObject dLinkPage;

    [Header("Login")]
    [SerializeField] private TMP_InputField usernameField;
    [SerializeField] private TMP_InputField passwordField;
    [SerializeField] private Button         loginBtn;
    [SerializeField] private GameObject     errorTMP;

    [Header("Settings")]
    [SerializeField] [TextArea(2, 4)] private string defaultWelcomeText =
        "Welcome!\nType an IP address in the address bar to navigate to a device.";

    [Header("Device Search Keywords (set in Inspector)")]
    [Tooltip("Keyword that routes to the AP DLink page (typed after AP reset).")]
    [SerializeField] private string apSearchKeyword = "dlinkap";

    [Header("Router WebUI")]
    [Tooltip("TPLinkTabManager on Page 2 - Main. Router IP is read live from GetRouterLanIP().")]
    [SerializeField] private TPLinkTabManager tpLinkTabManager;

    [Header("Reset Controllers")]
    [SerializeField] private RouterResetController      routerReset;
    [SerializeField] private AccessPointResetController apReset;

    private const string AdminCred = "admin";

    private readonly Stack<ChromePage> _history = new Stack<ChromePage>();
    private ChromePage _currentPage = ChromePage.Default;
    private bool       _isLoggedIn;

    // ----------------------------------------------------------------
    //  State load/save (called by VirtualOSManager on device switch)
    // ----------------------------------------------------------------

    public void LoadState(DeviceOSState state)
    {
        _isLoggedIn = state.ChromeIsLoggedIn;

        _history.Clear();
        foreach (int entry in state.ChromeHistory)
            _history.Push((ChromePage)entry);

        if (searchField   != null) searchField.text   = "";
        if (usernameField != null) usernameField.text = "";
        if (passwordField != null) passwordField.text = "";
        HideError();

        ShowPage((ChromePage)state.ChromeCurrentPage);
    }

    public void SaveState(DeviceOSState state)
    {
        state.ChromeCurrentPage = (int)_currentPage;
        state.ChromeIsLoggedIn  = _isLoggedIn;

        state.ChromeHistory.Clear();
        ChromePage[] arr = _history.ToArray();
        for (int i = arr.Length - 1; i >= 0; i--)
            state.ChromeHistory.Add((int)arr[i]);
    }

    public void CloseForSwitch() => gameObject.SetActive(false);

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        if (backBtn  != null) backBtn.onClick.AddListener(NavigateBack);
        if (exitBtn  != null) exitBtn.onClick.AddListener(ExitChrome);
        if (loginBtn != null) loginBtn.onClick.AddListener(TryLogin);

        if (searchField != null) searchField.onSubmit.AddListener(OnSearchSubmit);

        if (usernameField != null) usernameField.onValueChanged.AddListener(_ => HideError());
        if (passwordField != null) passwordField.onValueChanged.AddListener(_ => HideError());

        ResetFlow();
    }

    private void OnEnable()
    {
        if (searchField != null) searchField.text = "";
    }

    // ----------------------------------------------------------------
    //  Public API
    // ----------------------------------------------------------------

    public void OpenChrome() => gameObject.SetActive(true);

    public void ExitChrome()
    {
        ResetFlow();
        gameObject.SetActive(false);
        Debug.Log("[ChromePanelManager] Exited. Navigation reset; config preserved.");
    }

    // ----------------------------------------------------------------
    //  Search
    // ----------------------------------------------------------------

    private void OnSearchSubmit(string input)
    {
        string trimmed = input.Trim();

        string routerIP = tpLinkTabManager?.GetRouterLanIP() ?? "";
        if (!string.IsNullOrEmpty(routerIP) && trimmed == routerIP)
        {
            HandleWifiNavigation(
                VirtualOSManager.Instance?.IsWifiRouterTopologySatisfied() ?? false,
                routerReset != null && routerReset.IsDefaultIPReady,
                "Router");
            return;
        }

        if (!string.IsNullOrEmpty(apSearchKeyword) && trimmed == apSearchKeyword)
        {
            HandleDLinkNavigation(
                VirtualOSManager.Instance?.IsWifiAPTopologySatisfied() ?? false,
                apReset != null && apReset.IsDefaultIPReady);
            return;
        }

        if (defaultPageTMP != null)
        {
            defaultPageTMP.text = string.IsNullOrEmpty(trimmed)
                ? defaultWelcomeText
                : $"No result found, you searched: '{trimmed}'\nTry typing your IP address.";
        }
        if (_currentPage != ChromePage.Default)
            NavigateTo(ChromePage.Default);
    }

    private void HandleWifiNavigation(bool topologySatisfied, bool defaultIPReady, string deviceName)
    {
        if (!topologySatisfied)
        {
            _history.Clear();
            if (_currentPage != ChromePage.Default) ShowPage(ChromePage.Default);
            if (defaultPageTMP != null)
                defaultPageTMP.text =
                    $"You are trying to access '{deviceName} Configuration' without completing " +
                    $"the topology setup. Return after setting it up. Press 'Esc' to return.";
            Debug.Log($"[ChromePanelManager] Topology not satisfied for {deviceName} — blocked.");
            return;
        }

        if (!defaultIPReady)
        {
            _history.Clear();
            if (_currentPage != ChromePage.Default) ShowPage(ChromePage.Default);
            if (defaultPageTMP != null)
                defaultPageTMP.text =
                    $"The {deviceName} has not been reset to factory defaults. " +
                    $"Hold the reset button for 10 seconds first.";
            Debug.Log($"[ChromePanelManager] Default IP not ready for {deviceName} — blocked.");
            return;
        }

        NavigateTo(_isLoggedIn ? ChromePage.TPLinkMain : ChromePage.TPLinkLogin);
    }

    private void HandleDLinkNavigation(bool topologySatisfied, bool defaultIPReady)
    {
        if (!topologySatisfied)
        {
            _history.Clear();
            if (_currentPage != ChromePage.Default) ShowPage(ChromePage.Default);
            if (defaultPageTMP != null)
                defaultPageTMP.text =
                    "You are trying to access 'Access Point Configuration' without completing " +
                    "the topology setup. Return after setting it up. Press 'Esc' to return.";
            Debug.Log("[ChromePanelManager] AP topology not satisfied — DLink blocked.");
            return;
        }

        if (!defaultIPReady)
        {
            _history.Clear();
            if (_currentPage != ChromePage.Default) ShowPage(ChromePage.Default);
            if (defaultPageTMP != null)
                defaultPageTMP.text =
                    "The Access Point has not been reset to factory defaults. " +
                    "Hold the reset button for 10 seconds first.";
            Debug.Log("[ChromePanelManager] AP default IP not ready — DLink blocked.");
            return;
        }

        NavigateTo(ChromePage.DLinkAP);
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

        // Sync immediately so task conditions read the live page without waiting for SaveState.
        var liveState = VirtualOSManager.Instance?.CurrentState;
        if (liveState != null) liveState.ChromeCurrentPage = (int)page;

        bool isDefault = page == ChromePage.Default;
        bool isTPLink  = page == ChromePage.TPLinkLogin || page == ChromePage.TPLinkMain;
        bool isLogin   = page == ChromePage.TPLinkLogin;
        bool isMain    = page == ChromePage.TPLinkMain;
        bool isDLink   = page == ChromePage.DLinkAP;

        if (defaultPage != null) defaultPage.SetActive(isDefault);
        if (tpLinkPage  != null) tpLinkPage.SetActive(isTPLink);
        if (loginPage   != null) loginPage.SetActive(isLogin);
        if (mainPage    != null) mainPage.SetActive(isMain);
        if (dLinkPage   != null) dLinkPage.SetActive(isDLink);

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

        if (searchField != null) searchField.text = "";
        if (errorTMP    != null) errorTMP.SetActive(false);

        ShowPage(ChromePage.Default);
    }
}
