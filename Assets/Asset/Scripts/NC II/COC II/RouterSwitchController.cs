using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to RouterSwitch (child of RouterFront).
/// Double-click to toggle the router on/off.
/// Changes sprites on the Router root and RouterFront — not on this button itself.
/// Default state is off; off-sprites are captured from each renderer at Awake.
/// </summary>
public class RouterSwitchController : MonoBehaviour
{
    [Header("Target renderers")]
    [SerializeField] private SpriteRenderer routerRenderer;
    [SerializeField] private SpriteRenderer frontDetailRenderer;

    [Header("On-state sprites (off uses each renderer's default sprite)")]
    [SerializeField] private Sprite routerOnSprite;
    [SerializeField] private Sprite frontDetailOnSprite;

    [Tooltip("Maximum seconds between two clicks to count as a double-click.")]
    [SerializeField] private float doubleClickWindow = 0.35f;

    public bool IsOn { get; private set; }

    private Collider2D     _col;
    private SpriteRenderer _switchSr;
    private float          _lastClickTime = -99f;

    private Sprite _routerOffSprite;
    private Sprite _frontDetailOffSprite;

    private void Awake()
    {
        _switchSr = GetComponent<SpriteRenderer>();
        _col      = GetComponent<Collider2D>();

        if (routerRenderer      != null) _routerOffSprite      = routerRenderer.sprite;
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
        if (routerRenderer != null)
            routerRenderer.sprite = IsOn ? routerOnSprite : _routerOffSprite;

        if (frontDetailRenderer != null)
            frontDetailRenderer.sprite = IsOn ? frontDetailOnSprite : _frontDetailOffSprite;
    }
}
