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

        // Add a 2D collider so it can trigger screw collisions.
        // Radius is 1/4 of the original 0.3f, offset to the bottom-left
        // so only the screwdriver tip activates screws (not the whole body).
        CircleCollider2D collider = _dragObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.075f;
        if (sr.sprite != null)
        {
            float halfW = sr.sprite.rect.width  / sr.sprite.pixelsPerUnit * 0.5f;
            float halfH = sr.sprite.rect.height / sr.sprite.pixelsPerUnit * 0.5f;
            collider.offset = new Vector2(-halfW, -halfH);
        }

        // Add Rigidbody2D (required for trigger detection)
        Rigidbody2D rb = _dragObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Set initial position
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f)
        );
        worldPos.z = 0f;
        _dragObject.transform.position = worldPos;
        _dragObject.transform.localScale = Vector3.one * ComputeWorldScale(sr.sprite);

        Debug.Log("[ScrewdriverDrag] Started dragging screwdriver");
    }

    private float ComputeWorldScale(Sprite s)
    {
        if (s == null || Camera.main == null) return 1f;
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) return 1f;
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float iconWorldHeight = Vector3.Distance(corners[0], corners[1]);
        float spriteWorldHeight = s.rect.height / s.pixelsPerUnit;
        return spriteWorldHeight > 0f ? iconWorldHeight / spriteWorldHeight : 1f;
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