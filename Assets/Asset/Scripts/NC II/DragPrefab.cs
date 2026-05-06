using UnityEngine;
using UnityEngine.EventSystems;

public class DragPrefab : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform workspaceArea;
    private RectTransform hardwareArea;
    private Vector3 originalPos;

    private void Start()
    {
        // ✅ Get references from GameManager
        workspaceArea = GameManager.Instance.workspaceArea;
        hardwareArea = GameManager.Instance.hardwareArea;

        Debug.Log($"{name} → DragPrefab script initialized. Workspace={workspaceArea?.name}, Hardware={hardwareArea?.name}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPos = transform.position;
        Debug.Log($"{name} → OnBeginDrag fired at {originalPos}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log($"{name} → OnDrag fired at screen position {eventData.position}");

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f)
        );
        transform.position = worldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
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
