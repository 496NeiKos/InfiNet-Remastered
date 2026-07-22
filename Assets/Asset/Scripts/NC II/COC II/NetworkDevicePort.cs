using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to any workspace device (Computer, Router, PatchPanel, Switch) that can
/// accept logical network cable connections. Tracks all connected cables and enforces
/// a per-device connection limit.
/// </summary>
public class NetworkDevicePort : MonoBehaviour
{
    [Tooltip("Child Transform placed at the device's visual center — used as the cable line anchor. Falls back to this transform if unassigned.")]
    [SerializeField] private Transform cableAnchor;

    [Tooltip("Maximum number of logical cables that can connect to this device at once.")]
    [SerializeField] public int maxConnections = 4;

    private readonly List<NetworkLogicalCable> _connectedCables = new List<NetworkLogicalCable>();

    public IReadOnlyList<NetworkLogicalCable> ConnectedCables => _connectedCables;

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
}
