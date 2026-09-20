/*
 * ================================================================
 *  UNITY SETUP GUIDE — RDConnectionAppController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Remote Desktop Connection Panel" (starts INACTIVE).
 *    Desktop icon is ONLY SHOWN when CurrentPC == Client.
 *    (Managed automatically by ServerVirtualOSManager.RefreshDesktopIcons)
 *
 *  HIERARCHY
 *    Remote Desktop Connection Panel    ← this script here
 *      ├── TitleBar / CloseBtn          → closeBtn
 *      ├── Body
 *      │     ├── ComputerLabel          (static: "Computer:")
 *      │     ├── ComputerInput          → computerInput
 *      │     │     (auto-populated with server's static IP on Open)
 *      │     ├── ConnectBtn             → connectBtn
 *      │     └── StatusLabel            → statusLabelTMP
 *      └── (ClientRDSessionController panel is a sibling or child,
 *            opened by this controller after successful connect)
 *
 *  INSPECTOR ASSIGNMENTS
 *    computerInput   → TMP_InputField for the target server IP
 *    connectBtn      → Button
 *    statusLabelTMP  → TMP_Text for "Connecting..." / error messages
 *    clientSession   → ClientRDSessionController (sibling panel)
 *
 *  WIRING
 *    RD Connection desktop icon → Button OnClick → RDConnectionAppController.Open()
 *    The icon starts INACTIVE. ServerVirtualOSManager.RefreshDesktopIcons()
 *    activates it when CurrentPC == Client.
 *
 *  HOW IT WORKS
 *    On Open(): auto-populates computerInput with the server's static IP
 *    (ServerDeviceState.IPOctets joined as an IP string).
 *    On Connect(): validates input is non-empty AND state.RDServicesInstalled == true.
 *    If both pass, sets state.RDSessionOpened = true and opens ClientRDSessionController.
 *    Only accessible from Client PC context (icon hidden on Server PC).
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
        AutoFillServerIP();
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
            if (statusLabelTMP != null)
                statusLabelTMP.text = "Please enter a computer name or IP address.";
            return;
        }

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || !state.RDServicesInstalled)
        {
            if (statusLabelTMP != null)
                statusLabelTMP.text = "Remote Desktop Services are not installed on the server.";
            return;
        }

        if (statusLabelTMP != null) statusLabelTMP.text = $"Connecting to {ip}...";

        state.RDSessionOpened = true;
        state.ClientConnected = true;

        gameObject.SetActive(false);
        clientSession?.OpenSession(ip);

        ActivityLogManager.Log($"Remote Desktop connected to server at: {ip}", ActivityLogManager.EntryType.Action);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void AutoFillServerIP()
    {
        if (computerInput == null) return;
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null && state.UseStaticIP && state.IPOctets != null && state.IPOctets.Length == 4
            && !string.IsNullOrEmpty(state.IPOctets[0]))
        {
            computerInput.text =
                $"{state.IPOctets[0]}.{state.IPOctets[1]}.{state.IPOctets[2]}.{state.IPOctets[3]}";
        }
    }
}
