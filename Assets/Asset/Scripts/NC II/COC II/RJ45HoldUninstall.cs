using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to the RJ45 hardware prefab.
/// While the RJ45 is installed in a slot, click-and-hold for holdDuration seconds to uninstall.
/// Uninstalling returns the RJ45 to storage and re-shows the icon proxy in the hardware area,
/// so the player can pick it up again from there like normal.
/// </summary>
public class RJ45HoldUninstall : MonoBehaviour
{
    [Tooltip("Seconds the player must hold the click to uninstall.")]
    [SerializeField] private float holdDuration = 2f;

    private NetworkCableEndController _cableEnd;
    private NetworkHardwareHolder _hardwareHolder;
    private bool _holding;
    private float _holdTimer;

    private void Awake()
    {
        var oldConnector = GetComponent<RJ45Connector>();
        if (oldConnector != null) oldConnector.enabled = false;

        var drag = GetComponent<NetworkDragPrefab>();
        if (drag != null)
        {
            _hardwareHolder = drag.hardwareHolder;
            drag.enabled    = false;
        }
    }

    /// <summary>Called by NetworkCableEndController after reparenting into slot.</summary>
    public void OnInstalled(NetworkCableEndController cableEnd)
    {
        _cableEnd  = cableEnd;
        _holding   = false;
        _holdTimer = 0f;
    }

    /// <summary>
    /// Sends the RJ45 back to hardware storage and shows the icon proxy.
    /// Called by NetworkCableEndController during uninstall.
    /// </summary>
    public void StoreImmediately()
    {
        _holding   = false;
        _holdTimer = 0f;
        _hardwareHolder?.StoreHardware();
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 worldPt = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D col  = GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(worldPt))
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
                _cableEnd?.UninstallRJ45();
            }
        }
    }
}
