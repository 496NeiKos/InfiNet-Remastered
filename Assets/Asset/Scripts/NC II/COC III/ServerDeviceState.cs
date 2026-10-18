using System.Collections.Generic;

// ── Data classes ────────────────────────────────────────────────────────────

[System.Serializable]
public class OUData
{
    public string Name = "";
}

[System.Serializable]
public class UserData
{
    public string FirstName           = "";
    public string LastName            = "";
    public string Initials            = "";
    public string FullName            = "";   // auto-filled from First + Last, manually editable
    public string Username            = "";
    public string Password            = "";
    public string OUName              = "";   // which OU this user belongs to
    public bool   IsAdmin             = false; // true = added to Domain Admins group
    public bool   MustChangePassword  = false;
    public bool   CannotChangePassword = false;
    public bool   PasswordNeverExpires = true;
    public bool   AccountDisabled     = false;
}

[System.Serializable]
public class SharedFolderData
{
    public string FolderName    = "";
    public string Path          = "";  // local path e.g. C:\Profiles
    public string ShareName     = "";  // the share name
    public string NetworkPath   = "";  // \\SERVER\ShareName
    public bool   PermissionsSet = false;
}


[System.Serializable]
public class GPOData
{
    public string Name                  = "";
    public string LinkedOUName          = "";
    public int    LinkOrder             = 0;
    public string ModifiedDate          = "";
    public bool   Enforced              = false;
    public bool   LinkEnabled           = true;
    public bool   EditorOpened          = false;
    public bool   DesktopRedirectSet    = false;
    public string DesktopRedirectPath   = "";
    public bool   DocumentsRedirectSet  = false;
    public string DocumentsRedirectPath = "";
    public List<string> SecurityFilterUsernames = new List<string>();
}

[System.Serializable]
public class DNSZoneData
{
    public string ZoneName       = "";
    public string ZoneType       = "";  // "Standard Primary" | "Standard Secondary" | "Stub"
    public string LookupType     = "";  // "Forward" | "Reverse"
    public string FileName       = "";
    public bool   StoreInAD      = false;
    public string DynamicUpdates = "";  // "Both" | "DoNotAllow" | "SecureOnly"
}

// ── Main state class ─────────────────────────────────────────────────────────

/// <summary>
/// Holds all configuration state for the COC III server simulation.
/// Extends DeviceOSState so the reused IPv4PropertiesController and
/// NetworkSharingCenterController can read/write IP fields as normal.
/// </summary>
public class ServerDeviceState : DeviceOSState
{
    // ── Pre-configuration ────────────────────────────────────────────────────
    public string ComputerName    = "";
    public string AdminPassword   = "";

    // ── Session state ────────────────────────────────────────────────────────
    public string CurrentLoggedInUser = "";
    public bool   FirstBootDone       = false;

    // Computed shortcuts
    public bool PCRenamed         => !string.IsNullOrEmpty(ComputerName);
    public bool PasswordSet       => !string.IsNullOrEmpty(AdminPassword);

    // ── Navigation latches (set by controllers when player visits a page) ────
    public bool SettingsOpened         = false;
    public bool SettingsSystemOpened   = false;
    public bool SettingsAboutOpened    = false;
    public bool SettingsAccountsOpened = false;
    public bool PasswordPanelOpened    = false;
    public bool SettingsClosed         = false;
    // NSCOpened, IPv4PanelOpened, AdapterSettingsVisited — inherited from DeviceOSState

    // ── Network adapter latches ───────────────────────────────────────────────
    public bool EthernetPropertiesOpened = false;  // right-clicked adapter → Properties
    public bool IPv4Applied              = false;  // pressed OK to apply static IP changes
    public bool NSCClosed                = false;  // closed Network and Sharing Center

    // ── Server Manager ───────────────────────────────────────────────────────
    public bool ServerManagerOpened = false;
    public bool ServerManagerClosed = false;

    // ── Roles installed ──────────────────────────────────────────────────────
    public bool ADDSInstalled              = false;
    public bool IsDomainController         = false;
    public bool DNSInstalled               = false;
    public bool DHCPInstalled              = false;
    public bool PrintServicesInstalled     = false;
    public bool RDServicesInstalled        = false;

    // Wizard opened latches (one per role, prevents double-counting)
    public bool AddRolesWizardOpenedForDNS   = false;
    public bool AddRolesWizardOpenedForADDS  = false;
    public bool AddRolesWizardOpenedForDHCP  = false;
    public bool AddRolesWizardOpenedForPrint = false;
    public bool AddRolesWizardOpenedForRD    = false;

    // ── DNS Manager ──────────────────────────────────────────────────────────
    public bool              DNSManagerOpened = false;
    public bool              DNSManagerClosed = false;
    public bool              DNSRoleSelectedInWizard = false;
    public List<DNSZoneData> DNSZones         = new List<DNSZoneData>();
    public bool DNSZoneCreated        => DNSZones.Count > 0;
    public bool DNSForwardZoneCreated => DNSZones.Exists(z => z.LookupType == "Forward");
    public bool DNSReverseZoneCreated => DNSZones.Exists(z => z.LookupType == "Reverse");

