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
    [SerializeField] private CablePort psuBackPort;

    [Header("Motherboard Phase 1 — ATX cable slot (CablePSU-MOBO)")]
    [SerializeField] private CablePort psuMoboCableSlot;

    [Header("Motherboard Phase 1 — GPU power cable slot")]
    [SerializeField] private CablePort psuGpuCableSlot;

    [Header("Motherboard — PSU power cables (must be unplugged before removal)")]
    [SerializeField] private CablePort cableSlotPsuCpu;
    [SerializeField] private CablePort cableSlotPsuMobo;

    [Header("HDD — PSU cable (must be unplugged before removal)")]
    [SerializeField] private CablePort cableSlotHddPsu;

    [Header("System Unit Back — PSU mounting screws (must be unscrewed before removal)")]
    [SerializeField] private ScrewController psuScrew1;
    [SerializeField] private ScrewController psuScrew2;
    [SerializeField] private ScrewController psuScrew3;
    [SerializeField] private ScrewController psuScrew4;

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
            if (psuScrew1 != null && !psuScrew1.IsEmpty()) return false;
            if (psuScrew2 != null && !psuScrew2.IsEmpty()) return false;
            if (psuScrew3 != null && !psuScrew3.IsEmpty()) return false;
            if (psuScrew4 != null && !psuScrew4.IsEmpty()) return false;
            return true;
        }
    }
}
