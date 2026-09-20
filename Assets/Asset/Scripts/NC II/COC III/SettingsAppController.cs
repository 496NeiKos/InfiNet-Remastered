/*
 * ================================================================
 *  UNITY SETUP GUIDE — SettingsAppController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to the root "Settings Panel" GameObject (starts INACTIVE).
 *
 *  ── FULL HIERARCHY ──────────────────────────────────────────────
 *
 *    Settings Panel                          ← this script here (starts INACTIVE)
 *      ├── TitleBar
 *      │     ├── TitleTMP                    (static: "Settings")
 *      │     └── CloseBtn                    → closeBtn
 *      │
 *      ├── Level 0 Panel                     → level0Panel  (ACTIVE on Open)
 *      │     ├── SearchBar                   (visual only, no function)
 *      │     ├── UserInfoArea
 *      │     │     └── Level0UserInfoTMP     → level0UserInfoTMP
 *      │     │           (shows "ComputerName  |  LoggedInUser")
 *      │     └── TilesGrid
 *      │           ├── SystemTileBtn         → systemTileBtn
 *      │           ├── DevicesTileBtn        → devicesTileBtn        (visual only)
 *      │           ├── PhoneTileBtn          → phoneTileBtn          (visual only)
 *      │           ├── NetworkTileBtn        → networkTileBtn        (visual only)
 *      │           ├── PersonalizationTileBtn→ personalizationTileBtn(visual only)
 *      │           ├── AppsTileBtn           → appsTileBtn           (visual only)
 *      │           ├── AccountsTileBtn       → accountsTileBtn       (hidden on Client PC)
 *      │           ├── TimeTileBtn           → timeTileBtn           (visual only)
 *      │           ├── GamingTileBtn         → gamingTileBtn         (visual only)
 *      │           ├── EaseOfAccessTileBtn   → easeOfAccessTileBtn   (visual only)
 *      │           ├── PrivacyTileBtn        → privacyTileBtn        (visual only)
 *      │           └── UpdateTileBtn         → updateTileBtn         (visual only)
 *      │
 *      └── Level 1 Container                 → level1Container  (starts INACTIVE)
 *            ├── BackBtn                     → backBtn
 *            ├── CategoryTitleTMP            → level1CategoryTitleTMP
 *            │     (shows e.g. "System" or "Accounts")
 *            │
 *            ├── Sidebar
 *            │     ├── System Sidebar Group  → systemSidebarGroup  (INACTIVE for Accounts)
 *            │     │     ├── DisplaySideBtn          → displaySideBtn
 *            │     │     ├── SoundSideBtn            → soundSideBtn
 *            │     │     ├── NotificationsSideBtn    → notificationsSideBtn
 *            │     │     ├── FocusAssistSideBtn      → focusAssistSideBtn
 *            │     │     ├── PowerSleepSideBtn       → powerSleepSideBtn
 *            │     │     ├── StorageSideBtn          → storageSideBtn
 *            │     │     └── AboutSideBtn            → aboutSideBtn
 *            │     │
 *            │     └── Accounts Sidebar Group → accountsSidebarGroup (INACTIVE for System)
 *            │           ├── YourInfoSideBtn          → yourInfoSideBtn
 *            │           └── SignInOptionsSideBtn     → signInOptionsSideBtn
 *            │
 *            └── Content Area
 *                  ├── Placeholder Panel     → placeholderPanel
 *                  │     (empty — used by all visual-only sidebar items)
 *                  │
 *                  ├── About Panel           → aboutPanel
 *                  │     ├── PCNameLabel TMP         (static: "Device name")
 *                  │     ├── PCNameTMP               → pcNameTMP  (dynamic)
 *                  │     ├── RenameBtn               → renameBtn  (hidden on Client PC)
 *                  │     ├── AdvancedSettingsBtn      → advancedSettingsBtn
 *                  │     │     Label: "Advanced system settings"
 *                  │     │     Hidden on Server PC; hidden after domain join
 *                  │     └── Rename Dialog            → renameDialog  (starts INACTIVE)
 *                  │           ├── RenameInput        → renameInput
 *                  │           ├── RenameConfirmBtn   → renameConfirmBtn
 *                  │           └── RenameCancelBtn    → renameCancelBtn
 *                  │
 *                  └── Accounts Content Panel → accountsPanel
 *                        ├── ChangePassBtn            → changePasswordBtn
 *                        └── Password Dialog          → passwordDialog  (starts INACTIVE)
 *                              ├── NewPassInput       → newPasswordInput
 *                              ├── ConfirmInput       → confirmPasswordInput
 *                              ├── PasswordOKBtn      → passwordOKBtn
 *                              └── PasswordCancelBtn  → passwordCancelBtn
 *
 *  ── INSPECTOR ASSIGNMENTS ───────────────────────────────────────
 *    All fields as listed above.
 *    systemPropertiesController → SystemPropertiesController (sibling panel in Desktop Panel)
 *
 *  ── WIRING ──────────────────────────────────────────────────────
 *    Settings desktop icon → Button OnClick → SettingsAppController.Open()
 *    All tile/sidebar buttons wired via Awake() — no manual OnClick needed.
 *    advancedSettingsBtn → SystemPropertiesController.OpenSystemProperties()
 *      (wired in Awake via advancedSettingsBtn.onClick)
 *
 *  ── HOW IT WORKS ────────────────────────────────────────────────
 *    Open() → shows Level 0 (home tiles), refreshes user info label.
 *    Clicking a tile → hides Level 0, shows Level 1 (sidebar + content).
 *    Back button → hides Level 1, shows Level 0.
 *    Sidebar items → switch content panel; non-functional items show placeholderPanel.
 *    About page: PC name reads from correct state (server vs client).
 *    Rename button: only on Server PC, writes to ServerDeviceState.ComputerName.
 *    Advanced System Settings button: only on Client PC before domain join;
 *      triggers SystemPropertiesController.OpenSystemProperties().
 *    Accounts tab: hidden entirely on Client PC.
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsAppController : MonoBehaviour
{
    public static SettingsAppController Instance { get; private set; }

    // ── App root ──────────────────────────────────────────────────────────────

    [Header("App Root")]
    [SerializeField] private Button closeBtn;

    // ── Level 0 ───────────────────────────────────────────────────────────────

    [Header("Level 0 — Home")]
    [SerializeField] private GameObject level0Panel;
    [SerializeField] private TMP_Text   level0UserInfoTMP;

    [Header("Level 0 — Tile Buttons")]
    [SerializeField] private Button systemTileBtn;
    [SerializeField] private Button devicesTileBtn;
    [SerializeField] private Button phoneTileBtn;
    [SerializeField] private Button networkTileBtn;
    [SerializeField] private Button personalizationTileBtn;
    [SerializeField] private Button appsTileBtn;
    [SerializeField] private Button accountsTileBtn;
    [SerializeField] private Button timeTileBtn;
    [SerializeField] private Button gamingTileBtn;
    [SerializeField] private Button easeOfAccessTileBtn;
    [SerializeField] private Button privacyTileBtn;
    [SerializeField] private Button updateTileBtn;

    // ── Level 1 ───────────────────────────────────────────────────────────────

    [Header("Level 1 — Container")]
    [SerializeField] private GameObject level1Container;
    [SerializeField] private Button     backBtn;
    [SerializeField] private TMP_Text   level1CategoryTitleTMP;

    [Header("Level 1 — Sidebar Groups")]
    [SerializeField] private GameObject systemSidebarGroup;
    [SerializeField] private GameObject accountsSidebarGroup;

    [Header("Level 1 — System Sidebar Buttons")]
    [SerializeField] private Button displaySideBtn;
    [SerializeField] private Button soundSideBtn;
    [SerializeField] private Button notificationsSideBtn;
    [SerializeField] private Button focusAssistSideBtn;
    [SerializeField] private Button powerSleepSideBtn;
    [SerializeField] private Button storageSideBtn;
    [SerializeField] private Button aboutSideBtn;

    [Header("Level 1 — Accounts Sidebar Buttons")]
    [SerializeField] private Button yourInfoSideBtn;
    [SerializeField] private Button signInOptionsSideBtn;

    // ── Content Panels ────────────────────────────────────────────────────────

    [Header("Content Panels")]
    [SerializeField] private GameObject placeholderPanel;
    [SerializeField] private GameObject aboutPanel;
    [SerializeField] private GameObject accountsPanel;

    // ── About Page ────────────────────────────────────────────────────────────

    [Header("About")]
    [SerializeField] private TMP_Text       pcNameTMP;
    [SerializeField] private Button         renameBtn;
    [SerializeField] private Button         advancedSettingsBtn;
    [SerializeField] private GameObject     renameDialog;
    [SerializeField] private TMP_InputField renameInput;
    [SerializeField] private Button         renameConfirmBtn;
    [SerializeField] private Button         renameCancelBtn;

    // ── Accounts Page ─────────────────────────────────────────────────────────

    [Header("Accounts")]
    [SerializeField] private Button         changePasswordBtn;
    [SerializeField] private GameObject     passwordDialog;
    [SerializeField] private TMP_InputField newPasswordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;
    [SerializeField] private Button         passwordOKBtn;
    [SerializeField] private Button         passwordCancelBtn;

    // ── External reference ────────────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private SystemPropertiesController systemPropertiesController;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        closeBtn?.onClick.AddListener(Close);
        backBtn?.onClick.AddListener(BackToHome);

        // Level 0 tiles
        systemTileBtn?.onClick.AddListener(OpenCategory_System);
        accountsTileBtn?.onClick.AddListener(OpenCategory_Accounts);
        devicesTileBtn?.onClick.AddListener(() => OpenCategory_Generic("Devices"));
        phoneTileBtn?.onClick.AddListener(() => OpenCategory_Generic("Phone"));
        networkTileBtn?.onClick.AddListener(() => OpenCategory_Generic("Network & Internet"));
        personalizationTileBtn?.onClick.AddListener(() => OpenCategory_Generic("Personalization"));
        appsTileBtn?.onClick.AddListener(() => OpenCategory_Generic("Apps"));
        timeTileBtn?.onClick.AddListener(() => OpenCategory_Generic("Time & Language"));
        gamingTileBtn?.onClick.AddListener(() => OpenCategory_Generic("Gaming"));
        easeOfAccessTileBtn?.onClick.AddListener(() => OpenCategory_Generic("Ease of Access"));
        privacyTileBtn?.onClick.AddListener(() => OpenCategory_Generic("Privacy"));
        updateTileBtn?.onClick.AddListener(() => OpenCategory_Generic("Update & Security"));

        // System sidebar
        displaySideBtn?.onClick.AddListener(() => ShowContent(placeholderPanel));
        soundSideBtn?.onClick.AddListener(() => ShowContent(placeholderPanel));
        notificationsSideBtn?.onClick.AddListener(() => ShowContent(placeholderPanel));
        focusAssistSideBtn?.onClick.AddListener(() => ShowContent(placeholderPanel));
        powerSleepSideBtn?.onClick.AddListener(() => ShowContent(placeholderPanel));
        storageSideBtn?.onClick.AddListener(() => ShowContent(placeholderPanel));
        aboutSideBtn?.onClick.AddListener(ShowAbout);

        // Accounts sidebar
        yourInfoSideBtn?.onClick.AddListener(() => ShowContent(placeholderPanel));
        signInOptionsSideBtn?.onClick.AddListener(() => ShowContent(accountsPanel));

        // Rename PC
        renameBtn?.onClick.AddListener(OpenRenameDialog);
        renameConfirmBtn?.onClick.AddListener(ConfirmRename);
        renameCancelBtn?.onClick.AddListener(CloseRenameDialog);

        // Advanced system settings (domain join — client PC only)
        advancedSettingsBtn?.onClick.AddListener(() =>
        {
            systemPropertiesController?.OpenSystemProperties();
            ActivityLogManager.Log("Opened Advanced System Settings", ActivityLogManager.EntryType.Action);
        });

        // Change password
        changePasswordBtn?.onClick.AddListener(OpenPasswordDialog);
        passwordOKBtn?.onClick.AddListener(ConfirmPassword);
        passwordCancelBtn?.onClick.AddListener(ClosePasswordDialog);

        HideAllContent();
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        GoToLevel0();
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.SettingsOpened = true;
        ActivityLogManager.Log("Opened Settings", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        CloseRenameDialog();
        ClosePasswordDialog();
        gameObject.SetActive(false);
    }

    // ── Level 0 ───────────────────────────────────────────────────────────────

    private void GoToLevel0()
    {
        level0Panel?.SetActive(true);
        level1Container?.SetActive(false);
        HideAllContent();
        CloseRenameDialog();
        ClosePasswordDialog();
        RefreshLevel0();
    }

    private void RefreshLevel0()
    {
        var mgr = ServerVirtualOSManager.Instance;
        bool isClient = mgr != null && mgr.CurrentPC == ActivePC.Client;

        // Hide Accounts tile entirely on client PC
        accountsTileBtn?.gameObject.SetActive(!isClient);

        // Show user info: "ComputerName  |  LoggedInUser"
        if (level0UserInfoTMP != null)
        {
            string pcName   = isClient
                ? (mgr.ClientState?.ComputerName ?? "Client PC")
                : (mgr?.ServerState?.ComputerName is string s && s.Length > 0 ? s : "Server PC");
            string userName = isClient
                ? (mgr.ClientState?.CurrentLoggedInUser ?? "")
                : (mgr?.ServerState?.CurrentLoggedInUser ?? "Administrator");
            level0UserInfoTMP.text = $"{pcName}   |   {userName}";
        }
    }

    private void BackToHome()
    {
        CloseRenameDialog();
        ClosePasswordDialog();
        GoToLevel0();
    }

    // ── Level 1 navigation ────────────────────────────────────────────────────

    private void OpenCategory_System()
    {
        EnterLevel1("System");
        systemSidebarGroup?.SetActive(true);
        accountsSidebarGroup?.SetActive(false);
        ShowAbout(); // default to About since that's the functional page
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.SettingsSystemOpened = true;
        ActivityLogManager.Log("Navigated to Settings → System", ActivityLogManager.EntryType.Action);
    }

    private void OpenCategory_Accounts()
    {
        EnterLevel1("Accounts");
        systemSidebarGroup?.SetActive(false);
        accountsSidebarGroup?.SetActive(true);
        ShowContent(accountsPanel);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.SettingsAccountsOpened = true;
        ActivityLogManager.Log("Navigated to Settings → Accounts", ActivityLogManager.EntryType.Action);
    }

    private void OpenCategory_Generic(string categoryName)
    {
        EnterLevel1(categoryName);
        systemSidebarGroup?.SetActive(false);
        accountsSidebarGroup?.SetActive(false);
        ShowContent(placeholderPanel);
        ActivityLogManager.Log($"Navigated to Settings → {categoryName}", ActivityLogManager.EntryType.Action);
    }

    private void EnterLevel1(string categoryTitle)
    {
        level0Panel?.SetActive(false);
        level1Container?.SetActive(true);
        if (level1CategoryTitleTMP != null) level1CategoryTitleTMP.text = categoryTitle;
        HideAllContent();
    }

    // ── Content Panel helpers ─────────────────────────────────────────────────

    private void ShowContent(GameObject panel)
    {
        HideAllContent();
        panel?.SetActive(true);
    }

    private void ShowAbout()
    {
        HideAllContent();
        aboutPanel?.SetActive(true);
        RefreshAboutPage();
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.SettingsAboutOpened = true;
        ActivityLogManager.Log("Navigated to Settings → About", ActivityLogManager.EntryType.Action);
    }

    private void RefreshAboutPage()
    {
        var mgr     = ServerVirtualOSManager.Instance;
        bool isClient = mgr != null && mgr.CurrentPC == ActivePC.Client;

        // PC name
        if (pcNameTMP != null)
        {
            string name = isClient
                ? (mgr.ClientState?.ComputerName ?? "Client PC")
                : (mgr?.ServerState?.ComputerName is string s && s.Length > 0 ? s : "WIN-SERVER");
            pcNameTMP.text = name;
        }

        // Rename button: only on Server PC
        renameBtn?.gameObject.SetActive(!isClient);

        // Advanced settings button: only on Client PC AND not yet domain joined
        bool showAdvanced = isClient && !(mgr?.ClientState?.DomainJoined ?? false);
        advancedSettingsBtn?.gameObject.SetActive(showAdvanced);
    }

    private void HideAllContent()
    {
        placeholderPanel?.SetActive(false);
        aboutPanel?.SetActive(false);
        accountsPanel?.SetActive(false);
    }

    // ── Rename PC ─────────────────────────────────────────────────────────────

    private void OpenRenameDialog()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (renameInput != null) renameInput.text = state?.ComputerName ?? "";
        renameDialog?.SetActive(true);
    }

    private void CloseRenameDialog()
    {
        renameDialog?.SetActive(false);
    }

    private void ConfirmRename()
    {
        string newName = renameInput != null ? renameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(newName))
        {
            Debug.LogWarning("[SettingsAppController] PC name cannot be empty.");
            return;
        }
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.ComputerName = newName;
        CloseRenameDialog();
        RefreshAboutPage();
        RefreshLevel0();
        ActivityLogManager.Log($"PC renamed to: {newName}", ActivityLogManager.EntryType.Action);
    }

    // ── Change Password ───────────────────────────────────────────────────────

    private void OpenPasswordDialog()
    {
        if (newPasswordInput     != null) newPasswordInput.text     = "";
        if (confirmPasswordInput != null) confirmPasswordInput.text = "";
        passwordDialog?.SetActive(true);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.PasswordPanelOpened = true;
    }

    private void ClosePasswordDialog()
    {
        passwordDialog?.SetActive(false);
    }

    private void ConfirmPassword()
    {
        string pass1 = newPasswordInput?.text     ?? "";
        string pass2 = confirmPasswordInput?.text ?? "";
        if (string.IsNullOrEmpty(pass1))
        {
            Debug.LogWarning("[SettingsAppController] Password cannot be empty.");
            return;
        }
        if (pass1 != pass2)
        {
            Debug.LogWarning("[SettingsAppController] Passwords do not match.");
            return;
        }
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.AdminPassword = pass1;
        ClosePasswordDialog();
        ActivityLogManager.Log("Administrator password set.", ActivityLogManager.EntryType.Action);
    }
}
