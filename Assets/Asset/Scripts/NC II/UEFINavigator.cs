/*
 * ================================================================
 *  UNITY SETUP GUIDE — UEFINavigator
 * ================================================================
 *  STEP 1 — Component placement
 *    Add this script to the UEFIMonitor root GameObject (same object
 *    as T3MonitorController and T3MonitorInteraction).
 *
 *  STEP 2 — Panel hierarchy (inside UEFICanvas)
 *
 *    UEFICanvas (Canvas, Screen Space - Camera, starts INACTIVE)
 *      ├─ Navbar                      — always visible while canvas is open
 *      │    ├─ Tab_Main     (Button)  → UEFINavigator.GoToTab(0)
 *      │    ├─ Tab_Advanced (Button)  → UEFINavigator.GoToTab(1)
 *      │    ├─ Tab_Boot     (Button)  → UEFINavigator.GoToTab(2)
 *      │    ├─ Tab_Security (Button)  → UEFINavigator.GoToTab(3)
 *      │    └─ Tab_Exit     (Button)  → UEFINavigator.GoToTab(4)
 *      └─ TabContent
 *           ├─ Panel_Main     (index 0, active by default on open)
 *           ├─ Panel_Advanced (index 1)
 *           ├─ Panel_Boot     (index 2)
 *           ├─ Panel_Security (index 3)
 *           └─ Panel_Exit     (index 4)
 *               └─ SaveExitButton → UEFINavigator.SaveAndExit()
 *
 *    Only Panel_Main starts active inside TabContent.
 *    All others start inactive.
 *
 *  STEP 3 — Wire the inspector
 *    UEFINavigator:
 *      tabPanels[0] → Panel_Main
 *      tabPanels[1] → Panel_Advanced
 *      tabPanels[2] → Panel_Boot
 *      tabPanels[3] → Panel_Security
 *      tabPanels[4] → Panel_Exit
 *
 *  STEP 4 — Wire buttons
 *    Tab_Main     OnClick → GoToTab(0)
 *    Tab_Advanced OnClick → GoToTab(1)
 *    Tab_Boot     OnClick → GoToTab(2)
 *    Tab_Security OnClick → GoToTab(3)
 *    Tab_Exit     OnClick → GoToTab(4)
 *    SaveExitButton OnClick → SaveAndExit()
 *
 *  MILESTONE FLAGS (read by T3TaskListManager):
 *    UEFIOpened          — latches true on the first Open() call
 *    BootTabVisited      — latches true when GoToTab(2) is called
 *    BootOrderConfigured — set via SetBootOrderConfigured() from a button
 *                          inside Panel_Boot (wire when content is built)
 *    SavedAndExited      — set via SaveAndExit() on the Exit panel
 * ================================================================
 */

using UnityEngine;

public class UEFINavigator : MonoBehaviour
{
    [Tooltip("Tab content panels in order: Main=0, Advanced=1, Boot=2, Security=3, Exit=4.")]
    [SerializeField] private GameObject[] tabPanels;

    // Milestone flags — latch true once reached (tasks don't revert).
    public bool UEFIOpened          { get; private set; }
    public bool BootTabVisited      { get; private set; }
    public bool BootOrderConfigured { get; private set; }
    public bool SavedAndExited      { get; private set; }

    // Called by T3MonitorController when the canvas is shown.
    public void Open()
    {
        GoToTab(0);

        if (!UEFIOpened)
        {
            UEFIOpened = true;
            T3TaskListManager.CheckConditions();
            Debug.Log("[UEFINavigator] UEFI opened for the first time.");
        }
    }

    // Hard reset — only call this when resetting the whole topic from scratch.
    public void ResetToDefault()
    {
        UEFIOpened          = false;
        BootTabVisited      = false;
        BootOrderConfigured = false;
        SavedAndExited      = false;

        GoToTab(0);
    }

    // Called by each tab button: OnClick → GoToTab(n)
    public void GoToTab(int index)
    {
        if (tabPanels == null || index < 0 || index >= tabPanels.Length) return;

        foreach (var panel in tabPanels)
            if (panel != null) panel.SetActive(false);

        tabPanels[index].SetActive(true);

        if (index == 2 && !BootTabVisited)
        {
            BootTabVisited = true;
            T3TaskListManager.CheckConditions();
        }

        Debug.Log($"[UEFINavigator] Navigated to tab {index}: {tabPanels[index].name}");
    }

    // Wire to a button inside Panel_Boot once the boot order content is built.
    public void SetBootOrderConfigured()
    {
        if (BootOrderConfigured) return;
        BootOrderConfigured = true;
        T3TaskListManager.CheckConditions();
        Debug.Log("[UEFINavigator] Boot order configured.");
    }

    // Wire to the Save & Exit button inside Panel_Exit.
    public void SaveAndExit()
    {
        if (SavedAndExited) return;
        SavedAndExited = true;
        T3TaskListManager.CheckConditions();
        Debug.Log("[UEFINavigator] Saved and exited UEFI.");
    }
}
