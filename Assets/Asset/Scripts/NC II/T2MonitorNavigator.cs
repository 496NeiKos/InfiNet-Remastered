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
 *      Desktop                       — panels[0], shown first on open
 *        ├─ BrowserApp button             → GoTo(1)
 *        ├─ RufusApp button               → OpenRufus()   (starts greyed out)
 *        └─ Rufus Set Up                  — DIRECT child of Desktop, starts INACTIVE
 *             └─ close button             → CloseRufus()
 *      BrowserPanel                  — panels[1], start INACTIVE
 *        ├─ BrowserSearchBar         → persistent search bar (ALWAYS ACTIVE inside BrowserPanel)
 *        │    ├─ TMP_InputField      → searchField
 *        │    └─ SearchButton        → OnClick: SubmitSearch()
 *        ├─ BrowserHome              → browserHome (start ACTIVE — default browser content)
 *        │    └─ (optional) No Results object → searchNoResults
 *        ├─ BrowserSearchedRufusPanel → browserSearchedRufusPanel (start INACTIVE)
 *        │    ├─ Back button              → ShowBrowserHome()
 *        │    ├─ Link button              → ShowRufusDownload()
 *        │    └─ RufusDownloadPanel  → rufusDownloadPanel (CHILD, start INACTIVE)
 *        │         ├─ Download link       → OpenRufusPopUp()
 *        │         └─ Back button         → HideRufusDownload()
 *        └─ BrowserSearchedISOPanel  → browserSearchedISOPanel (start INACTIVE)
 *             ├─ Back button              → ShowBrowserHome()
 *             ├─ Link button              → ShowISODownload()
 *             └─ ISODownloadPanel    → isoDownloadPanel (CHILD, start INACTIVE)
 *                  ├─ Download link       → OpenISOPopUp()
 *                  └─ Back button         → HideISODownload()
 *
 *    Pop-ups (NOT in panels array — toggled independently, start INACTIVE):
 *      RufusPopUp  — child of RufusDownloadPanel (or canvas root)
 *        ├─ Confirm button  → DownloadRufus()
 *        └─ Cancel button   → CloseRufusPopUp()   (optional)
 *      IsoPopUp    — child of ISODownloadPanel (or canvas root)
 *        ├─ Confirm button  → DownloadISO()
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
 *      panels[0]                    → Desktop
 *      panels[1]                    → BrowserPanel
 *      browserPanel                 → BrowserPanel              (same object as panels[1])
 *      searchField                  → TMP_InputField inside BrowserSearchBar
 *      searchNoResults              → optional "no results" object inside BrowserHome
 *      browserHome                  → BrowserHome               (child of BrowserPanel, start ACTIVE)
 *      browserSearchedRufusPanel    → BrowserSearchedRufusPanel (child of BrowserPanel, start INACTIVE)
 *      rufusDownloadPanel           → RufusDownloadPanel        (child of BrowserSearchedRufusPanel, start INACTIVE)
 *      browserSearchedISOPanel      → BrowserSearchedISOPanel   (child of BrowserPanel, start INACTIVE)
 *      isoDownloadPanel             → ISODownloadPanel          (child of BrowserSearchedISOPanel, start INACTIVE)
 *      rufusSetupPanel              → Rufus Set Up              (direct child of Desktop, start INACTIVE)
 *      rufusAppButton               → the RufusApp Button on Desktop
 *      rufusPopUp                   → RufusPopUp panel
 *      isoPopUp                     → IsoPopUp panel
 *      downloadProgressContainer    → DownloadProgressContainer (starts INACTIVE)
 *      downloadProgressBar          → ProgressBar Slider inside the container
 *      progressLabel                → ProgressLabel TMP_Text inside the container (optional)
 *      downloadDuration             → seconds to fill the bar (default 5)
 *
 *  STEP 4 — Wire buttons / events
 *    BrowserApp button on Desktop              → GoTo(1)
 *    Search InputField (On Submit / Enter)     → SubmitSearch()
 *    SearchButton in BrowserSearchBar          → SubmitSearch()
 *    Back button in BrowserSearchedRufusPanel  → ShowBrowserHome()
 *    Link button in BrowserSearchedRufusPanel  → ShowRufusDownload()
 *    Download link in RufusDownloadPanel       → OpenRufusPopUp()
 *    Back button in RufusDownloadPanel         → HideRufusDownload()
 *    RufusPopUp confirm button                 → DownloadRufus()
 *    RufusPopUp cancel button                  → CloseRufusPopUp()
 *    Back button in BrowserSearchedISOPanel    → ShowBrowserHome()
 *    Link button in BrowserSearchedISOPanel    → ShowISODownload()
 *    Download link in ISODownloadPanel         → OpenISOPopUp()
 *    Back button in ISODownloadPanel           → HideISODownload()
 *    IsoPopUp confirm button                   → DownloadISO()
 *    IsoPopUp cancel button                    → CloseISOPopUp()
 *    RufusApp button on Desktop                → OpenRufus()
 *    Close/X button in Rufus Set Up            → CloseRufus()
 *
 *  SEARCH LOGIC
 *    The search bar is always visible inside BrowserPanel (persistent across all sub-panels).
 *    Matching is keyword-based, case-insensitive, any word order:
 *      Rufus  : query contains "rufus"
 *               e.g. "rufus", "download rufus", "rufus 4.2"
 *      ISO    : query contains "iso" AND "10" AND ("windows" or "win")
 *               e.g. "iso 10 windows", "windows 10 iso", "10 iso windows",
 *                    "download iso windows 10", "win 10 iso"
 *      No match: searchNoResults shown (if assigned); BrowserHome stays visible
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
    [Tooltip("Top-level navigation panels: Desktop=0, BrowserPanel=1. " +
             "BrowserPanel sub-panels are direct children, not in this array.")]
    [SerializeField] private GameObject[] panels;

    [Header("Task Tracking")]
    [Tooltip("The BrowserPanel — opening it sets BrowserOpened. Same object as panels[1].")]
    [SerializeField] private GameObject browserPanel;

    [Header("Browser Search")]
    [Tooltip("The TMP InputField inside BrowserSearchBar (always active within BrowserPanel).")]
    [SerializeField] private TMP_InputField searchField;
    [Tooltip("Optional object shown when the search query does not match. Assign inside BrowserHome.")]
    [SerializeField] private GameObject searchNoResults;

    [Header("Browser Sub-Panels (children of BrowserPanel)")]
    [Tooltip("Default browser content shown on open and after back. Start ACTIVE.")]
    [SerializeField] private GameObject browserHome;
    [Tooltip("Search result panel for Rufus. Start INACTIVE.")]
    [SerializeField] private GameObject browserSearchedRufusPanel;
    [Tooltip("Download page for Rufus. CHILD of BrowserSearchedRufusPanel. Start INACTIVE.")]
    [SerializeField] private GameObject rufusDownloadPanel;
    [Tooltip("Search result panel for ISO / Windows 10. Start INACTIVE.")]
    [SerializeField] private GameObject browserSearchedISOPanel;
    [Tooltip("Download page for ISO / Windows 10. CHILD of BrowserSearchedISOPanel. Start INACTIVE.")]
    [SerializeField] private GameObject isoDownloadPanel;

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
    [Tooltip("CanvasGroup on the monitor canvas root. Interaction is disabled while the progress container is visible.")]
    [SerializeField] private CanvasGroup canvasInteractionGroup;

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

    private void SetRufusAppState(bool visible)
    {
        if (rufusAppButton != null)
            rufusAppButton.gameObject.SetActive(visible);
    }

    // Called when the monitor detail canvas opens (via T2MonitorController.ShowDetailAtCenter).
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

    // Top-level navigation: 0 = Desktop, 1 = BrowserPanel.
    // When navigating to BrowserPanel, ShowBrowserHome() is called automatically.
    public void GoTo(int panelIndex)
    {
        if (panels == null || panelIndex < 0 || panelIndex >= panels.Length) return;

        foreach (var panel in panels)
            if (panel != null) panel.SetActive(false);

        GameObject shown = panels[panelIndex];
        shown.SetActive(true);

        bool isBrowserPanel = browserPanel != null ? (shown == browserPanel) : (panelIndex == 1);
        if (isBrowserPanel)
        {
            BrowserOpened = true;
            ShowBrowserHome();
        }

        T2TaskListManager.CheckConditions();
        Debug.Log($"[T2MonitorNavigator] Navigated to panel {panelIndex}: {shown.name}");
    }

    // ----------------------------------------------------------------
    //  Browser inner navigation (all stay within BrowserPanel;
    //  search bar remains visible throughout)
    // ----------------------------------------------------------------

    // Shows the default browser home; hides all result and download sub-panels.
    public void ShowBrowserHome()
    {
        browserHome?.SetActive(true);
        if (searchNoResults != null) searchNoResults.SetActive(false);
        browserSearchedRufusPanel?.SetActive(false);
        browserSearchedISOPanel?.SetActive(false);
        // Download panels are children — they deactivate with their parent.
    }

    // Shows the Rufus search result panel; hides home and ISO panel.
    public void ShowRufusResults()
    {
        browserHome?.SetActive(false);
        if (searchNoResults != null) searchNoResults.SetActive(false);
        browserSearchedRufusPanel?.SetActive(true);
        rufusDownloadPanel?.SetActive(false);
        browserSearchedISOPanel?.SetActive(false);
    }

    // Shows the ISO search result panel; hides home and Rufus panel.
    public void ShowISOResults()
    {
        browserHome?.SetActive(false);
        if (searchNoResults != null) searchNoResults.SetActive(false);
        browserSearchedRufusPanel?.SetActive(false);
        browserSearchedISOPanel?.SetActive(true);
        isoDownloadPanel?.SetActive(false);
    }

    // Shows the Rufus download panel (child of BrowserSearchedRufusPanel).
    // Wired to: Link button in BrowserSearchedRufusPanel.
    public void ShowRufusDownload()
    {
        rufusDownloadPanel?.SetActive(true);
    }

    // Hides the Rufus download panel, returning to BrowserSearchedRufusPanel content.
    // Wired to: Back button in RufusDownloadPanel.
    public void HideRufusDownload()
    {
        rufusDownloadPanel?.SetActive(false);
    }

    // Shows the ISO download panel (child of BrowserSearchedISOPanel).
    // Wired to: Link button in BrowserSearchedISOPanel.
    public void ShowISODownload()
    {
        isoDownloadPanel?.SetActive(true);
    }

    // Hides the ISO download panel, returning to BrowserSearchedISOPanel content.
    // Wired to: Back button in ISODownloadPanel.
    public void HideISODownload()
    {
        isoDownloadPanel?.SetActive(false);
    }

    // ----------------------------------------------------------------
    //  Search — wired to InputField On Submit and SearchButton OnClick
    // ----------------------------------------------------------------

    public void SubmitSearch()
    {
        if (searchField == null) return;

        string query = searchField.text != null ? searchField.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(query)) return;

        string lower = query.ToLowerInvariant();

        if (MatchesRufus(lower))
        {
            ShowRufusResults();
            Debug.Log($"[T2MonitorNavigator] Search '{query}' → Rufus results.");
        }
        else if (MatchesISO(lower))
        {
            ShowISOResults();
            Debug.Log($"[T2MonitorNavigator] Search '{query}' → ISO results.");
        }
        else
        {
            browserHome?.SetActive(true);
            if (searchNoResults != null) searchNoResults.SetActive(true);
            browserSearchedRufusPanel?.SetActive(false);
            browserSearchedISOPanel?.SetActive(false);
            Debug.Log($"[T2MonitorNavigator] Search '{query}' did not match any keyword.");
        }
    }

    // Matches any query containing the word "rufus".
    private static bool MatchesRufus(string lower)
    {
        return lower.Contains("rufus");
    }

    // Matches any query containing "iso", "10", and "windows" (or "win") in any order.
    // e.g. "iso 10 windows", "windows 10 iso", "10 iso windows", "download iso windows 10"
    private static bool MatchesISO(string lower)
    {
        return lower.Contains("iso") &&
               lower.Contains("10") &&
               (lower.Contains("windows") || lower.Contains("win"));
    }

    // ----------------------------------------------------------------
    //  Rufus download popup
    // ----------------------------------------------------------------

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
    public void DownloadRufus()
    {
        CloseRufusPopUp();

        if (_downloadCoroutine != null) StopCoroutine(_downloadCoroutine);

        if (IsRufusDownloaded)
        {
            _downloadCoroutine = StartCoroutine(ShowAlreadyDownloaded());
            return;
        }

        _downloadCoroutine = StartCoroutine(RunDownload(DownloadType.Rufus));
        Debug.Log("[T2MonitorNavigator] Rufus download started.");
    }

    // ----------------------------------------------------------------
    //  ISO download popup
    // ----------------------------------------------------------------

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
    public void DownloadISO()
    {
        CloseISOPopUp();

        if (_downloadCoroutine != null) StopCoroutine(_downloadCoroutine);

        if (IsIsoDownloaded)
        {
            _downloadCoroutine = StartCoroutine(ShowAlreadyDownloaded());
            return;
        }

        _downloadCoroutine = StartCoroutine(RunDownload(DownloadType.ISO));
        Debug.Log("[T2MonitorNavigator] ISO download started.");
    }

    // ----------------------------------------------------------------
    //  Download progress coroutine (shared by Rufus and ISO)
    // ----------------------------------------------------------------

    private IEnumerator RunDownload(DownloadType type)
    {
        _downloadRunning = true;
        SetInteractionBlocked(true);

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
        SetInteractionBlocked(false);
    }

    // Shows "already downloaded" in the label, hides the bar, then cleans up.
    private IEnumerator ShowAlreadyDownloaded()
    {
        SetInteractionBlocked(true);

        if (downloadProgressBar != null)
            downloadProgressBar.gameObject.SetActive(false);

        SetProgressLabel("Already downloaded!");
        downloadProgressContainer?.SetActive(true);

        yield return new WaitForSeconds(2.5f);

        downloadProgressContainer?.SetActive(false);

        if (downloadProgressBar != null)
            downloadProgressBar.gameObject.SetActive(true);

        SetInteractionBlocked(false);
        _downloadCoroutine = null;
    }

    private void SetInteractionBlocked(bool blocked)
    {
        if (canvasInteractionGroup == null) return;
        canvasInteractionGroup.interactable   = !blocked;
        canvasInteractionGroup.blocksRaycasts = !blocked;
    }

    private void SetProgressLabel(string text)
    {
        if (progressLabel != null)
            progressLabel.text = text;
    }

    // ----------------------------------------------------------------
    //  Rufus app
    // ----------------------------------------------------------------

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
