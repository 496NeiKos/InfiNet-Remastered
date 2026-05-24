using UnityEngine;

/// <summary>
/// On the PSU object in the system unit side view.
/// Exposes CanBeRemoved, which DragPrefab checks before allowing drag-out.
/// Both the back PSU port cable and the mobo ATX cable must be disconnected.
/// </summary>
public class PSUController : MonoBehaviour
{
    [Header("System Unit Back — PSU cable port")]
    [SerializeField] private BackPortSlot psuBackPort;

    [Header("Motherboard Phase 1 — ATX cable slot (CablePSU-MOBO)")]
    [SerializeField] private CableSlot psuMoboCableSlot;

    public bool CanBeRemoved
    {
        get
        {
            if (psuBackPort != null && !psuBackPort.IsUninstalled) return false;
            if (psuMoboCableSlot != null && psuMoboCableSlot.IsInstalled()) return false;
            return true;
        }
    }
}
