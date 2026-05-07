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

        // Check if dropped in hardware area (destruction zone)
        if (RectTransformUtility.RectangleContainsScreenPoint(hardwareArea, eventData.position, eventData.pressEventCamera))
        {
            Debug.Log($"{name} → Dropped in HardwareArea, saving state and destroying prefab");

            // ✅ SAVE STATE before destroying
            IHardwareState hardwareState = GetComponent<IHardwareState>();
            if (hardwareState != null && HardwareStateManager.Instance != null)
                HardwareStateManager.Instance.SaveHardwareState(hardwareState);

            Destroy(gameObject);
        }
        else if (!RectTransformUtility.RectangleContainsScreenPoint(workspaceArea, eventData.position, eventData.pressEventCamera))
        {
            Debug.Log($"{name} → Dropped outside Workspace, snapping back");
            transform.position = originalPos;
        }
        else
        {
            Debug.Log($"{name} → Dropped in Workspace, keeping position");
        }
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