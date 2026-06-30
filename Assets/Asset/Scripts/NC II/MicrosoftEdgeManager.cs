/*
 * ================================================================
 *  UNITY SETUP GUIDE — MicrosoftEdgeManager
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to the MicrosoftEdgePanel GameObject.
 *
 *  HIERARCHY
 *
 *    MicrosoftEdgePanel  (this script here, start INACTIVE)
 *      ├─ MicrosoftEdgeHome         → microsoftEdgeHome  (GameObject, start ACTIVE)
 *      ├─ ME_TopNav                 (always active — persistent, not managed by script)
 *      │    ├─ MicrosoftEdgeTitle   (Button or add Button component) → OnClick: MicrosoftEdgeManager.ResetToHome()
 *      │    ├─ MinimizeBtn          → OnClick: MicrosoftEdgeManager.Minimize()
 *      │    └─ ExitBtn              → OnClick: MicrosoftEdgeManager.Exit()
 *      ├─ ME_SearchBar              → searchBar (GameObject — the whole bar, hidden on download pages)
 *      │    ├─ TMP_InputField       → searchInput (TMP_InputField)
 *      │    └─ SearchButton         → OnClick: MicrosoftEdgeManager.SubmitSearch()
 *      ├─ MicrosoftEdgeResult       → microsoftEdgeResult (GameObject, start INACTIVE)
 *      │    ├─ SearchResultFailed   → searchResultFailed  (GameObject, start INACTIVE)
 *      │    ├─ GoogleChromeSearched → googleChromeSearched (GameObject, start INACTIVE)
 *      │    │    ├─ [content]
 *      │    │    ├─ LinkBtn → OnClick: MicrosoftEdgeManager.ShowGoogleChromeDownloadPage()
 *      │    │    └─ GoogleChromeDownloadPage → googleChromeDownloadPage (start INACTIVE)
 *      │    │         └─ DownloadBtn → OnClick: MicrosoftEdgeManager.ShowDownloadConfirm(0)
 *      │    └─ WinRarSearched       → winRarSearched (GameObject, start INACTIVE)
 *      │         ├─ [content]
 *      │         ├─ LinkBtn → OnClick: MicrosoftEdgeManager.ShowWinRarDownloadPage()
 *      │         └─ WinRarDownloadPage → winRarDownloadPage (start INACTIVE)
 *      │              └─ DownloadBtn → OnClick: MicrosoftEdgeManager.ShowDownloadConfirm(1)
 *      ├─ DownloadConfirmPanel      → downloadConfirmPanel (GameObject, start INACTIVE)
 *      │    ├─ ReturnBtn  → OnClick: MicrosoftEdgeManager.OnReturnClicked()
 *      │    └─ ConfirmBtn → OnClick: MicrosoftEdgeManager.OnConfirmClicked()
 *      └─ DownloadProgressContainer → downloadProgressContainer (GameObject, start INACTIVE)
 *           ├─ ProgressBar          → downloadProgressBar (UI Slider, Min=0, Max=1, Interactable OFF)
 *           └─ ProgressLabel        → progressLabel (TMP_Text, optional)
 *
 *  INSPECTOR ASSIGNMENTS
 *    microsoftEdgeHome           → MicrosoftEdgeHome child
 *    microsoftEdgeResult         → MicrosoftEdgeResult child
 *    searchResultFailed          → MicrosoftEdgeResult > SearchResultFailed
 *    googleChromeSearched        → MicrosoftEdgeResult > GoogleChromeSearched
 *    winRarSearched              → MicrosoftEdgeResult > WinRarSearched
 *    googleChromeDownloadPage    → GoogleChromeSearched > GoogleChromeDownloadPage
 *    winRarDownloadPage          → WinRarSearched > WinRarDownloadPage
 *    searchBar                   → ME_SearchBar GameObject (the whole bar container)
 *    searchInput                 → ME_SearchBar > TMP_InputField
 *    downloadConfirmPanel        → DownloadConfirmPanel child
 *    downloadProgressContainer   → DownloadProgressContainer child
 *    downloadProgressBar         → DownloadProgressContainer > ProgressBar (UI Slider)
 *    progressLabel               → DownloadProgressContainer > ProgressLabel (TMP_Text, optional)
 *    downloadDuration            → seconds the progress bar takes to fill (default 5)
 *    desktopManager              → DesktopContent > DesktopManager component
 *
 *  HOW IT WORKS
 *    Search: user types in ME_SearchBar and presses Enter or SearchButton.
 *      Case-insensitive substring match:
 *        "google chrome" → GoogleChromeSearched
 *        "winrar"        → WinRarSearched
 *        anything else   → SearchResultFailed  (including "microsoft word")
 *      Each new search resets the previous result.
 *
 *    Download flow:
 *      DownloadButton → ShowDownloadConfirm(appTypeIndex) — records pending app
 *      ReturnBtn      → hides DownloadConfirmPanel (no download started)
 *      ConfirmBtn     → starts download progress coroutine (cannot be cancelled)
 *      On complete    → DesktopManager.InstallApp(pendingApp) — enables app button
 *
 *    Panel reset:
 *      ResetToHome() is called when MicrosoftEdgePanel is opened from the desktop.
 *      It shows MicrosoftEdgeHome and hides all result/download panels.
 * ================================================================
 */

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MicrosoftEdgeManager : MonoBehaviour
{
    // ----------------------------------------------------------------
    //  Home / Result panels
    // ----------------------------------------------------------------

    [Header("Panels")]
    [SerializeField] private GameObject microsoftEdgeHome;
    [SerializeField] private GameObject microsoftEdgeResult;
    [SerializeField] private GameObject searchResultFailed;
    [SerializeField] private GameObject googleChromeSearched;
    [SerializeField] private GameObject winRarSearched;

    // ----------------------------------------------------------------
    //  Download pages (inside each result panel)
    // ----------------------------------------------------------------

    [Header("Download Pages")]
    [SerializeField] private GameObject googleChromeDownloadPage;
    [SerializeField] private GameObject winRarDownloadPage;

    // ----------------------------------------------------------------
    //  Search
    // ----------------------------------------------------------------

    [Header("Search")]
    [SerializeField] private GameObject searchBar;
    [SerializeField] private TMP_InputField searchInput;

    // ----------------------------------------------------------------
    //  Download confirm + progress
    // ----------------------------------------------------------------

    [Header("Download")]
    [SerializeField] private GameObject downloadConfirmPanel;
    [SerializeField] private GameObject downloadProgressContainer;
    [SerializeField] private Slider downloadProgressBar;
    [SerializeField] private TMP_Text progressLabel;
    [SerializeField] private float downloadDuration = 5f;

    // ----------------------------------------------------------------
    //  References
    // ----------------------------------------------------------------

    [Header("References")]
    [SerializeField] private DesktopManager desktopManager;

    // ----------------------------------------------------------------
    //  Runtime state
    // ----------------------------------------------------------------

    private AppType _pendingApp;
    private Coroutine _downloadCoroutine;
    private TMP_InputField _lastFocusedField;

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        HideAllResultPanels();
        downloadConfirmPanel?.SetActive(false);
        downloadProgressContainer?.SetActive(false);

        if (downloadProgressBar != null)
            downloadProgressBar.value = 0f;

        // Native InputField submit — fires on Enter regardless of Input System state
        if (searchInput != null)
            searchInput.onEndEdit.AddListener(OnSearchEndEdit);

        // Auto-resolve desktopManager if not wired in the Inspector
        if (desktopManager == null)
            desktopManager = FindObjectOfType<DesktopManager>();
    }

    private void OnDestroy()
    {
        if (searchInput != null)
            searchInput.onEndEdit.RemoveListener(OnSearchEndEdit);
    }

    private void OnSearchEndEdit(string value)
    {
        // onEndEdit fires on both Enter and focus-lost; only act on Enter
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            (UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame ||
             UnityEngine.InputSystem.Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            SubmitSearch();
        }
    }

    // ----------------------------------------------------------------
    //  Public — window control (wired to Minimize / Exit buttons in ME_TopNav)
    // ----------------------------------------------------------------

    public void Minimize()
    {
        gameObject.SetActive(false);
        Debug.Log("[MicrosoftEdgeManager] Minimized — browse state preserved.");
    }

    public void Exit()
    {
        ResetToHome();
        gameObject.SetActive(false);
        Debug.Log("[MicrosoftEdgeManager] Exited — reset to home.");
    }

    // ----------------------------------------------------------------
    //  Enter key detection — matches Windows10Manager pattern
    // ----------------------------------------------------------------

    private void Update()
    {
        if (searchInput != null && searchInput.isFocused)
            _lastFocusedField = searchInput;

        var kb = Keyboard.current;
        if (kb == null) return;
        if (!kb.enterKey.wasPressedThisFrame && !kb.numpadEnterKey.wasPressedThisFrame) return;
        if (_lastFocusedField == null || _lastFocusedField != searchInput) return;

        SubmitSearch();
    }

    // ----------------------------------------------------------------
    //  Public API
    // ----------------------------------------------------------------

    public void ResetToHome()
    {
        HideAllResultPanels();
        microsoftEdgeHome?.SetActive(true);
        microsoftEdgeResult?.SetActive(false);
        downloadConfirmPanel?.SetActive(false);
        downloadProgressContainer?.SetActive(false);
        searchBar?.SetActive(true);

        if (searchInput != null) searchInput.text = "";
        _lastFocusedField = null;

        Debug.Log("[MicrosoftEdgeManager] Reset to home.");
    }

    public void SubmitSearch()
    {
        if (searchInput == null) return;

        string query = searchInput.text;
        if (string.IsNullOrWhiteSpace(query)) return;

        // Hide download pages from any prior result before switching
        googleChromeDownloadPage?.SetActive(false);
        winRarDownloadPage?.SetActive(false);

        microsoftEdgeHome?.SetActive(false);
        microsoftEdgeResult?.SetActive(true);

        GameObject resultPanel = EvaluateSearch(query);
        ShowResultPanel(resultPanel);

        Debug.Log($"[MicrosoftEdgeManager] Search: \"{query}\" → {resultPanel?.name ?? "none"}.");
    }

    // ----------------------------------------------------------------
    //  Download page links (wired to LinkBtn OnClick in scene)
    // ----------------------------------------------------------------

    public void ShowGoogleChromeDownloadPage()
    {
        searchBar?.SetActive(false);
        googleChromeDownloadPage?.SetActive(true);
        Debug.Log("[MicrosoftEdgeManager] Google Chrome download page shown.");
    }

    public void ShowWinRarDownloadPage()
    {
        searchBar?.SetActive(false);
        winRarDownloadPage?.SetActive(true);
        Debug.Log("[MicrosoftEdgeManager] WinRar download page shown.");
    }

    // ----------------------------------------------------------------
    //  Download confirm — wired to DownloadBtn OnClick
    //  appTypeIndex: 0 = GoogleChrome, 1 = WinRar  (matches AppType enum)
    // ----------------------------------------------------------------

    public void ShowDownloadConfirm(int appTypeIndex)
    {
        _pendingApp = (AppType)appTypeIndex;
        downloadConfirmPanel?.SetActive(true);
        Debug.Log($"[MicrosoftEdgeManager] DownloadConfirmPanel shown for {_pendingApp}.");
    }

    public void OnReturnClicked()
    {
        downloadConfirmPanel?.SetActive(false);
        Debug.Log("[MicrosoftEdgeManager] Download cancelled.");
    }

    public void OnConfirmClicked()
    {
        downloadConfirmPanel?.SetActive(false);

        if (_downloadCoroutine != null)
            StopCoroutine(_downloadCoroutine);

        if (DesktopManager.IsInstalled(_pendingApp))
        {
            _downloadCoroutine = StartCoroutine(ShowAlreadyDownloaded());
            Debug.Log($"[MicrosoftEdgeManager] {_pendingApp} already installed — showing notice.");
            return;
        }

        _downloadCoroutine = StartCoroutine(RunDownload());
        Debug.Log($"[MicrosoftEdgeManager] Download confirmed for {_pendingApp}.");
    }

    private IEnumerator ShowAlreadyDownloaded()
    {
        if (downloadProgressBar != null)
            downloadProgressBar.gameObject.SetActive(false);

        SetProgressLabel("This Windows Application is already downloaded!");
        downloadProgressContainer?.SetActive(true);

        yield return new WaitForSeconds(2.5f);

        downloadProgressContainer?.SetActive(false);

        if (downloadProgressBar != null)
            downloadProgressBar.gameObject.SetActive(true);

        _downloadCoroutine = null;
    }

    // ----------------------------------------------------------------
    //  Download progress coroutine
    // ----------------------------------------------------------------

    private IEnumerator RunDownload()
    {
        downloadProgressContainer?.SetActive(true);

        if (downloadProgressBar != null)
            downloadProgressBar.value = 0f;

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

        if (downloadProgressBar != null)
            downloadProgressBar.value = 1f;

        SetProgressLabel("Download complete!");
        _downloadCoroutine = null;

        Debug.Log($"[MicrosoftEdgeManager] Download complete — installing {_pendingApp}.");
        OnDownloadComplete();
    }

    private void OnDownloadComplete()
    {
        desktopManager?.InstallApp(_pendingApp);

        // Brief pause before hiding progress so player sees "Download complete!"
        StartCoroutine(HideProgressAfterDelay(1.5f));
    }

    private IEnumerator HideProgressAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        downloadProgressContainer?.SetActive(false);
    }

    // ----------------------------------------------------------------
    //  Helpers
    // ----------------------------------------------------------------

    private GameObject EvaluateSearch(string input)
    {
        string lower = input.ToLower();

        if (lower.Contains("google chrome") || lower.Contains("chrome")) return googleChromeSearched;
        if (lower.Contains("winrar"))                                    return winRarSearched;

        // "microsoft word" and all other inputs → failed
        return searchResultFailed;
    }

    private void ShowResultPanel(GameObject panel)
    {
        searchResultFailed?.SetActive(false);
        googleChromeSearched?.SetActive(false);
        winRarSearched?.SetActive(false);

        panel?.SetActive(true);
    }

    private void HideAllResultPanels()
    {
        microsoftEdgeHome?.SetActive(false);
        microsoftEdgeResult?.SetActive(false);
        searchResultFailed?.SetActive(false);
        googleChromeSearched?.SetActive(false);
        winRarSearched?.SetActive(false);
        googleChromeDownloadPage?.SetActive(false);
        winRarDownloadPage?.SetActive(false);
    }

    private void SetProgressLabel(string text)
    {
        if (progressLabel != null)
            progressLabel.text = text;
    }
}
