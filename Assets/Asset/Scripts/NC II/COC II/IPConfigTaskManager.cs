/*
 * ================================================================
 *  UNITY SETUP GUIDE — IPConfigTaskManager  (COC II, Topic 2)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to an always-active "Managers" GameObject in the COC II scene.
 *
 *  ── HIERARCHY ────────────────────────────────────────────────────
 *
 *    Task Panel (IPConfigPanel)
 *      ├── Group Title Row                   (always visible above task list)
 *      │     ├── GroupTitleTMP               TMP_Text  → groupTitleTMP
 *      │     └── Button component            Button  → groupTitleBtn
 *      │           OnClick → opens Group Overview Popup
 *      │
 *      ├── Task List                         (active task rows parented here)
 *      │     └── [taskObjects[0..73] reparented here at runtime]
 *      │
 *      └── Finished Bucket                  (off-screen; latched tasks move here)
 *
 *    Group Overview Popup                   → groupOverviewPopup (INACTIVE)
 *      ├── Row Parent                       → groupRowParent  (VerticalLayoutGroup)
 *      │     └── [GroupRowPrefab × 8 spawned here at runtime]
 *      └── CloseBtn                         Button  → closePopupBtn
 *
 *  ── GROUP ROW PREFAB ─────────────────────────────────────────────
 *    Root: any layout container (e.g. HorizontalLayoutGroup)
 *      ├── NameTMP      TMP_Text  [0] — group title
 *      ├── ProgressTMP  TMP_Text  [1] — "2 / 6"
 *      └── StatusTMP    TMP_Text  [2] — "Pending" / "Active" / "Done"
 *    GetComponentsInChildren<TMP_Text>() must return them in order [0][1][2].
 *
 *  ── TASK GROUPS — 8 groups, 74 tasks total ───────────────────────
 *    Group 0  "Device Deployment & Cabling"              tasks  0 –  3  ( 4 tasks)
 *    Group 1  "Router Configuration"                     tasks  4 –  9  ( 6 tasks)
 *    Group 2  "Access Point Configuration"               tasks 10 – 14  ( 5 tasks)
 *    Group 3  "Switch Integration"                       tasks 15 – 16  ( 2 tasks)
 *    Group 4  "Full Device Deployment"                   tasks 17 – 20  ( 4 tasks)
 *    Group 5  "Server Desktop — IP & Network Settings"   tasks 21 – 37  (17 tasks)
 *    Group 6  "Second Desktop — IP & Network Settings"   tasks 38 – 54  (17 tasks)
 *    Group 7  "Laptop — WiFi & Network Settings"         tasks 55 – 73  (19 tasks)
 *
 *  ── INSPECTOR ASSIGNMENTS ────────────────────────────────────────
 *    taskParent           → VerticalLayoutGroup parent for active task rows
 *    finishedParent       → Off-screen parent for completed task GameObjects
 *    taskObjects[0..73]   → Task row GameObjects (TMP_Text labels)
 *    allTasksCompletedText→ (optional) GameObject shown when all tasks done
 *
 *    groupTitleTMP        → TMP_Text above the task list
 *    groupTitleBtn        → Button on / wrapping the group title TMP
 *    flashColor           → Color flashed on group complete (default green)
 *    flashDuration        → Seconds the flash lasts (default 1)
 *    cocLabel             → e.g. "COC II - IP Configuration"
 *
 *    groupOverviewPopup   → Popup panel GameObject (starts INACTIVE)
 *    groupRowParent       → Transform inside popup where rows are spawned
 *    groupRowPrefab       → Prefab for one popup row (3 TMP children)
 *    closePopupBtn        → Button to close the popup
 *    groupPendingColor    → Row color for groups not yet started
 *    groupActiveColor     → Row color for the current active group
 *    groupDoneColor       → Row color for completed groups
 *
 *    taskGroups[]         → TaskGroup array (8 entries, configure in Inspector)
 *    topicManagerCompletionIndex → -1 to disable TopicManager call on finish
 *
 *    (See full hardware/panel refs below in Inspector headers)
 *
 *  ── PATTERN ──────────────────────────────────────────────────────
 *    Latch-based: once a task condition is true it latches permanently.
 *    Sliding window: up to 3 unlatched tasks shown at a time, capped to
 *    the current active group — tasks from the next group never bleed in.
 *    One active group at a time; group advances only after all its tasks
 *    are latched (after a 1-second green flash on the group title TMP).
 * ================================================================
 */

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IPConfigTaskManager : MonoBehaviour, ITaskCategory
{
    public static IPConfigTaskManager Instance { get; private set; }

    public static event System.Action OnTasksUpdated;

    private const int TASK_COUNT = 74;

    // ── Task UI ───────────────────────────────────────────────────────────────

    [Header("Task UI")]
    [SerializeField] private Transform    taskParent;
    [SerializeField] private Transform    finishedParent;
    [SerializeField] private GameObject[] taskObjects = new GameObject[TASK_COUNT];
    [SerializeField] private GameObject   allTasksCompletedText;

    // ── Group Title ───────────────────────────────────────────────────────────

    [Header("Group Title")]
    [SerializeField] private TMP_Text groupTitleTMP;
    [SerializeField] private Button   groupTitleBtn;
    [SerializeField] private Color    flashColor    = Color.green;
    [SerializeField] private float    flashDuration = 1f;
    [SerializeField] private string   cocLabel      = "COC II - IP Configuration";

    // ── Group Overview Popup ──────────────────────────────────────────────────

    [Header("Group Overview Popup")]
    [SerializeField] private GameObject groupOverviewPopup;
    [SerializeField] private Transform  groupRowParent;
    [SerializeField] private GameObject groupRowPrefab;
    [SerializeField] private Button     closePopupBtn;
    [SerializeField] private Color      groupPendingColor = Color.gray;
    [SerializeField] private Color      groupActiveColor  = Color.yellow;
    [SerializeField] private Color      groupDoneColor    = Color.green;

    // ── Task Groups ───────────────────────────────────────────────────────────

    [Header("Task Groups (configure all entries in Inspector)")]
    [SerializeField] private TaskGroup[] taskGroups;

    // ── Completion Routing ────────────────────────────────────────────────────

    [Header("Completion Routing")]
    [Tooltip("Index passed to TopicManager.MarkTopicComplete() on full completion. -1 = disabled.")]
    [SerializeField] private int topicManagerCompletionIndex = -1;

    // ── Hardware Holders ──────────────────────────────────────────────────────

    [Header("Hardware Holders — Configurator")]
    [SerializeField] private NetworkHardwareHolder computer1Holder;
    [SerializeField] private NetworkHardwareHolder computer2Holder;
    [SerializeField] private NetworkHardwareHolder laptopHolder;

    [Header("Hardware Holders — Network Devices")]
    [SerializeField] private NetworkHardwareHolder routerHolder;
    [SerializeField] private NetworkHardwareHolder apHolder;
    [SerializeField] private NetworkHardwareHolder switchHolder;

    // ── Device Ports ──────────────────────────────────────────────────────────

    [Header("Device Ports — Configurator")]
    [SerializeField] private NetworkDevicePort computer1Port;
    [SerializeField] private NetworkDevicePort computer2Port;
    [SerializeField] private NetworkDevicePort laptopPort;

    [Header("Device Ports — Network Devices")]
    [SerializeField] private NetworkDevicePort routerPort;
    [SerializeField] private NetworkDevicePort apPort;
    [SerializeField] private NetworkDevicePort switchPort;
    [SerializeField] private NetworkDevicePort patchPanelPort;

    // ── Phase 2 Managers ──────────────────────────────────────────────────────

    [Header("Phase 2 Managers — Configurator")]
    [SerializeField] private NetworkDevicePhase2Manager computer1Phase2;
    [SerializeField] private NetworkDevicePhase2Manager computer2Phase2;
    [SerializeField] private NetworkDevicePhase2Manager laptopPhase2;

    [Header("Phase 2 Managers — Network Devices")]
    [SerializeField] private NetworkDevicePhase2Manager routerPhase2;
    [SerializeField] private NetworkDevicePhase2Manager apPhase2;
    [SerializeField] private NetworkDevicePhase2Manager patchPanelPhase2;
    [SerializeField] private NetworkDevicePhase2Manager switchPhase2;

    // ── Reset Controllers ─────────────────────────────────────────────────────

    [Header("Reset Controllers")]
    [SerializeField] private RouterResetController      routerReset;
    [SerializeField] private AccessPointResetController apReset;

    // ── WebUI Controllers ─────────────────────────────────────────────────────

    [Header("Router WebUI")]
    [SerializeField] private TPLinkTabManager tpLinkTabManager;

    [Header("AP WebUI")]
    [SerializeField] private DLinkAPManager dLinkAPManager;

    // ── Virtual OS Panels ─────────────────────────────────────────────────────

    [Header("Virtual OS — Shared Panels")]
    [SerializeField] private NetworkSharingCenterController networkSharingCenter;
    [SerializeField] private EthernetPropertiesController   ethernetPropertiesController;
    [SerializeField] private IPv4PropertiesController       ipv4PropertiesController;
    [SerializeField] private AdvancedSharingController      advancedSharingController;
    [SerializeField] private FirewallController             firewallController;
    [SerializeField] private InternetPanelController        internetPanelController;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private bool[]       _latched           = new bool[TASK_COUNT];
    private int          _currentTask       = 0;
    private int          _currentGroupIndex = 0;
    private bool         _allDone           = false;
    private bool         _isFlashing        = false;
    private TMP_Text[][] _popupRowTMPs;

    private static readonly Color DefaultTaskColor  = new Color(1.000f, 0.886f, 0.596f);
    private static readonly Color DefaultTitleColor = new Color(0.953f, 0.839f, 0.020f);

    private Queue<int>   _taskFlashQueue = new Queue<int>();
    private bool         _isTaskFlashing = false;

    // Captured when tasks 21 and 38 latch — used to gate device-specific tasks.
    private DeviceID _serverDevice;
    private DeviceID _secondDesktopDevice;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        groupTitleBtn?.onClick.AddListener(OpenPopup);
        closePopupBtn?.onClick.AddListener(ClosePopup);
    }

    private void Start()
    {
        for (int i = 0; i < TASK_COUNT; i++)
        {
            if (taskObjects[i] == null) continue;
            var tmp = taskObjects[i].GetComponent<TMP_Text>();
            if (tmp != null) tmp.color = DefaultTaskColor;
        }
        if (groupTitleTMP != null) groupTitleTMP.color = DefaultTitleColor;

        groupOverviewPopup?.SetActive(false);
        BuildPopupRows();
        RefreshGroupTitle();
        RefreshPopup();
        RefreshWindow();
    }

    private void OnEnable()
    {
        RefreshGroupTitle();
        if (groupOverviewPopup != null && groupOverviewPopup.activeSelf)
            RefreshPopup();
    }

    private void Update()
    {
        if (_allDone) return;
        CheckCurrentTask();
    }

    // ── Task condition checks ─────────────────────────────────────────────────

    private bool CheckCondition(int index)
    {
        switch (index)
        {
            // ── Group 0: Device Deployment & Cabling ──────────────────────────
            case 0:
                return (IsDeployed(computer1Holder) || IsDeployed(computer2Holder) || IsDeployed(laptopHolder)) &&
                       IsDeployed(routerHolder) && IsDeployed(apHolder);
            case 1:
                return (HasCables(computer1Port) || HasCables(computer2Port) || HasCables(laptopPort)) &&
                       HasCables(routerPort) && HasCables(apPort);
            case 2: {
                bool ok = true;
                if (IsDeployed(computer1Holder) && !AllPhase2Installed(computer1Phase2)) ok = false;
                if (IsDeployed(computer2Holder) && !AllPhase2Installed(computer2Phase2)) ok = false;
                if (IsDeployed(laptopHolder)    && !AllPhase2Installed(laptopPhase2))    ok = false;
                if (!AllPhase2Installed(routerPhase2))     ok = false;
                if (!AllPhase2Installed(apPhase2))         ok = false;
                if (!AllPhase2Installed(patchPanelPhase2)) ok = false;
                return ok;
            }
            case 3:
                return routerReset != null && routerReset.IsDefaultIPReady &&
                       apReset     != null && apReset.IsDefaultIPReady;

            // ── Group 1: Router Configuration ─────────────────────────────────
            case 4:
                return VirtualOSManager.Instance != null && VirtualOSManager.Instance.IsOpen;
            case 5: {
                var s = VirtualOSManager.Instance?.CurrentState;
                return s != null && s.ChromeCurrentPage >= 1;
            }
            case 6: {
                var s = VirtualOSManager.Instance?.CurrentState;
                return s != null && s.ChromeCurrentPage >= 2;
            }
            case 7:
                return tpLinkTabManager != null && tpLinkTabManager.HasLiveModifiedWireless;
            case 8:
                return tpLinkTabManager != null &&
                       tpLinkTabManager.HasSavedConfiguredWireless &&
                       tpLinkTabManager.LanPanelVisible;
            case 9:
                return tpLinkTabManager != null && tpLinkTabManager.HasSavedStaticLanConfig;

            // ── Group 2: Access Point Configuration ───────────────────────────
            case 10: {
                var s = VirtualOSManager.Instance?.CurrentState;
                return s != null && s.ChromeCurrentPage == 3;
            }
            case 11:
                return dLinkAPManager != null && dLinkAPManager.HasLiveModifiedWireless;
            case 12:
                return dLinkAPManager != null &&
                       dLinkAPManager.HasSavedAtLeastOnce &&
                       dLinkAPManager.NetworkPanelVisible;
            case 13:
                return dLinkAPManager != null && dLinkAPManager.HasLiveStaticLanConfig;
            case 14:
                return apReset != null && apReset.IsConfigured;

            // ── Group 3: Switch Integration ───────────────────────────────────
            case 15:
                return patchPanelPhase2 != null &&
                       patchPanelPhase2.HasUninstalledEntryFor(routerPort) &&
                       patchPanelPhase2.HasUninstalledEntryFor(apPort);
            case 16:
                return IsDeployed(switchHolder) &&
                       IsConnectedTo(routerPort, switchPort) &&
                       IsConnectedTo(apPort, switchPort);

            // ── Group 4: Full Device Deployment ───────────────────────────────
            case 17:
                return IsDeployed(computer1Holder) && IsDeployed(computer2Holder) && IsDeployed(laptopHolder);
            case 18:
                return HasCables(computer1Port) && HasCables(computer2Port);
            case 19:
                return AllPhase2Installed(computer1Phase2) && AllPhase2Installed(computer2Phase2);
            case 20:
                return patchPanelPhase2 != null &&
                       patchPanelPhase2.HasInstalledEntryFor(computer1Port) &&
                       patchPanelPhase2.HasInstalledEntryFor(computer2Port) &&
                       patchPanelPhase2.HasInstalledEntryFor(switchPort) &&
                       switchPhase2 != null &&
                       switchPhase2.HasInstalledEntryFor(routerPort) &&
                       switchPhase2.HasInstalledEntryFor(apPort) &&
                       switchPhase2.HasInstalledEntryFor(patchPanelPort);

            // ── Group 5: Server Desktop — IP & Network Settings ───────────────
            case 21:
                return VirtualOSManager.Instance != null &&
                       VirtualOSManager.Instance.IsOpen &&
                       (VirtualOSManager.Instance.CurrentDevice == DeviceID.Computer1 ||
                        VirtualOSManager.Instance.CurrentDevice == DeviceID.Computer2);
            case 22:
                return IsOnServerDevice() &&
                       networkSharingCenter != null &&
                       networkSharingCenter.gameObject.activeInHierarchy;
            case 23:
                return IsOnServerDevice() &&
                       ethernetPropertiesController != null &&
                       ethernetPropertiesController.gameObject.activeInHierarchy;
            case 24:
                return IsOnServerDevice() &&
                       ipv4PropertiesController != null &&
                       ipv4PropertiesController.gameObject.activeInHierarchy &&
                       VirtualOSManager.Instance.CurrentState.IPv6Unchecked;
            case 25:
                return IsOnServerDevice() &&
                       ipv4PropertiesController != null &&
                       ipv4PropertiesController.gameObject.activeInHierarchy &&
                       ipv4PropertiesController.IsStaticIPModeActive;
            case 26:
                return IsOnServerDevice() &&
                       ipv4PropertiesController != null &&
                       ipv4PropertiesController.gameObject.activeInHierarchy &&
                       MatchesIP(ipv4PropertiesController.LiveIPOctets, "192", "168", "100", "12");
            case 27:
                return IsOnServerDevice() &&
                       ipv4PropertiesController != null &&
                       ipv4PropertiesController.gameObject.activeInHierarchy &&
                       tpLinkTabManager != null &&
                       OctetsJoined(ipv4PropertiesController.LiveGatewayOctets) == tpLinkTabManager.GetRouterLanIP();
            case 28:
                return IsOnServerDevice() &&
                       ipv4PropertiesController != null &&
                       ipv4PropertiesController.gameObject.activeInHierarchy &&
                       OctetsMatch(ipv4PropertiesController.LivePreferredDNSOctets,
                                   ipv4PropertiesController.LiveIPOctets);
            case 29: {
                if (!IsOnServerDevice()) return false;
                var s = VirtualOSManager.Instance.CurrentState;
                return s.UseStaticIP && MatchesIP(s.IPOctets, "192", "168", "100", "12");
            }
            case 30:
                return IsOnServerDevice() &&
                       networkSharingCenter != null &&
                       networkSharingCenter.IsAtMainContent;
            case 31:
                return IsOnServerDevice() &&
                       advancedSharingController != null &&
                       advancedSharingController.gameObject.activeInHierarchy;
            case 32:
                return IsOnServerDevice() &&
                       advancedSharingController != null &&
                       advancedSharingController.LiveNetworkDiscoveryOn &&
                       advancedSharingController.LiveFilePrinterSharingOn;
            case 33:
                return IsOnServerDevice() &&
                       advancedSharingController != null &&
                       advancedSharingController.LivePublicFolderSharingOn &&
                       advancedSharingController.LivePasswordSharingOff;
            case 34: {
                if (!IsOnServerDevice() || advancedSharingController == null) return false;
                var s = VirtualOSManager.Instance.CurrentState;
                return s.NetworkDiscoveryOn && s.FilePrinterSharingOn &&
                       s.PublicFolderSharingOn && s.PasswordProtectedSharingOff &&
                       !advancedSharingController.gameObject.activeInHierarchy;
            }
            case 35:
                return IsOnServerDevice() &&
                       firewallController != null &&
                       firewallController.gameObject.activeInHierarchy &&
                       firewallController.IsOnOffPanelOpen;
            case 36: {
                if (!IsOnServerDevice()) return false;
                var s = VirtualOSManager.Instance.CurrentState;
                return s.FirewallPrivateOff && s.FirewallPublicOff;
            }
            case 37:
                return VirtualOSManager.Instance != null && !VirtualOSManager.Instance.IsOpen;

            // ── Group 6: Second Desktop — IP & Network Settings ───────────────
            case 38:
                return VirtualOSManager.Instance != null &&
                       VirtualOSManager.Instance.IsOpen &&
                       (VirtualOSManager.Instance.CurrentDevice == DeviceID.Computer1 ||
                        VirtualOSManager.Instance.CurrentDevice == DeviceID.Computer2) &&
                       VirtualOSManager.Instance.CurrentDevice != _serverDevice;
            case 39:
                return IsOnSecondDevice() &&
                       networkSharingCenter != null &&
                       networkSharingCenter.gameObject.activeInHierarchy;
            case 40:
                return IsOnSecondDevice() &&
                       ethernetPropertiesController != null &&
                       ethernetPropertiesController.gameObject.activeInHierarchy;
            case 41:
                return IsOnSecondDevice() &&
                       ipv4PropertiesController != null &&
                       ipv4PropertiesController.gameObject.activeInHierarchy &&
                       VirtualOSManager.Instance.CurrentState.IPv6Unchecked;
            case 42:
                return IsOnSecondDevice() &&
                       ipv4PropertiesController != null &&
                       ipv4PropertiesController.gameObject.activeInHierarchy &&
                       ipv4PropertiesController.IsStaticIPModeActive;
            case 43:
                return IsOnSecondDevice() &&
                       ipv4PropertiesController != null &&
                       ipv4PropertiesController.gameObject.activeInHierarchy &&
                       MatchesIP(ipv4PropertiesController.LiveIPOctets, "192", "168", "100", "13");
            case 44:
                return IsOnSecondDevice() &&
                       ipv4PropertiesController != null &&
                       ipv4PropertiesController.gameObject.activeInHierarchy &&
                       tpLinkTabManager != null &&
                       OctetsJoined(ipv4PropertiesController.LiveGatewayOctets) == tpLinkTabManager.GetRouterLanIP();
            case 45:
                return IsOnSecondDevice() &&
                       ipv4PropertiesController != null &&
                       ipv4PropertiesController.gameObject.activeInHierarchy &&
                       OctetsMatch(ipv4PropertiesController.LivePreferredDNSOctets, GetServerIPOctets());
            case 46: {
                if (!IsOnSecondDevice()) return false;
                var s = VirtualOSManager.Instance.CurrentState;
                return s.UseStaticIP && MatchesIP(s.IPOctets, "192", "168", "100", "13");
            }
            case 47:
                return IsOnSecondDevice() &&
                       networkSharingCenter != null &&
                       networkSharingCenter.IsAtMainContent;
            case 48:
                return IsOnSecondDevice() &&
                       advancedSharingController != null &&
                       advancedSharingController.gameObject.activeInHierarchy;
            case 49:
                return IsOnSecondDevice() &&
                       advancedSharingController != null &&
                       advancedSharingController.LiveNetworkDiscoveryOn &&
                       advancedSharingController.LiveFilePrinterSharingOn;
            case 50:
                return IsOnSecondDevice() &&
                       advancedSharingController != null &&
                       advancedSharingController.LivePublicFolderSharingOn &&
                       advancedSharingController.LivePasswordSharingOff;
            case 51: {
                if (!IsOnSecondDevice() || advancedSharingController == null) return false;
                var s = VirtualOSManager.Instance.CurrentState;
                return s.NetworkDiscoveryOn && s.FilePrinterSharingOn &&
                       s.PublicFolderSharingOn && s.PasswordProtectedSharingOff &&
                       !advancedSharingController.gameObject.activeInHierarchy;
            }
            case 52:
                return IsOnSecondDevice() &&
                       firewallController != null &&
                       firewallController.gameObject.activeInHierarchy &&
                       firewallController.IsOnOffPanelOpen;
            case 53: {
                if (!IsOnSecondDevice()) return false;
                var s = VirtualOSManager.Instance.CurrentState;
                return s.FirewallPrivateOff && s.FirewallPublicOff;
            }
            case 54:
                return VirtualOSManager.Instance != null && !VirtualOSManager.Instance.IsOpen;

            // ── Group 7: Laptop — WiFi & Network Settings ─────────────────────
            case 55:
                return VirtualOSManager.Instance != null &&
                       VirtualOSManager.Instance.IsOpen &&
                       VirtualOSManager.Instance.CurrentDevice == DeviceID.Laptop;
            case 56:
                return IsOnLaptop() &&
                       internetPanelController != null &&
                       internetPanelController.IsPasswordPanelOpen;
            case 57:
                return VirtualOSManager.Instance != null &&
                       VirtualOSManager.Instance.GetState(DeviceID.Laptop).WifiConnected;
            case 58:
                return IsOnLaptop() &&
                       networkSharingCenter != null &&
                       networkSharingCenter.gameObject.activeInHierarchy;
            case 59:
                return IsOnLaptop() &&
                       ethernetPropertiesController != null &&
                       ethernetPropertiesController.gameObject.activeInHierarchy;
            case 60:
                return IsOnLaptop() &&
                       ipv4PropertiesController != null &&
                       ipv4PropertiesController.gameObject.activeInHierarchy &&
                       VirtualOSManager.Instance.CurrentState.IPv6Unchecked;
            case 61:
                return IsOnLaptop() &&
                       ipv4PropertiesController != null &&
                       ipv4PropertiesController.gameObject.activeInHierarchy &&
                       ipv4PropertiesController.IsStaticIPModeActive;
            case 62:
                return IsOnLaptop() &&
                       ipv4PropertiesController != null &&
                       ipv4PropertiesController.gameObject.activeInHierarchy &&
                       MatchesIP(ipv4PropertiesController.LiveIPOctets, "192", "168", "100", "14");
            case 63:
                return IsOnLaptop() &&
                       ipv4PropertiesController != null &&
                       ipv4PropertiesController.gameObject.activeInHierarchy &&
                       tpLinkTabManager != null &&
                       OctetsJoined(ipv4PropertiesController.LiveGatewayOctets) == tpLinkTabManager.GetRouterLanIP();
            case 64:
                return IsOnLaptop() &&
                       ipv4PropertiesController != null &&
                       ipv4PropertiesController.gameObject.activeInHierarchy &&
                       OctetsMatch(ipv4PropertiesController.LivePreferredDNSOctets, GetServerIPOctets());
            case 65: {
                if (!IsOnLaptop()) return false;
                var s = VirtualOSManager.Instance.CurrentState;
                return s.UseStaticIP && MatchesIP(s.IPOctets, "192", "168", "100", "14");
            }
            case 66:
                return IsOnLaptop() &&
                       networkSharingCenter != null &&
                       networkSharingCenter.IsAtMainContent;
            case 67:
                return IsOnLaptop() &&
                       advancedSharingController != null &&
                       advancedSharingController.gameObject.activeInHierarchy;
            case 68:
                return IsOnLaptop() &&
                       advancedSharingController != null &&
                       advancedSharingController.LiveNetworkDiscoveryOn &&
                       advancedSharingController.LiveFilePrinterSharingOn;
            case 69:
                return IsOnLaptop() &&
                       advancedSharingController != null &&
                       advancedSharingController.LivePublicFolderSharingOn &&
                       advancedSharingController.LivePasswordSharingOff;
            case 70: {
                if (!IsOnLaptop() || advancedSharingController == null) return false;
                var s = VirtualOSManager.Instance.CurrentState;
                return s.NetworkDiscoveryOn && s.FilePrinterSharingOn &&
                       s.PublicFolderSharingOn && s.PasswordProtectedSharingOff &&
                       !advancedSharingController.gameObject.activeInHierarchy;
            }
            case 71:
                return IsOnLaptop() &&
                       firewallController != null &&
                       firewallController.gameObject.activeInHierarchy &&
                       firewallController.IsOnOffPanelOpen;
            case 72: {
                if (!IsOnLaptop()) return false;
                var s = VirtualOSManager.Instance.CurrentState;
                return s.FirewallPrivateOff && s.FirewallPublicOff;
            }
            case 73:
                return VirtualOSManager.Instance != null && !VirtualOSManager.Instance.IsOpen;

            default: return false;
        }
    }

    // ── Latch & window logic ──────────────────────────────────────────────────

    private void CheckCurrentTask()
    {
        while (_currentTask < TASK_COUNT && !_allDone)
        {
            if (_latched[_currentTask]) { AdvanceIfPossible(); continue; }
            if (!CheckCondition(_currentTask)) return;
            LatchTask(_currentTask);
        }
    }

    private void LatchTask(int index)
    {
        // Capture device context before setting the latch flag.
        if (index == 21 && VirtualOSManager.Instance != null)
            _serverDevice = VirtualOSManager.Instance.CurrentDevice;
        if (index == 38 && VirtualOSManager.Instance != null)
            _secondDesktopDevice = VirtualOSManager.Instance.CurrentDevice;

        _latched[index] = true;
        ActivityLogManager.Log($"Task completed: {GetTaskLabel(index)}", ActivityLogManager.EntryType.Action);

        bool wasVisible = taskObjects[index] != null
                       && taskObjects[index].activeSelf
                       && taskObjects[index].transform.parent == taskParent;

        AdvanceIfPossible();
        OnTasksUpdated?.Invoke();

        if (wasVisible)
        {
            _taskFlashQueue.Enqueue(index);
            if (!_isTaskFlashing) StartCoroutine(ProcessTaskFlashQueue());
        }
        else
        {
            if (taskObjects[index] != null && finishedParent != null)
                taskObjects[index].transform.SetParent(finishedParent, false);
            RefreshWindow();
        }

        RefreshGroupTitle();

        if (groupOverviewPopup != null && groupOverviewPopup.activeSelf)
            RefreshPopup();

        if (!_isFlashing)
            CheckGroupCompletion();
    }

    private void AdvanceIfPossible()
    {
        while (_currentTask < TASK_COUNT && _latched[_currentTask])
            _currentTask++;

        if (_currentTask >= TASK_COUNT && !_allDone)
        {
            _allDone = true;
            if (allTasksCompletedText != null) allTasksCompletedText.SetActive(true);
            if (topicManagerCompletionIndex >= 0)
                TopicManager.Instance?.MarkTopicComplete(topicManagerCompletionIndex);
            Debug.Log("[IPConfigTaskManager] All 74 IP Configuration tasks completed.");
        }
    }

    private void RefreshWindow()
    {
        int occupied   = (_isTaskFlashing ? 1 : 0) + _taskFlashQueue.Count;
        int maxDisplay = Mathf.Max(0, 3 - occupied);

        if (taskGroups == null || taskGroups.Length == 0)
        {
            int shown = 0;
            for (int i = _currentTask; i < TASK_COUNT && shown < maxDisplay; i++)
            {
                if (_latched[i]) continue;
                if (taskObjects[i] != null)
                {
                    taskObjects[i].transform.SetParent(taskParent, false);
                    taskObjects[i].SetActive(true);
                    shown++;
                }
            }
            return;
        }

        int groupEnd  = GetCurrentGroupLastTask();
        int displayed = 0;
        for (int i = _currentTask; i <= groupEnd && displayed < maxDisplay; i++)
        {
            if (_latched[i]) continue;
            if (taskObjects[i] != null)
            {
                taskObjects[i].transform.SetParent(taskParent, false);
                taskObjects[i].SetActive(true);
                displayed++;
            }
        }
    }

    private IEnumerator ProcessTaskFlashQueue()
    {
        _isTaskFlashing = true;
        while (_taskFlashQueue.Count > 0)
        {
            int idx = _taskFlashQueue.Dequeue();
            RefreshWindow();
            var tmp = taskObjects[idx]?.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.color     = Color.green;
                tmp.fontStyle |= FontStyles.Strikethrough;
            }
            yield return new WaitForSeconds(flashDuration);
            if (taskObjects[idx] != null && finishedParent != null)
                taskObjects[idx].transform.SetParent(finishedParent, false);
        }
        _isTaskFlashing = false;
        RefreshWindow();
    }

    // ── Group logic ───────────────────────────────────────────────────────────

    private int GetCurrentGroupLastTask()
    {
        if (taskGroups == null || _currentGroupIndex >= taskGroups.Length)
            return TASK_COUNT - 1;
        var g = taskGroups[_currentGroupIndex];
        return Mathf.Min(g.FirstTask + g.TaskCount - 1, TASK_COUNT - 1);
    }

    private int CountCompletedInGroup(int groupIndex)
    {
        if (taskGroups == null || groupIndex >= taskGroups.Length) return 0;
        var g    = taskGroups[groupIndex];
        int last = Mathf.Min(g.FirstTask + g.TaskCount - 1, TASK_COUNT - 1);
        int count = 0;
        for (int i = g.FirstTask; i <= last; i++)
            if (_latched[i]) count++;
        return count;
    }

    private void CheckGroupCompletion()
    {
        if (taskGroups == null || _currentGroupIndex >= taskGroups.Length) return;
        var g = taskGroups[_currentGroupIndex];
        if (CountCompletedInGroup(_currentGroupIndex) >= g.TaskCount)
            StartCoroutine(FlashAndAdvance());
    }

    private IEnumerator FlashAndAdvance()
    {
        _isFlashing = true;

        Color original = groupTitleTMP != null ? groupTitleTMP.color : Color.white;
        if (groupTitleTMP != null)
        {
            groupTitleTMP.color     = flashColor;
            groupTitleTMP.fontStyle |= FontStyles.Strikethrough;
        }

        yield return new WaitForSeconds(flashDuration);

        if (groupTitleTMP != null)
        {
            groupTitleTMP.color     = original;
            groupTitleTMP.fontStyle &= ~FontStyles.Strikethrough;
        }
        _isFlashing = false;

        if (_allDone)
        {
            if (groupTitleTMP != null)
                groupTitleTMP.text = $"Objective in {cocLabel} is met, all tasks have been completed.";
        }
        else
        {
            _currentGroupIndex++;
            RefreshGroupTitle();
            RefreshWindow();
        }

        if (groupOverviewPopup != null && groupOverviewPopup.activeSelf)
            RefreshPopup();
    }

    private void RefreshGroupTitle()
    {
        if (groupTitleTMP == null || taskGroups == null || _currentGroupIndex >= taskGroups.Length) return;
        var g         = taskGroups[_currentGroupIndex];
        int completed = CountCompletedInGroup(_currentGroupIndex);
        groupTitleTMP.text = $"{g.Title}  {completed} / {g.TaskCount}";
    }

    // ── Group Overview Popup ──────────────────────────────────────────────────

    private void OpenPopup()
    {
        RefreshPopup();
        groupOverviewPopup?.SetActive(true);
    }

    private void ClosePopup()
    {
        groupOverviewPopup?.SetActive(false);
    }

    private void BuildPopupRows()
    {
        if (groupRowParent == null || groupRowPrefab == null || taskGroups == null) return;

        foreach (Transform child in groupRowParent)
            Destroy(child.gameObject);

        _popupRowTMPs = new TMP_Text[taskGroups.Length][];
        for (int g = 0; g < taskGroups.Length; g++)
        {
            var row  = Instantiate(groupRowPrefab, groupRowParent);
            var tmps = row.GetComponentsInChildren<TMP_Text>();
            _popupRowTMPs[g] = new TMP_Text[3];
            if (tmps.Length >= 3)
            {
                _popupRowTMPs[g][0] = tmps[0];
                _popupRowTMPs[g][1] = tmps[1];
                _popupRowTMPs[g][2] = tmps[2];
            }
        }
    }

    private void RefreshPopup()
    {
        if (_popupRowTMPs == null || taskGroups == null) return;

        for (int g = 0; g < taskGroups.Length; g++)
        {
            if (_popupRowTMPs[g] == null) continue;

            var   group     = taskGroups[g];
            int   completed = CountCompletedInGroup(g);
            bool  isDone    = completed >= group.TaskCount;

            string statusText;
            Color  rowColor;

            if (isDone)
            {
                statusText = "Done";
                rowColor   = groupDoneColor;
            }
            else if (g == _currentGroupIndex)
            {
                statusText = "Active";
                rowColor   = groupActiveColor;
            }
            else
            {
                statusText = "Pending";
                rowColor   = groupPendingColor;
            }

            if (_popupRowTMPs[g][0] != null) { _popupRowTMPs[g][0].text = group.Title;                        _popupRowTMPs[g][0].color = rowColor; }
            if (_popupRowTMPs[g][1] != null) { _popupRowTMPs[g][1].text = $"{completed} / {group.TaskCount}"; _popupRowTMPs[g][1].color = rowColor; }
            if (_popupRowTMPs[g][2] != null) { _popupRowTMPs[g][2].text = statusText;                         _popupRowTMPs[g][2].color = rowColor; }
        }
    }

    // ── Task Labels ───────────────────────────────────────────────────────────

    private static string GetTaskLabel(int index)
    {
        return index switch
        {
            // ── Group 0 ───────────────────────────────────────────────────────
             0 => "Deploy at least 1 configurator device, Router, and Access Point.",
             1 => "Connect all deployed devices with logical network cables.",
             2 => "Install port cable in all connected device detail views.",
             3 => "Factory reset both the Router and Access Point.",

            // ── Group 1 ───────────────────────────────────────────────────────
             4 => "Enter Virtual OS from the configured device's detail view.",
             5 => "Open Chrome and navigate to the Router login page.",
             6 => "Log in with \"admin\" to enter the TP-Link WebUI.",
             7 => "Configure the SSID and Pre-Shared Key in the Wireless tab.",
             8 => "Save wireless configuration and navigate to the LAN tab.",
             9 => "Configure DHCP and IP Address, then save to finish Router config.",

            // ── Group 2 ───────────────────────────────────────────────────────
            10 => "Open Chrome and type \"dlinkap\" to enter the D-Link AP WebUI.",
            11 => "Configure Wireless Name (SSID), Security Mode (WPA-Personal), Pre-Shared Key.",
            12 => "Save wireless settings and navigate to the Network tab.",
            13 => "Configure LAN: Static IP, IP/Subnet/Gateway/DNS (192.168.100.x).",
            14 => "Save the Access Point configuration.",

            // ── Group 3 ───────────────────────────────────────────────────────
            15 => "Exit Virtual OS and unplug port cables from Router and Access Point.",
            16 => "Connect Router and Access Point from Patch Panel to Switch.",

            // ── Group 4 ───────────────────────────────────────────────────────
            17 => "Deploy the remaining devices (both Desktops and Laptop).",
            18 => "Connect LAN cable (Phase 1) to the newly deployed desktops.",
            19 => "Enter detail view of new desktops and plug Phase 2 cable into port.",
            20 => "Connect LAN cable to Patch Panel and Switch; plug Phase 2 cables into their ports.",

            // ── Group 5 ───────────────────────────────────────────────────────
            21 => "Enter the Virtual OS of one of the two deployed desktops (server).",
            22 => "Open Network and Sharing Center.",
            23 => "Click Change Adapter Settings and open the Ethernet Properties panel.",
            24 => "Uncheck IPv6, select IPv4, and click Properties.",
            25 => "Toggle Use the following IP address to enable manual config.",
            26 => "Input \"192.168.100.12\" for this server device's IP address.",
            27 => "Input the router's IP address in the Default Gateway field.",
            28 => "Input this device's IP address in the Preferred DNS field.",
            29 => "Click OK to save the IP configuration.",
            30 => "Close Network Properties and click Home to return to main content.",
            31 => "Click Change Advanced Sharing Settings.",
            32 => "Toggle Guest/Public: turn on Network Discovery and File and Printer Sharing.",
            33 => "Toggle All Networks: turn on Public Folder Sharing, turn off Password Protected Sharing.",
            34 => "Click Save Changes then click Return.",
            35 => "Click Windows Defender Firewall, then click Turn Firewall On and Off.",
            36 => "Toggle Private and Public Network Settings both to Off.",
            37 => "Close Network and Sharing Center and exit the server desktop Virtual OS.",

            // ── Group 6 ───────────────────────────────────────────────────────
            38 => "Enter the Virtual OS of the other desktop (not the server).",
            39 => "Open Network and Sharing Center.",
            40 => "Click Change Adapter Settings and open the Ethernet Properties panel.",
            41 => "Uncheck IPv6, select IPv4, and click Properties.",
            42 => "Toggle Use the following IP address to enable manual config.",
            43 => "Input \"192.168.100.13\" for this desktop's IP address.",
            44 => "Input the router's IP address in the Default Gateway field.",
            45 => "Input the server desktop's IP address in the Preferred DNS field.",
            46 => "Click OK to save the IP configuration.",
            47 => "Close Network Properties and click Home to return to main content.",
            48 => "Click Change Advanced Sharing Settings.",
            49 => "Toggle Guest/Public: turn on Network Discovery and File and Printer Sharing.",
            50 => "Toggle All Networks: turn on Public Folder Sharing, turn off Password Protected Sharing.",
            51 => "Click Save Changes then click Return.",
            52 => "Click Windows Defender Firewall, then click Turn Firewall On and Off.",
            53 => "Toggle Private and Public Network Settings both to Off.",
            54 => "Close Network and Sharing Center and exit the second desktop Virtual OS.",

            // ── Group 7 ───────────────────────────────────────────────────────
            55 => "Enter the Virtual OS of the Laptop.",
            56 => "Click the WiFi icon and connect to the Access Point network.",
            57 => "Enter the security key and connect to WiFi.",
            58 => "Close Internet Panel and open Network and Sharing Center.",
            59 => "Click Change Adapter Settings and open the Ethernet Properties panel.",
            60 => "Uncheck IPv6, select IPv4, and click Properties.",
            61 => "Toggle Use the following IP address to enable manual config.",
            62 => "Input \"192.168.100.14\" for the laptop's IP address.",
            63 => "Input the router's IP address in the Default Gateway field.",
            64 => "Input the server desktop's IP address in the Preferred DNS field.",
            65 => "Click OK to save the IP configuration.",
            66 => "Close Network Properties and click Home to return to main content.",
            67 => "Click Change Advanced Sharing Settings.",
            68 => "Toggle Guest/Public: turn on Network Discovery and File and Printer Sharing.",
            69 => "Toggle All Networks: turn on Public Folder Sharing, turn off Password Protected Sharing.",
            70 => "Click Save Changes then click Return.",
            71 => "Click Windows Defender Firewall, then click Turn Firewall On and Off.",
            72 => "Toggle Private and Public Network Settings both to Off.",
            73 => "Close Network and Sharing Center and exit the Laptop Virtual OS.",

            _  => $"Task {index + 1}"
        };
    }

    // ── Device Guards ─────────────────────────────────────────────────────────

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

    // ── Condition Helpers ─────────────────────────────────────────────────────

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

    private string[] GetServerIPOctets()
    {
        if (VirtualOSManager.Instance == null) return new string[4];
        return VirtualOSManager.Instance.GetState(_serverDevice).IPOctets;
    }

    // ── Hardware Helpers ──────────────────────────────────────────────────────

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

    // ── ITaskCategory ─────────────────────────────────────────────────────────

    public string GetNextIncompleteTaskText()
    {
        if (_allDone || _currentTask >= TASK_COUNT) return "All tasks completed!";
        return GetTaskLabel(_currentTask);
    }

    public Color GetDisplayColor(Color fallback) => _allDone ? Color.green : fallback;

    public void HideCompletionBanner()
    {
        if (allTasksCompletedText != null)
            allTasksCompletedText.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public bool     IsFullyComplete      => _allDone;
    public DeviceID ServerDevice         => _serverDevice;
    public DeviceID SecondDesktopDevice  => _secondDesktopDevice;
}
