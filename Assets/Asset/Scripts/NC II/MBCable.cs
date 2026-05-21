using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// On each cable child object inside a MB Phase 1 cable slot.
/// Hold 2s to detach, then drag to hardware area or snap back.
/// Same pattern as BackCable.
/// </summary>
public class MBCable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Identity")]
    [Tooltip("Must match parent CableSlot's cableType")]
    [SerializeField] private string cableType = "Cable1";

    [Header("Hardware Area Icon")]
    [SerializeField] private HardwareHolder hardwareHolder;

    [Header("Hold Settings")]
    [SerializeField] private float holdDuration = 2f;

    private CableSlot _parentSlot;
    private Transform _originalParent;
    private Vector3 _originalLocalPos;
    private Vector3 _originalLocalScale;

    private float _holdTimer = 0f;
    private bool _isHolding = false;
    private bool _detached = false;
    private bool _isDragging = false;

    private SpriteRenderer _dragIndicator;

    private void Start()
    {
        _parentSlot = GetComponentInParent<CableSlot>();
    }

    private void Update()
    {
        if (_detached) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.isPressed && IsMouseOver())
        {
            _isHolding = true;
            _holdTimer += Time.deltaTime;

            if (_holdTimer >= holdDuration)
                Detach();
        }
        else
        {
            _isHolding = false;
            _holdTimer = 0f;
        }
    }

    private void Detach()
    {
        _isHolding = false;
        _holdTimer = 0f;
        _detached = true;

        _originalParent = transform.parent;
        _originalLocalPos = transform.localPosition;
        _originalLocalScale = transform.localScale;

        _parentSlot?.SetUninstalled();

        // Capture world scale before reparenting — siblings are unaffected
        Vector3 worldLossy = transform.lossyScale;

        // worldPositionStays=true preserves world position
        transform.SetParent(GameManager.Instance.worldRoot, true);

        // WorldRoot scale is (1,1,1) so localScale == world scale
        transform.localScale = worldLossy;

        Debug.Log($"[MBCable] {cableType} detached — now draggable.");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!_detached) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        _isDragging = true;

        GameObject go = new GameObject("MBCableDrag");
        _dragIndicator = go.AddComponent<SpriteRenderer>();
        _dragIndicator.sprite = GetComponent<SpriteRenderer>()?.sprite;
        _dragIndicator.sortingOrder = 999;
        go.transform.localScale = transform.lossyScale;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        worldPos.z = 0f;
        transform.position = worldPos;

        if (_dragIndicator != null)
            _dragIndicator.transform.position = worldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragIndicator != null)
        {
            Destroy(_dragIndicator.gameObject);
            _dragIndicator = null;
        }

        if (!_isDragging) return;
        _isDragging = false;

        bool onHardwareArea = RectTransformUtility.RectangleContainsScreenPoint(
            GameManager.Instance.hardwareArea, eventData.position, eventData.pressEventCamera);

        if (onHardwareArea)
        {
            SendToHolder();
            Debug.Log($"[MBCable] {cableType} stored to hardware area.");
        }
        else
        {
            SnapBack();
            Debug.Log($"[MBCable] {cableType} snapped back to slot.");
        }
    }

    public void InstallToSlot(CableSlot slot)
    {
        _parentSlot = slot;
        _detached = false;

        transform.SetParent(slot.transform, false);
        transform.localPosition = _originalLocalPos;
        transform.localScale = _originalLocalScale;
        gameObject.SetActive(true);

        slot.SetInstalled();
        Debug.Log($"[MBCable] {cableType} installed to slot.");
    }

    private void SnapBack()
    {
        _detached = false;
        transform.SetParent(_originalParent, false);
        transform.localPosition = _originalLocalPos;
        transform.localScale = _originalLocalScale;
        _parentSlot?.SetInstalled();
    }

    private void SendToHolder()
    {
        if (hardwareHolder != null)
        {
            hardwareHolder.StoreHardware();
            return;
        }

        HardwareHolder[] all = FindObjectsOfType<HardwareHolder>(true);
        foreach (HardwareHolder h in all)
        {
            if (h.hardwarePrefab != null && h.hardwarePrefab.name == gameObject.name)
            {
                hardwareHolder = h;
                h.StoreHardware();
                return;
            }
        }

        Debug.LogWarning($"[MBCable] No HardwareHolder found for '{gameObject.name}'.");
        gameObject.SetActive(false);
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D col = GetComponent<Collider2D>();
        return col != null && col.OverlapPoint(mouseWorld);
    }

    public string GetCableType() => cableType;
}