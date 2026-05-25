using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On CPULock object (sibling of CPU under CPUSlot).
/// Drag left-to-right = open, right-to-left = close.
/// Only interactive when Motherboard Phase 2 editing panel is open.
/// </summary>
public class CPULockController : MonoBehaviour
{
    public enum LockState { Closed, Open }

    [Header("Sprites")]
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;

    [Header("Positions (local, relative to CPUSlot)")]
    [SerializeField] private Vector3 closedLocalPos;
    [SerializeField] private Vector3 openLocalPos;

    [Header("Drag Settings")]
    [SerializeField] private float dragThreshold = 20f;

    private SpriteRenderer _sr;
    private LockState _state = LockState.Closed;

    private bool _isPressed = false;
    private Vector2 _pressStartScreenPos;

    public bool IsLocked => _state == LockState.Closed;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        ApplyState();
    }

    private void Update()
    {
        if (!GameManager.Instance.IsEditorOpen) return;

        // Lock is only interactable after heatsink is uninstalled
        CPUSlotController cpuSlot = GetComponentInParent<CPUSlotController>();
        if (cpuSlot != null && cpuSlot.IsHeatsinkInstalled)
        {
            _isPressed = false;
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame && IsMouseOver())
        {
            _isPressed = true;
            _pressStartScreenPos = mouse.position.ReadValue();
        }

        if (_isPressed && mouse.leftButton.wasReleasedThisFrame)
        {
            _isPressed = false;
            Vector2 delta = mouse.position.ReadValue() - _pressStartScreenPos;

            if (Mathf.Abs(delta.x) >= dragThreshold)
            {
                if (delta.x > 0f)
                    SetState(LockState.Open);
                else
                    SetState(LockState.Closed);
            }
        }

        // Cancel if mouse button released elsewhere
        if (_isPressed && !mouse.leftButton.isPressed)
            _isPressed = false;
    }

    private void SetState(LockState newState)
    {
        if (_state == newState) return;
        _state = newState;
        ApplyState();
        Debug.Log($"[CPULockController] State → {_state}");
    }

    private void ApplyState()
    {
        if (_sr != null)
            _sr.sprite = (_state == LockState.Closed) ? closedSprite : openSprite;

        transform.localPosition = (_state == LockState.Closed) ? closedLocalPos : openLocalPos;
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        foreach (Collider2D col in GetComponents<Collider2D>())
            if (col.OverlapPoint(mouseWorld)) return true;
        return false;
    }
}