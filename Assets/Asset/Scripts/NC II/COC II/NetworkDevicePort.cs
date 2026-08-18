using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to any workspace device (Computer, Router, PatchPanel, Switch) that can
/// accept logical network cable connections. Tracks all connected cables and enforces
/// a per-device connection limit.
///
/// Also handles double-click detection on the device anchor to open the
/// NetworkCablePopupManager for cable management (Move End / Remove Cable).
/// Double-click is suppressed while any cable is pending its second end,
/// while the popup is already open, or while a detail editor is open.
/// </summary>
public class NetworkDevicePort : MonoBehaviour
{
    [Tooltip("Child Transform placed at the device's visual center — used as the cable line anchor. Falls back to this transform if unassigned.")]
    [SerializeField] private Transform cableAnchor;

    [Tooltip("Maximum number of logical cables that can connect to this device at once.")]
    [SerializeField] public int maxConnections = 4;

    [Tooltip("World-unit radius within which a click counts as 'on this anchor' for the double-click popup.")]
    [SerializeField] private float popupClickRadius = 1f;

    private readonly List<NetworkLogicalCable> _connectedCables = new List<NetworkLogicalCable>();

    public IReadOnlyList<NetworkLogicalCable> ConnectedCables => _connectedCables;

    // Double-click tracking.
    private float _lastClickTime        = -99f;
    private const float DoubleClickThreshold = 0.3f;

    // ----------------------------------------------------------------
    //  Public API
    // ----------------------------------------------------------------

    public Vector3 GetAnchorWorldPosition() =>
        cableAnchor != null ? cableAnchor.position : transform.position;

    public bool CanAcceptCable() => _connectedCables.Count < maxConnections;

    public void AcceptCable(NetworkLogicalCable cable)
    {
        if (cable != null && !_connectedCables.Contains(cable))
            _connectedCables.Add(cable);
    }

    public void DisconnectCable(NetworkLogicalCable cable)
    {
        _connectedCables.Remove(cable);
    }

    // ----------------------------------------------------------------
    //  Double-click detection — opens cable popup
    // ----------------------------------------------------------------

    private void Update()
    {
        if (Mouse.current == null)                                               return;
        if (_connectedCables.Count == 0)                                         return;
        if (NetworkLogicalCable.AnyPendingSecondEnd)                             return;
        if (NetworkCablePopupManager.IsOpen)                                     return;
        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen)  return;
        if (!Mouse.current.leftButton.wasPressedThisFrame)                       return;

        // Check if click landed within the anchor interaction radius.
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld  = Camera.main.ScreenToWorldPoint(
            new Vector3(mouseScreen.x, mouseScreen.y, 10f));
        mouseWorld.z = 0f;

        if (Vector3.Distance(GetAnchorWorldPosition(), mouseWorld) > popupClickRadius) return;

        // Double-click detection.
        float now = Time.unscaledTime;
        if (now - _lastClickTime <= DoubleClickThreshold)
        {
            _lastClickTime = -99f;  // reset so a triple-click doesn't re-trigger
            NetworkCablePopupManager.Instance?.Show(this, mouseScreen);
        }
        else
        {
            _lastClickTime = now;
        }
    }
}
