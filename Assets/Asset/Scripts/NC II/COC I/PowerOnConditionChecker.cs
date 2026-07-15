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

    [Header("PSU Switch (System Unit Back)")]
    [SerializeField] private PSUSwitchController psuSwitch;

    [Header("AVR Power Button")]
    [SerializeField] private AVRPowerButton avrPowerButton;

    [Header("SystemUnit Back Ports")]
    [SerializeField] private CablePort sUVGAPort;
    [SerializeField] private CablePort sUPSUPort;

    [Header("Monitor Back Ports")]
    [SerializeField] private CablePort mVGACableSlot;
    [SerializeField] private CablePort mPCableSlot;

    [Header("AVR Back Ports")]
    [SerializeField] private CablePort aPSUSlot;
    [SerializeField] private CablePort aMCableSlot;

    [Header("SystemUnit Side Hardware Slots")]
    [SerializeField] private Transform motherboardSlot;
    [SerializeField] private Transform psuSlot;

    [Header("Motherboard Component Slots")]
    [SerializeField] private CPUSlotController cpuSlotController;
    [SerializeField] private CPUController cpuController;
    [SerializeField] private HeatsinkController heatsinkController;
    [SerializeField] private Transform gpuSlot;
    [SerializeField] private Transform ssdSlot;
    [SerializeField] private Transform cmosSlot;
    [SerializeField] private Transform ramSlot1;
    [SerializeField] private Transform ramSlot2;

    [Header("Motherboard Cables")]
    [SerializeField] private CablePort cableSlot2;
    [SerializeField] private CablePort cableSlot3;

    public bool CanTurnOn()
    {
        bool pass = true;

        if (coverController == null || coverController.IsOpen())
        { Warn("Cannot power on — close the System Unit cover first."); pass = false; }

        if (!IsScrewed(screw1)) { Warn("Cannot power on — tighten cover screw 1 first."); pass = false; }
        if (!IsScrewed(screw2)) { Warn("Cannot power on — tighten cover screw 2 first."); pass = false; }
        if (!IsScrewed(screw3)) { Warn("Cannot power on — tighten cover screw 3 first."); pass = false; }
        if (!IsScrewed(screw4)) { Warn("Cannot power on — tighten cover screw 4 first."); pass = false; }

        if (psuSwitch == null || !psuSwitch.IsOn)
        { Warn("Cannot power on — turn on the PSU switch on the System Unit back first."); pass = false; }

        if (avrPowerButton == null || !avrPowerButton.IsPoweredOn)
        { Warn("Cannot power on — turn on the AVR first."); pass = false; }

        if (!IsPortInstalled(sUVGAPort))  { Warn("Cannot power on — plug in the VGA cable to the System Unit back."); pass = false; }
        if (!IsPortInstalled(sUPSUPort))  { Warn("Cannot power on — plug in the PSU cable to the System Unit back."); pass = false; }

        if (!IsPortInstalled(mVGACableSlot)) { Warn("Cannot power on — plug in the VGA cable to the monitor."); pass = false; }
        if (!IsPortInstalled(mPCableSlot))   { Warn("Cannot power on — plug in the power cable to the monitor."); pass = false; }

        if (!IsPortInstalled(aPSUSlot))    { Warn("Cannot power on — plug in the PSU cable to the AVR."); pass = false; }
        if (!IsPortInstalled(aMCableSlot)) { Warn("Cannot power on — plug in the monitor cable to the AVR."); pass = false; }

        if (!HasChild(motherboardSlot)) { Warn("Cannot power on — install the motherboard first."); pass = false; }
        if (!HasChild(psuSlot))         { Warn("Cannot power on — install the PSU first."); pass = false; }

        // CPU: must be seated in slot
        if (cpuSlotController == null || !cpuSlotController.HasCPU())
        { Warn("Cannot power on — install the CPU first."); pass = false; }

        // CPU: thermal paste must be applied before mounting the heatsink
        if (cpuController == null || cpuController.CurrentPasteState != CPUController.PasteState.PasteApplied)
        { Warn("Cannot power on — apply thermal paste to the CPU first."); pass = false; }

        // CPU: lock lever must be in the closed (locked) position
        if (cpuSlotController == null || !cpuSlotController.IsLockClosed)
        { Warn("Cannot power on — close the CPU lock lever first."); pass = false; }

        // Heatsink: must be in slot, fan cable connected (slid up), and all screws tightened
        if (heatsinkController == null || !heatsinkController.IsFullyInstalled)
        { Warn("Cannot power on — fully install the heatsink (connect the fan cable and tighten the screws)."); pass = false; }

        // GPU: must be latched, cables connected, and all screws tightened
        GPUController gpu = gpuSlot != null ? gpuSlot.GetComponentInChildren<GPUController>() : null;
        if (gpu == null || !gpu.IsFullyInstalled)
        { Warn("Cannot power on — fully install the GPU (latch, cables, and screws)."); pass = false; }

        if (!HasChild(ssdSlot))  { Warn("Cannot power on — install the SSD first."); pass = false; }
        if (!HasChild(cmosSlot)) { Warn("Cannot power on — install the CMOS battery first."); pass = false; }

        // RAM: at least one stick must be snapped to its slot AND latches engaged (slid down)
        if (!IsRAMProperlyInstalled(ramSlot1) && !IsRAMProperlyInstalled(ramSlot2))
        { Warn("Cannot power on — install and lock at least one RAM stick first."); pass = false; }

        if (!IsPortInstalled(cableSlot2)) { Warn("Cannot power on — connect motherboard cable 2 first."); pass = false; }
        if (!IsPortInstalled(cableSlot3)) { Warn("Cannot power on — connect motherboard cable 3 first."); pass = false; }

        if (pass) Debug.Log("[PowerOn] All conditions met — power on allowed.");
        return pass;
    }

    private void Warn(string message)
    {
        ActivityLogManager.Log(message, ActivityLogManager.EntryType.Warning);
        Debug.Log($"[PowerOn] {message}");
    }

    private bool HasChild(Transform slot)     => slot != null && slot.childCount > 0;
    private bool IsScrewed(ScrewController s) => s != null && s.IsScrewed();
    private bool IsPortInstalled(CablePort p) => p != null && p.IsInstalled;

    // Checks that a RAM stick is physically in the slot AND its latches are engaged (IsInstalled).
    private bool IsRAMProperlyInstalled(Transform slot)
    {
        if (slot == null || slot.childCount == 0) return false;
        RAMController ram = slot.GetComponentInChildren<RAMController>();
        return ram != null && ram.IsInstalled;
    }
}
