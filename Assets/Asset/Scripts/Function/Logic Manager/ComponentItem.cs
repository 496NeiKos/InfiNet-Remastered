using UnityEngine;

public class ComponentItem : MonoBehaviour
{
    public enum ComponentType
    {
        CPU,
        RAM,
        HDD,
        SSD,
        GPU,
        PSU,
        CoolingSystem,
        BIOSChip,
        CMOSBattery,
        HeatSink,
        OpticalDrive
    }

    public ComponentType type;

    public string GetDescription()
    {
        switch (type)
        {
            case ComponentType.CPU:
                return "CPU: The 'brain' of the computer, responsible for executing instructions and performing calculations. Speed measured in GHz.";
            case ComponentType.RAM:
                return "RAM: Short-term memory storing data the CPU needs for immediate access. Allows smoother multitasking. Data can be accessed in random order.";
            case ComponentType.HDD:
                return "HDD: Data storage device using rotating magnetic platters. Stores and retrieves digital information.";
            case ComponentType.SSD:
                return "SSD: Flash memory storage, faster and more reliable than HDD, but more expensive per gigabyte.";
            case ComponentType.GPU:
                return "GPU: Processes visual data, crucial for gaming, video editing, and design. Dedicated GPUs outperform integrated ones.";
            case ComponentType.PSU:
                return "PSU: Converts AC power from wall outlet to DC power for computer components.";
            case ComponentType.CoolingSystem:
                return "Cooling System: Keeps the system unit cool to prevent overheating. Includes fans and heat sinks.";
            case ComponentType.BIOSChip:
                return "BIOS Chip: Contains firmware that initializes hardware and starts the boot process.";
            case ComponentType.CMOSBattery:
                return "CMOS Battery: CR2032 lithium coin cell that powers BIOS memory.";
            case ComponentType.HeatSink:
                return "Heat Sink: Passive heat exchanger that cools a device by dissipating heat into the surrounding medium.";
            case ComponentType.OpticalDrive:
                return "CD/DVD-ROM: Optical disc formats for storing and distributing data. DVD-ROMs have higher capacity than CD-ROMs.";
            default:
                return "Unknown component.";
        }
    }
}
