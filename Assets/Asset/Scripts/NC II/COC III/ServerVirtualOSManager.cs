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
 *      ├── Restart UI Panel            ← restartUI field
 *      ├── Login UI Panel              ← loginUI field
 *      └── Desktop Panel              ← desktopPanel field
 *            ├── Taskbar
 *            │     ├── File Explorer Button
 *            │     └── Restart Button → Button OnClick → ServerVirtualOSManager.TriggerRestart()
 *            ├── Desktop Icons
 *            │     ├── Settings Icon       → Button OnClick → SettingsAppController.Open()
 *            │     ├── NSC Icon            → Button OnClick → NSC.gameObject.SetActive(true)
 *            │     ├── Server Manager Icon → serverManagerIcon (HIDDEN on Client PC)
 *            │     │     Button OnClick → ServerManagerController.Open()
 *            │     └── RD Connection Icon  → rdConnectionIcon (HIDDEN on Server PC)
 *            │           Button OnClick → RDConnectionAppController.Open()
 *            └── App Panels (all start INACTIVE)
 *                  ├── Settings Panel
 *                  ├── System Properties Chain
 *                  ├── Network & Sharing Ctr
 *                  ├── Server Manager Panel
 *                  └── (all other tool panels)
 *
 *  INSPECTOR ASSIGNMENTS
 *    virtualOSCanvas       → "Virtual OS Canvas" root GameObject
 *    desktopPanel          → "Desktop Panel" inside the canvas
 *    restartUI             → RestartUIController on Restart UI Panel
 *    loginUI               → LoginUIController on Login UI Panel
 *    networkSharingCenter  → NetworkSharingCenterController component
 *    serverManager         → ServerManagerController component
 *    serverManagerIcon     → Server Manager desktop icon GameObject
 *    rdConnectionIcon      → RD Connection desktop icon GameObject
 *    clientDefaults        → Default ComputerName, LocalUserName, LocalUserPassword
 *                            for the client PC (editable in Inspector)
 *
 *  HOW IT WORKS
 *    Single canvas manages both Server PC and Client PC contexts.
 *    ActivePC enum tracks which context is currently displayed.
 *    First ever open of each PC → Restart UI → Login UI → Desktop.
 *    ESC hides the canvas and resumes exactly where left off (no re-login).
 *    Restart button / DCPromo / domain join → TriggerRestart() → Restart UI → Login UI.
 *    SwitchToPC() is called from LoginUIController when player selects a different PC.
 *    OnLoginSuccess() is called from LoginUIController after valid credentials.
 *    Desktop icons refresh on every login to match the active PC context.
 * ================================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ActivePC { Server, Client }

public class ServerVirtualOSManager : MonoBehaviour, IVirtualOSManager
{
    public static ServerVirtualOSManager Instance { get; private set; }

    // ── Canvas + Boot UI ─────────────────────────────────────────────────────

    [Header("Canvas")]
    [SerializeField] private GameObject virtualOSCanvas;
    [SerializeField] private GameObject desktopPanel;

    [Header("Boot UI")]
    [SerializeField] private RestartUIController restartUI;
    [SerializeField] private LoginUIController   loginUI;

    // ── Desktop Icon References ───────────────────────────────────────────────

    [Header("Desktop Icons (context-switched)")]
    [SerializeField] private GameObject serverManagerIcon;
    [SerializeField] private GameObject rdConnectionIcon;

    // ── App References ────────────────────────────────────────────────────────

    [Header("App References")]
    [SerializeField] private NetworkSharingCenterController networkSharingCenter;
    [SerializeField] private ServerManagerController        serverManager;

    // ── Client PC defaults (Inspector-editable) ───────────────────────────────

    [Header("Client PC Defaults")]
    [SerializeField] private ClientPCData clientDefaults = new ClientPCData();

    // ── Runtime ───────────────────────────────────────────────────────────────

    private ServerDeviceState _state;
    private ClientPCData      _clientState;
    private ActivePC          _currentPC   = ActivePC.Server;
    private GameObject        _activeDetailView;
    private bool              _isOpen;
    private bool              _wasJustHidden;

    // ── IVirtualOSManager ────────────────────────────────────────────────────

    public DeviceOSState CurrentState  => _state;
    public DeviceID      CurrentDevice => DeviceID.Server;

    public bool IsIPTopologySatisfied() => true;
    public string GetIPBlockReason()    => "";

