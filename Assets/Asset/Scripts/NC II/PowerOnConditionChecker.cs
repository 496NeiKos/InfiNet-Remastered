using UnityEngine;

public class PowerOnConditionChecker : MonoBehaviour
{
    [Header("Cover")]
    [SerializeField] private CoverController coverController;

    [Header("Cover Screws")]
    [SerializeField] private ScrewController screw1;
    [SerializeField] private ScrewController screw2;
    [SerializeField] private ScrewController screw3;
    [SerializeField] private ScrewController screw4;

    [Header("SystemUnit Back Ports")]
    [SerializeField] private BackPortSlot sUVGAPort;
    [SerializeField] private BackPortSlot sUPSUPort;

    [Header("Monitor Back Ports")]
    [SerializeField] private BackPortSlot mVGACableSlot;
    [SerializeField] private BackPortSlot mPCableSlot;

    [Header("AVR Back Ports")]
    [SerializeField] private BackPortSlot aPSUSlot;
    [SerializeField] private BackPortSlot aMCableSlot;

    [Header("SystemUnit Side Hardware Slots")]
    [SerializeField] private Transform motherboardSlot;
    [SerializeField] private Transform hddSlot;
    [SerializeField] private Transform psuSlot;

    [Header("Motherboard Component Slots")]
    [SerializeField] private CPUSlotController cpuSlotController;
    [SerializeField] private Transform gpuSlot;
    [SerializeField] private Transform ssdSlot;
    [SerializeField] private Transform cmosSlot;
    [SerializeField] private Transform ramSlot1;
    [SerializeField] private Transform ramSlot2;

    [Header("Motherboard Cables")]
    [SerializeField] private CableSlot cableSlot1;
    [SerializeField] private CableSlot cableSlot2;
    [SerializeField] private CableSlot cableSlot3;

    public bool CanTurnOn()
    {
        bool pass = true;

        // Cover
        if (coverController == null || coverController.IsOpen())
        { Debug.Log("[PowerOn] FAIL — Cover is open."); pass = false; }

        // Cover screws
        if (!IsScrewed(screw1)) { Debug.Log("[PowerOn] FAIL — Screw 1 not tightened."); pass = false; }
        if (!IsScrewed(screw2)) { Debug.Log("[PowerOn] FAIL — Screw 2 not tightened."); pass = false; }
        if (!IsScrewed(screw3)) { Debug.Log("[PowerOn] FAIL — Screw 3 not tightened."); pass = false; }
        if (!IsScrewed(screw4)) { Debug.Log("[PowerOn] FAIL — Screw 4 not tightened."); pass = false; }

        // SystemUnit back ports
        if (!IsPortInstalled(sUVGAPort))  { Debug.Log("[PowerOn] FAIL — SU VGA back cable not plugged in."); pass = false; }
        if (!IsPortInstalled(sUPSUPort))  { Debug.Log("[PowerOn] FAIL — SU PSU back cable not plugged in."); pass = false; }

        // Monitor back ports
        if (!IsPortInstalled(mVGACableSlot)) { Debug.Log("[PowerOn] FAIL — Monitor VGA cable not plugged in."); pass = false; }
        if (!IsPortInstalled(mPCableSlot))   { Debug.Log("[PowerOn] FAIL — Monitor power cable not plugged in."); pass = false; }

        // AVR back ports
        if (!IsPortInstalled(aPSUSlot))    { Debug.Log("[PowerOn] FAIL — AVR PSU cable not plugged in."); pass = false; }
        if (!IsPortInstalled(aMCableSlot)) { Debug.Log("[PowerOn] FAIL — AVR monitor cable not plugged in."); pass = false; }

        // SystemUnit side hardware
        if (!HasChild(motherboardSlot)) { Debug.Log("[PowerOn] FAIL — Motherboard not installed."); pass = false; }
        if (!HasChild(hddSlot))         { Debug.Log("[PowerOn] FAIL — HDD not installed."); pass = false; }
        if (!HasChild(psuSlot))         { Debug.Log("[PowerOn] FAIL — PSU not installed."); pass = false; }

        // Motherboard components
        if (cpuSlotController == null || !cpuSlotController.HasCPU())
        { Debug.Log("[PowerOn] FAIL — CPU not installed."); pass = false; }

        if (cpuSlotController == null || !cpuSlotController.IsHeatsinkInstalled)
        { Debug.Log("[PowerOn] FAIL — Heatsink not installed."); pass = false; }

        if (!HasChild(gpuSlot))  { Debug.Log("[PowerOn] FAIL — GPU not installed."); pass = false; }
        if (!HasChild(ssdSlot))  { Debug.Log("[PowerOn] FAIL — SSD not installed."); pass = false; }
        if (!HasChild(cmosSlot)) { Debug.Log("[PowerOn] FAIL — CMOS not installed."); pass = false; }

        if (!HasChild(ramSlot1) && !HasChild(ramSlot2))
        { Debug.Log("[PowerOn] FAIL — No RAM installed (at least 1 required)."); pass = false; }

        // Motherboard cables
        if (!IsCableInstalled(cableSlot1)) { Debug.Log("[PowerOn] FAIL — MB Cable 1 not attached."); pass = false; }
        if (!IsCableInstalled(cableSlot2)) { Debug.Log("[PowerOn] FAIL — MB Cable 2 not attached."); pass = false; }
        if (!IsCableInstalled(cableSlot3)) { Debug.Log("[PowerOn] FAIL — MB Cable 3 not attached."); pass = false; }

        if (pass) Debug.Log("[PowerOn] All conditions met — power on allowed.");
        return pass;
    }

    private bool HasChild(Transform slot)       => slot != null && slot.childCount > 0;
    private bool IsScrewed(ScrewController s)   => s != null && s.IsScrewed();
    private bool IsCableInstalled(CableSlot c)  => c != null && c.IsInstalled();
    private bool IsPortInstalled(BackPortSlot p) => p != null && p.IsInstalled;
}
