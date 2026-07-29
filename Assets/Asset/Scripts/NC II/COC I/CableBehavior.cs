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
    [Tooltip("Human-readable name shown in the activity log. E.g. 'PSU Cable'. Auto-derived from cableType if left blank.")]
    [SerializeField] private string displayName;

    private string LogName => !string.IsNullOrEmpty(displayName) ? displayName : DeriveName(cableType);

    private static string DeriveName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "Cable";
        string s = raw;

        // Strip trailing Port / Slot / Cable (case-insensitive)
        foreach (string suffix in new[] { "Port", "Slot", "Cable" })
            if (s.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase))
                s = s.Substring(0, s.Length - suffix.Length).TrimEnd();

        // Expand known device prefixes (check longer ones first)
        if      (s.StartsWith("SU", System.StringComparison.OrdinalIgnoreCase))  s = "System Unit " + s.Substring(2).TrimStart();
        else if (s.StartsWith("MB", System.StringComparison.OrdinalIgnoreCase))  s = "Motherboard " + s.Substring(2).TrimStart();
        else if (s.StartsWith("GPU", System.StringComparison.OrdinalIgnoreCase)) s = "GPU " + s.Substring(3).TrimStart();
        else if (s.StartsWith("M", System.StringComparison.OrdinalIgnoreCase))   s = "Monitor " + s.Substring(1).TrimStart();
        else if (s.StartsWith("A", System.StringComparison.OrdinalIgnoreCase))   s = "AVR " + s.Substring(1).TrimStart();

        return s.Trim() + " Cable";
    }

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
    private bool _isMonitorCable;
    private Vector3 _installedLocalPos;
    private Vector3 _installedLocalScale;

    private float _holdTimer;
    private bool _detached;
    private bool _isDragging;
    private GameObject _dragIndicator;
    private Vector3 _grabOffset;
    private Vector3 _holdStartMouseWorld;

    // The port this cable started in at scene load. Never changes — used by HardwareHolder
    // to pick the cable end whose original port is closest to the install target, so
    // each cable end's intended sprite and scale are preserved regardless of install order.
    private CablePort _originalHomePort;

    private static CableBehavior _holdTarget;

    public bool IsDetached => _detached;
    public string GetCableType() => cableType;

    // World position of this cable's original home port. Used by HardwareHolder's
    // multi-cable install picker to preserve correct sprite assignment per port.
    public Vector3 OriginalHomePortPosition =>
        _originalHomePort != null ? _originalHomePort.transform.position : Vector3.zero;

    // Consume EventSystem drag events so they don't bubble up to parent DragPrefab components.
    public void OnBeginDrag(PointerEventData eventData) { }
    public void OnDrag(PointerEventData eventData)      { }
    public void OnEndDrag(PointerEventData eventData)   { }

    private void Start()
    {
        if (homePort == null)
            homePort = GetComponentInParent<CablePort>();

        // Lock in the original port once at startup — homePort is updated at runtime on installs.
        _originalHomePort = homePort;

        // Auto-resolve hardwareHolder by direct object reference if not wired in the inspector.
        // Name-based matching is fragile; this check is always reliable.
        if (hardwareHolder == null)
            hardwareHolder = FindHardwareHolderForThis();

        _installedLocalPos = transform.localPosition;
        _installedLocalScale = transform.localScale;

        if (powerButtonSource != null)
            _powerButton = powerButtonSource as IPowerButton;

        // Detect monitor cables by checking whether the home port lives inside a MonitorController.
        // If so, and monitorPowerGate isn't wired in the inspector, auto-resolve it so that
        // ForceOff (triggered by the system unit turning off) correctly unblocks these cables.
        _isMonitorCable = homePort != null &&
                          homePort.GetComponentInParent<MonitorController>(true) != null;
        if (_isMonitorCable && monitorPowerGate == null)
            monitorPowerGate = FindObjectOfType<MonitorPowerButton>(true);
    }

    // Finds the HardwareHolder that owns this cable.
    //
    // Pass 0: multi-cable holder whose managedCables list contains this cable.
    //         This covers the shared-proxy case where hardwarePrefab is not set on the holder.
    // Pass 1: direct hardwarePrefab reference + parent container active (multi-topic containers).
    // Pass 2: direct hardwarePrefab reference, any hierarchy (single-container fallback).
    // Pass 3: name match (legacy cables without a direct scene-object reference).
    private HardwareHolder FindHardwareHolderForThis()
    {
        // Pass 0: multi-cable proxy that explicitly lists this cable.
        foreach (HardwareHolder h in FindObjectsOfType<HardwareHolder>(true))
        {
            if (h.ContainsManagedCable(this)) return h;
        }
        // Pass 1: direct reference + parent container is active.
        foreach (HardwareHolder h in FindObjectsOfType<HardwareHolder>(true))
        {
            if (h.hardwarePrefab != gameObject) continue;
            Transform p = h.transform.parent;
            bool parentActive = p == null || p.gameObject.activeInHierarchy;
            if (parentActive) return h;
        }
        // Pass 2: direct reference, any hierarchy.
        foreach (HardwareHolder h in FindObjectsOfType<HardwareHolder>(true))
        {
            if (h.hardwarePrefab == gameObject) return h;
        }
        // Pass 3: name match (legacy cables without a direct scene-object reference).
        foreach (HardwareHolder h in FindObjectsOfType<HardwareHolder>(true))
        {
            if (h.hardwarePrefab != null && h.hardwarePrefab.name == gameObject.name)
                return h;
        }
        return null;
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
        {
            _holdTarget = this;
            if (Camera.main != null)
            {
                Vector2 sp = mouse.position.ReadValue();
                _holdStartMouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(sp.x, sp.y, 10f));
                _holdStartMouseWorld.z = 0f;
            }
        }

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
        // Monitor cables bypass the generic power gate when the monitor itself is off.
        // This covers the auto-ForceOff path (system unit turns off → monitor turns off)
        // where the upstream source (e.g. AVR) may still be on.
        bool monitorExplicitlyOff = _isMonitorCable &&
                                    monitorPowerGate != null &&
                                    !monitorPowerGate.IsPoweredOn;

        if (!monitorExplicitlyOff && _powerButton != null && _powerButton.IsPoweredOn)
        {
            ActivityLogManager.Log($"Cannot unplug {LogName} — turn off its power source first.", ActivityLogManager.EntryType.Warning);
            UnableAnimation.Shake(transform);
            return false;
        }
        if (secondaryPowerGate != null && secondaryPowerGate.IsPoweredOn)
        {
            ActivityLogManager.Log($"Cannot unplug {LogName} — turn off the System Unit first.", ActivityLogManager.EntryType.Warning);
            UnableAnimation.Shake(transform);
            return false;
        }
        if (monitorPowerGate != null && monitorPowerGate.IsPoweredOn)
        {
            ActivityLogManager.Log($"Cannot unplug {LogName} — turn off the Monitor first.", ActivityLogManager.EntryType.Warning);
            UnableAnimation.Shake(transform);
            return false;
        }
        if (psuSwitchGate != null && psuSwitchGate.IsOn)
        {
            ActivityLogManager.Log($"Cannot unplug {LogName} — turn off the PSU switch first.", ActivityLogManager.EntryType.Warning);
            UnableAnimation.Shake(transform);
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
        ActivityLogManager.Log($"{LogName} unplugged", ActivityLogManager.EntryType.Remove);

        // Always refresh the cached holder ref in case scene context changed (multi-topic containers).
        // Pass 0 in FindHardwareHolderForThis handles multi-cable holders correctly.
        hardwareHolder = FindHardwareHolderForThis();

        // SetParent with worldPositionStays=true already preserves world scale.
        transform.SetParent(GameManager.Instance.worldRoot, true);

        // Grab offset based on where the hold *started*, not where the cursor is now.
        // This carries mouse movement during the hold into the cable's initial position,
        // so the cable appears at the cursor the instant it detaches.
        _grabOffset = transform.position - _holdStartMouseWorld;

        Mouse grabMouse = Mouse.current;
        Vector3 initPos = transform.position;
        if (grabMouse != null && Camera.main != null)
        {
            Vector2 mp = grabMouse.position.ReadValue();
            Vector3 cursorNow = Camera.main.ScreenToWorldPoint(new Vector3(mp.x, mp.y, 10f));
            cursorNow.z = 0f;
            initPos = cursorNow + _grabOffset;
            initPos.z = 0f;
        }
        transform.position = initPos;

        _dragIndicator = new GameObject("CableDragIndicator");
        SpriteRenderer sr = _dragIndicator.AddComponent<SpriteRenderer>();
        sr.sprite = GetComponent<SpriteRenderer>()?.sprite;
        sr.sortingOrder = 999;
        _dragIndicator.transform.position = initPos;
        _dragIndicator.transform.localScale = transform.lossyScale;

        CableDragManager.Instance.Register(this);

        // Show the proxy immediately on detach so the player sees the cable is available.
        // For multi-cable holders this enables the shared proxy; for single holders this is a no-op.
        hardwareHolder?.UpdateProxyVisibility();

        Debug.Log($"[CableBehavior] {cableType} detached.");

        // CableDragManager already ran its Update() this frame (execution order 0 < 1),
        // so DragUpdate() won't be called until next frame — causing a one-frame lag where
        // the cable sits at the port while the cursor has already moved. Drive the first
        // position update right now so the cable is at the cursor the instant it detaches.
        _isDragging = true;
        DragUpdate();
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
            else
                return;
            // fall through — update position on this same frame so the cable
            // instantly follows the cursor the moment it detaches
        }

        if (mouse.leftButton.isPressed)
        {
            Vector2 mp = mouse.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mp.x, mp.y, 10f));
            worldPos.z = 0f;
            worldPos += _grabOffset;
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
            WalkthroughGuideManager.Instance?.NotifyBackCableUnplugged();
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

        // Force the physics engine to sync with the new transform position.
        Physics2D.SyncTransforms();

        // Update cache so SnapBack always uses the correct port-relative values.
        _installedLocalPos   = Vector3.zero;
        _installedLocalScale = transform.localScale;

        gameObject.SetActive(true);
        port.SetInstalled();

        // Notify the holder so it can recalculate proxy visibility.
        // For multi-cable holders: proxy hides only when all cable ends are installed.
        // For single holders: proxy hides immediately (same as old SetActive(false)).
        hardwareHolder?.OnCableInstalled(this);

        ActivityLogManager.Log($"{LogName} plugged in", ActivityLogManager.EntryType.Install);
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
            // Re-evaluate proxy visibility — another managed cable may still be uninstalled,
            // in which case the proxy should stay visible even though this cable returned.
            hardwareHolder?.UpdateProxyVisibility();
            Debug.Log($"[CableBehavior] {cableType} snapped back to {homePort.name}.");
        }
        else
        {
            gameObject.SetActive(false);
            hardwareHolder?.UpdateProxyVisibility();
            Debug.LogWarning($"[CableBehavior] {cableType} snap back failed — no homePort.");
        }
    }

    private void SendToHolder()
    {
        // Always search fresh — the inspector-wired or previously cached ref may belong to a
        // different topic's container (e.g. shared FlashDrive used in both Topic 2 and Topic 3).
        hardwareHolder = FindHardwareHolderForThis();

        if (hardwareHolder != null)
        {
            // OnCableStored deactivates this specific cable end and updates proxy visibility.
            // For single holders it mirrors the old StoreHardware() behaviour exactly.
            hardwareHolder.OnCableStored(this);
            return;
        }

        Debug.LogWarning($"[CableBehavior] No HardwareHolder found for '{gameObject.name}' — hiding cable.");
        gameObject.SetActive(false);
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            if (!col.enabled) continue;
            // Skip colliders with a near-zero size — they can never be clicked.
            if (col is BoxCollider2D box && (box.size.x < 0.01f || box.size.y < 0.01f)) continue;
            if (col.OverlapPoint(mouseWorld)) return true;
        }

        // Fallback: use the SpriteRenderer world bounds so a misconfigured collider
        // doesn't silently break hold-to-detach.
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            return sr.bounds.Contains(new Vector3(mouseWorld.x, mouseWorld.y, sr.bounds.center.z));

        return false;
    }
}
