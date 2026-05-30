/*
 * ================================================================
 *  UNITY SETUP GUIDE — T2MonitorNavigator
 * ================================================================
 *  STEP 1 — Component placement
 *    Add this to the same T2 Monitor GameObject as T2MonitorController.
 *
 *  STEP 2 — Panel hierarchy (inside the monitor detail Canvas)
 *
 *    Detail root (Canvas content)
 *      Desktop          — index 0, shown first on open
 *        ├─ [Browser icon button   → calls GoTo(1)]
 *        ├─ [Rufus app button      → calls OpenRufus()  — starts greyed out]
 *        └─ Rufus Set Up           — DIRECT child of Desktop, starts INACTIVE
 *             └─ [Rufus UI, close button → calls CloseRufus()]
 *      Browser          — index 1
 *        ├─ [Search bar  (InputField)]
 *        └─ [Search button → calls GoTo(2) to go to Rufus download page]
 *      Rufus Download   — index 2
 *        ├─ [Rufus download button → calls DownloadRufus()]
 *        └─ [Back button → calls GoTo(0)]
 *
 *    Only Desktop starts active. Deactivate Browser and Rufus Download.
 *    Rufus Set Up also starts inactive.
 *
 *    IMPORTANT:
 *      - Rufus Set Up is NOT in the panels array. It is toggled
 *        independently by OpenRufus() / CloseRufus().
 *      - The "Rufus App" button GameObject must be ACTIVE in the scene.
 *        The script greys it out (interactable=false) until Rufus is
 *        downloaded — do NOT leave the GameObject itself inactive, or
 *        it can never be clicked.
 *
 *  STEP 3 — Wire the inspector
 *    T2MonitorNavigator:
 *      panels[0]       → Desktop
 *      panels[1]       → Browser
 *      panels[2]       → Rufus Download
 *      browserPanel    → Browser            (same object as panels[1];
 *                                            used for the "open browser" task)
 *      rufusSetupPanel → Rufus Set Up       (direct child of Desktop)
 *      rufusAppButton  → the Rufus app Button on Desktop
 *
 *  STEP 4 — Wire buttons
 *    Browser icon on Desktop      → GoTo(1)
 *    Search button in Browser     → GoTo(2)
 *    Back button on Rufus Download → GoTo(0)
 *    Download button on Download  → DownloadRufus()
 *    Rufus app button on Desktop  → OpenRufus()
 *    Close/X button in Rufus Set Up → CloseRufus()
 *
 *  STEP 5 — Mark Rufus complete (Task 4 — tentative)
 *    The final confirm/flash button inside Rufus Set Up:
 *      Function: MarkRufusComplete()
 *    NOTE: Task 4's condition is not wired yet (tentative). Calling this
 *    sets IsRufusComplete; hook it into Task 4 later when the step is final.
 *
 *  TASK CONDITIONS (read by T2TaskListManager):
 *    Task 1 (open browser)   → BrowserOpened
 *    Task 2 (download rufus) → IsRufusDownloaded
 *    Task 3 (open rufus)     → RufusOpened
 *    Task 4 (finish setup)   → tentative, not active yet
 * ================================================================
 */

using UnityEngine;
using UnityEngine.UI;

public class T2MonitorNavigator : MonoBehaviour
{
    [Tooltip("Top-level navigation panels (Desktop=0, Browser=1, Download=2). Do NOT include Rufus Set Up here.")]
    [SerializeField] private GameObject[] panels;

    [Header("Task Tracking")]
    [Tooltip("The Browser panel — opening it completes the 'open browser' task. Usually the same object as panels[1].")]
    [SerializeField] private GameObject browserPanel;

    [Header("Rufus App")]
    [SerializeField] private GameObject rufusSetupPanel;
    [SerializeField] private Button rufusAppButton;

    // Milestone flags — latch true once reached (tasks don't revert).
    public bool BrowserOpened { get; private set; }
    public bool IsRufusDownloaded { get; private set; }
    public bool RufusOpened { get; private set; }
    public bool IsRufusComplete { get; private set; }

    public void ResetToDefault()
    {
        BrowserOpened = false;
        IsRufusDownloaded = false;
        RufusOpened = false;
        IsRufusComplete = false;

        if (rufusSetupPanel != null)
            rufusSetupPanel.SetActive(false);

        if (rufusAppButton != null)
            rufusAppButton.interactable = false;

        GoTo(0);
    }

    public void GoTo(int panelIndex)
    {
        if (panels == null || panelIndex < 0 || panelIndex >= panels.Length) return;

        foreach (var panel in panels)
            if (panel != null) panel.SetActive(false);

        GameObject shown = panels[panelIndex];
        shown.SetActive(true);

        if (browserPanel != null && shown == browserPanel)
            BrowserOpened = true;

        T2TaskListManager.CheckConditions();
        Debug.Log($"[T2MonitorNavigator] Navigated to panel {panelIndex}: {shown.name}");
    }

    // Called by the Download button on the Rufus Download panel.
    public void DownloadRufus()
    {
        if (IsRufusDownloaded) return;

        IsRufusDownloaded = true;

        if (rufusAppButton != null)
            rufusAppButton.interactable = true;

        GoTo(0);
        T2TaskListManager.CheckConditions();

        Debug.Log("[T2MonitorNavigator] Rufus downloaded — app button enabled on Desktop.");
    }

    // Called by the Rufus app button on the Desktop panel.
    public void OpenRufus()
    {
        if (!IsRufusDownloaded || rufusSetupPanel == null) return;

        rufusSetupPanel.SetActive(true);
        RufusOpened = true;

        T2TaskListManager.CheckConditions();
        Debug.Log("[T2MonitorNavigator] Rufus setup opened.");
    }

    // Called by the close/X button inside the Rufus Set Up panel.
    public void CloseRufus()
    {
        if (rufusSetupPanel != null)
            rufusSetupPanel.SetActive(false);
        Debug.Log("[T2MonitorNavigator] Rufus setup closed.");
    }

    // Called by the final flash/confirm button inside Rufus Set Up (Task 4 — tentative).
    public void MarkRufusComplete()
    {
        IsRufusComplete = true;
        T2TaskListManager.CheckConditions();
        Debug.Log("[T2MonitorNavigator] Rufus setup marked complete.");
    }
}
