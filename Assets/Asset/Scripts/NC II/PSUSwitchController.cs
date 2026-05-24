using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On the PSU object in the system unit back view.
/// Double-click (two clicks within 0.5 s on this object's own collider) toggles
/// the PSU switch on/off and swaps the sprite accordingly.
/// Toggle is blocked unless the PSU cable's BackPortSlot reports Installed.
/// </summary>
public class PSUSwitchController : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite offSprite;
    [SerializeField] private Sprite onSprite;

    [Header("PSU Port")]
    [Tooltip("The BackPortSlot the PSU cable plugs into — cable must be installed to allow toggling.")]
    [SerializeField] private BackPortSlot psuPort;

    [Header("Double-Click Window (seconds)")]
    [SerializeField] private float doubleClickWindow = 0.5f;

    private SpriteRenderer _sr;
    private bool _isOn = false;

    private int _clickCount;
    private float _clickTimer;

    public bool IsOn => _isOn;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        _clickCount = 0;
        _clickTimer = 0f;
        ApplySprite();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (_clickCount > 0)
        {
            _clickTimer += Time.deltaTime;
            if (_clickTimer >= doubleClickWindow)
            {
                _clickCount = 0;
                _clickTimer = 0f;
            }
        }

        if (!mouse.leftButton.wasPressedThisFrame || !IsMouseOver()) return;

        _clickCount++;

        if (_clickCount == 1)
        {
            _clickTimer = 0f;
        }
        else if (_clickCount >= 2)
        {
            _clickCount = 0;
            _clickTimer = 0f;
            TryToggle();
        }
    }

    private void TryToggle()
    {
        if (psuPort != null && !psuPort.IsInstalled)
        {
            Debug.Log("[PSUSwitchController] Cannot toggle — PSU cable not installed.");
            return;
        }

        _isOn = !_isOn;
        ApplySprite();
        Debug.Log($"[PSUSwitchController] PSU switch → {(_isOn ? "On" : "Off")}");
    }

    private void ApplySprite()
    {
        if (_sr == null) return;
        _sr.sprite = _isOn ? onSprite : offSprite;
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D col = GetComponent<Collider2D>();
        return col != null && col.OverlapPoint(mouseWorld);
    }
}
