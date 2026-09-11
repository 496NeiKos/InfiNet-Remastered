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
    public string FirstName  = "";
    public string LastName   = "";
    public string Username   = "";
    public string Password   = "";
    public string OUName     = "";    // which OU this user belongs to
    public bool   IsAdmin    = false; // true = added to Domain Admins group
}

[System.Serializable]
public class SharedFolderData
{
    public string FolderName   = "";
    public string Path         = "";  // e.g. D:\SharedFolder
    public bool   PermissionsSet = false;
}

[System.Serializable]
public class FileScreenData
{
    public string GroupName        = "";
    public string TargetFolderName = ""; // which SharedFolderData.FolderName it applies to
    public List<string> BlockedExtensions = new List<string>();
}

[System.Serializable]
public class GPOData
{
    public string Name          = "";
    public string LinkedOUName  = ""; // which OUData.Name it is linked to
    public string RedirectPath  = ""; // \\SERVER\FolderName
    public bool   EditorOpened  = false;
    public bool   RedirectSet   = false;
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

    // Computed shortcuts
    public bool PCRenamed         => !string.IsNullOrEmpty(ComputerName);
    public bool PasswordSet       => !string.IsNullOrEmpty(AdminPassword);

    // ── Navigation latches (set by controllers when player visits a page) ────
    public bool SettingsOpened        = false;
    public bool SettingsSystemOpened  = false;
    public bool SettingsAboutOpened   = false;
    public bool SettingsAccountsOpened = false;
    public bool PasswordPanelOpened   = false;
    // NSCOpened, IPv4PanelOpened, AdapterSettingsVisited — inherited from DeviceOSState

    // ── Server Manager ───────────────────────────────────────────────────────
    public bool ServerManagerOpened   = false;

    // ── Roles installed ──────────────────────────────────────────────────────
    public bool ADDSInstalled              = false;
    public bool IsDomainController         = false;
    public bool DNSInstalled               = false; // set true during dcpromo
    public bool DHCPInstalled              = false;
    public bool FileServicesInstalled      = false;
    public bool PrintServicesInstalled     = false;
    public bool RDServicesInstalled        = false;

    // Wizard opened latches (one per role, prevents double-counting)
    public bool AddRolesWizardOpenedForADDS  = false;
    public bool AddRolesWizardOpenedForDHCP  = false;
    public bool AddRolesWizardOpenedForFile  = false;
    public bool AddRolesWizardOpenedForPrint = false;
    public bool AddRolesWizardOpenedForRD    = false;

    // ── Domain / dcpromo ─────────────────────────────────────────────────────
    public bool   PromotionNotificationClicked = false;
    public string DomainName                   = "";
    public bool   DNSCheckedInDCPromo          = false;
    public string ForestFunctionalLevel        = "";
    public bool   DCPromoCompleted             = false;

    // ── Active Directory ─────────────────────────────────────────────────────
    public bool ADUCOpened = false;
    public List<OUData>   OrganizationalUnits = new List<OUData>();
    public List<UserData> UserAccounts        = new List<UserData>();

    public bool AdminAccountCreated => UserAccounts.Exists(u => u.IsAdmin);
    public int  RegularUserCount    => UserAccounts.FindAll(u => !u.IsAdmin).Count;

    // ── DHCP ─────────────────────────────────────────────────────────────────
    public bool   DHCPConsoleOpened = false;
    public string DHCPScopeName     = "";
    public string DHCPScopeStart    = "";
    public string DHCPScopeEnd      = "";
    public bool   DHCPScopeActive   = false;
    public bool   DHCPv6Disabled    = false;

    public bool DHCPScopeNameSet  => !string.IsNullOrEmpty(DHCPScopeName);
    public bool DHCPScopeStartSet => !string.IsNullOrEmpty(DHCPScopeStart);
    public bool DHCPScopeEndSet   => !string.IsNullOrEmpty(DHCPScopeEnd);

    // ── File Services ─────────────────────────────────────────────────────────
    public bool FSRMOpened = false;
    public List<SharedFolderData> SharedFolders = new List<SharedFolderData>();
    public List<FileScreenData>   FileScreens   = new List<FileScreenData>();

    public bool SharedFolderCreated    => SharedFolders.Count > 0;
    public bool FolderPermissionsSet   => SharedFolders.Exists(f => f.PermissionsSet);
    public bool FileGroupCreated       => FileScreens.Count > 0 && FileScreens[0].BlockedExtensions.Count > 0;
    public bool FileScreenApplied      => FileScreens.Count > 0 && !string.IsNullOrEmpty(FileScreens[0].TargetFolderName);

    // ── Group Policy ──────────────────────────────────────────────────────────
    public bool GPMOpened = false;
    public List<GPOData> GroupPolicies = new List<GPOData>();

    public int  GPOCount                  => GroupPolicies.Count;
    public bool BothGPOsEditorOpened      => GroupPolicies.FindAll(g => g.EditorOpened).Count >= 2;
    public bool BothFolderRedirectsSet    => GroupPolicies.FindAll(g => g.RedirectSet).Count >= 2;

    // ── Print Services ────────────────────────────────────────────────────────
    public bool PrintMgmtOpened    = false;
    public bool PrinterDriverAdded = false;
    public bool PrinterShared      = false;
    public string PrinterShareName = "";

    // ── Remote Desktop ────────────────────────────────────────────────────────
    public bool RDVerifiedInDashboard = false;
    public bool RDAppAvailable        => RDServicesInstalled;

    // ── Client Verification ───────────────────────────────────────────────────
    public bool RDSessionOpened           = false;
    public bool ClientConnected           = false;
    public bool ClientDHCPVerified        = false;
    public bool FolderRedirectionVerified = false;
    public bool PrinterVerified           = false;
    public bool ConnectivityVerified      = false;
}
