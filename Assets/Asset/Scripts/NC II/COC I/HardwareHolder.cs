using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HardwareHolder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
                              IPointerDownHandler
{
    public enum DropTargetMode { Slot, RaycastScrew }

    [Header("Hardware Reference")]
    public GameObject hardwarePrefab;

    [Header("Multi-Cable Group (optional)")]
    [Tooltip("All cable ends this proxy represents. When populated the holder manages a group instead of a single hardwarePrefab. "
           + "Proxy hides only when every cable is installed; shows when any cable is in storage or detached.")]
    [SerializeField] private List<CableBehavior> managedCables = new List<CableBehavior>();

    [Tooltip("Cable type string matched against CablePort.acceptedCableTypes when installing from this proxy. "
           + "Required when managedCables is populated.")]
    [SerializeField] private string proxyCableType;

    [Header("Spawn in Workspace")]
    [Tooltip("When true the prefab starts active in the workspace and the holder stays hidden.")]
    [SerializeField] private bool startInWorkspace = false;

    [Header("Drag Mode")]
    [Tooltip("When enabled: the drag object is spawned with a tag + collider immediately on drag start "
           + "(screwdriver / thermal paste style). On drop the object is destroyed — tool always returns to storage.")]
    [SerializeField] private bool immediateSpawnOnDrag = false;

    [Header("Immediate Spawn Settings")]
    [Tooltip("Tag assigned to the live drag object so world colliders can react (e.g. 'Screwdriver', 'ThermalPaste', 'TowelCloth').")]
    [SerializeField] private string spawnTag = "";
    [SerializeField] private float colliderRadius = 0.3f;
    [Tooltip("Offset of the trigger collider relative to the drag object centre (world units). "
           + "Use a negative X/Y to shift towards the tip of the tool sprite.")]
    [SerializeField] private Vector2 colliderOffset = Vector2.zero;

    [Header("Drop Target")]
    [Tooltip("Slot = standard proximity install. RaycastScrew = raycasts on drop to find a ScrewController hole.")]
    [SerializeField] private DropTargetMode dropTargetMode = DropTargetMode.Slot;

    [Header("Tool / Screw Visual")]
    [Tooltip("Sprite shown while dragging. For multi-cable holders set this to the cable's icon sprite.")]
    [SerializeField] private Sprite dragSprite;

    [Header("Slot Install Proximity (world units)")]
    public float slotInstallRadius = 1.5f;

    [Header("Info Panel")]
    [SerializeField] private Sprite[] infoImages;
    [SerializeField] private string infoName;
    [TextArea(3, 6)]
    [SerializeField] private string infoDescription;

    private const float ClickWindow = 0.5f;
    private int _clickCount;
    private float _lastClickTime;

    private GameObject _dragIndicator;
    private bool _isDragging = false;
    private Vector3 _worldScale;
    private Vector3 _originalLocalScale;
    private Vector3 _grabOffset;

    private bool IsToolMode => immediateSpawnOnDrag || dropTargetMode == DropTargetMode.RaycastScrew;

    // True when this holder manages a group of cable ends instead of a single hardwarePrefab.
    public bool IsMultiCableMode => managedCables != null && managedCables.Count > 0;

    // Returns true if the given cable is in this holder's managed list.
    public bool ContainsManagedCable(CableBehavior cable) =>
        managedCables != null && managedCables.Contains(cable);

    private void Start()
    {
        if (IsToolMode)
        {
            gameObject.SetActive(true);
            return;
        }

        if (IsMultiCableMode)
        {
            // Derive drag-indicator world scale from the first active (installed) cable.
            var firstActive = managedCables.FirstOrDefault(c => c != null && c.gameObject.activeSelf);
            _worldScale = firstActive != null ? firstActive.transform.lossyScale : Vector3.one;

            UpdateProxyVisibility();
            return;
        }

        if (hardwarePrefab != null)
        {
            _worldScale = hardwarePrefab.transform.lossyScale;
            _originalLocalScale = hardwarePrefab.transform.localScale;

            if (!startInWorkspace)
            {
                bool isCable = hardwarePrefab.GetComponent<CableBehavior>() != null;
                bool isSlotSibling = hardwarePrefab.GetComponent<HeatsinkController>() != null
                                  || hardwarePrefab.GetComponent<CPUController>() != null;

                if (!isCable && !isSlotSibling)
                    hardwarePrefab.SetActive(false);
            }
        }

        bool prefabInactive = hardwarePrefab == null || !hardwarePrefab.activeSelf;
        gameObject.SetActive(prefabInactive);
    }

    // ── Multi-Cable Proxy Visibility ──────────────────────────────────────

    // Show proxy when at least one managed cable is not installed at a port
    // (either stored/inactive, or mid-drag/detached). Hide only when all are installed.
    public void UpdateProxyVisibility()
    {
        if (!IsMultiCableMode) return;
        bool anyUnavailable = managedCables.Any(c => c == null || !c.gameObject.activeSelf || c.IsDetached);
        gameObject.SetActive(anyUnavailable);
    }

    // Called by CableBehavior.InstallToPort — re-evaluates whether all cables are now installed.
    public void OnCableInstalled(CableBehavior cable)
    {
        if (IsMultiCableMode)
            UpdateProxyVisibility();
        else
            gameObject.SetActive(false);
    }

    // Called by CableBehavior.SendToHolder — deactivates the specific cable end and updates visibility.
    public void OnCableStored(CableBehavior cable)
    {
        if (cable == null) return;
        cable.transform.SetParent(GameManager.Instance.ActiveHardwareStorageContainer, true);
        cable.gameObject.SetActive(false);
        if (IsMultiCableMode)
            UpdateProxyVisibility();
        else
            gameObject.SetActive(true);
    }

    // ── Info Panel (double right-click) ───────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(infoName)) return;
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (WalkthroughGuideManager.Instance != null && WalkthroughGuideManager.Instance.IsShowing) return;

        if (Time.unscaledTime - _lastClickTime > ClickWindow)
            _clickCount = 0;

        _lastClickTime = Time.unscaledTime;
        _clickCount++;

        if (_clickCount >= 2)
        {
            _clickCount = 0;
            HardwareInfoPanel.Instance?.Show(infoImages, infoName, infoDescription);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────

    public bool IsAvailable()
    {
        if (IsToolMode) return true;
        if (IsMultiCableMode)
            return managedCables.Any(c => c != null && (!c.gameObject.activeSelf || c.IsDetached));
        return hardwarePrefab != null && !hardwarePrefab.activeSelf;
    }

    public void StoreHardware()
    {
        if (IsToolMode) return;
        if (hardwarePrefab == null) return;
        hardwarePrefab.transform.SetParent(GameManager.Instance.ActiveHardwareStorageContainer, true);
        hardwarePrefab.SetActive(false);
        gameObject.SetActive(true);
    }

    // ── Drag ──────────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsAvailable()) return;
        if (WalkthroughGuideManager.Instance != null && WalkthroughGuideManager.Instance.IsShowing) return;

        _clickCount = 0;
        _isDragging = true;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        worldPos.z = 0f;

        if (immediateSpawnOnDrag)
        {
            _grabOffset = Vector3.zero;

            Sprite sprite = dragSprite != null ? dragSprite : GetComponent<Image>()?.sprite;

            _dragIndicator = new GameObject("ImmediateDrag_" + spawnTag);
            if (!string.IsNullOrEmpty(spawnTag))
                _dragIndicator.tag = spawnTag;

            SpriteRenderer sr = _dragIndicator.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 999;

            CircleCollider2D col = _dragIndicator.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = colliderRadius;
            col.offset = colliderOffset;

            Rigidbody2D rb = _dragIndicator.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            _dragIndicator.transform.position = worldPos;
            _dragIndicator.transform.localScale = Vector3.one * ComputeWorldScale(sprite);
        }
        else if (dropTargetMode == DropTargetMode.RaycastScrew)
        {
            _grabOffset = GetIconWorldCenter() - worldPos;

            Sprite sprite = dragSprite != null ? dragSprite : GetComponent<Image>()?.sprite;

            _dragIndicator = new GameObject("ScrewDragIndicator");

            SpriteRenderer sr = _dragIndicator.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 999;

            _dragIndicator.transform.position = worldPos + _grabOffset;
            _dragIndicator.transform.localScale = Vector3.one * ComputeWorldScale(sprite);
        }
        else
        {
            _grabOffset = GetIconWorldCenter() - worldPos;

            _dragIndicator = new GameObject("DragIndicator");

            SpriteRenderer sr = _dragIndicator.AddComponent<SpriteRenderer>();
            // Unity fake-null won't survive ?. — use explicit != null guard.
            // For multi-cable holders hardwarePrefab is unassigned; fall back to dragSprite.
            Sprite indicatorSprite = (hardwarePrefab != null)
                ? hardwarePrefab.GetComponent<SpriteRenderer>()?.sprite
                : null;
            if (indicatorSprite == null) indicatorSprite = dragSprite;
            sr.sprite = indicatorSprite;
            sr.sortingOrder = 999;

            _dragIndicator.transform.position = worldPos + _grabOffset;
            _dragIndicator.transform.localScale = _worldScale;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || _dragIndicator == null) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        worldPos.z = 0f;
        _dragIndicator.transform.position = worldPos + _grabOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragIndicator != null) { Destroy(_dragIndicator); _dragIndicator = null; }
        if (!_isDragging) return;
        _isDragging = false;

        if (immediateSpawnOnDrag) return;

        if (dropTargetMode == DropTargetMode.RaycastScrew)
        {
            Ray ray = Camera.main.ScreenPointToRay(eventData.position);
            RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null) continue;
                ScrewController screw = hit.collider.GetComponent<ScrewController>();
                if (screw != null && screw.TryPlaceScrew())
                {
                    Debug.Log($"[HardwareHolder] Screw placed in {screw.name}");
                    return;
                }
            }

            Debug.Log("[HardwareHolder] Screw returned to hardware area (no valid hole found)");
            return;
        }

        // Cables (both single and multi-cable) only install when the editor is open.
        bool isCable = IsMultiCableMode ||
                       (hardwarePrefab != null && hardwarePrefab.GetComponent<CableBehavior>() != null);
        if (isCable)
        {
            if (GameManager.Instance.IsEditorOpen)
                TryInstallInSlot(eventData);
            return;
        }

        if (GameManager.Instance.IsEditorOpen)
        {
            TryInstallInSlot(eventData);
            return;
        }

        bool onWorkspace = RectTransformUtility.RectangleContainsScreenPoint(
            GameManager.Instance.workspaceArea, eventData.position, eventData.pressEventCamera);
        if (!onWorkspace) return;

        Vector3 dropPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        dropPos.z = 0f;

        hardwarePrefab.transform.SetParent(GameManager.Instance.ActiveWorldContainer, false);
        hardwarePrefab.transform.position = dropPos;
        ApplyWorldScale(hardwarePrefab.transform, _worldScale);
        hardwarePrefab.SetActive(true);

        WalkthroughGuideManager.Instance?.TryTrigger(WalkthroughGuideManager.WalkthroughTrigger.FirstComponentDrop);

        bool isMajorHardware = hardwarePrefab.GetComponent<SystemUnitController>() != null
                            || hardwarePrefab.GetComponent<MonitorController>()    != null
                            || hardwarePrefab.GetComponent<AVRController>()        != null;
        if (isMajorHardware)
            WalkthroughGuideManager.Instance?.TryTrigger(
                WalkthroughGuideManager.WalkthroughTrigger.MajorHardwareDeployed);

        DragPrefab dp = hardwarePrefab.GetComponent<DragPrefab>();
        if (dp != null) dp.enabled = true;

        gameObject.SetActive(false);
    }

    // ── Slot Install ──────────────────────────────────────────────────────

    private void TryInstallInSlot(PointerEventData eventData)
    {
        Vector3 dropWorldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        dropWorldPos.z = 0f;

        // ── Multi-cable path ──────────────────────────────────────────────
        if (IsMultiCableMode)
        {
            CablePort[] allPorts = FindObjectsOfType<CablePort>(true);

            CablePort bestPort = null;
            float bestDist = float.MaxValue;

            foreach (CablePort port in allPorts)
            {
                if (!port.gameObject.activeInHierarchy) continue;
                if (!port.IsUninstalled) continue;
                if (!port.CanAcceptCable(proxyCableType)) continue;
                float dist = Vector3.Distance(port.transform.position, dropWorldPos);
                if (dist < slotInstallRadius && dist < bestDist) { bestDist = dist; bestPort = port; }
            }

            if (bestPort == null)
            {
                Debug.Log($"[HardwareHolder] Multi-cable proxy '{name}' (type='{proxyCableType}') — no valid port in range.");
                return;
            }

            // Pick the stored cable whose original home port is closest to the target port.
            // This preserves each cable end's intended sprite and scale at its matching port.
            CableBehavior bestCable = null;
            float bestCableDist = float.MaxValue;

            foreach (CableBehavior candidate in managedCables)
            {
                if (candidate == null) continue;
                // Skip cables that are already installed at a port or currently mid-drag.
                if (candidate.gameObject.activeSelf && !candidate.IsDetached) continue;
                // Skip cables that are mid-drag (floating in world — not available from storage).
                if (candidate.IsDetached) continue;

                float dist = Vector3.Distance(candidate.OriginalHomePortPosition, bestPort.transform.position);
                if (dist < bestCableDist) { bestCableDist = dist; bestCable = candidate; }
            }

            if (bestCable == null)
            {
                Debug.Log($"[HardwareHolder] Multi-cable proxy '{name}' — no stored cable available to install.");
                return;
            }

            bestCable.gameObject.SetActive(true);
            bestCable.InstallToPort(bestPort);
            NCIITaskListManager.CheckConditions();
            T2TaskListManager.CheckConditions();
            Debug.Log($"[HardwareHolder] Multi-cable proxy '{name}': installed '{bestCable.name}' to '{bestPort.name}' (dist={bestDist:F2}).");
            return;
        }

        // ── Legacy single-prefab path ─────────────────────────────────────
        if (hardwarePrefab == null) return;

        string prefabName = hardwarePrefab.name;
        DragPrefab dp = hardwarePrefab.GetComponent<DragPrefab>();
        string prefabDisplay = dp != null ? dp.LogDisplayName : prefabName;

        CableBehavior cable = hardwarePrefab.GetComponent<CableBehavior>();
        if (cable != null)
        {
            string cableType = cable.GetCableType();
            CablePort[] allPorts = FindObjectsOfType<CablePort>(true);

            CablePort bestPort = null;
            float bestDist = float.MaxValue;
            var rejectedActive    = new System.Collections.Generic.List<string>();
            var rejectedInstalled = new System.Collections.Generic.List<string>();
            var rejectedType      = new System.Collections.Generic.List<string>();
            var rejectedDist      = new System.Collections.Generic.List<string>();

            foreach (CablePort port in allPorts)
            {
                if (!port.gameObject.activeInHierarchy) { rejectedActive.Add(port.name); continue; }
                if (!port.IsUninstalled) { rejectedInstalled.Add(port.name); continue; }
                if (!port.CanAcceptCable(cableType)) { rejectedType.Add(port.name); continue; }
                float dist = Vector3.Distance(port.transform.position, dropWorldPos);
                if (dist >= slotInstallRadius) { rejectedDist.Add($"{port.name}@{dist:F2}"); continue; }
                if (dist < bestDist) { bestDist = dist; bestPort = port; }
            }

            if (bestPort != null)
            {
                hardwarePrefab.SetActive(true);
                cable.InstallToPort(bestPort);
                gameObject.SetActive(false);
                Debug.Log($"[HardwareHolder] {prefabName} installed to {bestPort.name} (dist={bestDist:F2}).");
                NCIITaskListManager.CheckConditions();
                T2TaskListManager.CheckConditions();
            }
            else
            {
                Debug.Log($"[HardwareHolder] {prefabName} (cableType='{cableType}') failed.\n" +
                          $"  Inactive: [{string.Join(", ", rejectedActive)}]\n" +
                          $"  Already installed: [{string.Join(", ", rejectedInstalled)}]\n" +
                          $"  Wrong type: [{string.Join(", ", rejectedType)}]\n" +
                          $"  Out of range: [{string.Join(", ", rejectedDist)}]");
            }
            return;
        }

        HeatsinkController heatsink = hardwarePrefab.GetComponent<HeatsinkController>();
        if (heatsink != null)
        {
            CPUSlotController[] allSlots = FindObjectsOfType<CPUSlotController>(true);
            CPUSlotController bestSlot = null;
            float bestDist = float.MaxValue;

            foreach (CPUSlotController slot in allSlots)
            {
                if (slot.IsHeatsinkInstalled) continue;

                float dist = Vector3.Distance(slot.transform.position, dropWorldPos);
                if (dist < slotInstallRadius && dist < bestDist)
                {
                    bestDist = dist;
                    bestSlot = slot;
                }
            }

            if (bestSlot == null)
            {
                Debug.Log($"[HardwareHolder] No valid CPUSlot found for Heatsink.");
                return;
            }

            hardwarePrefab.transform.SetParent(bestSlot.transform, false);
            hardwarePrefab.transform.localPosition = heatsink.InstalledLocalPosition;
            Vector3 reinstallScale = heatsink.InstalledLocalScale;
            hardwarePrefab.transform.localScale = reinstallScale != Vector3.zero
                ? reinstallScale
                : _originalLocalScale;
            hardwarePrefab.SetActive(true);
            heatsink.OnInstalledToSlot(bestSlot);
            gameObject.SetActive(false);
            ActivityLogManager.Log("Heatsink installed", ActivityLogManager.EntryType.Install);
            Debug.Log($"[HardwareHolder] Heatsink installed to CPUSlot.");
            NCIITaskListManager.CheckConditions();
            return;
        }

        CPUController cpuCtrl = hardwarePrefab.GetComponent<CPUController>();
        if (cpuCtrl != null)
        {
            CPUSlotController[] allSlots = FindObjectsOfType<CPUSlotController>(true);
            CPUSlotController bestSlot = null;
            float bestDist = float.MaxValue;

            Debug.Log($"[HardwareHolder] CPU install — found {allSlots.Length} CPUSlotController(s)");

            foreach (CPUSlotController slot in allSlots)
            {
                Debug.Log($"[HardwareHolder] Checking slot '{slot.gameObject.name}': state={slot.State} IsCPUInstalled={slot.IsCPUInstalled} IsHeatsinkInstalled={slot.IsHeatsinkInstalled} IsLockClosed={slot.IsLockClosed}");

                if (slot.IsCPUInstalled) continue;
                if (slot.IsLockClosed) { Debug.Log("[HardwareHolder] CPU install blocked — CPU lock is closed."); continue; }
                if (slot.IsHeatsinkInstalled) { Debug.Log("[HardwareHolder] CPU install blocked — heatsink is installed."); continue; }

                float dist = Vector3.Distance(slot.transform.position, dropWorldPos);
                if (dist < slotInstallRadius && dist < bestDist)
                {
                    bestDist = dist;
                    bestSlot = slot;
                }
            }

            if (bestSlot == null)
            {
                Debug.Log($"[HardwareHolder] No valid CPUSlot found for CPU.");
                return;
            }

            hardwarePrefab.transform.SetParent(bestSlot.transform, false);
            hardwarePrefab.transform.localPosition = cpuCtrl.InstalledLocalPosition;
            Vector3 cpuScale = cpuCtrl.InstalledLocalScale;
            hardwarePrefab.transform.localScale = cpuScale != Vector3.zero ? cpuScale : _originalLocalScale;
            hardwarePrefab.SetActive(true);
            bestSlot.OnCPUInstalled();
            gameObject.SetActive(false);
            ActivityLogManager.Log("CPU installed to slot", ActivityLogManager.EntryType.Install);
            Debug.Log($"[HardwareHolder] CPU installed to CPUSlot.");
            NCIITaskListManager.CheckConditions();
            return;
        }

        SlotContainer[] allSlotContainers = FindObjectsOfType<SlotContainer>();
        SlotContainer bestSlotContainer = null;
        float bestSlotDist = float.MaxValue;

        foreach (SlotContainer slot in allSlotContainers)
        {
            if (!slot.IsSlotEmpty()) continue;
            if (!slot.CanAcceptPrefab(prefabName)) continue;

            float dist = Vector3.Distance(slot.transform.position, dropWorldPos);
            if (dist < slotInstallRadius && dist < bestSlotDist)
            {
                bestSlotDist = dist;
                bestSlotContainer = slot;
            }
        }

        if (bestSlotContainer == null) return;

        if (hardwarePrefab.GetComponent<GPUController>() != null)
        {
            MotherboardPhaseManager phase = bestSlotContainer.GetComponentInParent<MotherboardPhaseManager>();
            if (phase != null && phase.CurrentPhase == MotherboardPhaseManager.Phase.Phase2)
            {
                Debug.Log($"[HardwareHolder] {prefabName} install blocked — motherboard is in Phase 2.");
                return;
            }
        }

        hardwarePrefab.SetActive(true);
        bestSlotContainer.InstallChild(hardwarePrefab, prefabName);
        ActivityLogManager.Log($"{prefabDisplay} placed in workspace", ActivityLogManager.EntryType.Install);

        var mb = hardwarePrefab.GetComponent<MotherboardController>();
        if (mb != null) mb.MarkInstalled();

        hardwarePrefab.GetComponent<RAMController>()?.OnSnappedToSlot();
        hardwarePrefab.GetComponent<GPUController>()?.OnSnappedToSlot();
        hardwarePrefab.GetComponent<HDDController>()?.OnSnappedToSlot();
        hardwarePrefab.GetComponent<SSDController>()?.OnSnappedToSlot();
        hardwarePrefab.GetComponent<MotherboardController>()?.OnSnappedToSlot();

        gameObject.SetActive(false);
        NCIITaskListManager.CheckConditions();
    }

    // Returns this holder icon's center converted from UI space to main-camera world space.
    private Vector3 GetIconWorldCenter()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null || Camera.main == null) return Vector3.zero;

        Canvas rootCanvas = rt.GetComponentInParent<Canvas>();
        if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;
        Camera uiCam = (rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            ? rootCanvas.worldCamera : null;

        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(uiCam, (corners[0] + corners[2]) * 0.5f);

        Vector3 worldCenter = Camera.main.ScreenToWorldPoint(new Vector3(screenCenter.x, screenCenter.y, 10f));
        worldCenter.z = 0f;
        return worldCenter;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

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

    private void ApplyWorldScale(Transform t, Vector3 targetWorldScale)
    {
        t.localScale = Vector3.one;
        Vector3 ls = t.lossyScale;
        t.localScale = new Vector3(
            targetWorldScale.x / ls.x,
            targetWorldScale.y / ls.y,
            targetWorldScale.z / ls.z);
    }
}
