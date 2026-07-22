using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to each socket sprite inside a device's Detail view.
/// A NetworkLogicalCablePhase2 can be dragged and dropped near this socket to install.
/// Hold the installed cable for holdDuration seconds to uninstall it.
///
/// Scene setup:
///   • Add this component + a Collider2D (BoxCollider2D recommended) to the socket sprite GameObject.
///   • Duplicate the socket for as many ports as the device needs.
///   • Each socket accepts only one Phase2 cable at a time.
/// </summary>
public class NetworkDeviceSocket : MonoBehaviour
{
    [SerializeField] private float holdDuration = 1f;

    public bool IsCableInstalled => _installedCable != null;

    private NetworkLogicalCablePhase2 _installedCable;
    private Collider2D _col;
    private SpriteRenderer _sr;

    private bool  _holding;
    private float _holdTimer;

    // Static guard — only one socket hold at a time across all instances.
    private static NetworkDeviceSocket _holdTarget;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _sr  = GetComponent<SpriteRenderer>();
    }

    // ----------------------------------------------------------------
    //  Install
    // ----------------------------------------------------------------

    /// <summary>
    /// Called by NetworkLogicalCablePhase2 on drop. Returns false if already occupied.
    /// </summary>
    public bool TryInstall(NetworkLogicalCablePhase2 cable)
    {
        if (IsCableInstalled) return false;
        _installedCable = cable;
        return true;
    }

    // ----------------------------------------------------------------
    //  Update — hold to uninstall
    // ----------------------------------------------------------------

    private void Update()
    {
        if (!IsCableInstalled) return;
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame && _holdTarget == null)
        {
            Vector2 worldPt = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            if (IsMouseOver(worldPt))
            {
                _holdTarget  = this;
                _holding     = true;
                _holdTimer   = 0f;
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (_holdTarget == this) _holdTarget = null;
            _holding   = false;
            _holdTimer = 0f;
        }

        if (_holding && _holdTarget == this)
        {
            _holdTimer += Time.deltaTime;
            if (_holdTimer >= holdDuration)
            {
                _holdTarget = null;
                _holding    = false;
                _holdTimer  = 0f;
                Uninstall();
            }
        }
    }

    // ----------------------------------------------------------------
    //  Uninstall
    // ----------------------------------------------------------------

    private void Uninstall()
    {
        if (_installedCable == null) return;
        var cable = _installedCable;
        _installedCable = null;        // Clear before notifying to avoid re-entry.
        cable.OnUninstalledFromSocket();
    }

    // ----------------------------------------------------------------
    //  Helpers
    // ----------------------------------------------------------------

    private bool IsMouseOver(Vector2 worldPt)
    {
        if (_col != null && _col.enabled) return _col.OverlapPoint(worldPt);
        if (_sr  != null) return _sr.bounds.Contains(new Vector3(worldPt.x, worldPt.y, _sr.bounds.center.z));
        return false;
    }
}
