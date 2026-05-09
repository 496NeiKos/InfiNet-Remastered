using UnityEngine;
using UnityEngine.EventSystems;

public class DragPrefab : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform workspaceArea;
    private RectTransform hardwareArea;
    private Vector3 originalPos;
    private bool _isDragging = false;

    private void Start()
    {
        workspaceArea = GameManager.Instance.workspaceArea;
        hardwareArea = GameManager.Instance.hardwareArea;
        Debug.Log($"{name} → DragPrefab script initialized. Workspace={workspaceArea?.name}, Hardware={hardwareArea?.name}");

        // Try to load any previously saved state
        if (HardwareStateManager.Instance != null)
        {
            IHardwareState hardwareState = GetComponent<IHardwareState>();
            if (hardwareState != null)
                HardwareStateManager.Instance.LoadHardwareState(hardwareState);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Only block drag if:
        // 1. Editor is open AND
        // 2. This prefab is NOT a child of the detail view
        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen && !IsInDetailView())
        {
            _isDragging = false;
            return;
        }

        _isDragging = true;
        originalPos = transform.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f)
        );
        transform.position = worldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        Debug.Log($"{name} → OnEndDrag fired at screen position {eventData.position}");

        bool isChild = IsInDetailView();

        // If this is a child, it can ONLY be dropped to:
        // 1. Its designated slot
        // 2. Hardware area (destroy it)
        if (isChild)
        {
            // Try to find parent slot manager
            HardwareSlotManager parentSlotManager = GetComponentInParent<HardwareSlotManager>();
            if (parentSlotManager != null)
            {
                // Check if dropped on parent's slot (reinstall)
                if (TryDropOnParentSlot(parentSlotManager, eventData))
                {
                    return;
                }
            }

            // Otherwise, only allow hardware area drop
            if (RectTransformUtility.RectangleContainsScreenPoint(hardwareArea, eventData.position, eventData.pressEventCamera))
            {
                Debug.Log($"{name} → Child dropped in HardwareArea, removing from slot and destroying");

                // Remove from parent slot first
                if (parentSlotManager != null)
                {
                    // Find which slot contains this child and remove it
                    foreach (var slotType in GetAllSlotTypes(parentSlotManager))
                    {
                        SlotContainer slot = parentSlotManager.GetSlotByType(slotType);
                        if (slot != null && slot.GetInstalledChild() == gameObject)
                        {
                            parentSlotManager.RemovePrefabFromSlot(slotType);

                            // ✅ KEY FIX: Save parent state IMMEDIATELY after slot change
                            IHardwareState parentState = parentSlotManager.GetComponent<IHardwareState>();
                            if (parentState != null && HardwareStateManager.Instance != null)
                            {
                                HardwareStateManager.Instance.SaveHardwareState(parentState);
                                Debug.Log($"[DragPrefab] Saved parent state after removing child from slot");
                            }
                            break;
                        }
                    }
                }

                // Save state before destroying
                IHardwareState hardwareState = GetComponent<IHardwareState>();
                if (hardwareState != null && HardwareStateManager.Instance != null)
                    HardwareStateManager.Instance.SaveHardwareState(hardwareState);

                Destroy(gameObject);
                return;
            }

            // Invalid drop location for child
            Debug.Log($"{name} → Child dropped in invalid location, snapping back");
            transform.position = originalPos;
            return;
        }

        // If this is a parent (in workspace), handle as before
        if (RectTransformUtility.RectangleContainsScreenPoint(hardwareArea, eventData.position, eventData.pressEventCamera))
        {
            Debug.Log($"{name} → Parent dropped in HardwareArea, saving state and destroying");

            // ✅ KEY FIX: Save parent state with current slot configuration
            IHardwareState hardwareState = GetComponent<IHardwareState>();
            if (hardwareState != null && HardwareStateManager.Instance != null)
                HardwareStateManager.Instance.SaveHardwareState(hardwareState);

            Destroy(gameObject);
        }
        else if (!RectTransformUtility.RectangleContainsScreenPoint(workspaceArea, eventData.position, eventData.pressEventCamera))
        {
            Debug.Log($"{name} → Parent dropped outside Workspace, snapping back");
            transform.position = originalPos;
        }
        else
        {
            Debug.Log($"{name} → Parent dropped in Workspace, keeping position");
        }
    }

    /// <summary>
    /// Check if dropped on parent's slot (reinstall child).
    /// </summary>
    private bool TryDropOnParentSlot(HardwareSlotManager parentSlotManager, PointerEventData eventData)
    {
        // This is simplified — in a full implementation, you'd raycast to find the specific slot
        // For now, we just check if it's still over the parent
        // TODO: Implement proper slot detection on drop
        return false;
    }

    /// <summary>
    /// Get all slot types from a manager (helper).
    /// </summary>
    private System.Collections.Generic.List<string> GetAllSlotTypes(HardwareSlotManager manager)
    {
        var states = manager.GetAllSlotStates();
        return new System.Collections.Generic.List<string>(states.Keys);
    }

    private bool IsInDetailView()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            if (current.name == "DetailGroup")
                return true;
            current = current.parent;
        }
        return false;
    }
}