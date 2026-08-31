using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to RouterReset (child of RouterFront, sibling of RouterSwitch).
/// Left-click and hold for holdDuration seconds to reset the router state.
/// The state tracked here (e.g. IP config) is independent of the power toggle sprite.
/// After-reset behavior is intentionally left as a placeholder for future implementation.
/// </summary>
public class RouterResetController : MonoBehaviour
{
    [Tooltip("Seconds to hold before the reset triggers.")]
    [SerializeField] private float holdDuration = 10f;

    [Tooltip("TPLinkTabManager to reset to factory defaults when the router is reset.")]
    [SerializeField] private TPLinkTabManager tpLinkTabManager;

    /// <summary>
    /// False by default — router starts without its default IP ready.
    /// Becomes true only after the user performs a factory reset (hold for holdDuration).
    /// Use this flag in future validation to check that the router was reset before configuration.
    /// </summary>
    public bool IsDefaultIPReady { get; private set; }

    /// <summary>
    /// True after the student saves a valid IP configuration for the Router
    /// in the TP-Link Virtual OS. Set by TPLinkTabManager on a valid Save.
    /// Cleared back to false if the router is reset again.
    /// </summary>
    public bool IsConfigured { get; private set; }

    public void SetConfigured()
    {
        IsConfigured = true;
        Debug.Log("[RouterReset] Router marked as configured.");
        ActivityLogManager.Log("Router configuration saved.", ActivityLogManager.EntryType.Action);
    }

    private Collider2D     _col;
    private SpriteRenderer _sr;

    private bool  _holding;
    private float _holdTimer;

    private static RouterResetController _holdTarget;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _sr  = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame && _holdTarget == null)
        {
            Vector2 worldPt = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            if (IsHit(worldPt))
            {
                _holdTarget = this;
                _holding    = true;
                _holdTimer  = 0f;
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
            Debug.Log($"[RouterReset] Holding... {_holdTimer:F1} / {holdDuration:F1}s");
            if (_holdTimer >= holdDuration)
            {
                _holdTarget = null;
                _holding    = false;
                _holdTimer  = 0f;
                ResetRouter();
            }
        }
    }

    private void ResetRouter()
    {
        IsDefaultIPReady = true;
        IsConfigured     = false;
        tpLinkTabManager?.ResetToDefaults();
        Debug.Log("[RouterReset] Reset triggered — IsDefaultIPReady = true, IsConfigured = false");
        ActivityLogManager.Log("Router reset to factory defaults.", ActivityLogManager.EntryType.Action);
    }

    private bool IsHit(Vector2 worldPt)
    {
        if (_col != null) return _col.OverlapPoint(worldPt);
        if (_sr  != null) return _sr.bounds.Contains(worldPt);
        return false;
    }
}
