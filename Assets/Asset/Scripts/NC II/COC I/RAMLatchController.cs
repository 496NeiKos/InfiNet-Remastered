using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On each retention clip child of RAMDetailedView (LeftLatch, RightLatch).
/// Detects a horizontal drag gesture (≥ 100 px) and toggles this latch open/closed.
///
/// Left latch  — slide right to close (latch), slide left to open (unlatch)
/// Right latch — slide left  to close (latch), slide right to open (unlatch)
///
/// Both latches must be closed for RAMController.IsInstalled to be true.
/// Either latch remaining closed keeps the RAM locked in the slot.
/// </summary>
public class RAMLatchController : MonoBehaviour
{
    public enum LatchSide { Left, Right }

    [SerializeField] private LatchSide side;

    private const float DragThreshold = 100f;

    private RAMController _ramController;
    private bool _isPressed;
    private Vector2 _pressStartScreenPos;

    private void Awake()
    {
        _ramController = GetComponentInParent<RAMController>();
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsEditorOpen) return;

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

            if (Mathf.Abs(delta.x) >= DragThreshold)
            {
                bool slideRight = delta.x > 0f;

                if (side == LatchSide.Left)
                {
                    if (slideRight) TryLatch();
                    else            TryUnlatch();
                }
                else
                {
                    if (!slideRight) TryLatch();
                    else             TryUnlatch();
                }
            }
        }

        if (_isPressed && !mouse.leftButton.isPressed)
            _isPressed = false;
    }

    private void TryLatch()
    {
        if (_ramController == null) return;
        if (IsCurrentlyLatched()) return;
        _ramController.SetLatchState(side, true);
        ActivityLogManager.Log($"{_ramController.name} {SideName()} latch closed — RAM seated.", ActivityLogManager.EntryType.Install);
        Debug.Log($"[RAMLatchController:{name}] {side} latch → Closed");
    }

    private void TryUnlatch()
    {
        if (_ramController == null) return;
        if (!IsCurrentlyLatched()) return;
        _ramController.SetLatchState(side, false);
        ActivityLogManager.Log($"{_ramController.name} {SideName()} latch opened — RAM released.", ActivityLogManager.EntryType.Remove);
        Debug.Log($"[RAMLatchController:{name}] {side} latch → Opened");
    }

    private bool IsCurrentlyLatched() =>
        _ramController != null &&
        (side == LatchSide.Left ? _ramController.IsLeftLatched : _ramController.IsRightLatched);

    private string SideName() => side == LatchSide.Left ? "left" : "right";

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        foreach (Collider2D col in GetComponents<Collider2D>())
            if (col.OverlapPoint(mouseWorld)) return true;
        return false;
    }
}
