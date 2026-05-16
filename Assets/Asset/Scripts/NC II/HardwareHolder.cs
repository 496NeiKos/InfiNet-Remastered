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

    private void Start()
    {
        if (hardwarePrefab != null)
        {
            _worldScale = hardwarePrefab.transform.lossyScale;

            // BackCable starts active (installed in port) — do not deactivate it here.
            // All other hardware starts inactive (stored) until dragged to workspace.
            bool isBackCable = hardwarePrefab.GetComponent<BackCable>() != null;
            if (!isBackCable)
                hardwarePrefab.SetActive(false);
        }

        // Icon shows only when prefab is inactive (stored)
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
        sr.sprite = GetComponent<Image>().sprite;
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
        if (_dragIndicator != null)
        {
            Destroy(_dragIndicator);
            _dragIndicator = null;
        }

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

        // BackCable path — install to BackPortSlot
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
                Debug.Log($"[HardwareHolder] {prefabName} dropped on wrong/no port — returning to hardware area.");
            }
            return;
        }

        // Standard SlotContainer path
        SlotContainer[] allSlots = FindObjectsOfType<SlotContainer>();
        SlotContainer bestSlot = null;
        float bestSlotDist = float.MaxValue;

        foreach (SlotContainer slot in allSlots)
        {
            if (!slot.IsSlotEmpty()) continue;
            if (!slot.CanAcceptPrefab(prefabName)) continue;

            float dist = Vector3.Distance(slot.transform.position, dropWorldPos);
            if (dist < slotInstallRadius && dist < bestSlotDist)
            {
                bestSlotDist = dist;
                bestSlot = slot;
            }
        }

        if (bestSlot == null) return;

        hardwarePrefab.SetActive(true);
        bestSlot.InstallChild(hardwarePrefab, prefabName);

        var mb = hardwarePrefab.GetComponent<MotherboardController>();
        if (mb != null) mb.MarkInstalled();

        gameObject.SetActive(false);
    }

    private void ApplyWorldScale(Transform t, Vector3 targetWorldScale)
    {
        t.localScale = Vector3.one;
        Vector3 currentLossy = t.lossyScale;
        t.localScale = new Vector3(
            targetWorldScale.x / currentLossy.x,
            targetWorldScale.y / currentLossy.y,
            targetWorldScale.z / currentLossy.z
        );
    }
}