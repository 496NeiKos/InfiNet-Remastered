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
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Block dragging while the editing panel is open
        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen)
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

        if (RectTransformUtility.RectangleContainsScreenPoint(hardwareArea, eventData.position, eventData.pressEventCamera))
        {
            Debug.Log($"{name} → Dropped in HardwareArea, destroying prefab");
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
}