using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HardwareHolder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Hardware Reference")]
    public GameObject hardwarePrefab;

    [Header("Slot Install Proximity (world units)")]
    public float slotInstallRadius = 1.5f;

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

            bool isBackCable = hardwarePrefab.GetComponent<BackCable>() != null;
            bool isMBCable = hardwarePrefab.GetComponent<MBCable>() != null;
            bool isSlotSibling = hardwarePrefab.GetComponent<HeatsinkController>() != null
                              || hardwarePrefab.GetComponent<CPUController>() != null;

            if (!isBackCable && !isMBCable && !isSlotSibling)
                hardwarePrefab.SetActive(false);
        }

        bool prefabInactive = hardwarePrefab == null || !hardwarePrefab.activeSelf;
        gameObject.SetActive(prefabInactive);
    }

    public bool IsAvailable() => hardwarePrefab != null && !hardwarePrefab.activeSelf;

    public void StoreHardware()
    {
        if (hardwarePrefab == null) return;
        hardwarePrefab.transform.SetParent(GameManager.Instance.hardwareStorage, true);
        hardwarePrefab.SetActive(false);
        gameObject.SetActive(true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsAvailable()) return;

        _isDragging = true;

        _dragIndicator = new GameObject("DragIndicator");
        SpriteRenderer sr = _dragIndicator.AddComponent<SpriteRenderer>();
        sr.sprite = GetComponent<Image>()?.sprite;
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

        bool onWorkspace = RectTransformUtility.RectangleContainsScreenPoint(
            GameManager.Instance.workspaceArea, eventData.position, eventData.pressEventCamera);
        if (!onWorkspace) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        worldPos.z = 0f;

        hardwarePrefab.transform.SetParent(GameManager.Instance.worldRoot, false);
        hardwarePrefab.transform.position = worldPos;
        ApplyWorldScale(hardwarePrefab.transform, _worldScale);
        hardwarePrefab.SetActive(true);
        gameObject.SetActive(false);
    }

    private void TryInstallInSlot(PointerEventData eventData)
    {
        if (hardwarePrefab == null) return;

        string prefabName = hardwarePrefab.name;

        Vector3 dropWorldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        dropWorldPos.z = 0f;

        // BackCable path
        BackCable backCable = hardwarePrefab.GetComponent<BackCable>();
        if (backCable != null)
        {
            BackPortSlot[] allPorts = FindObjectsOfType<BackPortSlot>(true);
            BackPortSlot bestPort = null;
            float bestDist = float.MaxValue;

            foreach (BackPortSlot port in allPorts)
            {
                if (!port.IsUninstalled) continue;
                if (port.gameObject.name != backCable.GetCableType() &&
                    !port.gameObject.name.Contains(backCable.GetCableType())) continue;

                float dist = Vector3.Distance(port.transform.position, dropWorldPos);
                if (dist < slotInstallRadius && dist < bestDist)
                {
                    bestDist = dist;
                    bestPort = port;
                }
            }

            if (bestPort != null)
            {
                hardwarePrefab.SetActive(true);
                backCable.InstallToPort(bestPort);
                gameObject.SetActive(false);
            }
            else
            {
                Debug.Log($"[HardwareHolder] {prefabName} dropped on wrong/no port.");
            }
            return;
        }

        // MBCable path — reinstalls to matching CableSlot by cableType
        MBCable mbCable = hardwarePrefab.GetComponent<MBCable>();
        if (mbCable != null)
        {
            CableSlot[] allSlots = FindObjectsOfType<CableSlot>(true);
            CableSlot bestSlot = null;
            float bestDist = float.MaxValue;

            foreach (CableSlot slot in allSlots)
            {
                if (!slot.enabled) continue;          // Phase 1 inactive — slot is disabled
                if (slot.IsInstalled()) continue;
                if (!slot.CanAcceptCable(mbCable.GetCableType())) continue;

                float dist = Vector3.Distance(slot.transform.position, dropWorldPos);
                if (dist < slotInstallRadius && dist < bestDist)
                {
                    bestDist = dist;
                    bestSlot = slot;
                }
            }

            if (bestSlot != null)
            {
                hardwarePrefab.SetActive(true);
                mbCable.InstallToSlot(bestSlot);
                gameObject.SetActive(false);
                Debug.Log($"[HardwareHolder] {prefabName} installed to CableSlot.");
            }
            else
            {
                Debug.Log($"[HardwareHolder] {prefabName} dropped on wrong/no slot — stays in hardware area.");
            }
            return;
        }

        // Heatsink path — installs back to CPUSlot (finds CPUSlotController by proximity)
        HeatsinkController heatsink = hardwarePrefab.GetComponent<HeatsinkController>();
        if (heatsink != null)
        {
            CPUSlotController[] allSlots = FindObjectsOfType<CPUSlotController>(true);

            foreach (CPUSlotController slot in allSlots)
            {
                if (slot.IsHeatsinkInstalled) continue;

                hardwarePrefab.transform.SetParent(slot.transform, false);
                hardwarePrefab.transform.localPosition = Vector3.zero;
                Vector3 reinstallScale = heatsink.InstalledLocalScale;
                hardwarePrefab.transform.localScale = reinstallScale != Vector3.zero
                    ? reinstallScale
                    : _originalLocalScale;
                hardwarePrefab.SetActive(true);
                heatsink.OnInstalledToSlot(slot);
                gameObject.SetActive(false);
                Debug.Log($"[HardwareHolder] Heatsink installed to CPUSlot.");
                return;
            }

            Debug.Log($"[HardwareHolder] No valid CPUSlot found for Heatsink.");
            return;
        }

        // CPU path — installs back to CPUSlot
        CPUController cpuCtrl = hardwarePrefab.GetComponent<CPUController>();
        if (cpuCtrl != null)
        {
            CPUSlotController[] allSlots = FindObjectsOfType<CPUSlotController>(true);

            Debug.Log($"[HardwareHolder] CPU install — found {allSlots.Length} CPUSlotController(s)");

            foreach (CPUSlotController slot in allSlots)
            {
                Debug.Log($"[HardwareHolder] Checking slot '{slot.gameObject.name}': state={slot.State} IsCPUInstalled={slot.IsCPUInstalled} IsHeatsinkInstalled={slot.IsHeatsinkInstalled} IsLockClosed={slot.IsLockClosed}");

                if (slot.IsCPUInstalled) continue;
                if (slot.IsLockClosed) { Debug.Log("[HardwareHolder] CPU install blocked — CPU lock is closed."); continue; }
                if (slot.IsHeatsinkInstalled) { Debug.Log("[HardwareHolder] CPU install blocked — heatsink is installed."); continue; }

                hardwarePrefab.transform.SetParent(slot.transform, false);
                hardwarePrefab.transform.localPosition = Vector3.zero;
                Vector3 cpuScale = cpuCtrl.InstalledLocalScale;
                hardwarePrefab.transform.localScale = cpuScale != Vector3.zero ? cpuScale : _originalLocalScale;
                hardwarePrefab.SetActive(true);
                slot.OnCPUInstalled();
                gameObject.SetActive(false);
                Debug.Log($"[HardwareHolder] CPU installed to CPUSlot.");
                return;
            }

            Debug.Log($"[HardwareHolder] No valid CPUSlot found for CPU.");
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

        hardwarePrefab.SetActive(true);
        bestSlotContainer.InstallChild(hardwarePrefab, prefabName);

        var mb = hardwarePrefab.GetComponent<MotherboardController>();
        if (mb != null) mb.MarkInstalled();

        gameObject.SetActive(false);
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