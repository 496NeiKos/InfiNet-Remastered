using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to LanTesterPort (inside TopDetail view of the LanTester).
/// Drag the NetworkCable icon proxy onto this port to install the cable.
///
/// Uninstall — mirrors RJ45HoldUninstall / CableBehavior pattern:
///   1. Click-and-hold for holdDuration seconds on the installed cable.
///   2. Cable detaches and becomes draggable (Update-driven, same as BackCable).
///   3. Drop on hardware area  → stored; icon proxy reappears.
///      Drop anywhere else     → snaps back and reinstalls into the port.
///
/// Requires a BoxCollider2D on LanTesterPort for reliable click detection;
/// falls back to SpriteRenderer bounds if absent.
/// </summary>
public class LanTesterPortController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The CableInstalledIndicator GameObject in FrontDetail — enabled when cable is installed.")]
    [SerializeField] private GameObject cableInstalledIndicator;

    [Header("Hold to Detach")]
    [SerializeField] private float holdDuration = 1f;

    public bool IsCableInstalled { get; private set; }

    private NetworkHardwareHolder _hardwareHolder;
    private GameObject            _installedCable;
    private Collider2D            _col;
    private SpriteRenderer        _sr;

    // Hold state
    private bool  _holding;
    private float _holdTimer;

    // Drag state
    private bool       _detached;
    private bool       _isDragging;
    private GameObject _dragIndicator;
    private Vector3    _cachedWorldScale;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _sr  = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (cableInstalledIndicator != null)
            cableInstalledIndicator.SetActive(false);
    }

    // ----------------------------------------------------------------
    //  Install
    // ----------------------------------------------------------------

    /// <summary>
    /// Called by NetworkHardwareHolder when the cable icon proxy is dropped onto this port.
    /// Both cable ends must be crimped before the port accepts the cable.
    /// </summary>
    public bool InstallCable(NetworkHardwareHolder source)
    {
        if (IsCableInstalled) return false;
        if (source == null || source.hardwarePrefab == null) return false;

        var ends = source.hardwarePrefab.GetComponentsInChildren<NetworkCableEndController>(true);
        foreach (var end in ends)
            if (!end.IsCrimped) return false;

        IsCableInstalled  = true;
        _hardwareHolder   = source;
        _installedCable   = source.hardwarePrefab;
        _cachedWorldScale = _installedCable.transform.lossyScale;

        _installedCable.SetActive(true);
        _installedCable.transform.SetParent(transform, false);
        RestoreWorldScale(_installedCable.transform, _cachedWorldScale);
        _installedCable.transform.localPosition = Vector3.zero;

        SetCableInteractable(false);

        if (cableInstalledIndicator != null)
            cableInstalledIndicator.SetActive(true);

        _holding   = false;
        _holdTimer = 0f;
        return true;
    }

    // ----------------------------------------------------------------
    //  Update — hold detection then drag driving
    // ----------------------------------------------------------------

    private void Update()
    {
        if (Mouse.current == null) return;

        if (_detached)
        {
            DragUpdate();
            return;
        }

        if (!IsCableInstalled) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 worldPt = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            if (IsClickOnPortOrCable(worldPt))
            {
                _holding   = true;
                _holdTimer = 0f;
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            _holding   = false;
            _holdTimer = 0f;
        }

        if (_holding)
        {
            _holdTimer += Time.deltaTime;
            if (_holdTimer >= holdDuration)
            {
                _holding   = false;
                _holdTimer = 0f;
                Detach();
            }
        }
    }

    // ----------------------------------------------------------------
    //  Detach — reparent to world root, spawn drag indicator
    // ----------------------------------------------------------------

    private void Detach()
    {
        IsCableInstalled = false;
        _detached        = true;
        _isDragging      = false;

        if (cableInstalledIndicator != null)
            cableInstalledIndicator.SetActive(false);

        // Reparent to world root before touching anything else so the cable
        // stays visible at its current world position.
        _cachedWorldScale = _installedCable.transform.lossyScale;
        _installedCable.transform.SetParent(GameManager.Instance.ActiveWorldContainer, true);

        // Spawn a drag indicator sprite that follows the cursor.
        SpriteRenderer sourceSR = _installedCable.GetComponentInChildren<SpriteRenderer>(true);
        _dragIndicator = new GameObject("CableDragIndicator");
        SpriteRenderer sr = _dragIndicator.AddComponent<SpriteRenderer>();
        sr.sprite       = sourceSR != null ? sourceSR.sprite : null;
        sr.sortingOrder = 999;
        _dragIndicator.transform.position   = _installedCable.transform.position;
        _dragIndicator.transform.localScale = _cachedWorldScale;

        ActivityLogManager.Log("Network cable removed from LAN tester", ActivityLogManager.EntryType.Remove);
    }

    // ----------------------------------------------------------------
    //  Drag — Update-driven mouse follow (same pattern as CableBehavior)
    // ----------------------------------------------------------------

    private void DragUpdate()
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
            _installedCable.transform.position = worldPos;
            if (_dragIndicator != null)
                _dragIndicator.transform.position = worldPos;
        }
        else
        {
            OnDragReleased();
        }
    }

    // ----------------------------------------------------------------
    //  Drop resolution
    // ----------------------------------------------------------------

    private void OnDragReleased()
    {
        if (_dragIndicator != null) { Object.Destroy(_dragIndicator); _dragIndicator = null; }
        _isDragging = false;
        _detached   = false;

        Vector2 screenPos   = Mouse.current.position.ReadValue();
        bool onHardwareArea = RectTransformUtility.RectangleContainsScreenPoint(
            GameManager.Instance.hardwareArea, screenPos, Camera.main);

        if (onHardwareArea)
        {
            SendToHolder();
        }
        else
        {
            SnapBack();
        }
    }

    // Drop on hardware area — store cable, show icon proxy
    private void SendToHolder()
    {
        SetCableInteractable(true);
        _hardwareHolder?.StoreHardware();
        _installedCable = null;
        _hardwareHolder = null;
        NetworkCableTaskManager.CheckConditions();
    }

    // Drop anywhere else — reinstall back into the port
    private void SnapBack()
    {
        IsCableInstalled = true;

        _installedCable.transform.SetParent(transform, false);
        RestoreWorldScale(_installedCable.transform, _cachedWorldScale);
        _installedCable.transform.localPosition = Vector3.zero;

        SetCableInteractable(false);

        if (cableInstalledIndicator != null)
            cableInstalledIndicator.SetActive(true);
    }

    // ----------------------------------------------------------------
    //  Helpers
    // ----------------------------------------------------------------

    private void SetCableInteractable(bool on)
    {
        if (_installedCable == null) return;
        var drag     = _installedCable.GetComponent<NetworkDragPrefab>();
        var interact = _installedCable.GetComponent<NetworkPrefabInteraction>();
        if (drag     != null) drag.enabled     = on;
        if (interact != null) interact.enabled = on;
    }

    private bool IsClickOnPortOrCable(Vector2 worldPt)
    {
        if (_col != null && _col.OverlapPoint(worldPt)) return true;
        if (_col == null && _sr != null && _sr.bounds.Contains(worldPt)) return true;

        if (_installedCable != null)
        {
            Collider2D cableCol = _installedCable.GetComponent<Collider2D>();
            if (cableCol != null && cableCol.OverlapPoint(worldPt)) return true;
        }

        return false;
    }

    private static void RestoreWorldScale(Transform t, Vector3 targetWorldScale)
    {
        t.localScale = Vector3.one;
        Vector3 ls = t.lossyScale;
        t.localScale = new Vector3(
            targetWorldScale.x / (ls.x != 0f ? ls.x : 1f),
            targetWorldScale.y / (ls.y != 0f ? ls.y : 1f),
            targetWorldScale.z / (ls.z != 0f ? ls.z : 1f));
    }
}
