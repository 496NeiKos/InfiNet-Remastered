using UnityEngine;

/// <summary>
/// On Heatsink object (child of HeatsinkSlot, which is child of CPU).
/// Manages install/uninstall to/from CPU's HeatsinkSlot.
/// Notifies CPUController of heatsink state.
/// </summary>
public class HeatsinkController : MonoBehaviour
{
    // HeatsinkSlot is the parent Transform on the CPU that the heatsink snaps to.
    // Tracked so we can snap back on invalid drops.
    private Transform _heatsinkSlot;
    private CPUController _cpuController;

    private void Start()
    {
        // Parent is HeatsinkSlot, grandparent is CPU
        _heatsinkSlot = transform.parent;

        if (_heatsinkSlot != null)
            _cpuController = _heatsinkSlot.GetComponentInParent<CPUController>();

        if (_cpuController != null)
            _cpuController.SetHeatsinkInstalled(true);
    }

    public void OnInstalledToCPU(Transform heatsinkSlot)
    {
        _heatsinkSlot = heatsinkSlot;
        _cpuController = heatsinkSlot.GetComponentInParent<CPUController>();
        _cpuController?.SetHeatsinkInstalled(true);
    }

    public void OnRemovedFromCPU()
    {
        _cpuController?.SetHeatsinkInstalled(false);
    }

    public Transform GetHeatsinkSlot() => _heatsinkSlot;
}