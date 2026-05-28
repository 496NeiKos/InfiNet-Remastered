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
        { Warn("Cannot power on — close the System Unit cover first."); pass = false; }

        // Cover screws
        if (!IsScrewed(screw1)) { Warn("Cannot power on — tighten cover screw 1 first."); pass = false; }
        if (!IsScrewed(screw2)) { Warn("Cannot power on — tighten cover screw 2 first."); pass = false; }
        if (!IsScrewed(screw3)) { Warn("Cannot power on — tighten cover screw 3 first."); pass = false; }
        if (!IsScrewed(screw4)) { Warn("Cannot power on — tighten cover screw 4 first."); pass = false; }

        // PSU switch (system unit back) must be on
        if (psuSwitch == null || !psuSwitch.IsOn)
        { Warn("Cannot power on — turn on the PSU switch on the System Unit back first."); pass = false; }

        // AVR must be powered on
        if (avrPowerButton == null || !avrPowerButton.IsPoweredOn)
        { Warn("Cannot power on — turn on the AVR first."); pass = false; }

        // SystemUnit back ports
        if (!IsPortInstalled(sUVGAPort))  { Warn("Cannot power on — plug in the VGA cable to the System Unit back."); pass = false; }
        if (!IsPortInstalled(sUPSUPort))  { Warn("Cannot power on — plug in the PSU cable to the System Unit back."); pass = false; }

        // Monitor back ports
        if (!IsPortInstalled(mVGACableSlot)) { Warn("Cannot power on — plug in the VGA cable to the monitor."); pass = false; }
        if (!IsPortInstalled(mPCableSlot))   { Warn("Cannot power on — plug in the power cable to the monitor."); pass = false; }

        // AVR back ports
        if (!IsPortInstalled(aPSUSlot))    { Warn("Cannot power on — plug in the PSU cable to the AVR."); pass = false; }
        if (!IsPortInstalled(aMCableSlot)) { Warn("Cannot power on — plug in the monitor cable to the AVR."); pass = false; }

        // SystemUnit side hardware
        if (!HasChild(motherboardSlot)) { Warn("Cannot power on — install the motherboard first."); pass = false; }
        if (!HasChild(hddSlot))         { Warn("Cannot power on — install the HDD first."); pass = false; }
        if (!HasChild(psuSlot))         { Warn("Cannot power on — install the PSU first."); pass = false; }

        // Motherboard components
        if (cpuSlotController == null || !cpuSlotController.HasCPU())
        { Warn("Cannot power on — install the CPU first."); pass = false; }

        if (cpuSlotController == null || !cpuSlotController.IsHeatsinkInstalled)
        { Warn("Cannot power on — install the heatsink first."); pass = false; }

        if (!HasChild(gpuSlot))  { Warn("Cannot power on — install the GPU first."); pass = false; }
        if (!HasChild(ssdSlot))  { Warn("Cannot power on — install the SSD first."); pass = false; }
        if (!HasChild(cmosSlot)) { Warn("Cannot power on — install the CMOS battery first."); pass = false; }

        if (!HasChild(ramSlot1) && !HasChild(ramSlot2))
        { Warn("Cannot power on — install at least one RAM stick first."); pass = false; }

        // Motherboard cables
        if (!IsCableInstalled(cableSlot1)) { Warn("Cannot power on — connect motherboard cable 1 first."); pass = false; }
        if (!IsCableInstalled(cableSlot2)) { Warn("Cannot power on — connect motherboard cable 2 first."); pass = false; }
        if (!IsCableInstalled(cableSlot3)) { Warn("Cannot power on — connect motherboard cable 3 first."); pass = false; }

        if (pass) Debug.Log("[PowerOn] All conditions met — power on allowed.");
        return pass;
    }

    private void Warn(string message)
    {
        ActivityLogManager.Log(message, ActivityLogManager.EntryType.Warning);
        Debug.Log($"[PowerOn] {message}");
    }

    private bool HasChild(Transform slot)       => slot != null && slot.childCount > 0;
    private bool IsScrewed(ScrewController s)   => s != null && s.IsScrewed();
    private bool IsCableInstalled(CableSlot c)  => c != null && c.IsInstalled();
    private bool IsPortInstalled(BackPortSlot p) => p != null && p.IsInstalled;
}
