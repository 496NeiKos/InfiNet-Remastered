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
 *        ├─ Download link button    → OpenRufusPopUp()   ← opens popup first
 *        └─ Back button             → GoTo(0)
 *      BrowserSearchedRufus     — index 3
 *        ├─ Back button             → GoTo(0)   (returns to Desktop)
 *        └─ Link button             → GoTo(2)   (opens Rufus Download)
 *      ISODownloadPanel         — index 4
 *        ├─ Download link button    → OpenISOPopUp()     ← opens popup first
 *        └─ Back button             → GoTo(0)
 *      BrowserSearchedISO       — index 5
 *        ├─ Back button             → GoTo(0)   (returns to Desktop)
 *        └─ Link button             → GoTo(4)   (opens ISO Download)
 *
 *    Pop-ups (NOT in panels array — toggled independently, start INACTIVE):
 *      RufusPopUp  — child of RufusDownloadPanel (or canvas root)
 *        ├─ Confirm button  → DownloadRufus()
 *        │     (closes popup → shows progress bar → IsRufusDownloaded latches on complete)
 *        └─ Cancel button   → CloseRufusPopUp()   (optional)
 *      IsoPopUp    — child of ISODownloadPanel (or canvas root)
 *        ├─ Confirm button  → DownloadISO()
 *        │     (closes popup → shows progress bar → IsIsoDownloaded latches on complete)
 *        └─ Cancel button   → CloseISOPopUp()      (optional)
 *
 *    Download Progress (NOT in panels array — overlay, starts INACTIVE):
 *      DownloadProgressContainer — direct child of the Canvas content root (or Desktop)
 *        ├─ ProgressBar    (UI Slider, Min=0, Max=1, Interactable OFF)
 *        └─ ProgressLabel  (TMP_Text, optional — shows "Downloading... 45%")
 *
 *    Only Desktop starts active. Deactivate all others.
 *    Rufus Set Up also starts inactive.
 *
 *  STEP 3 — Wire the inspector
 *    T2MonitorNavigator:
 *      panels[0]                   → Desktop
 *      panels[1]                   → Browser
 *      panels[2]                   → Rufus Download
 *      panels[3]                   → BrowserSearchedRufus
 *      panels[4]                   → ISODownloadPanel
 *      panels[5]                   → BrowserSearchedISO
 *      browserPanel                → Browser            (same object as panels[1])
 *      searchField                 → Search InputField (TMP) on the Browser panel
 *      searchKeyword               → "rufus"            (case-insensitive, trimmed)
 *      isoSearchKeyword            → "iso windows 10"  (case-insensitive, trimmed)
 *      downloadPanelIndex          → 2
 *      searchResultsPanelIndex     → 3
 *      isoDownloadPanelIndex       → 4
 *      isoSearchResultsPanelIndex  → 5
 *      searchNoResults             → optional "no results" object (leave empty to skip)
 *      rufusSetupPanel             → Rufus Set Up       (direct child of Desktop)
 *      rufusAppButton              → the RufusApp Button on Desktop
 *      rufusPopUp                  → RufusPopUp panel
 *      isoPopUp                    → IsoPopUp panel
 *      downloadProgressContainer   → DownloadProgressContainer (starts INACTIVE)
 *      downloadProgressBar         → ProgressBar Slider inside the container
 *      progressLabel               → ProgressLabel TMP_Text inside the container (optional)
 *      downloadDuration            → seconds to fill the bar (default 5)
 *
 *  STEP 4 — Wire buttons / events
 *    BrowserApp button on Desktop          → GoTo(1)
 *    Search InputField (On Submit)         → SubmitSearch()   ← fires on Enter
 *    Back button on Browser                → GoTo(0)
 *    Back button on BrowserSearchedRufus   → GoTo(0)
 *    Link button on BrowserSearchedRufus   → GoTo(2)
 *    Download link on Rufus Download       → OpenRufusPopUp()
 *    Back button on Rufus Download         → GoTo(0)
 *    RufusPopUp confirm button             → DownloadRufus()
 *    RufusPopUp cancel button              → CloseRufusPopUp()
 *    Download link on ISODownloadPanel     → OpenISOPopUp()
 *    Back button on ISODownloadPanel       → GoTo(0)
 *    IsoPopUp confirm button               → DownloadISO()
 *    IsoPopUp cancel button                → CloseISOPopUp()
 *    Back button on BrowserSearchedISO     → GoTo(0)
 *    Link button on BrowserSearchedISO     → GoTo(4)
 *    RufusApp button on Desktop            → OpenRufus()
 *    Close/X button in Rufus Set Up        → CloseRufus()
 *
 *  DOWNLOAD FLOW
 *    Both Rufus and ISO use the shared DownloadProgressContainer.
 *    Confirm button → closes popup → bar fills over downloadDuration seconds →
 *      bar hides → flag (IsRufusDownloaded / IsIsoDownloaded) latches →
 *      T2TaskListManager task 3 can complete once BOTH are true.
 *
 *  TASK CONDITIONS (read by T2TaskListManager):
 *    Task 2  (canvas opened)        → MonitorCanvasOpened    (set on first Open() call)
 *    Task 3  (download rufus + ISO) → IsRufusDownloaded && IsIsoDownloaded
 *    Task 4  (open rufus)           → RufusOpened            (RufusApp button → OpenRufus())
 *    Task 12 (configure rufus)      → IsRufusComplete        (via RufusSetupManager)
 * ================================================================
 */

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class T2MonitorNavigator : MonoBehaviour
{
    [Tooltip("Top-level navigation panels: Desktop=0, Browser=1, RufusDownload=2, BrowserSearchedRufus=3, ISODownload=4, BrowserSearchedISO=5. Do NOT include Rufus Set Up here.")]
    [SerializeField] private GameObject[] panels;

    [Header("Task Tracking")]
    [Tooltip("The Browser panel — opening it completes the 'open browser' task. Usually the same object as panels[1].")]
    [SerializeField] private GameObject browserPanel;

    [Header("Browser Search")]
    [Tooltip("The TMP InputField on the Browser panel.")]
    [SerializeField] private TMP_InputField searchField;
    [Tooltip("Query that must be typed to reach the Rufus download page (case-insensitive, trimmed).")]
    [SerializeField] private string searchKeyword = "rufus";
    [Tooltip("Index in 'panels' of the Rufus Download page.")]
    [SerializeField] private int downloadPanelIndex = 2;
    [Tooltip("Index in 'panels' of the BrowserSearchedRufus results page.")]
    [SerializeField] private int searchResultsPanelIndex = 3;
    [Tooltip("Keyword that navigates to the ISO Download panel (case-insensitive, trimmed).")]
    [SerializeField] private string isoSearchKeyword = "iso windows 10";
    [Tooltip("Index in 'panels' of the ISO Download page.")]
    [SerializeField] private int isoDownloadPanelIndex = 4;
    [Tooltip("Index in 'panels' of the BrowserSearchedISO results page.")]
    [SerializeField] private int isoSearchResultsPanelIndex = 5;
    [Tooltip("Optional object shown when the search query does not match. Leave empty to skip.")]
    [SerializeField] private GameObject searchNoResults;

    [Header("Rufus App")]
    [SerializeField] private GameObject rufusSetupPanel;
    [SerializeField] private Button rufusAppButton;

    [Header("Download Pop-ups")]
    [Tooltip("Popup shown when the Rufus download link is clicked. Starts inactive.")]
    [SerializeField] private GameObject rufusPopUp;
    [Tooltip("Popup shown when the ISO download link is clicked. Starts inactive.")]
    [SerializeField] private GameObject isoPopUp;

    [Header("Download Progress")]
    [Tooltip("Overlay container shown during download. Starts inactive.")]
    [SerializeField] private GameObject downloadProgressContainer;
    [Tooltip("UI Slider (Min=0 Max=1, Interactable OFF) that fills during download.")]
    [SerializeField] private Slider downloadProgressBar;
    [Tooltip("Optional TMP_Text that shows 'Downloading... X%'.")]
    [SerializeField] private TMP_Text progressLabel;
    [Tooltip("Seconds the progress bar takes to fill (default 5).")]
    [SerializeField] [Range(1f, 30f)] private float downloadDuration = 5f;

    // Milestone flags — latch true once reached (tasks don't revert).
    public bool MonitorCanvasOpened { get; private set; }
    public bool BrowserOpened       { get; private set; }
    public bool IsRufusDownloaded   { get; private set; }
    public bool IsIsoDownloaded     { get; private set; }
    public bool RufusOpened         { get; private set; }
    public bool IsRufusComplete     { get; private set; }

    private enum DownloadType { Rufus, ISO }
    private Coroutine _downloadCoroutine;
    private bool _downloadRunning;

    private void Start()
    {
        SetRufusAppState(false);
    }

    // Shows or hides the Rufus app icon.
    private void SetRufusAppState(bool visible)
    {
        if (rufusAppButton != null)
            rufusAppButton.gameObject.SetActive(visible);
    }

    // Called when the monitor detail canvas opens (via T2MonitorController.ShowDetailAtCenter).
    // Latches MonitorCanvasOpened, resets transient UI, preserves milestone flags.
    public void Open()
    {
        MonitorCanvasOpened = true;
        OpenPanels();
        T2TaskListManager.CheckConditions();
    }

    // Internal panel reset shared by Open() and ResetToDefault().
    private void OpenPanels()
    {
        if (rufusSetupPanel != null) rufusSetupPanel.SetActive(false);
        if (searchField != null)     searchField.text = string.Empty;
        if (searchNoResults != null) searchNoResults.SetActive(false);
        if (rufusPopUp != null)      rufusPopUp.SetActive(false);
        if (isoPopUp != null)        isoPopUp.SetActive(false);

        // Preserve the progress container if a download is still in flight.
        if (!_downloadRunning && downloadProgressContainer != null)
            downloadProgressContainer.SetActive(false);

        SetRufusAppState(IsRufusDownloaded);
        GoTo(0);
    }

    // Hard reset — only call when resetting the whole topic from scratch.
    public void ResetToDefault()
    {
        MonitorCanvasOpened = false;
        BrowserOpened       = false;
        IsRufusDownloaded   = false;
        IsIsoDownloaded     = false;
        RufusOpened         = false;
        IsRufusComplete     = false;

        if (_downloadCoroutine != null)
        {
            StopCoroutine(_downloadCoroutine);
            _downloadCoroutine = null;
            _downloadRunning   = false;
        }

        OpenPanels();
    }

    public void GoTo(int panelIndex)
    {
        if (panels == null || panelIndex < 0 || panelIndex >= panels.Length) return;

        foreach (var panel in panels)
            if (panel != null) panel.SetActive(false);

        GameObject shown = panels[panelIndex];
        shown.SetActive(true);

        bool isBrowserPanel = browserPanel != null ? (shown == browserPanel) : (panelIndex == 1);
        if (isBrowserPanel) BrowserOpened = true;

        T2TaskListManager.CheckConditions();
        Debug.Log($"[T2MonitorNavigator] Navigated to panel {panelIndex}: {shown.name}");
    }

    // Called by the Search InputField's On Submit (Enter key).
    public void SubmitSearch()
    {
        if (searchField == null) return;

        string query = searchField.text != null ? searchField.text.Trim() : string.Empty;

        if (string.Equals(query, searchKeyword, System.StringComparison.OrdinalIgnoreCase))
        {
            if (searchNoResults != null) searchNoResults.SetActive(false);
            GoTo(searchResultsPanelIndex);
        }
        else if (string.Equals(query, isoSearchKeyword, System.StringComparison.OrdinalIgnoreCase))
        {
            if (searchNoResults != null) searchNoResults.SetActive(false);
            GoTo(isoSearchResultsPanelIndex);
        }
        else
        {
            if (searchNoResults != null) searchNoResults.SetActive(true);
            Debug.Log($"[T2MonitorNavigator] Search '{query}' did not match any keyword.");
        }
    }

    // ---- Rufus download popup ----

    public void OpenRufusPopUp()
    {
        if (rufusPopUp != null)
            rufusPopUp.SetActive(true);
    }

    public void CloseRufusPopUp()
    {
        if (rufusPopUp != null)
            rufusPopUp.SetActive(false);
    }

    // Wired to the confirm button inside RufusPopUp.
    // Closes the popup and starts the download progress bar.
    // IsRufusDownloaded latches only after the bar finishes.
    public void DownloadRufus()
    {
        if (IsRufusDownloaded) return;

        CloseRufusPopUp();

        if (_downloadCoroutine != null) StopCoroutine(_downloadCoroutine);
        _downloadCoroutine = StartCoroutine(RunDownload(DownloadType.Rufus));

        Debug.Log("[T2MonitorNavigator] Rufus download started.");
    }

    // ---- ISO download popup ----

    public void OpenISOPopUp()
    {
        if (isoPopUp != null)
            isoPopUp.SetActive(true);
    }

    public void CloseISOPopUp()
    {
        if (isoPopUp != null)
            isoPopUp.SetActive(false);
    }

    // Wired to the confirm button inside IsoPopUp.
    // Closes the popup and starts the download progress bar.
    // IsIsoDownloaded latches only after the bar finishes.
    public void DownloadISO()
    {
        if (IsIsoDownloaded) return;

        CloseISOPopUp();

        if (_downloadCoroutine != null) StopCoroutine(_downloadCoroutine);
        _downloadCoroutine = StartCoroutine(RunDownload(DownloadType.ISO));

        Debug.Log("[T2MonitorNavigator] ISO download started.");
    }

    // ---- Download progress coroutine ----

    private IEnumerator RunDownload(DownloadType type)
    {
        _downloadRunning = true;

        if (downloadProgressContainer != null) downloadProgressContainer.SetActive(true);
        if (downloadProgressBar != null)       downloadProgressBar.value = 0f;

        SetProgressLabel("Downloading... 0%");

        float duration = Mathf.Max(0.1f, downloadDuration);
        float elapsed  = 0f;
        int   lastPct  = -1;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            int pct = Mathf.Clamp(Mathf.FloorToInt((elapsed / duration) * 100f), 0, 99);

            if (pct != lastPct)
            {
                lastPct = pct;
                if (downloadProgressBar != null)
                    downloadProgressBar.value = elapsed / duration;
                SetProgressLabel($"Downloading... {pct}%");
            }

            yield return null;
        }

        if (downloadProgressBar != null) downloadProgressBar.value = 1f;
        SetProgressLabel("Download complete!");

        _downloadRunning   = false;
        _downloadCoroutine = null;

        // Latch the milestone flag after progress finishes.
        if (type == DownloadType.Rufus)
        {
            IsRufusDownloaded = true;
            SetRufusAppState(true);
            Debug.Log("[T2MonitorNavigator] Rufus downloaded — app button enabled.");
        }
        else
        {
            IsIsoDownloaded = true;
            Debug.Log("[T2MonitorNavigator] ISO downloaded.");
        }

        T2TaskListManager.CheckConditions();

        yield return new WaitForSeconds(1.5f);
        if (downloadProgressContainer != null) downloadProgressContainer.SetActive(false);
    }

    private void SetProgressLabel(string text)
    {
        if (progressLabel != null)
            progressLabel.text = text;
    }

    // ---- Rufus app ----

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

    // Called by RufusSetupManager after formatting completes successfully.
    public void MarkRufusComplete()
    {
        IsRufusComplete = true;
        T2TaskListManager.CheckConditions();
        Debug.Log("[T2MonitorNavigator] Rufus setup marked complete.");
    }
}
