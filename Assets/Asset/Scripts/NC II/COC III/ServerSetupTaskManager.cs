/*
 * ================================================================
 *  UNITY SETUP GUIDE — ServerSetupTaskManager (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to an always-active "Managers" GameObject in the COC III scene.
 *
 *  ── HIERARCHY ────────────────────────────────────────────────────
 *
 *    Task Panel
 *      ├── Group Title Row                   (always visible above task list)
 *      │     ├── GroupTitleTMP               TMP_Text  → groupTitleTMP
 *      │     │     Text auto-set: "Configure DNS  2 / 18"
 *      │     └── Button component            Button  → groupTitleBtn
 *      │           OnClick → opens Group Overview Popup
 *      │
 *      ├── Task List                         (active task rows parented here)
 *      │     └── [taskObjects[0..137] reparented here at runtime]
 *      │
 *      └── Finished Bucket                  (off-screen; latched tasks move here)
 *
 *    Group Overview Popup                   → groupOverviewPopup (INACTIVE)
 *      ├── Header / Title TMP               (static "Task Groups" label, optional)
 *      ├── Row Parent                       → groupRowParent  (VerticalLayoutGroup)
 *      │     └── [GroupRowPrefab × 13 spawned here at runtime]
 *      └── CloseBtn                         Button  → closePopupBtn
 *
 *  ── GROUP ROW PREFAB ─────────────────────────────────────────────
 *    Root: any layout container (e.g. HorizontalLayoutGroup)
 *      ├── NameTMP      TMP_Text  [0] — group title  (left-aligned)
 *      ├── ProgressTMP  TMP_Text  [1] — "2 / 18"     (center-aligned)
 *      └── StatusTMP    TMP_Text  [2] — "Pending" / "Active" / "Done"
 *    GetComponentsInChildren<TMP_Text>() must return them in order [0][1][2].
 *
 *  ── TASK GROUPS — 13 groups, 138 tasks total ─────────────────────
 *    Group  0  "Pre-Server Configuration - Name and Password"          tasks   0 –   6
 *    Group  1  "Pre-Server Configuration - IP Address"                 tasks   7 –  14
 *    Group  2  "DNS Role - Lookup Zone Configuration"                  tasks  15 –  32
 *    Group  3  "DHCP Role - Scope Configuration"                       tasks  33 –  47
 *    Group  4  "ADDS Role - Domain Promotion"                          tasks  48 –  56
 *    Group  5  "ADDS Role - Creation of Organization unit and User"    tasks  57 –  65
 *    Group  6  "GPM - Creation of Group Policy Object & Adding Users"  tasks  66 –  72
 *    Group  7  "File Explorer - File Sharing Environment Creation"     tasks  73 –  84
 *    Group  8  "GPM - Group Policy Management Editor"                  tasks  85 –  92
 *    Group  9  "Print and Document Services"                           tasks  93 – 106
 *    Group 10  "RDS: Setting up Remote Desktop Service"                tasks 107 – 113
 *    Group 11  "Client Side: Joining the Domain as user"               tasks 114 – 131
 *    Group 12  "Client Side - Configuring Server Manager through Client PC"  tasks 132 – 137
 *
 *  ── INSPECTOR ASSIGNMENTS ────────────────────────────────────────
 *    taskParent           → VerticalLayoutGroup parent for active task rows
 *    finishedParent       → Off-screen parent for completed task GameObjects
 *    taskObjects[0..137]  → Task row GameObjects (TMP_Text labels)
 *    allTasksCompletedText→ (optional) GameObject shown when all tasks done
 *
 *    groupTitleTMP        → TMP_Text above the task list
 *    groupTitleBtn        → Button on / wrapping the group title TMP
 *    flashColor           → Color flashed on group complete (default green)
 *    flashDuration        → Seconds the flash lasts (default 1)
 *    cocLabel             → e.g. "COC III" — used in completion message
 *
 *    groupOverviewPopup   → Popup panel GameObject (starts INACTIVE)
 *    groupRowParent       → Transform inside popup where rows are spawned
 *    groupRowPrefab       → Prefab for one popup row (3 TMP children)
 *    closePopupBtn        → Button to close the popup
 *    groupPendingColor    → Row color for groups not yet started
 *    groupActiveColor     → Row color for the current active group
 *    groupDoneColor       → Row color for completed groups
 *
 *    taskGroups[]         → TaskGroup array (13 entries, configure in Inspector)
 *    serverHolder         → NetworkHardwareHolder for the Server PC
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
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TaskGroup
{
    public string Title;
    public int    FirstTask;
    public int    TaskCount;
}

public class ServerSetupTaskManager : MonoBehaviour
{
    public static ServerSetupTaskManager Instance { get; private set; }

    private const int TASK_COUNT = 138;

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
    [SerializeField] private string   cocLabel      = "COC III";

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

    // ── Hardware ──────────────────────────────────────────────────────────────

    [Header("Hardware")]
    [SerializeField] private NetworkHardwareHolder serverHolder;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private bool[]       _latched           = new bool[TASK_COUNT];
    private int          _currentTask       = 0;
    private int          _currentGroupIndex = 0;
    private bool         _allDone           = false;
    private bool         _isFlashing        = false;
    private TMP_Text[][] _popupRowTMPs;

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
        groupOverviewPopup?.SetActive(false);
        BuildPopupRows();
        RefreshGroupTitle();
        RefreshPopup();
        RefreshWindow();
    }

    private void Update()
    {
        if (_allDone) return;
        CheckCurrentTask();
    }

    // ── Task condition checks ─────────────────────────────────────────────────

    private bool CheckCondition(int index)
    {
        var mgr    = ServerVirtualOSManager.Instance;
        var state  = mgr?.ServerState;
        var client = mgr?.ClientState;
        if (state == null) return false;

        return index switch
        {
            // ── Group 0: Pre-Server Configuration - Name and Password ─────────
             0 => serverHolder != null && serverHolder.hardwarePrefab != null && serverHolder.hardwarePrefab.activeSelf,
             1 => !string.IsNullOrEmpty(state.CurrentLoggedInUser),
             2 => state.SettingsAboutOpened,
             3 => state.PCRenamed,
             4 => state.SettingsAccountsOpened,
             5 => state.PasswordSet,
             6 => state.SettingsClosed,

            // ── Group 1: Pre-Server Configuration - IP Address ────────────────
             7 => state.NSCOpened,
             8 => state.AdapterSettingsVisited,
             9 => state.EthernetPropertiesOpened,
            10 => state.IPv4PanelOpened,
            11 => state.UseStaticIP,
            12 => state.IPConfigured,
            13 => state.IPv4Applied,
            14 => state.NSCClosed,

            // ── Group 2: DNS Role - Lookup Zone Configuration ─────────────────
            15 => state.ServerManagerOpened,
            16 => state.AddRolesWizardOpenedForDNS,
            17 => state.DNSRoleSelectedInWizard,
            18 => state.DNSInstalled && state.DNSManagerOpened,
            19 => state.ForwardZoneWizardOpened,
            20 => state.ForwardZoneTypeSelected,
            21 => state.ForwardZoneLookupTypeSelected,
            22 => state.ForwardZoneNameEntered,
            23 => state.ForwardZoneFileCreated,
            24 => state.DNSForwardZoneCreated,
            25 => state.ReverseZoneWizardOpened,
            26 => state.ReverseZoneTypeSelected,
            27 => state.ReverseZoneLookupTypeSelected,
            28 => state.ReverseZoneIPv4Selected,
            29 => state.ReverseZoneNameEntered,
            30 => state.ReverseZoneFileCreated,
            31 => state.DNSReverseZoneCreated,
            32 => state.DNSManagerClosed,

            // ── Group 3: DHCP Role - Scope Configuration ──────────────────────
            33 => state.AddRolesWizardOpenedForDHCP,
            34 => state.DHCPInstalled,
            35 => state.DHCPConsoleOpened,
            36 => state.DHCPScopeWizardStarted,
            37 => state.DHCPScopeNameSet,
            38 => state.DHCPScopeStartSet && state.DHCPScopeEndSet,
            39 => state.DHCPExclusionsPageVisited,
            40 => state.DHCPLeaseDurationPageVisited,
            41 => state.DHCPConfigureNow,
            42 => state.DHCPRouterList.Count > 0,
            43 => !string.IsNullOrEmpty(state.DHCPParentDomain) && state.DHCPDNSList.Count > 0,
            44 => state.DHCPWINSPageVisited,
            45 => state.DHCPActivatedOnFinish,
            46 => state.DHCPAuthorized,
            47 => state.DHCPConsoleClosed,

            // ── Group 4: ADDS Role - Domain Promotion ─────────────────────────
            48 => state.AddRolesWizardOpenedForADDS,
            49 => state.ADDSInstalled,
            50 => state.PromotionNotificationClicked,
            51 => state.DCPromoDeploymentType == "NewForest" && !string.IsNullOrEmpty(state.DomainName),
            52 => !string.IsNullOrEmpty(state.ForestFunctionalLevel) && !string.IsNullOrEmpty(state.DomainFunctionalLevel),
            53 => !string.IsNullOrEmpty(state.DSRMPassword),
            54 => state.DNSOptionsPageVisited,
            55 => state.DCPromoInstallStarted,
            56 => state.DCPromoCompleted,

            // ── Group 5: ADDS Role - OU and User Creation ─────────────────────
            57 => state.SignedInAfterPromotion,
            58 => state.ADUCOpened,
            59 => state.OUWizardOpened,
            60 => state.OrganizationalUnits.Count >= 1,
            61 => state.UserWizardOpened,
            62 => state.UserWizardNameEntered,
            63 => state.UserWizardAdminToggled,
            64 => state.AdminAccountCreated,
            65 => state.ADUCClosed,

            // ── Group 6: GPM - GPO Creation & Adding Users ────────────────────
            66 => state.GPMOpened,
            67 => state.GPOWizardOpened,
            68 => state.GPOCreatedForAllOUs,
            69 => state.AllGPOsEnforced,
            70 => state.SecurityFilterAddDialogOpened,
            71 => state.SecurityFilterNameTyped,
            72 => state.SecurityFilteringConfigured,

            // ── Group 7: File Explorer - File Sharing Environment Creation ─────
            73 => state.FileExplorerOpened,
            74 => state.ThisPCDriveSelected,
            75 => state.NewFolderOptionClicked,
            76 => state.UserCreatedFolders.Count >= 1,
            77 => state.FolderPropertiesOpened,
            78 => state.FolderSharingTabOpened,
            79 => state.AdvancedSharingOpened,
            80 => state.ShareFolderToggled,
            81 => state.FolderPermissionsDialogOpened,
            82 => state.SharedFolderCreated,
            83 => state.FolderPropertiesClosed,
            84 => state.FileExplorerClosed,

            // ── Group 8: GPM - Group Policy Management Editor ─────────────────
            85 => state.AllGPOEditorsOpened,
            86 => state.FolderRedirectionNodeOpened,
            87 => state.FolderRedirectionPropertiesOpened,
            88 => state.FolderRedirectionSettingChanged,
            89 => state.FolderRedirectionRootPathEntered,
            90 => state.FolderRedirectionSettingsTabOpened,
            91 => state.AllDesktopRedirectsSet || state.AllDocumentsRedirectsSet,
            92 => state.GPMClosed,

            // ── Group 9: Print and Document Services ──────────────────────────
            93 => state.AddRolesWizardOpenedForPrint,
            94 => state.PrintServicesInstalled,
            95 => state.PrintMgmtOpened,
            96 => state.PrintServerNodeExpanded,
            97 => state.AddPrinterWizardOpened,
            98 => state.ExistingPortOptionSelected,
            99 => state.PrinterPortSelected,
           100 => state.InstallNewDriverSelected,
           101 => state.PrinterManufacturerSelected,
           102 => state.PrinterTypeSelected,
           103 => !string.IsNullOrEmpty(state.InstalledPrinterName),
           104 => state.PrinterDriverAdded,
           105 => state.PrinterShared,
           106 => state.PrintMgmtClosed,

            // ── Group 10: RDS - Setting up Remote Desktop Service ─────────────
           107 => state.AddRolesWizardOpenedForRD,
           108 => state.RDServicesInstalled,
           109 => state.LocalServerTabVisited,
           110 => state.RemoteDesktopPropertyClicked,
           111 => state.RemoteDesktopCheckboxToggled,
           112 => state.RemoteDesktopEnabled,
           113 => state.ServerManagerClosed,

            // ── Group 11: Client Side - Joining the Domain ────────────────────
           114 => state.ServerRestartInitiated,
           115 => state.ServerRestartCompleted,
           116 => state.SwitchPCButtonClicked,
           117 => mgr != null && mgr.CurrentPC == ActivePC.Client,
           118 => client != null && client.ClientLocalUserLoggedIn,
           119 => client != null && client.ClientSettingsOpened,
           120 => client != null && client.ClientSettingsAboutOpened,
           121 => client != null && client.ClientAdvancedSysOpened,
           122 => client != null && client.ClientDomainChangeOpened,
           123 => client != null && client.ClientDomainNameEntered,
           124 => client != null && client.ClientDomainCredsEntered,
           125 => client != null && client.DomainJoined,
           126 => client != null && client.ClientOtherUserClicked,
           127 => client != null && client.ClientDomainUserSelected,
           128 => CheckClientDomainUserLoggedIn(),
           129 => state.RDConnectionPanelOpened,
           130 => state.RDComputerNameEntered,
           131 => state.RDCredentialsEntered,

            // ── Group 12: Client Side - Server Manager via RDS ────────────────
           132 => state.ADUCOpenedInRDSSession,
           133 => state.DomainNodeExpandedInRDS,
           134 => state.OrganizationalUnits.Count >= 2,
           135 => state.RegularUserCount >= 1,
           136 => state.ADUCClosedInRDSSession,
           137 => state.ServerManagerClosedInRDS,

            _ => false
        };
    }

    // ── Latch & window logic ──────────────────────────────────────────────────

    private void CheckCurrentTask()
    {
        if (_currentTask >= TASK_COUNT) return;
        if (_latched[_currentTask]) { AdvanceIfPossible(); return; }
        if (!CheckCondition(_currentTask)) return;
        LatchTask(_currentTask);
    }

    private void LatchTask(int index)
    {
        _latched[index] = true;
        OnTaskLatched?.Invoke();
        ActivityLogManager.Log($"Task completed: {GetTaskLabel(index)}", ActivityLogManager.EntryType.Action);

        if (taskObjects[index] != null && finishedParent != null)
            taskObjects[index].transform.SetParent(finishedParent, false);

        AdvanceIfPossible();
        RefreshWindow();
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

        if (_currentTask >= TASK_COUNT)
        {
            _allDone = true;
            if (allTasksCompletedText != null) allTasksCompletedText.SetActive(true);
            Debug.Log("[ServerSetupTaskManager] All tasks completed.");
        }
    }

    private void RefreshWindow()
    {
        if (taskGroups == null || taskGroups.Length == 0)
        {
            int shown = 0;
            for (int i = _currentTask; i < TASK_COUNT && shown < 3; i++)
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
        for (int i = _currentTask; i <= groupEnd && displayed < 3; i++)
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
        if (groupTitleTMP != null) groupTitleTMP.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        if (groupTitleTMP != null) groupTitleTMP.color = original;
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
                _popupRowTMPs[g][0] = tmps[0]; // group name
                _popupRowTMPs[g][1] = tmps[1]; // progress
                _popupRowTMPs[g][2] = tmps[2]; // status
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

    // ── Task labels ───────────────────────────────────────────────────────────

    private static string GetTaskLabel(int index)
    {
        return index switch
        {
            // ── Group 0 ───────────────────────────────────────────────────────
             0 => "Deploy the computer desktop in the workspace.",
             1 => "Enter Virtual OS then Sign in as Administrator using Server PC.",
             2 => "Run Settings application then navigate to: System > About.",
             3 => "Rename your PC (make sure it is memorable).",
             4 => "Return and navigate to: System > Account.",
             5 => "Add/Change your Server PC password (make sure it is memorable).",
             6 => "Exit Settings application.",

            // ── Group 1 ───────────────────────────────────────────────────────
             7 => "Run Control Panel \"Network\" Application (Right-click to open properties).",
             8 => "Navigate to \"Change adapter settings\".",
             9 => "Right click the provided connected network and click properties.",
            10 => "Click the properties for Internet Protocol v4 (IPv4).",
            11 => "Select \"Use the following IP address\"",
            12 => "Configure all the following IP fields.",
            13 => "Press OK for the both IPv4 and Ethernet Window to confirm changes.",
            14 => "Exit the Control panel - Network and Sharing application.",

            // ── Group 2 ───────────────────────────────────────────────────────
            15 => "Run Server Manager Application.",
            16 => "Click Manage in the top nav to add roles.",
            17 => "Navigate and select DNS role in 'Server Roles' to install.",
            18 => "After installation, close the wizard then click 'tools' in the top nav and open DNS.",
            19 => "Right click the Server node and create a new zone.",
            20 => "Select primary as zone type.",
            21 => "Select Forward lookup zone.",
            22 => "Create a name for the forward lookup zone.",
            23 => "Create a new file for your zone file.",
            24 => "Proceed and finish creating the forward lookup zone.",
            25 => "Create another zone.",
            26 => "Select primary as zone type.",
            27 => "Select Reverse lookup zone.",
            28 => "select IPv4.",
            29 => "Create a name for the reverse lookup zone.",
            30 => "Create a new file for your zone file.",
            31 => "Proceed and finish creating the reverse lookup zone.",
            32 => "Close the DNS window to finish up configuration for DNS.",

            // ── Group 3 ───────────────────────────────────────────────────────
            33 => "Click manage to add new role: DHCP Server",
            34 => "Confirm and Install the DHCP Server role.",
            35 => "Close the wizard window and click tools then open DHCP window.",
            36 => "Right click IPv4 to create a new scope (Expand Server node if not yet expanded).",
            37 => "Create a name for the scope.",
            38 => "Input the IP range for your DHCP (minimum to maximum).",
            39 => "Press next to skip \"Add Exclusion and Delay\".",
            40 => "Press next to skip \"Lease Duration\".",
            41 => "Select \"Yes, I want to configure these options now\".",
            42 => "Input the Router (Default Gateway) IP.",
            43 => "Input Domain name and DNS Server Name.",
            44 => "Press next to skip \"WINS Server\".",
            45 => "Select \"Yes, I want to activate this scope now\".",
            46 => "Right click the Server node and activate the IPv4 (select Authorize).",
            47 => "Close the DHCP window to finish up configuration for DHCP.",

            // ── Group 4 ───────────────────────────────────────────────────────
            48 => "Click manage to add new role: ADDS",
            49 => "Confirm and Install the ADDS role.",
            50 => "Close the wizard window and click the flag icon to promote the ADDS.",
            51 => "Select \"Add a new forest\" then Input a Domain name.",
            52 => "Configure \"Forest functional level\" and \"Domain functional level\".",
            53 => "Input a restore password for DSRM.",
            54 => "Press next and leave DNS delegation as disabled.",
            55 => "Continue pressing next and wait for the installation process to finish.",
            56 => "Wait for the computer to load the boot to properly save the configuration.",

            // ── Group 5 ───────────────────────────────────────────────────────
            57 => "Sign in again (input the password you entered in the Settings > Account).",
            58 => "Run Server Manager then open Active Directory Users and Computers window in the tool.",
            59 => "Expand Domain node and right click to create an Organizational unit.",
            60 => "Input an organization unit name and press OK to save.",
            61 => "Right click the created organizational unit and create a new user.",
            62 => "Input: First name, Last name and User name for the user and press next.",
            63 => "Input a password for the user and toggle \"Add to domain admins group\".",
            64 => "Read to confirm the summary and finish creating the user.",
            65 => "Close the Active Directory Users and Computers window.",

            // ── Group 6 ───────────────────────────────────────────────────────
            66 => "Open Group Policy Management in tool to open the window.",
            67 => "Expand Forest node and right click the organizational unit to create group policy object.",
            68 => "Input a Group Policy Object name and press OK.",
            69 => "Select the GPO node then Right click to Enforce.",
            70 => "Click add in the Security Filtering tab.",
            71 => "Input the name of the user created in the ADUC.",
            72 => "Press check names and select the user in the list then press OK.",

            // ── Group 7 ───────────────────────────────────────────────────────
            73 => "Run File Explorer application in the taskbar.",
            74 => "Select the main disk 'This PC' has.",
            75 => "Select \"New\" in the top nav to create a new folder file.",
            76 => "Name the folder and press enter.",
            77 => "Right click the created folder' properties.",
            78 => "Switch to Sharing tab in the top nav.",
            79 => "Click \"Advance sharing\" to open a new window.",
            80 => "Toggle \"Share this folder\".",
            81 => "Click \"Permission\" to browse the permission control of the folder.",
            82 => "Press OK till you return to the main properties of the folder (read the sharing file path).",
            83 => "Press ok to close the folder's properties.",
            84 => "Exit File Explorer application to finish the creation of file sharing.",

            // ── Group 8 ───────────────────────────────────────────────────────
            85 => "Return to the Group Policy Management Window and Edit the GPO.",
            86 => "Navigate to: User Configuration > Policies > Windows Settings > Folder Redirection.",
            87 => "Right click any of the folder (Recommended: Desktop / Document) to open their properties.",
            88 => "Change the Settings dropdown to \"Basic - Redirect everyone's folder to the same location\".",
            89 => "Input the file path of the created folder from the File Explorer in the Root Path.",
            90 => "Swithc to Settings tab in the top nav to browse.",
            91 => "Press OK to confirm the changes.",
            92 => "Close the GPM Editor and the GPM window to finish the file sharing.",

            // ── Group 9 ───────────────────────────────────────────────────────
            93 => "Click manage to add new role: File Sharing and Services",
            94 => "Confirm and Install the FSS role.",
            95 => "Open the Print Management window in the tools.",
            96 => "Navigate: Print Management > Print Servers > Server (local).",
            97 => "Right click Server (local) to add a printer.",
            98 => "Select \"Add a new printer using existing port.",
            99 => "Select a Printer port (Recommended: LPT1).",
           100 => "Select \"Install a new driver\".",
           101 => "Select Microsoft as Manufacturer.",
           102 => "Select a printer type (Recommended: Microsoft to PDF).",
           103 => "Input a name fo the Printer.",
           104 => "Read to confirm the summary.",
           105 => "Finish the printer wizard.",
           106 => "Close the Print Management Window to finish the adding of printer.",

            // ── Group 10 ──────────────────────────────────────────────────────
           107 => "Click manage to add new role: Remote Desktop Service",
           108 => "Confirm and Install the RDS role.",
           109 => "Navigate to local server tab in the side bar of Server Manager.",
           110 => "Select the value of 'Remote Desktop'.",
           111 => "Toggle \"Allow remote desktop connections to this computer\".",
           112 => "Press OK to confirm changes.",
           113 => "Exit Server Manager application to finish the server management configuration.",

            // ── Group 11 ──────────────────────────────────────────────────────
           114 => "Press restart in the taskbar.",
           115 => "Wait for the boot to load.",
           116 => "Press \"Switch PC\" in the Sign in Window.",
           117 => "Select \"Client PC\" to switch to a different Computer Desktop.",
           118 => "Input the default Client PC password to sign in.",
           119 => "Run Settings application.",
           120 => "Navigate: System > About.",
           121 => "Press \"Advance system settings\".",
           122 => "Press \"Change...\" to change domain or workgroup.",
           123 => "Toggle \"Domain\" and Input the Domain name create in the Server.",
           124 => "Input the Server PC Username and Password.",
           125 => "Confirm joining the Domain to restart.",
           126 => "Wait for the boot and Press \"Other Users\"",
           127 => "Select user created in ADUC - Organizational Unit.",
           128 => "Input the password of the User to sign in.",
           129 => "Run Remote Desktop Controller application.",
           130 => "Input the name of Server PC and press connect.",
           131 => "Input the password of the User that is joining the domain.",

            // ── Group 12 ──────────────────────────────────────────────────────
           132 => "Open Active Directory Users and Computers.",
           133 => "Expand the Domain node.",
           134 => "Create an Organizational unit.",
           135 => "Create a new User in that Organizational Unit.",
           136 => "Close the Active Directory Users and Computers.",
           137 => "Exit Server Manager application.",

            _  => $"Task {index + 1}"
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool CheckClientDomainUserLoggedIn()
    {
        var mgr    = ServerVirtualOSManager.Instance;
        var client = mgr?.ClientState;
        if (client == null || mgr?.ServerState == null) return false;

        return mgr.CurrentPC == ActivePC.Client
            && client.DomainJoined
            && !string.IsNullOrEmpty(client.CurrentLoggedInUser)
            && mgr.ServerState.UserAccounts.Exists(u =>
                string.Equals(u.Username, client.CurrentLoggedInUser,
                    System.StringComparison.OrdinalIgnoreCase));
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public static event System.Action OnTaskLatched;

    public string GetCurrentTaskLabel()
    {
        if (_allDone || _currentTask >= TASK_COUNT) return null;
        return GetTaskLabel(_currentTask);
    }

    public void NotifyStateChanged() { }
}
