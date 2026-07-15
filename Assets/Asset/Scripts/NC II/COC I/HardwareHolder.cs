using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HardwareHolder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
                              IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hardware Reference")]
    public GameObject hardwarePrefab;

    [Header("Spawn in Workspace")]
    [Tooltip("When true the prefab starts active in the workspace and the holder stays hidden.")]
    [SerializeField] private bool startInWorkspace = false;

    [Header("Slot Install Proximity (world units)")]
    public float slotInstallRadius = 1.5f;

    [Header("Info Panel")]
    [SerializeField] private Sprite infoImage;
    [SerializeField] private string infoName;
    [TextArea(3, 6)]
    [SerializeField] private string infoDescription;

    private Coroutine _hoverCoroutine;
    private GameObject _dragIndicator;
    private bool _isDragging = false;
    private Vector3 _worldScale;
    private Vector3 _originalLocalScale;

    private void Start()
    {
        if (hardwarePrefab != null)
        {
            _worldScale = hardwarePrefab.transform.lossyScale;
            _originalLocalScale = hardwarePrefab.transform.localScale;

            if (!startInWorkspace)
            {
                bool isBackCable = hardwarePrefab.GetComponent<BackCable>() != null;
                bool isMBCable = hardwarePrefab.GetComponent<MBCable>() != null;
                bool isSlotSibling = hardwarePrefab.GetComponent<HeatsinkController>() != null
                                  || hardwarePrefab.GetComponent<CPUController>() != null;

                if (!isBackCable && !isMBCable && !isSlotSibling)
                    hardwarePrefab.SetActive(false);
            }
        }

        bool prefabInactive = hardwarePrefab == null || !hardwarePrefab.activeSelf;
        gameObject.SetActive(prefabInactive);
    }

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

    public bool IsAvailable() => hardwarePrefab != null && !hardwarePrefab.activeSelf;

    public void StoreHardware()
    {
        if (hardwarePrefab == null) return;
        hardwarePrefab.transform.SetParent(GameManager.Instance.ActiveHardwareStorageContainer, true);
        hardwarePrefab.SetActive(false);
        gameObject.SetActive(true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsAvailable()) return;

        CancelHover();
        HardwareInfoPanel.Instance?.Hide();

        _isDragging = true;

        _dragIndicator = new GameObject("DragIndicator");
        SpriteRenderer sr = _dragIndicator.AddComponent<SpriteRenderer>();
        sr.sprite = hardwarePrefab.GetComponent<SpriteRenderer>()?.sprite;
        sr.sortingOrder = 999;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        worldPos.z = 0f;
        _dragIndicator.transform.position = worldPos;
        _dragIndicator.transform.localScale = _worldScale;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || _dragIndicator == null) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        worldPos.z = 0f;
        _dragIndicator.transform.position = worldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragIndicator != null) { Destroy(_dragIndicator); _dragIndicator = null; }
        if (!_isDragging) return;
        _isDragging = false;

        if (GameManager.Instance.IsEditorOpen)
        {
            TryInstallInSlot(eventData);
            return;
        }

        // Cables must only install to a port via TryInstallInSlot (editor open).
        // Never place them loose in worldRoot — snap back to hardware area instead.
        bool isCable = hardwarePrefab != null &&
            hardwarePrefab.GetComponent<CableBehavior>() != null;
        if (isCable) return;

        bool onWorkspace = RectTransformUtility.RectangleContainsScreenPoint(
            GameManager.Instance.workspaceArea, eventData.position, eventData.pressEventCamera);
        if (!onWorkspace) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        worldPos.z = 0f;

        hardwarePrefab.transform.SetParent(GameManager.Instance.ActiveWorldContainer, false);
        hardwarePrefab.transform.position = worldPos;
        ApplyWorldScale(hardwarePrefab.transform, _worldScale);
        hardwarePrefab.SetActive(true);

        // Re-enable DragPrefab — CPUSlotController may have disabled it while the
        // component was seated in its slot (BothUninstalled state disables both).
        // Collider state is managed by DragPrefab.Update() via the workspaceProxy.
        DragPrefab dp = hardwarePrefab.GetComponent<DragPrefab>();
        if (dp != null) dp.enabled = true;

        gameObject.SetActive(false);
    }

    private void TryInstallInSlot(PointerEventData eventData)
    {
        if (hardwarePrefab == null) return;

        string prefabName = hardwarePrefab.name;
        DragPrefab dp = hardwarePrefab.GetComponent<DragPrefab>();
        string prefabDisplay = dp != null ? dp.LogDisplayName : prefabName;

        Vector3 dropWorldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        dropWorldPos.z = 0f;

        // Unified cable path — works for all CableBehavior cables (formerly BackCable and MBCable)
        CableBehavior cable = hardwarePrefab.GetComponent<CableBehavior>();
        if (cable != null)
        {
            string cableType = cable.GetCableType();
            CablePort[] allPorts = FindObjectsOfType<CablePort>(true);

            CablePort bestPort = null;
            float bestDist = float.MaxValue;
            var rejectedActive = new System.Collections.Generic.List<string>();
            var rejectedInstalled = new System.Collections.Generic.List<string>();
            var rejectedType = new System.Collections.Generic.List<string>();
            var rejectedDist = new System.Collections.Generic.List<string>();

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

        // Heatsink path � installs back to CPUSlot (finds CPUSlotController by proximity)
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

        // CPU path � installs back to CPUSlot
        CPUController cpuCtrl = hardwarePrefab.GetComponent<CPUController>();
        if (cpuCtrl != null)
        {
            CPUSlotController[] allSlots = FindObjectsOfType<CPUSlotController>(true);
            CPUSlotController bestSlot = null;
            float bestDist = float.MaxValue;

            Debug.Log($"[HardwareHolder] CPU install � found {allSlots.Length} CPUSlotController(s)");

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

        // Standard SlotContainer path (GPU, RAM, CMOS, Motherboard, HDD, PSU)
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

        // Block GPU installation when the motherboard it belongs to is in Phase 2
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