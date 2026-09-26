/*
 * ================================================================
 *  UNITY SETUP GUIDE — ServerSetupTaskManager (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to an always-active "Managers" GameObject in the COC III scene.
 *
 *  TASK LIST (65 tasks, 0–64, one long linear flow)
 *  ── Pre-Configuration ──────────────────────────────────────────
 *  [ 0] Deploy the Server PC to the workspace
 *  [ 1] Right-click the Server PC to open its Virtual OS
 *  [ 2] Open the Settings app on the desktop
 *  [ 3] Click "System" in the Settings sidebar
 *  [ 4] Click "About" to view computer information
 *  [ 5] Rename this PC (enter a server name and confirm)
 *  [ 6] Click "Accounts" in the Settings sidebar
 *  [ 7] Click "Change Password" under Administrator
 *  [ 8] Set and confirm the Administrator password
 *  [ 9] Open Network and Sharing Center
 *  [10] Click "Change adapter settings"
 *  [11] Open IPv4 Properties for the Local Area Connection
 *  [12] Select "Use the following IP address"
 *  [13] Enter the IP Address
 *  [14] Enter the Default Gateway
 *  [15] Enter the Preferred DNS Server address
 *  [16] Click OK to apply the network configuration
 *  ── Role Installation ───────────────────────────────────────────
 *  [17] Open Server Manager from the desktop
 *  [18] Click "Add Roles and Features" — select Active Directory Domain Services
 *  [19] Complete the wizard to install AD DS
 *  [20] Click the notification flag to promote this server to a domain controller
 *  [21] Enter the domain name in the dcpromo wizard
 *  [22] Check the "DNS Server" checkbox to install DNS alongside AD DS
 *  [23] Select the Forest Functional Level
 *  [24] Complete dcpromo and confirm the server reboot
 *  [25] Click "Add Roles and Features" — select DHCP Server
 *  [26] Complete the wizard to install DHCP Server
 *  [27] Click "Add Roles and Features" — select File Services with File Resource Manager
 *  [28] Complete the wizard to install File Services
 *  [29] Click "Add Roles and Features" — select Print and Document Services
 *  [30] Complete the wizard to install Print and Document Services
 *  [31] Click "Add Roles and Features" — select Remote Desktop Services
 *  [32] Complete the wizard to install Remote Desktop Services
 *  ── Active Directory ────────────────────────────────────────────
 *  [33] Open Tools → Active Directory Users and Computers
 *  [34] Create the first Organizational Unit (OU)
 *  [35] Create the second Organizational Unit (OU)
 *  [36] Create a User Account inside the first OU
 *  [37] Create a User Account inside the second OU
 *  [38] Create an Administrator Account (set account type to Administrator)
 *  ── DHCP ────────────────────────────────────────────────────────
 *  [39] Open Tools → DHCP Console
 *  [40] Create a new DHCP Scope and enter a scope name
 *  [41] Set the Start IP Address for the DHCP Scope
 *  [42] Set the End IP Address for the DHCP Scope
 *  [43] Activate the DHCP Scope
 *  [44] Authorize the DHCP Server
 *  ── File Services ───────────────────────────────────────────────
 *  [45] Open Tools → File Server Resource Manager
 *  [46] Create a Shared Folder
 *  [47] Set folder permissions (Everyone — Read/Write)
 *  [48] Create a File Group with blocked file extensions
 *  [49] Apply a File Screen to the shared folder
 *  ── Group Policy ────────────────────────────────────────────────
 *  [50] Open Tools → Group Policy Management
 *  [51] Create a GPO linked to each Organizational Unit
 *  [52] Open the GPO editor for every OU's GPO
 *  [53] Configure Desktop Folder Redirection in every GPO
 *  [54] Configure Documents Folder Redirection in every GPO
 *  [55] Set Enforced = Yes on every GPO
 *  [56] Add domain users to GPO Security Filtering (one per GPO)
 *  ── Print Services ──────────────────────────────────────────────
 *  [57] Open Tools → Print Management
 *  [58] Add a Printer Driver using the Add Driver Wizard
 *  [59] Share the printer and set a share name
 *  ── Remote Desktop ──────────────────────────────────────────────
 *  [60] Switch to the Client PC and log in as the domain user account
 *  [61] Open the Remote Desktop Connection app on the Client PC desktop
 *  [62] Enter the server computer name and click Connect
 *  [63] Enter domain credentials in Windows Security and click OK
 *  [64] Wait for the Remote Desktop session to establish
 *
 *  INSPECTOR SETUP
 *    taskParent            → VerticalLayoutGroup parent for active task rows
 *    finishedParent        → Off-screen parent for completed task GameObjects
 *    taskObjects[0..64]    → 65 task row GameObjects (TMP_Text labels)
 *    NOTE: TASK_COUNT is now 65. Add a new task row GO at slot [56] and
 *          re-assign slots [56] through [64] in the inspector. Old slots
 *          [56]-[63] (Print + Client) shift to [57]-[64].
 *    sectionHeaders[]      → Section divider GameObjects (non-interactive labels)
 *                            Order: Pre-Configuration, Role Installation,
 *                            Active Directory, DHCP, File Services,
 *                            Group Policy, Print Services, Remote Desktop
 *    serverHolder          → NetworkHardwareHolder (or equivalent) on the Server PC icon
 *    allTasksCompletedText → (optional) TMP shown when all tasks done
 *
 *  PATTERN
 *    Same latch-based sliding-window pattern as NetworkCableTaskManager.
 *    Each task is checked every Update; once the condition is true the
 *    task is latched and never reverts. Three tasks visible at a time.
 * ================================================================
 */

