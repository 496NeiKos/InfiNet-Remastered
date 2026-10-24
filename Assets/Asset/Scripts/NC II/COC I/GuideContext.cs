using System;
using UnityEngine;

public enum GuideContext
{
    None,

    // ── WORKSPACE ────────────────────────────────────────────────────
    // Pushed by INGContextProvider on each topic's worldRootContainer.
    // Shown without an editor open; topic index matches the active topic.
    Workspace_Topic1,
    Workspace_Topic2,
    Workspace_Topic3,

    // ── TOPIC 1 — Hardware Assembly ──────────────────────────────────
    // Pushed by INGContextProvider on systemUnitSide.
    SystemUnit_SideCoverPanel,

    // Pushed by INGContextProvider on SU back, Monitor back, and AVR back.
    Hardware_DetailView,

    // Pushed by MotherboardPhaseManager (replace pattern, not INGContextProvider).
    Motherboard_InSystemUnit,   // MB detail view opened while MB is inside the System Unit
    Motherboard_InWorkspace,    // MB detail view opened while MB is in the workspace

    // ── TOPIC 2 — Rufus / Bootable USB ───────────────────────────────
    // Pushed by INGContextProvider on their respective panel GameObjects.
    T2_Desktop,         // Desktop panel (panels[0] in T2MonitorNavigator)
    T2_Rufus,           // Rufus Set Up panel (child of Desktop)
    T2_CommandPrompt,   // CommandPromptApp panel (child of Desktop)

    // ── TOPIC 3 — Windows Setup ───────────────────────────────────────
    // Pushed by INGContextProvider on their respective panel GameObjects.
    T3_UEFI,            // UEFIPanel
    T3_WindowsSetup,    // SetUpInitialize or SetUpLicenseAgreement (both share this value)
    T3_Windows10Panel,  // Windows10Panel (login + desktop root)
    T3_SettingPanel,    // SettingPanel (Windows10Desktop > WindowsContent > SettingPanel)
    T3_MicrosoftEdgePanel, // MicrosoftEdgePanel (Windows10Desktop > WindowsContent > MicrosoftEdgePanel)

    // ── COC II ───────────────────────────────────────────────────────────
    // Pushed by INGContextProvider on their respective panel GameObjects.
    COCII_UTPCableDetailView,   // UTP Cable detail view panel

    // ── COC III ──────────────────────────────────────────────────────────

    // ── COC IV ───────────────────────────────────────────────────────────
}

[Serializable]
public class GuideContextEntry
{
    public GuideContext context;
    public string title;
    [TextArea(2, 5)] public string description;
}
