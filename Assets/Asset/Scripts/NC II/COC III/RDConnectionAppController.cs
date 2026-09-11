/*
 * ================================================================
 *  UNITY SETUP GUIDE — RDConnectionAppController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Remote Desktop Connection Panel" (starts INACTIVE).
 *    Desktop icon is INACTIVE until state.RDServicesInstalled = true.
 *    (Wire the desktop icon's SetActive to a script that checks state,
 *     or simply let ServerManagerController enable it after RD role install.)
 *
 *  HIERARCHY
 *    Remote Desktop Connection Panel    ← this script here
 *      ├── TitleBar / CloseBtn          → closeBtn
 *      ├── Body
 *      │     ├── ComputerLabel          (static: "Computer:")
 *      │     ├── IPInputField           → computerInput
 *      │     │     (auto-populated with DHCP scope start IP)
 *      │     ├── ConnectBtn             → connectBtn
 *      │     └── StatusLabel            → statusLabelTMP
 *      └── (ClientRDSessionController panel is a sibling or child,
 *            opened by this controller after connect)
 *
 *  INSPECTOR ASSIGNMENTS
 *    computerInput   → TMP_InputField for the target IP
 *    connectBtn      → Button
 *    statusLabelTMP  → TMP_Text showing "Connecting..." / error messages
 *    clientSession   → ClientRDSessionController (sibling panel)
 *
 *  DESKTOP ICON WIRING
 *    RD Connection desktop icon → Button OnClick → RDConnectionAppController.Open()
 *    Make the icon's GameObject start INACTIVE.
 *    In ServerManagerController, after RD Services is installed,
 *    call rdConnectionIcon.SetActive(true).
 *    Reference: rdIconObject field in ServerManagerController (add if needed).
 *
 *  HOW IT WORKS
 *    On Open(): auto-populates computerInput with the DHCP scope start IP.
 *    On Connect(): validates input is non-empty → opens ClientRDSessionController.
 *    Sets state.ClientConnected = true and state.RDSessionOpened = true.
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RDConnectionAppController : MonoBehaviour
{
    public static RDConnectionAppController Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Button         closeBtn;
    [SerializeField] private TMP_InputField computerInput;
    [SerializeField] private Button         connectBtn;
    [SerializeField] private TMP_Text       statusLabelTMP;

    [Header("References")]
    [SerializeField] private ClientRDSessionController clientSession;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        closeBtn?.onClick.AddListener(Close);
        connectBtn?.onClick.AddListener(Connect);

        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        if (statusLabelTMP != null) statusLabelTMP.text = "";
        AutoFillClientIP();
        ActivityLogManager.Log("Opened Remote Desktop Connection", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // ── Connect ───────────────────────────────────────────────────────────────

    private void Connect()
    {
        string ip = computerInput != null ? computerInput.text.Trim() : "";
        if (string.IsNullOrEmpty(ip))
        {
            if (statusLabelTMP != null) statusLabelTMP.text = "Please enter a computer name or IP address.";
            return;
        }

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null && !state.RDServicesInstalled)
        {
            if (statusLabelTMP != null) statusLabelTMP.text = "Remote Desktop Services are not installed.";
            return;
        }

        if (statusLabelTMP != null) statusLabelTMP.text = $"Connecting to {ip}...";

        if (state != null)
        {
            state.RDSessionOpened  = true;
            state.ClientConnected  = true;
        }

        // Hide this panel and open the client session
        gameObject.SetActive(false);
        clientSession?.OpenSession(ip);
        ActivityLogManager.Log($"Remote Desktop connected to: {ip}", ActivityLogManager.EntryType.Action);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void AutoFillClientIP()
    {
        if (computerInput == null) return;
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null && !string.IsNullOrEmpty(state.DHCPScopeStart))
            computerInput.text = state.DHCPScopeStart;
    }
}
