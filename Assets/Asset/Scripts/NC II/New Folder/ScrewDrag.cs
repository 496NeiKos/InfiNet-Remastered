using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drag behavior for the Screw icon in the hardware area.
/// - Creates a temporary world-space object tagged "Screw" with a collider
/// - When it touches an unscrewed ScrewController, it instantly re-screws it
/// - ScrewController destroys the drag object on contact (screw is "used")
/// - If dropped without touching a screw, it just disappears (returns to hardware area)
/// </summary>
public class ScrewDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Screw Settings")]
    [SerializeField] private Sprite screwSprite;

    [Header("Canvas Reference")]
    public Canvas canvas;

    private GameObject _dragObject;
    private bool _isDragging = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;

        _dragObject = new GameObject("ScrewDrag");
        _dragObject.tag = "Screw";

        // Sprite so it's visible
        SpriteRenderer sr = _dragObject.AddComponent<SpriteRenderer>();
        sr.sprite = screwSprite != null ? screwSprite : GetComponent<Image>().sprite;
        sr.sortingOrder = 100;

        // Collider for trigger detection with screw holes
        CircleCollider2D collider = _dragObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.3f;

        // Rigidbody required for trigger detection
        Rigidbody2D rb = _dragObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Initial position
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f)
        );
        worldPos.z = 0f;
        _dragObject.transform.position = worldPos;

        Debug.Log("[ScrewDrag] Started dragging screw");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || _dragObject == null) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f)
        );
        worldPos.z = 0f;
        _dragObject.transform.position = worldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Clean up if not already destroyed by ScrewController
        if (_dragObject != null)
        {
            Destroy(_dragObject);
            _dragObject = null;
            Debug.Log("[ScrewDrag] Screw returned to hardware area");
        }

        _isDragging = false;
    }
}