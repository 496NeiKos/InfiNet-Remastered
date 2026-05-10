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
        // ✅ REMOVED: IsEditorOpen block
        // User can now drag hardware from hardware area even when editing panel is open
        // This allows screwdriver and other tools to be used in the editing panel

        _isDragging = true;

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
        if (dragIndicator != null)
        {
            Destroy(dragIndicator);
            dragIndicator = null;
        }

        if (!_isDragging) return;
        _isDragging = false;

        if (!RectTransformUtility.RectangleContainsScreenPoint(
            workspaceArea, eventData.position, eventData.pressEventCamera))
            return;

        // Only instantiate if editing panel is NOT open
        // When editing panel is open, hardware area is for tools only (screwdriver etc.)
        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen)
            return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f)
        );
        worldPos.z = 0f;

        GameObject spawned = Instantiate(prefabToSpawn, worldPos, Quaternion.identity);
        spawned.transform.SetParent(workspaceArea.transform, true);

        Debug.Log($"{name} → spawned {spawned.name} as child of {workspaceArea.name}");
    }
}