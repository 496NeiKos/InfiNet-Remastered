/*
 * ================================================================
 *  UNITY SETUP GUIDE — TPLinkTabManager
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to "Page 2 - Main" (or its direct child MainContent).
 *
 *  HIERARCHY (relevant nodes only)
 *    Page 2 - Main  (this script here)
 *      └── MainContent
 *            ├── Header
 *            │     └── Bottom Header
 *            │           └── Right-Bottom Header
 *            │                 ├── Main Tab
 *            │                 │     ├── [5 non-functional btns] → disabledMainTabBtns[]
 *            │                 │     ├── InterfaceSetupBtn       → interfaceSetupBtn  ← default selected
 *            │                 │     └── StatusBtn               → statusBtn
 *            │                 └── Sub Tab
 *            │                       ├── InterfaceSetup Sub Tab  → interfaceSetupSubTab  ← active by default
 *            │                       │     ├── WirelessBtn       → wirelessBtn  ← default selected
 *            │                       │     ├── LANBtn            → lanBtn
 *            │                       │     └── [3 non-functional btns] → disabledSubTabBtns[]
 *            │                       └── Status Sub Tab          → statusSubTab  ← inactive by default
 *            ├── Body
 *            │     ├── InterfaceSetup Tab - Wireless  → wirelessPanel  ← active by default
 *            │     │     ├── WPS Settings panel
 *            │     │     │     ├── WPS State TMP (display only)  → wpsStateTMP
 *            │     │     │     ├── WPS Mode buttons
 *            │     │     │     │     ├── Pin Code btn  → wpsPinCodeBtn
 *            │     │     │     │     ├── PBC btn       → wpsPbcBtn       ← default selected
 *            │     │     │     │     └── Start WPS btn → wpsStartBtn     (dummy — no action)
 *            │     │     │     ├── WPS Progress TMP (display only)  → wpsProgressTMP
 *            │     │     │     ├── Reset to OOB btn   → wpsResetOobBtn  (dummy — no action)
 *            │     │     │     ├── SSID input          → ssidField
 *            │     │     │     └── Authentication Type → authTypeDropdown
 *            │     │     │           options (index):  0=Open / 1=Shared / 2=WPA-PSK /
 *            │     │     │                             3=WPA2-PSK / 4=WPA-PSK/WPA2-PSK
 *            │     │     │           default: 3 (WPA2-PSK)
 *            │     │     └── WPA2-PSK panel
 *            │     │           ├── Encryption dropdown → encryptionDropdown
 *            │     │           │     options (index):  0=TKIP / 1=AES / 2=TKIP+AES
 *            │     │           │     default: 1 (AES)
 *            │     │           └── Pre-Shared Key input → preSharedKeyField
 *            │     │
 *            │     └── InterfaceSetup Tab - Lan  → lanPanel  ← inactive by default
 *            │           ├── WPS Settings panel
 *            │           │     ├── IP Address input      → lanIPField
 *            │           │     │     default: "192.168.1.1"
 *            │           │     ├── IP Subnet Mask input  → lanSubnetMaskField
 *            │           │     │     default: "255.255.255.0"
 *            │           │     ├── Dynamic Router dropdown → dynamicRouterDropdown
 *            │           │     │     options (index): 0=None / 1=RIP1 / 2=RIP2-B / 3=RIP2-M
 *            │           │     │     default: 0 (None)
 *            │           │     ├── Direction dropdown    → directionDropdown
 *            │           │     │     options (index): 0=None / 1=Both In / 2=Both Out / 3=Both
 *            │           │     │     default: 0 (None); non-interactable when Dynamic Router = None
 *            │           │     ├── Multicast dropdown    → multicastDropdown
 *            │           │     │     options (index): 0=Disabled / 1=IGMP-v1 / 2=IGMP-v2 / 3=IGMP-v3
 *            │           │     │     default: 0 (Disabled)
 *            │           │     ├── IGMP Snoop buttons
 *            │           │     │     ├── Disable btn → igmpSnoopDisableBtn  ← default selected
 *            │           │     │     └── Enable btn  → igmpSnoopEnableBtn
 *            │           │     └── MLD Snoop buttons
 *            │           │           ├── Disable btn → mldSnoopDisableBtn   ← default selected
 *            │           │           └── Enable btn  → mldSnoopEnableBtn
 *            │           └── DHCP panel
 *            │                 └── DHCP buttons
 *            │                       ├── Disable btn → dhcpDisableBtn
 *            │                       ├── Enable btn  → dhcpEnableBtn   ← default selected
 *            │                       └── Relay btn   → dhcpRelayBtn
 *            │
 *            └── Save&Cancel Panel
 *                  ├── Save Button   → saveBtn
 *                  ├── Cancel Button → cancelBtn
 *                  └── PopUp - Save TP Link Configuration → savePopUp  (inactive by default)
 *                        ├── OK Button     → popUpOkBtn
 *                        └── Cancel Button → popUpCancelBtn
 *
 *  DROPDOWN OPTION ORDER
 *    Set the dropdown options in Unity in the exact order listed above.
 *    The indices in this script must match that order.
 *
 *  BUTTON OnClick — auto-wired in Awake. Do NOT wire any buttons manually in Inspector.
 *
 *  BEHAVIOR SUMMARY
 *    Highlight rule     selected = #B2D9FF, unselected = white; 1 per group at a time
 *    Non-functional btns  disabled (not inactive) — visible, not clickable, no highlight
 *    Dynamic Router       when set to None, Direction becomes non-interactable and resets to None
 *    Sub tab switch       WITHOUT saving → resets the panel being LEFT to last saved state
 *    Save                 commits full config (LAN + Wireless); popup fires if LAN IP changed
 *    Cancel               resets current active panel to last saved state
 *    Popup OK / Cancel    both just close the popup (notification only)
 *    First open           all fields show built-in defaults (no Save needed first)
 *    WPS State / Progress display-only — set once at init, not part of save/restore
 *    Start WPS / ResetOOB visual dummy buttons — no action
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TPLinkTabManager : MonoBehaviour
{
    private enum MainTab { InterfaceSetup, Status }
    private enum SubTab  { Wireless, LAN }

    // Full saved TP-Link configuration for both panels
    private struct TPLinkConfig
    {
        // LAN — WPS Settings
        public string LanIP;
        public string LanSubnetMask;
        public int    DynamicRouter;     // 0=None 1=RIP1 2=RIP2-B 3=RIP2-M
        public int    Direction;         // 0=None 1=Both In 2=Both Out 3=Both
        public int    Multicast;         // 0=Disabled 1=IGMP-v1 2=IGMP-v2 3=IGMP-v3
        public bool   IgmpSnoopEnabled;  // false=Disable true=Enable
        public bool   MldSnoopEnabled;   // false=Disable true=Enable

        // LAN — DHCP
        public int    DhcpMode;          // 0=Disable 1=Enable 2=Relay

        // Wireless — WPS Settings
        public int    WpsModeIndex;      // 0=Pin Code 1=PBC
        public string Ssid;
        public int    AuthType;          // 0=Open 1=Shared 2=WPA-PSK 3=WPA2-PSK 4=WPA-PSK/WPA2-PSK

        // Wireless — WPA2-PSK
        public int    Encryption;        // 0=TKIP 1=AES 2=TKIP+AES
        public string PreSharedKey;
    }

    // ----------------------------------------------------------------
    //  Inspector fields
    // ----------------------------------------------------------------

    [Header("Main Tab — Functional Buttons")]
    [SerializeField] private Button interfaceSetupBtn;
    [SerializeField] private Button statusBtn;

    [Header("Main Tab — Non-Functional Buttons (disabled, not inactive)")]
    [SerializeField] private Button[] disabledMainTabBtns;

    [Header("Sub Tab Panels")]
    [SerializeField] private GameObject interfaceSetupSubTab;
    [SerializeField] private GameObject statusSubTab;

    [Header("Interface Setup Sub Tab — Functional Buttons")]
    [SerializeField] private Button wirelessBtn;
    [SerializeField] private Button lanBtn;

    [Header("Interface Setup Sub Tab — Non-Functional Buttons (disabled, not inactive)")]
    [SerializeField] private Button[] disabledSubTabBtns;

    [Header("Body Panels")]
    [SerializeField] private GameObject wirelessPanel;
    [SerializeField] private GameObject lanPanel;

    [Header("LAN — WPS Settings Fields")]
    [SerializeField] private TMP_InputField lanIPField;
    [SerializeField] private TMP_InputField lanSubnetMaskField;
    [SerializeField] private TMP_Dropdown   dynamicRouterDropdown;
    [SerializeField] private TMP_Dropdown   directionDropdown;
    [SerializeField] private TMP_Dropdown   multicastDropdown;
    [SerializeField] private Button         igmpSnoopDisableBtn;
    [SerializeField] private Button         igmpSnoopEnableBtn;
    [SerializeField] private Button         mldSnoopDisableBtn;
    [SerializeField] private Button         mldSnoopEnableBtn;

    [Header("LAN — DHCP Fields")]
    [SerializeField] private Button dhcpDisableBtn;
    [SerializeField] private Button dhcpEnableBtn;
    [SerializeField] private Button dhcpRelayBtn;

    [Header("Wireless — WPS Settings Fields")]
    [Tooltip("Display only — set at init, not part of save/restore.")]
    [SerializeField] private TMP_Text       wpsStateTMP;
    [SerializeField] private Button         wpsPinCodeBtn;
    [SerializeField] private Button         wpsPbcBtn;
    [Tooltip("Dummy button — no action.")]
    [SerializeField] private Button         wpsStartBtn;
    [Tooltip("Display only — set at init, not part of save/restore.")]
    [SerializeField] private TMP_Text       wpsProgressTMP;
    [Tooltip("Dummy button — no action.")]
    [SerializeField] private Button         wpsResetOobBtn;
    [SerializeField] private TMP_InputField ssidField;
    [SerializeField] private TMP_Dropdown   authTypeDropdown;

    [Header("Wireless — WPA2-PSK Fields")]
    [SerializeField] private TMP_Dropdown   encryptionDropdown;
    [SerializeField] private TMP_InputField preSharedKeyField;

    [Header("Save & Cancel")]
    [SerializeField] private Button     saveBtn;
    [SerializeField] private Button     cancelBtn;
    [SerializeField] private GameObject savePopUp;
    [SerializeField] private Button     popUpOkBtn;
    [SerializeField] private Button     popUpCancelBtn;

    [Header("Colors")]
    [SerializeField] private Color selectedColor   = new Color(0.698f, 0.851f, 1f); // #B2D9FF
    [SerializeField] private Color unselectedColor = Color.white;

    [Header("Configurable Defaults")]
    [SerializeField] private string defaultSsid         = "TP-LINK_WiFi";
    [SerializeField] private string defaultPreSharedKey = "12345678";

    [Header("Configuration Validation")]
    [Tooltip("LAN IP must start with this prefix and end with a valid octet (1–254) to count as configured.")]
    [SerializeField] private string validConfiguredIPPrefix = "";

    [Header("WiFi Config — Target Controllers")]
    [SerializeField] private ChromePanelManager         chromePanelManager;
    [SerializeField] private RouterResetController      routerReset;
    [SerializeField] private AccessPointResetController apReset;

    // ----------------------------------------------------------------
    //  Runtime state
    // ----------------------------------------------------------------

    private MainTab _activeMainTab = MainTab.InterfaceSetup;
    private SubTab  _activeSubTab  = SubTab.Wireless;

    // Live toggle states — read on Save, written on Restore
    private bool _igmpSnoopEnabled;
    private bool _mldSnoopEnabled;
    private int  _dhcpMode;       // 0=Disable 1=Enable 2=Relay
    private int  _wpsModeIndex;   // 0=PinCode 1=PBC

    // One config per physical device — independent, never shared between Router and AP.
    private TPLinkConfig _routerConfig;
    private TPLinkConfig _apConfig;
    private bool         _routerHasSavedOnce;
    private bool         _apHasSavedOnce;

    // True after the first OnEnable (first-ever login activates this panel)
    private bool _initialized;

    // ----------------------------------------------------------------
    //  Target-aware config helpers
    // ----------------------------------------------------------------

    private ChromePanelManager.WifiTarget GetCurrentTarget()
        => chromePanelManager != null ? chromePanelManager.CurrentTarget : ChromePanelManager.WifiTarget.Router;

    private TPLinkConfig GetConfigForTarget(ChromePanelManager.WifiTarget target)
        => target == ChromePanelManager.WifiTarget.AccessPoint ? _apConfig : _routerConfig;

    private void SetConfigForTarget(ChromePanelManager.WifiTarget target, TPLinkConfig config)
    {
        if (target == ChromePanelManager.WifiTarget.AccessPoint) _apConfig    = config;
        else                                                      _routerConfig = config;
    }

    private bool HasSavedForTarget(ChromePanelManager.WifiTarget target)
        => target == ChromePanelManager.WifiTarget.AccessPoint ? _apHasSavedOnce : _routerHasSavedOnce;

    private void SetHasSavedForTarget(ChromePanelManager.WifiTarget target)
    {
        if (target == ChromePanelManager.WifiTarget.AccessPoint) _apHasSavedOnce    = true;
        else                                                      _routerHasSavedOnce = true;
    }

    private TPLinkConfig GetCurrentTargetConfig() => GetConfigForTarget(GetCurrentTarget());

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        // Non-functional buttons — visible but not clickable
        foreach (var btn in disabledMainTabBtns)
            if (btn != null) btn.interactable = false;
        foreach (var btn in disabledSubTabBtns)
            if (btn != null) btn.interactable = false;

        // Main tab
        if (interfaceSetupBtn != null) interfaceSetupBtn.onClick.AddListener(() => SelectMainTab(MainTab.InterfaceSetup));
        if (statusBtn         != null) statusBtn.onClick.AddListener(() => SelectMainTab(MainTab.Status));

        // Sub tab
        if (wirelessBtn != null) wirelessBtn.onClick.AddListener(() => SelectSubTab(SubTab.Wireless));
        if (lanBtn      != null) lanBtn.onClick.AddListener(() => SelectSubTab(SubTab.LAN));

        // LAN toggles
        if (igmpSnoopDisableBtn != null) igmpSnoopDisableBtn.onClick.AddListener(() => SetIgmpSnoop(false));
        if (igmpSnoopEnableBtn  != null) igmpSnoopEnableBtn.onClick.AddListener(() => SetIgmpSnoop(true));
        if (mldSnoopDisableBtn  != null) mldSnoopDisableBtn.onClick.AddListener(() => SetMldSnoop(false));
        if (mldSnoopEnableBtn   != null) mldSnoopEnableBtn.onClick.AddListener(() => SetMldSnoop(true));
        if (dhcpDisableBtn      != null) dhcpDisableBtn.onClick.AddListener(() => SetDhcpMode(0));
        if (dhcpEnableBtn       != null) dhcpEnableBtn.onClick.AddListener(() => SetDhcpMode(1));
        if (dhcpRelayBtn        != null) dhcpRelayBtn.onClick.AddListener(() => SetDhcpMode(2));

        // Direction interactability driven by Dynamic Router selection
        if (dynamicRouterDropdown != null)
            dynamicRouterDropdown.onValueChanged.AddListener(OnDynamicRouterChanged);

        // Wireless toggles
        if (wpsPinCodeBtn != null) wpsPinCodeBtn.onClick.AddListener(() => SetWpsMode(0));
        if (wpsPbcBtn     != null) wpsPbcBtn.onClick.AddListener(() => SetWpsMode(1));
        // wpsStartBtn and wpsResetOobBtn are visual dummies — no listener

        // Save / Cancel / Popup
        if (saveBtn        != null) saveBtn.onClick.AddListener(OnSave);
        if (cancelBtn      != null) cancelBtn.onClick.AddListener(OnCancel);
        if (popUpOkBtn     != null) popUpOkBtn.onClick.AddListener(ClosePopUp);
        if (popUpCancelBtn != null) popUpCancelBtn.onClick.AddListener(ClosePopUp);

        if (savePopUp != null) savePopUp.SetActive(false);
    }

    // ----------------------------------------------------------------
    //  Lifecycle — OnEnable
    // ----------------------------------------------------------------

    private void OnEnable()
    {
        if (!_initialized)
        {
            _routerConfig        = BuildDefaultConfig();
            _apConfig            = BuildDefaultConfig();
            _routerHasSavedOnce  = false;
            _apHasSavedOnce      = false;
            _initialized         = true;

            _activeMainTab = MainTab.InterfaceSetup;
            _activeSubTab  = SubTab.Wireless;

            if (wpsStateTMP    != null) wpsStateTMP.text    = "Unconfigured";
            if (wpsProgressTMP != null) wpsProgressTMP.text = "Idle";

            TPLinkConfig cfg = GetCurrentTargetConfig();
            ApplyLanStateToUI(cfg);
            ApplyWirelessStateToUI(cfg);

            RefreshMainTabVisuals();
            RefreshSubTabPanel();
            RefreshSubTabVisuals();
            RefreshBodyPanel();
            return;
        }

        // Always apply current target's config — handles login-redirect and re-open cases.
        TPLinkConfig activeCfg = GetCurrentTargetConfig();
        ApplyLanStateToUI(activeCfg);
        ApplyWirelessStateToUI(activeCfg);

        RefreshMainTabVisuals();
        RefreshSubTabPanel();
        RefreshSubTabVisuals();
        RefreshBodyPanel();
    }

    // ----------------------------------------------------------------
    //  State load/save (called by VirtualOSManager on device switch)
    // ----------------------------------------------------------------

    public void LoadState(DeviceOSState state)
    {
        _activeMainTab = (MainTab)state.TPLinkActiveMainTab;
        _activeSubTab  = (SubTab)state.TPLinkActiveSubTab;

        // chromePanelManager.LoadState ran first, so CurrentTarget is already correct
        // for the incoming device. Apply the matching config to the UI.
        // SetActive on inactive children is valid — renders correctly on next activation.
        if (_initialized)
        {
            TPLinkConfig cfg = GetCurrentTargetConfig();
            ApplyLanStateToUI(cfg);
            ApplyWirelessStateToUI(cfg);
            RefreshMainTabVisuals();
            RefreshSubTabPanel();
            RefreshSubTabVisuals();
            RefreshBodyPanel();
        }
        // If not yet initialized, OnEnable handles full init on first login.
    }

    public void SaveState(DeviceOSState state)
    {
        state.TPLinkActiveMainTab = (int)_activeMainTab;
        state.TPLinkActiveSubTab  = (int)_activeSubTab;
        // _savedConfig (router values) is intentionally NOT saved here —
        // it represents the physical router and is shared across all devices.
    }

    // ----------------------------------------------------------------
    //  Initialization
    // ----------------------------------------------------------------

    private TPLinkConfig BuildDefaultConfig() => new TPLinkConfig
    {
        LanIP            = "192.168.1.1",
        LanSubnetMask    = "255.255.255.0",
        DynamicRouter    = 0,     // None
        Direction        = 0,     // None
        Multicast        = 0,     // Disabled
        IgmpSnoopEnabled = false, // Disable
        MldSnoopEnabled  = false, // Disable
        DhcpMode         = 1,     // Enable
        WpsModeIndex     = 1,     // PBC
        Ssid             = defaultSsid,
        AuthType         = 3,     // WPA2-PSK
        Encryption       = 1,     // AES
        PreSharedKey     = defaultPreSharedKey,
    };

    // ----------------------------------------------------------------
    //  Main Tab
    // ----------------------------------------------------------------

    private void SelectMainTab(MainTab tab)
    {
        if (tab == _activeMainTab) return;
        _activeMainTab = tab;
        RefreshMainTabVisuals();
        RefreshSubTabPanel();
        Debug.Log($"[TPLinkTabManager] Main tab → {tab}.");
    }

    private void RefreshMainTabVisuals()
    {
        SetButtonColor(interfaceSetupBtn, _activeMainTab == MainTab.InterfaceSetup ? selectedColor : unselectedColor);
        SetButtonColor(statusBtn,         _activeMainTab == MainTab.Status          ? selectedColor : unselectedColor);
    }

    private void RefreshSubTabPanel()
    {
        bool isInterface = _activeMainTab == MainTab.InterfaceSetup;
        if (interfaceSetupSubTab != null) interfaceSetupSubTab.SetActive(isInterface);
        if (statusSubTab         != null) statusSubTab.SetActive(!isInterface);
    }

    // ----------------------------------------------------------------
    //  Sub Tab
    // ----------------------------------------------------------------

    private void SelectSubTab(SubTab tab)
    {
        if (tab == _activeSubTab) return;

        // Reset the panel being LEFT to its last saved (or default) state
        RestoreActiveSubTabToSavedState();

        _activeSubTab = tab;
        RefreshSubTabVisuals();
        RefreshBodyPanel();
        Debug.Log($"[TPLinkTabManager] Sub tab → {tab}.");
    }

    private void RefreshSubTabVisuals()
    {
        SetButtonColor(wirelessBtn, _activeSubTab == SubTab.Wireless ? selectedColor : unselectedColor);
        SetButtonColor(lanBtn,      _activeSubTab == SubTab.LAN      ? selectedColor : unselectedColor);
    }

    private void RefreshBodyPanel()
    {
        bool showWireless = _activeSubTab == SubTab.Wireless;
        if (wirelessPanel != null) wirelessPanel.SetActive(showWireless);
        if (lanPanel      != null) lanPanel.SetActive(!showWireless);
    }

    // ----------------------------------------------------------------
    //  LAN — Toggle groups
    // ----------------------------------------------------------------

    private void SetIgmpSnoop(bool enabled)
    {
        _igmpSnoopEnabled = enabled;
        SetButtonColor(igmpSnoopDisableBtn, !enabled ? selectedColor : unselectedColor);
        SetButtonColor(igmpSnoopEnableBtn,   enabled ? selectedColor : unselectedColor);
    }

    private void SetMldSnoop(bool enabled)
    {
        _mldSnoopEnabled = enabled;
        SetButtonColor(mldSnoopDisableBtn, !enabled ? selectedColor : unselectedColor);
        SetButtonColor(mldSnoopEnableBtn,   enabled ? selectedColor : unselectedColor);
    }

    private void SetDhcpMode(int mode)
    {
        _dhcpMode = mode;
        SetButtonColor(dhcpDisableBtn, mode == 0 ? selectedColor : unselectedColor);
        SetButtonColor(dhcpEnableBtn,  mode == 1 ? selectedColor : unselectedColor);
        SetButtonColor(dhcpRelayBtn,   mode == 2 ? selectedColor : unselectedColor);
    }

    // ----------------------------------------------------------------
    //  LAN — Dynamic Router → Direction dependency
    // ----------------------------------------------------------------

    // Called by onValueChanged listener when user changes the dropdown interactively.
    private void OnDynamicRouterChanged(int value)
    {
        if (directionDropdown == null) return;
        bool hasRoute = value != 0;
        directionDropdown.interactable = hasRoute;
        if (!hasRoute) directionDropdown.value = 0;
    }

    // ----------------------------------------------------------------
    //  Wireless — Toggle groups
    // ----------------------------------------------------------------

    private void SetWpsMode(int mode)
    {
        _wpsModeIndex = mode;
        SetButtonColor(wpsPinCodeBtn, mode == 0 ? selectedColor : unselectedColor);
        SetButtonColor(wpsPbcBtn,     mode == 1 ? selectedColor : unselectedColor);
    }

    // ----------------------------------------------------------------
    //  Save
    // ----------------------------------------------------------------

    private void OnSave()
    {
        ChromePanelManager.WifiTarget target = GetCurrentTarget();
        string previousLanIP = GetConfigForTarget(target).LanIP;
        CommitCurrentState();
        string savedLanIP  = GetConfigForTarget(target).LanIP;
        bool   lanIPChanged = savedLanIP != previousLanIP;

        if (!string.IsNullOrEmpty(validConfiguredIPPrefix)
            && savedLanIP.StartsWith(validConfiguredIPPrefix))
        {
            string lastOctet = savedLanIP.Substring(validConfiguredIPPrefix.Length);
            if (int.TryParse(lastOctet, out int octetVal) && octetVal >= 1 && octetVal <= 254)
            {
                if (target == ChromePanelManager.WifiTarget.Router)
                    routerReset?.SetConfigured();
                else if (target == ChromePanelManager.WifiTarget.AccessPoint)
                    apReset?.SetConfigured();
            }
        }

        if (lanIPChanged)
        {
            if (savePopUp != null) savePopUp.SetActive(true);
            Debug.Log($"[TPLinkTabManager] {target} saved — LAN IP changed, popup shown.");
        }
        else
        {
            Debug.Log($"[TPLinkTabManager] {target} saved.");
        }
    }

    private void CommitCurrentState()
    {
        ChromePanelManager.WifiTarget target = GetCurrentTarget();
        TPLinkConfig config = GetConfigForTarget(target);

        if (lanIPField            != null) config.LanIP         = lanIPField.text;
        if (lanSubnetMaskField    != null) config.LanSubnetMask = lanSubnetMaskField.text;
        if (dynamicRouterDropdown != null) config.DynamicRouter = dynamicRouterDropdown.value;
        if (directionDropdown     != null) config.Direction     = directionDropdown.value;
        if (multicastDropdown     != null) config.Multicast     = multicastDropdown.value;
        config.IgmpSnoopEnabled = _igmpSnoopEnabled;
        config.MldSnoopEnabled  = _mldSnoopEnabled;
        config.DhcpMode         = _dhcpMode;

        config.WpsModeIndex = _wpsModeIndex;
        if (ssidField          != null) config.Ssid         = ssidField.text;
        if (authTypeDropdown   != null) config.AuthType     = authTypeDropdown.value;
        if (encryptionDropdown != null) config.Encryption   = encryptionDropdown.value;
        if (preSharedKeyField  != null) config.PreSharedKey = preSharedKeyField.text;

        SetConfigForTarget(target, config);
        SetHasSavedForTarget(target);

        Debug.Log($"[TPLinkTabManager] Config committed to {target} state.");
    }

    // ----------------------------------------------------------------
    //  Cancel / Restore
    // ----------------------------------------------------------------

    private void OnCancel()
    {
        RestoreActiveSubTabToSavedState();
        Debug.Log("[TPLinkTabManager] Cancel — active tab restored to last saved state.");
    }

    // Restores only the currently active sub tab panel.
    // Called by Cancel and by sub tab switches (resets the tab being left).
    private void RestoreActiveSubTabToSavedState()
    {
        ChromePanelManager.WifiTarget target = GetCurrentTarget();
        TPLinkConfig source = HasSavedForTarget(target) ? GetConfigForTarget(target) : BuildDefaultConfig();

        if (_activeSubTab == SubTab.LAN)
            ApplyLanStateToUI(source);
        else
            ApplyWirelessStateToUI(source);
    }

    // Called by ChromePanelManager after successful navigation — updates the UI to show
    // the correct config when the user switches between Router and AP while TP-Link is
    // already open (same-page navigation is a no-op so OnEnable doesn't fire).
    public void RefreshForCurrentTarget()
    {
        if (!_initialized) return;
        TPLinkConfig cfg = GetCurrentTargetConfig();
        ApplyLanStateToUI(cfg);
        ApplyWirelessStateToUI(cfg);
    }

    // ----------------------------------------------------------------
    //  Apply config to UI
    // ----------------------------------------------------------------

    private void ApplyLanStateToUI(TPLinkConfig cfg)
    {
        if (lanIPField         != null) lanIPField.text         = cfg.LanIP;
        if (lanSubnetMaskField != null) lanSubnetMaskField.text = cfg.LanSubnetMask;

        if (dynamicRouterDropdown != null) dynamicRouterDropdown.value = cfg.DynamicRouter;

        // Explicitly set Direction interactability — onValueChanged may not fire if value unchanged
        if (directionDropdown != null)
        {
            directionDropdown.interactable = cfg.DynamicRouter != 0;
            directionDropdown.value        = cfg.Direction;
        }

        if (multicastDropdown != null) multicastDropdown.value = cfg.Multicast;

        SetIgmpSnoop(cfg.IgmpSnoopEnabled);
        SetMldSnoop(cfg.MldSnoopEnabled);
        SetDhcpMode(cfg.DhcpMode);
    }

    private void ApplyWirelessStateToUI(TPLinkConfig cfg)
    {
        SetWpsMode(cfg.WpsModeIndex);

        if (ssidField          != null) ssidField.text          = cfg.Ssid;
        if (authTypeDropdown   != null) authTypeDropdown.value  = cfg.AuthType;
        if (encryptionDropdown != null) encryptionDropdown.value = cfg.Encryption;
        if (preSharedKeyField  != null) preSharedKeyField.text  = cfg.PreSharedKey;
    }

    // ----------------------------------------------------------------
    //  PopUp
    // ----------------------------------------------------------------

    private void ClosePopUp()
    {
        if (savePopUp != null) savePopUp.SetActive(false);
    }

    // ----------------------------------------------------------------
    //  Helpers
    // ----------------------------------------------------------------

    private void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = color;
    }
}
