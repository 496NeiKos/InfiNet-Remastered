using UnityEngine;

public class HeatsinkController : MonoBehaviour
{
    private CPUSlotController _cpuSlot;

    private void Start()
    {
        // Cache on start while still parented to CPUSlot
        _cpuSlot = GetComponentInParent<CPUSlotController>();
    }

    // Called by DragPrefab.OnEndDrag when heatsink is dragged to hardware area
    public void OnRemovedFromSlot()
    {
        // Use cached ref — by this point Heatsink may already be reparented away from CPUSlot
        if (_cpuSlot != null)
        {
            _cpuSlot.OnHeatsinkUninstalled();
            Debug.Log("[HeatsinkController] Notified CPUSlotController: heatsink uninstalled.");
        }
        else
        {
            Debug.LogWarning("[HeatsinkController] _cpuSlot is null — CPUSlotController not notified.");
        }
    }

    // Called by HardwareHolder.TryInstallInSlot when reinstalled
    public void OnInstalledToSlot(CPUSlotController slot)
    {
        _cpuSlot = slot;
        _cpuSlot?.OnHeatsinkInstalled();
    }
}