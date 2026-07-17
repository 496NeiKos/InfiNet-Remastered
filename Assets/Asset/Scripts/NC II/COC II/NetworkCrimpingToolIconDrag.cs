using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to CrimpingToolIcon in the HardwareArea.
/// Drag onto a cable end that has an installed (uncrimped) RJ45 to crimp it.
/// Crimping locks the RJ45 permanently — only a wire stripper reset can undo it.
/// Returns to hardware area after every use.
/// </summary>
public class NetworkCrimpingToolIconDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
                                           IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tool Visuals")]
    [SerializeField] private Sprite toolSprite;
    [SerializeField] private float colliderRadius = 0.3f;

    [Header("Detection")]
    [SerializeField] private float dropRadius = 1.5f;

    [Header("Info Panel")]
    [SerializeField] private Sprite infoImage;
    [SerializeField] private string infoName;
    [TextArea(3, 6)]
    [SerializeField] private string infoDescription;

    private Coroutine _hoverCoroutine;
    private GameObject _dragObject;
    private bool _isDragging;

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

        _dragObject = new GameObject("CrimpingToolDrag");

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
        _isDragging = false;

        if (_dragObject == null) return;

        Vector3 dropPos = _dragObject.transform.position;
        Destroy(_dragObject);
        _dragObject = null;

        NetworkCableEndController hit = FindNearestCrimpableEnd(dropPos);
        if (hit == null) return;

        hit.Crimp();
    }

    private NetworkCableEndController FindNearestCrimpableEnd(Vector3 worldPos)
    {
        NetworkCableEndController[] ends =
            FindObjectsByType<NetworkCableEndController>(FindObjectsSortMode.None);

        NetworkCableEndController closest = null;
        float bestDist = dropRadius;

        foreach (var end in ends)
        {
            if (!end.gameObject.activeInHierarchy) continue;
            if (!end.IsStripped || !end.IsRJ45Installed || end.IsCrimped) continue;
            float dist = Vector3.Distance(worldPos, end.transform.position);
            if (dist < bestDist) { bestDist = dist; closest = end; }
        }

        return closest;
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
}
