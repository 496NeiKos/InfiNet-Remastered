using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to CrimpingToolIcon in the HardwareArea.
/// The crimping tool has three distinct actions depending on where it is dropped:
///
///   1. Near the RJ45 slot   — crimps an installed, uncrimped RJ45.
///   2. Near the wires area  — trims the wires (equalizes length). Must be stripped first,
///                             and must be done before the RJ45 can be installed.
///   3. Near the cable body  — fully resets the cable end back to its intact state.
///                             Works from any state, even after crimping.
///
/// Returns to hardware area after every use.
/// </summary>
public class NetworkCrimpingToolIconDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
                                           IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tool Visuals")]
    [SerializeField] private Sprite toolSprite;
    [SerializeField] private float colliderRadius = 0.3f;

    [Header("Detection Radii")]
    [Tooltip("Proximity radius around the RJ45 slot position for the crimp action.")]
    [SerializeField] private float rj45CrimpRadius = 1.5f;
    [Tooltip("Proximity radius around the wires container position for the trim action.")]
    [SerializeField] private float wiresTrimRadius = 1.5f;
    [Tooltip("Proximity radius around the cable body position for the cut/reset action.")]
    [SerializeField] private float bodyResetRadius = 1.5f;

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

        // ── Priority 1: RJ45 slot zone — success and blocking feedback ───────

        // 1a. Crimpable RJ45 installed — crimp it.
        NetworkCableEndController crimpTarget = FindNearestCrimpableEnd(dropPos);
        if (crimpTarget != null)
        {
            crimpTarget.Crimp();
            ActivityLogManager.Log($"RJ45 successfully crimped — {crimpTarget.name}", ActivityLogManager.EntryType.Install);
            CrimpingFeedbackPopup.Instance?.ShowMessage("You have successfully crimped the RJ45.");
            return;
        }

        // 1b. RJ45 already crimped.
        NetworkCableEndController alreadyCrimped = FindNearestAlreadyCrimpedEnd(dropPos);
        if (alreadyCrimped != null)
        {
            CrimpingFeedbackPopup.Instance?.ShowMessage("RJ45 has already been crimped.");
            return;
        }

        // ── Priority 2: Wires zone — trim action must come before slot feedback ──

        // 2a. Stripped and not trimmed — trim the wires.
        NetworkCableEndController trimTarget = FindNearestTrimmableEnd(dropPos);
        if (trimTarget != null)
        {
            trimTarget.Trim();
            ActivityLogManager.Log($"Wires trimmed — {trimTarget.name}", ActivityLogManager.EntryType.Action);
            CrimpingFeedbackPopup.Instance?.ShowMessage("Internal wirings has been trimmed.");
            return;
        }

        // 2b. Already trimmed — wires zone drop with nothing left to trim.
        NetworkCableEndController alreadyTrimmed = FindNearestAlreadyTrimmedEnd(dropPos);
        if (alreadyTrimmed != null)
        {
            CrimpingFeedbackPopup.Instance?.ShowMessage("Already been trimmed.");
            return;
        }

        // ── Priority 3: Slot zone feedback (after wires zone is exhausted) ────

        // 3a. Trimmed but no RJ45 installed yet.
        NetworkCableEndController trimmedNoRJ45 = FindNearestTrimmedNoRJ45End(dropPos, useSlotRadius: true);
        if (trimmedNoRJ45 != null)
        {
            CrimpingFeedbackPopup.Instance?.ShowMessage("Install the RJ45 first to crimp.");
            return;
        }

        // 3b. Stripped but not yet trimmed — dropped near slot zone but missed wires zone.
        NetworkCableEndController strippedNotTrimmedSlot = FindNearestStrippedNotTrimmedEnd(dropPos, useSlotRadius: true);
        if (strippedNotTrimmedSlot != null)
        {
            CrimpingFeedbackPopup.Instance?.ShowMessage("Trim the wires first before installing the RJ45.");
            return;
        }

        // ── Priority 4: Body zone ─────────────────────────────────────────────

        // 4a. Intact cable — guide the user to strip first.
        NetworkCableEndController unstripped = FindNearestUnstrippedEnd(dropPos);
        if (unstripped != null)
        {
            CrimpingFeedbackPopup.Instance?.ShowMessage("Use wire stripper first to expose the inner wire.");
            return;
        }

        // 4b. Any stripped end near body — full reset.
        NetworkCableEndController resetTarget = FindNearestResettableEnd(dropPos);
        if (resetTarget != null)
        {
            resetTarget.ResetEnd();
            ActivityLogManager.Log($"Cable end reset to intact — {resetTarget.name}", ActivityLogManager.EntryType.Remove);
        }
    }

    // ── Detection helpers ─────────────────────────────────────────────────────

    private NetworkCableEndController FindNearestCrimpableEnd(Vector3 worldPos)
    {
        return FindNearest(worldPos, rj45CrimpRadius,
            e => e.IsStripped && e.IsTrimmed && e.IsRJ45Installed && !e.IsCrimped,
            e => e.SlotWorldPosition);
    }

    private NetworkCableEndController FindNearestAlreadyCrimpedEnd(Vector3 worldPos)
    {
        return FindNearest(worldPos, rj45CrimpRadius,
            e => e.IsRJ45Installed && e.IsCrimped,
            e => e.SlotWorldPosition);
    }

    private NetworkCableEndController FindNearestTrimmedNoRJ45End(Vector3 worldPos, bool useSlotRadius)
    {
        float radius = useSlotRadius ? rj45CrimpRadius : wiresTrimRadius;
        return FindNearest(worldPos, radius,
            e => e.IsStripped && e.IsTrimmed && !e.IsRJ45Installed,
            e => e.SlotWorldPosition);
    }

    private NetworkCableEndController FindNearestStrippedNotTrimmedEnd(Vector3 worldPos, bool useSlotRadius)
    {
        float radius = useSlotRadius ? rj45CrimpRadius : bodyResetRadius;
        return FindNearest(worldPos, radius,
            e => e.IsStripped && !e.IsTrimmed,
            e => e.SlotWorldPosition);
    }

    private NetworkCableEndController FindNearestTrimmableEnd(Vector3 worldPos)
    {
        return FindNearest(worldPos, wiresTrimRadius,
            e => e.IsStripped && !e.IsTrimmed,
            e => e.WiresWorldPosition);
    }

    private NetworkCableEndController FindNearestAlreadyTrimmedEnd(Vector3 worldPos)
    {
        return FindNearest(worldPos, wiresTrimRadius,
            e => e.IsStripped && e.IsTrimmed,
            e => e.WiresWorldPosition);
    }

    private NetworkCableEndController FindNearestUnstrippedEnd(Vector3 worldPos)
    {
        return FindNearest(worldPos, bodyResetRadius,
            e => !e.IsStripped,
            e => e.BodyWorldPosition);
    }

    private NetworkCableEndController FindNearestResettableEnd(Vector3 worldPos)
    {
        return FindNearest(worldPos, bodyResetRadius,
            e => e.IsStripped,
            e => e.BodyWorldPosition);
    }

    private NetworkCableEndController FindNearest(
        Vector3 worldPos,
        float radius,
        System.Func<NetworkCableEndController, bool> predicate,
        System.Func<NetworkCableEndController, Vector3> positionSelector)
    {
        NetworkCableEndController[] ends =
            FindObjectsByType<NetworkCableEndController>(FindObjectsSortMode.None);

        NetworkCableEndController closest = null;
        float bestDist = radius;

        foreach (var end in ends)
        {
            if (!end.gameObject.activeInHierarchy) continue;
            if (!predicate(end)) continue;
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
