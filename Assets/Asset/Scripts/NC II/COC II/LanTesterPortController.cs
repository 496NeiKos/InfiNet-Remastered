using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to LanTesterPort (inside TopDetail view of the LanTester).
/// Drag the NetworkCable icon proxy onto this port to install the cable.
///
/// Uninstall — click-and-hold on the installed cable sprite in TopDetail:
///   1. Hold for holdDuration seconds on the installed cable.
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

    [Header("Cable Sprites")]
    [Tooltip("Sprite shown on the cable when it is installed in the port.")]
    [SerializeField] private Sprite installedCableSprite;

    [Header("Hold to Detach")]
    private const float holdDuration = 1f;

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
    private int        _originalSortingOrder;
    private Sprite     _originalCableSprite;

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

        IsCableInstalled = true;
        _hardwareHolder  = source;
        _installedCable  = source.hardwarePrefab;

        _installedCable.SetActive(true);
        _installedCable.transform.SetParent(transform, false);
        _installedCable.transform.localScale    = Vector3.one;
        _installedCable.transform.localPosition = Vector3.zero;

        var cableSR = _installedCable.GetComponentInChildren<SpriteRenderer>(true);
        if (cableSR != null)
        {
            _originalSortingOrder = cableSR.sortingOrder;
            _originalCableSprite  = cableSR.sprite;
            if (installedCableSprite != null)
                cableSR.sprite = installedCableSprite;
        }

        SetCableInteractable(false);

        if (cableInstalledIndicator != null)
            cableInstalledIndicator.SetActive(true);

        ActivityLogManager.Log("Cable connected to LAN Tester port", ActivityLogManager.EntryType.Install);
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
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (float.IsNaN(mousePos.x) || float.IsNaN(mousePos.y)) return;
            Vector2 worldPt = Camera.main.ScreenToWorldPoint(mousePos);
            if (IsCableHit(worldPt))
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

    // Returns true if worldPt overlaps the installed cable's collider or sprite bounds.
    private bool IsCableHit(Vector2 worldPt)
    {
        if (_installedCable == null) return false;
        var col = _installedCable.GetComponentInChildren<Collider2D>(true);
        if (col != null) return col.OverlapPoint(worldPt);
        var sr = _installedCable.GetComponentInChildren<SpriteRenderer>(true);
        if (sr  != null) return sr.bounds.Contains(worldPt);
        return false;
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

        // Restore original sprite before the cable re-enters the world.
        var cableSR = _installedCable.GetComponentInChildren<SpriteRenderer>(true);
        if (cableSR != null)
        {
            if (_originalCableSprite != null)
                cableSR.sprite = _originalCableSprite;
            cableSR.sortingOrder = _originalSortingOrder;
        }

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
            Vector2 mp = mouse.position.ReadValue();
            if (float.IsNaN(mp.x) || float.IsNaN(mp.y)) return;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mp.x, mp.y, 10f));
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
        ActivityLogManager.Log("Cable returned to storage from LAN Tester", ActivityLogManager.EntryType.Remove);
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
        _installedCable.transform.localScale    = Vector3.one;
        _installedCable.transform.localPosition = Vector3.zero;

        var cableSR = _installedCable.GetComponentInChildren<SpriteRenderer>(true);
        if (cableSR != null)
        {
            if (installedCableSprite != null)
                cableSR.sprite = installedCableSprite;
            cableSR.sortingOrder = _originalSortingOrder;
        }

        SetCableInteractable(false);

        if (cableInstalledIndicator != null)
            cableInstalledIndicator.SetActive(true);

        ActivityLogManager.Log("Cable reconnected to LAN Tester port", ActivityLogManager.EntryType.Install);
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

}
