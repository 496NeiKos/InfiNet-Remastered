using UnityEngine;
using UnityEngine.EventSystems;

public class DragPrefab : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] public HardwareHolder hardwareHolder;

    private RectTransform workspaceArea;
    private RectTransform hardwareArea;
    private Vector3 _originalPos;
    private Transform _originalParent;
    private Vector3 _originalLocalPos;
    private Vector3 _originalLocalScale;
    private bool _isDragging = false;
    private bool _wasInSlot = false;
    private SlotContainer _originalSlot;

    private SpriteRenderer _dragIndicator;

    private void Start()
    {
        workspaceArea = GameManager.Instance.workspaceArea;
        hardwareArea = GameManager.Instance.hardwareArea;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        SlotContainer slot = GetComponentInParent<SlotContainer>();
        bool isInSlot = slot != null;

        if (GameManager.Instance.IsEditorOpen && !isInSlot)
        {
            _isDragging = false;
            return;
        }

        var dvm = FindObjectOfType<DetailViewManager>();
        if (dvm != null && dvm.IsInnerPanelOpen)
        {
            _isDragging = false;
            return;
        }

        var cover = GetComponentInParent<CoverController>();
        if (cover != null && cover.IsSliding)
        {
            _isDragging = false;
            return;
        }

        if (isInSlot && !AreAllScrewsEmpty())
        {
            _isDragging = false;
            return;
        }

        if (isInSlot && !AreAllCablesDetached())
        {
            _isDragging = false;
            return;
        }

        // Gate: Motherboard/HDD/PSU inside SystemUnit require power off + back cables unplugged
        if (isInSlot && GetComponentInParent<SystemUnitController>() != null)
        {
            SystemUnitConditionChecker checker = GetComponentInParent<SystemUnitConditionChecker>();
            if (checker != null && !checker.IsHardwareInteractable())
            {
                Debug.Log("[DragPrefab] Hardware locked — power still on or back cables still connected.");
                _isDragging = false;
                return;
            }
        }

        _isDragging = true;
        _originalPos = transform.position;
        _originalParent = transform.parent;
        _originalLocalPos = transform.localPosition;
        _originalLocalScale = transform.localScale;
        _wasInSlot = isInSlot;
        _originalSlot = slot;

        if (_wasInSlot)
        {
            Vector3 worldScale = transform.lossyScale;
            transform.SetParent(GameManager.Instance.worldRoot, true);
            ApplyWorldScale(worldScale);
        }

        GameObject indicatorGO = new GameObject("DragIndicator");
        _dragIndicator = indicatorGO.AddComponent<SpriteRenderer>();
        _dragIndicator.sprite = GetComponent<SpriteRenderer>()?.sprite;
        _dragIndicator.sortingOrder = 999;
        indicatorGO.transform.localScale = transform.lossyScale;
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
            hardwareArea, eventData.position, eventData.pressEventCamera);

        if (_wasInSlot && onHardwareArea)
        {
            _originalSlot?.RemoveChild();
            GetComponent<MotherboardController>()?.MarkUninstalled();
            SendToHolder();
        }
        else if (_wasInSlot)
        {
            transform.SetParent(_originalParent, false);
            transform.localPosition = _originalLocalPos;
            transform.localScale = _originalLocalScale;
        }
        else if (onHardwareArea)
        {
            SendToHolder();
        }
        else
        {
            bool onWorkspace = RectTransformUtility.RectangleContainsScreenPoint(
                workspaceArea, eventData.position, eventData.pressEventCamera);
            if (!onWorkspace)
                transform.position = _originalPos;
        }
    }

    private void SendToHolder()
    {
        if (hardwareHolder != null)
        {
            hardwareHolder.StoreHardware();
            return;
        }

        HardwareHolder[] allHolders = FindObjectsOfType<HardwareHolder>(true);
        foreach (HardwareHolder h in allHolders)
        {
            if (h.hardwarePrefab != null && h.hardwarePrefab.name == gameObject.name)
            {
                hardwareHolder = h;
                h.StoreHardware();
                return;
            }
        }

        Debug.LogWarning($"[DragPrefab] hardwareHolder not found for '{gameObject.name}' — deactivating in place.");
        gameObject.SetActive(false);
    }

    private void ApplyWorldScale(Vector3 targetWorldScale)
    {
        Vector3 ls = transform.lossyScale;
        transform.localScale = new Vector3(
            targetWorldScale.x / (ls.x == 0 ? 1 : ls.x),
            targetWorldScale.y / (ls.y == 0 ? 1 : ls.y),
            targetWorldScale.z / (ls.z == 0 ? 1 : ls.z));
    }

    private bool AreAllScrewsEmpty()
    {
        foreach (var s in GetComponentsInChildren<ScrewController>(true))
            if (!s.IsUnscrewed()) return false;
        return true;
    }

    private bool AreAllCablesDetached()
    {
        foreach (var c in GetComponentsInChildren<CableSlot>(true))
            if (c.IsInstalled()) return false;
        return true;
    }
}