    // DNS zone wizard latches — forward zone
    public bool ForwardZoneWizardOpened       = false;
    public bool ForwardZoneTypeSelected       = false;
    public bool ForwardZoneLookupTypeSelected = false;
    public bool ForwardZoneNameEntered        = false;
    public bool ForwardZoneFileCreated        = false;

    // DNS zone wizard latches — reverse zone
    public bool ReverseZoneWizardOpened       = false;
    public bool ReverseZoneTypeSelected       = false;
    public bool ReverseZoneLookupTypeSelected = false;
    public bool ReverseZoneIPv4Selected       = false;
    public bool ReverseZoneNameEntered        = false;
    public bool ReverseZoneFileCreated        = false;

    // ── Domain / dcpromo ─────────────────────────────────────────────────────
    public bool   PromotionNotificationClicked = false;
    public string DomainName                   = "";
    public string NetBIOSName                  = "";
    public string DCPromoDeploymentType        = "";   // "NewForest" | "AddDC" | "AddDomain"
    public bool   DNSDelegationEnabled         = false;
    public string ForestFunctionalLevel        = "";
    public string DomainFunctionalLevel        = "";
    public string DSRMPassword                 = "";
    public string DCDatabasePath               = @"C:\Windows\NTDS";
    public string DCLogPath                    = @"C:\Windows\NTDS";
    public string DCSysvolPath                 = @"C:\Windows\SYSVOL";
    public bool   DNSOptionsPageVisited        = false;
    public bool   DCPromoInstallStarted        = false;
    public bool   DCPromoCompleted             = false;

    // ── Active Directory ─────────────────────────────────────────────────────
    public bool SignedInAfterPromotion = false;
    public bool ADUCOpened             = false;
    public bool ADUCClosed             = false;
    public bool OUWizardOpened         = false;
    public bool UserWizardOpened       = false;
    public bool UserWizardNameEntered  = false;
    public bool UserWizardAdminToggled = false;
    public List<OUData>   OrganizationalUnits = new List<OUData>();
    public List<UserData> UserAccounts        = new List<UserData>();

    public bool AdminAccountCreated => UserAccounts.Exists(u => u.IsAdmin);
    public int  RegularUserCount    => UserAccounts.FindAll(u => !u.IsAdmin).Count;

    // ── DHCP ─────────────────────────────────────────────────────────────────
    public bool   DHCPConsoleOpened          = false;
    public bool   DHCPConsoleClosed          = false;
    public bool   DHCPScopeWizardStarted     = false;
    public bool   DHCPExclusionsPageVisited  = false;
    public bool   DHCPLeaseDurationPageVisited = false;
    public bool   DHCPWINSPageVisited        = false;
    public string DHCPScopeName              = "";
    public string DHCPScopeDescription       = "";
    public string DHCPScopeStart             = "";
    public string DHCPScopeEnd               = "";
    public string DHCPSubnetLength           = "";
    public string DHCPSubnetMask             = "";
    public string DHCPSubnetDelay            = "0";
    public List<string> DHCPExclusions       = new List<string>();
    public int    DHCPLeaseDays              = 8;
    public int    DHCPLeaseHours             = 0;
    public int    DHCPLeaseMinutes           = 0;
    public bool   DHCPConfigureNow           = true;
    public List<string> DHCPRouterList       = new List<string>();
    public string DHCPParentDomain           = "";
    public List<string> DHCPDNSList          = new List<string>();
    public bool   DHCPScopeActive            = false;
    public bool   DHCPActivatedOnFinish      = false;
    public bool   DHCPv6Disabled             = false;
    public bool   DHCPAuthorized             = false;

    public bool DHCPScopeNameSet  => !string.IsNullOrEmpty(DHCPScopeName);
    public bool DHCPScopeStartSet => !string.IsNullOrEmpty(DHCPScopeStart);
    public bool DHCPScopeEndSet   => !string.IsNullOrEmpty(DHCPScopeEnd);

    // ── File Services ─────────────────────────────────────────────────────────
    public bool FileExplorerOpened           = false;
    public bool FileExplorerClosed           = false;
    public bool ThisPCDriveSelected          = false;
    public bool NewFolderOptionClicked       = false;
    public bool FolderPropertiesOpened       = false;
    public bool FolderSharingTabOpened       = false;
    public bool AdvancedSharingOpened        = false;
    public bool ShareFolderToggled           = false;
    public bool FolderPermissionsDialogOpened = false;
    public bool FolderPropertiesClosed       = false;
    public List<FolderData>       UserCreatedFolders = new List<FolderData>();
    public List<SharedFolderData> SharedFolders      = new List<SharedFolderData>();

