using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drag behavior for the Screw icon in the hardware area.
/// Creates a temporary drag visual. On DROP, raycasts to find a
/// ScrewController in Empty state and places the screw there.
/// No longer relies on collision — only drop position matters.
/// </summary>
public class ScrewDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
                         IPointerEnterHandler, IPointerExitHandler
{
    [Header("Screw Settings")]
    [SerializeField] private Sprite screwSprite;

    [Header("Canvas Reference")]
    public Canvas canvas;

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
        HardwareInfoPanel.Instance?.Show(new[] { infoImage }, infoName, infoDescription);
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

        // Create a visual-only drag object (NO collider, NO tag)
        _dragObject = new GameObject("ScrewDrag");

        SpriteRenderer sr = _dragObject.AddComponent<SpriteRenderer>();
        sr.sprite = screwSprite != null ? screwSprite : GetComponent<Image>().sprite;
        sr.sortingOrder = 100;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f)
        );
        worldPos.z = 0f;
        _dragObject.transform.position = worldPos;
        _dragObject.transform.localScale = Vector3.one * ComputeWorldScale(sr.sprite);
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
        // Clean up drag visual
        if (_dragObject != null)
        {
            Destroy(_dragObject);
            _dragObject = null;
        }

        if (!_isDragging) return;
        _isDragging = false;

        // Raycast to find an empty ScrewController at drop position
        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null) continue;

            ScrewController screw = hit.collider.GetComponent<ScrewController>();
            if (screw != null && screw.TryPlaceScrew())
            {
                Debug.Log($"[ScrewDrag] Screw placed in {screw.name}");
                return;
            }
        }

        Debug.Log("[ScrewDrag] Screw returned to hardware area (no valid hole found)");
    }
}