    public string GetNetworkPrefix()
    {
        if (_state == null || !_state.UseStaticIP) return "";
        if (string.IsNullOrEmpty(_state.IPOctets[0])) return "";
        return $"{_state.IPOctets[0]}.{_state.IPOctets[1]}.{_state.IPOctets[2]}";
    }

    public List<string> GetUsedHostOctets() => new List<string>();

    // ── Public Accessors ──────────────────────────────────────────────────────

    public ServerDeviceState ServerState => _state;
    public ClientPCData      ClientState => _clientState;
    public ActivePC          CurrentPC   => _currentPC;
    public bool              IsOpen      => _isOpen;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        VirtualOSManagerLocator.Current = this;

        _state = new ServerDeviceState { Device = DeviceID.Server };

        // Copy inspector-set defaults into runtime client state
        _clientState = new ClientPCData
        {
            ComputerName      = clientDefaults.ComputerName,
            LocalUserName     = clientDefaults.LocalUserName,
            LocalUserPassword = clientDefaults.LocalUserPassword
        };

        virtualOSCanvas?.SetActive(false);
        desktopPanel?.SetActive(false);
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
    /// Called by ServerDeviceOSAccessPoint when player right-clicks the server device.
    /// </summary>
    public void OpenForDevice(DeviceID device, GameObject detailView = null)
    {
        _activeDetailView = detailView;
        if (_activeDetailView != null) _activeDetailView.SetActive(false);

        virtualOSCanvas?.SetActive(true);
        _isOpen = true;

        if (_wasJustHidden)
        {
            // ESC-hidden resume — restore canvas exactly as-is, no restart/login
            _wasJustHidden = false;
            Debug.Log("[ServerVirtualOSManager] Canvas resumed (ESC restore).");
            return;
        }

        // First ever open for the current PC context → boot sequence
        bool firstBoot = _currentPC == ActivePC.Server
            ? !_state.FirstBootDone
            : !_clientState.FirstBootDone;

        if (firstBoot)
        {
            MarkFirstBootDone();
            TriggerRestart();
        }
        else
        {
            ShowLoginUI();
        }
    }

    public void Close()
    {
        _wasJustHidden = true;
        virtualOSCanvas?.SetActive(false);
        _isOpen = false;

        if (_activeDetailView != null) _activeDetailView.SetActive(true);
        Debug.Log("[ServerVirtualOSManager] Canvas hidden. State preserved.");
    }

    /// <summary>
    /// Deactivates desktop, shows Restart UI, then Login UI.
    /// Safe to call from any child controller (DCPromo, domain join, taskbar button).
    /// </summary>
    public void TriggerRestart()
    {
        desktopPanel?.SetActive(false);
        restartUI?.Show(ShowLoginUI);
        ActivityLogManager.Log("System restarting...", ActivityLogManager.EntryType.Action);
    }

    /// <summary>
    /// Switches to a different PC context and triggers the full restart→login sequence.
    /// Called from LoginUIController when player selects a different PC.
    /// </summary>
    public void SwitchToPC(ActivePC pc)
    {
        _currentPC = pc;
        ActivityLogManager.Log(
            $"Switching to {(pc == ActivePC.Server ? "Server PC" : "Client PC")}",
            ActivityLogManager.EntryType.Action);
        TriggerRestart();
    }

    /// <summary>
    /// Called by LoginUIController after credentials are validated.
    /// Stores the logged-in username, shows the desktop, and refreshes icon states.
    /// </summary>
    public void OnLoginSuccess(string username)
    {
        if (_currentPC == ActivePC.Server)
            _state.CurrentLoggedInUser = username;
        else
            _clientState.CurrentLoggedInUser = username;

        desktopPanel?.SetActive(true);
        RefreshDesktopIcons();

        ActivityLogManager.Log(
            $"Desktop loaded — PC: {(_currentPC == ActivePC.Server ? "Server" : "Client")}, User: {username}",
            ActivityLogManager.EntryType.Action);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void ShowLoginUI()
    {
        loginUI?.Show(_currentPC);
    }

    private void MarkFirstBootDone()
    {
        if (_currentPC == ActivePC.Server)
            _state.FirstBootDone = true;
        else
            _clientState.FirstBootDone = true;
    }

    private void RefreshDesktopIcons()
    {
        bool isServer = _currentPC == ActivePC.Server;
        serverManagerIcon?.SetActive(isServer);
        rdConnectionIcon?.SetActive(!isServer);
    }
}
