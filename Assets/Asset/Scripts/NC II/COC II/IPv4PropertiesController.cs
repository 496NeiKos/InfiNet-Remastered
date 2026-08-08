/*
 * ================================================================
 *  UNITY SETUP GUIDE — IPv4PropertiesController
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to "IPv4PropertiesPanel".
 *
 *  RADIO BUTTON BEHAVIOR (the linked rule)
 *    Default state:   [Obtain IP Auto] selected  +  [Obtain DNS Auto] selected
 *                     → all input fields locked
 *
 *    Click [IP Address Btn]:
 *      → IP Address Btn becomes selected (blue)
 *      → IP fields become interactable
 *      → DNS auto-switches to [DNS Server Btn] (blue)
 *      → DNS fields become interactable
 *      → [Obtain DNS Auto Btn] becomes LOCKED (non-interactable) while IP static is active
 *
 *    Click [Obtain IP Auto Btn] (while IP static is active):
 *      → IP Address Btn deselected, Obtain IP Auto selected (blue)
 *      → IP fields locked
 *      → [Obtain DNS Auto Btn] becomes UNLOCKED again
 *      → DNS selection stays as-is (does NOT reset to auto)
 *
 *  HIERARCHY
 *    IPv4PropertiesPanel  (this script here)
 *      ├── Nav
 *      ├── Body
 *      │     ├── Description
 *      │     ├── Obtain IP Auto Btn      → obtainIPAutoBtn
 *      │     ├── IP Adress Panel         → ipAddressPanel (contains octet rows)
 *      │     ├── IP Adress Btn           → useStaticIPBtn
 *      │     ├── DNS Server Adress Panel → dnsPanel (contains octet rows)
 *      │     ├── Obtain DNS Server Auto Btn → obtainDNSAutoBtn
 *      │     ├── DNS Server Btn          → useStaticDNSBtn
 *      │     └── ExtraOptionPanel        (non-functional)
 *      └── DecisionPanel
 *            ├── OKBtn     → OnClick: IPv4PropertiesController.OnOK()
 *            └── CancelBtn → OnClick: IPv4PropertiesController.OnCancel()
 *
 *  INSPECTOR ASSIGNMENTS
 *    obtainIPAutoBtn     → "Obtain IP Auto Btn"
 *    useStaticIPBtn      → "IP Adress Btn"
 *    obtainDNSAutoBtn    → "Obtain DNS Server Auto Btn"
 *    useStaticDNSBtn     → "DNS Server Btn"
 *    ipAddressPanel      → "IP Adress Panel"
 *    dnsPanel            → "DNS Server Adress Panel"
 *    obtainIPRadioImg    → selection-dot Image on Obtain IP Auto row
 *    staticIPRadioImg    → selection-dot Image on IP Address Btn row
 *    obtainDNSRadioImg   → selection-dot Image on Obtain DNS Auto row
 *    staticDNSRadioImg   → selection-dot Image on DNS Server Btn row
 *    ipOctets[0..3]      → IPOctetField on each octet of the IP address row
 *    subnetOctets[0..3]  → IPOctetField on each octet of the Subnet Mask row (isReadOnly=true)
 *    gatewayOctets[0..3] → IPOctetField on each octet of the Default Gateway row
 *    preferredDNSOctets[0..3] → IPOctetField on Preferred DNS row
 *    alternateDNSOctets[0..3] → IPOctetField on Alternate DNS row
 *
 *  BUTTON OnClick WIRING
 *    Obtain IP Auto Btn      → IPv4PropertiesController.ClickObtainIPAuto()
 *    IP Adress Btn           → IPv4PropertiesController.ClickUseStaticIP()
 *    Obtain DNS Auto Btn     → IPv4PropertiesController.ClickObtainDNSAuto()
 *    DNS Server Btn          → IPv4PropertiesController.ClickUseStaticDNS()
 *    OKBtn                   → IPv4PropertiesController.OnOK()
 *    CancelBtn               → IPv4PropertiesController.OnCancel()
 *
 *  IMPLEMENTATION GUIDE (what to add in the Editor)
 *    Inside "IP Adress Panel" create three rows, each with:
 *      • TMP_Text label ("IP address:", "Subnet mask:", "Default gateway:")
 *      • 4 x TMP_InputField, each with an IPOctetField component
 *        Chain: Octet1.nextField=Octet2, Octet2.nextField=Octet3, Octet3.nextField=Octet4
 *      • 3 x TMP_Text "." separators between the fields
 *    Subnet mask row: set isReadOnly=true on all 4 IPOctetFields (auto-filled).
 *    Repeat inside "DNS Server Adress Panel" for Preferred DNS + Alternate DNS rows.
 *    Add selection-dot Images (small circle/dot) next to each radio button label —
 *    these are the blue indicator images assigned above.
 * ================================================================
 */

