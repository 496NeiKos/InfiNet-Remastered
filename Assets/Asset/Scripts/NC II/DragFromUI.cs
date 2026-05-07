using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragFromUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Prefab to spawn in Workspace")]
    public GameObject prefabToSpawn;

    [Header("Workspace Panel (drop zone)")]
    public RectTransform workspaceArea;

    [Header("Canvas Reference")]
    public Canvas canvas;

    private GameObject dragIndicator;
    private RectTransform indicatorRect;
    private bool _isDragging = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Block dragging from UI while the editing panel is open
        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen)
        {
            _isDragging = false;
            return;
        }

        _isDragging = true;
        Debug.Log($"{name} → OnBeginDrag: creating drag indicator");

        dragIndicator = new GameObject("DragIndicator");
        dragIndicator.transform.SetParent(canvas.transform, false);

        Image indicatorImage = dragIndicator.AddComponent<Image>();
        indicatorImage.sprite = GetComponent<Image>().sprite;
        indicatorImage.raycastTarget = false;

        indicatorRect = dragIndicator.GetComponent<RectTransform>();
        indicatorRect.sizeDelta = GetComponent<RectTransform>().sizeDelta;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || dragIndicator == null) return;

        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out pos
        );
        indicatorRect.localPosition = pos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Always clean up indicator regardless of whether drag was active
        if (dragIndicator != null)
        {
            Destroy(dragIndicator);
            dragIndicator = null;
        }

        if (!_isDragging) return;
        _isDragging = false;

        Debug.Log($"{name} → OnEndDrag: drag ended");

        if (RectTransformUtility.RectangleContainsScreenPoint(workspaceArea, eventData.position, eventData.pressEventCamera))
        {
            Debug.Log($"{name} → dropped in Workspace, spawning prefab");

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(eventData.position.x, eventData.position.y, 10f)
            );
            GameObject spawned = Instantiate(prefabToSpawn, worldPos, Quaternion.identity);
            Debug.Log($"{name} → spawned {spawned.name} at {worldPos}");
        }
        else
        {
            Debug.Log($"{name} → dropped outside Workspace, no prefab spawned");
        }
    }
}