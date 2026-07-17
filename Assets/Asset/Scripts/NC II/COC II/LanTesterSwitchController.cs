using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to LanTesterSwitch. Double-click to toggle on/off.
/// Swaps sprite between on and off states.
/// Requires a BoxCollider2D on the same GameObject for reliable click detection;
/// falls back to SpriteRenderer bounds if no collider is present.
/// </summary>
public class LanTesterSwitchController : MonoBehaviour
{
    [SerializeField] private Sprite offSprite;
    [SerializeField] private Sprite onSprite;
    [Tooltip("Maximum seconds between two clicks to count as a double-click.")]
    [SerializeField] private float doubleClickWindow = 0.35f;

    public bool IsOn { get; private set; }

    private SpriteRenderer _sr;
    private Collider2D _col;
    private float _lastClickTime = -99f;

    private void Awake()
    {
        _sr  = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();
        ApplySprite();
    }

    private void Update()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector2 worldPt = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        if (!IsHit(worldPt)) return;

        float now = Time.time;
        if (now - _lastClickTime <= doubleClickWindow)
        {
            IsOn = !IsOn;
            ApplySprite();
            _lastClickTime = -99f; // reset so triple-click needs a fresh double
        }
        else
        {
            _lastClickTime = now;
        }
    }

    private bool IsHit(Vector2 worldPt)
    {
        if (_col != null) return _col.OverlapPoint(worldPt);
        if (_sr  != null) return _sr.bounds.Contains(worldPt);
        return false;
    }

    private void ApplySprite()
    {
        if (_sr == null) return;
        Sprite target = IsOn ? onSprite : offSprite;
        if (target != null) _sr.sprite = target;
    }
}
