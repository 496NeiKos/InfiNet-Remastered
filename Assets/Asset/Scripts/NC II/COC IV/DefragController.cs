/*
 * ================================================================
 *  UNITY SETUP GUIDE — DefragController (COC IV)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Defrag Chain" GameObject (starts INACTIVE).
 *    Place as a DIRECT CHILD of App Panels so CloseAllApps() covers it.
 *
 *  ── FULL HIERARCHY ──────────────────────────────────────────────
 *
 *    Defrag Chain                          ← this script (starts INACTIVE)
 *      └── Optimize Drives Panel           → optimizeDrivesPanel (INACTIVE)
 *            ├── TitleTMP                  (static: "Optimize Drives")
 *            ├── DescTMP                   (static: "You can optimize your drives to help your
 *            │                              computer run more efficiently, or analyze them to
 *            │                              find out if they need to be optimized.")
 *            ├── StatusHeaderTMP           (static: "Status")
 *            ├── Column Header Row
 *            │     ├── DriveColTMP         (static: "Drive")
 *            │     ├── MediaTypeColTMP     (static: "Media type")
 *            │     ├── LastRunColTMP       (static: "Last run")
 *            │     └── CurrentStatusColTMP (static: "Current status")
 *            ├── Scroll View
 *            │     └── Content             → driveListContent  (Transform)
 *            ├── Button Row
 *            │     ├── AnalyzeBtn          → analyzeBtn   (starts Interactable: false)
 *            │     ├── OptimizeBtn         → optimizeBtn  (starts Interactable: false)
 *            │     └── StopBtn             → stopBtn      (starts INACTIVE)
 *            ├── ScheduledOptimizationTMP  → scheduledOptimizationTMP
 *            │     (static: "Scheduled optimization: On")
 *            ├── ScheduledFrequencyTMP     → scheduledFrequencyTMP
 *            │     (static: "Drives are being optimized automatically. Frequency: Weekly")
 *            ├── ChangeSettingsBtn         → changeSettingsBtn (no-op visual button)
 *            └── Footer
 *                  └── CloseBtn            → closeBtn
 *
 *  ── PREFAB ──────────────────────────────────────────────────────
 *    Create a prefab using DefragDriveRowUI.cs.
 *    Assign to: driveRowPrefab
 *
 *  ── TIMING ──────────────────────────────────────────────────────
 *    analyzeDuration   default 3s  — short analysis pass
 *    optimizeDuration  default 8s  — full optimization pass
 *    Both are SerializeFields — adjust in the Inspector.
 *
 *  ── INSPECTOR ASSIGNMENTS ───────────────────────────────────────
 *    All fields listed in the hierarchy above, plus driveRowPrefab.
 *
 *  ── WIRING ──────────────────────────────────────────────────────
 *    SearchWindowController.defragBtn OnClick
 *      → handled in SearchWindowController.OpenDefrag()
 *
 *  ── BEHAVIOR SUMMARY ────────────────────────────────────────────
 *    • Analyze: 3-second coroutine; during it both buttons are disabled;
 *      status shows "Analyzing..."; on completion updates Last run + status.
 *    • Optimize: 8-second coroutine; both Analyze and Optimize hide, Stop appears;
 *      status shows "Running: X%"; on completion updates Last run + status;
 *      clicking Stop cancels and restores the previous status.
 *    • Drive state (LastRun, CurrentStatus) persists across Close/reopen
 *      for the session because DriveData entries are mutable class instances.
 * ================================================================
 */

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DefragController : MonoBehaviour
{
    // ── Timing ────────────────────────────────────────────────────────────────

    [Header("Timing")]
    [SerializeField] private float analyzeDuration  = 3f;
    [SerializeField] private float optimizeDuration = 8f;

    // ── Optimize Drives Panel ─────────────────────────────────────────────────

    [Header("Optimize Drives Panel")]
    [SerializeField] private GameObject optimizeDrivesPanel;
    [SerializeField] private Transform  driveListContent;
    [SerializeField] private TMP_Text   scheduledOptimizationTMP;
    [SerializeField] private TMP_Text   scheduledFrequencyTMP;
    [SerializeField] private Button     analyzeBtn;
    [SerializeField] private Button     optimizeBtn;
    [SerializeField] private Button     stopBtn;
    [SerializeField] private Button     changeSettingsBtn;
    [SerializeField] private Button     closeBtn;

    // ── Prefab ────────────────────────────────────────────────────────────────

    [Header("Prefab")]
    [SerializeField] private GameObject driveRowPrefab;

    // ── Static Drive Data ─────────────────────────────────────────────────────
    // Entry fields (LastRun, CurrentStatus) are mutable — updates persist for
    // the session so the user sees accurate data when reopening the app.

    private static readonly DefragDriveEntry[] DriveData =
    {
        new DefragDriveEntry
        {
            DriveDisplay  = "C: (Windows)",
            MediaType     = "Solid State Drive",
            LastRun       = "10/7/2026 2:14 AM",
            CurrentStatus = "OK (0 days since last run)",
            IsSSD         = true
        },
        new DefragDriveEntry
        {
            DriveDisplay  = "D: (New Volume)",
            MediaType     = "Hard Disk Drive",
            LastRun       = "Never run",
            CurrentStatus = "OK (0% fragmented)",
            IsSSD         = false
        },
    };

    // ── Runtime State ─────────────────────────────────────────────────────────

    private readonly List<DefragDriveRowUI> _spawnedRows = new List<DefragDriveRowUI>();
    private DefragDriveRowUI _selectedRow;
    private Coroutine        _operationCoroutine;
    private bool             _isOptimizing;
    private string           _statusBeforeOperation;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        analyzeBtn?.onClick.AddListener(OnAnalyze);
        optimizeBtn?.onClick.AddListener(OnOptimize);
        stopBtn?.onClick.AddListener(OnStop);
        closeBtn?.onClick.AddListener(OnClose);

        HideAll();
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        PopulateDriveList();

        SetInteractable(analyzeBtn,  false);
        SetInteractable(optimizeBtn, false);
        if (stopBtn != null) stopBtn.gameObject.SetActive(false);

        if (scheduledOptimizationTMP != null)
            scheduledOptimizationTMP.text = "Scheduled optimization: On";
        if (scheduledFrequencyTMP != null)
            scheduledFrequencyTMP.text = "Drives are being optimized automatically. Frequency: Weekly";

        optimizeDrivesPanel?.SetActive(true);
        ActivityLogManager.Log("Opened Defragment and Optimize Drives", ActivityLogManager.EntryType.Action);
    }

    // ── Drive List ────────────────────────────────────────────────────────────

    private void PopulateDriveList()
    {
        foreach (DefragDriveRowUI row in _spawnedRows)
            if (row != null) Destroy(row.gameObject);
        _spawnedRows.Clear();
        _selectedRow = null;

        foreach (DefragDriveEntry entry in DriveData)
        {
            GameObject rowGO = Instantiate(driveRowPrefab, driveListContent);
            DefragDriveRowUI row = rowGO.GetComponent<DefragDriveRowUI>();
            if (row != null)
            {
                row.Setup(entry, OnDriveSelected);
                _spawnedRows.Add(row);
            }
        }
    }

    private void OnDriveSelected(DefragDriveRowUI row)
    {
        _selectedRow?.SetSelected(false);
        _selectedRow = row;
        _selectedRow.SetSelected(true);

        SetInteractable(analyzeBtn,  true);
        SetInteractable(optimizeBtn, true);
    }

    // ── Analyze ───────────────────────────────────────────────────────────────

    private void OnAnalyze()
    {
        if (_selectedRow == null) return;

        SetInteractable(analyzeBtn,  false);
        SetInteractable(optimizeBtn, false);

        _isOptimizing          = false;
        _statusBeforeOperation = _selectedRow.Entry.CurrentStatus;
        _selectedRow.UpdateStatus("Analyzing...");

        _operationCoroutine = StartCoroutine(RunOperation(analyzeDuration));
        ActivityLogManager.Log($"Analyzing {_selectedRow.Entry.DriveDisplay}...", ActivityLogManager.EntryType.Action);
    }

    // ── Optimize ──────────────────────────────────────────────────────────────

    private void OnOptimize()
    {
        if (_selectedRow == null) return;

        // Both action buttons hide; Stop takes their place
        if (analyzeBtn  != null) analyzeBtn.gameObject.SetActive(false);
        if (optimizeBtn != null) optimizeBtn.gameObject.SetActive(false);
        if (stopBtn     != null) stopBtn.gameObject.SetActive(true);

        _isOptimizing          = true;
        _statusBeforeOperation = _selectedRow.Entry.CurrentStatus;
        _selectedRow.UpdateStatus("Running: 0%");

        _operationCoroutine = StartCoroutine(RunOperation(optimizeDuration));
        ActivityLogManager.Log($"Optimizing {_selectedRow.Entry.DriveDisplay}...", ActivityLogManager.EntryType.Action);
    }

    // ── Stop ──────────────────────────────────────────────────────────────────

    private void OnStop()
    {
        if (_operationCoroutine != null)
        {
            StopCoroutine(_operationCoroutine);
            _operationCoroutine = null;
        }

        // Restore status to what it was before the operation
        if (_selectedRow != null)
        {
            _selectedRow.Entry.CurrentStatus = _statusBeforeOperation;
            _selectedRow.UpdateStatus(_statusBeforeOperation);
        }

        RestoreActionButtons();
        ActivityLogManager.Log($"Operation stopped on {_selectedRow?.Entry.DriveDisplay}.", ActivityLogManager.EntryType.Action);
    }

    // ── Operation Coroutine ───────────────────────────────────────────────────

    private IEnumerator RunOperation(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pct = Mathf.Clamp01(elapsed / duration) * 100f;

            if (_isOptimizing)
                _selectedRow?.UpdateStatus($"Running: {pct:F0}%");
            // Analyze keeps "Analyzing..." throughout — no percentage needed

            yield return null;
        }

        _operationCoroutine = null;
        OnOperationComplete();
    }

    private void OnOperationComplete()
    {
        if (_selectedRow == null) return;

        string now = System.DateTime.Now.ToString("M/d/yyyy h:mm tt");
        _selectedRow.Entry.LastRun = now;
        _selectedRow.UpdateLastRun(now);

        string completionStatus = _selectedRow.Entry.IsSSD
            ? "OK (0 days since last run)"
            : "OK (0% fragmented)";
        _selectedRow.Entry.CurrentStatus = completionStatus;
        _selectedRow.UpdateStatus(completionStatus);

        RestoreActionButtons();

        string opLabel = _isOptimizing ? "Optimization" : "Analysis";
        ActivityLogManager.Log($"{opLabel} of {_selectedRow.Entry.DriveDisplay} complete.", ActivityLogManager.EntryType.Action);
    }

    // ── Close ─────────────────────────────────────────────────────────────────

    private void OnClose()
    {
        if (_operationCoroutine != null)
        {
            StopCoroutine(_operationCoroutine);
            _operationCoroutine = null;
        }
        HideAll();
        gameObject.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void RestoreActionButtons()
    {
        if (stopBtn     != null) stopBtn.gameObject.SetActive(false);
        if (analyzeBtn  != null) analyzeBtn.gameObject.SetActive(true);
        if (optimizeBtn != null) optimizeBtn.gameObject.SetActive(true);

        bool hasSelection = _selectedRow != null;
        SetInteractable(analyzeBtn,  hasSelection);
        SetInteractable(optimizeBtn, hasSelection);
    }

    private void HideAll()
    {
        optimizeDrivesPanel?.SetActive(false);
    }

    private static void SetInteractable(Button btn, bool interactable)
    {
        if (btn != null) btn.interactable = interactable;
    }
}
