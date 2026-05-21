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

    [Header("SystemUnit Hardware Slots")]
    [SerializeField] private Transform motherboardSlot;
    [SerializeField] private Transform hddSlot;
    [SerializeField] private Transform psuSlot;

    [Header("Motherboard Component Slots")]
    [SerializeField] private CPUSlotController cpuSlotController;
    [SerializeField] private Transform gpuSlot;
    [SerializeField] private Transform ramSlot1;
    [SerializeField] private Transform ramSlot2;
    [SerializeField] private Transform cmosSlot;

    [Header("Motherboard Cables")]
    [SerializeField] private CableSlot cableSlot1;
    [SerializeField] private CableSlot cableSlot2;
    [SerializeField] private CableSlot cableSlot3;

    [Header("SystemUnit Back Port Cables")]
    [SerializeField] private BackPortSlot psuBackPort;
    [SerializeField] private BackPortSlot vgaBackPort;

    [Header("External Port Cables")]
    [SerializeField] private BackPortSlot monitorVgaPort;
    [SerializeField] private BackPortSlot avrPsuPort;

    public bool CanTurnOn()
    {
        bool pass = true;

        if (coverController == null || coverController.IsOpen())
        { Debug.Log("[PowerOn] FAIL — Cover is open."); pass = false; }

        if (!IsScrewed(screw1)) { Debug.Log("[PowerOn] FAIL — Screw 1 not tightened."); pass = false; }
        if (!IsScrewed(screw2)) { Debug.Log("[PowerOn] FAIL — Screw 2 not tightened."); pass = false; }
        if (!IsScrewed(screw3)) { Debug.Log("[PowerOn] FAIL — Screw 3 not tightened."); pass = false; }
        if (!IsScrewed(screw4)) { Debug.Log("[PowerOn] FAIL — Screw 4 not tightened."); pass = false; }

        if (!HasChild(motherboardSlot)) { Debug.Log("[PowerOn] FAIL — Motherboard not installed."); pass = false; }
        if (!HasChild(hddSlot)) { Debug.Log("[PowerOn] FAIL — HDD not installed."); pass = false; }
        if (!HasChild(psuSlot)) { Debug.Log("[PowerOn] FAIL — PSU not installed."); pass = false; }

        if (cpuSlotController == null || !cpuSlotController.HasCPU()) { Debug.Log("[PowerOn] FAIL — CPU not installed."); pass = false; }
        if (!HasChild(gpuSlot)) { Debug.Log("[PowerOn] FAIL — GPU not installed."); pass = false; }
        if (!HasChild(cmosSlot)) { Debug.Log("[PowerOn] FAIL — CMOS not installed."); pass = false; }

        // At least 1 RAM slot must be occupied
        bool hasRam = HasChild(ramSlot1) || HasChild(ramSlot2);
        if (!hasRam) { Debug.Log("[PowerOn] FAIL — No RAM installed (at least 1 required)."); pass = false; }

        if (!IsCableInstalled(cableSlot1)) { Debug.Log("[PowerOn] FAIL — MB Cable 1 not attached."); pass = false; }
        if (!IsCableInstalled(cableSlot2)) { Debug.Log("[PowerOn] FAIL — MB Cable 2 not attached."); pass = false; }
        if (!IsCableInstalled(cableSlot3)) { Debug.Log("[PowerOn] FAIL — MB Cable 3 not attached."); pass = false; }

        if (psuBackPort == null || psuBackPort.IsUninstalled)
        { Debug.Log("[PowerOn] FAIL — SystemUnit PSU back cable not plugged in."); pass = false; }

        if (vgaBackPort == null || vgaBackPort.IsUninstalled)
        { Debug.Log("[PowerOn] FAIL — SystemUnit VGA back cable not plugged in."); pass = false; }

        if (monitorVgaPort == null || monitorVgaPort.IsUninstalled)
        { Debug.Log("[PowerOn] FAIL — Monitor VGA port cable not plugged in."); pass = false; }

        if (avrPsuPort == null || avrPsuPort.IsUninstalled)
        { Debug.Log("[PowerOn] FAIL — AVR PSU port cable not plugged in."); pass = false; }

        if (pass) Debug.Log("[PowerOn] All conditions met — power on allowed.");
        return pass;
    }

    private bool HasChild(Transform slot) => slot != null && slot.childCount > 0;
    private bool IsScrewed(ScrewController s) => s != null && s.IsScrewed();
    private bool IsCableInstalled(CableSlot c) => c != null && c.IsInstalled();
}