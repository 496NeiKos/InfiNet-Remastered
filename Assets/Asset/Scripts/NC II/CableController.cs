using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// A draggable cable object.
/// Spawned when user detaches a cable from CableSlot, or dragged from hardware area.
/// Can be dropped on a matching CableSlot to install, or on hardware area to destroy.
///
/// This is the CABLE OBJECT, not the slot. It has no installed/uninstalled state —
/// it simply exists as a draggable thing. The CableSlot holds the visual state.
/// </summary>
public class CableController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Identity")]
    [Tooltip("Must match CableSlot's cableType")]
    [SerializeField] private string cableType = "Cable1";

    private bool _isDragging = false;
    private bool _startedFromDetach = false;
    private RectTransform _hardwareArea;

    private void Start()
    {
        if (GameManager.Instance != null)
            _hardwareArea = GameManager.Instance.hardwareArea;
    }

    /// <summary>
    /// Called by CableSlot immediately after spawning this cable from a detach.
    /// Starts following the mouse right away without needing a new drag gesture.
    /// </summary>
    public void StartDragImmediately()
    {
        _startedFromDetach = true;
        _isDragging = true;

        if (GameManager.Instance != null)
            _hardwareArea = GameManager.Instance.hardwareArea;
    }

    private void Update()
    {
        // If started from detach, follow mouse until user releases
        if (!_startedFromDetach) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(Mouse.current.position.ReadValue().x,
                        Mouse.current.position.ReadValue().y, 10f)
        );
        worldPos.z = 0f;
        transform.position = worldPos;

        // When user releases mouse, handle the drop
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            _startedFromDetach = false;
            _isDragging = false;
            HandleDrop(Mouse.current.position.ReadValue());
        }
    }

    // ── Drag Handlers (for normal drag from hardware area via CableDrag) ──

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_startedFromDetach) return; // Already being handled by Update

        _isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || _startedFromDetach) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f)
        );
        worldPos.z = 0f;
        transform.position = worldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging || _startedFromDetach) return;
        _isDragging = false;

        HandleDrop(eventData.position);
    }

    // ── Drop Handling ─────────────────────────────────────────────────────

    private void HandleDrop(Vector2 screenPos)
    {
        // Check if dropped on a matching cable slot
        CableSlot matchingSlot = FindMatchingSlotAtPosition(screenPos);
        if (matchingSlot != null)
        {
            matchingSlot.InstallCable();
            Debug.Log($"[CableController] {cableType} installed in slot");
            Destroy(gameObject);
            return;
        }

        // Check if dropped on hardware area
        if (_hardwareArea != null &&
            RectTransformUtility.RectangleContainsScreenPoint(_hardwareArea, screenPos, Camera.main))
        {
            Debug.Log($"[CableController] {cableType} returned to hardware area");
            Destroy(gameObject);
            return;
        }

        // Invalid drop → destroy (cable disappears)
        Debug.Log($"[CableController] {cableType} dropped in invalid area, destroyed");
        Destroy(gameObject);
    }

    /// <summary>
    /// Raycast to find a CableSlot that matches this cable type.
    /// </summary>
    private CableSlot FindMatchingSlotAtPosition(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null) continue;

            CableSlot slot = hit.collider.GetComponent<CableSlot>();
            if (slot != null && slot.CanAcceptCable(cableType))
            {
                return slot;
            }
        }

        return null;
    }

    // ── Getters ───────────────────────────────────────────────────────────

    public string GetCableType() => cableType;
}