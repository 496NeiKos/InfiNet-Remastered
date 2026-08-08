/*
 * ================================================================
 *  UNITY SETUP GUIDE — VirtualOSManager
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to the "Virtual Operating System" Canvas
 *    GameObject.
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
 *    Call OpenForDevice(DeviceType) from any hardware object's right-click
 *    interaction to open the Virtual OS canvas for that device. The canvas
 *    loads that device's saved DeviceOSState (or creates a fresh one).
 *    ESC closes the canvas and saves the current state back to the dictionary.
 *    WiFiBtn is only interactable when the current device is Laptop.
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
    [Tooltip("WiFiBtn GameObject — interactable only for Laptop.")]
    [SerializeField] private GameObject wifiBtnObject;
    [Tooltip("TMP_Text inside EthernetNamePanel that shows the adapter name.")]
    [SerializeField] private TMP_Text adapterNameTMP;

    // Per-device state dictionary
    private readonly Dictionary<DeviceType, DeviceOSState> _states =
        new Dictionary<DeviceType, DeviceOSState>();

    private DeviceType   _currentDevice;
    private DeviceOSState _currentState;
    private bool          _isOpen;

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Always seed a default state so CurrentState is never null during editor testing
        // (even before OpenForDevice is called).
        _currentDevice = DeviceType.Server;
        _states[_currentDevice] = new DeviceOSState { Device = _currentDevice };
        _currentState = _states[_currentDevice];

        virtualOSCanvas?.SetActive(false);
    }

    private void Start()
    {
        // Wire WiFiBtn → InternetPanel.Toggle (left-click, no manual Inspector wiring needed)
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
    //  Public API — call from hardware right-click interaction
    // ----------------------------------------------------------------

    public void OpenForDevice(DeviceType device)
    {
        _currentDevice = device;

        if (!_states.ContainsKey(device))
            _states[device] = new DeviceOSState { Device = device };

        _currentState = _states[device];
        _isOpen       = true;

        virtualOSCanvas?.SetActive(true);
        ApplyDeviceContext();
        networkSharingCenter?.LoadState(_currentState);

        Debug.Log($"[VirtualOSManager] Opened for {device}.");
    }

    public void Close()
    {
        networkSharingCenter?.SaveState(_currentState);
        virtualOSCanvas?.SetActive(false);
        _isOpen = false;
        Debug.Log($"[VirtualOSManager] Closed. State saved for {_currentDevice}.");
    }

    // ----------------------------------------------------------------
    //  Accessors used by child controllers
    // ----------------------------------------------------------------

    public DeviceOSState CurrentState  => _currentState;
    public DeviceType    CurrentDevice => _currentDevice;

    public DeviceOSState GetState(DeviceType device)
    {
        if (!_states.ContainsKey(device))
            _states[device] = new DeviceOSState { Device = device };
        return _states[device];
    }

    // Returns the network prefix (first 3 octets) set by whichever device
    // was configured first, or empty string if none configured yet.
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

    // Returns all host octets (4th) already committed by other devices.
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
        bool isLaptop = _currentDevice == DeviceType.Laptop;

        // WiFiBtn — interactable only for Laptop
        if (wifiBtnObject != null)
        {
            var btn = wifiBtnObject.GetComponent<Button>();
            if (btn != null) btn.interactable = isLaptop;
        }

        // Adapter name label
        if (adapterNameTMP != null)
            adapterNameTMP.text = isLaptop ? "Wireless Network Connection" : "Local Area Connection";
    }
}
