using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Logical network cable connecting two NetworkDevicePorts via a LineRenderer.
///
/// Lifecycle:
///   1. Instantiated by NetworkLogicalCableHolder when icon is dropped near a device.
///   2. PendingSecondEnd — line tracks deviceA→cursor. Left-click near another device
///      to connect. Left-click on deviceA OR right-click → cancel (destroy).
///      If the cable is re-routing (DetachEnd was called), right-click or clicking
///      deviceA instead restores the cable to its previous connection.
///   3. Connected — both ends track their device anchors every frame.
///      Double-click either device anchor → NetworkCablePopupManager shows a list of
///      all cables at that device with [Move End] and [Remove Cable] per cable.
///      [Move End] calls DetachEnd, returning this cable to PendingSecondEnd so the
///      free end can be re-routed to a different device.
///
/// Prefab setup:
///   Root: LineRenderer + this script
///   Children: EndpointPlugA, EndpointPlugB (each needs a CircleCollider2D)
/// </summary>
public class NetworkLogicalCable : MonoBehaviour
{
    [Header("Line")]
    [SerializeField] private float lineWidth = 0.08f;
    [SerializeField] private Color lineColor = Color.yellow;

    [Header("Connection")]
    [Tooltip("World-unit radius for second-end device detection on click.")]
    [SerializeField] private float secondEndSnapRadius = 2f;

    [Header("Hold to Disconnect")]
    [SerializeField] private float holdDuration = 1f;

    [Header("Endpoint Plugs")]
    [Tooltip("Child GameObject at deviceA end — needs a CircleCollider2D for hold detection.")]
    [SerializeField] private GameObject endpointPlugA;
    [Tooltip("Child GameObject at deviceB end — needs a CircleCollider2D for hold detection.")]
    [SerializeField] private GameObject endpointPlugB;

    // ----------------------------------------------------------------
    //  State
    // ----------------------------------------------------------------

    private enum CableState { PendingSecondEnd, Connected }
    private CableState _state;

    private LineRenderer      _lr;
    private NetworkDevicePort _deviceA;
    private NetworkDevicePort _deviceB;

    /// <summary>
    /// Stores the port detached by DetachEnd so the cable can be restored to its
    /// original connection if the user cancels the re-route (right-click / click deviceA).
    /// Null on freshly deployed cables — cancel in that case destroys the cable.
    /// </summary>
    private NetworkDevicePort _restorePort;

    // Static: only one cable hold-to-disconnect at a time.
    private static NetworkLogicalCable _holdTarget;
    private float _holdTimer;

    // Blocks NetworkLogicalCableHolder from deploying another cable while one is pending.
    private static int _pendingCount = 0;
    public  static bool AnyPendingSecondEnd => _pendingCount > 0;

    // ----------------------------------------------------------------
    //  Init
    // ----------------------------------------------------------------

    private void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        if (_lr == null) _lr = gameObject.AddComponent<LineRenderer>();

        _lr.positionCount = 2;
        _lr.startWidth    = lineWidth;
        _lr.endWidth      = lineWidth;
        _lr.useWorldSpace = true;

