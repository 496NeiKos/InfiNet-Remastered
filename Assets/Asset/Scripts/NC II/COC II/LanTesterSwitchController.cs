using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to LanTesterSwitch. Double-click to toggle on/off.
/// Changes sprites on the LanTester parent and FrontDetail parent,
/// not on this switch button itself.
/// </summary>
public class LanTesterSwitchController : MonoBehaviour
{
    [Header("Target renderers")]
    [SerializeField] private SpriteRenderer lanTesterRenderer;
    [SerializeField] private SpriteRenderer frontDetailRenderer;

    [Header("On-state sprites (off-state uses each object's default sprite)")]
    [SerializeField] private Sprite lanTesterOnSprite;
    [SerializeField] private Sprite frontDetailOnSprite;

    [Tooltip("Maximum seconds between two clicks to count as a double-click.")]
    [SerializeField] private float doubleClickWindow = 0.35f;

    public bool IsOn { get; private set; }

    private Collider2D _col;
    private SpriteRenderer _switchSr; // used only for hit-testing
    private float _lastClickTime = -99f;

    private Sprite _lanTesterOffSprite;
    private Sprite _frontDetailOffSprite;

    private void Awake()
    {
        _switchSr = GetComponent<SpriteRenderer>();
        _col      = GetComponent<Collider2D>();

        if (lanTesterRenderer  != null) _lanTesterOffSprite  = lanTesterRenderer.sprite;
        if (frontDetailRenderer != null) _frontDetailOffSprite = frontDetailRenderer.sprite;
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
            ApplySprites();
            _lastClickTime = -99f;
        }
        else
        {
            _lastClickTime = now;
        }
    }

    private bool IsHit(Vector2 worldPt)
    {
        if (_col      != null) return _col.OverlapPoint(worldPt);
        if (_switchSr != null) return _switchSr.bounds.Contains(worldPt);
        return false;
    }

    private void ApplySprites()
    {
        if (lanTesterRenderer != null)
            lanTesterRenderer.sprite = IsOn ? lanTesterOnSprite : _lanTesterOffSprite;

        if (frontDetailRenderer != null)
            frontDetailRenderer.sprite = IsOn ? frontDetailOnSprite : _frontDetailOffSprite;
    }
}
