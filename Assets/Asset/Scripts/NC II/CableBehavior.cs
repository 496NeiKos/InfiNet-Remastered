using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Unified cable behavior. Replaces BackCable and MBCable.
/// Hold 1s to detach (conditions permitting) — port goes Uninstalled immediately.
/// Drag freely; drop on hardware area to store, drop on a valid CablePort to
/// install there, or drop anywhere else to snap back to the home port.
///
/// Implements the EventSystem drag interfaces as empty consumers so drag events
/// do not bubble up to parent DragPrefab components (e.g. SystemUnit).
/// Actual hold/drag logic is Update-based via CableDragManager.
/// </summary>
[DefaultExecutionOrder(1)]
public class CableBehavior : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Identity")]
    [SerializeField] private string cableType;

    [Header("Home Port")]
    [Tooltip("The CablePort this cable starts installed in. Auto-detected from parent if blank. Updated at runtime when installed to a new port.")]
    [SerializeField] private CablePort homePort;

    [Header("Hardware Area")]
    [SerializeField] private HardwareHolder hardwareHolder;

    [Header("Hold Settings")]
    [SerializeField] private float holdDuration = 1f;

    [Header("Power Gates (optional)")]
    [SerializeField] private MonoBehaviour powerButtonSource;
    [SerializeField] private PowerButton secondaryPowerGate;
    [SerializeField] private MonitorPowerButton monitorPowerGate;
    [SerializeField] private PSUSwitchController psuSwitchGate;

    private IPowerButton _powerButton;
    private Vector3 _installedLocalPos;
    private Vector3 _installedLocalScale;

    private float _holdTimer;
    private bool _detached;
    private bool _isDragging;
    private GameObject _dragIndicator;

    private static CableBehavior _holdTarget;

    public bool IsDetached => _detached;
    public string GetCableType() => cableType;

    // Consume EventSystem drag events so they don't bubble up to parent DragPrefab components.
    public void OnBeginDrag(PointerEventData eventData) { }
    public void OnDrag(PointerEventData eventData)      { }
    public void OnEndDrag(PointerEventData eventData)   { }

    private void Start()
    {
        if (homePort == null)
            homePort = GetComponentInParent<CablePort>();

        _installedLocalPos = transform.localPosition;
        _installedLocalScale = transform.localScale;

        if (powerButtonSource != null)
            _powerButton = powerButtonSource as IPowerButton;
    }

    private void Update()
    {
        if (_detached) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (CableDragManager.Instance.HasActiveDrag)
        {
            _holdTimer = 0f;
            if (_holdTarget == this) _holdTarget = null;
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame && IsMouseOver())
            _holdTarget = this;

        if (_holdTarget == this)
        {
            if (mouse.leftButton.isPressed)
            {
                _holdTimer += Time.deltaTime;
                if (_holdTimer >= holdDuration)
                {
                    _holdTarget = null;
                    if (CanDetach())
                        Detach();
                    else
                        _holdTimer = 0f;
                }
            }
            else
            {
                _holdTarget = null;
                _holdTimer = 0f;
            }
        }
        else
        {
            _holdTimer = 0f;
        }
    }

    private bool CanDetach()
    {
        if (_powerButton != null && _powerButton.IsPoweredOn)
        {
            ActivityLogManager.Log($"Cannot unplug {cableType} — turn off its power source first.", ActivityLogManager.EntryType.Warning);
            return false;
        }
        if (secondaryPowerGate != null && secondaryPowerGate.IsPoweredOn)
        {
            ActivityLogManager.Log($"Cannot unplug {cableType} — turn off the System Unit first.", ActivityLogManager.EntryType.Warning);
            return false;
        }
        if (monitorPowerGate != null && monitorPowerGate.IsPoweredOn)
        {
            ActivityLogManager.Log($"Cannot unplug {cableType} — turn off the Monitor first.", ActivityLogManager.EntryType.Warning);
            return false;
        }
        if (psuSwitchGate != null && psuSwitchGate.IsOn)
        {
            ActivityLogManager.Log($"Cannot unplug {cableType} — turn off the PSU switch first.", ActivityLogManager.EntryType.Warning);
            return false;
        }
        return true;
    }

    private void Detach()
    {
        _holdTimer = 0f;
        _detached = true;
        _isDragging = false;

        homePort?.SetUninstalled();
        ActivityLogManager.Log($"{cableType} cable unplugged", ActivityLogManager.EntryType.Remove);

        // SetParent with worldPositionStays=true already preserves world scale.
        // Do NOT reassign localScale here — it would corrupt the transform.
        transform.SetParent(GameManager.Instance.worldRoot, true);

        _dragIndicator = new GameObject("CableDragIndicator");
        SpriteRenderer sr = _dragIndicator.AddComponent<SpriteRenderer>();
        sr.sprite = GetComponent<SpriteRenderer>()?.sprite;
        sr.sortingOrder = 999;
        _dragIndicator.transform.position = transform.position;
        _dragIndicator.transform.localScale = transform.lossyScale;

        CableDragManager.Instance.Register(this);
        Debug.Log($"[CableBehavior] {cableType} detached.");
    }

    // Called every frame by CableDragManager while detached.
    public void DragUpdate()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (!_isDragging)
        {
            if (mouse.leftButton.isPressed)
                _isDragging = true;
            return;
        }

        if (mouse.leftButton.isPressed)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(mouse.position.ReadValue().x, mouse.position.ReadValue().y, 10f));
            worldPos.z = 0f;
            transform.position = worldPos;
            if (_dragIndicator != null)
                _dragIndicator.transform.position = worldPos;
        }
        else
        {
            OnDragReleased();
        }
    }

    private void OnDragReleased()
    {
        if (_dragIndicator != null) { Destroy(_dragIndicator); _dragIndicator = null; }
        _isDragging = false;
        _detached = false;
        CableDragManager.Instance.Unregister(this);

        Vector2 screenPos = Mouse.current.position.ReadValue();
        bool onHardwareArea = RectTransformUtility.RectangleContainsScreenPoint(
            GameManager.Instance.hardwareArea, screenPos, Camera.main);

        if (onHardwareArea)
        {
            SendToHolder();
            NCIITaskListManager.CheckConditions();
            T2TaskListManager.CheckConditions();
            Debug.Log($"[CableBehavior] {cableType} stored.");
            return;
        }

        Vector3 dropPos = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, 10f));
        dropPos.z = 0f;

        CablePort target = FindPortAtPosition(dropPos);
        if (target != null)
        {
            InstallToPort(target);
            NCIITaskListManager.CheckConditions();
            T2TaskListManager.CheckConditions();
        }
        else
        {
            SnapBack();
        }
    }

    private CablePort FindPortAtPosition(Vector3 worldPos)
    {
        const float radius = 1.5f;
        CablePort[] allPorts = FindObjectsOfType<CablePort>(true);

        CablePort best = null;
        float bestDist = float.MaxValue;
        foreach (CablePort port in allPorts)
        {
            if (!port.gameObject.activeInHierarchy) continue;
            if (!port.IsUninstalled) continue;
            if (!port.CanAcceptCable(cableType)) continue;
            float dist = Vector3.Distance(port.transform.position, worldPos);
            if (dist < radius && dist < bestDist) { bestDist = dist; best = port; }
        }
        return best;
    }

    public void InstallToPort(CablePort port)
    {
        homePort = port;
        _detached = false;
        _isDragging = false;

        // worldPositionStays = true preserves world scale across reparenting.
        // Then snap to port's local origin so the cable sits on the port regardless
        // of where it came from (storage or another port).
        transform.SetParent(port.transform, true);
        transform.localPosition = Vector3.zero;

        // Update cache so SnapBack always uses the correct port-relative values.
        _installedLocalPos   = Vector3.zero;
        _installedLocalScale = transform.localScale;

        gameObject.SetActive(true);
        port.SetInstalled();
        ActivityLogManager.Log($"{cableType} cable plugged in", ActivityLogManager.EntryType.Install);
        Debug.Log($"[CableBehavior] {cableType} installed to {port.name}.");
    }

    private void SnapBack()
    {
        _detached = false;
        if (homePort != null)
        {
            transform.SetParent(homePort.transform, false);
            transform.localPosition = _installedLocalPos;
            transform.localScale = _installedLocalScale;
            homePort.SetInstalled();
            Debug.Log($"[CableBehavior] {cableType} snapped back to {homePort.name}.");
        }
        else
        {
            gameObject.SetActive(false);
            Debug.LogWarning($"[CableBehavior] {cableType} snap back failed — no homePort.");
        }
    }

    private void SendToHolder()
    {
        if (hardwareHolder != null) { hardwareHolder.StoreHardware(); return; }
        foreach (HardwareHolder h in FindObjectsOfType<HardwareHolder>(true))
        {
            if (h.hardwarePrefab != null && h.hardwarePrefab.name == gameObject.name)
            {
                hardwareHolder = h;
                h.StoreHardware();
                return;
            }
        }
        Debug.LogWarning($"[CableBehavior] No HardwareHolder found for '{gameObject.name}'.");
        gameObject.SetActive(false);
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        foreach (Collider2D col in GetComponents<Collider2D>())
            if (col.OverlapPoint(mouseWorld)) return true;
        return false;
    }
}