        // Always create a fresh owned material so shader is always correct.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader != null) _lr.material = new Material(shader);

        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(lineColor, 0f), new GradientColorKey(lineColor, 1f) },
            new[] { new GradientAlphaKey(lineColor.a, 0f), new GradientAlphaKey(lineColor.a, 1f) }
        );
        _lr.colorGradient = gradient;

        _lr.enabled = false;
    }

    private void OnDestroy()
    {
        if (_state == CableState.PendingSecondEnd)
            _pendingCount = Mathf.Max(0, _pendingCount - 1);

        if (_holdTarget == this)
            _holdTarget = null;
    }

    // ----------------------------------------------------------------
    //  Public API — connection
    // ----------------------------------------------------------------

    public void AttachFirstEnd(NetworkDevicePort port)
    {
        _deviceA = port;
        port.AcceptCable(this);
        _state = CableState.PendingSecondEnd;
        _pendingCount++;
        _lr.enabled = true;
    }

    public void AttachSecondEnd(NetworkDevicePort port)
    {
        if (port == _deviceA)       return;
        if (!port.CanAcceptCable()) return;

        _deviceB     = port;
        _restorePort = null;    // clear any pending restore reference
        port.AcceptCable(this);
        _state = CableState.Connected;
        _pendingCount = Mathf.Max(0, _pendingCount - 1);

        // Notify both devices to register a Phase2 cable entry.
        _deviceA.GetComponent<NetworkDevicePhase2Manager>()?.RegisterCable(this, _deviceB);
        _deviceB.GetComponent<NetworkDevicePhase2Manager>()?.RegisterCable(this, _deviceA);

        ActivityLogManager.Log(
            $"Cable connected: {_deviceA.name} ↔ {_deviceB.name}",
            ActivityLogManager.EntryType.Install);
    }

    /// <summary>
    /// Detaches one end of a Connected cable, returning it to PendingSecondEnd.
    /// The detached end's port is stored in _restorePort so cancel restores the
    /// original connection rather than destroying the cable.
    ///
    /// Called by NetworkCablePopupManager when the player clicks [Move End].
    /// Phase2 must NOT be installed on either side (enforced by the popup).
    /// </summary>
    public void DetachEnd(NetworkDevicePort port)
    {
        if (_state != CableState.Connected)              return;
        if (port != _deviceA && port != _deviceB)        return;

        // Grab both Phase2 managers before any swap or null — order matters.
        var mA = _deviceA?.GetComponent<NetworkDevicePhase2Manager>();
        var mB = _deviceB?.GetComponent<NetworkDevicePhase2Manager>();
        mA?.UnregisterCable(this);
        mB?.UnregisterCable(this);

        _restorePort = port;
        port.DisconnectCable(this);

        if (port == _deviceA)
        {
            // Swap so _deviceA is always the remaining connected end.
            _deviceA = _deviceB;
            _deviceB = null;
        }
        else
        {
            _deviceB = null;
        }

        _state = CableState.PendingSecondEnd;
        _pendingCount++;

        ActivityLogManager.Log(
            $"Cable end detached from {port.name} — re-routing in progress.",
            ActivityLogManager.EntryType.Remove);
    }

    public void Disconnect()
    {
        if (_state == CableState.Connected)
        {
            var managerA = _deviceA?.GetComponent<NetworkDevicePhase2Manager>();
            var managerB = _deviceB?.GetComponent<NetworkDevicePhase2Manager>();

            bool aInstalled = managerA != null && managerA.IsPhase2InstalledFor(this);
            bool bInstalled = managerB != null && managerB.IsPhase2InstalledFor(this);

            if (aInstalled || bInstalled)
            {
                string sides = (aInstalled && bInstalled)
                    ? $"{_deviceA.name} and {_deviceB.name}"
                    : aInstalled ? _deviceA.name : _deviceB.name;
                ActivityLogManager.Log(
                    $"Cannot disconnect {_deviceA?.name} ↔ {_deviceB?.name} — remove the port cable on {sides} first.",
                    ActivityLogManager.EntryType.Warning);
                return;
            }

            managerA?.UnregisterCable(this);
            managerB?.UnregisterCable(this);
        }

        _deviceA?.DisconnectCable(this);
        _deviceB?.DisconnectCable(this);

        if (_deviceA != null && _deviceB != null)
            ActivityLogManager.Log(
                $"Cable disconnected: {_deviceA.name} ↔ {_deviceB.name}",
                ActivityLogManager.EntryType.Remove);

        Destroy(gameObject);
    }

    // ----------------------------------------------------------------
    //  Public API — queries used by NetworkCablePopupManager
    // ----------------------------------------------------------------

    /// <summary>Returns the port on the opposite end from the given port, or null.</summary>
    public NetworkDevicePort GetOtherPort(NetworkDevicePort from)
    {
        if (from == _deviceA) return _deviceB;
        if (from == _deviceB) return _deviceA;
        return null;
    }

    /// <summary>
    /// Returns a display-friendly name for the device on the opposite end from <paramref name="from"/>.
    /// Used by NetworkCablePopupManager to label each cable row.
    /// </summary>
    public string GetOtherPortDisplayName(NetworkDevicePort from)
    {
        NetworkDevicePort other = GetOtherPort(from);
        if (other == null) return "Unknown";
        var drag = other.GetComponentInParent<NetworkDragPrefab>();
        return drag != null ? drag.LogDisplayName : other.name;
    }

    /// <summary>
    /// True if the given port's own Phase2 is installed for this cable.
    /// Blocks [Move End] — you cannot pull your own end while your port cable is plugged in.
    /// The other device's Phase2 state is irrelevant for this check; if it is installed it
    /// will be destroyed as an accepted consequence of re-routing.
    /// </summary>
    public bool IsMoveEndBlocked(NetworkDevicePort fromPort)
    {
        if (_state != CableState.Connected) return false;
        var manager = fromPort?.GetComponent<NetworkDevicePhase2Manager>();
        return manager != null && manager.IsPhase2InstalledFor(this);
    }

    /// <summary>
    /// True if Phase2 is installed on EITHER end, blocking [Remove Cable].
    /// Full removal requires both port cables to be unplugged first.
    /// </summary>
    public bool IsRemoveCableBlocked()
    {
        if (_state != CableState.Connected) return false;
        var mA = _deviceA?.GetComponent<NetworkDevicePhase2Manager>();
        var mB = _deviceB?.GetComponent<NetworkDevicePhase2Manager>();
        return (mA != null && mA.IsPhase2InstalledFor(this)) ||
               (mB != null && mB.IsPhase2InstalledFor(this));
    }

    // ----------------------------------------------------------------
    //  Update
    // ----------------------------------------------------------------

    private void Update()
    {
        if (Mouse.current == null) return;

        // Always keep line and plug visibility in sync with editor state.
        bool editorOpen = GameManager.Instance != null && GameManager.Instance.IsEditorOpen;
        _lr.enabled = !editorOpen;
        SetPlugsVisible(!editorOpen);

        if (editorOpen || _deviceA == null) return;

        // Suppress input processing while the cable popup is open.
        if (NetworkCablePopupManager.IsOpen) return;

        if (_state == CableState.PendingSecondEnd)
            UpdatePendingSecondEnd();
        else
            UpdateConnected();
    }

    /// <summary>
    /// Plugs are only shown when Connected and the editor is closed.
    /// Guarding by state prevents re-showing plugs the frame after DetachEnd.
    /// </summary>
    private void SetPlugsVisible(bool on)
    {
        bool show = on && _state == CableState.Connected;
        if (endpointPlugA != null) endpointPlugA.SetActive(show);
        if (endpointPlugB != null) endpointPlugB.SetActive(show);
    }

    private void UpdatePendingSecondEnd()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 cursorWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 10f));
        cursorWorld.z = 0f;

        _lr.SetPosition(0, _deviceA.GetAnchorWorldPosition());
        _lr.SetPosition(1, cursorWorld);

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (_restorePort != null) RestoreEnd();
            else                      CancelPending();
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // A valid nearby port always takes priority over cancellation.
            NetworkDevicePort closest = FindClosestPort(cursorWorld, secondEndSnapRadius, exclude: _deviceA);
            if (closest != null)
            {
                AttachSecondEnd(closest);
                return;
            }

            // No valid port found — cancel or restore if the click landed near deviceA.
            if (Vector3.Distance(_deviceA.GetAnchorWorldPosition(), cursorWorld) < secondEndSnapRadius)
            {
                if (_restorePort != null) RestoreEnd();
                else                      CancelPending();
            }
        }
    }

    private void CancelPending()
    {
        _deviceA?.DisconnectCable(this);
        _deviceA = null;
        Destroy(gameObject);
    }

    /// <summary>
    /// Restores the cable to its original connection after a DetachEnd re-route is cancelled.
    /// Falls back to CancelPending if the restore port is no longer available.
    /// </summary>
    private void RestoreEnd()
    {
        if (_restorePort == null) { CancelPending(); return; }

        if (!_restorePort.gameObject.activeInHierarchy || !_restorePort.CanAcceptCable())
        {
            ActivityLogManager.Log(
                $"Cannot restore cable — {_restorePort.name} is unavailable. Cable removed.",
                ActivityLogManager.EntryType.Warning);
            _restorePort = null;
            CancelPending();
            return;
        }

        // AttachSecondEnd clears _restorePort and re-registers Phase2 managers.
        AttachSecondEnd(_restorePort);
    }

    private void UpdateConnected()
    {
        Vector3 anchorA = _deviceA.GetAnchorWorldPosition();
        Vector3 anchorB = _deviceB.GetAnchorWorldPosition();

        _lr.SetPosition(0, anchorA);
        _lr.SetPosition(1, anchorB);

        if (endpointPlugA != null) endpointPlugA.transform.position = anchorA;
        if (endpointPlugB != null) endpointPlugB.transform.position = anchorB;

        HandleHoldToDisconnect();
    }

    // ----------------------------------------------------------------
    //  Hold-to-disconnect (single-cable shortcut; popup handles multi-cable)
    // ----------------------------------------------------------------

    private void HandleHoldToDisconnect()
    {
        Mouse  mouse      = Mouse.current;
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());

        if (mouse.leftButton.wasPressedThisFrame && _holdTarget == null)
        {
            if (IsMouseOverPlug(endpointPlugA, mouseWorld) || IsMouseOverPlug(endpointPlugB, mouseWorld))
            {
                _holdTarget = this;
                _holdTimer  = 0f;
            }
        }

        if (_holdTarget == this)
        {
            if (mouse.leftButton.isPressed)
            {
                _holdTimer += Time.deltaTime;
                if (_holdTimer >= holdDuration)
                {
                    _holdTarget = null;
                    Disconnect();
                }
            }
            else
            {
                _holdTarget = null;
                _holdTimer  = 0f;
            }
        }
    }

    private static bool IsMouseOverPlug(GameObject plug, Vector2 mouseWorld)
    {
        if (plug == null || !plug.activeSelf) return false;
        Collider2D col = plug.GetComponent<Collider2D>();
        if (col != null && col.enabled) return col.OverlapPoint(mouseWorld);
        return Vector2.Distance(plug.transform.position, mouseWorld) < 0.35f;
    }

    // ----------------------------------------------------------------
    //  Port scanning
    // ----------------------------------------------------------------

    private static NetworkDevicePort FindClosestPort(Vector3 worldPos, float radius,
                                                     NetworkDevicePort exclude = null)
    {
        NetworkDevicePort[] ports = FindObjectsByType<NetworkDevicePort>(FindObjectsSortMode.None);
        NetworkDevicePort closest = null;
        float bestDist = radius;

        foreach (NetworkDevicePort port in ports)
        {
            if (!port.gameObject.activeInHierarchy) continue;
            if (port == exclude)                    continue;
            if (!port.CanAcceptCable())             continue;
            float dist = Vector3.Distance(port.GetAnchorWorldPosition(), worldPos);
            if (dist < bestDist) { bestDist = dist; closest = port; }
        }

        return closest;
    }
}
