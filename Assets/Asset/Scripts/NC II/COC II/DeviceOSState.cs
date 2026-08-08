using System.Collections.Generic;

public enum DeviceType { Server, Desktop, Laptop }

public class DeviceOSState
{
    public DeviceType Device;

    // WiFi (Laptop only)
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
    public bool NetworkDiscoveryOn   = true;
    public bool FilePrinterSharingOn = true;

    // Advanced sharing (Option 3 — All Networks)
    public bool PublicFolderSharingOn = true;
    public bool MediaStreamingOn      = true;
    public bool PasswordProtectedSharingOff;   // false = PPS is ON (default); student must set to true (Off)

    // Firewall
    public bool FirewallPrivateOff;
    public bool FirewallPublicOff;

    // Navigation history for Prev/Next
    public readonly List<int> PanelHistory = new List<int>();
    public int HistoryIndex = -1;

    public bool IPConfigured    => UseStaticIP && IsValidIP(IPOctets);
    public bool SharingConfigured => NetworkDiscoveryOn && FilePrinterSharingOn && PasswordProtectedSharingOff;
    public bool FirewallConfigured => FirewallPrivateOff && FirewallPublicOff;
    public bool FullyConfigured =>
        (Device != DeviceType.Laptop || WifiConnected) &&
        IPConfigured && IPv6Unchecked &&
        SharingConfigured && FirewallConfigured;

    private static bool IsValidIP(string[] octets)
    {
        if (octets == null || octets.Length != 4) return false;
        foreach (string o in octets)
            if (string.IsNullOrEmpty(o)) return false;
        return true;
    }
}
