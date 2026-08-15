/*
 * ================================================================
 *  UNITY SETUP GUIDE — VirtualOSManager
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to a SEPARATE always-active GameObject
 *    (e.g. an empty "Managers" object in the scene root).
 *    Do NOT place it on the canvas — a disabled canvas never
 *    runs Awake, so the singleton would never initialize.
 *
 *  HIERARCHY
 *    Virtual Operating System  (this script here)
 *      └── Windows Desktop
 *            ├── Taskbar
 *            │     └── WiFiBtn         → wifiBtnObject (the Button's GameObject)
 *            ├── Application
 *            └── Internet Panel        → (handled by InternetPanelController)
 *
 *  INSPECTOR ASSIGNMENTS
 *    virtualOSCanvas         → Virtual Operating System Canvas GameObject
 *    networkSharingCenter    → NetworkSharingCenterController on "Network And Sharing Center"
 *    wifiBtnObject           → WiFiBtn GameObject under Taskbar
 *    adapterNameTMP          → EthernetNamePanel's TMP_Text child (shows
 *                              "Local Area Connection" or "Wireless Network Connection")
 *
 *  HOW IT WORKS
 *    Call OpenForDevice(DeviceID) from DeviceOSAccessPoint on each device's
 *    front detail view. Switching to a different device saves the outgoing
 *    state and loads the incoming one. ESC just hides the canvas — all panel
 *    and nav state is preserved so the player can resume by reopening the
 *    same device. chromePanelManager and tpLinkTabManager also need Inspector
 *    assignment (Chrome GameObject and Page 2 - Main respectively).
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VirtualOSManager : MonoBehaviour
{
    public static VirtualOSManager Instance { get; private set; }

    [Header("Canvas")]
    [SerializeField] private GameObject virtualOSCanvas;

    [Header("References")]
    [SerializeField] private NetworkSharingCenterController networkSharingCenter;
    [SerializeField] private InternetPanelController        internetPanel;
    [SerializeField] private ChromePanelManager             chromePanelManager;
    [SerializeField] private TPLinkTabManager               tpLinkTabManager;
    [SerializeField] private TopologyConditionManager       topologyManager;
    [Tooltip("Used to check AP.IsConfigured for the Laptop IP config gate.")]
    [SerializeField] private AccessPointResetController     apResetController;
    [Tooltip("WiFiBtn GameObject under Taskbar.")]
    [SerializeField] private GameObject wifiBtnObject;
    [Tooltip("CmdBtn GameObject on Windows Desktop (sibling of WiFiBtn area).")]
    [SerializeField] private GameObject cmdBtnObject;
    [Tooltip("TMP_Text inside EthernetNamePanel that shows the adapter name.")]
    [SerializeField] private TMP_Text adapterNameTMP;

    [Header("CMD")]
    [SerializeField] private PingCmdManager pingCmdManager;

    private readonly Dictionary<DeviceID, DeviceOSState> _states =
        new Dictionary<DeviceID, DeviceOSState>();

    private DeviceID      _currentDevice;
    private DeviceOSState _currentState;
    private bool          _isOpen;
    // True after Close() (ESC) — lets OpenForDevice know to resume instead of reload
    private bool          _wasJustHidden;

    private GameObject _activeDetailView;

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _currentDevice = DeviceID.Computer1;
        _states[_currentDevice] = new DeviceOSState { Device = _currentDevice };
        _currentState = _states[_currentDevice];

        virtualOSCanvas?.SetActive(false);
    }

    private void Start()
    {
        if (wifiBtnObject != null && internetPanel != null)
        {
            var btn = wifiBtnObject.GetComponent<Button>();
            btn?.onClick.AddListener(internetPanel.Toggle);
        }

        if (cmdBtnObject != null && pingCmdManager != null)
        {
            var btn = cmdBtnObject.GetComponent<Button>();
            btn?.onClick.AddListener(pingCmdManager.Toggle);
        }
    }

    private void Update()
    {
        if (!_isOpen) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    // ----------------------------------------------------------------
    //  Public API
    // ----------------------------------------------------------------

    public void OpenForDevice(DeviceID device, GameObject detailView = null)
    {
        bool resumingSameDevice = _wasJustHidden && device == _currentDevice;
        _wasJustHidden = false;

        if (!resumingSameDevice)
        {
            // Save outgoing device state before switching
            networkSharingCenter?.SaveState(_currentState);
            chromePanelManager?.SaveState(_currentState);
            tpLinkTabManager?.SaveState(_currentState);
            chromePanelManager?.CloseForSwitch();
            pingCmdManager?.CloseForSwitch();

            // Restore the outgoing device's front panel
            if (_activeDetailView != null)
                _activeDetailView.SetActive(true);

            // Set up the incoming device
            _currentDevice = device;

            if (!_states.ContainsKey(device))
                _states[device] = new DeviceOSState { Device = device };

            _currentState = _states[device];

            ApplyDeviceContext();
            networkSharingCenter?.LoadState(_currentState);
            chromePanelManager?.LoadState(_currentState);
            tpLinkTabManager?.LoadState(_currentState);
        }

        _activeDetailView = detailView;
        if (_activeDetailView != null)
            _activeDetailView.SetActive(false);

        virtualOSCanvas?.SetActive(true);
        _isOpen = true;

        Debug.Log($"[VirtualOSManager] Opened for {device}{(resumingSameDevice ? " (resumed)" : "")}.");
    }

    // ESC — hides the canvas only. All panel and nav state is preserved so the
    // player can return to exactly where they left off by reopening the same device.
    public void Close()
    {
        _wasJustHidden = true;
        virtualOSCanvas?.SetActive(false);
        _isOpen = false;
        Debug.Log($"[VirtualOSManager] Canvas hidden. State preserved for {_currentDevice}.");
    }

    // ----------------------------------------------------------------
    //  Accessors used by child controllers
    // ----------------------------------------------------------------

    public DeviceOSState CurrentState  => _currentState;
    public DeviceID      CurrentDevice => _currentDevice;

    public bool IsWifiRouterTopologySatisfied() => topologyManager?.IsWifiRouterSatisfied(_currentDevice) ?? false;
    public bool IsWifiAPTopologySatisfied()     => topologyManager?.IsWifiAPSatisfied(_currentDevice)     ?? false;

    public bool IsIPTopologySatisfied()
    {
        if (!(topologyManager?.IsIPSatisfied(_currentDevice) ?? false)) return false;
        // Laptop connects wirelessly — the Access Point must also be configured (WiFi phase complete).
        if (_currentDevice == DeviceID.Laptop)
            return apResetController != null && apResetController.IsConfigured;
        return true;
    }

    /// <summary>Returns a human-readable reason why IP topology is blocked, or empty string if satisfied.</summary>
    public string GetIPBlockReason()
    {
        if (topologyManager == null)
            return "Topology manager is not assigned.";
        if (!topologyManager.IsIPSatisfied(_currentDevice))
            return "Network topology is not complete. Make sure all cable connections also have port cables installed in each device's detail view.";
        if (_currentDevice == DeviceID.Laptop)
        {
            if (apResetController == null)
                return "Access Point controller is not assigned.";
            if (!apResetController.IsConfigured)
                return "The Access Point must be configured first. Complete the WiFi setup for the Access Point (TP-Link configuration) before configuring the Laptop.";
        }
        return "";
    }

    public DeviceOSState GetState(DeviceID device)
    {
        if (!_states.ContainsKey(device))
            _states[device] = new DeviceOSState { Device = device };
        return _states[device];
    }

    public string GetNetworkPrefix()
    {
        foreach (DeviceOSState s in _states.Values)
        {
            if (!s.UseStaticIP) continue;
            if (string.IsNullOrEmpty(s.IPOctets[0])) continue;
            return $"{s.IPOctets[0]}.{s.IPOctets[1]}.{s.IPOctets[2]}";
        }
        return "";
    }

    public List<string> GetUsedHostOctets()
    {
        var used = new List<string>();
        foreach (var kvp in _states)
        {
            if (kvp.Key == _currentDevice) continue;
            if (kvp.Value.UseStaticIP && !string.IsNullOrEmpty(kvp.Value.IPOctets[3]))
                used.Add(kvp.Value.IPOctets[3]);
        }
        return used;
    }

    // ----------------------------------------------------------------
    //  Private helpers
    // ----------------------------------------------------------------

    private void ApplyDeviceContext()
    {
        bool isLaptop = _currentDevice == DeviceID.Laptop;

        if (adapterNameTMP != null)
            adapterNameTMP.text = isLaptop ? "Wireless Network Connection" : "Local Area Connection";
    }
}
