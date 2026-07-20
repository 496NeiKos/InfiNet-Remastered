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
using UnityEngine.InputSystem;

public class UEFINavigator : MonoBehaviour
{
    [Tooltip("Tab content panels in order: Main=0, Advanced=1, Boot=2, Security=3, Exit=4.")]
    [SerializeField] private GameObject[] tabPanels;

    [Header("Save Confirmation")]
    [Tooltip("Child popup shown when the player presses F10. Assign UEFISaveConfirmationPopup child of UEFIPanel.")]
    [SerializeField] private UEFISaveConfirmationPopup saveConfirmPopup;

    [Header("References")]
    [Tooltip("T3MonitorController on the UEFI Monitor root — used to close the canvas from the Exit tab.")]
    [SerializeField] private T3MonitorController monitorController;

    // Milestone flags — latch true once reached (tasks don't revert).
    public bool UEFIOpened          { get; private set; }
    public bool BootTabVisited      { get; private set; }
    public bool BootOrderConfigured { get; private set; }
    public bool SavedAndExited      { get; private set; }

    // Set true when the player presses F10 → Save Changes.
    public bool BootStateSaved { get; private set; }

    // Injected by T3MonitorController.Start() — used to gate the F10 listener.
    private GameObject _uefiPanel;

    public void SetUEFIPanel(GameObject panel) => _uefiPanel = panel;

    private void Update()
    {
        // Only listen for F10 while the UEFI panel itself is visible.
        if (_uefiPanel == null || !_uefiPanel.activeSelf) return;
        if (Keyboard.current == null || !Keyboard.current.f10Key.wasPressedThisFrame) return;

        // Don't open the save popup while an option selection popup is already open.
        if (UEFIOptionPopup.Instance != null && UEFIOptionPopup.Instance.gameObject.activeSelf) return;

        // Don't open the save popup again while it is already showing.
        if (saveConfirmPopup != null && saveConfirmPopup.IsVisible) return;

        if (saveConfirmPopup == null)
        {
            Debug.LogWarning("[UEFINavigator] F10 pressed but saveConfirmPopup is not assigned.");
            return;
        }

        saveConfirmPopup.Show();
        Debug.Log("[UEFINavigator] F10 detected — save confirmation opened.");
    }

    // Called by T3MonitorController when the UEFI panel is shown.
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

    // Called by T3MonitorController when the canvas is hidden (ESC or after save).
    public void Close()
    {
        saveConfirmPopup?.gameObject.SetActive(false);
    }

    // Called by UEFISaveConfirmationPopup.SaveChanges().
    public void CommitSave()
    {
        BootStateSaved = true;
        T3TaskListManager.CheckConditions();
        Debug.Log("[UEFINavigator] Boot state saved via F10.");
    }

    // Hard reset — only call this when resetting the whole topic from scratch.
    public void ResetToDefault()
    {
        UEFIOpened          = false;
        BootTabVisited      = false;
        BootOrderConfigured = false;
        SavedAndExited      = false;
        BootStateSaved      = false;

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

        // Close the canvas so the player isn't left hanging in the UEFI after clicking this.
        monitorController?.OnBootStateSaved();
    }
}
