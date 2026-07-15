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

    [Header("Initial State")]
    [SerializeField] private bool startOn = false;

    [Header("Double-Click Window (seconds)")]
    [SerializeField] private float doubleClickWindow = 0.5f;

    private SpriteRenderer _sr;
    private bool _isOn = false;
    private bool _initialized = false;

    private int _clickCount;
    private float _clickTimer;

    // Falls back to startOn if Awake hasn't run yet (object was inactive at scene load).
    public bool IsOn => _initialized ? _isOn : startOn;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _isOn = startOn;
        _initialized = true;
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
            ActivityLogManager.Log("Cannot toggle PSU switch — plug in the PSU cable first.", ActivityLogManager.EntryType.Warning);
            Debug.Log("[PSUSwitchController] Cannot toggle — PSU cable not installed.");
            return;
        }

        _isOn = !_isOn;
        ApplySprite();
        ActivityLogManager.Log($"PSU switch turned {(_isOn ? "ON" : "OFF")}",
            _isOn ? ActivityLogManager.EntryType.Install : ActivityLogManager.EntryType.Remove);
        Debug.Log($"[PSUSwitchController] PSU switch → {(_isOn ? "On" : "Off")}");

        NCIITaskListManager.CheckConditions();
    }

    private void ApplySprite()
    {
        if (_sr == null) return;
        _sr.sprite = _isOn ? onSprite : offSprite;
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        var ownCols = GetComponents<Collider2D>();
        bool ownHit = false;
        foreach (Collider2D col in ownCols)
            if (col.OverlapPoint(mouseWorld)) { ownHit = true; break; }
        if (!ownHit) return false;

        // If a child object (cable, port) with a higher sorting order is on top,
        // let that object handle the click instead of the PSU switch.
        SpriteRenderer mySR = GetComponent<SpriteRenderer>();
        int myOrder = mySR != null ? mySR.sortingOrder : 0;

        var ownColSet = new System.Collections.Generic.HashSet<Collider2D>(ownCols);
        foreach (Collider2D hit in Physics2D.OverlapPointAll(mouseWorld))
        {
            if (ownColSet.Contains(hit)) continue;
            SpriteRenderer sr = hit.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sortingOrder > myOrder)
                return false;
        }

        return true;
    }
}
