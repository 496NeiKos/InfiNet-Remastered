/*
 * ================================================================
 *  UNITY SETUP GUIDE — LocalServerPanelController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to the "Local Server Panel" GameObject, which is a direct
 *    child of "Main Panel" inside "Server Manager Panel".
 *    Starts INACTIVE. Activated by ServerManagerController.ShowLocalServer().
 *
 *  HIERARCHY
 *    Local Server Panel                   ← this script here (INACTIVE)
 *      │
 *      ├── PropertiesGroup1               Image (group background)
 *      │     ├── GroupHeaderTMP           TMP_Text  "PROPERTIES"
 *      │     ├── ComputerNameRow          (label + value pair)
 *      │     │     ├── LabelTMP           TMP_Text  "Computer name:"
 *      │     │     └── ValueTMP           TMP_Text  → computerNameTMP  (dynamic)
 *      │     ├── DomainRow
 *      │     │     ├── LabelTMP           TMP_Text  "Domain:"
 *      │     │     └── ValueTMP           TMP_Text  → domainTMP  (dynamic)
 *      │     ├── LastUpdatesRow
 *      │     │     ├── LabelTMP           TMP_Text  "Last installed updates:"
 *      │     │     └── ValueTMP           TMP_Text  "Never"  (static — leave in scene)
 *      │     ├── WindowsUpdateRow
 *      │     │     ├── LabelTMP           TMP_Text  "Windows Update:"
 *      │     │     └── ValueTMP           TMP_Text  "Not configured"  (static)
 *      │     └── LastCheckedRow
 *      │           ├── LabelTMP           TMP_Text  "Last checked for updates:"
 *      │           └── ValueTMP           TMP_Text  "Never"  (static)
 *      │
 *      ├── PropertiesGroup2               Image (group background)
 *      │     ├── FirewallRow
 *      │     │     ├── LabelTMP           TMP_Text  "Windows Defender Firewall:"
 *      │     │     └── ValueTMP           TMP_Text  "Domain: On, Private: On"  (static)
 *      │     ├── RemoteManagementRow
 *      │     │     ├── LabelTMP           TMP_Text  "Remote management:"
 *      │     │     └── ValueTMP           TMP_Text  "Enabled"  (static)
 *      │     ├── RemoteDesktopRow         ← interactive row
 *      │     │     ├── LabelTMP           TMP_Text  "Remote Desktop:"
 *      │     │     └── RemoteDesktopBtn   Button  → remoteDesktopBtn
 *      │     │           └── ValueTMP     TMP_Text  → remoteDesktopValueTMP  (dynamic)
 *      │     ├── NICTeamingRow
 *      │     │     ├── LabelTMP           TMP_Text  "NIC Teaming:"
 *      │     │     └── ValueTMP           TMP_Text  "Disabled"  (static)
 *      │     ├── EthernetRow
 *      │     │     ├── LabelTMP           TMP_Text  "Ethernet:"
 *      │     │     └── ValueTMP           TMP_Text  → ethernetTMP  (dynamic)
 *      │     ├── DefenderRow
 *      │     │     ├── LabelTMP           TMP_Text  "Windows Defender Antivirus:"
 *      │     │     └── ValueTMP           TMP_Text  "Real-Time Protection: On"  (static)
 *      │     ├── FeedbackRow
 *      │     │     ├── LabelTMP           TMP_Text  "Feedback & Diagnostics:"
 *      │     │     └── ValueTMP           TMP_Text  "On"  (static)
 *      │     ├── IESecurityRow
 *      │     │     ├── LabelTMP           TMP_Text  "IE Enhanced Security Config:"
 *      │     │     └── ValueTMP           TMP_Text  "On"  (static)
 *      │     ├── TimeZoneRow
 *      │     │     ├── LabelTMP           TMP_Text  "Time zone:"
 *      │     │     └── ValueTMP           TMP_Text  "(UTC+08:00) Manila"  (static)
 *      │     └── ProductIDRow
 *      │           ├── LabelTMP           TMP_Text  "Product ID:"
 *      │           └── ValueTMP           TMP_Text  "00000-00000-00000-AAOEM"  (static)
 *      │
 *      └── PropertiesGroup3               Image (group background)
 *            ├── OSVersionRow
 *            │     ├── LabelTMP           TMP_Text  "Operating system version:"
 *            │     └── ValueTMP           TMP_Text  "Microsoft Windows Server 2019 Standard"  (static)
 *            ├── HardwareInfoRow
 *            │     ├── LabelTMP           TMP_Text  "Hardware information:"
 *            │     └── ValueTMP           TMP_Text  "Standard PC"  (static)
 *            ├── ProcessorRow
 *            │     ├── LabelTMP           TMP_Text  "Processors:"
 *            │     └── ValueTMP           TMP_Text  "Intel(R) Core(TM) i5-9600K @ 3.70GHz"  (static)
 *            ├── RAMRow
 *            │     ├── LabelTMP           TMP_Text  "Installed memory (RAM):"
 *            │     └── ValueTMP           TMP_Text  "8.00 GB"  (static)
 *            └── DiskSpaceRow
 *                  ├── LabelTMP           TMP_Text  "Total disk space:"
 *                  └── ValueTMP           TMP_Text  "500.00 GB"  (static)
 *
 *  NOTE ON STATIC ROWS
 *    All rows except ComputerName, Domain, Ethernet, and RemoteDesktop are purely
 *    visual — set their TMP text directly in the scene Inspector and leave them alone.
 *    Only the four dynamic rows have references in this script.
 *
 *  INSPECTOR ASSIGNMENTS
 *    computerNameTMP        → PropertiesGroup1/ComputerNameRow/ValueTMP
 *    domainTMP              → PropertiesGroup1/DomainRow/ValueTMP
 *    ethernetTMP            → PropertiesGroup2/EthernetRow/ValueTMP
 *    remoteDesktopValueTMP  → PropertiesGroup2/RemoteDesktopRow/RemoteDesktopBtn/ValueTMP
 *    remoteDesktopBtn       → PropertiesGroup2/RemoteDesktopRow/RemoteDesktopBtn
 *    systemPropertiesRemote → SystemPropertiesRemoteController component
 *                             (on "System Properties Remote Panel", sibling of "Main Panel")
 *
 *  HOW IT WORKS
 *    ServerManagerController.ShowLocalServer() activates this panel and calls Open().
 *    Open() reads ServerDeviceState and binds the four dynamic fields.
 *    After the player changes Remote Desktop in System Properties Remote and clicks
 *    OK or Apply, SystemPropertiesRemoteController calls Refresh() on this script
 *    so the button label updates without reopening the panel.
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocalServerPanelController : MonoBehaviour
{
    [Header("Dynamic Fields — Group 1")]
    [SerializeField] private TMP_Text computerNameTMP;
    [SerializeField] private TMP_Text domainTMP;

    [Header("Dynamic Fields — Group 2")]
    [SerializeField] private TMP_Text ethernetTMP;
    [SerializeField] private TMP_Text remoteDesktopValueTMP;
    [SerializeField] private Button   remoteDesktopBtn;

    [Header("References")]
    [SerializeField] private SystemPropertiesRemoteController systemPropertiesRemote;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        remoteDesktopBtn?.onClick.AddListener(() => systemPropertiesRemote?.Open());
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by ServerManagerController.ShowLocalServer(). Panel is already activated
    /// by SetMainPanel() before this is called — just populate the fields.
    /// </summary>
    public void Open()
    {
        Refresh();
    }

    /// <summary>
    /// Re-reads state and updates all dynamic fields.
    /// Called by SystemPropertiesRemoteController after OK or Apply.
    /// </summary>
    public void Refresh()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;

        if (computerNameTMP != null)
            computerNameTMP.text = !string.IsNullOrEmpty(state.ComputerName)
                ? state.ComputerName
                : "SERVER";

        if (domainTMP != null)
            domainTMP.text = state.IsDomainController && !string.IsNullOrEmpty(state.DomainName)
                ? state.DomainName
                : "WORKGROUP";

        if (ethernetTMP != null)
        {
            bool hasIP = state.UseStaticIP
                      && state.IPOctets != null
                      && state.IPOctets.Length == 4
                      && !string.IsNullOrEmpty(state.IPOctets[0]);
            ethernetTMP.text = hasIP ? string.Join(".", state.IPOctets) : "Not connected";
        }

        if (remoteDesktopValueTMP != null)
            remoteDesktopValueTMP.text = state.RemoteDesktopEnabled ? "Enabled" : "Disabled";
    }
}