using UnityEngine;
using UnityEngine.UI;

public class IPv4PropertiesController : MonoBehaviour
{
    [Header("Radio Buttons")]
    [SerializeField] private Button obtainIPAutoBtn;
    [SerializeField] private Button useStaticIPBtn;
    [SerializeField] private Button obtainDNSAutoBtn;
    [SerializeField] private Button useStaticDNSBtn;

    [Header("Radio Indicator Images (dot/circle next to each option)")]
    [SerializeField] private Image obtainIPRadioImg;
    [SerializeField] private Image staticIPRadioImg;
    [SerializeField] private Image obtainDNSRadioImg;
    [SerializeField] private Image staticDNSRadioImg;

    [Header("Field Panels")]
    [SerializeField] private GameObject ipAddressPanel;
    [SerializeField] private GameObject dnsPanel;

    [Header("IP Address Octets (0–3)")]
    [SerializeField] private IPOctetField[] ipOctets      = new IPOctetField[4];
    [SerializeField] private IPOctetField[] subnetOctets  = new IPOctetField[4];
    [SerializeField] private IPOctetField[] gatewayOctets = new IPOctetField[4];

    [Header("DNS Octets (0–3)")]
    [SerializeField] private IPOctetField[] preferredDNSOctets = new IPOctetField[4];
    [SerializeField] private IPOctetField[] alternateDNSOctets = new IPOctetField[4];

    [Header("Colors")]
    [SerializeField] private Color selectedColor     = new Color(0.29f, 0.56f, 0.85f); // blue
    [SerializeField] private Color unselectedColor   = Color.white;
    [SerializeField] private Color lockedButtonColor = new Color(0.6f, 0.6f, 0.6f);    // gray when locked

    private bool _useStaticIP;   // false = Obtain Auto (default)
    private bool _useStaticDNS;  // false = Obtain Auto (default)

    // Snapshot for Cancel
    private bool     _snapStaticIP, _snapStaticDNS;
    private string[] _snapIP    = new string[4];
    private string[] _snapGW    = new string[4];
    private string[] _snapPDNS  = new string[4];
    private string[] _snapADNS  = new string[4];

    private static readonly string[] SubnetDefault = { "255", "255", "255", "0" };

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void OnEnable()
    {
        LoadFromState();
        TakeSnapshot();
    }

    // ----------------------------------------------------------------
    //  Radio button handlers
    // ----------------------------------------------------------------

    public void ClickObtainIPAuto()
    {
        _useStaticIP = false;
        // Clear all IP fields — real Windows resets them when switching back to auto
        ClearOctets(ipOctets);
        ClearOctets(subnetOctets);
        ClearOctets(gatewayOctets);
        SetIPFieldsInteractable(false);
        SetIPRadio(false);
        // Unlock DNS auto — but don't change DNS selection
        SetObtainDNSInteractable(true);
        Debug.Log("[IPv4PropertiesController] Obtain IP automatically selected — fields cleared.");
    }

    public void ClickUseStaticIP()
    {
        _useStaticIP  = true;
        _useStaticDNS = true; // auto-switch DNS to static
        SetIPFieldsInteractable(true);
        SetIPRadio(true);
        SetDNSFieldsInteractable(true);
        SetDNSRadio(true);
        // Lock DNS auto while IP static is active
        SetObtainDNSInteractable(false);

        // Auto-fill subnet mask
        for (int i = 0; i < 4; i++)
            subnetOctets[i]?.SetValue(SubnetDefault[i]);

        Debug.Log("[IPv4PropertiesController] Use static IP selected — DNS auto-switched to static.");
    }

