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
    [SerializeField] private float holdDuration = 5f;

    /// <summary>True after a reset has been performed at least once this session.</summary>
    public bool IsReset { get; private set; }

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
        IsReset = true;
        ActivityLogManager.Log("Router reset to factory defaults.", ActivityLogManager.EntryType.Action);
        // TODO: after-reset behavior (restore default IP, clear config, etc.) goes here.
    }

    private bool IsHit(Vector2 worldPt)
    {
        if (_col != null) return _col.OverlapPoint(worldPt);
        if (_sr  != null) return _sr.bounds.Contains(worldPt);
        return false;
    }
}
