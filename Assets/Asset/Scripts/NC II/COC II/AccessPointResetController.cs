/*
 * ================================================================
 *  UNITY SETUP GUIDE — AccessPointResetController
 * ================================================================
 *  PLACEMENT
 *    Attach to the reset button child of the Access Point's
 *    front detail view (e.g. APReset, child of APFront).
 *    Requires a Collider2D or SpriteRenderer on the same object
 *    for hit detection.
 *
 *  HOW IT WORKS
 *    Left-click and hold for holdDuration seconds to trigger a
 *    factory reset. Sets IsDefaultIPReady to true — used by future
 *    validation to confirm the AP was reset before configuration.
 * ================================================================
 */

using UnityEngine;
using UnityEngine.InputSystem;

public class AccessPointResetController : MonoBehaviour
{
    [Tooltip("Seconds to hold before the reset triggers.")]
    [SerializeField] private float holdDuration = 10f;

    /// <summary>
    /// False by default — access point starts without its default IP ready.
    /// Becomes true only after the user performs a factory reset (hold for holdDuration).
    /// Use this flag in future validation to check that the AP was reset before configuration.
    /// </summary>
    public bool IsDefaultIPReady { get; private set; }

    /// <summary>
    /// True after the student saves a valid IP configuration for the Access Point
    /// in the TP-Link Virtual OS. Set by TPLinkTabManager on a valid Save.
    /// Cleared back to false if the AP is reset again.
    /// </summary>
    public bool IsConfigured { get; private set; }

    public void SetConfigured()
    {
        IsConfigured = true;
        Debug.Log("[APReset] Access Point marked as configured.");
        ActivityLogManager.Log("Access Point configuration saved.", ActivityLogManager.EntryType.Action);
    }

    private Collider2D     _col;
    private SpriteRenderer _sr;

    private bool  _holding;
    private float _holdTimer;

    private static AccessPointResetController _holdTarget;

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
            Debug.Log($"[APReset] Holding... {_holdTimer:F1} / {holdDuration:F1}s");
            if (_holdTimer >= holdDuration)
            {
                _holdTarget = null;
                _holding    = false;
                _holdTimer  = 0f;
                ResetAccessPoint();
            }
        }
    }

    private void ResetAccessPoint()
    {
        IsDefaultIPReady = true;
        IsConfigured     = false;
        Debug.Log("[APReset] Reset triggered — IsDefaultIPReady = true, IsConfigured = false");
        ActivityLogManager.Log("Access Point reset to factory defaults.", ActivityLogManager.EntryType.Action);
    }

    private bool IsHit(Vector2 worldPt)
    {
        if (_col != null) return _col.OverlapPoint(worldPt);
        if (_sr  != null) return _sr.bounds.Contains(worldPt);
        return false;
    }
}
