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
 *    front detail view. The canvas loads that device's saved DeviceOSState
 *    (or creates a fresh one). ESC closes the canvas and saves state back.
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
    [SerializeField] private InternetPanelController internetPanel;
    [Tooltip("WiFiBtn GameObject under Taskbar.")]
    [SerializeField] private GameObject wifiBtnObject;
    [Tooltip("TMP_Text inside EthernetNamePanel that shows the adapter name.")]
    [SerializeField] private TMP_Text adapterNameTMP;

    private readonly Dictionary<DeviceID, DeviceOSState> _states =
        new Dictionary<DeviceID, DeviceOSState>();

    private DeviceID      _currentDevice;
    private DeviceOSState _currentState;
    private bool          _isOpen;

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
        _currentDevice = device;

        if (!_states.ContainsKey(device))
            _states[device] = new DeviceOSState { Device = device };

        _currentState = _states[device];
        _isOpen       = true;

        _activeDetailView = detailView;
        if (_activeDetailView != null)
            _activeDetailView.SetActive(false);

        virtualOSCanvas?.SetActive(true);
        ApplyDeviceContext();
        networkSharingCenter?.LoadState(_currentState);

        Debug.Log($"[VirtualOSManager] Opened for {device}.");
    }

    public void Close()
    {
        networkSharingCenter?.SaveState(_currentState);
        networkSharingCenter?.ClosePanel(); // reset UI to clean Desktop before disabling
        virtualOSCanvas?.SetActive(false);
        _isOpen = false;

        if (_activeDetailView != null)
            _activeDetailView.SetActive(true);

        Debug.Log($"[VirtualOSManager] Closed. State saved for {_currentDevice}.");
    }

    // ----------------------------------------------------------------
    //  Accessors used by child controllers
    // ----------------------------------------------------------------

    public DeviceOSState CurrentState  => _currentState;
    public DeviceID      CurrentDevice => _currentDevice;

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