using TMPro;
using UnityEngine;

public class ServerSetupTaskManager : MonoBehaviour
{
    public static ServerSetupTaskManager Instance { get; private set; }

    private const int TASK_COUNT = 65;

    [Header("Task UI")]
    [SerializeField] private Transform    taskParent;
    [SerializeField] private Transform    finishedParent;
    [SerializeField] private GameObject[] taskObjects = new GameObject[65];
    [SerializeField] private GameObject   allTasksCompletedText;

    [Header("Hardware")]
    [Tooltip("The hardware holder for the Server PC — exposes IsDeployed.")]
    [SerializeField] private NetworkHardwareHolder serverHolder;

    // ── Internal state ────────────────────────────────────────────────────────

    private bool[] _latched      = new bool[TASK_COUNT];
    private int    _currentTask  = 0;
    private bool   _allDone      = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
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
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return false;

        return index switch
        {
            // Pre-Configuration
             0 => serverHolder != null && serverHolder.hardwarePrefab != null && serverHolder.hardwarePrefab.activeSelf,
             1 => ServerVirtualOSManager.Instance?.IsOpen == true || state.SettingsOpened,
             2 => state.SettingsOpened,
             3 => state.SettingsSystemOpened,
             4 => state.SettingsAboutOpened,
             5 => state.PCRenamed,
             6 => state.SettingsAccountsOpened,
             7 => state.PasswordPanelOpened,
             8 => state.PasswordSet,
             9 => state.NSCOpened,
            10 => state.AdapterSettingsVisited,
            11 => state.IPv4PanelOpened,
            12 => state.UseStaticIP,
            13 => state.UseStaticIP && !string.IsNullOrEmpty(state.IPOctets[3]),
            14 => state.UseStaticIP && !string.IsNullOrEmpty(state.GatewayOctets[3]),
            15 => state.UseStaticIP && !string.IsNullOrEmpty(state.PreferredDNSOctets[3]),
            16 => state.IPConfigured,

            // Role Installation
            17 => state.ServerManagerOpened,
            18 => state.AddRolesWizardOpenedForADDS,
            19 => state.ADDSInstalled,
            20 => state.PromotionNotificationClicked,
            21 => !string.IsNullOrEmpty(state.DomainName),
            22 => state.DNSCheckedInDCPromo,
            23 => !string.IsNullOrEmpty(state.ForestFunctionalLevel),
            24 => state.DCPromoCompleted,
            25 => state.AddRolesWizardOpenedForDHCP,
            26 => state.DHCPInstalled,
            27 => state.AddRolesWizardOpenedForFile,
            28 => state.FileServicesInstalled,
            29 => state.AddRolesWizardOpenedForPrint,
            30 => state.PrintServicesInstalled,
            31 => state.AddRolesWizardOpenedForRD,
            32 => state.RDServicesInstalled,

            // Active Directory
            33 => state.ADUCOpened,
            34 => state.OrganizationalUnits.Count >= 1,
            35 => state.OrganizationalUnits.Count >= 2,
            36 => state.RegularUserCount >= 1,
            37 => state.RegularUserCount >= 2,
            38 => state.AdminAccountCreated,

            // DHCP
            39 => state.DHCPConsoleOpened,
            40 => state.DHCPScopeNameSet,
            41 => state.DHCPScopeStartSet,
            42 => state.DHCPScopeEndSet,
            43 => state.DHCPScopeActive,
            44 => state.DHCPAuthorized,

            // File Services
            45 => state.FSRMOpened,
            46 => state.SharedFolderCreated,
            47 => state.FolderPermissionsSet,
            48 => state.FileGroupCreated,
            49 => state.FileScreenApplied,

            // Group Policy
            50 => state.GPMOpened,
            51 => state.GPOCreatedForAllOUs,
            52 => state.AllGPOEditorsOpened,
            53 => state.AllDesktopRedirectsSet,
            54 => state.AllDocumentsRedirectsSet,
            55 => state.AllGPOsEnforced,
            56 => state.SecurityFilteringConfigured,

            // Print Services
            57 => state.PrintMgmtOpened,
            58 => state.PrinterDriverAdded,
            59 => state.PrinterShared,

            // Remote Desktop
            60 => CheckClientDomainUserLoggedIn(),
            61 => state.RDConnectionPanelOpened,
            62 => state.RDComputerNameEntered,
            63 => state.RDCredentialsEntered,
            64 => state.RDSessionOpened,

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
        ActivityLogManager.Log($"Task completed: {GetTaskLabel(index)}", ActivityLogManager.EntryType.Action);

        if (taskObjects[index] != null && finishedParent != null)
            taskObjects[index].transform.SetParent(finishedParent, false);

        AdvanceIfPossible();
        RefreshWindow();
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
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GetTaskLabel(int index)
    {
        return index switch
        {
             0 => "Deploy Server PC to workspace",
             1 => "Open Server PC Virtual OS",
             2 => "Open Settings app",
             3 => "Navigate to System in Settings",
             4 => "Navigate to About",
             5 => "Rename this PC",
             6 => "Navigate to Accounts in Settings",
             7 => "Open Change Password panel",
             8 => "Set Administrator password",
             9 => "Open Network and Sharing Center",
            10 => "Open adapter settings",
            11 => "Open IPv4 Properties",
            12 => "Select static IP mode",
            13 => "Enter IP Address",
            14 => "Enter Default Gateway",
            15 => "Enter Preferred DNS",
            16 => "Apply IP configuration",
            17 => "Open Server Manager",
            18 => "Start Add Roles wizard for AD DS",
            19 => "Install Active Directory Domain Services",
            20 => "Click promotion notification",
            21 => "Enter domain name",
            22 => "Enable DNS Server in dcpromo",
            23 => "Select Forest Functional Level",
            24 => "Complete domain promotion (reboot)",
            25 => "Start Add Roles wizard for DHCP",
            26 => "Install DHCP Server",
            27 => "Start Add Roles wizard for File Services",
            28 => "Install File Services",
            29 => "Start Add Roles wizard for Print Services",
            30 => "Install Print and Document Services",
            31 => "Start Add Roles wizard for Remote Desktop",
            32 => "Install Remote Desktop Services",
            33 => "Open Active Directory Users and Computers",
            34 => "Create first Organizational Unit",
            35 => "Create second Organizational Unit",
            36 => "Create user account in first OU",
            37 => "Create user account in second OU",
            38 => "Create Administrator account",
            39 => "Open DHCP Console",
            40 => "Create DHCP Scope with name",
            41 => "Set DHCP Scope start IP",
            42 => "Set DHCP Scope end IP",
            43 => "Activate DHCP Scope",
            44 => "Authorize DHCP Server",
            45 => "Open File Server Resource Manager",
            46 => "Create shared folder",
            47 => "Set shared folder permissions",
            48 => "Create File Group with blocked extensions",
            49 => "Apply File Screen to shared folder",
            50 => "Open Group Policy Management",
            51 => "Create a GPO linked to each OU",
            52 => "Open GPO editor for all GPOs",
            53 => "Configure Desktop Folder Redirection in all GPOs",
            54 => "Configure Documents Folder Redirection in all GPOs",
            55 => "Set Enforced on all GPOs",
            56 => "Add domain users to GPO Security Filtering",
            57 => "Open Print Management",
            58 => "Add printer driver",
            59 => "Share the printer",
            60 => "Switch to Client PC and log in as domain user",
            61 => "Open Remote Desktop Connection app on Client PC",
            62 => "Enter server computer name and click Connect",
            63 => "Enter domain credentials in Windows Security",
            64 => "RD session established — Server Manager accessible from Client PC",
            _  => $"Task {index}"
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Task [60]: true when the player is on the Client PC and logged in as a domain user
    // (a user that exists in the server's UserAccounts list).
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

    public void NotifyStateChanged() { /* Update() handles polling; this is a hook for future use */ }
}
