using UnityEngine;

/// <summary>
/// Attached to SystemUnit root.
/// Validates all conditions required before the power button can be turned on.
/// Called by PowerButton on left-click when state is Off.
/// </summary>
public class PowerOnConditionChecker : MonoBehaviour
{
    [Header("Cover")]
    [SerializeField] private CoverController coverController;

    [Header("Cover Screws (all 4)")]
    [SerializeField] private ScrewController screw1;
    [SerializeField] private ScrewController screw2;
    [SerializeField] private ScrewController screw3;
    [SerializeField] private ScrewController screw4;

    [Header("SystemUnit Hardware Slots (direct Transform refs)")]
    [SerializeField] private Transform motherboardSlot;
    [SerializeField] private Transform hddSlot;
    [SerializeField] private Transform psuSlot;

    [Header("Motherboard Component Slots (direct Transform refs)")]
    [Tooltip("Assign the slot Transform directly — checked via childCount, works even when inactive")]
    [SerializeField] private Transform cpuSlot;
    [SerializeField] private Transform gpuSlot;
    [SerializeField] private Transform ramSlot1;
    [SerializeField] private Transform ramSlot2;
    [SerializeField] private Transform cmosSlot;

    [Header("Motherboard Cables")]
    [Tooltip("CableSlot.IsInstalled() reads internal state, not GameObject active — works when inactive")]
    [SerializeField] private CableSlot cableSlot1;
    [SerializeField] private CableSlot cableSlot2;
    [SerializeField] private CableSlot cableSlot3;
    [SerializeField] private CableSlot cableSlot4;

    [Header("Back Port Cables")]
    [SerializeField] private BackPortSlot psuBackPort;
    [SerializeField] private BackPortSlot vgaPort;

    public bool CanTurnOn()
    {
        bool pass = true;

        // Cover must be closed
        if (coverController == null || coverController.IsOpen())
        {
            Debug.Log("[PowerOn] FAIL — Cover is open.");
            pass = false;
        }

        // All 4 cover screws must be tightened
        if (!IsScrewed(screw1)) { Debug.Log("[PowerOn] FAIL — Screw 1 not tightened."); pass = false; }
        if (!IsScrewed(screw2)) { Debug.Log("[PowerOn] FAIL — Screw 2 not tightened."); pass = false; }
        if (!IsScrewed(screw3)) { Debug.Log("[PowerOn] FAIL — Screw 3 not tightened."); pass = false; }
        if (!IsScrewed(screw4)) { Debug.Log("[PowerOn] FAIL — Screw 4 not tightened."); pass = false; }

        // SystemUnit hardware slots — use SlotContainer (these Awake() fine, they're always active)
        if (!HasChild(motherboardSlot))
        {
            Debug.Log("[PowerOn] FAIL — Motherboard not installed.");
            pass = false;
        }

        if (!HasChild(hddSlot))
        {
            Debug.Log("[PowerOn] FAIL — HDD not installed.");
            pass = false;
        }

        if (!HasChild(psuSlot))
        {
            Debug.Log("[PowerOn] FAIL — PSU not installed.");
            pass = false;
        }

        // Motherboard component slots — use childCount directly (works even when inactive)
        if (!HasChild(cpuSlot)) { Debug.Log("[PowerOn] FAIL — CPU not installed."); pass = false; }
        if (!HasChild(gpuSlot)) { Debug.Log("[PowerOn] FAIL — GPU not installed."); pass = false; }
        if (!HasChild(cmosSlot)) { Debug.Log("[PowerOn] FAIL — CMOS not installed."); pass = false; }

        bool ramInstalled = HasChild(ramSlot1) || HasChild(ramSlot2);
        if (!ramInstalled)
        {
            Debug.Log("[PowerOn] FAIL — No RAM installed (at least 1 required).");
            pass = false;
        }

        // Motherboard cables — CableSlot._state is internal, not tied to active state
        if (!IsCableInstalled(cableSlot1)) { Debug.Log("[PowerOn] FAIL — MB Cable 1 not attached."); pass = false; }
        if (!IsCableInstalled(cableSlot2)) { Debug.Log("[PowerOn] FAIL — MB Cable 2 not attached."); pass = false; }
        if (!IsCableInstalled(cableSlot3)) { Debug.Log("[PowerOn] FAIL — MB Cable 3 not attached."); pass = false; }
        if (!IsCableInstalled(cableSlot4)) { Debug.Log("[PowerOn] FAIL — MB Cable 4 not attached."); pass = false; }

        // Back port cables
        if (psuBackPort == null || psuBackPort.IsUninstalled)
        {
            Debug.Log("[PowerOn] FAIL — PSU back cable not plugged in.");
            pass = false;
        }

        if (vgaPort == null || vgaPort.IsUninstalled)
        {
            Debug.Log("[PowerOn] FAIL — VGA cable not plugged in.");
            pass = false;
        }

        if (pass)
            Debug.Log("[PowerOn] All conditions met — power on allowed.");

        return pass;
    }

    // childCount > 0 works on inactive GameObjects — Unity always maintains the hierarchy
    private bool HasChild(Transform slot)
    {
        return slot != null && slot.childCount > 0;
    }

    private bool IsScrewed(ScrewController screw)
    {
        return screw != null && screw.IsScrewed();
    }

    private bool IsCableInstalled(CableSlot cable)
    {
        return cable != null && cable.IsInstalled();
    }
}