    public bool SharedFolderCreated  => SharedFolders.Count > 0;
    public bool FolderPermissionsSet => SharedFolders.Exists(f => f.PermissionsSet);

    // ── Group Policy ──────────────────────────────────────────────────────────
    public bool GPMOpened                       = false;
    public bool GPMClosed                       = false;
    public bool GPOWizardOpened                 = false;
    public bool SecurityFilterAddDialogOpened   = false;
    public bool SecurityFilterNameTyped         = false;
    public bool FolderRedirectionNodeOpened     = false;
    public bool FolderRedirectionPropertiesOpened = false;
    public bool FolderRedirectionSettingChanged = false;
    public bool FolderRedirectionRootPathEntered = false;
    public bool FolderRedirectionSettingsTabOpened = false;
    public List<GPOData> GroupPolicies          = new List<GPOData>();

    // At least one GPO exists for every OU
    public bool GPOCreatedForAllOUs =>
        OrganizationalUnits.Count > 0 &&
        OrganizationalUnits.TrueForAll(ou => GroupPolicies.Exists(g => g.LinkedOUName == ou.Name));

    // Every OU's GPO has been opened in the editor
    public bool AllGPOEditorsOpened =>
        GPOCreatedForAllOUs &&
        OrganizationalUnits.TrueForAll(ou =>
            GroupPolicies.Exists(g => g.LinkedOUName == ou.Name && g.EditorOpened));

    // Every OU's GPO has Desktop redirect configured
    public bool AllDesktopRedirectsSet =>
        GPOCreatedForAllOUs &&
        OrganizationalUnits.TrueForAll(ou =>
            GroupPolicies.Exists(g => g.LinkedOUName == ou.Name && g.DesktopRedirectSet));

    // Every OU's GPO has Documents redirect configured
    public bool AllDocumentsRedirectsSet =>
        GPOCreatedForAllOUs &&
        OrganizationalUnits.TrueForAll(ou =>
            GroupPolicies.Exists(g => g.LinkedOUName == ou.Name && g.DocumentsRedirectSet));

    // Every OU's GPO is set to Enforced
    public bool AllGPOsEnforced =>
        GPOCreatedForAllOUs &&
        OrganizationalUnits.TrueForAll(ou =>
            GroupPolicies.Exists(g => g.LinkedOUName == ou.Name && g.Enforced));

    // Every OU's GPO has at least one user added to Security Filtering
    public bool SecurityFilteringConfigured =>
        GPOCreatedForAllOUs &&
        OrganizationalUnits.TrueForAll(ou =>
            GroupPolicies.Exists(g => g.LinkedOUName == ou.Name
                                   && g.SecurityFilterUsernames.Count > 0));

    // ── Print Services ────────────────────────────────────────────────────────
    public bool   PrintMgmtOpened           = false;
    public bool   PrintMgmtClosed           = false;
    public bool   PrintServerNodeExpanded   = false;
    public bool   AddPrinterWizardOpened    = false;
    public bool   ExistingPortOptionSelected = false;
    public bool   PrinterPortSelected       = false;
    public bool   InstallNewDriverSelected  = false;
    public bool   PrinterManufacturerSelected = false;
    public bool   PrinterTypeSelected       = false;
    public bool   PrinterDriverAdded        = false; // set when wizard summary (step 5) renders
    public bool   PrinterShared             = false; // set when Finish is clicked (step 6)
    public string PrinterShareName          = "";    // kept for state compatibility
    public string InstalledPrinterName      = "";
    public string InstalledDriverName       = "";
    public string InstalledPortName         = "";

    // ── Remote Desktop ────────────────────────────────────────────────────────
    public bool LocalServerTabVisited        = false;
    public bool RemoteDesktopPropertyClicked = false;
    public bool RemoteDesktopCheckboxToggled = false;
    public bool RemoteDesktopEnabled         = false; // toggled via System Properties → Remote tab
    public bool RDVerifiedInDashboard        = false;
    public bool RDAppAvailable               => RDServicesInstalled;

    // ── Remote Desktop Session (Client PC → Server) ───────────────────────────
    public bool RDConnectionPanelOpened = false;
    public bool RDComputerNameEntered   = false;
    public bool RDCredentialsEntered    = false;
    public bool RDSessionOpened         = false;
    public bool ClientConnected         = false; // mirrors RDSessionOpened; kept for compatibility

    // ── Restart & PC switch ──────────────────────────────────────────────────
    public bool ServerRestartInitiated = false;
    public bool ServerRestartCompleted = false;
    public bool SwitchPCButtonClicked  = false;

    // ── RDS session context (only set while RDSessionOpened is true) ─────────
    public bool ADUCOpenedInRDSSession   = false;
    public bool DomainNodeExpandedInRDS  = false;
    public bool ADUCClosedInRDSSession   = false;
    public bool ServerManagerClosedInRDS = false;
}
