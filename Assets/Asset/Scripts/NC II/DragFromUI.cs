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

    public void OnBeginDrag(PointerEventData eventData)
    {
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
        if (dragIndicator != null)
        {
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out pos
            );
            indicatorRect.localPosition = pos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"{name} → OnEndDrag: drag ended");

        if (RectTransformUtility.RectangleContainsScreenPoint(workspaceArea, eventData.position, eventData.pressEventCamera))
        {
            Debug.Log($"{name} → dropped in Workspace, spawning prefab");

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(eventData.position.x, eventData.position.y, 10f)
            );

            GameObject spawned = Instantiate(prefabToSpawn, worldPos, Quaternion.identity);

            // NOTE: PrefabInteraction no longer needs editingPanel injected here —
            // it reads it from GameManager.Instance.editingPanel at runtime.
            // This log confirms the spawn worked correctly.
            Debug.Log($"{name} → spawned {spawned.name} at {worldPos}");
        }
        else
        {
            Debug.Log($"{name} → dropped outside Workspace, no prefab spawned");
        }

        if (dragIndicator != null)
        {
            Destroy(dragIndicator);
            dragIndicator = null;
        }
    }
}