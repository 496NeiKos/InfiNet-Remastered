/*
 * ================================================================
 *  UNITY SETUP GUIDE — ClientRDSessionController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Client RD Session Panel" (starts INACTIVE).
 *    This panel simulates the client's desktop as seen through a Remote Desktop session.
 *    It is a full-screen (or near-full-screen) panel with a title bar showing
 *    the connected IP and a simulated Windows 10 client desktop inside.
 *
 *  HIERARCHY
 *    Client RD Session Panel            ← this script here
 *      ├── Title Bar
 *      │     ├── ConnectionLabel       → connectionLabelTMP ("192.168.x.x - Remote Desktop")
 *      │     └── DisconnectBtn         → disconnectBtn
 *      └── Client Desktop (simulated Windows 10 desktop inside the RD window)
 *            ├── Desktop Background
 *            ├── Taskbar
 *            │     └── StartBtn (optional)
 *            └── Desktop Icons
 *                  ├── CMDIcon         → cmdIcon (Button → opens ClientCMDPanel)
 *                  ├── FileExplorerIcon → fileExplorerIcon (Button → opens FileExplorerPanel)
 *                  └── DevicesIcon     → devicesIcon (Button → opens DevicesAndPrintersPanel)
 *            ├── Client CMD Panel      → clientCMDPanel (starts INACTIVE)
 *            │     ├── OutputTMP       → cmdOutputTMP
 *            │     ├── InputField      → cmdInputField
 *            │     ├── EnterBtn        → cmdEnterBtn
 *            │     └── CloseBtn        → cmdCloseBtn
 *            ├── File Explorer Panel   → fileExplorerPanel (starts INACTIVE)
 *            │     ├── DocumentsRow   → documentsRowTMP (shows redirect path or local)
 *            │     └── CloseBtn        → feCloseBtn
 *            └── Devices & Printers Panel → devicesPanel (starts INACTIVE)
 *                  ├── PrinterRow     → printerRowTMP (shows shared printer or empty)
 *                  └── CloseBtn        → dpCloseBtn
 *
 *  INSPECTOR ASSIGNMENTS
 *    All fields as listed above.
 *
 *  HOW IT WORKS
 *    OpenSession(ip) sets the connection label and opens this panel.
 *    CMD panel: player types commands — "ipconfig" shows auto-assigned IP from DHCP scope.
 *               "ping [server IP]" shows reply simulation.
 *    File Explorer: shows Documents folder path (redirected to \\SERVER\... if configured).
 *    Devices and Printers: shows the shared printer if state.PrinterShared = true.
 *    Each verified action sets the corresponding flag in ServerDeviceState.
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClientRDSessionController : MonoBehaviour
{
    public static ClientRDSessionController Instance { get; private set; }

    [Header("Session")]
    [SerializeField] private TMP_Text connectionLabelTMP;
    [SerializeField] private Button   disconnectBtn;

    [Header("CMD Panel")]
    [SerializeField] private GameObject    clientCMDPanel;
    [SerializeField] private TMP_Text      cmdOutputTMP;
    [SerializeField] private TMP_InputField cmdInputField;
    [SerializeField] private Button        cmdEnterBtn;
    [SerializeField] private Button        cmdCloseBtn;
    [SerializeField] private Button        cmdIcon;

    [Header("File Explorer Panel")]
    [SerializeField] private GameObject fileExplorerPanel;
    [SerializeField] private TMP_Text   documentsRowTMP;
    [SerializeField] private Button     feCloseBtn;
    [SerializeField] private Button     fileExplorerIcon;

    [Header("Devices and Printers Panel")]
    [SerializeField] private GameObject devicesPanel;
    [SerializeField] private TMP_Text   printerRowTMP;
    [SerializeField] private Button     dpCloseBtn;
    [SerializeField] private Button     devicesIcon;

    private string _connectedIP = "";
    private readonly List<string> _cmdLines = new List<string>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        disconnectBtn?.onClick.AddListener(Disconnect);
        cmdIcon?.onClick.AddListener(OpenCMD);
        cmdEnterBtn?.onClick.AddListener(ExecuteCommand);
        cmdCloseBtn?.onClick.AddListener(CloseCMD);
        fileExplorerIcon?.onClick.AddListener(OpenFileExplorer);
        feCloseBtn?.onClick.AddListener(CloseFileExplorer);
        devicesIcon?.onClick.AddListener(OpenDevicesAndPrinters);
        dpCloseBtn?.onClick.AddListener(CloseDevicesAndPrinters);

        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void OpenSession(string ip)
    {
        _connectedIP = ip;
        _cmdLines.Clear();
        gameObject.SetActive(true);
        clientCMDPanel?.SetActive(false);
        fileExplorerPanel?.SetActive(false);
        devicesPanel?.SetActive(false);

        if (connectionLabelTMP != null)
            connectionLabelTMP.text = $"{ip} — Remote Desktop Connection";

        ActivityLogManager.Log($"RD Session opened — client: {ip}", ActivityLogManager.EntryType.Action);
    }

    public void Disconnect()
    {
        gameObject.SetActive(false);
        ActivityLogManager.Log("RD Session disconnected.", ActivityLogManager.EntryType.Action);
    }

    // ── CMD ───────────────────────────────────────────────────────────────────

    private void OpenCMD()
    {
        clientCMDPanel?.SetActive(true);
        RefreshCMDOutput();
        if (cmdInputField != null) cmdInputField.text = "";
    }

    private void CloseCMD()
    {
        clientCMDPanel?.SetActive(false);
    }

    private void ExecuteCommand()
    {
        string cmd = cmdInputField != null ? cmdInputField.text.Trim().ToLower() : "";
        if (string.IsNullOrEmpty(cmd)) return;

        _cmdLines.Add($"C:\\Users\\Client> {cmdInputField.text}");

        if (cmd == "ipconfig" || cmd == "ipconfig /all")
            ExecuteIPConfig();
        else if (cmd.StartsWith("ping"))
            ExecutePing(cmd);
        else
            _cmdLines.Add($"'{cmd}' is not recognized as an internal or external command.");

        if (cmdInputField != null) cmdInputField.text = "";
        RefreshCMDOutput();
    }

    private void ExecuteIPConfig()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        string assignedIP = !string.IsNullOrEmpty(state?.DHCPScopeStart)
            ? state.DHCPScopeStart : "192.168.1.100";
        string gateway = state?.UseStaticIP == true && !string.IsNullOrEmpty(state.GatewayOctets[3])
            ? $"{state.GatewayOctets[0]}.{state.GatewayOctets[1]}.{state.GatewayOctets[2]}.{state.GatewayOctets[3]}"
            : "192.168.1.1";

        _cmdLines.Add("");
        _cmdLines.Add("Ethernet adapter Local Area Connection:");
        _cmdLines.Add($"   IPv4 Address. . . . . . : {assignedIP}");
        _cmdLines.Add( "   Subnet Mask . . . . . . : 255.255.255.0");
        _cmdLines.Add($"   Default Gateway . . . . : {gateway}");
        _cmdLines.Add($"   DHCP Server . . . . . . : {GetServerIP(state)}");
        _cmdLines.Add("");

        if (state != null) state.ClientDHCPVerified = true;
        ActivityLogManager.Log("Client ipconfig executed — DHCP IP verified", ActivityLogManager.EntryType.Action);
    }

    private void ExecutePing(string cmd)
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        string serverIP = GetServerIP(state);
        string target   = cmd.Replace("ping", "").Trim();
        if (string.IsNullOrEmpty(target)) target = serverIP;

        _cmdLines.Add("");
        _cmdLines.Add($"Pinging {target} with 32 bytes of data:");
        _cmdLines.Add($"Reply from {target}: bytes=32 time<1ms TTL=128");
        _cmdLines.Add($"Reply from {target}: bytes=32 time<1ms TTL=128");
        _cmdLines.Add($"Reply from {target}: bytes=32 time<1ms TTL=128");
        _cmdLines.Add($"Reply from {target}: bytes=32 time<1ms TTL=128");
        _cmdLines.Add("");
        _cmdLines.Add($"Ping statistics for {target}:");
        _cmdLines.Add("    Packets: Sent = 4, Received = 4, Lost = 0 (0% loss)");
        _cmdLines.Add("");

        if (state != null) state.ConnectivityVerified = true;
        ActivityLogManager.Log($"Client ping to {target} — connectivity verified", ActivityLogManager.EntryType.Action);
    }

    private void RefreshCMDOutput()
    {
        if (cmdOutputTMP == null) return;
        cmdOutputTMP.text = string.Join("\n", _cmdLines);
    }

    // ── File Explorer ─────────────────────────────────────────────────────────

    private void OpenFileExplorer()
    {
        fileExplorerPanel?.SetActive(true);

        var state      = ServerVirtualOSManager.Instance?.ServerState;
        string serverName = !string.IsNullOrEmpty(state?.ComputerName) ? state.ComputerName : "SERVER";
        bool anyRedirect = state != null && state.GroupPolicies.Exists(g => g.DocumentsRedirectSet);

        if (documentsRowTMP != null)
        {
            if (anyRedirect && state.SharedFolders.Count > 0)
            {
                var gpo     = state.GroupPolicies.Find(g => g.DocumentsRedirectSet);
                string path = gpo != null && !string.IsNullOrEmpty(gpo.DocumentsRedirectPath)
                              ? gpo.DocumentsRedirectPath
                              : $"\\\\{serverName}\\{state.SharedFolders[0].ShareName}";
                documentsRowTMP.text = $"Documents → {path} (Network Location)";
                if (state != null) state.FolderRedirectionVerified = true;
                ActivityLogManager.Log("Folder Redirection verified in client File Explorer",
                    ActivityLogManager.EntryType.Action);
            }
            else
            {
                documentsRowTMP.text = "Documents → C:\\Users\\Client\\Documents (Local)";
            }
        }
    }

    private void CloseFileExplorer() => fileExplorerPanel?.SetActive(false);

    // ── Devices and Printers ──────────────────────────────────────────────────

    private void OpenDevicesAndPrinters()
    {
        devicesPanel?.SetActive(true);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        string serverName = !string.IsNullOrEmpty(state?.ComputerName) ? state.ComputerName : "SERVER";

        if (printerRowTMP != null)
        {
            if (state != null && state.PrinterShared)
            {
                printerRowTMP.text = $"\\\\{serverName}\\{state.PrinterShareName}  [Network Printer — Ready]";
                state.PrinterVerified = true;
                ActivityLogManager.Log("Shared printer verified in client Devices and Printers",
                    ActivityLogManager.EntryType.Action);
            }
            else
            {
                printerRowTMP.text = "No network printers found.";
            }
        }
    }

    private void CloseDevicesAndPrinters() => devicesPanel?.SetActive(false);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GetServerIP(ServerDeviceState state)
    {
        if (state == null || !state.UseStaticIP) return "192.168.1.1";
        return $"{state.IPOctets[0]}.{state.IPOctets[1]}.{state.IPOctets[2]}.{state.IPOctets[3]}";
    }
}