    public void ClickObtainDNSAuto()
    {
        // Only reachable when IP is in auto mode (otherwise button is locked)
        _useStaticDNS = false;
        // Clear DNS fields — real Windows resets them when switching back to auto
        ClearOctets(preferredDNSOctets);
        ClearOctets(alternateDNSOctets);
        SetDNSFieldsInteractable(false);
        SetDNSRadio(false);
        Debug.Log("[IPv4PropertiesController] Obtain DNS automatically selected — fields cleared.");
    }

    public void ClickUseStaticDNS()
    {
        _useStaticDNS = true;
        SetDNSFieldsInteractable(true);
        SetDNSRadio(true);
        Debug.Log("[IPv4PropertiesController] Use static DNS selected.");
    }

    // ----------------------------------------------------------------
    //  OK / Cancel
    // ----------------------------------------------------------------

    public void OnOK()
    {
        string error = Validate();
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogWarning($"[IPv4PropertiesController] Validation failed: {error}");
            return;
        }
        SaveToState();
        gameObject.SetActive(false);
        Debug.Log("[IPv4PropertiesController] OK — IP config saved.");
    }

    public void OnCancel()
    {
        RestoreSnapshot();
        gameObject.SetActive(false);
        Debug.Log("[IPv4PropertiesController] Cancel — reverted.");
    }

    // ----------------------------------------------------------------
    //  Validation
    // ----------------------------------------------------------------

    private string Validate()
    {
        if (!_useStaticIP) return "";

        for (int i = 0; i < 4; i++)
            if (string.IsNullOrEmpty(ipOctets[i]?.Value))
                return "IP address is incomplete.";

        if (!int.TryParse(ipOctets[3].Value, out int host) || host < 1 || host > 254)
            return "Host octet must be between 1 and 254.";

        if (VirtualOSManager.Instance != null)
        {
            string prefix = VirtualOSManager.Instance.GetNetworkPrefix();
            if (!string.IsNullOrEmpty(prefix))
            {
                string myPrefix = $"{ipOctets[0].Value}.{ipOctets[1].Value}.{ipOctets[2].Value}";
                if (myPrefix != prefix)
                    return $"IP must be on the same network ({prefix}.x).";
            }

            var usedHosts = VirtualOSManager.Instance.GetUsedHostOctets();
            if (usedHosts.Contains(ipOctets[3].Value))
                return "That IP is already used by another device.";
        }

        return "";
    }

    // ----------------------------------------------------------------
    //  State sync
    // ----------------------------------------------------------------

    private void LoadFromState()
    {
        DeviceOSState s = GetState();
        _useStaticIP  = s.UseStaticIP;
        _useStaticDNS = s.UseStaticDNS;

        for (int i = 0; i < 4; i++)
        {
            ipOctets[i]?.SetValue(s.IPOctets[i]);
            subnetOctets[i]?.SetValue(s.SubnetOctets[i]);
            gatewayOctets[i]?.SetValue(s.GatewayOctets[i]);
            preferredDNSOctets[i]?.SetValue(s.PreferredDNSOctets[i]);
            alternateDNSOctets[i]?.SetValue(s.AlternateDNSOctets[i]);
        }

        if (_useStaticIP)
            for (int i = 0; i < 4; i++)
                subnetOctets[i]?.SetValue(SubnetDefault[i]);

        SetIPFieldsInteractable(_useStaticIP);
        SetDNSFieldsInteractable(_useStaticDNS);
        SetIPRadio(_useStaticIP);
        SetDNSRadio(_useStaticDNS);
        SetObtainDNSInteractable(!_useStaticIP);
    }

    private void SaveToState()
    {
        DeviceOSState s = GetState();
        s.UseStaticIP  = _useStaticIP;
        s.UseStaticDNS = _useStaticDNS;

        for (int i = 0; i < 4; i++)
        {
            s.IPOctets[i]           = ipOctets[i]?.Value ?? "";
            s.SubnetOctets[i]       = subnetOctets[i]?.Value ?? SubnetDefault[i];
            s.GatewayOctets[i]      = gatewayOctets[i]?.Value ?? "";
            s.PreferredDNSOctets[i] = preferredDNSOctets[i]?.Value ?? "";
            s.AlternateDNSOctets[i] = alternateDNSOctets[i]?.Value ?? "";
        }
    }

    private void TakeSnapshot()
    {
        DeviceOSState s = GetState();
        _snapStaticIP  = s.UseStaticIP;
        _snapStaticDNS = s.UseStaticDNS;
        System.Array.Copy(s.IPOctets,           _snapIP,   4);
        System.Array.Copy(s.GatewayOctets,      _snapGW,   4);
        System.Array.Copy(s.PreferredDNSOctets, _snapPDNS, 4);
        System.Array.Copy(s.AlternateDNSOctets, _snapADNS, 4);
    }

    private void RestoreSnapshot()
    {
        DeviceOSState s = GetState();
        s.UseStaticIP  = _snapStaticIP;
        s.UseStaticDNS = _snapStaticDNS;
        System.Array.Copy(_snapIP,   s.IPOctets,           4);
        System.Array.Copy(_snapGW,   s.GatewayOctets,      4);
        System.Array.Copy(_snapPDNS, s.PreferredDNSOctets, 4);
        System.Array.Copy(_snapADNS, s.AlternateDNSOctets, 4);
    }

    // ----------------------------------------------------------------
    //  UI helpers
    // ----------------------------------------------------------------

    private void ClearOctets(IPOctetField[] octets)
    {
        foreach (var f in octets) f?.SetValue("");
    }

    private void SetIPFieldsInteractable(bool on)
    {
        foreach (var f in ipOctets)      f?.SetInteractable(on);
        foreach (var f in gatewayOctets) f?.SetInteractable(on);
        foreach (var f in subnetOctets)  f?.SetInteractable(false); // always read-only
    }

    private void SetDNSFieldsInteractable(bool on)
    {
        foreach (var f in preferredDNSOctets) f?.SetInteractable(on);
        foreach (var f in alternateDNSOctets) f?.SetInteractable(on);
    }

    private void SetObtainDNSInteractable(bool on)
    {
        if (obtainDNSAutoBtn != null)
        {
            obtainDNSAutoBtn.interactable = on;
            // Gray out the button when locked
            var img = obtainDNSAutoBtn.GetComponent<Image>();
            if (img != null) img.color = on ? Color.white : lockedButtonColor;
        }
    }

    private void SetIPRadio(bool staticSelected)
    {
        SetImageActive(obtainIPRadioImg, !staticSelected);
        SetImageActive(staticIPRadioImg,  staticSelected);
        SetButtonColor(obtainIPAutoBtn,  !staticSelected ? selectedColor : unselectedColor);
        SetButtonColor(useStaticIPBtn,    staticSelected ? selectedColor : unselectedColor);
    }

    private void SetDNSRadio(bool staticSelected)
    {
        SetImageActive(obtainDNSRadioImg, !staticSelected);
        SetImageActive(staticDNSRadioImg,  staticSelected);
        SetButtonColor(useStaticDNSBtn,    staticSelected ? selectedColor : unselectedColor);
        // Note: obtainDNSAutoBtn color is managed by SetObtainDNSInteractable
        if (obtainDNSAutoBtn != null && obtainDNSAutoBtn.interactable)
            SetButtonColor(obtainDNSAutoBtn, !staticSelected ? selectedColor : unselectedColor);
    }

    private void SetImageActive(Image img, bool active)
    {
        if (img != null) img.gameObject.SetActive(active);
    }

    private void SetButtonColor(Button btn, Color c)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = c;
    }

    private static DeviceOSState GetState()
    {
        return VirtualOSManager.Instance?.CurrentState ?? new DeviceOSState();
    }
}
