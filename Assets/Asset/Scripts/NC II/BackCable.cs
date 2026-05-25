using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// On cable child objects inside back ports (PSUPortCable, VGAPortCable, etc.).
/// Handles drag-out, snap-back, and store to hardware area.
/// Optionally gated by a power button � cable cannot be removed while power is on.
/// </summary>
public class BackCable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Identity")]
    [Tooltip("Must match the GameObject name of the matching BackPortSlot")]
    [SerializeField] private string cableType;

    [Header("Hardware Area Icon")]
    [SerializeField] private HardwareHolder hardwareHolder;

    [Header("Power Gate (assign the power button that must be OFF before unplugging)")]
    [SerializeField] private MonoBehaviour powerButtonSource;
    private IPowerButton _powerButton;

    [Header("PSU Switch Gate (assign PSUSwitchController — cable locked while switch is On)")]
    [SerializeField] private PSUSwitchController psuSwitchGate;

    private BackPortSlot _parentPort;
    private Transform _originalParent;
    private Vector3 _originalLocalPos;
    private Vector3 _originalLocalScale;

    private SpriteRenderer _dragIndicator;
    private bool _isDragging = false;

    private void Start()
    {
        _parentPort = GetComponentInParent<BackPortSlot>();

        // Capture designed transform so InstallToPort and SnapBack always restore correctly
        _originalParent = transform.parent;
        _originalLocalPos = transform.localPosition;
        _originalLocalScale = transform.localScale;

        if (powerButtonSource != null)
            _powerButton = powerButtonSource as IPowerButton;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // Always capture current transform so snap-back has valid values even on a blocked drag
        _originalParent = transform.parent;
        _originalLocalPos = transform.localPosition;
        _originalLocalScale = transform.localScale;

        // Gate: power must be off before unplugging
        if (_powerButton != null && _powerButton.IsPoweredOn)
        {
            Debug.Log($"[BackCable] Cannot unplug '{cableType}' � turn off the power button first.");
            _isDragging = false;
            return;
        }

        // Gate: PSU switch must be off before unplugging the PSU cable
        if (psuSwitchGate != null && psuSwitchGate.IsOn)
        {
            Debug.Log($"[BackCable] Cannot unplug '{cableType}' — PSU switch is on.");
            _isDragging = false;
            return;
        }

        _isDragging = true;

        Vector3 worldScale = transform.lossyScale;
        transform.SetParent(GameManager.Instance.worldRoot, true);
        ApplyWorldScale(worldScale);

        GameObject go = new GameObject("BackCableDrag");
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

        bool wasInPort = _originalParent?.GetComponent<BackPortSlot>() != null;

        if (wasInPort)
        {
            bool onHardwareArea = RectTransformUtility.RectangleContainsScreenPoint(
                GameManager.Instance.hardwareArea, eventData.position, eventData.pressEventCamera);

            if (onHardwareArea)
            {
                _parentPort?.SetUninstalled();
                SendToHolder();
                Debug.Log($"[BackCable] '{cableType}' stored to hardware area.");
            }
            else
            {
                SnapBack();
                Debug.Log($"[BackCable] '{cableType}' snapped back to port.");
            }
        }
        else
        {
            // Dragged from worldRoot (not from a port).
            // Try to install to the nearest visible port; otherwise just snap back in place.
            // Do NOT send to hardware area or touch any port state.
            BackPortSlot target = FindNearestPortInPanel(eventData);
            if (target != null)
            {
                InstallToPort(target);
                Debug.Log($"[BackCable] '{cableType}' installed to port from workspace.");
            }
            else
            {
                transform.SetParent(_originalParent, false);
                transform.localPosition = _originalLocalPos;
                transform.localScale = _originalLocalScale;
                Debug.Log($"[BackCable] '{cableType}' snapped back to workspace (no port in range).");
            }
        }
    }

    private BackPortSlot FindNearestPortInPanel(PointerEventData eventData)
    {
        GameObject panel = GameManager.Instance?.firstLayer;
        if (panel == null) return null;

        Vector3 dropPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        dropPos.z = 0f;

        const float radius = 1.5f;
        BackPortSlot best = null;
        float bestDist = float.MaxValue;

        foreach (BackPortSlot port in panel.GetComponentsInChildren<BackPortSlot>())
        {
            if (!port.IsUninstalled) continue;
            if (port.gameObject.name != cableType && !port.gameObject.name.Contains(cableType)) continue;
            float dist = Vector3.Distance(port.transform.position, dropPos);
            if (dist < radius && dist < bestDist) { bestDist = dist; best = port; }
        }
        return best;
    }

    public void InstallToPort(BackPortSlot port)
    {
        _parentPort = port;
        transform.SetParent(port.transform, false);
        transform.localPosition = _originalLocalPos;
        transform.localScale = _originalLocalScale;
        gameObject.SetActive(true);
        port.SetInstalled();
        Debug.Log($"[BackCable] '{cableType}' installed to port.");
    }

    private void SnapBack()
    {
        transform.SetParent(_originalParent, false);
        transform.localPosition = _originalLocalPos;
        transform.localScale = _originalLocalScale;
        // Only restore port state when actually snapping back into a BackPortSlot.
        if (_originalParent?.GetComponent<BackPortSlot>() != null)
            _parentPort?.SetInstalled();
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

        Debug.LogWarning($"[BackCable] No HardwareHolder found for '{gameObject.name}'.");
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

    public string GetCableType() => cableType;
}