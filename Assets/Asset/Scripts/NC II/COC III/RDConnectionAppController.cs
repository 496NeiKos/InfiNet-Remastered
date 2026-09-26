/*
 * ================================================================
 *  UNITY SETUP GUIDE — RDConnectionAppController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Remote Desktop Connection Panel" (starts INACTIVE).
 *    This is Stage 1 of the 3-stage Remote Desktop chain.
 *    Place as a child of the Desktop Panel's App Panels group,
 *    alongside the other app panels.
 *
 *  WHEN THIS PANEL IS ACCESSIBLE
 *    The desktop icon that opens this panel (rdConnectionIcon in
 *    ServerVirtualOSManager) is only visible when ALL of:
 *      • currentPC == Client
 *      • ClientPCData.DomainJoined == true
 *      • The currently logged-in client user exists in UserAccounts
 *      • state.RDSessionOpened == false (session not yet established)
 *    (Managed by ServerVirtualOSManager.RefreshDesktopIcons)
 *
 *  HIERARCHY
 *    Remote Desktop Connection Panel       ← this script here  (start ACTIVE)
 *      ├── TitleBar
 *      │     ├── TitleTMP                  TMP_Text  "Remote Desktop Connection"
 *      │     └── CloseBtn                  Button  → closeBtn
 *      ├── Body
 *      │     ├── ComputerLabel             TMP_Text  "Computer:"
 *      │     ├── ComputerInput             TMP_InputField  → computerInput
 *      │     │     Placeholder: "Enter PC name or IP address"
 *      │     ├── HintTMP                   TMP_Text  → hintTMP
 *      │     │     (auto-populated: "Hint: [ServerComputerName]")
 *      │     ├── UsernameLabel             TMP_Text  "User name:"
 *      │     └── UsernameTMP               TMP_Text  → usernameTMP
 *      │           (read-only, auto-filled: "NETBIOS\username")
 *      ├── StatusTMP                       TMP_Text  → statusTMP
 *      │     (error/status messages — starts with text "")
 *      ├── Footer
 *      │     ├── ConnectBtn                Button  → connectBtn  label: "Connect"
 *      │     └── HelpBtn                   Button  → helpBtn     label: "Help"
 *      │           interactable: false
 *      └── Windows Security RD Panel       ← child panel (start ACTIVE — Awake hides it)
 *            └── RD Loading Panel          ← grandchild panel (start ACTIVE — Awake hides it)
 *
 *  NESTING RULE
 *    All three RD panels start ACTIVE in the editor so their Awake() runs at
 *    scene load and wires buttons. Each Awake calls SetActive(false) on itself.
 *    When Connect() validates successfully, this panel stays OPEN while
 *    Windows Security RD Panel (child) shows on top. This panel only closes
 *    itself after the full RD session is established (called by RDLoadingController).
 *
 *  INSPECTOR ASSIGNMENTS
 *    closeBtn        → TitleBar/CloseBtn
 *    computerInput   → Body/ComputerInput (TMP_InputField)
 *    hintTMP         → Body/HintTMP
 *    usernameTMP     → Body/UsernameTMP
 *    statusTMP       → StatusTMP
 *    connectBtn      → Footer/ConnectBtn
 *    helpBtn         → Footer/HelpBtn
 *    windowsSecurity → WindowsSecurityRDController on the child panel
 *
 *  WIRING
 *    rdConnectionIcon desktop Button → OnClick → RDConnectionAppController.Open()
 *    (Assign in the Inspector on the desktop icon's Button component.)
 *
 *  HOW IT WORKS
 *    Open(): fills hintTMP with server ComputerName, fills usernameTMP with
 *    NETBIOS\CurrentLoggedInUser, clears computerInput and statusTMP,
 *    sets state.RDConnectionPanelOpened = true.
 *
 *    Connect() runs 3 validation steps in order:
 *      1. computerInput not empty
 *         → "Please enter the computer name or IP address."
 *      2. Typed value matches ServerDeviceState.ComputerName (case-insensitive)
 *         OR matches the server's static IP string
 *         → "Remote Desktop can't connect to the remote computer.
 *            Make sure the computer name or IP address is correct."
 *      3. Both RDServicesInstalled == true AND RemoteDesktopEnabled == true
 *         → specific message per missing condition (see below)
 *    All pass → sets state.RDComputerNameEntered = true, closes this panel,
 *    calls WindowsSecurityRDController.Open(computerName).
 *
 *  VALIDATION STATUS MESSAGES
 *    Neither flag set:
 *      "Make sure to add the Remote Desktop role and enable Remote Desktop
 *       in Local Server → System Properties → Remote in the server
 *       before connecting."
 *    Role not installed only:
 *      "Make sure the Remote Desktop role is first installed in the server."
 *    RD not enabled only:
 *      "Make sure Remote Desktop is enabled in Local Server →
 *       System Properties → Remote."
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RDConnectionAppController : MonoBehaviour
{
    public static RDConnectionAppController Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Button          closeBtn;
    [SerializeField] private TMP_InputField  computerInput;
    [SerializeField] private TMP_Text        hintTMP;
    [SerializeField] private TMP_Text        usernameTMP;
    [SerializeField] private TMP_Text        statusTMP;
    [SerializeField] private Button          connectBtn;
    [SerializeField] private Button          helpBtn;

    [Header("References")]
    [SerializeField] private WindowsSecurityRDController windowsSecurity;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        closeBtn?.onClick.AddListener(Close);
        connectBtn?.onClick.AddListener(Connect);
        if (helpBtn != null) helpBtn.interactable = false;

        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);

        if (computerInput != null) computerInput.text = "";
        if (statusTMP     != null) statusTMP.text     = "";

        AutoFillHint();
        AutoFillUsername();

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.RDConnectionPanelOpened = true;

        ActivityLogManager.Log("Opened Remote Desktop Connection", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // ── Connect ───────────────────────────────────────────────────────────────

    private void Connect()
    {
        string input = computerInput != null ? computerInput.text.Trim() : "";

        // Step 1 — field must not be empty
        if (string.IsNullOrEmpty(input))
        {
            SetStatus("Please enter the computer name or IP address.");
            return;
        }

        var state = ServerVirtualOSManager.Instance?.ServerState;

        // Step 2 — must match server PC name or IP
        if (!MatchesServer(input, state))
        {
            SetStatus("Remote Desktop can't connect to the remote computer.\nMake sure the computer name or IP address is correct.");
            return;
        }

        // Step 3 — both server flags must be true
        bool roleInstalled = state?.RDServicesInstalled ?? false;
        bool rdEnabled     = state?.RemoteDesktopEnabled ?? false;

        if (!roleInstalled && !rdEnabled)
        {
            SetStatus("Make sure to add the Remote Desktop role and enable Remote Desktop in Local Server → System Properties → Remote in the server before connecting.");
            return;
        }
        if (!roleInstalled)
        {
            SetStatus("Make sure the Remote Desktop role is first installed in the server.");
            return;
        }
        if (!rdEnabled)
        {
            SetStatus("Make sure Remote Desktop is enabled in Local Server → System Properties → Remote.");
            return;
        }

        // All validations passed
        if (state != null) state.RDComputerNameEntered = true;

        // Do NOT close this panel — Windows Security RD Panel is a child and
        // will appear on top while this panel remains the active root of the chain.
        string computerName = !string.IsNullOrEmpty(state?.ComputerName) ? state.ComputerName : input;
        windowsSecurity?.Open(computerName);

        ActivityLogManager.Log($"RD Connection: validated — opening Windows Security for {input}", ActivityLogManager.EntryType.Action);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void AutoFillHint()
    {
        if (hintTMP == null) return;
        var state = ServerVirtualOSManager.Instance?.ServerState;
        string name = !string.IsNullOrEmpty(state?.ComputerName)
            ? state.ComputerName
            : "(Server PC name not set)";
        hintTMP.text = $"Hint: {name}";
    }

    private void AutoFillUsername()
    {
        if (usernameTMP == null) return;
        var mgr     = ServerVirtualOSManager.Instance;
        string user = mgr?.ClientState?.CurrentLoggedInUser ?? "";
        string nb   = GetNetBIOS(mgr?.ServerState);
        usernameTMP.text = string.IsNullOrEmpty(user) ? "" : $"{nb}\\{user}";
    }

    private static bool MatchesServer(string input, ServerDeviceState state)
    {
        if (state == null) return false;

        if (!string.IsNullOrEmpty(state.ComputerName) &&
            string.Equals(input, state.ComputerName, System.StringComparison.OrdinalIgnoreCase))
            return true;

        if (state.UseStaticIP && state.IPOctets != null && state.IPOctets.Length == 4
            && !string.IsNullOrEmpty(state.IPOctets[0]))
        {
            string ip = $"{state.IPOctets[0]}.{state.IPOctets[1]}.{state.IPOctets[2]}.{state.IPOctets[3]}";
            if (input == ip) return true;
        }

        return false;
    }

    internal static string GetNetBIOS(ServerDeviceState state)
    {
        if (state != null && !string.IsNullOrEmpty(state.NetBIOSName))
            return state.NetBIOSName.ToUpper();
        if (state != null && !string.IsNullOrEmpty(state.DomainName))
        {
            int dot = state.DomainName.IndexOf('.');
            return (dot > 0 ? state.DomainName.Substring(0, dot) : state.DomainName).ToUpper();
        }
        return "DOMAIN";
    }

    private void SetStatus(string msg)
    {
        if (statusTMP != null) statusTMP.text = msg;
    }
}
