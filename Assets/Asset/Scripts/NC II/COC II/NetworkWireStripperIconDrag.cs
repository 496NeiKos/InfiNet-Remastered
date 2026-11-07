using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to WireStripperIcon in the HardwareArea.
/// Drag onto a cable end body or head to strip the jacket and expose the 8 inner wires.
///
/// Drop targets and results:
///   - Body or head area, cable intact  → Expose() (strip the jacket)
///   - Body area, already stripped      → Feedback: "Cable jacket has already been stripped."
///   - Wires area, already stripped     → Feedback: "Cable jacket stripped, internal wires shown."
///
/// Returns to hardware area after every use.
/// </summary>
public class NetworkWireStripperIconDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
                                           IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tool Visuals")]
    [SerializeField] private Sprite toolSprite;
    [SerializeField] private float colliderRadius = 0.3f;

    [Header("Detection")]
    [Tooltip("Radius used to detect the cable end (body/head zone) for stripping.")]
    [SerializeField] private float dropRadius = 1.5f;
    [Tooltip("Radius used to detect the wires container area for the 'already stripped' feedback.")]
    [SerializeField] private float wiresRadius = 1.5f;

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

        _dragObject = new GameObject("WireStripperDrag");

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

        // Check body/head zone first (uses cable end root position — covers both since they're adjacent).
        NetworkCableEndController bodyHit = FindNearestCableEnd(dropPos, e => e.BodyWorldPosition, dropRadius);

        if (bodyHit != null)
        {
            if (!bodyHit.IsStripped)
            {
                bodyHit.Expose();
                ActivityLogManager.Log($"Cable jacket stripped — {bodyHit.name}", ActivityLogManager.EntryType.Action);
            }
            else
            {
                CrimpingFeedbackPopup.Instance?.ShowMessage("Cable jacket has already been stripped.");
            }
            return;
        }

        // Check wires zone — only reachable when already stripped (head is hidden, wires are shown).
        NetworkCableEndController wiresHit = FindNearestCableEnd(dropPos, e => e.WiresWorldPosition, wiresRadius);

        if (wiresHit != null && wiresHit.IsStripped)
        {
            CrimpingFeedbackPopup.Instance?.ShowMessage("Cable jacket stripped, internal wires shown.");
        }
    }

    private NetworkCableEndController FindNearestCableEnd(
        Vector3 worldPos,
        System.Func<NetworkCableEndController, Vector3> positionSelector,
        float radius)
    {
        NetworkCableEndController[] ends =
            FindObjectsByType<NetworkCableEndController>(FindObjectsSortMode.None);

        NetworkCableEndController closest = null;
        float bestDist = radius;

        foreach (var end in ends)
        {
            if (!end.gameObject.activeInHierarchy) continue;
            float dist = Vector3.Distance(worldPos, positionSelector(end));
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
