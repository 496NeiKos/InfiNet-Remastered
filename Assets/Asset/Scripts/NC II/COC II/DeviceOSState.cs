using System.Collections.Generic;

public enum DeviceID { Computer1, Computer2, Laptop }

public class DeviceOSState
{
    public DeviceID Device;

    // WiFi
    public bool WifiConnected;
    public string WifiSSID = "";
    public string WifiPassword = "";

    // IPv4 config
    public bool UseStaticIP;
    public string[] IPOctets    = new string[4] { "", "", "", "" };
    public string[] SubnetOctets = new string[4] { "255", "255", "255", "0" };
    public string[] GatewayOctets = new string[4] { "", "", "", "" };
    public bool UseStaticDNS;
    public string[] PreferredDNSOctets = new string[4] { "", "", "", "" };
    public string[] AlternateDNSOctets = new string[4] { "", "", "", "" };

    // IPv6
    public bool IPv6Unchecked;

    // Advanced sharing (Option 2 — Guest/Public)
    public bool NetworkDiscoveryOn   = false;
    public bool FilePrinterSharingOn = false;

    // Advanced sharing (Option 3 — All Networks)
    public bool PublicFolderSharingOn = false;
    public bool MediaStreamingOn      = false;
    public bool PasswordProtectedSharingOff;   // false = PPS is ON (default); student must set to true (Off)

    // Firewall
    public bool FirewallPrivateOff;
    public bool FirewallPublicOff;

    // Navigation history for Prev/Next
    public readonly List<int> PanelHistory = new List<int>();
    public int HistoryIndex = -1;

    // Chrome navigation blueprint (per-device, independent of router/AP values)
    public int        ChromeCurrentPage  = 0;     // 0=Default 1=TPLinkLogin 2=TPLinkMain 3=DLinkAP
    public List<int>  ChromeHistory      = new List<int>(); // history stack, bottom→top
    public bool       ChromeIsLoggedIn   = false;

    // TPLink navigation blueprint (per-device; router config values stay on TPLinkTabManager)
    public int TPLinkActiveMainTab = 0;  // 0=InterfaceSetup 1=Status
    public int TPLinkActiveSubTab  = 0;  // 0=Wireless 1=LAN

    // Ping Test — per device, independent progress
    public bool[]       PingResults      = new bool[5];        // [0]=Router [1]=AP [2]=PC1 [3]=PC2 [4]=Laptop
    public List<string> CmdOutputLines   = new List<string>(); // visual history; cleared on CMD exit, kept on ESC/switch
    public int          IpconfigRunCount = 0;                  // incremented each time ipconfig is executed on this device

    public bool IPConfigured      => UseStaticIP && IsValidIP(IPOctets);
    public bool SharingConfigured => NetworkDiscoveryOn && FilePrinterSharingOn && PasswordProtectedSharingOff;
    public bool FirewallConfigured => FirewallPrivateOff && FirewallPublicOff;
    public bool FullyConfigured   => IPConfigured && IPv6Unchecked && SharingConfigured && FirewallConfigured;

    private static bool IsValidIP(string[] octets)
    {
        if (octets == null || octets.Length != 4) return false;
        foreach (string o in octets)
            if (string.IsNullOrEmpty(o)) return false;
        return true;
    }
}
