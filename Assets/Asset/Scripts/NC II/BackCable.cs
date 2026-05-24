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

        if (powerButtonSource != null)
            _powerButton = powerButtonSource as IPowerButton;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // Gate: power must be off before unplugging
        if (_powerButton != null && _powerButton.IsPoweredOn)
        {
            Debug.Log($"[BackCable] Cannot unplug '{cableType}' � turn off the power button first.");
            return;
        }

        // Gate: PSU switch must be off before unplugging the PSU cable
        if (psuSwitchGate != null && psuSwitchGate.IsOn)
        {
            Debug.Log($"[BackCable] Cannot unplug '{cableType}' — PSU switch is on.");
            return;
        }

        _isDragging = true;
        _originalParent = transform.parent;
        _originalLocalPos = transform.localPosition;
        _originalLocalScale = transform.localScale;

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

    public void InstallToPort(BackPortSlot port)
    {
        _parentPort = port;
        transform.SetParent(port.transform, false);
        transform.localPosition = Vector3.zero;
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