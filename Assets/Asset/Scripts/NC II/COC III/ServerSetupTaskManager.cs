/*
 * ================================================================
 *  UNITY SETUP GUIDE — ServerSetupTaskManager (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to an always-active "Managers" GameObject in the COC III scene.
 *
 *  TASK LIST (63 tasks, 0–62, one long linear flow)
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
 *  [44] Disable DHCPv6 stateless mode
 *  ── File Services ───────────────────────────────────────────────
 *  [45] Open Tools → File Server Resource Manager
 *  [46] Create a Shared Folder
 *  [47] Set folder permissions (Everyone — Read/Write)
 *  [48] Create a File Group with blocked file extensions
 *  [49] Apply a File Screen to the shared folder
 *  ── Group Policy ────────────────────────────────────────────────
 *  [50] Open Tools → Group Policy Management
 *  [51] Create a GPO linked to the first OU
 *  [52] Open the GPO editor for the first OU and configure Folder Redirection
 *  [53] Create a GPO linked to the second OU
 *  [54] Open the GPO editor for the second OU and configure Folder Redirection
 *  ── Print Services ──────────────────────────────────────────────
 *  [55] Open Tools → Print Management
 *  [56] Add a Printer Driver using the Add Driver Wizard
 *  [57] Share the printer and set a share name
 *  ── Client Verification ─────────────────────────────────────────
 *  [58] Open Remote Desktop Connection and connect to the client machine
 *  [59] In the client CMD, run ipconfig — verify the client received a DHCP IP
 *  [60] In the client File Explorer, verify Documents is redirected to the server
 *  [61] In the client Devices and Printers, verify the shared printer is accessible
 *  [62] In the client CMD, ping the server IP — confirm connectivity
 *
 *  INSPECTOR SETUP
 *    taskParent           → VerticalLayoutGroup parent for active task rows
 *    finishedParent       → Off-screen parent for completed task GameObjects
 *    taskObjects[0..62]   → 63 task row GameObjects (TMP_Text labels)
 *    sectionHeaders[]     → Section divider GameObjects (non-interactive labels)
 *                           Order: Pre-Configuration, Role Installation,
 *                           Active Directory, DHCP, File Services,
 *                           Group Policy, Print Services, Client Verification
 *    serverHolder         → NetworkHardwareHolder (or equivalent) on the Server PC icon
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

    private const int TASK_COUNT = 63;

    [Header("Task UI")]
    [SerializeField] private Transform   taskParent;
    [SerializeField] private Transform   finishedParent;
    [SerializeField] private GameObject[] taskObjects = new GameObject[TASK_COUNT];
    [SerializeField] private GameObject  allTasksCompletedText;

    [Header("Hardware")]
    [Tooltip("The hardware holder for the Server PC — exposes IsDeployed.")]
    [SerializeField] private NetworkHardwareHolder serverHolder;

    // ── Internal state ────────────────────────────────────────────────────────

    private bool[]  _latched  = new bool[TASK_COUNT];
    private int     _currentTask = 0;
    private bool    _allDone  = false;

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
            44 => state.DHCPv6Disabled,

            // File Services
            45 => state.FSRMOpened,
            46 => state.SharedFolderCreated,
            47 => state.FolderPermissionsSet,
            48 => state.FileGroupCreated,
            49 => state.FileScreenApplied,

            // Group Policy
            50 => state.GPMOpened,
            51 => state.GPOCount >= 1,
            52 => state.GroupPolicies.Count >= 1 && state.GroupPolicies[0].RedirectSet,
            53 => state.GPOCount >= 2,
            54 => state.GroupPolicies.Count >= 2 && state.GroupPolicies[1].RedirectSet,

            // Print Services
            55 => state.PrintMgmtOpened,
            56 => state.PrinterDriverAdded,
            57 => state.PrinterShared,

            // Client Verification
            58 => state.ClientConnected,
            59 => state.ClientDHCPVerified,
            60 => state.FolderRedirectionVerified,
            61 => state.PrinterVerified,
            62 => state.ConnectivityVerified,

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

        // Move completed task GameObject off-screen
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
        // Show up to 3 upcoming tasks under taskParent
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
            44 => "Disable DHCPv6",
            45 => "Open File Server Resource Manager",
            46 => "Create shared folder",
            47 => "Set shared folder permissions",
            48 => "Create File Group with blocked extensions",
            49 => "Apply File Screen to shared folder",
            50 => "Open Group Policy Management",
            51 => "Create GPO for first OU",
            52 => "Configure Folder Redirection for first OU",
            53 => "Create GPO for second OU",
            54 => "Configure Folder Redirection for second OU",
            55 => "Open Print Management",
            56 => "Add printer driver",
            57 => "Share the printer",
            58 => "Connect to client via Remote Desktop",
            59 => "Verify client DHCP IP (ipconfig)",
            60 => "Verify Folder Redirection in client File Explorer",
            61 => "Verify shared printer in client Devices and Printers",
            62 => "Verify connectivity (ping server)",
            _  => $"Task {index}"
        };
    }

    // ── Public API (called by controllers to force-check a task) ─────────────

    public void NotifyStateChanged() { /* Update() handles polling; this is a hook for future use */ }
}
