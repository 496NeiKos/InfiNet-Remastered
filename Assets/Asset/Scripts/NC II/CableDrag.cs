using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drag behavior for cable icons in the hardware area.
/// Each cable icon is unique (Cable1, Cable2, etc.) and only fits its matching slot.
///
/// - Creates a temporary visual while dragging
/// - If dropped on matching CableSlot → instantiates cable prefab, slot becomes installed
/// - If dropped elsewhere → visual disappears (returned to hardware area)
/// </summary>
public class CableDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Cable Settings")]
    [SerializeField] private Sprite cableDragSprite;

    [Tooltip("Must match CableSlot's cableType")]
    [SerializeField] private string cableType = "Cable1";

    [Header("Canvas Reference")]
    public Canvas canvas;

    private GameObject _dragVisual;
    private bool _isDragging = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;

        _dragVisual = new GameObject("CableDrag_" + cableType);

        SpriteRenderer sr = _dragVisual.AddComponent<SpriteRenderer>();
        sr.sprite = cableDragSprite != null ? cableDragSprite : GetComponent<Image>().sprite;
        sr.sortingOrder = 100;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f)
        );
        worldPos.z = 0f;
        _dragVisual.transform.position = worldPos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || _dragVisual == null) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f)
        );
        worldPos.z = 0f;
        _dragVisual.transform.position = worldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragVisual != null)
        {
            Destroy(_dragVisual);
            _dragVisual = null;
        }

        if (!_isDragging) return;
        _isDragging = false;

        // Check if dropped on a matching cable slot
        CableSlot matchingSlot = FindMatchingSlotAtPosition(eventData.position);
        if (matchingSlot != null)
        {
            matchingSlot.InstallCable();
            Debug.Log($"[CableDrag] Installed {cableType} from hardware area");
            return;
        }

        Debug.Log($"[CableDrag] {cableType} returned to hardware area");
    }

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
}