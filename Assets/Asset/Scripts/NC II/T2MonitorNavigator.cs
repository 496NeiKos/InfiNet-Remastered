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
 *      Desktop                  — index 0, shown first on open
 *        ├─ BrowserApp button       → GoTo(1)
 *        ├─ RufusApp button         → OpenRufus()   (starts greyed out)
 *        └─ Rufus Set Up            — DIRECT child of Desktop, starts INACTIVE
 *             └─ close button       → CloseRufus()
 *      Browser                  — index 1
 *        ├─ Search InputField (TMP) → On Submit → SubmitSearch()
 *        ├─ (optional) "No results" object → assign to searchNoResults
 *        └─ Back button             → GoTo(0)
 *      Rufus Download           — index 2
 *        ├─ Download link button    → DownloadRufus()
 *        └─ Back button             → GoTo(0)
 *      BrowserSearchedRufus     — index 3  ← NEW
 *        ├─ Back button             → GoTo(0)   (returns to Desktop)
 *        └─ Link button             → GoTo(2)   (opens Rufus Download)
 *
 *    Only Desktop starts active. Deactivate Browser, Rufus Download,
 *    and BrowserSearchedRufus. Rufus Set Up also starts inactive.
 *
 *    NAMING TIP: the BrowserApp button on the Desktop and the Browser
 *    panel are different objects — give them distinct names (e.g.
 *    "BrowserIcon" vs "Browser") so wiring isn't mixed up.
 *
 *    IMPORTANT:
 *      - Rufus Set Up is NOT in the panels array. It is toggled
 *        independently by OpenRufus() / CloseRufus().
 *      - The RufusApp button GameObject must be ACTIVE in the scene.
 *        The script greys it out (interactable=false) until Rufus is
 *        downloaded — do NOT leave the GameObject itself inactive, or
 *        it can never be clicked.
 *
 *  STEP 3 — Wire the inspector
 *    T2MonitorNavigator:
 *      panels[0]              → Desktop
 *      panels[1]              → Browser
 *      panels[2]              → Rufus Download
 *      panels[3]              → BrowserSearchedRufus       ← NEW
 *      browserPanel           → Browser            (same object as panels[1])
 *      searchField            → Search InputField (TMP) on the Browser panel
 *      searchKeyword          → "rufus" (default; case-insensitive, trimmed)
 *      downloadPanelIndex     → 2
 *      searchResultsPanelIndex→ 3                          ← NEW
 *      searchNoResults        → optional "no results" object (leave empty to skip)
 *      rufusSetupPanel        → Rufus Set Up       (direct child of Desktop)
 *      rufusAppButton         → the RufusApp Button on Desktop
 *
 *  STEP 4 — Wire buttons / events
 *    BrowserApp button on Desktop          → GoTo(1)
 *    Search InputField (On Submit)         → SubmitSearch()   ← fires on Enter
 *    Back button on Browser                → GoTo(0)
 *    Back button on BrowserSearchedRufus   → GoTo(0)          ← NEW
 *    Link button on BrowserSearchedRufus   → GoTo(2)          ← NEW
 *    Download button on Rufus Download     → DownloadRufus()
 *    Back button on Rufus Download         → GoTo(0)
 *    RufusApp button on Desktop            → OpenRufus()
 *    Close/X button in Rufus Set Up        → CloseRufus()
 *
 *    NOTE on the search: SubmitSearch() reads the InputField directly, so
 *    it works wired to the InputField's "On Submit (String)" (Enter key)
 *    OR to a search Button. If you use Enter, the separate search button
 *    can be deleted. Prefer "On Submit" over "On End Edit" — On End Edit
 *    also fires when the field loses focus.
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

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class T2MonitorNavigator : MonoBehaviour
{
    [Tooltip("Top-level navigation panels (Desktop=0, Browser=1, Download=2). Do NOT include Rufus Set Up here.")]
    [SerializeField] private GameObject[] panels;

    [Header("Task Tracking")]
    [Tooltip("The Browser panel — opening it completes the 'open browser' task. Usually the same object as panels[1].")]
    [SerializeField] private GameObject browserPanel;

    [Header("Browser Search")]
    [Tooltip("The TMP InputField on the Browser panel.")]
    [SerializeField] private TMP_InputField searchField;
    [Tooltip("Query that must be typed to reach the download page (case-insensitive, trimmed).")]
    [SerializeField] private string searchKeyword = "rufus";
    [Tooltip("Index in 'panels' of the Rufus Download page.")]
    [SerializeField] private int downloadPanelIndex = 2;
    [Tooltip("Index in 'panels' of the BrowserSearchedRufus results page (shown after a successful search, before download).")]
    [SerializeField] private int searchResultsPanelIndex = 3;
    [Tooltip("Optional object shown when the search query does not match (e.g. a 'No results' label). Leave empty to skip.")]
    [SerializeField] private GameObject searchNoResults;

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

        if (searchField != null)
            searchField.text = string.Empty;

        if (searchNoResults != null)
            searchNoResults.SetActive(false);

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

    // Called by the Search InputField's On Submit (Enter key) — also works
    // wired to a search Button. Navigates to the download page on a match.
    public void SubmitSearch()
    {
        if (searchField == null) return;

        string query = searchField.text != null ? searchField.text.Trim() : string.Empty;
        bool match = string.Equals(query, searchKeyword, StringComparison.OrdinalIgnoreCase);

        if (match)
        {
            if (searchNoResults != null) searchNoResults.SetActive(false);
            GoTo(searchResultsPanelIndex);
        }
        else
        {
            if (searchNoResults != null) searchNoResults.SetActive(true);
            Debug.Log($"[T2MonitorNavigator] Search '{query}' did not match '{searchKeyword}'.");
        }
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

    // Called by the RufusApp button on the Desktop panel.
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
