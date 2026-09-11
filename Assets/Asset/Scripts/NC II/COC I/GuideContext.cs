using System;
using UnityEngine;

public enum GuideContext
{
    None,

    // ── TOPIC 1 — Hardware Assembly ───────────────────────────────────

    // System Unit views (HardwareViewController)
    SystemUnit_Front,
    SystemUnit_Side,
    SystemUnit_Back,

    // Monitor views
    Monitor_Front,
    Monitor_Back,

    // AVR views
    AVR_Front,
    AVR_Back,

    // Motherboard — phase set by MotherboardPhaseManager (not INGContextProvider)
    Motherboard_Phase1,   // MB inside System Unit — screws + cables
    Motherboard_Phase2,   // MB standalone in workspace — components

    // Inner components (Detailed child GameObjects inside MB or SU)
    CPU,
    RAM,
    GPU,
    Heatsink,
    SSD,
    HDD,

    // Front panel connector sub-detail (FrontPanelDetailedView)
    FrontPanel,

    // ── TOPIC 2 — Rufus / Bootable USB ───────────────────────────────

    T2_Desktop,
    T2_Browser_Home,
    T2_Browser_RufusSearch,
    T2_Browser_RufusDownload,
    T2_Browser_ISOSearch,
    T2_Browser_ISODownload,
    T2_Rufus_Setup,

    // ── TOPIC 3 — Windows Setup ───────────────────────────────────────

    T3_UEFI,
    T3_PrivacySettings,
    T3_PasswordSetup,
    T3_PasswordLogin,
    T3_Desktop,
    T3_Settings,
    T3_DeviceManager,
    T3_CommandPrompt,
    T3_DriverInstall,
    T3_Partition,
}

[Serializable]
public class GuideContextEntry
{
    public GuideContext context;
    public string title;
    [TextArea(2, 5)] public string description;
}
