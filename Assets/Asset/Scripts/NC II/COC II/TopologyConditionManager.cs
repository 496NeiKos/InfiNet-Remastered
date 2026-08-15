/*
 * ================================================================
 *  UNITY SETUP GUIDE — TopologyConditionManager
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to a dedicated always-active GameObject in the scene root.
 *    Wire it into VirtualOSManager via Inspector.
 *
 *  INSPECTOR ASSIGNMENTS
 *
 *  wifiDeviceEntries         → all three devices (Computer1, Computer2, Laptop → PatchPanel)
 *                              Laptop uses a physical cable during WiFi config because the
 *                              Access Point is not yet configured and wireless is unavailable.
 *    deviceID                → Computer1 / Computer2 / Laptop
 *    ownPort                 → that device's NetworkDevicePhase2Manager
 *    connectsToPort          → PatchPanel's NetworkDevicePhase2Manager
 *
 *  ipDeviceEntries           → wired devices only (Computer1, Computer2 → PatchPanel)
 *                              Laptop is excluded — it connects wirelessly for IP config.
 *
 *  wifiSharedEdgesRouter     → edges required when configuring the ROUTER
 *                              e.g. [0] endpointA=Router  endpointB=PatchPanel
 *
 *  wifiSharedEdgesAP         → edges required when configuring the ACCESS POINT
 *                              e.g. [0] endpointA=AccessPoint  endpointB=PatchPanel
 *
 *  ipSharedEdges             → edges required by ALL devices for IP config
 *                              e.g. [0] Switch→Router
 *
 *  ipDeviceExtraEdges        → per-device additional edges for IP config
 *                              Computer1: [0] PatchPanel→Switch
 *                              Computer2: [0] PatchPanel→Switch
 *                              Laptop:    [0] AccessPoint→Switch
 *
 *  HOW IT WORKS
 *    IsWifiRouterSatisfied(device) → wifiDeviceEntries[device] + wifiSharedEdgesRouter
 *    IsWifiAPSatisfied(device)     → wifiDeviceEntries[device] + wifiSharedEdgesAP
 *    IsIPSatisfied(device)         → ipDeviceEntries[device] (skipped if absent) +
 *                                    ipSharedEdges + ipDeviceExtraEdges[device]
 *
 *    An edge passes only when:
 *      • A Phase1 logical cable connects endpointA to endpointB
 *      • Phase2 is installed on endpointA's socket for that cable
 *      • Phase2 is installed on endpointB's socket for that cable
 *    All edges in the relevant set must pass for the condition to be met.
 *    Port references are resolved once in Start() — no GetComponent in hot paths.
 * ================================================================
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public class TopologyConditionManager : MonoBehaviour
{
    // ----------------------------------------------------------------
    //  Serialized data types
    // ----------------------------------------------------------------

    [Serializable]
    public class DeviceEntry
    {
        public DeviceID deviceID;
        [Tooltip("This device's NetworkDevicePhase2Manager.")]
        public NetworkDevicePhase2Manager ownPort;
        [Tooltip("The Phase2Manager it must connect to (typically PatchPanel).")]
        public NetworkDevicePhase2Manager connectsToPort;
    }

    [Serializable]
    public struct RequiredEdge
    {
        public NetworkDevicePhase2Manager endpointA;
        public NetworkDevicePhase2Manager endpointB;
    }

    [Serializable]
    public class DeviceExtraEdgeSet
    {
        public DeviceID       deviceID;
        public RequiredEdge[] extraEdges;
    }

    // ----------------------------------------------------------------
    //  Inspector fields
    // ----------------------------------------------------------------

    [Header("WiFi Phase — Device Entries (all three devices → PatchPanel)")]
    [SerializeField] private DeviceEntry[] wifiDeviceEntries;

    [Header("IP Phase — Device Entries (wired devices only — Laptop excluded)")]
    [SerializeField] private DeviceEntry[] ipDeviceEntries;

    [Header("WiFi — Router target edges (e.g. Router → PatchPanel)")]
    [SerializeField] private RequiredEdge[] wifiSharedEdgesRouter;

    [Header("WiFi — Access Point target edges (e.g. AccessPoint → PatchPanel)")]
    [SerializeField] private RequiredEdge[] wifiSharedEdgesAP;

    [Header("IP Shared Edges (apply to all devices, e.g. Switch → Router)")]
    [SerializeField] private RequiredEdge[] ipSharedEdges;

    [Header("IP Extra Edges Per Device (e.g. Computer1/2: PatchPanel→Switch; Laptop: AP→Switch)")]
    [SerializeField] private DeviceExtraEdgeSet[] ipDeviceExtraEdges;

    // ----------------------------------------------------------------
    //  Resolved (cached) internal types
    // ----------------------------------------------------------------

    private struct ResolvedEdge
    {
        public NetworkDevicePort          PortA;
        public NetworkDevicePort          PortB;
        public NetworkDevicePhase2Manager ManagerA;
        public NetworkDevicePhase2Manager ManagerB;
    }

    private struct ResolvedDeviceEntry
    {
        public NetworkDevicePort          OwnNetworkPort;
        public NetworkDevicePort          ConnectsToNetworkPort;
        public NetworkDevicePhase2Manager OwnManager;
        public NetworkDevicePhase2Manager ConnectsToManager;
    }

    private readonly Dictionary<DeviceID, ResolvedDeviceEntry> _wifiDeviceCache =
        new Dictionary<DeviceID, ResolvedDeviceEntry>();

    private readonly Dictionary<DeviceID, ResolvedDeviceEntry> _ipDeviceCache =
        new Dictionary<DeviceID, ResolvedDeviceEntry>();

    private ResolvedEdge[] _resolvedWifiRouter = Array.Empty<ResolvedEdge>();
    private ResolvedEdge[] _resolvedWifiAP     = Array.Empty<ResolvedEdge>();
    private ResolvedEdge[] _resolvedIPShared   = Array.Empty<ResolvedEdge>();

    private readonly Dictionary<DeviceID, ResolvedEdge[]> _resolvedIPExtras =
        new Dictionary<DeviceID, ResolvedEdge[]>();

    // ----------------------------------------------------------------
    //  Lifecycle — resolve all port references once
    // ----------------------------------------------------------------

    private void Start()
    {
        ResolveDeviceEntries(wifiDeviceEntries, _wifiDeviceCache);
        ResolveDeviceEntries(ipDeviceEntries,   _ipDeviceCache);

        _resolvedWifiRouter = ResolveEdges(wifiSharedEdgesRouter);
        _resolvedWifiAP     = ResolveEdges(wifiSharedEdgesAP);
        _resolvedIPShared   = ResolveEdges(ipSharedEdges);

        if (ipDeviceExtraEdges != null)
            foreach (DeviceExtraEdgeSet set in ipDeviceExtraEdges)
                _resolvedIPExtras[set.deviceID] = ResolveEdges(set.extraEdges);

        LogResolutionDiagnostics();
    }

    private void LogResolutionDiagnostics()
    {
        foreach (var kvp in _wifiDeviceCache)
            LogEntryDiagnostic("WiFi", kvp.Key, kvp.Value);
        foreach (var kvp in _ipDeviceCache)
            LogEntryDiagnostic("IP", kvp.Key, kvp.Value);
        LogEdgesDiagnostic("WiFi-Router", _resolvedWifiRouter);
        LogEdgesDiagnostic("WiFi-AP",     _resolvedWifiAP);
        LogEdgesDiagnostic("IP-Shared",   _resolvedIPShared);
        foreach (var kvp in _resolvedIPExtras)
            LogEdgesDiagnostic($"IP-Extra[{kvp.Key}]", kvp.Value);
    }

    private static void LogEntryDiagnostic(string category, DeviceID device, ResolvedDeviceEntry entry)
    {
        if (entry.OwnNetworkPort == null)
            Debug.LogWarning($"[TopologyConditionManager] {category}[{device}] ownPort has no NetworkDevicePort on the same GameObject.");
        if (entry.ConnectsToNetworkPort == null)
            Debug.LogWarning($"[TopologyConditionManager] {category}[{device}] connectsToPort has no NetworkDevicePort on the same GameObject.");
    }

    private static void LogEdgesDiagnostic(string label, ResolvedEdge[] edges)
    {
        for (int i = 0; i < edges.Length; i++)
        {
            if (edges[i].PortA == null)
                Debug.LogWarning($"[TopologyConditionManager] {label}[{i}] endpointA has no NetworkDevicePort on the same GameObject.");
            if (edges[i].PortB == null)
                Debug.LogWarning($"[TopologyConditionManager] {label}[{i}] endpointB has no NetworkDevicePort on the same GameObject.");
        }
    }

    // ----------------------------------------------------------------
    //  Public API
    // ----------------------------------------------------------------

    public bool IsWifiRouterSatisfied(DeviceID device)
    {
        if (!CheckDeviceEntry(_wifiDeviceCache, device)) return false;
        foreach (ResolvedEdge edge in _resolvedWifiRouter)
            if (!CheckEdge(edge)) return false;
        return true;
    }

    public bool IsWifiAPSatisfied(DeviceID device)
    {
        if (!CheckDeviceEntry(_wifiDeviceCache, device)) return false;
        foreach (ResolvedEdge edge in _resolvedWifiAP)
            if (!CheckEdge(edge)) return false;
        return true;
    }

    public bool IsIPSatisfied(DeviceID device)
    {
        // IP device entry is optional — Laptop has none (connects wirelessly for IP config).
        if (_ipDeviceCache.TryGetValue(device, out ResolvedDeviceEntry entry))
        {
            if (!CheckEdge(entry.OwnNetworkPort, entry.OwnManager,
                           entry.ConnectsToNetworkPort, entry.ConnectsToManager))
                return false;
        }

        foreach (ResolvedEdge edge in _resolvedIPShared)
            if (!CheckEdge(edge)) return false;

        if (_resolvedIPExtras.TryGetValue(device, out ResolvedEdge[] extras))
            foreach (ResolvedEdge edge in extras)
                if (!CheckEdge(edge)) return false;

        return true;
    }

    // ----------------------------------------------------------------
    //  Private helpers
    // ----------------------------------------------------------------

    private bool CheckDeviceEntry(Dictionary<DeviceID, ResolvedDeviceEntry> cache, DeviceID device)
    {
        if (!cache.TryGetValue(device, out ResolvedDeviceEntry entry))
        {
            Debug.LogWarning($"[TopologyConditionManager] No entry for {device}.");
            return false;
        }
        return CheckEdge(entry.OwnNetworkPort, entry.OwnManager,
                         entry.ConnectsToNetworkPort, entry.ConnectsToManager);
    }

    private static bool CheckEdge(ResolvedEdge edge) =>
        CheckEdge(edge.PortA, edge.ManagerA, edge.PortB, edge.ManagerB);

    private static bool CheckEdge(
        NetworkDevicePort portA,          NetworkDevicePhase2Manager managerA,
        NetworkDevicePort portB,          NetworkDevicePhase2Manager managerB)
    {
        if (portA == null || portB == null || managerA == null || managerB == null)
            return false;

        foreach (NetworkLogicalCable cable in portA.ConnectedCables)
        {
            if (cable.GetOtherPort(portA) != portB) continue;
            if (managerA.IsPhase2InstalledFor(cable) && managerB.IsPhase2InstalledFor(cable))
                return true;
        }
        return false;
    }

    private static void ResolveDeviceEntries(
        DeviceEntry[]                              entries,
        Dictionary<DeviceID, ResolvedDeviceEntry>  cache)
    {
        if (entries == null) return;
        foreach (DeviceEntry entry in entries)
        {
            cache[entry.deviceID] = new ResolvedDeviceEntry
            {
                OwnManager            = entry.ownPort,
                ConnectsToManager     = entry.connectsToPort,
                OwnNetworkPort        = entry.ownPort        != null
                    ? entry.ownPort.GetComponent<NetworkDevicePort>()        : null,
                ConnectsToNetworkPort = entry.connectsToPort != null
                    ? entry.connectsToPort.GetComponent<NetworkDevicePort>() : null
            };
        }
    }

    private static ResolvedEdge[] ResolveEdges(RequiredEdge[] edges)
    {
        if (edges == null || edges.Length == 0) return Array.Empty<ResolvedEdge>();
        var resolved = new ResolvedEdge[edges.Length];
        for (int i = 0; i < edges.Length; i++)
        {
            RequiredEdge e = edges[i];
            resolved[i] = new ResolvedEdge
            {
                ManagerA = e.endpointA,
                ManagerB = e.endpointB,
                PortA    = e.endpointA != null ? e.endpointA.GetComponent<NetworkDevicePort>() : null,
                PortB    = e.endpointB != null ? e.endpointB.GetComponent<NetworkDevicePort>() : null
            };
        }
        return resolved;
    }
}
