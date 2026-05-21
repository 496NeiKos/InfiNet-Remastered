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
        bool isInSlot = slot != null || GetComponentInParent<CPUSlotController>() != null;

        if (GameManager.Instance.IsEditorOpen && !isInSlot)
        {
            Debug.Log($"[DragPrefab:{name}] BLOCKED — editor open but not in slot.");
            _isDragging = false;
            return;
        }

        var dvm = FindObjectOfType<DetailViewManager>();
        if (dvm != null && dvm.IsInnerPanelOpen)
        {
            Debug.Log($"[DragPrefab:{name}] BLOCKED — inner panel is open.");
            _isDragging = false;
            return;
        }

        var mbdvm = FindObjectOfType<MotherboardDetailViewManager>();
        if (mbdvm != null && mbdvm.IsInnerPanelOpen)
        {
            Debug.Log($"[DragPrefab:{name}] BLOCKED — component detail panel is open.");
            _isDragging = false;
            return;
        }

        var cover = GetComponentInParent<CoverController>();
        if (cover != null && cover.IsSliding)
        {
            Debug.Log($"[DragPrefab:{name}] BLOCKED — cover is sliding.");
            _isDragging = false;
            return;
        }

        if (isInSlot && !AreAllScrewsEmpty())
        {
            Debug.Log($"[DragPrefab:{name}] BLOCKED — screws not empty.");
            _isDragging = false;
            return;
        }

        if (isInSlot && !AreAllCablesDetached())
        {
            Debug.Log($"[DragPrefab:{name}] BLOCKED — cables not detached.");
            _isDragging = false;
            return;
        }

        // Block CPU drag if lock is closed
        CPUSlotController cpuSlot = GetComponentInParent<CPUSlotController>();
        if (cpuSlot != null && isInSlot && GetComponent<CPUController>() != null && cpuSlot.IsLockClosed)
        {
            Debug.Log($"[DragPrefab:{name}] BLOCKED — CPU lock is closed.");
            _isDragging = false;
            return;
        }

        // Block CPU drag if heatsink is installed
        if (cpuSlot != null && isInSlot && cpuSlot.IsHeatsinkInstalled && GetComponent<CPUController>() != null)
        {
            Debug.Log($"[DragPrefab:{name}] BLOCKED — heatsink is still installed.");
            _isDragging = false;
            return;
        }

        // Layer 1 gate — applies to ALL SystemUnit hardware (Motherboard, HDD, PSU).
        // SU back VGA and PSU cables must both be unplugged before any hardware can be dragged.
        if (isInSlot && GetComponentInParent<SystemUnitController>() != null)
        {
            SystemUnitConditionChecker checker = GetComponentInParent<SystemUnitConditionChecker>();
            if (checker != null && !checker.IsHardwareInteractable())
            {
                Debug.Log($"[DragPrefab:{name}] BLOCKED — SU back cables still connected.");
                _isDragging = false;
                return;
            }
        }

        Debug.Log($"[DragPrefab:{name}] Drag started. isInSlot={isInSlot}");
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
            // Cache slot ref before reparenting removes it from the hierarchy
            CPUSlotController cpuSlot = _originalParent?.GetComponent<CPUSlotController>();

            _originalSlot?.RemoveChild();
            GetComponent<MotherboardController>()?.MarkUninstalled();

            // Notify AFTER caching ref, BEFORE SendToHolder (which reparents to storage)
            if (GetComponent<HeatsinkController>() != null)
                GetComponent<HeatsinkController>().OnRemovedFromSlot(cpuSlot);
            else if (GetComponent<CPUController>() != null)
                cpuSlot?.OnCPUUninstalled();

            SendToHolder();
        }
        else if (_wasInSlot)
        {
            // Snap back — notify slot that component is reinstalled
            CPUSlotController cpuSlot = _originalParent?.GetComponent<CPUSlotController>();
            if (GetComponent<HeatsinkController>() != null)
                GetComponent<HeatsinkController>().OnInstalledToSlot(cpuSlot);
            else if (GetComponent<CPUController>() != null)
                cpuSlot?.OnCPUInstalled();

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
        // If this is a Motherboard, only check Phase1 screws — not heatsink screws in Phase2
        MotherboardPhaseManager phase = GetComponent<MotherboardPhaseManager>();
        Transform screwRoot = (phase != null) ? phase.GetPhase1Root() : transform;

        if (screwRoot == null) screwRoot = transform;

        foreach (var s in screwRoot.GetComponentsInChildren<ScrewController>(true))
        {
            if (!s.IsUnscrewed())
            {
                Debug.Log($"[DragPrefab:{name}] Screw blocking: {s.gameObject.name} state={s.GetState()}");
                return false;
            }
        }
        return true;
    }

    private bool AreAllCablesDetached()
    {
        // If this is a Motherboard, only check Phase1 cables — not any cables in Phase2
        MotherboardPhaseManager phase = GetComponent<MotherboardPhaseManager>();
        Transform cableRoot = (phase != null) ? phase.GetPhase1Root() : transform;

        if (cableRoot == null) cableRoot = transform;

        foreach (var c in cableRoot.GetComponentsInChildren<CableSlot>(true))
        {
            if (c.IsInstalled())
            {
                Debug.Log($"[DragPrefab:{name}] Cable blocking: {c.gameObject.name}");
                return false;
            }
        }
        return true;
    }
}