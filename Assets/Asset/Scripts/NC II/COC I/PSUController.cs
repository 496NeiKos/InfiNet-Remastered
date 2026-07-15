using UnityEngine;

/// <summary>
/// On the PSU object in the system unit side view.
/// Exposes CanBeRemoved, which DragPrefab checks before allowing drag-out.
/// The back PSU port cable, mobo ATX cable, PSU-CPU cable, and PSU-MOBO cable
/// must all be disconnected before the PSU can be removed.
/// </summary>
public class PSUController : MonoBehaviour
{
    [Header("System Unit Back — PSU cable port")]
    [SerializeField] private BackPortSlot psuBackPort;

    [Header("Motherboard Phase 1 — ATX cable slot (CablePSU-MOBO)")]
    [SerializeField] private CableSlot psuMoboCableSlot;

    [Header("Motherboard Phase 1 — GPU power cable slot")]
    [SerializeField] private CableSlot psuGpuCableSlot;

    [Header("Motherboard — PSU power cables (must be unplugged before removal)")]
    [SerializeField] private CablePort cableSlotPsuCpu;
    [SerializeField] private CablePort cableSlotPsuMobo;

    [Header("HDD — PSU cable (must be unplugged before removal)")]
    [SerializeField] private CablePort cableSlotHddPsu;

    public bool CanBeRemoved
    {
        get
        {
            if (psuBackPort != null && !psuBackPort.IsUninstalled) return false;
            if (psuMoboCableSlot != null && psuMoboCableSlot.IsInstalled) return false;
            if (psuGpuCableSlot != null && psuGpuCableSlot.IsInstalled) return false;
            if (cableSlotPsuCpu != null && cableSlotPsuCpu.IsInstalled) return false;
            if (cableSlotPsuMobo != null && cableSlotPsuMobo.IsInstalled) return false;
            if (cableSlotHddPsu != null && cableSlotHddPsu.IsInstalled) return false;
            return true;
        }
    }
}
