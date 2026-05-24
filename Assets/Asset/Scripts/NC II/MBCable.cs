using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On each cable child object inside a MB Phase 1 cable slot.
/// Hold 2s to detach — fully Update()-based, bypasses EventSystem/DragPrefab gates.
/// Once detached (_detached=true), drag continues even if this component is disabled
/// by MotherboardPhaseManager, because OnDragUpdate is called from a separate path.
/// </summary>
[DefaultExecutionOrder(1)] // runs after MotherboardPhaseManager
public class MBCable : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string cableType;

    [Header("Hardware Area Icon")]
    [SerializeField] private HardwareHolder hardwareHolder;

    [Header("Hold Settings")]
    [SerializeField] private float holdDuration = 2f;

    private CableSlot _parentSlot;
    private Transform _originalParent;
    private Vector3 _originalLocalPos;
    private Vector3 _originalLocalScale;

    private float _holdTimer = 0f;
    private bool _detached = false;
    private bool _isDragging = false;
    private GameObject _dragIndicator;

    public bool IsDetached => _detached;

    // Registered with a manager so drag continues even when this component is disabled
    private static MBCableDragManager _dragManager;
    // Only one cable may accumulate hold time at a time; set on the frame the press starts
    private static MBCable _holdTarget;

    private void Start()
    {
        _parentSlot = GetComponentInParent<CableSlot>();

        // Ensure drag manager exists
        if (_dragManager == null)
        {
            GameObject go = new GameObject("MBCableDragManager");
            _dragManager = go.AddComponent<MBCableDragManager>();
            DontDestroyOnLoad(go);
        }
    }

    private void Update()
    {
        if (_detached) return; // drag handled by MBCableDragManager once detached

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // Block new holds while any cable is already being dragged
        if (_dragManager != null && _dragManager.HasActiveDrag)
        {
            _holdTimer = 0f;
            if (_holdTarget == this) _holdTarget = null;
            return;
        }

        // Claim the hold target only on the exact frame the press begins over this cable
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
                    Detach();
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

    private void Detach()
    {
        _holdTimer = 0f;
        _detached = true;
        _isDragging = false;

        _originalParent = transform.parent;
        _originalLocalPos = transform.localPosition;
        _originalLocalScale = transform.localScale;

        _parentSlot?.SetUninstalled();

        Vector3 worldLossy = transform.lossyScale;
        transform.SetParent(GameManager.Instance.worldRoot, true);
        transform.localScale = worldLossy;

        _dragIndicator = new GameObject("MBCableDragIndicator");
        SpriteRenderer sr = _dragIndicator.AddComponent<SpriteRenderer>();
        sr.sprite = GetComponent<SpriteRenderer>()?.sprite;
        sr.sortingOrder = 999;
        _dragIndicator.transform.position = transform.position;
        _dragIndicator.transform.localScale = transform.lossyScale;

        // Register with drag manager so drag continues regardless of enabled state
        _dragManager?.Register(this);

        Debug.Log($"[MBCable] {cableType} detached.");
    }

    // Called by MBCableDragManager every frame while detached
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
            // Released
            OnDragReleased();
        }
    }

    private void OnDragReleased()
    {
        if (_dragIndicator != null) { Destroy(_dragIndicator); _dragIndicator = null; }

        _isDragging = false;
        _dragManager?.Unregister(this);

        Vector2 screenPos = Mouse.current.position.ReadValue();
        bool onHardwareArea = RectTransformUtility.RectangleContainsScreenPoint(
            GameManager.Instance.hardwareArea, screenPos, Camera.main);

        if (onHardwareArea)
        {
            SendToHolder();
            Debug.Log($"[MBCable] {cableType} stored to hardware area.");
        }
        else
        {
            SnapBack();
            Debug.Log($"[MBCable] {cableType} snapped back to slot.");
        }
    }

    public void InstallToSlot(CableSlot slot)
    {
        _parentSlot = slot;
        _detached = false;
        _isDragging = false;

        transform.SetParent(slot.transform, false);
        transform.localPosition = _originalLocalPos;
        transform.localScale = _originalLocalScale;
        gameObject.SetActive(true);

        slot.SetInstalled();
        Debug.Log($"[MBCable] {cableType} installed to slot.");
    }

    private void SnapBack()
    {
        _detached = false;
        transform.SetParent(_originalParent, false);
        transform.localPosition = _originalLocalPos;
        transform.localScale = _originalLocalScale;
        _parentSlot?.SetInstalled();
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

        Debug.LogWarning($"[MBCable] No HardwareHolder found for '{gameObject.name}'.");
        gameObject.SetActive(false);
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D col = GetComponent<Collider2D>();
        return col != null && col.OverlapPoint(mouseWorld);
    }

    public string GetCableType() => cableType;
}