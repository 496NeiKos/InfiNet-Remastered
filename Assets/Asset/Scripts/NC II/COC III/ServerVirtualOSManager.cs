/*
 * ================================================================
 *  UNITY SETUP GUIDE — ServerVirtualOSManager (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to a SEPARATE always-active empty GameObject (e.g. "Managers")
 *    in the COC III scene root. Do NOT place on the canvas itself.
 *
 *  HIERARCHY (COC III scene)
 *    Managers                          ← this script here
 *    Virtual OS Canvas                 ← virtualOSCanvas field
 *      └── Windows Desktop
 *            ├── Taskbar
 *            ├── Desktop Icons
 *            │     ├── SettingsIcon      (Button → SettingsAppController.Open)
 *            │     ├── NSCIcon           (Button → NetworkSharingCenter.gameObject.SetActive(true))
 *            │     ├── ServerManagerIcon (Button → ServerManagerController.Open)
 *            │     └── RDConnectionIcon  (Button → RDConnectionAppController.Open)
 *            └── App Panels (children, start INACTIVE)
 *                  ├── Settings Panel         → settingsApp
 *                  ├── Network & Sharing Ctr  → networkSharingCenter
 *                  └── Server Manager Panel   → serverManager
 *                  (Other panels referenced by their own controllers)
 *
 *  INSPECTOR ASSIGNMENTS
 *    virtualOSCanvas      → "Virtual OS Canvas" root GameObject
 *    networkSharingCenter → NetworkSharingCenterController component
 *    serverManager        → ServerManagerController component
 *
 *  HOW IT WORKS
 *    COC III has only 1 device (Server PC). ServerDeviceOSAccessPoint on the
 *    server prefab calls OpenForDevice(DeviceID.Server, detailView).
 *    The canvas shows — no device switching logic needed.
 *    ESC hides the canvas; reopening the device resumes where the player left off.
 *    Implements IVirtualOSManager so reused IPv4 and NSC controllers work.
 * ================================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ServerVirtualOSManager : MonoBehaviour, IVirtualOSManager
{
    public static ServerVirtualOSManager Instance { get; private set; }

    [Header("Canvas")]
    [SerializeField] private GameObject virtualOSCanvas;

    [Header("References")]
    [SerializeField] private NetworkSharingCenterController networkSharingCenter;
    [SerializeField] private ServerManagerController        serverManager;

    private ServerDeviceState _state;
    private GameObject        _activeDetailView;
    private bool              _isOpen;
    private bool              _wasJustHidden;

    // ── IVirtualOSManager ────────────────────────────────────────────────────

    public DeviceOSState CurrentState  => _state;
    public DeviceID      CurrentDevice => DeviceID.Server;

    public bool IsIPTopologySatisfied() => true; // no physical topology gate in COC III

    public string GetIPBlockReason() => "";

    public string GetNetworkPrefix()
    {
        if (_state == null || !_state.UseStaticIP) return "";
        if (string.IsNullOrEmpty(_state.IPOctets[0])) return "";
        return $"{_state.IPOctets[0]}.{_state.IPOctets[1]}.{_state.IPOctets[2]}";
    }

    public List<string> GetUsedHostOctets() => new List<string>(); // single device — no conflicts

    // ── Public accessors ──────────────────────────────────────────────────────

    public ServerDeviceState ServerState => _state;
    public bool IsOpen => _isOpen;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        VirtualOSManagerLocator.Current = this;

        _state = new ServerDeviceState { Device = DeviceID.Server };
        virtualOSCanvas?.SetActive(false);
    }

    private void OnDestroy()
    {
        if (VirtualOSManagerLocator.Current == (IVirtualOSManager)this)
            VirtualOSManagerLocator.Current = null;
    }

    private void Update()
    {
        if (!_isOpen) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by ServerDeviceOSAccessPoint when the player right-clicks
    /// the server device in the workspace.
    /// </summary>
    public void OpenForDevice(DeviceID device, GameObject detailView = null)
    {
        _wasJustHidden = false;
        _activeDetailView = detailView;

        if (_activeDetailView != null)
            _activeDetailView.SetActive(false);

        virtualOSCanvas?.SetActive(true);
        _isOpen = true;

        _state.ServerManagerOpened = _state.ServerManagerOpened; // preserve — no reset on reopen

        Debug.Log("[ServerVirtualOSManager] Opened.");
    }

    public void Close()
    {
        _wasJustHidden = true;
        virtualOSCanvas?.SetActive(false);
        _isOpen = false;

        if (_activeDetailView != null)
            _activeDetailView.SetActive(true);

        Debug.Log("[ServerVirtualOSManager] Canvas hidden. State preserved.");
    }
}
