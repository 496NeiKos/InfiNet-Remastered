/*
 * ================================================================
 *  UNITY SETUP GUIDE — WindowsSecurityRDController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Windows Security RD Panel" (starts INACTIVE).
 *    This is Stage 2 of the 3-stage Remote Desktop chain.
 *    Place as a sibling of "Remote Desktop Connection Panel" inside
 *    the Desktop Panel's App Panels group.
 *
 *  HIERARCHY
 *    Windows Security RD Panel            ← this script here
 *      ├── TitleBar
 *      │     └── TitleTMP                 TMP_Text  "Windows Security"
 *      ├── Body
 *      │     ├── PromptTMP                TMP_Text  → promptTMP
 *      │     │     (auto-filled: "Connecting to: [ComputerName]")
 *      │     ├── UsernameLabel            TMP_Text  "User name:"
 *      │     ├── UsernameTMP              TMP_Text  → usernameTMP
 *      │     │     (read-only, auto-filled: "NETBIOS\username")
 *      │     ├── PasswordLabel            TMP_Text  "Password:"
 *      │     ├── PasswordInput            TMP_InputField  → passwordInput
 *      │     │     Content Type: Password
 *      │     │     Placeholder: "Password"
 *      │     ├── ErrorTMP                 TMP_Text  → errorTMP
 *      │     │     starts INACTIVE  (red or warning color recommended)
 *      │     └── RememberToggleRow        (HorizontalLayoutGroup)
 *      │           ├── RememberToggle     Toggle  → rememberToggle  isOn: false
 *      │           └── RememberLabel      TMP_Text  "Remember my credentials"
 *      └── Footer
 *            ├── OKBtn                    Button  → okBtn      label: "OK"
 *            └── CancelBtn               Button  → cancelBtn  label: "Cancel"
 *
 *  INSPECTOR ASSIGNMENTS
 *    promptTMP      → Body/PromptTMP
 *    usernameTMP    → Body/UsernameTMP
 *    passwordInput  → Body/PasswordInput
 *    errorTMP       → Body/ErrorTMP
 *    rememberToggle → Body/RememberToggleRow/RememberToggle
 *    okBtn          → Footer/OKBtn
 *    cancelBtn      → Footer/CancelBtn
 *    rdLoading      → RDLoadingController (next panel in chain, starts INACTIVE)
 *    rdConnection   → RDConnectionAppController (previous panel in chain)
 *
 *  HOW IT WORKS
 *    Open(computerName) is called by RDConnectionAppController after
 *    all Stage 1 validations pass. It fills promptTMP and usernameTMP,
 *    clears the password field, hides errorTMP, and shows the panel.
 *
 *    OK validates the typed password:
 *      • Empty password → "Password cannot be empty."
 *      • Looks up ClientPCData.CurrentLoggedInUser in ServerDeviceState.UserAccounts
 *      • Wrong password → "The credentials that were used to connect to
 *        [ComputerName] did not work. Please try again."
 *      • Valid → sets state.RDCredentialsEntered = true,
 *        closes this panel, calls RDLoadingController.Open(computerName)
 *
 *    Cancel → closes this panel, calls RDConnectionAppController.Open()
 *    (goes back to Stage 1).
 *
 *  IMPORTANT
 *    errorTMP must start INACTIVE in the scene (GameObject active = false).
 *    rememberToggle is visual only — its value is never read for validation.
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WindowsSecurityRDController : MonoBehaviour
{
    public static WindowsSecurityRDController Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text        promptTMP;
    [SerializeField] private TMP_Text        usernameTMP;
    [SerializeField] private TMP_InputField  passwordInput;
    [SerializeField] private TMP_Text        errorTMP;
    [SerializeField] private Toggle          rememberToggle;

    [Header("Footer")]
    [SerializeField] private Button okBtn;
    [SerializeField] private Button cancelBtn;

    [Header("References")]
    [SerializeField] private RDLoadingController       rdLoading;
    [SerializeField] private RDConnectionAppController rdConnection;

    private string _computerName = "";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        okBtn?.onClick.AddListener(OnOK);
        cancelBtn?.onClick.AddListener(OnCancel);

        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open(string computerName)
    {
        _computerName = computerName;

        gameObject.SetActive(true);
        if (passwordInput  != null) passwordInput.text = "";
        if (errorTMP       != null) errorTMP.gameObject.SetActive(false);
        if (rememberToggle != null) rememberToggle.SetIsOnWithoutNotify(false);
        if (promptTMP      != null) promptTMP.text = $"Connecting to: {computerName}";

        AutoFillUsername();
        ActivityLogManager.Log("Opened Windows Security — RD credentials prompt", ActivityLogManager.EntryType.Action);
    }

    // ── OK ────────────────────────────────────────────────────────────────────

    private void OnOK()
    {
        string typed = passwordInput != null ? passwordInput.text : "";

        if (string.IsNullOrEmpty(typed))
        {
            ShowError("Password cannot be empty.");
            return;
        }

        var mgr      = ServerVirtualOSManager.Instance;
        string user  = mgr?.ClientState?.CurrentLoggedInUser ?? "";
        var userData = mgr?.ServerState?.UserAccounts
            .Find(u => string.Equals(u.Username, user, System.StringComparison.OrdinalIgnoreCase));

        if (userData == null || typed != userData.Password)
        {
            if (passwordInput != null) passwordInput.text = "";
            ShowError($"The credentials that were used to connect to {_computerName} did not work.\nPlease try again.");
            return;
        }

        // Valid credentials
        var state = mgr?.ServerState;
        if (state != null) state.RDCredentialsEntered = true;

        gameObject.SetActive(false);
        rdLoading?.Open(_computerName);

        ActivityLogManager.Log($"RD credentials accepted — starting session to {_computerName}", ActivityLogManager.EntryType.Action);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    private void OnCancel()
    {
        gameObject.SetActive(false);
        rdConnection?.Open();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void AutoFillUsername()
    {
        if (usernameTMP == null) return;
        var mgr     = ServerVirtualOSManager.Instance;
        string user = mgr?.ClientState?.CurrentLoggedInUser ?? "";
        string nb   = RDConnectionAppController.GetNetBIOS(mgr?.ServerState);
        usernameTMP.text = string.IsNullOrEmpty(user) ? "" : $"{nb}\\{user}";
    }

    private void ShowError(string msg)
    {
        if (errorTMP == null) return;
        errorTMP.text = msg;
        errorTMP.gameObject.SetActive(true);
    }
}
