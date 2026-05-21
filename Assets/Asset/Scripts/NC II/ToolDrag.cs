using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Generic tool drag for ThermalPaste and TowelCloth icons in the hardware area.
/// Same pattern as ScrewdriverDrag:
///   - Creates a temporary world-space drag object with a trigger collider
///   - Tag is set to toolTag (e.g. "ThermalPaste" or "TowelCloth")
///   - On drop: drag object is destroyed (tool returns to hardware area)
/// </summary>
public class ToolDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Tool Settings")]
    [SerializeField] private string toolTag = "ThermalPaste";
    [SerializeField] private Sprite toolSprite;
    [SerializeField] private float colliderRadius = 0.3f;

    private GameObject _dragObject;
    private bool _isDragging = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;

        _dragObject = new GameObject("ToolDrag_" + toolTag);
        _dragObject.tag = toolTag;

        SpriteRenderer sr = _dragObject.AddComponent<SpriteRenderer>();
        sr.sprite = toolSprite != null ? toolSprite : GetComponent<Image>()?.sprite;
        sr.sortingOrder = 100;

        CircleCollider2D col = _dragObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = colliderRadius;

        Rigidbody2D rb = _dragObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        worldPos.z = 0f;
        _dragObject.transform.position = worldPos;

        Debug.Log($"[ToolDrag] Started dragging {toolTag}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || _dragObject == null) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        worldPos.z = 0f;
        _dragObject.transform.position = worldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragObject != null)
        {
            Destroy(_dragObject);
            _dragObject = null;
        }
        _isDragging = false;
        Debug.Log($"[ToolDrag] {toolTag} returned to hardware area.");
    }
}