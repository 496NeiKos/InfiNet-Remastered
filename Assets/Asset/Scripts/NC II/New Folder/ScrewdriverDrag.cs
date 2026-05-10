using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Special drag behavior for the screwdriver icon in the hardware area.
/// Unlike other hardware:
/// - Does NOT instantiate a prefab
/// - Creates a temporary drag object with a 2D collider (tagged "Screwdriver")
/// - When dragged over screws, the collider triggers ScrewController
/// - On drop: drag object is destroyed (screwdriver "returns" to hardware area)
/// </summary>
public class ScrewdriverDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Screwdriver Settings")]
    [SerializeField] private Sprite screwdriverSprite;

    [Header("Canvas Reference")]
    public Canvas canvas;

    private GameObject _dragObject;
    private bool _isDragging = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;

        // Create a temporary world-space screwdriver object with a collider
        _dragObject = new GameObject("ScrewdriverDrag");
        _dragObject.tag = "Screwdriver";

        // Add SpriteRenderer so it's visible
        SpriteRenderer sr = _dragObject.AddComponent<SpriteRenderer>();
        sr.sprite = screwdriverSprite != null ? screwdriverSprite : GetComponent<Image>().sprite;
        sr.sortingOrder = 100; // Draw on top of everything

        // Add a 2D collider so it can trigger screw collisions
        CircleCollider2D collider = _dragObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.3f;

        // Add Rigidbody2D (required for trigger detection)
        Rigidbody2D rb = _dragObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Set initial position
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f)
        );
        worldPos.z = 0f;
        _dragObject.transform.position = worldPos;

        Debug.Log("[ScrewdriverDrag] Started dragging screwdriver");
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
        if (_dragObject != null)
        {
            Destroy(_dragObject);
            _dragObject = null;
            Debug.Log("[ScrewdriverDrag] Screwdriver returned to hardware area");
        }

        _isDragging = false;
    }
}