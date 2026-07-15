using System.Collections;
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
public class ToolDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
                        IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tool Settings")]
    [SerializeField] private string toolTag = "ThermalPaste";
    [SerializeField] private Sprite toolSprite;
    [SerializeField] private float colliderRadius = 0.3f;

    [Header("Info Panel")]
    [SerializeField] private Sprite infoImage;
    [SerializeField] private string infoName;
    [TextArea(3, 6)]
    [SerializeField] private string infoDescription;

    private Coroutine _hoverCoroutine;
    private GameObject _dragObject;
    private bool _isDragging = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(infoName)) return;
        _hoverCoroutine = StartCoroutine(ShowInfoAfterDelay());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CancelHover();
    }

    private IEnumerator ShowInfoAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        HardwareInfoPanel.Instance?.Show(infoImage, infoName, infoDescription);
        _hoverCoroutine = null;
    }

    private void CancelHover()
    {
        if (_hoverCoroutine != null)
        {
            StopCoroutine(_hoverCoroutine);
            _hoverCoroutine = null;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        CancelHover();
        HardwareInfoPanel.Instance?.Hide();

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
        _dragObject.transform.localScale = Vector3.one * ComputeWorldScale(sr.sprite);

        Debug.Log($"[ToolDrag] Started dragging {toolTag}");
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