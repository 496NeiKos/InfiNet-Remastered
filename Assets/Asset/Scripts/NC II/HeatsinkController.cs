using UnityEngine;

public class HeatsinkController : MonoBehaviour
{
    private CPUSlotController _cpuSlot;
    private Vector3 _installedLocalScale;
    private Vector3 _installedLocalPosition;

    private void Start()
    {
        _cpuSlot = GetComponentInParent<CPUSlotController>();
        // Capture transform while correctly parented under CPUSlot
        _installedLocalScale = transform.localScale;
        _installedLocalPosition = transform.localPosition;
    }

    public Vector3 InstalledLocalScale => _installedLocalScale;
    public Vector3 InstalledLocalPosition => _installedLocalPosition;

    // Called by DragPrefab.OnEndDrag � slot ref passed directly since Heatsink
    // may have already moved away from CPUSlot hierarchy by this point
    public void OnRemovedFromSlot(CPUSlotController slot)
    {
        CPUSlotController target = slot != null ? slot : _cpuSlot;
        if (target != null)
        {
            target.OnHeatsinkUninstalled();
            Debug.Log("[HeatsinkController] Notified CPUSlotController: heatsink uninstalled.");
        }
        else
        {
            Debug.LogWarning("[HeatsinkController] No CPUSlotController found � state not updated.");
        }
    }

    // Called by HardwareHolder.TryInstallInSlot when reinstalled from hardware area
    public void OnInstalledToSlot(CPUSlotController slot)
    {
        _cpuSlot = slot;
        _cpuSlot?.OnHeatsinkInstalled();
    }
}