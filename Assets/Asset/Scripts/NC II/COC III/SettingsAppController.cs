/*
 * ================================================================
 *  UNITY SETUP GUIDE — SettingsAppController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to the root "Settings Panel" GameObject (starts INACTIVE).
 *
 *  HIERARCHY
 *    Settings Panel                      ← this script here
 *      ├── TitleBar
 *      │     └── CloseBtn               → closeBtn
 *      ├── Sidebar
 *      │     ├── SystemBtn              → systemBtn
 *      │     └── AccountsBtn           → accountsBtn
 *      └── Content Area
 *            ├── System Panel           → systemPanel (starts INACTIVE)
 *            │     ├── AboutBtn         → aboutBtn  (Button inside System panel — opens About sub-panel)
 *            │     └── About Panel      → aboutPanel  (starts INACTIVE)
 *            │           ├── PC Name Label   → pcNameTMP
 *            │           └── RenameBtn       → renameBtn
 *            ├── Rename Dialog          → renameDialog (starts INACTIVE)
 *            │     ├── InputField       → renameInput
 *            │     ├── ConfirmBtn       → renameConfirmBtn
 *            │     └── CancelBtn        → renameCancelBtn
 *            └── Accounts Panel         → accountsPanel (starts INACTIVE)
 *                  ├── ChangePassBtn    → changePasswordBtn
 *                  └── Password Dialog  → passwordDialog (starts INACTIVE)
 *                        ├── NewPassInput    → newPasswordInput
 *                        ├── ConfirmInput    → confirmPasswordInput
 *                        ├── OKBtn           → passwordOKBtn
 *                        └── CancelBtn       → passwordCancelBtn
 *
 *  INSPECTOR ASSIGNMENTS
 *    All fields as listed in hierarchy above.
 *
 *  DESKTOP ICON WIRING
 *    Settings desktop icon → Button OnClick → SettingsAppController.Open()
 *
 *  HOW IT WORKS
 *    Sidebar buttons show one panel at a time (System or Accounts).
 *    Inside System, clicking About reveals the About sub-panel.
 *    Rename dialog: player types new PC name → Confirm saves to ServerDeviceState.ComputerName.
 *    Password dialog: player types + confirms password → saves to ServerDeviceState.AdminPassword.
 *    All navigation events set latch flags in ServerDeviceState for the task manager.
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsAppController : MonoBehaviour
{
    public static SettingsAppController Instance { get; private set; }

    [Header("App Root")]
    [SerializeField] private Button closeBtn;

    [Header("Sidebar")]
    [SerializeField] private Button systemBtn;
    [SerializeField] private Button accountsBtn;

    [Header("Panels")]
    [SerializeField] private GameObject systemPanel;
    [SerializeField] private Button     aboutBtn;    // inside systemPanel — navigates to About sub-panel
    [SerializeField] private GameObject aboutPanel;
    [SerializeField] private GameObject accountsPanel;

    [Header("Rename PC")]
    [SerializeField] private TMP_Text      pcNameTMP;
    [SerializeField] private Button        renameBtn;
    [SerializeField] private GameObject    renameDialog;
    [SerializeField] private TMP_InputField renameInput;
    [SerializeField] private Button        renameConfirmBtn;
    [SerializeField] private Button        renameCancelBtn;

    [Header("Password")]
    [SerializeField] private Button        changePasswordBtn;
    [SerializeField] private GameObject    passwordDialog;
    [SerializeField] private TMP_InputField newPasswordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;
    [SerializeField] private Button        passwordOKBtn;
    [SerializeField] private Button        passwordCancelBtn;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        closeBtn?.onClick.AddListener(Close);
        systemBtn?.onClick.AddListener(OpenSystem);
        aboutBtn?.onClick.AddListener(OpenAbout);
        accountsBtn?.onClick.AddListener(OpenAccounts);
        renameBtn?.onClick.AddListener(OpenRenameDialog);
        renameConfirmBtn?.onClick.AddListener(ConfirmRename);
        renameCancelBtn?.onClick.AddListener(CloseRenameDialog);
        changePasswordBtn?.onClick.AddListener(OpenPasswordDialog);
        passwordOKBtn?.onClick.AddListener(ConfirmPassword);
        passwordCancelBtn?.onClick.AddListener(ClosePasswordDialog);

        HideAll();
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        HideAll();
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

    // ── Sidebar navigation ────────────────────────────────────────────────────

    private void OpenSystem()
    {
        HideAll();
        systemPanel?.SetActive(true);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.SettingsSystemOpened = true;
        ActivityLogManager.Log("Navigated to Settings → System", ActivityLogManager.EntryType.Action);
    }

    private void OpenAbout()
    {
        aboutPanel?.SetActive(true);
        RefreshPCName();
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.SettingsAboutOpened = true;
        ActivityLogManager.Log("Navigated to Settings → About", ActivityLogManager.EntryType.Action);
    }

    private void OpenAccounts()
    {
        HideAll();
        accountsPanel?.SetActive(true);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.SettingsAccountsOpened = true;
        ActivityLogManager.Log("Navigated to Settings → Accounts", ActivityLogManager.EntryType.Action);
    }

    // ── Rename PC ─────────────────────────────────────────────────────────────

    private void OpenRenameDialog()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (renameInput != null)
            renameInput.text = state?.ComputerName ?? "";
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
        RefreshPCName();
        ActivityLogManager.Log($"PC renamed to: {newName}", ActivityLogManager.EntryType.Action);
    }

    // ── Password ──────────────────────────────────────────────────────────────

    private void OpenPasswordDialog()
    {
        if (newPasswordInput  != null) newPasswordInput.text  = "";
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
        string pass1 = newPasswordInput?.text ?? "";
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void HideAll()
    {
        systemPanel?.SetActive(false);
        aboutPanel?.SetActive(false);
        accountsPanel?.SetActive(false);
        CloseRenameDialog();
        ClosePasswordDialog();
    }

    private void RefreshPCName()
    {
        if (pcNameTMP == null) return;
        var state = ServerVirtualOSManager.Instance?.ServerState;
        pcNameTMP.text = !string.IsNullOrEmpty(state?.ComputerName)
            ? state.ComputerName
            : "WIN-SERVER";
    }
}
