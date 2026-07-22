using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Phase2 cable that lives inside a device's Detail view.
/// Spawned and managed by NetworkDevicePhase2Manager.
///
/// Behaviour:
///   • Sits at an anchor transform when idle.
///   • Left-click and drag to carry it toward a NetworkDeviceSocket.
///   • Release near a socket → snaps and installs (socket takes ownership).
///   • Release elsewhere → snaps back to anchor.
///   • Once installed, interact via the socket (hold to uninstall).
///
/// The connection label ("Computer → Router") is displayed by the manager's
/// sharedLabel — a TextMeshProUGUI on a Screen Space - Camera canvas in the scene.
/// This avoids world-space canvas camera-reference issues in prefabs.
///
/// Prefab setup:
///   Root: SpriteRenderer + Collider2D + this script (no label child needed)
/// </summary>
public class NetworkLogicalCablePhase2 : MonoBehaviour
{
    [SerializeField] private float snapRadius = 1.5f;

    // Set by NetworkDevicePhase2Manager.
    private NetworkDevicePhase2Manager _manager;
    private NetworkLogicalCable        _representedCable;
    private Transform                  _anchor;
    private Transform                  _detailViewParent;

    private bool _isDragging;
    private bool _isInstalled;

    // Static guard — one Phase2 drag at a time.
    private static NetworkLogicalCablePhase2 _dragTarget;

    public bool IsInstalled => _isInstalled;

    // ----------------------------------------------------------------
    //  Init (called by manager right after Instantiate)
    // ----------------------------------------------------------------

    public void Initialize(
        NetworkDevicePhase2Manager manager,
        NetworkLogicalCable        cable,
        Transform                  anchor,
        Transform                  detailViewParent)
    {
        _manager          = manager;
        _representedCable = cable;
        _anchor           = anchor;
        _detailViewParent = detailViewParent;

        // Position at anchor in world space. Because this object is a child of
        // detailViewParent, subsequent moves of the device (e.g. into firstLayer)
        // preserve the relative local position automatically.
        transform.position = anchor.position;
    }

    // ----------------------------------------------------------------
    //  Update — drag loop
    // ----------------------------------------------------------------

    private void Update()
    {
        if (_isInstalled || Mouse.current == null) return;

        if (!_isDragging)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame
                && _dragTarget == null
                && IsMouseOver())
            {
                _dragTarget  = this;
                _isDragging  = true;
            }
            return;
        }

        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 screen   = Mouse.current.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
            worldPos.z       = 0f;
            transform.position = worldPos;
        }
        else
        {
            _dragTarget = null;
            _isDragging = false;
            OnDropped();
        }
    }

    // ----------------------------------------------------------------
    //  Drop resolution
    // ----------------------------------------------------------------

    private void OnDropped()
    {
        NetworkDeviceSocket socket = FindClosestSocket(transform.position);
        if (socket != null && socket.TryInstall(this))
        {
            _isInstalled       = true;
            transform.position = socket.transform.position;
            _manager.OnPhase2Installed(this);
        }
        else
        {
            ReturnToAnchor();
        }
    }

    // ----------------------------------------------------------------
    //  Called by NetworkDeviceSocket when hold-uninstall completes
    // ----------------------------------------------------------------

    public void OnUninstalledFromSocket()
    {
        _isInstalled = false;
        _manager.OnPhase2Uninstalled(this);
    }

    // ----------------------------------------------------------------
    //  Called by manager to reposition at anchor and re-enable dragging
    // ----------------------------------------------------------------

    public void ReturnToAnchor()
    {
        _isInstalled = false;
        _isDragging  = false;
        if (_anchor != null)
            transform.position = _anchor.position;
    }

    // ----------------------------------------------------------------
    //  Helpers
    // ----------------------------------------------------------------

    private NetworkDeviceSocket FindClosestSocket(Vector3 worldPos)
    {
        // Scope search to sockets within this device's detail view only.
        Transform parent = _detailViewParent != null ? _detailViewParent : transform.parent;
        var sockets = parent.GetComponentsInChildren<NetworkDeviceSocket>(false);

        NetworkDeviceSocket closest  = null;
        float               bestDist = snapRadius;

        foreach (NetworkDeviceSocket s in sockets)
        {
            if (!s.gameObject.activeInHierarchy) continue;
            if (s.IsCableInstalled) continue;
            float dist = Vector3.Distance(s.transform.position, worldPos);
            if (dist < bestDist) { bestDist = dist; closest = s; }
        }
        return closest;
    }

    private bool IsMouseOver()
    {
        Vector2    mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D col        = GetComponent<Collider2D>();
        if (col != null && col.enabled) return col.OverlapPoint(mouseWorld);
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) return sr.bounds.Contains(new Vector3(mouseWorld.x, mouseWorld.y, sr.bounds.center.z));
        return false;
    }
}
