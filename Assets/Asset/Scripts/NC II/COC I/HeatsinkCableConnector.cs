using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Placed on a child GameObject inside the Heatsink detailed view (its own Box Collider 2D).
/// Slide down to disconnect the cable, slide up to connect it.
/// HeatsinkController reads IsConnected to gate the drag-out.
/// </summary>
public class HeatsinkCableConnector : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite connectedSprite;
    [SerializeField] private Sprite disconnectedSprite;

    [Header("Slide Threshold (pixels)")]
    [SerializeField] private float slideThreshold = 80f;

    private SpriteRenderer _sr;
    private bool _isConnected = true;
    private bool _isPressed;
    private Vector2 _pressStartPos;

    public bool IsConnected => _isConnected;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        ApplySprite();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame && IsMouseOver())
        {
            _isPressed = true;
            _pressStartPos = mouse.position.ReadValue();
        }

        if (_isPressed && mouse.leftButton.wasReleasedThisFrame)
        {
            _isPressed = false;
            Vector2 delta = mouse.position.ReadValue() - _pressStartPos;

            if (delta.y < -slideThreshold)
                SetConnected(false);
            else if (delta.y > slideThreshold)
                SetConnected(true);
        }
    }

    private void SetConnected(bool connected)
    {
        _isConnected = connected;
        ApplySprite();
        GetComponentInParent<HeatsinkController>()?.UpdateRootSprite(connected);
        ActivityLogManager.Log(
            connected ? "Heatsink fan cable connected" : "Heatsink fan cable disconnected",
            connected ? ActivityLogManager.EntryType.Install : ActivityLogManager.EntryType.Remove);
        Debug.Log($"[HeatsinkCableConnector] Cable {(connected ? "connected" : "disconnected")}.");
        NCIITaskListManager.CheckConditions();
    }

    private void ApplySprite()
    {
        if (_sr == null) return;
        _sr.sprite = _isConnected ? connectedSprite : disconnectedSprite;
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        foreach (Collider2D col in GetComponents<Collider2D>())
            if (col.OverlapPoint(mouseWorld)) return true;
        return false;
    }
}
