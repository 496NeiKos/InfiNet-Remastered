using UnityEngine;
using UnityEngine.EventSystems;

public class DragPrefab : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] public HardwareHolder hardwareHolder;
    [SerializeField] private GameObject workspaceProxy;
    [Tooltip("Human-readable name shown in the activity log. E.g. 'System Unit'. Auto-derived from the GameObject name if left blank.")]
    [SerializeField] public string displayName;

    public string LogDisplayName =>
        !string.IsNullOrEmpty(displayName) ? displayName : SplitCamelCase(name);

    private static string SplitCamelCase(string s) =>
        System.Text.RegularExpressions.Regex.Replace(s,
            @"(?<=[a-z\d])(?=[A-Z])|(?<=[A-Z]+)(?=[A-Z][a-z])", " ").Trim();
    [Tooltip("When false the object always snaps back and can never be placed in the workspace (e.g. thermal paste, towel cloth).")]
    [SerializeField] public bool canPlaceInWorkspace = true;

    private RectTransform workspaceArea;
    private RectTransform hardwareArea;
    private Canvas _workspaceCanvas;
    private Vector3 _originalPos;
    private Transform _originalParent;
    private Vector3 _originalLocalPos;
    private Vector3 _originalLocalScale;
    private bool _isDragging = false;
    private bool _wasInSlot = false;
    private SlotContainer _originalSlot;

    private SpriteRenderer _dragIndicator;
    private DragPrefab _redirectTarget;
    private Vector3 _grabOffset;

    private void Start()
    {
        workspaceArea = GameManager.Instance.workspaceArea;
        hardwareArea  = GameManager.Instance.hardwareArea;
        _workspaceCanvas = workspaceArea != null
            ? workspaceArea.GetComponentInParent<Canvas>()
            : null;
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        bool inWorldRoot = transform.parent == GameManager.Instance.ActiveWorldContainer;

        if (workspaceProxy != null)
        {
            if (workspaceProxy.activeSelf != inWorldRoot)
                workspaceProxy.SetActive(inWorldRoot);

            // Disable parent collider while proxy is active to avoid double-hits.
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = !inWorldRoot;
        }
        else if (inWorldRoot)
        {
            // No proxy — re-enable own collider in case slot state management disabled it.
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _redirectTarget = null;

        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (WalkthroughGuideManager.Instance != null && WalkthroughGuideManager.Instance.IsShowing) return;

        SlotContainer slot = GetComponentInParent<SlotContainer>();
        bool isInSlot = slot != null || GetComponentInParent<CPUSlotController>() != null;

        if (GameManager.Instance.IsEditorOpen && !isInSlot)
        {
            // This DragPrefab is a panel-level object (e.g. Motherboard in firstLayer).
            // A child GO without its own DragPrefab (e.g. an indicator overlay) was clicked,
            // and the EventSystem bubbled up here instead of reaching the correct child DragPrefab.
            // Find which child DragPrefab the cursor is actually over and forward the whole drag to it.
            Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(
                new Vector3(eventData.position.x, eventData.position.y, 0f));

            foreach (DragPrefab child in GetComponentsInChildren<DragPrefab>(true))
            {
                if (child == this || !child.enabled) continue;
                Collider2D col = child.GetComponent<Collider2D>()
                              ?? child.GetComponentInChildren<Collider2D>(true);
                if (col != null && col.enabled && col.OverlapPoint(mouseWorld))
                {
                    _redirectTarget = child;
                    child.OnBeginDrag(eventData);
                    return;
                }
            }

            Debug.Log($"[DragPrefab:{name}] BLOCKED — editor open but not in slot.");
            _isDragging = false;
            return;
        }

        var dvm = FindObjectOfType<DetailViewManager>();
        if (dvm != null && dvm.IsInnerPanelOpen)
        {
            ActivityLogManager.Log($"Cannot drag {LogDisplayName} — close the detail panel first.", ActivityLogManager.EntryType.Warning);
            Debug.Log($"[DragPrefab:{name}] BLOCKED — inner panel is open.");
            UnableAnimation.Shake(transform);
            _isDragging = false;
            return;
        }

        var mbdvm = FindObjectOfType<MotherboardDetailViewManager>();
        if (mbdvm != null && mbdvm.IsInnerPanelOpen)
        {
            ActivityLogManager.Log($"Cannot drag {LogDisplayName} — close the component panel first.", ActivityLogManager.EntryType.Warning);
            Debug.Log($"[DragPrefab:{name}] BLOCKED — component detail panel is open.");
            UnableAnimation.Shake(transform);
            _isDragging = false;
            return;
        }

        var cover = GetComponentInParent<CoverController>();
        if (cover != null && cover.IsSliding)
        {
            ActivityLogManager.Log($"Cannot drag {LogDisplayName} — wait for the cover to finish sliding.", ActivityLogManager.EntryType.Warning);
            Debug.Log($"[DragPrefab:{name}] BLOCKED — cover is sliding.");
            UnableAnimation.Shake(transform);
            _isDragging = false;
            return;
        }

        if (isInSlot && !AreAllScrewsEmpty())
        {
            ActivityLogManager.Log($"Cannot remove {LogDisplayName} — unscrew all screws first.", ActivityLogManager.EntryType.Warning);
            Debug.Log($"[DragPrefab:{name}] BLOCKED — screws not empty.");
            UnableAnimation.Shake(transform);
            _isDragging = false;
            return;
        }

        if (isInSlot && !AreAllCablesDetached())
        {
            ActivityLogManager.Log($"Cannot remove {LogDisplayName} — disconnect all cables first.", ActivityLogManager.EntryType.Warning);
            Debug.Log($"[DragPrefab:{name}] BLOCKED — cables not detached.");
            UnableAnimation.Shake(transform);
            _isDragging = false;
            return;
        }

        // Block CPU drag if lock is closed
        CPUSlotController cpuSlot = GetComponentInParent<CPUSlotController>();
        if (cpuSlot != null && isInSlot && GetComponent<CPUController>() != null && cpuSlot.IsLockClosed)
        {
            ActivityLogManager.Log("Cannot remove CPU — open the lock lever first.", ActivityLogManager.EntryType.Warning);
            Debug.Log($"[DragPrefab:{name}] BLOCKED — CPU lock is closed.");
            UnableAnimation.Shake(transform);
            _isDragging = false;
            return;
        }

        // Block CPU drag if heatsink is installed
        if (cpuSlot != null && isInSlot && cpuSlot.IsHeatsinkInstalled && GetComponent<CPUController>() != null)
        {
            ActivityLogManager.Log("Cannot remove CPU — remove the heatsink first.", ActivityLogManager.EntryType.Warning);
            Debug.Log($"[DragPrefab:{name}] BLOCKED — heatsink is still installed.");
            UnableAnimation.Shake(transform);
            _isDragging = false;
            return;
        }

        // Block Heatsink drag until its cable is disconnected
        HeatsinkController heatsink = GetComponent<HeatsinkController>();
        if (heatsink != null && isInSlot && !heatsink.CanBeRemoved)
        {
            ActivityLogManager.Log("Cannot remove Heatsink — disconnect the fan cable first.", ActivityLogManager.EntryType.Warning);
            Debug.Log($"[DragPrefab:{name}] BLOCKED — heatsink cable still connected.");
            UnableAnimation.Shake(transform);
            _isDragging = false;
            return;
        }

        // Block RAM drag while latch is engaged (must Slide-Up in detail view first)
        RAMController ram = GetComponent<RAMController>();
        if (ram != null && isInSlot && ram.IsInstalled)
        {
            ActivityLogManager.Log(
                "Cannot remove RAM — open the latch first: right-click the RAM in the Motherboard view, then slide UP.",
                ActivityLogManager.EntryType.Warning);
            Debug.Log($"[DragPrefab:{name}] BLOCKED — RAM latch is still engaged.");
            UnableAnimation.Shake(transform);
            _isDragging = false;
            return;
        }

        // Block GPU drag until latched out AND all screws empty AND cable detached
        GPUController gpu = GetComponent<GPUController>();
        if (gpu != null && isInSlot && gpu.IsLatched)
        {
            ActivityLogManager.Log("Cannot remove GPU — release the PCIe latch first.", ActivityLogManager.EntryType.Warning);
            Debug.Log($"[DragPrefab:{name}] BLOCKED — GPU is still latched.");
            UnableAnimation.Shake(transform);
            _isDragging = false;
            return;
        }

        // Block PSU drag until back port cable and mobo ATX cable are both disconnected
        PSUController psu = GetComponent<PSUController>();
        if (psu != null && isInSlot && !psu.CanBeRemoved)
        {
            ActivityLogManager.Log("Cannot remove PSU — disconnect all PSU cables and unscrew the mounting screws first.", ActivityLogManager.EntryType.Warning);
            Debug.Log($"[DragPrefab:{name}] BLOCKED — PSU cables still connected.");
            UnableAnimation.Shake(transform);
            _isDragging = false;
            return;
        }

        // Block HDD drag until its own cable and mobo HDD cable are both disconnected
        HDDController hdd = GetComponent<HDDController>();
        if (hdd != null && isInSlot && !hdd.CanBeRemoved)
        {
            ActivityLogManager.Log("Cannot remove HDD — disconnect all HDD cables first.", ActivityLogManager.EntryType.Warning);
            Debug.Log($"[DragPrefab:{name}] BLOCKED — HDD cables still connected.");
            UnableAnimation.Shake(transform);
            _isDragging = false;
            return;
        }

        // Block Motherboard drag until the GPU has been fully removed from its slot
        MotherboardController mb = GetComponent<MotherboardController>();
        if (mb != null && isInSlot)
        {
            MotherboardPhaseManager phase = GetComponent<MotherboardPhaseManager>();
            GPUPhase1CableInteraction gpuPhase1 = phase?.GetGPUPhase1CableInteraction();
            if (gpuPhase1 != null && gpuPhase1.GetComponentInParent<SlotContainer>() != null)
            {
                ActivityLogManager.Log("Cannot remove Motherboard — remove the GPU from its slot first.", ActivityLogManager.EntryType.Warning);
                Debug.Log($"[DragPrefab:{name}] BLOCKED — GPU must be removed before dragging the motherboard.");
                UnableAnimation.Shake(transform);
                _isDragging = false;
                return;
            }
        }

        // Layer 1 gate — applies to ALL SystemUnit hardware (Motherboard, HDD, PSU).
        // SU back VGA and PSU cables must both be unplugged before any hardware can be dragged.
        if (isInSlot && GetComponentInParent<SystemUnitController>() != null)
        {
            SystemUnitConditionChecker checker = GetComponentInParent<SystemUnitConditionChecker>();
            if (checker != null && !checker.IsHardwareInteractable())
            {
                ActivityLogManager.Log($"Cannot remove {LogDisplayName} — unplug the back panel cables first.", ActivityLogManager.EntryType.Warning);
                Debug.Log($"[DragPrefab:{name}] BLOCKED — SU back cables still connected.");
                UnableAnimation.Shake(transform);
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
            transform.SetParent(GameManager.Instance.ActiveWorldContainer, true);
            ApplyWorldScale(worldScale);
            GetComponent<RAMController>()?.OnRemovedFromSlot();
            GetComponent<GPUController>()?.OnRemovedFromSlot();
            GetComponent<HDDController>()?.OnRemovedFromSlot();
            GetComponent<SSDController>()?.OnRemovedFromSlot();
            GetComponent<MotherboardController>()?.OnRemovedFromSlot();
        }

        Vector3 grabMouseWorld = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        grabMouseWorld.z = 0f;
        _grabOffset = transform.position - grabMouseWorld;

        GameObject indicatorGO = new GameObject("DragIndicator");
        _dragIndicator = indicatorGO.AddComponent<SpriteRenderer>();
        _dragIndicator.sprite = GetComponent<SpriteRenderer>()?.sprite;
        _dragIndicator.sortingOrder = 999;
        indicatorGO.transform.position = transform.position;
        indicatorGO.transform.localScale = transform.lossyScale;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_redirectTarget != null) { _redirectTarget.OnDrag(eventData); return; }
        if (!_isDragging) return;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        worldPos.z = 0f;
        worldPos += _grabOffset;
        worldPos = ClampToWorkspace(worldPos, eventData.position, eventData.pressEventCamera);
        transform.position = worldPos;

        if (_dragIndicator != null)
            _dragIndicator.transform.position = worldPos;
    }

    // Clamps a world-space drag position so the object's collider stays inside whichever
    // valid zone the cursor is currently over — hardware area takes priority, otherwise
    // falls back to workspaceArea. Reads offsetMin/offsetMax for the workspace (same values
    // SimPanelLayoutManager manages) and GetWorldCorners for the hardware area so both
    // stay correct after panel toggles, camera pan, and zoom — every frame.
    private Vector3 ClampToWorkspace(Vector3 worldPos, Vector2 cursorScreenPos, Camera eventCamera)
    {
        if (workspaceArea == null || Camera.main == null) return worldPos;

        float   ppu = Screen.height / (2f * Camera.main.orthographicSize);
        Vector2 ext = GetActiveColliderExtents();

        // If the cursor is over the hardware area, clamp the object position to that region.
        if (hardwareArea != null &&
            RectTransformUtility.RectangleContainsScreenPoint(hardwareArea, cursorScreenPos, eventCamera))
        {
            // GetWorldCorners returns world-space positions. For Screen Space Camera canvases
            // those are NOT screen pixels, so we must convert via RectTransformUtility to get
            // actual screen-pixel coordinates before clamping against sp (which IS pixels).
            Canvas rootCanvas = hardwareArea.GetComponentInParent<Canvas>();
            if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;
            Camera uiCam = (rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                ? rootCanvas.worldCamera
                : null;

            Vector3[] hwCorners = new Vector3[4];
            hardwareArea.GetWorldCorners(hwCorners);
            // corners[0]=bottom-left, [2]=top-right after conversion to screen pixels
            Vector2 hwMin = RectTransformUtility.WorldToScreenPoint(uiCam, hwCorners[0]);
            Vector2 hwMax = RectTransformUtility.WorldToScreenPoint(uiCam, hwCorners[2]);

            float haL = hwMin.x;
            float haB = hwMin.y;
            float haR = hwMax.x;
            float haT = hwMax.y;

            Vector2 sp = Camera.main.WorldToScreenPoint(worldPos);
            float cx = Mathf.Clamp(sp.x, haL + ext.x * ppu, haR - ext.x * ppu);
            float cy = Mathf.Clamp(sp.y, haB + ext.y * ppu, haT - ext.y * ppu);

            Vector3 result = Camera.main.ScreenToWorldPoint(new Vector3(cx, cy, 10f));
            result.z = 0f;
            return result;
        }

        // Default: clamp to workspace bounds.
        // offsetMin/offsetMax are in canvas units; for Constant Pixel Size canvas sf=1
        // so canvas units == screen pixels. We use Screen.width/Height directly instead
        // of canvasRT.rect.width because the canvas root stores sizeDelta (0,0) in the
        // scene file and its rect can read as 0 before the Canvas drives it at runtime.
        float   sf   = _workspaceCanvas != null ? _workspaceCanvas.scaleFactor : 1f;
        Vector2 oMin = workspaceArea.offsetMin;
        Vector2 oMax = workspaceArea.offsetMax;

        float wsL = oMin.x * sf;
        float wsB = oMin.y * sf;
        float wsR = Screen.width  + oMax.x * sf;   // (Screen.width/sf  + oMax.x) * sf
        float wsT = Screen.height + oMax.y * sf;   // (Screen.height/sf + oMax.y) * sf

        Vector2 wsp = Camera.main.WorldToScreenPoint(worldPos);
        float cwx = Mathf.Clamp(wsp.x, wsL + ext.x * ppu, wsR - ext.x * ppu);
        float cwy = Mathf.Clamp(wsp.y, wsB + ext.y * ppu, wsT - ext.y * ppu);

        Vector3 res = Camera.main.ScreenToWorldPoint(new Vector3(cwx, cwy, 10f));
        res.z = 0f;
        return res;
    }

    // workspaceProxy's collider is active while the object is in the world container
    // (root collider is disabled to avoid double-hits — see Update).
    private Vector2 GetActiveColliderExtents()
    {
        Collider2D col = workspaceProxy != null
            ? workspaceProxy.GetComponent<Collider2D>()
            : null;
        if (col == null) col = GetComponent<Collider2D>();
        if (col != null) return new Vector2(col.bounds.extents.x, col.bounds.extents.y);

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) return new Vector2(sr.bounds.extents.x, sr.bounds.extents.y);

        return Vector2.zero;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_redirectTarget != null)
        {
            _redirectTarget.OnEndDrag(eventData);
            _redirectTarget = null;
            return;
        }

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
            ActivityLogManager.Log($"{LogDisplayName} returned to storage", ActivityLogManager.EntryType.Remove);

            // Cache slot ref before reparenting removes it from the hierarchy.
            // Use GetComponentInParent so an intermediate GO between CPUSlotController
            // and the Heatsink/CPU (e.g. after a prefab restructure) doesn't break the lookup.
            CPUSlotController cpuSlot = _originalParent?.GetComponent<CPUSlotController>()
                                     ?? _originalParent?.GetComponentInParent<CPUSlotController>();

            _originalSlot?.RemoveChild();
            GetComponent<MotherboardController>()?.MarkUninstalled();

            // Notify AFTER caching ref, BEFORE SendToHolder (which reparents to storage)
            if (GetComponent<HeatsinkController>() != null)
                GetComponent<HeatsinkController>().OnRemovedFromSlot(cpuSlot);
            else if (GetComponent<CPUController>() != null)
                cpuSlot?.OnCPUUninstalled();

            SendToHolder();
            NCIITaskListManager.CheckConditions();
        }
        else if (_wasInSlot)
        {
            // Snap back — notify slot that component is reinstalled
            CPUSlotController cpuSlot = _originalParent?.GetComponent<CPUSlotController>()
                                     ?? _originalParent?.GetComponentInParent<CPUSlotController>();
            if (GetComponent<HeatsinkController>() != null)
                GetComponent<HeatsinkController>().OnInstalledToSlot(cpuSlot);
            else if (GetComponent<CPUController>() != null)
                cpuSlot?.OnCPUInstalled();

            GetComponent<RAMController>()?.OnSnappedToSlot();
            GetComponent<GPUController>()?.OnSnappedToSlot();
            GetComponent<HDDController>()?.OnSnappedToSlot();
            GetComponent<SSDController>()?.OnSnappedToSlot();
            GetComponent<MotherboardController>()?.OnSnappedToSlot();

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
            if (!onWorkspace || !canPlaceInWorkspace)
                transform.position = _originalPos;
            else
                WalkthroughGuideManager.Instance?.TryTrigger(WalkthroughGuideManager.WalkthroughTrigger.FirstComponentDrop);
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

        foreach (var c in cableRoot.GetComponentsInChildren<CablePort>(true))
        {
            if (c.IsInstalled)
            {
                Debug.Log($"[DragPrefab:{name}] Cable blocking: {c.gameObject.name}");
                return false;
            }
        }

        // GPU cable must also be disconnected before the motherboard can be dragged out
        if (phase != null)
        {
            GPUPhase1CableInteraction gpuPhase1 = phase.GetGPUPhase1CableInteraction();
            if (gpuPhase1 != null)
            {
                foreach (var cs in gpuPhase1.GetComponentsInChildren<CablePort>(true))
                {
                    if (cs.IsInstalled)
                    {
                        Debug.Log($"[DragPrefab:{name}] GPU cable blocking: {cs.gameObject.name}");
                        return false;
                    }
                }
            }
        }

        return true;
    }
}