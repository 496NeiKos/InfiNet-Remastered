/*
 * ================================================================
 *  UNITY SETUP GUIDE — DLinkAPManager
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to "DLink AP Page" (direct child of Chrome,
 *    sibling of Default Page and TP Link Page).
 *
 *  HIERARCHY
 *    DLink AP Page  (this script here)
 *      └── MainContent
 *            ├── Tab Row
 *            │     ├── WirelessBtn   → wirelessBtn  ← default selected
 *            │     └── NetworkBtn    → networkBtn
 *            ├── Wireless Panel  → wirelessPanel  ← active by default
 *            │     ├── Wi-Fi Name (SSID) input  → ssidField
 *            │     ├── Security dropdown         → securityDropdown
 *            │     │     options: 0=None (default)  1=WPA/WPA2 Personal
 *            │     └── Password input            → passwordField
 *            │           (non-interactable + empty when Security = None)
 *            ├── Network Panel   → networkPanel  ← inactive by default
 *            │     ├── Connection Type dropdown  → connectionTypeDropdown
 *            │     │     options: 0=Dynamic IP (default)  1=Static IP
 *            │     ├── IP Address input          → ipAddressField
 *            │     │     (Class C recommended: 192.168.100.x)
 *            │     ├── Subnet Mask input         → subnetMaskField
 *            │     ├── Gateway Address input     → gatewayField
 *            │     │     (enter router's LAN IP here)
 *            │     └── Primary DNS Server input  → primaryDNSField
 *            │           (stub for COC III — visible, not validated here)
 *            │     IP/Subnet/Gateway/DNS are non-interactable when Connection Type = Dynamic IP.
 *            └── Save & Cancel Panel
 *                  ├── Save Button   → saveBtn
 *                  └── Cancel Button → cancelBtn
 *
 *  INSPECTOR ASSIGNMENTS
 *    wirelessBtn            → Wireless tab button
 *    networkBtn             → Network tab button
 *    wirelessPanel          → Wireless body panel
 *    networkPanel           → Network body panel
 *    ssidField              → Wi-Fi Name (SSID) TMP_InputField
 *    securityDropdown       → Security TMP_Dropdown
 *    passwordField          → Password TMP_InputField
 *    connectionTypeDropdown → Connection Type TMP_Dropdown
 *    ipAddressField         → IP Address TMP_InputField
 *    subnetMaskField        → Subnet Mask TMP_InputField
 *    gatewayField           → Gateway Address TMP_InputField
 *    primaryDNSField        → Primary DNS Server TMP_InputField
 *    saveBtn                → Save Button
 *    cancelBtn              → Cancel Button
 *    apReset                → AccessPointResetController on the AP reset button
 *    defaultSsid            → default SSID before any save (e.g. "DLink_WiFi")
 *    defaultSubnetMask      → default subnet mask (e.g. "255.255.255.0")
 *
 *  BUTTON OnClick — auto-wired in Awake. Do NOT wire manually.
 *
 *  HOW IT WORKS
 *    No login — Chrome navigates here directly after typing apSearchKeyword.
 *    Tab switch WITHOUT saving → resets the tab being left to last saved state.
 *    Security = None           → Password field is non-interactable and shows empty.
 *    Security = WPA/WPA2 Personal → Password field is interactable.
 *    Connection Type = Dynamic → IP/Subnet/Gateway/DNS fields are non-interactable.
 *    Connection Type = Static  → all LAN fields are interactable.
 *    Save + Static IP + valid IP → calls apReset.SetConfigured().
 *    Save + Dynamic IP           → apReset.IsConfigured is NOT set (student must use Static).
 *    OnEnable applies stored config — no per-device save/load (AP is global physical state).
 *
 *  COLORS
 *    selectedColor   → #B2D9FF (matches TP-Link tab highlight)
 *    unselectedColor → white
 *
 *  PUBLIC API (used by InternetPanelController)
 *    GetApSsid()         → saved SSID (displayed as Network Option 2 label)
 *    GetApPreSharedKey() → saved password (validated on WiFi connection)
 *    GetApSecurityMode() → 0=None  1=WPA/WPA2 Personal
 *                          (used to skip password panel for open networks)
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DLinkAPManager : MonoBehaviour
{
    private enum Tab { Wireless, Network }

    private struct DLinkConfig
    {
        public string Ssid;
        public int    SecurityMode;    // 0=None  1=WPA/WPA2 Personal
        public string Password;
        public int    ConnectionType;  // 0=Dynamic IP  1=Static IP
        public string IPAddress;
        public string SubnetMask;
        public string GatewayAddress;
        public string PrimaryDNS;
    }

    // ----------------------------------------------------------------
    //  Inspector fields
    // ----------------------------------------------------------------

    [Header("Tab Buttons")]
    [SerializeField] private Button wirelessBtn;
    [SerializeField] private Button networkBtn;

    [Header("Body Panels")]
    [SerializeField] private GameObject wirelessPanel;
    [SerializeField] private GameObject networkPanel;

    [Header("Wireless Fields")]
    [SerializeField] private TMP_InputField ssidField;
    [SerializeField] private TMP_Dropdown   securityDropdown;
    [SerializeField] private TMP_InputField passwordField;

    [Header("Network Fields")]
    [SerializeField] private TMP_Dropdown   connectionTypeDropdown;
    [SerializeField] private TMP_InputField ipAddressField;
    [SerializeField] private TMP_InputField subnetMaskField;
    [SerializeField] private TMP_InputField gatewayField;
    [SerializeField] private TMP_InputField primaryDNSField;

    [Header("Save & Cancel")]
    [SerializeField] private Button saveBtn;
    [SerializeField] private Button cancelBtn;

    [Header("Colors")]
    [SerializeField] private Color selectedColor   = new Color(0.698f, 0.851f, 1f); // #B2D9FF
    [SerializeField] private Color unselectedColor = Color.white;

    [Header("Configurable Defaults")]
    [SerializeField] private string defaultSsid       = "DLink_WiFi";
    [SerializeField] private string defaultSubnetMask = "255.255.255.0";

    [Header("AP Reset Controller")]
    [SerializeField] private AccessPointResetController apReset;

    // ----------------------------------------------------------------
    //  Runtime state
    // ----------------------------------------------------------------

    private Tab        _activeTab = Tab.Wireless;
    private DLinkConfig _config;
    private bool        _hasSavedOnce;
    private bool        _initialized;

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        if (wirelessBtn != null) wirelessBtn.onClick.AddListener(() => SelectTab(Tab.Wireless));
        if (networkBtn  != null) networkBtn.onClick.AddListener(() => SelectTab(Tab.Network));
        if (saveBtn     != null) saveBtn.onClick.AddListener(OnSave);
        if (cancelBtn   != null) cancelBtn.onClick.AddListener(OnCancel);

        if (securityDropdown       != null)
            securityDropdown.onValueChanged.AddListener(OnSecurityChanged);
        if (connectionTypeDropdown != null)
            connectionTypeDropdown.onValueChanged.AddListener(OnConnectionTypeChanged);
    }

    private void OnEnable()
    {
        if (!_initialized)
        {
            _config       = BuildDefaultConfig();
            _hasSavedOnce = false;
            _initialized  = true;
        }

        _activeTab = Tab.Wireless;
        ApplyConfigToUI(_config);
        RefreshTabVisuals();
        RefreshBodyPanels();
    }

    // ----------------------------------------------------------------
    //  Tab
    // ----------------------------------------------------------------

    private void SelectTab(Tab tab)
    {
        if (tab == _activeTab) return;
        RestoreActiveTabToSavedState();
        _activeTab = tab;
        RefreshTabVisuals();
        RefreshBodyPanels();
        Debug.Log($"[DLinkAPManager] Tab → {tab}.");
    }

    private void RefreshTabVisuals()
    {
        SetButtonColor(wirelessBtn, _activeTab == Tab.Wireless ? selectedColor : unselectedColor);
        SetButtonColor(networkBtn,  _activeTab == Tab.Network  ? selectedColor : unselectedColor);
    }

    private void RefreshBodyPanels()
    {
        if (wirelessPanel != null) wirelessPanel.SetActive(_activeTab == Tab.Wireless);
        if (networkPanel  != null) networkPanel.SetActive(_activeTab == Tab.Network);
    }

    // ----------------------------------------------------------------
    //  Conditional field interactability
    // ----------------------------------------------------------------

    private void OnSecurityChanged(int value)
    {
        bool hasPassword = value == 1;
        if (passwordField != null)
        {
            passwordField.interactable = hasPassword;
            if (!hasPassword) passwordField.text = "";
        }
    }

    private void OnConnectionTypeChanged(int value)
    {
        bool isStatic = value == 1;
        if (ipAddressField  != null) ipAddressField.interactable  = isStatic;
        if (subnetMaskField != null) subnetMaskField.interactable = isStatic;
        if (gatewayField    != null) gatewayField.interactable    = isStatic;
        if (primaryDNSField != null) primaryDNSField.interactable = isStatic;
    }

    // ----------------------------------------------------------------
    //  Save
    // ----------------------------------------------------------------

    private void OnSave()
    {
        CommitCurrentState();

        if (_config.ConnectionType == 1 && IsValidIPAddress(_config.IPAddress))
            apReset?.SetConfigured();

        Debug.Log($"[DLinkAPManager] Saved — Static: {_config.ConnectionType == 1}, IP: {_config.IPAddress}.");
    }

    private void CommitCurrentState()
    {
        DLinkConfig cfg = _config;

        if (ssidField != null) cfg.Ssid = ssidField.text;
        cfg.SecurityMode = securityDropdown != null ? securityDropdown.value : 0;
        cfg.Password     = (cfg.SecurityMode == 1 && passwordField != null) ? passwordField.text : "";

        cfg.ConnectionType = connectionTypeDropdown != null ? connectionTypeDropdown.value : 0;
        if (ipAddressField  != null) cfg.IPAddress     = ipAddressField.text;
        if (subnetMaskField != null) cfg.SubnetMask    = subnetMaskField.text;
        if (gatewayField    != null) cfg.GatewayAddress = gatewayField.text;
        if (primaryDNSField != null) cfg.PrimaryDNS    = primaryDNSField.text;

        _config       = cfg;
        _hasSavedOnce = true;
        Debug.Log("[DLinkAPManager] Config committed.");
    }

    // ----------------------------------------------------------------
    //  Cancel / Restore
    // ----------------------------------------------------------------

    private void OnCancel()
    {
        RestoreActiveTabToSavedState();
        Debug.Log("[DLinkAPManager] Cancel — active tab restored to saved state.");
    }

    private void RestoreActiveTabToSavedState()
    {
        DLinkConfig source = _hasSavedOnce ? _config : BuildDefaultConfig();
        if (_activeTab == Tab.Wireless) ApplyWirelessToUI(source);
        else                            ApplyNetworkToUI(source);
    }

    // ----------------------------------------------------------------
    //  Apply config to UI
    // ----------------------------------------------------------------

    private void ApplyConfigToUI(DLinkConfig cfg)
    {
        ApplyWirelessToUI(cfg);
        ApplyNetworkToUI(cfg);
    }

    private void ApplyWirelessToUI(DLinkConfig cfg)
    {
        if (ssidField        != null) ssidField.text        = cfg.Ssid;
        if (securityDropdown != null) securityDropdown.value = cfg.SecurityMode;

        bool hasPassword = cfg.SecurityMode == 1;
        if (passwordField != null)
        {
            passwordField.interactable = hasPassword;
            passwordField.text         = hasPassword ? cfg.Password : "";
        }
    }

    private void ApplyNetworkToUI(DLinkConfig cfg)
    {
        if (connectionTypeDropdown != null) connectionTypeDropdown.value = cfg.ConnectionType;

        bool isStatic = cfg.ConnectionType == 1;
        if (ipAddressField  != null) { ipAddressField.interactable  = isStatic; ipAddressField.text  = cfg.IPAddress;      }
        if (subnetMaskField != null) { subnetMaskField.interactable = isStatic; subnetMaskField.text = cfg.SubnetMask;     }
        if (gatewayField    != null) { gatewayField.interactable    = isStatic; gatewayField.text    = cfg.GatewayAddress; }
        if (primaryDNSField != null) { primaryDNSField.interactable = isStatic; primaryDNSField.text = cfg.PrimaryDNS;    }
    }

    // ----------------------------------------------------------------
    //  Defaults
    // ----------------------------------------------------------------

    private DLinkConfig BuildDefaultConfig() => new DLinkConfig
    {
        Ssid           = defaultSsid,
        SecurityMode   = 0,
        Password       = "",
        ConnectionType = 0,
        IPAddress      = "",
        SubnetMask     = defaultSubnetMask,
        GatewayAddress = "",
        PrimaryDNS     = "",
    };

    // ----------------------------------------------------------------
    //  Public API (InternetPanelController)
    // ----------------------------------------------------------------

    public string GetApSsid()         => _config.Ssid;
    public string GetApPreSharedKey() => _config.Password;
    public int    GetApSecurityMode() => _config.SecurityMode;

    // ----------------------------------------------------------------
    //  Helpers
    // ----------------------------------------------------------------

    private static bool IsValidIPAddress(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return false;
        string[] parts = ip.Split('.');
        if (parts.Length != 4) return false;
        foreach (string part in parts)
            if (!int.TryParse(part, out int val) || val < 0 || val > 255) return false;
        return true;
    }

    private void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = color;
    }
}
