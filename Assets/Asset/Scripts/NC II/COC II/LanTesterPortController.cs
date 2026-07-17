using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to LanTesterPort (inside TopDetail view of the LanTester).
/// Drag the NetworkCable icon proxy onto this port to install the cable.
/// Click-and-hold for holdDuration seconds on the installed cable to uninstall —
/// the cable is returned to hardware storage and the icon proxy reappears.
///
/// Requires a BoxCollider2D on LanTesterPort for reliable click detection;
/// falls back to SpriteRenderer bounds if absent.
/// </summary>
public class LanTesterPortController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The CableInstalledIndicator GameObject in FrontDetail — enabled when cable is installed.")]
    [SerializeField] private GameObject cableInstalledIndicator;

    [Header("Hold to Uninstall")]
    [SerializeField] private float holdDuration = 2f;

    public bool IsCableInstalled { get; private set; }

    private NetworkHardwareHolder _hardwareHolder;
    private GameObject _installedCable;
    private Collider2D _col;
    private SpriteRenderer _sr;
    private bool _holding;
    private float _holdTimer;

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

    /// <summary>
    /// Called by NetworkHardwareHolder when the cable icon proxy is dropped onto this port.
    /// Activates the cable hardware object and reparents it into the port.
    /// </summary>
    public bool InstallCable(NetworkHardwareHolder source)
    {
        if (IsCableInstalled) return false;
        if (source == null || source.hardwarePrefab == null) return false;

        // Both cable ends must have a crimped RJ45 before the port accepts the cable.
        var ends = source.hardwarePrefab.GetComponentsInChildren<NetworkCableEndController>(true);
        foreach (var end in ends)
            if (!end.IsCrimped) return false;

        IsCableInstalled  = true;
        _hardwareHolder   = source;
        _installedCable   = source.hardwarePrefab;

        Vector3 worldScale = _installedCable.transform.lossyScale;
        _installedCable.SetActive(true);
        _installedCable.transform.SetParent(transform, false);
        RestoreWorldScale(_installedCable.transform, worldScale);
        _installedCable.transform.localPosition = Vector3.zero;

        // Suspend cable's own drag and detail-view interaction while installed
        var drag     = _installedCable.GetComponent<NetworkDragPrefab>();
        var interact = _installedCable.GetComponent<NetworkPrefabInteraction>();
        if (drag     != null) drag.enabled     = false;
        if (interact != null) interact.enabled = false;

        if (cableInstalledIndicator != null)
            cableInstalledIndicator.SetActive(true);

        _holding   = false;
        _holdTimer = 0f;
        return true;
    }

    /// <summary>
    /// Returns the cable to hardware storage (proxy icon reappears in the hardware area).
    /// Called after the hold-uninstall timer completes.
    /// </summary>
    public void UninstallCable()
    {
        if (!IsCableInstalled) return;

        IsCableInstalled = false;
        _holding         = false;
        _holdTimer       = 0f;

        if (_installedCable != null)
        {
            // Re-enable components before StoreHardware deactivates the object
            var drag     = _installedCable.GetComponent<NetworkDragPrefab>();
            var interact = _installedCable.GetComponent<NetworkPrefabInteraction>();
            if (drag     != null) drag.enabled     = true;
            if (interact != null) interact.enabled = true;
        }

        _hardwareHolder?.StoreHardware(); // reparents cable to storage, shows proxy icon
        _installedCable = null;
        _hardwareHolder = null;

        if (cableInstalledIndicator != null)
            cableInstalledIndicator.SetActive(false);
    }

    private void Update()
    {
        if (!IsCableInstalled || Mouse.current == null) return;

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
                UninstallCable();
            }
        }
    }

    private bool IsClickOnPortOrCable(Vector2 worldPt)
    {
        // Port's own collider / bounds
        if (_col != null && _col.OverlapPoint(worldPt)) return true;
        if (_col == null && _sr != null && _sr.bounds.Contains(worldPt)) return true;

        // Installed cable's collider
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
