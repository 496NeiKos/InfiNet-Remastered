using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Logical network cable connecting two NetworkDevicePorts via a LineRenderer.
///
/// Lifecycle:
///   1. Instantiated by NetworkLogicalCableHolder when the icon proxy is dropped near a device.
///   2. PendingSecondEnd — line tracks deviceA→cursor. Left-click near another device to connect.
///      Left-click on the first device again OR right-click → cancel and destroy.
///   3. Connected — both ends track their device anchors every frame (follows dragged devices).
///      Hold either endpoint plug for holdDuration seconds to disconnect and destroy.
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

    private LineRenderer _lr;
    private NetworkDevicePort _deviceA;
    private NetworkDevicePort _deviceB;

    // Static so only one cable disconnect can accumulate at a time.
    private static NetworkLogicalCable _holdTarget;
    private bool  _holdTargetIsA;
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

        // Always create a fresh owned Sprites/Default instance.
        // Never reference an Inspector material — too easy to assign a wrong shader.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader != null) _lr.material = new Material(shader);

        // colorGradient is the direct per-vertex color API — works regardless of shader.
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(lineColor, 0f), new GradientColorKey(lineColor, 1f) },
            new[] { new GradientAlphaKey(lineColor.a, 0f), new GradientAlphaKey(lineColor.a, 1f) }
        );
        _lr.colorGradient = gradient;

        _lr.enabled = false;

        if (endpointPlugA != null) endpointPlugA.SetActive(false);
        if (endpointPlugB != null) endpointPlugB.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_state == CableState.PendingSecondEnd)
            _pendingCount = Mathf.Max(0, _pendingCount - 1);

        if (_holdTarget == this)
            _holdTarget = null;
    }

    // ----------------------------------------------------------------
    //  Public API
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

        _deviceB = port;
        port.AcceptCable(this);
        _state = CableState.Connected;
        _pendingCount = Mathf.Max(0, _pendingCount - 1);

        if (endpointPlugA != null) endpointPlugA.SetActive(true);
        if (endpointPlugB != null) endpointPlugB.SetActive(true);

        // Notify both devices to register a Phase2 cable entry.
        _deviceA.GetComponent<NetworkDevicePhase2Manager>()?.RegisterCable(this, _deviceB);
        _deviceB.GetComponent<NetworkDevicePhase2Manager>()?.RegisterCable(this, _deviceA);

        ActivityLogManager.Log(
            $"Cable connected: {_deviceA.name} ↔ {_deviceB.name}",
            ActivityLogManager.EntryType.Install);
    }

    /// <summary>Returns the port on the opposite end from the given port, or null if unconnected.</summary>
    public NetworkDevicePort GetOtherPort(NetworkDevicePort from)
    {
        if (from == _deviceA) return _deviceB;
        if (from == _deviceB) return _deviceA;
        return null;
    }

    public void Disconnect()
    {
        // Block if Phase2 is still installed on either end.
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

            // Clean up any pending (non-installed) Phase2 entries on both devices.
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
    //  Update
    // ----------------------------------------------------------------

    private void Update()
    {
        if (Mouse.current == null) return;

        // Hide Phase1 cable entirely while any device detail view is open.
        // The device moves to firstLayer when editing, which would drag the line with it.
        bool editorOpen = GameManager.Instance != null && GameManager.Instance.IsEditorOpen;
        _lr.enabled = !editorOpen;
        SetPlugsVisible(!editorOpen);

        if (editorOpen || _deviceA == null) return;

        if (_state == CableState.PendingSecondEnd)
            UpdatePendingSecondEnd();
        else
            UpdateConnected();
    }

    private void SetPlugsVisible(bool on)
    {
        if (endpointPlugA != null) endpointPlugA.SetActive(on);
        if (endpointPlugB != null) endpointPlugB.SetActive(on);
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
            CancelPending();
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Click on first device → cancel.
            if (Vector3.Distance(_deviceA.GetAnchorWorldPosition(), cursorWorld) < secondEndSnapRadius)
            {
                CancelPending();
                return;
            }

            NetworkDevicePort closest = FindClosestPort(cursorWorld, secondEndSnapRadius, exclude: _deviceA);
            if (closest != null)
                AttachSecondEnd(closest);
        }
    }

    private void CancelPending()
    {
        _deviceA?.DisconnectCable(this);
        _deviceA = null;
        Destroy(gameObject);
    }

    private void UpdateConnected()
    {
        Vector3 anchorA = _deviceA.GetAnchorWorldPosition();
        Vector3 anchorB = _deviceB.GetAnchorWorldPosition();

        _lr.SetPosition(0, anchorA);
        _lr.SetPosition(1, anchorB);

        if (endpointPlugA != null) endpointPlugA.transform.position = anchorA;
        if (endpointPlugB != null) endpointPlugB.transform.position = anchorB;

        HandleHoldToDisconnect(anchorA, anchorB);
    }

    // ----------------------------------------------------------------
    //  Hold-to-disconnect
    // ----------------------------------------------------------------

    private void HandleHoldToDisconnect(Vector3 anchorA, Vector3 anchorB)
    {
        Mouse mouse = Mouse.current;
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());

        if (mouse.leftButton.wasPressedThisFrame && _holdTarget == null)
        {
            if (IsMouseOverPlug(endpointPlugA, mouseWorld))
            {
                _holdTarget    = this;
                _holdTargetIsA = true;
                _holdTimer     = 0f;
            }
            else if (IsMouseOverPlug(endpointPlugB, mouseWorld))
            {
                _holdTarget    = this;
                _holdTargetIsA = false;
                _holdTimer     = 0f;
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

    private static NetworkDevicePort FindClosestPort(Vector3 worldPos, float radius, NetworkDevicePort exclude = null)
    {
        NetworkDevicePort[] ports = FindObjectsByType<NetworkDevicePort>(FindObjectsSortMode.None);
        NetworkDevicePort closest = null;
        float bestDist = radius;

        foreach (NetworkDevicePort port in ports)
        {
            if (!port.gameObject.activeInHierarchy) continue;
            if (port == exclude) continue;
            if (!port.CanAcceptCable()) continue;
            float dist = Vector3.Distance(port.GetAnchorWorldPosition(), worldPos);
            if (dist < bestDist) { bestDist = dist; closest = port; }
        }

        return closest;
    }
}
