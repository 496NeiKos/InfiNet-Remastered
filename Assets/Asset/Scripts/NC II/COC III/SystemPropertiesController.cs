/*
 * ================================================================
 *  UNITY SETUP GUIDE — SystemPropertiesController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to a "System Properties Chain" GameObject (starts INACTIVE).
 *    Place as a child of the Desktop Panel's App Panels group,
 *    sibling of the Settings Panel.
 *
 *  HIERARCHY
 *    System Properties Chain              ← this script here (starts INACTIVE)
 *      │
 *      ├── System Properties Popup        → systemPropertiesPanel (starts INACTIVE)
 *      │     ├── Tab Header "Computer Name"  (static label)
 *      │     ├── DescriptionLabel TMP        (static: "Computer description:")
 *      │     ├── DescriptionInput            → computerDescriptionInput
 *      │     │     (visual only, no validation)
 *      │     ├── FullNameLabel TMP           (static: "Full computer name:")
 *      │     ├── FullComputerNameTMP         → fullComputerNameTMP (reads ClientPCData.ComputerName)
 *      │     ├── ChangeBtn                   → changeBtn
 *      │     │     Label: "Change..."
 *      │     └── Footer
 *      │           ├── OKBtn              → sysPropsOKBtn      (closes entire chain)
 *      │           ├── CancelBtn          → sysPropsCancelBtn  (closes entire chain)
 *      │           └── ApplyBtn           → sysPropsApplyBtn   (no-op — visual only)
 *      │
 *      ├── Computer Name/Domain Changes Popup  → computerNameChangesPanel (starts INACTIVE)
 *      │     ├── Heading TMP  (static: "Computer Name/Domain Changes")
 *      │     ├── SubLabel TMP (static: "You can change the name and the membership of this
 *      │     │                          computer. Changes might affect access to network resources.")
 *      │     ├── ComputerNameLabel TMP   (static: "Computer name:")
 *      │     ├── ComputerNameInput       → computerNameInput  (auto-filled; non-editable in sim)
 *      │     ├── FullNameLabel TMP       (static: "Full computer name:")
 *      │     ├── CompChangesFullNameTMP  → compChangesFullNameTMP  (mirrors computerNameInput)
 *      │     ├── MemberOfLabel TMP       (static: "Member of")
 *      │     ├── DomainRow
 *      │     │     ├── DomainToggle      → domainToggle
 *      │     │     │     Label: "Domain:"
 *      │     │     └── DomainInput       → domainInput  (starts INACTIVE until DomainToggle on)
 *      │     │           Placeholder: "e.g. TESDA.COM"
 *      │     ├── WorkgroupRow
 *      │     │     ├── WorkgroupToggle   → workgroupToggle  (default ON)
 *      │     │     │     Label: "Workgroup:"
 *      │     │     └── WorkgroupInput    → workgroupInput  (auto-filled "WORKGROUP")
 *      │     └── Footer
 *      │           ├── OKBtn             → compChangesOKBtn
 *      │           └── CancelBtn         → compChangesCancelBtn
 *      │
 *      ├── Domain Credentials Popup       → credentialsPanel (starts INACTIVE)
 *      │     ├── PromptTMP               → credentialsPromptTMP
 *      │     │     (dynamic: "Enter the name and password of an account
 *      │     │                with permission to join the domain.")
 *      │     ├── UsernameLabel TMP        (static: "User name:")
 *      │     ├── CredUsernameInput        → credUsernameInput
 *      │     ├── PasswordLabel TMP        (static: "Password:")
 *      │     ├── CredPasswordInput        → credPasswordInput  (Content Type: Password)
 *      │     ├── ErrorTMP                 → credentialsErrorTMP  (starts INACTIVE, red text)
 *      │     └── Footer
 *      │           ├── OKBtn             → credOKBtn
 *      │           └── CancelBtn         → credCancelBtn
 *      │
 *      ├── Welcome Popup                  → welcomePanel (starts INACTIVE)
 *      │     ├── WelcomeTMP              → welcomeTMP
 *      │     │     (dynamic: "Welcome to the [domain] domain.")
 *      │     └── OKBtn                   → welcomeOKBtn
 *      │
 *      └── Restart Notice Popup           → restartNoticePanel (starts INACTIVE)
 *            ├── NoticeTMP               → restartNoticeTMP
 *            │     (static: "You must restart your computer to apply these changes.")
 *            └── OKBtn                   → restartNoticeOKBtn
 *
 *  INSPECTOR ASSIGNMENTS
 *    All fields as listed above.
 *
 *  WIRING
 *    SettingsAppController.advancedSettingsBtn OnClick → SystemPropertiesController.OpenSystemProperties()
 *    (SettingsAppController holds a [SerializeField] reference to this script.)
 *
 *  CONSTRAINTS
 *    Only reachable from Client PC context. SettingsAppController hides
 *    the Advanced System Settings button when CurrentPC == Server.
 *
 *  HOW IT WORKS
 *    Five-panel modal chain for domain joining.
 *    Credentials are validated against ServerDeviceState:
 *      - IsDomainController must be true
 *      - domain name must match state.DomainName (case-insensitive)
 *      - username must match a UserData.Username with IsAdmin = true
 *      - password must match that user's UserData.Password
 *    On success: ClientPCData.DomainJoined = true, JoinedDomain stored.
 *    Restart Notice OK → CloseChain() → ServerVirtualOSManager.TriggerRestart().
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SystemPropertiesController : MonoBehaviour
{
    // ── System Properties Popup ───────────────────────────────────────────────

    [Header("System Properties")]
    [SerializeField] private GameObject     systemPropertiesPanel;
    [SerializeField] private TMP_InputField computerDescriptionInput;
    [SerializeField] private TMP_Text       fullComputerNameTMP;
    [SerializeField] private Button         changeBtn;
    [SerializeField] private Button         sysPropsOKBtn;
    [SerializeField] private Button         sysPropsCancelBtn;
    [SerializeField] private Button         sysPropsApplyBtn;

    // ── Computer Name/Domain Changes Popup ────────────────────────────────────

    [Header("Computer Name/Domain Changes")]
    [SerializeField] private GameObject     computerNameChangesPanel;
    [SerializeField] private TMP_InputField computerNameInput;
    [SerializeField] private TMP_Text       compChangesFullNameTMP;
    [SerializeField] private Toggle         domainToggle;
    [SerializeField] private TMP_InputField domainInput;
    [SerializeField] private Toggle         workgroupToggle;
    [SerializeField] private TMP_InputField workgroupInput;
    [SerializeField] private Button         compChangesOKBtn;
    [SerializeField] private Button         compChangesCancelBtn;

    // ── Domain Credentials Popup ──────────────────────────────────────────────

    [Header("Domain Credentials")]
    [SerializeField] private GameObject     credentialsPanel;
    [SerializeField] private TMP_Text       credentialsPromptTMP;
    [SerializeField] private TMP_InputField credUsernameInput;
    [SerializeField] private TMP_InputField credPasswordInput;
    [SerializeField] private TMP_Text       credentialsErrorTMP;
    [SerializeField] private Button         credOKBtn;
    [SerializeField] private Button         credCancelBtn;

    // ── Welcome Popup ─────────────────────────────────────────────────────────

    [Header("Welcome")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private TMP_Text   welcomeTMP;
    [SerializeField] private Button     welcomeOKBtn;

    // ── Restart Notice Popup ──────────────────────────────────────────────────

    [Header("Restart Notice")]
    [SerializeField] private GameObject restartNoticePanel;
    [SerializeField] private TMP_Text   restartNoticeTMP;
    [SerializeField] private Button     restartNoticeOKBtn;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        changeBtn?.onClick.AddListener(OpenComputerNameChanges);
        sysPropsOKBtn?.onClick.AddListener(CloseChain);
        sysPropsCancelBtn?.onClick.AddListener(CloseChain);
        sysPropsApplyBtn?.onClick.AddListener(() => { });

        domainToggle?.onValueChanged.AddListener(OnDomainToggleChanged);
        workgroupToggle?.onValueChanged.AddListener(OnWorkgroupToggleChanged);
        computerNameInput?.onValueChanged.AddListener(v =>
        {
            if (compChangesFullNameTMP != null) compChangesFullNameTMP.text = v;
        });

        compChangesOKBtn?.onClick.AddListener(OnComputerNameChangesOK);
        compChangesCancelBtn?.onClick.AddListener(CloseToSystemProperties);

        credOKBtn?.onClick.AddListener(OnCredentialsOK);
        credCancelBtn?.onClick.AddListener(CloseToComputerNameChanges);

        welcomeOKBtn?.onClick.AddListener(OpenRestartNotice);
        restartNoticeOKBtn?.onClick.AddListener(OnRestartNoticeOK);

        if (restartNoticeTMP != null)
            restartNoticeTMP.text =
                "You must restart your computer to apply these changes.\n\nClick OK to restart now.";

        HideAll();
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void OpenSystemProperties()
    {
        gameObject.SetActive(true);
        HideAll();

        string pcName = ServerVirtualOSManager.Instance?.ClientState?.ComputerName ?? "CLIENT-PC";
        if (fullComputerNameTMP    != null) fullComputerNameTMP.text    = pcName;
        if (computerDescriptionInput != null) computerDescriptionInput.text = "";

        systemPropertiesPanel?.SetActive(true);
        ActivityLogManager.Log("Opened System Properties", ActivityLogManager.EntryType.Action);
    }

    // ── System Properties → Computer Name Changes ─────────────────────────────

    private void OpenComputerNameChanges()
    {
        systemPropertiesPanel?.SetActive(false);

        string pcName = ServerVirtualOSManager.Instance?.ClientState?.ComputerName ?? "CLIENT-PC";
        if (computerNameInput      != null) { computerNameInput.text = pcName; computerNameInput.interactable = false; }
        if (compChangesFullNameTMP != null) compChangesFullNameTMP.text = pcName;

        // Default: Workgroup selected
        workgroupToggle?.SetIsOnWithoutNotify(true);
        domainToggle?.SetIsOnWithoutNotify(false);
        if (workgroupInput != null) workgroupInput.text = "WORKGROUP";
        domainInput?.gameObject.SetActive(false);
        workgroupInput?.gameObject.SetActive(true);

        credentialsErrorTMP?.gameObject.SetActive(false);
        computerNameChangesPanel?.SetActive(true);

        ActivityLogManager.Log("Opened Computer Name/Domain Changes", ActivityLogManager.EntryType.Action);
    }

    private void OnDomainToggleChanged(bool on)
    {
        if (!on) return;
        workgroupToggle?.SetIsOnWithoutNotify(false);
        domainInput?.gameObject.SetActive(true);
        workgroupInput?.gameObject.SetActive(false);
    }

    private void OnWorkgroupToggleChanged(bool on)
    {
        if (!on) return;
        domainToggle?.SetIsOnWithoutNotify(false);
        domainInput?.gameObject.SetActive(false);
        workgroupInput?.gameObject.SetActive(true);
    }

    private void OnComputerNameChangesOK()
    {
        bool isDomain = domainToggle != null && domainToggle.isOn;
        if (!isDomain)
        {
            // Workgroup selected — no domain join, just close back
            CloseToSystemProperties();
            return;
        }

        string typedDomain = domainInput != null ? domainInput.text.Trim() : "";
        if (string.IsNullOrEmpty(typedDomain))
        {
            ActivityLogManager.Log("Domain name is required.", ActivityLogManager.EntryType.Warning);
            return;
        }

        computerNameChangesPanel?.SetActive(false);

        if (credentialsPromptTMP != null)
            credentialsPromptTMP.text =
                "Enter the name and password of an account with\npermission to join the domain.";
        if (credUsernameInput  != null) credUsernameInput.text  = "";
        if (credPasswordInput  != null) credPasswordInput.text  = "";
        credentialsErrorTMP?.gameObject.SetActive(false);

        credentialsPanel?.SetActive(true);
        ActivityLogManager.Log($"Attempting to join domain: {typedDomain}", ActivityLogManager.EntryType.Action);
    }

    // ── Domain Credentials ────────────────────────────────────────────────────

    private void OnCredentialsOK()
    {
        string typedDomain   = domainInput       != null ? domainInput.text.Trim()        : "";
        string typedUsername = credUsernameInput  != null ? credUsernameInput.text.Trim()  : "";
        string typedPassword = credPasswordInput  != null ? credPasswordInput.text         : "";

        var state = ServerVirtualOSManager.Instance?.ServerState;
        string error = ValidateDomainJoin(state, typedDomain, typedUsername, typedPassword);

        if (!string.IsNullOrEmpty(error))
        {
            if (credentialsErrorTMP != null)
            {
                credentialsErrorTMP.text = error;
                credentialsErrorTMP.gameObject.SetActive(true);
            }
            if (credPasswordInput != null) credPasswordInput.text = "";
            return;
        }

        var client = ServerVirtualOSManager.Instance?.ClientState;
        if (client != null)
        {
            client.DomainJoined = true;
            client.JoinedDomain = typedDomain;
        }

        credentialsPanel?.SetActive(false);
        if (welcomeTMP != null) welcomeTMP.text = $"Welcome to the {typedDomain} domain.";
        welcomePanel?.SetActive(true);

        ActivityLogManager.Log($"Successfully joined domain: {typedDomain}", ActivityLogManager.EntryType.Action);
    }

    private static string ValidateDomainJoin(
        ServerDeviceState state, string domain, string username, string password)
    {
        if (state == null || !state.IsDomainController)
            return "The domain could not be found.\nMake sure the domain name is typed correctly.";

        if (!string.Equals(domain, state.DomainName, System.StringComparison.OrdinalIgnoreCase))
            return "The specified domain either does not exist or could not be contacted.";

        // Built-in Administrator account (password stored separately from UserAccounts)
        if (string.Equals(username, "Administrator", System.StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(state.AdminPassword) && password == state.AdminPassword)
                return "";
            return "Logon failure: unknown user name or bad password.";
        }

        // ADUC-created user added to Domain Admins group
        var admin = state.UserAccounts.Find(u =>
            string.Equals(u.Username, username, System.StringComparison.OrdinalIgnoreCase)
            && u.IsAdmin);

        if (admin == null || admin.Password != password)
            return "Logon failure: unknown user name or bad password.";

        return "";
    }

    // ── Welcome → Restart Notice ──────────────────────────────────────────────

    private void OpenRestartNotice()
    {
        welcomePanel?.SetActive(false);
        restartNoticePanel?.SetActive(true);
    }

    private void OnRestartNoticeOK()
    {
        CloseChain();
        ServerVirtualOSManager.Instance?.TriggerRestart();
    }

    // ── Navigation helpers ────────────────────────────────────────────────────

    private void CloseToSystemProperties()
    {
        computerNameChangesPanel?.SetActive(false);
        systemPropertiesPanel?.SetActive(true);
    }

    private void CloseToComputerNameChanges()
    {
        credentialsPanel?.SetActive(false);
        computerNameChangesPanel?.SetActive(true);
    }

    public void CloseChain()
    {
        HideAll();
        gameObject.SetActive(false);
    }

    private void HideAll()
    {
        systemPropertiesPanel?.SetActive(false);
        computerNameChangesPanel?.SetActive(false);
        credentialsPanel?.SetActive(false);
        welcomePanel?.SetActive(false);
        restartNoticePanel?.SetActive(false);
    }
}
