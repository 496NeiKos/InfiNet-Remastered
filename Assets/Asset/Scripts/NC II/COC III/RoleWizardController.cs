/*
 * ================================================================
 *  UNITY SETUP GUIDE — RoleWizardController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to the "Add Roles Wizard Panel" GameObject (starts INACTIVE).
 *
 *  HIERARCHY — 7 step panels + popup + left tracker
 *
 *    Add Roles Wizard Panel              ← this script here
 *      ├── Left Tracker Panel
 *      │     ├── TrackerEntry_0          → trackerEntries[0] (always active)
 *      │     │     └── TMP "Before You Begin"   → trackerLabels[0]
 *      │     ├── TrackerEntry_1          → trackerEntries[1]
 *      │     │     └── TMP "Installation Type"  → trackerLabels[1]
 *      │     ├── TrackerEntry_2          → trackerEntries[2]
 *      │     │     └── TMP "Server Selection"   → trackerLabels[2]
 *      │     ├── TrackerEntry_3          → trackerEntries[3]
 *      │     │     └── TMP "Server Roles"       → trackerLabels[3]
 *      │     ├── TrackerEntry_4          → trackerEntries[4] (STARTS INACTIVE — shown only after role confirmed)
 *      │     │     └── TMP "[Role Name]"        → trackerLabels[4] (text set at runtime)
 *      │     ├── TrackerEntry_5          → trackerEntries[5]
 *      │     │     └── TMP "Confirmation"       → trackerLabels[5]
 *      │     └── TrackerEntry_6          → trackerEntries[6]
 *      │           └── TMP "Results"            → trackerLabels[6]
 *      │
 *      ├── Step 0 — Before You Begin     → steps[0]
 *      │
 *      ├── Step 1 — Installation Type    → steps[1]
 *      │     ├── OverviewTMP             (static description text, set in Inspector)
 *      │     ├── RoleBasedToggle         → installTypeRoleBasedToggle
 *      │     ├── RDSToggle               → installTypeRDSToggle
 *      │     └── RDSBlockedNotice        → rdsBlockedNotice (STARTS INACTIVE)
 *      │           └── TMP "Remote Desktop Services uses a dedicated wizard."
 *      │
 *      ├── Step 2 — Server Selection     → steps[2]
 *      │     ├── ServerPoolToggle        → serverPoolToggle
 *      │     ├── VHDToggle               → vhdToggle
 *      │     ├── ServerPoolListContainer → serverPoolListContainer
 *      │     │     ├── HeaderRow         (static TMPs: "Name", "IP Address", "OS")
 *      │     │     └── ServerPoolParent  → serverPoolParent (rows spawned here at runtime)
 *      │     ├── ServerCountTMP          → serverCountTMP   "X Computer(s) found"
 *      │     └── ServerDescTMP          → serverSelectionDescTMP (static, set in Inspector)
 *      │
 *      ├── Step 3 — Server Roles         → steps[3]
 *      │     └── RoleList (scroll)
 *      │           └── RoleToggleParent  → roleToggleParent (rows spawned at runtime)
 *      │
 *      ├── Step 4 — Role Detail          → steps[4]
 *      │     └── RoleDetailTMP           → roleDetailTMP (set at runtime)
 *      │
 *      ├── Step 5 — Confirmation         → steps[5]
 *      │     └── ConfirmLabel            → confirmLabelTMP
 *      │
 *      ├── Step 6 — Results              → steps[6]
 *      │     └── ResultLabel             → resultLabelTMP
 *      │
 *      ├── Add Features Popup            → addFeaturesPopup (STARTS INACTIVE)
 *      │     ├── FeaturesHeaderTMP       → featuresHeaderTMP
 *      │     ├── FeaturesListTMP         → featuresListTMP
 *      │     ├── IncludeManagementToggle → includeManagementToggle
 *      │     ├── AddFeatureBtn           → addFeatureBtn
 *      │     └── CancelFeatureBtn        → cancelFeaturePopupBtn
 *      │
 *      ├── PrevBtn                       → prevBtn   (shared navigation)
 *      ├── NextBtn                       → nextBtn
 *      ├── InstallBtn                    → installBtn
 *      ├── CloseWizardBtn                → closeWizardBtn (step 6 only)
 *      └── CancelBtn                     → cancelBtn (always visible steps 0–5)
 *
 *  SERVER POOL ROW PREFAB
 *    serverPoolRowPrefab must have exactly 3 TMP_Text children in order:
 *    [0] Name column, [1] IP Address column, [2] Operating System column.
 *
 *  ROLE OPTIONS (populate in Inspector)
 *    roleOptions[] — array of RoleOption (DisplayName + Role enum + Description).
 *    Only roles NOT yet installed are shown in the list.
 *    Feature text, role overview, and things-to-note are code-defined (no Inspector entry needed).
 *
 *  TRACKER BEHAVIOR
 *    trackerEntries[4] starts INACTIVE in the scene. It becomes active only after the
 *    player confirms a role via the Add Features popup. Its TMP text is set at runtime
 *    to the selected role name.
 *
 *  STEP FLOW
 *    Step 0 → Step 1 → Step 2 → Step 3 → Step 4 → Step 5 → Step 6
 *    (Step 1: selecting RDS toggle disables Next and shows rdsBlockedNotice —
 *     RDS installation uses a separate dedicated wizard to be wired later.)
 * ================================================================
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ServerRole { ADDS, DNS, DHCP, FileServices, PrintServices, RemoteDesktopServices }

[System.Serializable]
public class RoleOption
{
    public string     DisplayName;
    [TextArea(2, 4)]
    public string     Description;
    public ServerRole Role;
}

public class RoleWizardController : MonoBehaviour
{
    public static RoleWizardController Instance { get; private set; }

    [Header("Steps (0–6)")]
    [SerializeField] private GameObject[] steps = new GameObject[7];

    [Header("Step 1 — Installation Type")]
    [SerializeField] private Toggle     installTypeRoleBasedToggle;
    [SerializeField] private Toggle     installTypeRDSToggle;
    [SerializeField] private GameObject rdsBlockedNotice;

    [Header("Step 2 — Server Selection")]
    [SerializeField] private Toggle     serverPoolToggle;
    [SerializeField] private Toggle     vhdToggle;
    [SerializeField] private GameObject serverPoolListContainer;
    [SerializeField] private Transform  serverPoolParent;
    [SerializeField] private GameObject serverPoolRowPrefab;
    [SerializeField] private TMP_Text   serverCountTMP;
    [SerializeField] private TMP_Text   serverSelectionDescTMP;

    [Header("Step 3 — Server Roles")]
    [SerializeField] private Transform  roleToggleParent;
    [SerializeField] private GameObject roleTogglePrefab;

    [Header("Add Features Popup")]
    [SerializeField] private GameObject addFeaturesPopup;
    [SerializeField] private TMP_Text   featuresHeaderTMP;
    [SerializeField] private TMP_Text   featuresListTMP;
    [SerializeField] private Toggle     includeManagementToggle;
    [SerializeField] private Button     addFeatureBtn;
    [SerializeField] private Button     cancelFeaturePopupBtn;

    [Header("Step 4 — Role Detail")]
    [SerializeField] private TMP_Text roleDetailTMP;

    [Header("Step 5 — Confirmation")]
    [SerializeField] private TMP_Text confirmLabelTMP;

    [Header("Step 6 — Results")]
    [SerializeField] private TMP_Text resultLabelTMP;

    [Header("Buttons")]
    [SerializeField] private Button prevBtn;
    [SerializeField] private Button nextBtn;
    [SerializeField] private Button installBtn;
    [SerializeField] private Button closeWizardBtn;
    [SerializeField] private Button cancelBtn;

    [Header("Step Tracker")]
    [SerializeField] private GameObject[] trackerEntries = new GameObject[7];
    [SerializeField] private TMP_Text[]   trackerLabels  = new TMP_Text[7];
    [SerializeField] private Color trackerActiveColor   = Color.white;
    [SerializeField] private Color trackerInactiveColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Header("Role Options (configure in Inspector)")]
    [SerializeField] private RoleOption[] roleOptions;

    // ── State ─────────────────────────────────────────────────────────────────

    private int        _currentStep      = 0;
    private ServerRole _selectedRole;
    private bool       _roleSelected     = false;
    private string     _selectedRoleName = "";
    private RoleOption _pendingOption    = null;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        prevBtn?.onClick.AddListener(GoBack);
        nextBtn?.onClick.AddListener(GoNext);
        installBtn?.onClick.AddListener(Install);
        closeWizardBtn?.onClick.AddListener(CloseWizard);
        cancelBtn?.onClick.AddListener(CloseWizard);
        addFeatureBtn?.onClick.AddListener(ConfirmFeatureAdd);
        cancelFeaturePopupBtn?.onClick.AddListener(CancelFeatureAdd);

        installTypeRoleBasedToggle?.onValueChanged.AddListener(OnInstallTypeRoleBasedChanged);
        installTypeRDSToggle?.onValueChanged.AddListener(OnInstallTypeRDSChanged);
        serverPoolToggle?.onValueChanged.AddListener(OnServerPoolToggleChanged);
        vhdToggle?.onValueChanged.AddListener(OnVHDToggleChanged);

        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        _currentStep      = 0;
        _roleSelected     = false;
        _selectedRoleName = "";
        _pendingOption    = null;

        installTypeRoleBasedToggle?.SetIsOnWithoutNotify(true);
        installTypeRDSToggle?.SetIsOnWithoutNotify(false);
        serverPoolToggle?.SetIsOnWithoutNotify(true);
        vhdToggle?.SetIsOnWithoutNotify(false);

        rdsBlockedNotice?.SetActive(false);
        addFeaturesPopup?.SetActive(false);

        if (trackerEntries.Length > 4) trackerEntries[4]?.SetActive(false);

        gameObject.SetActive(true);
        ShowStep(0);
        ActivityLogManager.Log("Opened Add Roles and Features Wizard", ActivityLogManager.EntryType.Action);
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private void GoNext()
    {
        switch (_currentStep)
        {
            case 0:
                ShowStep(1);
                break;

            case 1:
                if (installTypeRDSToggle != null && installTypeRDSToggle.isOn)
                {
                    rdsBlockedNotice?.SetActive(true);
                    return;
                }
                ShowStep(2);
                break;

            case 2:
                PopulateRoleList();
                ShowStep(3);
                break;

            case 3:
                if (!_roleSelected)
                {
                    Debug.LogWarning("[RoleWizardController] No role confirmed yet.");
                    return;
                }
                RefreshRoleDetail();
                ShowStep(4);
                break;

            case 4:
                if (confirmLabelTMP != null)
                    confirmLabelTMP.text = $"The following role will be installed:\n\n{_selectedRoleName}";
                ShowStep(5);
                break;
        }
    }

    private void GoBack()
    {
        if (_currentStep <= 0) return;

        if (_currentStep == 4)
        {
            _roleSelected = false;
            if (trackerEntries.Length > 4) trackerEntries[4]?.SetActive(false);
        }

        ShowStep(_currentStep - 1);
    }

    private void Install()
    {
        ApplyRoleToState(_selectedRole);

        // Notify Server Manager so tree panel and dashboard tiles update immediately
        ServerManagerController.Instance?.RefreshTreeRoleButtons();
        ServerManagerController.Instance?.RefreshDashboard();

        if (resultLabelTMP != null)
            resultLabelTMP.text = $"{_selectedRoleName}\nInstallation succeeded.";
        ShowStep(6);
        ActivityLogManager.Log($"Role installed: {_selectedRoleName}", ActivityLogManager.EntryType.Action);
    }

    private void CloseWizard()
    {
        addFeaturesPopup?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Step 1: Installation Type ─────────────────────────────────────────────

    private void OnInstallTypeRoleBasedChanged(bool on)
    {
        if (on)
            installTypeRDSToggle?.SetIsOnWithoutNotify(false);
        else if (installTypeRDSToggle != null && !installTypeRDSToggle.isOn)
            installTypeRoleBasedToggle?.SetIsOnWithoutNotify(true);

        rdsBlockedNotice?.SetActive(false);
    }

    private void OnInstallTypeRDSChanged(bool on)
    {
        if (on)
            installTypeRoleBasedToggle?.SetIsOnWithoutNotify(false);
        else if (installTypeRoleBasedToggle != null && !installTypeRoleBasedToggle.isOn)
            installTypeRDSToggle?.SetIsOnWithoutNotify(true);

        rdsBlockedNotice?.SetActive(false);
    }

    // ── Step 2: Server Selection ──────────────────────────────────────────────

    private void OnServerPoolToggleChanged(bool on)
    {
        if (on) vhdToggle?.SetIsOnWithoutNotify(false);
        else if (vhdToggle != null && !vhdToggle.isOn)
            serverPoolToggle?.SetIsOnWithoutNotify(true);

        RefreshServerPoolVisibility();
    }

    private void OnVHDToggleChanged(bool on)
    {
        if (on) serverPoolToggle?.SetIsOnWithoutNotify(false);
        else if (serverPoolToggle != null && !serverPoolToggle.isOn)
            vhdToggle?.SetIsOnWithoutNotify(true);

        RefreshServerPoolVisibility();
    }

    private void RefreshServerPoolVisibility()
    {
        bool showPool = serverPoolToggle != null && serverPoolToggle.isOn;
        serverPoolListContainer?.SetActive(showPool);
    }

    private void RefreshServerPool()
    {
        if (serverPoolParent == null || serverPoolRowPrefab == null) return;

        foreach (Transform child in serverPoolParent)
            Destroy(child.gameObject);

        var state = ServerVirtualOSManager.Instance?.ServerState;

        string name = !string.IsNullOrEmpty(state?.ComputerName) ? state.ComputerName : "SERVER";

        string ip = "—";
        if (state?.IPOctets != null && Array.Exists(state.IPOctets, o => !string.IsNullOrEmpty(o)))
            ip = string.Join(".", state.IPOctets);

        const string os = "Microsoft Windows Server 2019 Standard";

        var row  = Instantiate(serverPoolRowPrefab, serverPoolParent);
        var tmps = row.GetComponentsInChildren<TMP_Text>();
        if (tmps.Length >= 3)
        {
            tmps[0].text = name;
            tmps[1].text = ip;
            tmps[2].text = os;
        }

        if (serverCountTMP != null)
            serverCountTMP.text = $"{serverPoolParent.childCount} Computer(s) found";
    }

    // ── Step 3: Role List ─────────────────────────────────────────────────────

    private void PopulateRoleList()
    {
        foreach (Transform child in roleToggleParent)
            Destroy(child.gameObject);

        var state = ServerVirtualOSManager.Instance?.ServerState;
        foreach (var option in roleOptions)
        {
            if (state != null && IsRoleInstalled(state, option.Role)) continue;

            var row    = Instantiate(roleTogglePrefab, roleToggleParent);
            var toggle = row.GetComponentInChildren<Toggle>();
            var label  = row.GetComponentInChildren<TMP_Text>();
            if (label  != null) label.text = option.DisplayName;
            if (toggle != null) toggle.SetIsOnWithoutNotify(false);

            var captured = option;
            toggle?.onValueChanged.AddListener(on =>
            {
                if (!on) return;
                foreach (Transform t in roleToggleParent)
                {
                    var other = t.GetComponentInChildren<Toggle>();
                    if (other != null && other != toggle)
                        other.SetIsOnWithoutNotify(false);
                }
                _roleSelected  = false;
                _pendingOption = captured;
                OpenFeaturesPopup(captured);
            });
        }
    }

    // ── Add Features Popup ────────────────────────────────────────────────────

    private void OpenFeaturesPopup(RoleOption option)
    {
        if (addFeaturesPopup == null) return;
        if (featuresHeaderTMP != null)
            featuresHeaderTMP.text = $"Add features that are required for {option.DisplayName}?";
        if (featuresListTMP != null)
            featuresListTMP.text = GetFeaturesText(option.Role);
        includeManagementToggle?.SetIsOnWithoutNotify(false);
        addFeaturesPopup.SetActive(true);
    }

    private void ConfirmFeatureAdd()
    {
        if (_pendingOption == null) return;

        _selectedRole     = _pendingOption.Role;
        _selectedRoleName = _pendingOption.DisplayName;
        _roleSelected     = true;

        if (trackerEntries.Length > 4) trackerEntries[4]?.SetActive(true);
        if (trackerLabels.Length  > 4 && trackerLabels[4] != null)
            trackerLabels[4].text = _selectedRoleName;

        SetWizardOpenedLatch(_pendingOption.Role);
        addFeaturesPopup?.SetActive(false);
        _pendingOption = null;

        RefreshTracker(_currentStep);
    }

    private void CancelFeatureAdd()
    {
        foreach (Transform t in roleToggleParent)
            t.GetComponentInChildren<Toggle>()?.SetIsOnWithoutNotify(false);

        _roleSelected  = false;
        _pendingOption = null;
        addFeaturesPopup?.SetActive(false);
    }

    // ── Step 4: Role Detail ───────────────────────────────────────────────────

    private void RefreshRoleDetail()
    {
        if (roleDetailTMP == null || !_roleSelected) return;
        roleDetailTMP.text =
            $"{GetRoleOverview(_selectedRole)}\n\nThings to Note:\n{GetThingsToNote(_selectedRole)}";
    }

    // ── Step Display ──────────────────────────────────────────────────────────

    private void ShowStep(int index)
    {
        for (int i = 0; i < steps.Length; i++)
            steps[i]?.SetActive(i == index);
        _currentStep = index;

        bool isLast = index == 6;

        prevBtn?.gameObject.SetActive(index > 0 && !isLast);
        nextBtn?.gameObject.SetActive(index < 5);
        installBtn?.gameObject.SetActive(index == 5);
        closeWizardBtn?.gameObject.SetActive(isLast);
        cancelBtn?.gameObject.SetActive(!isLast);

        if (index == 2)
        {
            RefreshServerPool();
            RefreshServerPoolVisibility();
        }

        RefreshTracker(index);
    }

    private void RefreshTracker(int currentStep)
    {
        for (int i = 0; i < trackerLabels.Length; i++)
        {
            if (trackerLabels[i] == null) continue;
            bool active = i == currentStep;
            trackerLabels[i].color     = active ? trackerActiveColor : trackerInactiveColor;
            trackerLabels[i].fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    // ── Role Content (code-defined) ───────────────────────────────────────────

    private static string GetFeaturesText(ServerRole role) => role switch
    {
        ServerRole.DNS =>
            "DNS Server\n" +
            "  Remote Server Administration Tools\n" +
            "    Role Administration Tools\n" +
            "      DNS Server Tools",

        ServerRole.ADDS =>
            "Active Directory Domain Services\n" +
            "  Group Policy Management\n" +
            "  Remote Server Administration Tools\n" +
            "    Role Administration Tools\n" +
            "      AD DS and AD LDS Tools\n" +
            "        Active Directory Module for Windows PowerShell\n" +
            "        AD DS Tools\n" +
            "          Active Directory Administrative Center\n" +
            "          Active Directory Sites and Services\n" +
            "          Active Directory Users and Computers\n" +
            "          ADSI Edit",

        ServerRole.DHCP =>
            "DHCP Server\n" +
            "  Remote Server Administration Tools\n" +
            "    Role Administration Tools\n" +
            "      DHCP Server Tools",

        ServerRole.FileServices =>
            "File and Storage Services\n" +
            "  File Server\n" +
            "  File Server Resource Manager\n" +
            "  Storage Services",

        ServerRole.PrintServices =>
            "Print and Document Services\n" +
            "  Print Server\n" +
            "  Internet Printing Client\n" +
            "  LPD Print Service",

        ServerRole.RemoteDesktopServices =>
            "Remote Desktop Services\n" +
            "  Remote Desktop Session Host\n" +
            "  Remote Desktop Licensing\n" +
            "  Remote Desktop Web Access\n" +
            "  Remote Server Administration Tools\n" +
            "    Role Administration Tools\n" +
            "      Remote Desktop Services Tools",

        _ => ""
    };

    private static string GetRoleOverview(ServerRole role) => role switch
    {
        ServerRole.DNS =>
            "Domain Name System (DNS) Server translates human-readable domain names (such as " +
            "test.edu.ph) into IP addresses that computers use to identify each other on a network. " +
            "As a critical network infrastructure service, DNS is required for Active Directory Domain " +
            "Services and enables clients to locate domain controllers, services, and other network " +
            "resources by name.",

        ServerRole.ADDS =>
            "Active Directory Domain Services (AD DS) stores information about network objects — " +
            "users, computers, and other devices — and makes this information available to network " +
            "administrators and users. AD DS uses domain controllers to give network users access " +
            "to permitted resources anywhere on the network through a single logon process.",

        ServerRole.DHCP =>
            "The DHCP Server role enables this server to automatically assign IP addresses and " +
            "related network configuration settings — such as subnet mask, default gateway, and DNS " +
            "server addresses — to client computers on your network, eliminating the need to " +
            "manually configure each device.",

        ServerRole.FileServices =>
            "File and Storage Services provides technologies that help you set up and manage file " +
            "servers and storage. File servers provide a central location on your network where you " +
            "can store and share files with users. Shared folders allow users across the network " +
            "to access and collaborate on files from a single managed location.",

        ServerRole.PrintServices =>
            "Print and Document Services enables you to centralize print server and network printer " +
            "management tasks. Using Print Management, you can install and manage printers across " +
            "your network, distribute printer drivers to client computers, and monitor print queues " +
            "remotely from a single console.",

        ServerRole.RemoteDesktopServices =>
            "Remote Desktop Services (RDS) enables users to connect to virtual desktops, " +
            "RemoteApp programs, and session-based desktops over the network. RDS provides " +
            "secure, centralized remote desktop access for client computers running Windows or " +
            "other supported operating systems without requiring applications to be installed locally.",

        _ => ""
    };

    private static string GetThingsToNote(ServerRole role) => role switch
    {
        ServerRole.DNS =>
            "• DNS Server is automatically installed as a dependency when you install and promote " +
            "Active Directory Domain Services — it is not required to install it separately in that flow.\n" +
            "• After installation, open DNS Manager to create Forward and Reverse Lookup Zones.\n" +
            "• A Forward Lookup Zone resolves domain names (e.g., test.edu.ph) to IP addresses.\n" +
            "• A Reverse Lookup Zone resolves IP addresses back to domain names (PTR records).\n" +
            "• Ensure DNS is configured before promoting this server to a Domain Controller.",

        ServerRole.ADDS =>
            "• After installation, you must promote this server to a Domain Controller using the " +
            "DCPromo wizard before Active Directory becomes functional.\n" +
            "• DNS Server is required and will be installed alongside AD DS automatically.\n" +
            "• Ensure the server has a static IP address configured before promoting.\n" +
            "• The Forest Functional Level chosen during promotion determines which AD DS features " +
            "are available across the domain.",

        ServerRole.DHCP =>
            "• A static IP address must be configured on this server before installing DHCP.\n" +
            "• After installation, you must create and activate a DHCP scope to begin assigning " +
            "addresses to clients.\n" +
            "• DHCP scopes define the range of IP addresses available for dynamic assignment.\n" +
            "• If a domain controller is present, you must authorize the DHCP server in Active " +
            "Directory before it can serve leases.",

        ServerRole.FileServices =>
            "• After installation, use File Server Resource Manager (FSRM) to manage shared " +
            "folders, quotas, and file screening policies.\n" +
            "• Both share permissions and NTFS permissions must be configured correctly for " +
            "secure network access to shared folders.\n" +
            "• File screening allows you to block specific file types from being saved on the server.\n" +
            "• Folder redirection can be configured through Group Policy to transparently redirect " +
            "user profile folders (e.g., Documents) to the file server.",

        ServerRole.PrintServices =>
            "• After installation, open Print Management to add printer drivers and configure " +
            "shared printers on the network.\n" +
            "• Shared printers must have a share name assigned before clients can connect to them.\n" +
            "• Clients may need the appropriate printer driver installed, or it can be distributed " +
            "automatically by the print server.\n" +
            "• Internet Printing allows clients to connect to and print via shared printers over HTTP.",

        ServerRole.RemoteDesktopServices =>
            "• Remote Desktop Services uses a dedicated installation wizard with a different " +
            "configuration flow from standard role installation.\n" +
            "• A Remote Desktop Session Host (RDSH) must be configured to serve session-based " +
            "remote desktop connections.\n" +
            "• Remote Desktop Licensing is required for client access licenses (CALs) in " +
            "any production or multi-user deployment.\n" +
            "• Ensure firewall rules permit RDP traffic on port 3389 for remote connectivity.",

        _ => ""
    };

    // ── State Helpers ─────────────────────────────────────────────────────────

    private static bool IsRoleInstalled(ServerDeviceState s, ServerRole role) => role switch
    {
        ServerRole.DNS                   => s.DNSInstalled,
        ServerRole.ADDS                  => s.ADDSInstalled,
        ServerRole.DHCP                  => s.DHCPInstalled,
        ServerRole.FileServices          => s.FileServicesInstalled,
        ServerRole.PrintServices         => s.PrintServicesInstalled,
        ServerRole.RemoteDesktopServices => s.RDServicesInstalled,
        _                                => false
    };

    private static void ApplyRoleToState(ServerRole role)
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;
        switch (role)
        {
            case ServerRole.DNS:                   state.DNSInstalled           = true; break;
            case ServerRole.ADDS:                  state.ADDSInstalled          = true; break;
            case ServerRole.DHCP:                  state.DHCPInstalled          = true; break;
            case ServerRole.FileServices:          state.FileServicesInstalled  = true; break;
            case ServerRole.PrintServices:         state.PrintServicesInstalled = true; break;
            case ServerRole.RemoteDesktopServices: state.RDServicesInstalled    = true; break;
        }
    }

    private static void SetWizardOpenedLatch(ServerRole role)
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;
        switch (role)
        {
            case ServerRole.DNS:                   state.AddRolesWizardOpenedForDNS   = true; break;
            case ServerRole.ADDS:                  state.AddRolesWizardOpenedForADDS  = true; break;
            case ServerRole.DHCP:                  state.AddRolesWizardOpenedForDHCP  = true; break;
            case ServerRole.FileServices:          state.AddRolesWizardOpenedForFile  = true; break;
            case ServerRole.PrintServices:         state.AddRolesWizardOpenedForPrint = true; break;
            case ServerRole.RemoteDesktopServices: state.AddRolesWizardOpenedForRD    = true; break;
        }
    }
}
