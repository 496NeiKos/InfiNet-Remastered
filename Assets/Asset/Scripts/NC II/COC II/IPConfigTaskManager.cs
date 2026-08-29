/*
 * ================================================================
 *  UNITY SETUP GUIDE — IPConfigTaskManager  (COC II, Category 2)
 * ================================================================
 *
 *  PURPOSE
 *    Drives the 74-task "IP Configuration" sequence.
 *    Mirrors the NetworkCableTaskManager pattern: 3-task sliding
 *    window, latch-based conditions, flash-on-complete.
 *
 *  TASKS — PART 1 (original 17)
 *    [ 0] Deploy at least 1 configurator device, Router, and Access Point
 *    [ 1] Connect all deployed devices with logical network cables
 *    [ 2] Install port cable in all connected device detail views
 *    [ 3] Factory reset both the Router and Access Point
 *    [ 4] Enter Virtual OS from the configured device's detail view
 *    [ 5] Open Chrome and navigate to the Router login page
 *    [ 6] Log in with "admin" to enter the TP-Link WebUI
 *    [ 7] Configure the SSID and Pre-Shared Key in the Wireless tab
 *    [ 8] Save wireless configuration and navigate to the LAN tab
 *    [ 9] Configure DHCP and IP Address, then save to finish Router config
 *    [10] Open Chrome and type "dlinkap" to enter the D-Link AP WebUI
 *    [11] Configure Wireless Name (SSID), Security Mode (WPA-Personal), Pre-Shared Key
 *    [12] Save wireless settings and navigate to the Network tab
 *    [13] Configure LAN: Static IP, IP/Subnet/Gateway/DNS (192.168.100.x)
 *    [14] Save the Access Point configuration
 *    [15] Exit Virtual OS and unplug port cables from Router and Access Point
 *    [16] Connect Router and Access Point from Patch Panel to Switch
 *
 *  TASKS — PART 2 (tasks 17–73, indices into taskObjects[])
 *    [17] Deploy the remaining devices (both Desktops and Laptop)
 *    [18] Connect LAN cable (Phase 1) to the newly deployed desktops
 *    [19] Enter detail view of new desktops and plug Phase 2 cable into port
 *    [20] Connect LAN cable to Patch Panel and Switch; plug Phase 2 cables into their ports
 *    --- SERVER DESKTOP (whichever desktop the student enters first) ---
 *    [21] Enter the Virtual OS of one of the two deployed desktops (server)
 *    [22] Open Network and Sharing Center
 *    [23] Click Change Adapter Settings and open the Ethernet Properties panel
 *    [24] Uncheck IPv6, select IPv4, and click Properties
 *    [25] Toggle Use the following IP address to enable manual config
 *    [26] Input "192.168.100.12" for this server device's IP address
 *    [27] Input the router's IP address in the Default Gateway field
 *    [28] Input this device's IP address in the Preferred DNS field
 *    [29] Click OK to save the IP configuration
 *    [30] Close Network Properties and click Home to return to main content
 *    [31] Click Change Advanced Sharing Settings
 *    [32] Toggle Guest/Public: turn on Network Discovery and File and Printer Sharing
 *    [33] Toggle All Networks: turn on Public Folder Sharing, turn off Password Protected Sharing
 *    [34] Click Save Changes then click Return
 *    [35] Click Windows Defender Firewall, then click Turn Firewall On and Off
 *    [36] Toggle Private and Public Network Settings both to Off
 *    [37] Close Network and Sharing Center and exit the server desktop Virtual OS
 *    --- SECOND DESKTOP ---
 *    [38] Enter the Virtual OS of the other desktop (not the server)
 *    [39] Open Network and Sharing Center
 *    [40] Click Change Adapter Settings and open the Ethernet Properties panel
 *    [41] Uncheck IPv6, select IPv4, and click Properties
 *    [42] Toggle Use the following IP address to enable manual config
 *    [43] Input "192.168.100.13" for this desktop's IP address
 *    [44] Input the router's IP address in the Default Gateway field
 *    [45] Input the server desktop's IP address in the Preferred DNS field
 *    [46] Click OK to save the IP configuration
 *    [47] Close Network Properties and click Home to return to main content
 *    [48] Click Change Advanced Sharing Settings
 *    [49] Toggle Guest/Public: turn on Network Discovery and File and Printer Sharing
 *    [50] Toggle All Networks: turn on Public Folder Sharing, turn off Password Protected Sharing
 *    [51] Click Save Changes then click Return
 *    [52] Click Windows Defender Firewall, then click Turn Firewall On and Off
 *    [53] Toggle Private and Public Network Settings both to Off
 *    [54] Close Network and Sharing Center and exit the second desktop Virtual OS
 *    --- LAPTOP ---
 *    [55] Enter the Virtual OS of the Laptop
 *    [56] Click the WiFi icon and connect to the Access Point network
 *    [57] Enter the security key and connect to WiFi
 *    [58] Close Internet Panel and open Network and Sharing Center
 *    [59] Click Change Adapter Settings and open the Ethernet Properties panel
 *    [60] Uncheck IPv6, select IPv4, and click Properties
 *    [61] Toggle Use the following IP address to enable manual config
 *    [62] Input "192.168.100.14" for the laptop's IP address
 *    [63] Input the router's IP address in the Default Gateway field
 *    [64] Input the server desktop's IP address in the Preferred DNS field
 *    [65] Click OK to save the IP configuration
 *    [66] Close Network Properties and click Home to return to main content
 *    [67] Click Change Advanced Sharing Settings
 *    [68] Toggle Guest/Public: turn on Network Discovery and File and Printer Sharing
 *    [69] Toggle All Networks: turn on Public Folder Sharing, turn off Password Protected Sharing
 *    [70] Click Save Changes then click Return
 *    [71] Click Windows Defender Firewall, then click Turn Firewall On and Off
 *    [72] Toggle Private and Public Network Settings both to Off
 *    [73] Close Network and Sharing Center and exit the Laptop Virtual OS
 *
 *  INSPECTOR SETUP
 *    Task UI
 *      taskParent          → Vertical Layout Group parent inside IPConfig panel
 *      finishedParent      → off-screen sibling of taskParent (NOT nested inside it)
 *      taskObjects[0..73]  → 74 TMP text GameObjects in order above
 *    Completion UI
 *      allTasksCompletedText → (optional) TMP shown when all 74 tasks done
 *    Completion Routing
 *      topicManagerCompletionIndex → -1 to disable
 *    Hardware Holders (Task 0 — initial deployment)
 *      computer1Holder / computer2Holder / laptopHolder → NetworkHardwareHolder per device
 *      routerHolder / apHolder / switchHolder           → NetworkHardwareHolder per device
 *    Device Ports (Tasks 1 & 18)
 *      computer1Port / computer2Port / laptopPort → NetworkDevicePort per device
 *      routerPort / apPort / switchPort           → NetworkDevicePort per device
 *    Phase 2 Managers (Tasks 2, 19, 20)
 *      computer1Phase2 / computer2Phase2 / laptopPhase2 → NetworkDevicePhase2Manager per device
 *      routerPhase2 / apPhase2 / patchPanelPhase2 / switchPhase2 → NetworkDevicePhase2Manager
 *    Reset Controllers (Task 3)
 *      routerReset → RouterResetController
 *      apReset     → AccessPointResetController
 *    Router WebUI (Tasks 7–9)
 *      tpLinkTabManager → TPLinkTabManager on "Page 2 - Main"
 *    AP WebUI (Tasks 11–14)
 *      dLinkAPManager → DLinkAPManager on "DLink AP Page"
 *    Virtual OS Panels (Tasks 21–73)
 *      networkSharingCenter       → NetworkSharingCenterController
 *      ethernetPropertiesController → EthernetPropertiesController on EthernetPropertiesPanel
 *      ipv4PropertiesController   → IPv4PropertiesController on IPv4PropertiesPanel
 *      advancedSharingController  → AdvancedSharingController on Advance Sharing Settings Panel
 *      firewallController         → FirewallController on Windows Defender Firewall Panel
 *      internetPanelController    → InternetPanelController on Internet Panel
 * ================================================================
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class IPConfigTaskManager : MonoBehaviour, ITaskCategory
{
    public static IPConfigTaskManager Instance { get; private set; }

    public static event Action OnTasksUpdated;

    private static readonly Color GoldColor = new Color(1f, 0.843f, 0f);

    // ── Inspector — Task UI ────────────────────────────────────────────────────────────

    [Header("Task UI")]
    [SerializeField] private Transform    taskParent;
    [Tooltip("Off-screen sibling of taskParent — do NOT nest inside taskParent.")]
    [SerializeField] private Transform    finishedParent;
    [Tooltip("Exactly 74 task GameObjects in the order listed in the header comment.")]
    [SerializeField] private GameObject[] taskObjects;

    [Header("Completion UI")]
    [Tooltip("(Optional) TMP shown after all 74 tasks are complete.")]
    [SerializeField] private TextMeshProUGUI allTasksCompletedText;

    [Header("Completion Routing")]
    [Tooltip("Index passed to TopicManager.MarkTopicComplete() on full completion. -1 = disabled.")]
    [SerializeField] private int topicManagerCompletionIndex = -1;

    // ── Inspector — Hardware Holders (Task 0) ──────────────────────────────────────────

    [Header("Hardware Holders — Configurator (at least one must be deployed)")]
    [SerializeField] private NetworkHardwareHolder computer1Holder;
    [SerializeField] private NetworkHardwareHolder computer2Holder;
    [SerializeField] private NetworkHardwareHolder laptopHolder;

    [Header("Hardware Holders — Network Devices")]
    [SerializeField] private NetworkHardwareHolder routerHolder;
    [SerializeField] private NetworkHardwareHolder apHolder;
    [SerializeField] private NetworkHardwareHolder switchHolder;

    // ── Inspector — Device Ports (Tasks 1 & 18) ───────────────────────────────────────

    [Header("Device Ports — Configurator")]
    [SerializeField] private NetworkDevicePort computer1Port;
    [SerializeField] private NetworkDevicePort computer2Port;
    [SerializeField] private NetworkDevicePort laptopPort;

    [Header("Device Ports — Network Devices")]
    [SerializeField] private NetworkDevicePort routerPort;
    [SerializeField] private NetworkDevicePort apPort;
    [SerializeField] private NetworkDevicePort switchPort;
    [SerializeField] private NetworkDevicePort patchPanelPort;

    // ── Inspector — Phase 2 Managers (Tasks 2, 19, 20) ───────────────────────────────

    [Header("Phase 2 Managers — Configurator")]
    [SerializeField] private NetworkDevicePhase2Manager computer1Phase2;
    [SerializeField] private NetworkDevicePhase2Manager computer2Phase2;
    [SerializeField] private NetworkDevicePhase2Manager laptopPhase2;

    [Header("Phase 2 Managers — Network Devices")]
    [SerializeField] private NetworkDevicePhase2Manager routerPhase2;
    [SerializeField] private NetworkDevicePhase2Manager apPhase2;
    [SerializeField] private NetworkDevicePhase2Manager patchPanelPhase2;
    [SerializeField] private NetworkDevicePhase2Manager switchPhase2;

    // ── Inspector — Reset Controllers (Task 3) ────────────────────────────────────────

    [Header("Reset Controllers")]
    [SerializeField] private RouterResetController       routerReset;
    [SerializeField] private AccessPointResetController  apReset;

    // ── Inspector — Router WebUI (Tasks 7–9) ──────────────────────────────────────────

    [Header("Router WebUI")]
    [SerializeField] private TPLinkTabManager tpLinkTabManager;

    // ── Inspector — AP WebUI (Tasks 11–14) ────────────────────────────────────────────

    [Header("AP WebUI")]
    [SerializeField] private DLinkAPManager dLinkAPManager;

    // ── Inspector — Virtual OS Panels (Tasks 21–73) ───────────────────────────────────

    [Header("Virtual OS — Shared Panels")]
    [SerializeField] private NetworkSharingCenterController networkSharingCenter;
    [SerializeField] private EthernetPropertiesController   ethernetPropertiesController;
    [SerializeField] private IPv4PropertiesController       ipv4PropertiesController;
    [SerializeField] private AdvancedSharingController      advancedSharingController;
    [SerializeField] private FirewallController             firewallController;
    [SerializeField] private InternetPanelController        internetPanelController;

    // ── State ──────────────────────────────────────────────────────────────────────────

    private readonly bool[] _latched = new bool[74];

    // Captured when each device phase starts — used to gate tasks to the correct device.
    private DeviceID _serverDevice;
    private DeviceID _secondDesktopDevice;

    private class TaskEntry
    {
        public GameObject taskObject;
        public int        originalIndex;
        public bool       isCompleted;
        public bool       isFlashing;
        public Func<bool> condition;
    }

    private List<TaskEntry> _tasks;
    private const int       WindowSize = 3;

    private string _displayOverride      = null;
    private bool   _isCompletionOverride = false;

    // ── Lifecycle ──────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (taskObjects == null || taskObjects.Length < 74)
        {
            Debug.LogError("[IPConfigTaskManager] Assign all 74 task objects in the inspector.");
            return;
        }

        BuildTasks();

        foreach (var task in _tasks)
        {
            task.taskObject.SetActive(false);
            task.taskObject.transform.SetParent(taskParent, false);
            task.taskObject.transform.SetSiblingIndex(task.originalIndex);
            var tmp = task.taskObject.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.color = GoldColor;
        }

        if (allTasksCompletedText != null)
            allTasksCompletedText.gameObject.SetActive(false);

        RefreshWindow();
    }

    // ── Task Definitions ───────────────────────────────────────────────────────────────

    private void BuildTasks()
    {
        _tasks = new List<TaskEntry>
        {
            // ── PART 1 — Original 17 tasks (indices 0–16) ─────────────────────────────

            // [0] Deploy at least 1 configurator + Router + Access Point
            new TaskEntry
            {
                taskObject    = taskObjects[0],
                originalIndex = 0,
                condition     = () =>
                {
                    if (!_latched[0] &&
                        (IsDeployed(computer1Holder) || IsDeployed(computer2Holder) || IsDeployed(laptopHolder)) &&
                        IsDeployed(routerHolder) &&
                        IsDeployed(apHolder))
                        _latched[0] = true;
                    return _latched[0];
                }
            },

            // [1] Connect deployed devices with Phase 1 logical cables
            new TaskEntry
            {
                taskObject    = taskObjects[1],
                originalIndex = 1,
                condition     = () =>
                {
                    if (!_latched[1] && _latched[0] &&
                        (HasCables(computer1Port) || HasCables(computer2Port) || HasCables(laptopPort)) &&
                        HasCables(routerPort) &&
                        HasCables(apPort))
                        _latched[1] = true;
                    return _latched[1];
                }
            },

            // [2] Install Phase 2 port cables in all deployed device detail views
            new TaskEntry
            {
                taskObject    = taskObjects[2],
                originalIndex = 2,
                condition     = () =>
                {
                    if (!_latched[2] && _latched[1])
                    {
                        bool ok = true;
                        if (IsDeployed(computer1Holder) && !AllPhase2Installed(computer1Phase2)) ok = false;
                        if (IsDeployed(computer2Holder) && !AllPhase2Installed(computer2Phase2)) ok = false;
                        if (IsDeployed(laptopHolder)    && !AllPhase2Installed(laptopPhase2))    ok = false;
                        if (!AllPhase2Installed(routerPhase2))     ok = false;
                        if (!AllPhase2Installed(apPhase2))         ok = false;
                        if (!AllPhase2Installed(patchPanelPhase2)) ok = false;
                        if (ok) _latched[2] = true;
                    }
                    return _latched[2];
                }
            },

            // [3] Factory reset both Router and Access Point
            new TaskEntry
            {
                taskObject    = taskObjects[3],
                originalIndex = 3,
                condition     = () =>
                {
                    if (!_latched[3] && _latched[2] &&
                        routerReset != null && routerReset.IsDefaultIPReady &&
                        apReset     != null && apReset.IsDefaultIPReady)
                        _latched[3] = true;
                    return _latched[3];
                }
            },

            // [4] Enter Virtual OS from the configured device's detail view
            new TaskEntry
            {
                taskObject    = taskObjects[4],
                originalIndex = 4,
                condition     = () =>
                {
                    if (!_latched[4] && _latched[3] &&
                        VirtualOSManager.Instance != null && VirtualOSManager.Instance.IsOpen)
                        _latched[4] = true;
                    return _latched[4];
                }
            },

            // [5] Navigate to the Router login page in Chrome (ChromeCurrentPage >= 1)
            new TaskEntry
            {
                taskObject    = taskObjects[5],
                originalIndex = 5,
                condition     = () =>
                {
                    if (!_latched[5] && _latched[4])
                    {
                        var state = VirtualOSManager.Instance?.CurrentState;
                        if (state != null && state.ChromeCurrentPage >= 1)
                            _latched[5] = true;
                    }
                    return _latched[5];
                }
            },

            // [6] Log in to TP-Link WebUI (ChromeCurrentPage >= 2)
            new TaskEntry
            {
                taskObject    = taskObjects[6],
                originalIndex = 6,
                condition     = () =>
                {
                    if (!_latched[6] && _latched[5])
                    {
                        var state = VirtualOSManager.Instance?.CurrentState;
                        if (state != null && state.ChromeCurrentPage >= 2)
                            _latched[6] = true;
                    }
                    return _latched[6];
                }
            },

            // [7] Configure SSID and Pre-Shared Key (live fields differ from defaults)
            new TaskEntry
            {
                taskObject    = taskObjects[7],
                originalIndex = 7,
                condition     = () =>
                {
                    if (!_latched[7] && _latched[6] &&
                        tpLinkTabManager != null && tpLinkTabManager.HasLiveModifiedWireless)
                        _latched[7] = true;
                    return _latched[7];
                }
            },

            // [8] Save wireless config and navigate to LAN tab
            new TaskEntry
            {
                taskObject    = taskObjects[8],
                originalIndex = 8,
                condition     = () =>
                {
                    if (!_latched[8] && _latched[7] &&
                        tpLinkTabManager != null &&
                        tpLinkTabManager.HasSavedConfiguredWireless &&
                        tpLinkTabManager.LanPanelVisible)
                        _latched[8] = true;
                    return _latched[8];
                }
            },

            // [9] Configure DHCP + IP Address and save
            new TaskEntry
            {
                taskObject    = taskObjects[9],
                originalIndex = 9,
                condition     = () =>
                {
                    if (!_latched[9] && _latched[8] &&
                        tpLinkTabManager != null && tpLinkTabManager.HasSavedStaticLanConfig)
                        _latched[9] = true;
                    return _latched[9];
                }
            },

            // [10] Navigate to D-Link AP WebUI (ChromeCurrentPage == 3)
            new TaskEntry
            {
                taskObject    = taskObjects[10],
                originalIndex = 10,
                condition     = () =>
                {
                    if (!_latched[10] && _latched[9])
                    {
                        var state = VirtualOSManager.Instance?.CurrentState;
                        if (state != null && state.ChromeCurrentPage == 3)
                            _latched[10] = true;
                    }
                    return _latched[10];
                }
            },

            // [11] Configure SSID, WPA-Personal, Pre-Shared Key (live fields)
            new TaskEntry
            {
                taskObject    = taskObjects[11],
                originalIndex = 11,
                condition     = () =>
                {
                    if (!_latched[11] && _latched[10] &&
                        dLinkAPManager != null && dLinkAPManager.HasLiveModifiedWireless)
                        _latched[11] = true;
                    return _latched[11];
                }
            },

            // [12] Save AP wireless config and navigate to Network (LAN) tab
            new TaskEntry
            {
                taskObject    = taskObjects[12],
                originalIndex = 12,
                condition     = () =>
                {
                    if (!_latched[12] && _latched[11] &&
                        dLinkAPManager != null &&
                        dLinkAPManager.HasSavedAtLeastOnce &&
                        dLinkAPManager.NetworkPanelVisible)
                        _latched[12] = true;
                    return _latched[12];
                }
            },

            // [13] Configure LAN: Static IP + valid 192.168.100.x + Subnet/Gateway/DNS (live)
            new TaskEntry
            {
                taskObject    = taskObjects[13],
                originalIndex = 13,
                condition     = () =>
                {
                    if (!_latched[13] && _latched[12] &&
                        dLinkAPManager != null && dLinkAPManager.HasLiveStaticLanConfig)
                        _latched[13] = true;
                    return _latched[13];
                }
            },

            // [14] Save AP configuration
            new TaskEntry
            {
                taskObject    = taskObjects[14],
                originalIndex = 14,
                condition     = () =>
                {
                    if (!_latched[14] && _latched[13] &&
                        apReset != null && apReset.IsConfigured)
                        _latched[14] = true;
                    return _latched[14];
                }
            },

            // [15] Exit Virtual OS and unplug patch panel port cables for Router and AP
            new TaskEntry
            {
                taskObject    = taskObjects[15],
                originalIndex = 15,
                condition     = () =>
                {
                    if (!_latched[15] && _latched[14] &&
                        patchPanelPhase2 != null &&
                        patchPanelPhase2.HasUninstalledEntryFor(routerPort) &&
                        patchPanelPhase2.HasUninstalledEntryFor(apPort))
                        _latched[15] = true;
                    return _latched[15];
                }
            },

            // [16] Deploy Switch and reconnect Router + AP via Phase 1 cables to Switch
            new TaskEntry
            {
                taskObject    = taskObjects[16],
                originalIndex = 16,
                condition     = () =>
                {
                    if (!_latched[16] && _latched[15] &&
                        IsDeployed(switchHolder) &&
                        IsConnectedTo(routerPort, switchPort) &&
                        IsConnectedTo(apPort,     switchPort))
                        _latched[16] = true;
                    return _latched[16];
                }
            },

            // ── PART 2 — Remaining device deployment (indices 17–20) ──────────────────

            // [17] Deploy both desktops and the laptop
            new TaskEntry
            {
                taskObject    = taskObjects[17],
                originalIndex = 17,
                condition     = () =>
                {
                    if (!_latched[17] && _latched[16] &&
                        IsDeployed(computer1Holder) &&
                        IsDeployed(computer2Holder) &&
                        IsDeployed(laptopHolder))
                        _latched[17] = true;
                    return _latched[17];
                }
            },

            // [18] Connect Phase 1 cables to both desktop ports (laptop excluded)
            new TaskEntry
            {
                taskObject    = taskObjects[18],
                originalIndex = 18,
                condition     = () =>
                {
                    if (!_latched[18] && _latched[17] &&
                        HasCables(computer1Port) &&
                        HasCables(computer2Port))
                        _latched[18] = true;
                    return _latched[18];
                }
            },

            // [19] Install Phase 2 cables in both desktop detail views
            new TaskEntry
            {
                taskObject    = taskObjects[19],
                originalIndex = 19,
                condition     = () =>
                {
                    if (!_latched[19] && _latched[18] &&
                        AllPhase2Installed(computer1Phase2) &&
                        AllPhase2Installed(computer2Phase2))
                        _latched[19] = true;
                    return _latched[19];
                }
            },

            // [20] Install Phase 2 cables in Patch Panel and Switch detail views
            new TaskEntry
            {
                taskObject    = taskObjects[20],
                originalIndex = 20,
                condition     = () =>
                {
                    if (!_latched[20] && _latched[19] &&
                        patchPanelPhase2 != null &&
                        patchPanelPhase2.HasInstalledEntryFor(computer1Port) &&
                        patchPanelPhase2.HasInstalledEntryFor(computer2Port) &&
                        patchPanelPhase2.HasInstalledEntryFor(switchPort) &&
                        switchPhase2 != null &&
                        switchPhase2.HasInstalledEntryFor(routerPort) &&
                        switchPhase2.HasInstalledEntryFor(apPort) &&
                        switchPhase2.HasInstalledEntryFor(patchPanelPort))
                        _latched[20] = true;
                    return _latched[20];
                }
            },

            // ── SERVER DESKTOP FLOW (indices 21–37) ───────────────────────────────────
            // _serverDevice is captured when task 21 latches.
            // All tasks 22–37 require CurrentDevice == _serverDevice.

            // [21] Enter Virtual OS of one of the two desktops (becomes the server)
            new TaskEntry
            {
                taskObject    = taskObjects[21],
                originalIndex = 21,
                condition     = () =>
                {
                    if (!_latched[21] && _latched[20] &&
                        VirtualOSManager.Instance != null &&
                        VirtualOSManager.Instance.IsOpen &&
                        (VirtualOSManager.Instance.CurrentDevice == DeviceID.Computer1 ||
                         VirtualOSManager.Instance.CurrentDevice == DeviceID.Computer2))
                    {
                        _serverDevice = VirtualOSManager.Instance.CurrentDevice;
                        _latched[21] = true;
                    }
                    return _latched[21];
                }
            },

            // [22] Open Network and Sharing Center
            new TaskEntry
            {
                taskObject    = taskObjects[22],
                originalIndex = 22,
                condition     = () =>
                {
                    if (!_latched[22] && _latched[21] &&
                        IsOnServerDevice() &&
                        networkSharingCenter != null &&
                        networkSharingCenter.gameObject.activeInHierarchy)
                        _latched[22] = true;
                    return _latched[22];
                }
            },

            // [23] Open Ethernet Properties panel
            new TaskEntry
            {
                taskObject    = taskObjects[23],
                originalIndex = 23,
                condition     = () =>
                {
                    if (!_latched[23] && _latched[22] &&
                        IsOnServerDevice() &&
                        ethernetPropertiesController != null &&
                        ethernetPropertiesController.gameObject.activeInHierarchy)
                        _latched[23] = true;
                    return _latched[23];
                }
            },

            // [24] Uncheck IPv6 and open IPv4 Properties panel
            new TaskEntry
            {
                taskObject    = taskObjects[24],
                originalIndex = 24,
                condition     = () =>
                {
                    if (!_latched[24] && _latched[23] &&
                        IsOnServerDevice() &&
                        ipv4PropertiesController != null &&
                        ipv4PropertiesController.gameObject.activeInHierarchy &&
                        VirtualOSManager.Instance.CurrentState.IPv6Unchecked)
                        _latched[24] = true;
                    return _latched[24];
                }
            },

            // [25] Toggle static IP mode on
            new TaskEntry
            {
                taskObject    = taskObjects[25],
                originalIndex = 25,
                condition     = () =>
                {
                    if (!_latched[25] && _latched[24] &&
                        IsOnServerDevice() &&
                        ipv4PropertiesController != null &&
                        ipv4PropertiesController.gameObject.activeInHierarchy &&
                        ipv4PropertiesController.IsStaticIPModeActive)
                        _latched[25] = true;
                    return _latched[25];
                }
            },

            // [26] Input "192.168.100.12" in the IP address field
            new TaskEntry
            {
                taskObject    = taskObjects[26],
                originalIndex = 26,
                condition     = () =>
                {
                    if (!_latched[26] && _latched[25] &&
                        IsOnServerDevice() &&
                        ipv4PropertiesController != null &&
                        ipv4PropertiesController.gameObject.activeInHierarchy &&
                        MatchesIP(ipv4PropertiesController.LiveIPOctets, "192", "168", "100", "12"))
                        _latched[26] = true;
                    return _latched[26];
                }
            },

            // [27] Input the router's IP in the Default Gateway field
            new TaskEntry
            {
                taskObject    = taskObjects[27],
                originalIndex = 27,
                condition     = () =>
                {
                    if (!_latched[27] && _latched[26] &&
                        IsOnServerDevice() &&
                        ipv4PropertiesController != null &&
                        ipv4PropertiesController.gameObject.activeInHierarchy &&
                        tpLinkTabManager != null &&
                        OctetsJoined(ipv4PropertiesController.LiveGatewayOctets) == tpLinkTabManager.GetRouterLanIP())
                        _latched[27] = true;
                    return _latched[27];
                }
            },

            // [28] Input this device's IP in the Preferred DNS field
            new TaskEntry
            {
                taskObject    = taskObjects[28],
                originalIndex = 28,
                condition     = () =>
                {
                    if (!_latched[28] && _latched[27] &&
                        IsOnServerDevice() &&
                        ipv4PropertiesController != null &&
                        ipv4PropertiesController.gameObject.activeInHierarchy &&
                        OctetsMatch(ipv4PropertiesController.LivePreferredDNSOctets,
                                    ipv4PropertiesController.LiveIPOctets))
                        _latched[28] = true;
                    return _latched[28];
                }
            },

            // [29] Click OK — IP config saved to server device state
            new TaskEntry
            {
                taskObject    = taskObjects[29],
                originalIndex = 29,
                condition     = () =>
                {
                    if (!_latched[29] && _latched[28] &&
                        IsOnServerDevice())
                    {
                        var s = VirtualOSManager.Instance.CurrentState;
                        if (s.UseStaticIP &&
                            MatchesIP(s.IPOctets, "192", "168", "100", "12"))
                            _latched[29] = true;
                    }
                    return _latched[29];
                }
            },

            // [30] Click Home to return to NSC main content
            new TaskEntry
            {
                taskObject    = taskObjects[30],
                originalIndex = 30,
                condition     = () =>
                {
                    if (!_latched[30] && _latched[29] &&
                        IsOnServerDevice() &&
                        networkSharingCenter != null &&
                        networkSharingCenter.IsAtMainContent)
                        _latched[30] = true;
                    return _latched[30];
                }
            },

            // [31] Open Advanced Sharing Settings panel
            new TaskEntry
            {
                taskObject    = taskObjects[31],
                originalIndex = 31,
                condition     = () =>
                {
                    if (!_latched[31] && _latched[30] &&
                        IsOnServerDevice() &&
                        advancedSharingController != null &&
                        advancedSharingController.gameObject.activeInHierarchy)
                        _latched[31] = true;
                    return _latched[31];
                }
            },

            // [32] Live: Guest/Public — Network Discovery ON + File & Printer Sharing ON
            new TaskEntry
            {
                taskObject    = taskObjects[32],
                originalIndex = 32,
                condition     = () =>
                {
                    if (!_latched[32] && _latched[31] &&
                        IsOnServerDevice() &&
                        advancedSharingController != null &&
                        advancedSharingController.LiveNetworkDiscoveryOn &&
                        advancedSharingController.LiveFilePrinterSharingOn)
                        _latched[32] = true;
                    return _latched[32];
                }
            },

            // [33] Live: All Networks — Public Folder Sharing ON + Password Protection OFF
            new TaskEntry
            {
                taskObject    = taskObjects[33],
                originalIndex = 33,
                condition     = () =>
                {
                    if (!_latched[33] && _latched[32] &&
                        IsOnServerDevice() &&
                        advancedSharingController != null &&
                        advancedSharingController.LivePublicFolderSharingOn &&
                        advancedSharingController.LivePasswordSharingOff)
                        _latched[33] = true;
                    return _latched[33];
                }
            },

            // [34] Save Changes clicked AND panel closed (Return clicked)
            new TaskEntry
            {
                taskObject    = taskObjects[34],
                originalIndex = 34,
                condition     = () =>
                {
                    if (!_latched[34] && _latched[33] &&
                        IsOnServerDevice() &&
                        advancedSharingController != null)
                    {
                        var s = VirtualOSManager.Instance.CurrentState;
                        if (s.NetworkDiscoveryOn &&
                            s.FilePrinterSharingOn &&
                            s.PublicFolderSharingOn &&
                            s.PasswordProtectedSharingOff &&
                            !advancedSharingController.gameObject.activeInHierarchy)
                            _latched[34] = true;
                    }
                    return _latched[34];
                }
            },

            // [35] Open Firewall panel AND click Turn Firewall On and Off
            new TaskEntry
            {
                taskObject    = taskObjects[35],
                originalIndex = 35,
                condition     = () =>
                {
                    if (!_latched[35] && _latched[34] &&
                        IsOnServerDevice() &&
                        firewallController != null &&
                        firewallController.gameObject.activeInHierarchy &&
                        firewallController.IsOnOffPanelOpen)
                        _latched[35] = true;
                    return _latched[35];
                }
            },

            // [36] OK clicked with both Private and Public firewall set to Off
            new TaskEntry
            {
                taskObject    = taskObjects[36],
                originalIndex = 36,
                condition     = () =>
                {
                    if (!_latched[36] && _latched[35] &&
                        IsOnServerDevice())
                    {
                        var s = VirtualOSManager.Instance.CurrentState;
                        if (s.FirewallPrivateOff && s.FirewallPublicOff)
                            _latched[36] = true;
                    }
                    return _latched[36];
                }
            },

            // [37] Exit server desktop Virtual OS
            new TaskEntry
            {
                taskObject    = taskObjects[37],
                originalIndex = 37,
                condition     = () =>
                {
                    if (!_latched[37] && _latched[36] &&
                        VirtualOSManager.Instance != null &&
                        !VirtualOSManager.Instance.IsOpen)
                        _latched[37] = true;
                    return _latched[37];
                }
            },

            // ── SECOND DESKTOP FLOW (indices 38–54) ───────────────────────────────────
            // _secondDesktopDevice is captured when task 38 latches.
            // All tasks 39–54 require CurrentDevice == _secondDesktopDevice.

            // [38] Enter Virtual OS of the OTHER desktop (not the server)
            new TaskEntry
            {
                taskObject    = taskObjects[38],
                originalIndex = 38,
                condition     = () =>
                {
                    if (!_latched[38] && _latched[37] &&
                        VirtualOSManager.Instance != null &&
                        VirtualOSManager.Instance.IsOpen)
                    {
                        DeviceID cur = VirtualOSManager.Instance.CurrentDevice;
                        if ((cur == DeviceID.Computer1 || cur == DeviceID.Computer2) &&
                            cur != _serverDevice)
                        {
                            _secondDesktopDevice = cur;
                            _latched[38] = true;
                        }
                    }
                    return _latched[38];
                }
            },

            // [39] Open Network and Sharing Center
            new TaskEntry
            {
                taskObject    = taskObjects[39],
                originalIndex = 39,
                condition     = () =>
                {
                    if (!_latched[39] && _latched[38] &&
                        IsOnSecondDevice() &&
                        networkSharingCenter != null &&
                        networkSharingCenter.gameObject.activeInHierarchy)
                        _latched[39] = true;
                    return _latched[39];
                }
            },

            // [40] Open Ethernet Properties panel
            new TaskEntry
            {
                taskObject    = taskObjects[40],
                originalIndex = 40,
                condition     = () =>
                {
                    if (!_latched[40] && _latched[39] &&
                        IsOnSecondDevice() &&
                        ethernetPropertiesController != null &&
                        ethernetPropertiesController.gameObject.activeInHierarchy)
                        _latched[40] = true;
                    return _latched[40];
                }
            },

            // [41] Uncheck IPv6 and open IPv4 Properties panel
            new TaskEntry
            {
                taskObject    = taskObjects[41],
                originalIndex = 41,
                condition     = () =>
                {
                    if (!_latched[41] && _latched[40] &&
                        IsOnSecondDevice() &&
                        ipv4PropertiesController != null &&
                        ipv4PropertiesController.gameObject.activeInHierarchy &&
                        VirtualOSManager.Instance.CurrentState.IPv6Unchecked)
                        _latched[41] = true;
                    return _latched[41];
                }
            },

            // [42] Toggle static IP mode on
            new TaskEntry
            {
                taskObject    = taskObjects[42],
                originalIndex = 42,
                condition     = () =>
                {
                    if (!_latched[42] && _latched[41] &&
                        IsOnSecondDevice() &&
                        ipv4PropertiesController != null &&
                        ipv4PropertiesController.gameObject.activeInHierarchy &&
                        ipv4PropertiesController.IsStaticIPModeActive)
                        _latched[42] = true;
                    return _latched[42];
                }
            },

            // [43] Input "192.168.100.13" in the IP address field
            new TaskEntry
            {
                taskObject    = taskObjects[43],
                originalIndex = 43,
                condition     = () =>
                {
                    if (!_latched[43] && _latched[42] &&
                        IsOnSecondDevice() &&
                        ipv4PropertiesController != null &&
                        ipv4PropertiesController.gameObject.activeInHierarchy &&
                        MatchesIP(ipv4PropertiesController.LiveIPOctets, "192", "168", "100", "13"))
                        _latched[43] = true;
                    return _latched[43];
                }
            },

            // [44] Input the router's IP in the Default Gateway field
            new TaskEntry
            {
                taskObject    = taskObjects[44],
                originalIndex = 44,
                condition     = () =>
                {
                    if (!_latched[44] && _latched[43] &&
                        IsOnSecondDevice() &&
                        ipv4PropertiesController != null &&
                        ipv4PropertiesController.gameObject.activeInHierarchy &&
                        tpLinkTabManager != null &&
                        OctetsJoined(ipv4PropertiesController.LiveGatewayOctets) == tpLinkTabManager.GetRouterLanIP())
                        _latched[44] = true;
                    return _latched[44];
                }
            },

            // [45] Input the server desktop's IP in the Preferred DNS field
            new TaskEntry
            {
                taskObject    = taskObjects[45],
                originalIndex = 45,
                condition     = () =>
                {
                    if (!_latched[45] && _latched[44] &&
                        IsOnSecondDevice() &&
                        ipv4PropertiesController != null &&
                        ipv4PropertiesController.gameObject.activeInHierarchy &&
                        OctetsMatch(ipv4PropertiesController.LivePreferredDNSOctets,
                                    GetServerIPOctets()))
                        _latched[45] = true;
                    return _latched[45];
                }
            },

            // [46] Click OK — IP config saved to second desktop state
            new TaskEntry
            {
                taskObject    = taskObjects[46],
                originalIndex = 46,
                condition     = () =>
                {
                    if (!_latched[46] && _latched[45] &&
                        IsOnSecondDevice())
                    {
                        var s = VirtualOSManager.Instance.CurrentState;
                        if (s.UseStaticIP &&
                            MatchesIP(s.IPOctets, "192", "168", "100", "13"))
                            _latched[46] = true;
                    }
                    return _latched[46];
                }
            },

            // [47] Click Home to return to NSC main content
            new TaskEntry
            {
                taskObject    = taskObjects[47],
                originalIndex = 47,
                condition     = () =>
                {
                    if (!_latched[47] && _latched[46] &&
                        IsOnSecondDevice() &&
                        networkSharingCenter != null &&
                        networkSharingCenter.IsAtMainContent)
                        _latched[47] = true;
                    return _latched[47];
                }
            },

            // [48] Open Advanced Sharing Settings panel
            new TaskEntry
            {
                taskObject    = taskObjects[48],
                originalIndex = 48,
                condition     = () =>
                {
                    if (!_latched[48] && _latched[47] &&
                        IsOnSecondDevice() &&
                        advancedSharingController != null &&
                        advancedSharingController.gameObject.activeInHierarchy)
                        _latched[48] = true;
                    return _latched[48];
                }
            },

            // [49] Live: Guest/Public — Network Discovery ON + File & Printer Sharing ON
            new TaskEntry
            {
                taskObject    = taskObjects[49],
                originalIndex = 49,
                condition     = () =>
                {
                    if (!_latched[49] && _latched[48] &&
                        IsOnSecondDevice() &&
                        advancedSharingController != null &&
                        advancedSharingController.LiveNetworkDiscoveryOn &&
                        advancedSharingController.LiveFilePrinterSharingOn)
                        _latched[49] = true;
                    return _latched[49];
                }
            },

            // [50] Live: All Networks — Public Folder Sharing ON + Password Protection OFF
            new TaskEntry
            {
                taskObject    = taskObjects[50],
                originalIndex = 50,
                condition     = () =>
                {
                    if (!_latched[50] && _latched[49] &&
                        IsOnSecondDevice() &&
                        advancedSharingController != null &&
                        advancedSharingController.LivePublicFolderSharingOn &&
                        advancedSharingController.LivePasswordSharingOff)
                        _latched[50] = true;
                    return _latched[50];
                }
            },

            // [51] Save Changes clicked AND panel closed
            new TaskEntry
            {
                taskObject    = taskObjects[51],
                originalIndex = 51,
                condition     = () =>
                {
                    if (!_latched[51] && _latched[50] &&
                        IsOnSecondDevice() &&
                        advancedSharingController != null)
                    {
                        var s = VirtualOSManager.Instance.CurrentState;
                        if (s.NetworkDiscoveryOn &&
                            s.FilePrinterSharingOn &&
                            s.PublicFolderSharingOn &&
                            s.PasswordProtectedSharingOff &&
                            !advancedSharingController.gameObject.activeInHierarchy)
                            _latched[51] = true;
                    }
                    return _latched[51];
                }
            },

            // [52] Open Firewall panel AND click Turn Firewall On and Off
            new TaskEntry
            {
                taskObject    = taskObjects[52],
                originalIndex = 52,
                condition     = () =>
                {
                    if (!_latched[52] && _latched[51] &&
                        IsOnSecondDevice() &&
                        firewallController != null &&
                        firewallController.gameObject.activeInHierarchy &&
                        firewallController.IsOnOffPanelOpen)
                        _latched[52] = true;
                    return _latched[52];
                }
            },

            // [53] OK clicked with both Private and Public firewall set to Off
            new TaskEntry
            {
                taskObject    = taskObjects[53],
                originalIndex = 53,
                condition     = () =>
                {
                    if (!_latched[53] && _latched[52] &&
                        IsOnSecondDevice())
                    {
                        var s = VirtualOSManager.Instance.CurrentState;
                        if (s.FirewallPrivateOff && s.FirewallPublicOff)
                            _latched[53] = true;
                    }
                    return _latched[53];
                }
            },

            // [54] Exit second desktop Virtual OS
            new TaskEntry
            {
                taskObject    = taskObjects[54],
                originalIndex = 54,
                condition     = () =>
                {
                    if (!_latched[54] && _latched[53] &&
                        VirtualOSManager.Instance != null &&
                        !VirtualOSManager.Instance.IsOpen)
                        _latched[54] = true;
                    return _latched[54];
                }
            },

            // ── LAPTOP FLOW (indices 55–73) ────────────────────────────────────────────
            // All tasks 56–73 require CurrentDevice == DeviceID.Laptop.

            // [55] Enter Laptop Virtual OS
            new TaskEntry
            {
                taskObject    = taskObjects[55],
                originalIndex = 55,
                condition     = () =>
                {
                    if (!_latched[55] && _latched[54] &&
                        VirtualOSManager.Instance != null &&
                        VirtualOSManager.Instance.IsOpen &&
                        VirtualOSManager.Instance.CurrentDevice == DeviceID.Laptop)
                        _latched[55] = true;
                    return _latched[55];
                }
            },

            // [56] Click WiFi — password panel visible (student is on the AP connect step)
            new TaskEntry
            {
                taskObject    = taskObjects[56],
                originalIndex = 56,
                condition     = () =>
                {
                    if (!_latched[56] && _latched[55] &&
                        IsOnLaptop() &&
                        VirtualOSManager.Instance.IsOpen &&
                        internetPanelController != null &&
                        internetPanelController.IsPasswordPanelOpen)
                        _latched[56] = true;
                    return _latched[56];
                }
            },

            // [57] WiFi connection successful
            new TaskEntry
            {
                taskObject    = taskObjects[57],
                originalIndex = 57,
                condition     = () =>
                {
                    if (!_latched[57] && _latched[56] &&
                        VirtualOSManager.Instance != null &&
                        VirtualOSManager.Instance.GetState(DeviceID.Laptop).WifiConnected)
                        _latched[57] = true;
                    return _latched[57];
                }
            },

            // [58] Open Network and Sharing Center on Laptop
            new TaskEntry
            {
                taskObject    = taskObjects[58],
                originalIndex = 58,
                condition     = () =>
                {
                    if (!_latched[58] && _latched[57] &&
                        IsOnLaptop() &&
                        networkSharingCenter != null &&
                        networkSharingCenter.gameObject.activeInHierarchy)
                        _latched[58] = true;
                    return _latched[58];
                }
            },

            // [59] Open Ethernet Properties panel
            new TaskEntry
            {
                taskObject    = taskObjects[59],
                originalIndex = 59,
                condition     = () =>
                {
                    if (!_latched[59] && _latched[58] &&
                        IsOnLaptop() &&
                        ethernetPropertiesController != null &&
                        ethernetPropertiesController.gameObject.activeInHierarchy)
                        _latched[59] = true;
                    return _latched[59];
                }
            },

            // [60] Uncheck IPv6 and open IPv4 Properties panel
            new TaskEntry
            {
                taskObject    = taskObjects[60],
                originalIndex = 60,
                condition     = () =>
                {
                    if (!_latched[60] && _latched[59] &&
                        IsOnLaptop() &&
                        ipv4PropertiesController != null &&
                        ipv4PropertiesController.gameObject.activeInHierarchy &&
                        VirtualOSManager.Instance.CurrentState.IPv6Unchecked)
                        _latched[60] = true;
                    return _latched[60];
                }
            },

            // [61] Toggle static IP mode on
            new TaskEntry
            {
                taskObject    = taskObjects[61],
                originalIndex = 61,
                condition     = () =>
                {
                    if (!_latched[61] && _latched[60] &&
                        IsOnLaptop() &&
                        ipv4PropertiesController != null &&
                        ipv4PropertiesController.gameObject.activeInHierarchy &&
                        ipv4PropertiesController.IsStaticIPModeActive)
                        _latched[61] = true;
                    return _latched[61];
                }
            },

            // [62] Input "192.168.100.14" in the IP address field
            new TaskEntry
            {
                taskObject    = taskObjects[62],
                originalIndex = 62,
                condition     = () =>
                {
                    if (!_latched[62] && _latched[61] &&
                        IsOnLaptop() &&
                        ipv4PropertiesController != null &&
                        ipv4PropertiesController.gameObject.activeInHierarchy &&
                        MatchesIP(ipv4PropertiesController.LiveIPOctets, "192", "168", "100", "14"))
                        _latched[62] = true;
                    return _latched[62];
                }
            },

            // [63] Input the router's IP in the Default Gateway field
            new TaskEntry
            {
                taskObject    = taskObjects[63],
                originalIndex = 63,
                condition     = () =>
                {
                    if (!_latched[63] && _latched[62] &&
                        IsOnLaptop() &&
                        ipv4PropertiesController != null &&
                        ipv4PropertiesController.gameObject.activeInHierarchy &&
                        tpLinkTabManager != null &&
                        OctetsJoined(ipv4PropertiesController.LiveGatewayOctets) == tpLinkTabManager.GetRouterLanIP())
                        _latched[63] = true;
                    return _latched[63];
                }
            },

            // [64] Input the server desktop's IP in the Preferred DNS field
            new TaskEntry
            {
                taskObject    = taskObjects[64],
                originalIndex = 64,
                condition     = () =>
                {
                    if (!_latched[64] && _latched[63] &&
                        IsOnLaptop() &&
                        ipv4PropertiesController != null &&
                        ipv4PropertiesController.gameObject.activeInHierarchy &&
                        OctetsMatch(ipv4PropertiesController.LivePreferredDNSOctets,
                                    GetServerIPOctets()))
                        _latched[64] = true;
                    return _latched[64];
                }
            },

            // [65] Click OK — IP config saved to laptop state
            new TaskEntry
            {
                taskObject    = taskObjects[65],
                originalIndex = 65,
                condition     = () =>
                {
                    if (!_latched[65] && _latched[64] &&
                        IsOnLaptop())
                    {
                        var s = VirtualOSManager.Instance.CurrentState;
                        if (s.UseStaticIP &&
                            MatchesIP(s.IPOctets, "192", "168", "100", "14"))
                            _latched[65] = true;
                    }
                    return _latched[65];
                }
            },

            // [66] Click Home to return to NSC main content
            new TaskEntry
            {
                taskObject    = taskObjects[66],
                originalIndex = 66,
                condition     = () =>
                {
                    if (!_latched[66] && _latched[65] &&
                        IsOnLaptop() &&
                        networkSharingCenter != null &&
                        networkSharingCenter.IsAtMainContent)
                        _latched[66] = true;
                    return _latched[66];
                }
            },

            // [67] Open Advanced Sharing Settings panel
            new TaskEntry
            {
                taskObject    = taskObjects[67],
                originalIndex = 67,
                condition     = () =>
                {
                    if (!_latched[67] && _latched[66] &&
                        IsOnLaptop() &&
                        advancedSharingController != null &&
                        advancedSharingController.gameObject.activeInHierarchy)
                        _latched[67] = true;
                    return _latched[67];
                }
            },

            // [68] Live: Guest/Public — Network Discovery ON + File & Printer Sharing ON
            new TaskEntry
            {
                taskObject    = taskObjects[68],
                originalIndex = 68,
                condition     = () =>
                {
                    if (!_latched[68] && _latched[67] &&
                        IsOnLaptop() &&
                        advancedSharingController != null &&
                        advancedSharingController.LiveNetworkDiscoveryOn &&
                        advancedSharingController.LiveFilePrinterSharingOn)
                        _latched[68] = true;
                    return _latched[68];
                }
            },

            // [69] Live: All Networks — Public Folder Sharing ON + Password Protection OFF
            new TaskEntry
            {
                taskObject    = taskObjects[69],
                originalIndex = 69,
                condition     = () =>
                {
                    if (!_latched[69] && _latched[68] &&
                        IsOnLaptop() &&
                        advancedSharingController != null &&
                        advancedSharingController.LivePublicFolderSharingOn &&
                        advancedSharingController.LivePasswordSharingOff)
                        _latched[69] = true;
                    return _latched[69];
                }
            },

            // [70] Save Changes clicked AND panel closed
            new TaskEntry
            {
                taskObject    = taskObjects[70],
                originalIndex = 70,
                condition     = () =>
                {
                    if (!_latched[70] && _latched[69] &&
                        IsOnLaptop() &&
                        advancedSharingController != null)
                    {
                        var s = VirtualOSManager.Instance.CurrentState;
                        if (s.NetworkDiscoveryOn &&
                            s.FilePrinterSharingOn &&
                            s.PublicFolderSharingOn &&
                            s.PasswordProtectedSharingOff &&
                            !advancedSharingController.gameObject.activeInHierarchy)
                            _latched[70] = true;
                    }
                    return _latched[70];
                }
            },

            // [71] Open Firewall panel AND click Turn Firewall On and Off
            new TaskEntry
            {
                taskObject    = taskObjects[71],
                originalIndex = 71,
                condition     = () =>
                {
                    if (!_latched[71] && _latched[70] &&
                        IsOnLaptop() &&
                        firewallController != null &&
                        firewallController.gameObject.activeInHierarchy &&
                        firewallController.IsOnOffPanelOpen)
                        _latched[71] = true;
                    return _latched[71];
                }
            },

            // [72] OK clicked with both Private and Public firewall set to Off
            new TaskEntry
            {
                taskObject    = taskObjects[72],
                originalIndex = 72,
                condition     = () =>
                {
                    if (!_latched[72] && _latched[71] &&
                        IsOnLaptop())
                    {
                        var s = VirtualOSManager.Instance.CurrentState;
                        if (s.FirewallPrivateOff && s.FirewallPublicOff)
                            _latched[72] = true;
                    }
                    return _latched[72];
                }
            },

            // [73] Exit Laptop Virtual OS — all tasks complete
            new TaskEntry
            {
                taskObject    = taskObjects[73],
                originalIndex = 73,
                condition     = () =>
                {
                    if (!_latched[73] && _latched[72] &&
                        VirtualOSManager.Instance != null &&
                        !VirtualOSManager.Instance.IsOpen)
                        _latched[73] = true;
                    return _latched[73];
                }
            },
        };
    }

    // ── Public API (ITaskCategory) ────────────────────────────────────────────────────

    public string GetNextIncompleteTaskText()
    {
        if (_displayOverride != null) return _displayOverride;
        if (_tasks == null || _tasks.Count == 0) return null;
        var next = _tasks.FirstOrDefault(t => !t.isCompleted);
        if (next == null) return null;
        var tmp = next.taskObject.GetComponent<TextMeshProUGUI>();
        return tmp != null ? tmp.text : null;
    }

    public Color GetDisplayColor(Color fallback) =>
        (_isCompletionOverride && _displayOverride != null) ? Color.green : fallback;

    public void HideCompletionBanner()
    {
        if (allTasksCompletedText != null)
            allTasksCompletedText.gameObject.SetActive(false);
    }

    public bool     IsFullyComplete    => _tasks != null && _tasks.All(t => t.isCompleted);
    public DeviceID ServerDevice       => _serverDevice;
    public DeviceID SecondDesktopDevice => _secondDesktopDevice;

    public static void CheckConditions()
    {
        if (Instance == null) return;
        Instance.EvaluateConditions();
    }

    // ── Evaluation ────────────────────────────────────────────────────────────────────

    private void Update() => EvaluateConditions();

    private void EvaluateConditions()
    {
        if (_tasks == null || !gameObject.activeInHierarchy) return;

        foreach (var task in _tasks)
        {
            if (task.isFlashing) continue;

            if (!task.isCompleted)
            {
                if (!task.taskObject.activeSelf) continue;

                if (task.condition())
                {
                    task.isCompleted = true;
                    StartCoroutine(FlashAndComplete(task));
                }
            }
            else
            {
                if (!task.condition())
                {
                    task.isCompleted = false;
                    task.taskObject.transform.SetParent(taskParent, false);
                    task.taskObject.transform.SetSiblingIndex(task.originalIndex);
                    var revertTmp = task.taskObject.GetComponent<TextMeshProUGUI>();
                    if (revertTmp != null) revertTmp.color = GoldColor;
                    task.taskObject.SetActive(false);
                    RefreshWindow();
                    if (task.taskObject.activeSelf)
                        StartCoroutine(FlashRevert(task));
                }
            }
        }
    }

    // ── Window ────────────────────────────────────────────────────────────────────────

    private void RefreshWindow()
    {
        if (_tasks == null) return;

        var incomplete = _tasks
            .Where(t => !t.isCompleted)
            .OrderBy(t => t.originalIndex)
            .ToList();

        for (int i = 0; i < incomplete.Count; i++)
            incomplete[i].taskObject.SetActive(i < WindowSize);
    }

    // ── Coroutines ────────────────────────────────────────────────────────────────────

    private IEnumerator FlashAndComplete(TaskEntry task)
    {
        task.isFlashing = true;
        var tmp = task.taskObject.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.color = Color.green;
        yield return new WaitForSeconds(0.6f);
        task.isFlashing = false;
        task.taskObject.transform.SetParent(finishedParent, false);
        task.taskObject.SetActive(false);
        RefreshWindow();
        OnTasksUpdated?.Invoke();

        if (_tasks.All(t => t.isCompleted))
        {
            ShowAllTasksCompleted();
            yield break;
        }

        EvaluateConditions();
    }

    private IEnumerator FlashRevert(TaskEntry task)
    {
        task.isFlashing = true;
        var tmp = task.taskObject.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.color = new Color(1f, 0.647f, 0f);
        yield return new WaitForSeconds(0.6f);
        task.isFlashing = false;
        if (tmp != null) tmp.color = GoldColor;
        OnTasksUpdated?.Invoke();
        EvaluateConditions();
    }

    // ── Completion ────────────────────────────────────────────────────────────────────

    private void ShowAllTasksCompleted()
    {
        _displayOverride      = "All tasks completed!";
        _isCompletionOverride = true;

        if (allTasksCompletedText != null)
        {
            allTasksCompletedText.text  = _displayOverride;
            allTasksCompletedText.color = Color.green;
            allTasksCompletedText.gameObject.SetActive(true);
        }

        OnTasksUpdated?.Invoke();

        if (topicManagerCompletionIndex >= 0)
            TopicManager.Instance?.MarkTopicComplete(topicManagerCompletionIndex);

        Debug.Log("[IPConfigTaskManager] All 74 IP Configuration tasks complete.");
    }

    // ── Device guards ─────────────────────────────────────────────────────────────────

    private bool IsOnServerDevice() =>
        VirtualOSManager.Instance != null &&
        VirtualOSManager.Instance.IsOpen &&
        VirtualOSManager.Instance.CurrentDevice == _serverDevice;

    private bool IsOnSecondDevice() =>
        VirtualOSManager.Instance != null &&
        VirtualOSManager.Instance.IsOpen &&
        VirtualOSManager.Instance.CurrentDevice == _secondDesktopDevice;

    private bool IsOnLaptop() =>
        VirtualOSManager.Instance != null &&
        VirtualOSManager.Instance.IsOpen &&
        VirtualOSManager.Instance.CurrentDevice == DeviceID.Laptop;

    // ── Condition helpers ─────────────────────────────────────────────────────────────

    private static bool MatchesIP(string[] octets, string a, string b, string c, string d) =>
        octets != null && octets.Length == 4 &&
        octets[0] == a && octets[1] == b && octets[2] == c && octets[3] == d;

    private static bool OctetsMatch(string[] a, string[] b)
    {
        if (a == null || b == null || a.Length != 4 || b.Length != 4) return false;
        for (int i = 0; i < 4; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    private static string OctetsJoined(string[] octets) =>
        octets != null && octets.Length == 4
            ? $"{octets[0]}.{octets[1]}.{octets[2]}.{octets[3]}"
            : "";

    // Returns the saved IP octets of the server device (set when task 29 fires).
    private string[] GetServerIPOctets()
    {
        if (VirtualOSManager.Instance == null) return new string[4];
        return VirtualOSManager.Instance.GetState(_serverDevice).IPOctets;
    }

    // ── Hardware helpers ──────────────────────────────────────────────────────────────

    private static bool IsDeployed(NetworkHardwareHolder holder) =>
        holder != null && holder.hardwarePrefab != null && holder.hardwarePrefab.activeSelf;

    private static bool HasCables(NetworkDevicePort port) =>
        port != null && port.ConnectedCables.Count > 0;

    private static bool AllPhase2Installed(NetworkDevicePhase2Manager mgr) =>
        mgr != null && mgr.AreAllInstalled;

    private static bool IsConnectedTo(NetworkDevicePort source, NetworkDevicePort target)
    {
        if (source == null || target == null) return false;
        foreach (var cable in source.ConnectedCables)
        {
            if (cable == null) continue;
            if ((cable.PortA == source && cable.PortB == target) ||
                (cable.PortB == source && cable.PortA == target))
                return true;
        }
        return false;
    }
}
