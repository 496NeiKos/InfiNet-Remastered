using System;
using UnityEngine;

/// <summary>
/// Unified cable port. Replaces BackPortSlot and CableSlot.
/// Tracks installed state, accepts matching cable types, and notifies hardware
/// controllers for indicator updates. Attach a BoxCollider2D (trigger) to act
/// as the drop zone for CableBehavior installation.
/// </summary>
public class CablePort : MonoBehaviour
{
    // Fired the moment a cable is plugged in. T3TaskListManager subscribes for Task 1.
    public event Action OnInstalled;
    // Fired the moment a cable is unplugged. T3TaskListManager and UEFISettingButton subscribe.
    public event Action OnUninstalled;
    [Header("Accepted Cables")]
    [Tooltip("Cable type strings this port accepts. One entry for a single cable, more for shared ports.")]
    [SerializeField] private string[] acceptedCableTypes = new string[0];

    [Tooltip("Legacy single-type field from old CableSlot. Auto-migrated to acceptedCableTypes on Awake if array is empty.")]
    [SerializeField] private string cableType = "";

    [Header("State")]
    [Tooltip("Tick for ports that start empty (no cable installed by default).")]
    [SerializeField] private bool startEmpty = false;

    [Header("Prerequisite (optional)")]
    [Tooltip("This port only accepts a cable when the referenced port is already installed.")]
    [SerializeField] private CablePort prerequisitePort;
    [Tooltip("This port only accepts a cable when the referenced slot already has a component installed (e.g. PSU must be in PSUSlot).")]
    [SerializeField] private SlotContainer prerequisiteSlot;

    [Header("Controller Notifications (optional)")]
    [SerializeField] private GPUController gpuController;
    [SerializeField] private HDDController hddController;
    [SerializeField] private MotherboardController motherboardController;

    // Initialized true so ports inside initially-inactive hierarchies (e.g. HDDDetailed)
    // still report Installed before their Awake runs. Awake re-applies !startEmpty.
    private bool _isInstalled = true;

    public bool IsInstalled => _isInstalled;
    public bool IsUninstalled => !_isInstalled;

    private void Awake()
    {
        _isInstalled = !startEmpty;

        // Migrate legacy single cableType field into the array if the array is empty.
        if ((acceptedCableTypes == null || acceptedCableTypes.Length == 0) && !string.IsNullOrEmpty(cableType))
            acceptedCableTypes = new[] { cableType };
    }

    public bool CanAcceptCable(string type)
    {
        if (string.IsNullOrEmpty(type)) return false;
        if (!IsPrerequisiteMet()) return false;

        // Explicit accepted types from inspector
        if (acceptedCableTypes != null)
            foreach (string t in acceptedCableTypes)
                if (t == type) return true;

        // Legacy migrated single cableType field
        if (!string.IsNullOrEmpty(cableType) && cableType == type) return true;

        // GameObject name fallback — preserves old BackPortSlot behavior
        if (gameObject.name == type || gameObject.name.Contains(type)) return true;

        return false;
    }

    public bool IsPrerequisiteMet()
    {
        if (prerequisitePort != null && !prerequisitePort.IsInstalled) return false;
        if (prerequisiteSlot != null && !prerequisiteSlot.HasInstalledChild()) return false;
        return true;
    }

    public void SetInstalled()
    {
        _isInstalled = true;
        NotifyControllers();
        OnInstalled?.Invoke();
        Debug.Log($"[CablePort] {name} → Installed");
    }

    public void SetUninstalled()
    {
        _isInstalled = false;
        NotifyControllers();
        OnUninstalled?.Invoke();
        Debug.Log($"[CablePort] {name} → Uninstalled");
    }

    private void NotifyControllers()
    {
        // Explicit inspector refs first, then auto-find via parent hierarchy.
        GPUController gpu = gpuController != null ? gpuController : GetComponentInParent<GPUController>(true);
        HDDController hdd = hddController != null ? hddController : GetComponentInParent<HDDController>(true);
        MotherboardController mb = motherboardController != null ? motherboardController : GetComponentInParent<MotherboardController>(true);

        gpu?.RefreshCableSprite();
        hdd?.RefreshCableSprite();
        mb?.RefreshCableSprite();
    }
}
