/*
 * ================================================================
 *  UNITY SETUP GUIDE — SystemRestoreController (COC IV)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "System Restore Chain" GameObject (starts INACTIVE).
 *    Place as a DIRECT CHILD of App Panels so CloseAllApps() covers it.
 *
 *  ── FULL HIERARCHY ──────────────────────────────────────────────
 *
 *    System Restore Chain                ← this script (starts INACTIVE)
 *      │
 *      ├── Run Panel                     → runPanel (INACTIVE)
 *      │     ├── Title Bar
 *      │     │     └── TitleTMP          (static: "Run")
 *      │     ├── DescriptionTMP          (static: "Type the name of a program, folder, document,
 *      │     │                            or Internet resource, and Windows will open it for you.")
 *      │     ├── Open Row
 *      │     │     ├── OpenLabelTMP      (static: "Open:")
 *      │     │     └── RunInput          → runInput  (TMP_InputField)
 *      │     ├── ErrorTMP               → runErrorTMP  (starts INACTIVE, red text)
 *      │     └── Footer
 *      │           ├── OKBtn            → runOKBtn
 *      │           ├── CancelBtn        → runCancelBtn
 *      │           └── BrowseBtn        → runBrowseBtn  (Interactable: false in Inspector + Awake)
 *      │
 *      ├── Panel 1 — Welcome             → panel1 (INACTIVE)
 *      │     ├── Title Bar
 *      │     │     └── TitleTMP          (static: "System Restore")
 *      │     ├── HeaderTMP               (static: "Restore system files and settings")
 *      │     ├── DescriptionTMP          (static: "This wizard helps you restore your computer's
 *      │     │                            system files and settings to an earlier point in time
 *      │     │                            without affecting your personal files, such as e-mail,
 *      │     │                            documents, or photos.")
 *      │     ├── Radio Options           ← add a ToggleGroup component to this GO
 *      │     │     │   Assign the ToggleGroup to both toggles' Group field.
 *      │     │     │   Set ToggleGroup Allow Switch Off = false.
 *      │     │     ├── Recommended Toggle  → recommendedToggle  (IS ON = true)
 *      │     │     │     ├── LabelTMP     (static: "Recommended restore")
 *      │     │     │     └── SubtitleTMP  → recommendedSubtitleTMP  (dynamic, set by code)
 *      │     │     └── Choose Toggle      → chooseDifferentToggle  (IS ON = false)
 *      │     │           └── LabelTMP    (static: "Choose a different restore point")
 *      │     └── Footer
 *      │           ├── BackBtn           → p1BackBtn   (Interactable: false in Inspector + Awake)
 *      │           ├── NextBtn           → p1NextBtn
 *      │           └── CancelBtn         → p1CancelBtn
 *      │
 *      ├── Panel 2 — Select              → panel2 (INACTIVE)
 *      │     ├── Title Bar
 *      │     │     └── TitleTMP          (static: "System Restore")
 *      │     ├── HeaderTMP               (static: "Restore your computer to the state it was in
 *      │     │                            before the selected event")
 *      │     ├── TimezoneTMP             (static: "Current time zone: GMT+8:00 Philippine Standard Time")
 *      │     ├── Column Headers          (static TMPs: "Date and Time" | "Description" | "Type")
 *      │     ├── Scroll View
 *      │     │     └── Content           → restorePointContent  (Transform)
 *      │     ├── Bottom Controls
 *      │     │     ├── ShowMoreToggle    → showMoreToggle  (Toggle, IS ON = false)
 *      │     │     │     └── LabelTMP   (static: "Show more restore points")
 *      │     │     └── ScanAffectedBtn  → scanAffectedBtn  (Interactable: false — set by code)
 *      │     │           └── LabelTMP  (static: "Scan for affected programs")
 *      │     └── Footer
 *      │           ├── BackBtn           → p2BackBtn
 *      │           ├── NextBtn           → p2NextBtn  (Interactable: false — set by code)
 *      │           └── CancelBtn         → p2CancelBtn
 *      │
 *      ├── Panel 3 — Confirm             → panel3 (INACTIVE)
 *      │     ├── Title Bar
 *      │     │     └── TitleTMP          (static: "System Restore")
 *      │     ├── HeaderTMP               (static: "Confirm your restore point")
 *      │     ├── DescriptionTMP          (static: "Your computer will be restored to the state it
 *      │     │                            was in before the event described below.")
 *      │     ├── Time Row
 *      │     │     ├── TimeLabelTMP      (static: "Time:")
 *      │     │     └── TimeValueTMP      → p3TimeTMP  (dynamic)
 *      │     ├── Description Row
 *      │     │     ├── DescLabelTMP      (static: "Description:")
 *      │     │     └── DescValueTMP      → p3DescTMP  (dynamic)
 *      │     ├── WarningTMP              (static, bold: "Once started, System Restore cannot be
 *      │     │                            interrupted. Do you want to continue?")
 *      │     ├── DrivesLabelTMP          (static: "Drives:")
 *      │     ├── Drives Scroll View
 *      │     │     └── Content
 *      │     │           └── DriveRowTMP (static: "Windows (C:)    Local Disk")
 *      │     └── Footer
 *      │           ├── BackBtn           → p3BackBtn
 *      │           ├── FinishBtn         → p3FinishBtn  (label: "Finish")
 *      │           └── CancelBtn         → p3CancelBtn
 *      │
 *      ├── Panel 4 — Restoring           → panel4 (INACTIVE)
 *      │     ├── Title Bar
 *      │     │     └── TitleTMP          (static: "System Restore")
 *      │     ├── StatusTMP              → panel4StatusTMP  (dynamic)
 *      │     └── ProgressSlider         → panel4Slider
 *      │           Interactable: false   Min: 0   Max: 1
 *      │           Remove the Handle Slide Area child (fill-only bar).
 *      │
 *      └── Scan Popup                   → scanPopup (INACTIVE)
 *            MUST be the LAST CHILD of System Restore Chain (renders on top).
 *            ├── Overlay                (fullscreen semi-transparent Image — blocks input behind)
 *            └── Popup Panel
 *                  ├── TitleTMP         (static: "Scan for Affected Programs")
 *                  ├── Section1LabelTMP (static: "Programs and drivers that will be deleted:")
 *                  ├── Section1RowTMP   (static: "Adobe Reader DC — Installed 10/11/2026")
 *                  ├── Section2LabelTMP (static: "Programs and drivers that might be restored:")
 *                  ├── Section2RowTMP   (static: "(None detected)")
 *                  ├── NoteTMP          (static, small: "This list may be incomplete. It does not
 *                  │                    include programs that were already installed when the
 *                  │                    selected event occurred.")
 *                  └── CloseBtn        → scanPopupCloseBtn
 *
 *  ── RESTORE POINT ROW PREFAB ────────────────────────────────────
 *    Reuse the existing RestorePointRowUI prefab (used by SystemProtectionController).
 *    Assign to: restoreRowPrefab
 *
 *  ── TIMING ──────────────────────────────────────────────────────
 *    restoreDuration       default 5s   — progress slider fill duration
 *    completionHoldSeconds default 3s   — hold after "completed" before ReloadScene
 *    Both are SerializeFields — adjust in the Inspector.
 *
 *  ── INSPECTOR ASSIGNMENTS ───────────────────────────────────────
 *    All fields as listed in the hierarchy above.
 *    runBrowseBtn: set Interactable = false in Inspector (also enforced in Awake).
 *    p1BackBtn:    set Interactable = false in Inspector (also enforced in Awake).
 *    restoreRowPrefab: drag the RestorePointRowUI prefab here.
 *
 *  ── WIRING ──────────────────────────────────────────────────────
 *    SearchWindowController.systemRestoreBtn OnClick
 *      → handled in SearchWindowController.OpenSystemRestore()
 *    Set systemRestoreBtn.interactable = true in the Inspector.
 *
 *  ── PANEL FLOW ──────────────────────────────────────────────────
 *    Open → Run Panel
 *    Run: "rstrui.exe" (case-insensitive) → Panel 1 (toggle reset to Recommended)
 *    Run: other input → show ErrorTMP, stay on Run Panel
 *    Panel 1 Recommended + Next → skip Panel 2, go to Panel 3 (row 0 pre-selected)
 *    Panel 1 Choose different + Next → Panel 2
 *    Panel 2: select row → Next → Panel 3
 *    Panel 2: Scan button (enabled when row selected) → Scan Popup (Panel 2 stays open)
 *    Panel 3: Back → Panel 1 (if came from Recommended) or Panel 2 (if from Choose different)
 *    Panel 3: Finish → Panel 4 (no Back/Cancel)
 *    Panel 4: slider animates → "completed" → hold → ReloadScene()
 *    Any Cancel → CloseChain
 * ================================================================
 */

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SystemRestoreController : MonoBehaviour
{
    // ── Timing ────────────────────────────────────────────────────────────────

    [Header("Timing")]
    [SerializeField] private float restoreDuration       = 5f;
    [SerializeField] private float completionHoldSeconds = 3f;

    // ── Run Panel ─────────────────────────────────────────────────────────────

    [Header("Run Panel")]
    [SerializeField] private GameObject     runPanel;
    [SerializeField] private TMP_InputField runInput;
    [SerializeField] private TMP_Text       runErrorTMP;
    [SerializeField] private Button         runOKBtn;
    [SerializeField] private Button         runCancelBtn;
    [SerializeField] private Button         runBrowseBtn;

    // ── Panel 1 — Welcome ─────────────────────────────────────────────────────

    [Header("Panel 1 — Welcome")]
    [SerializeField] private GameObject panel1;
    [SerializeField] private Toggle     recommendedToggle;
    [SerializeField] private Toggle     chooseDifferentToggle;
    [SerializeField] private TMP_Text   recommendedSubtitleTMP;
    [SerializeField] private Button     p1BackBtn;
    [SerializeField] private Button     p1NextBtn;
    [SerializeField] private Button     p1CancelBtn;

    // ── Panel 2 — Select ──────────────────────────────────────────────────────

    [Header("Panel 2 — Select Restore Point")]
    [SerializeField] private GameObject panel2;
    [SerializeField] private Transform  restorePointContent;
    [SerializeField] private Toggle     showMoreToggle;
    [SerializeField] private Button     scanAffectedBtn;
    [SerializeField] private Button     p2BackBtn;
    [SerializeField] private Button     p2NextBtn;
    [SerializeField] private Button     p2CancelBtn;

    // ── Panel 3 — Confirm ─────────────────────────────────────────────────────

    [Header("Panel 3 — Confirm")]
    [SerializeField] private GameObject panel3;
    [SerializeField] private TMP_Text   p3TimeTMP;
    [SerializeField] private TMP_Text   p3DescTMP;
    [SerializeField] private Button     p3BackBtn;
    [SerializeField] private Button     p3FinishBtn;
    [SerializeField] private Button     p3CancelBtn;

    // ── Panel 4 — Restoring ───────────────────────────────────────────────────

    [Header("Panel 4 — Restoring")]
    [SerializeField] private GameObject panel4;
    [SerializeField] private TMP_Text   panel4StatusTMP;
    [SerializeField] private Slider     panel4Slider;

    // ── Scan Popup ────────────────────────────────────────────────────────────

    [Header("Scan Popup")]
    [SerializeField] private GameObject scanPopup;
    [SerializeField] private Button     scanPopupCloseBtn;

    // ── Prefab ────────────────────────────────────────────────────────────────

    [Header("Prefab")]
    [SerializeField] private GameObject restoreRowPrefab;

    // ── Static Restore Point Data ─────────────────────────────────────────────

    private static readonly RestorePointEntry[] RestorePoints =
    {
        new RestorePointEntry { DateTime = "10/12/2026 3:45 PM",  Name = "Windows Update",             Type = "Critical Update", IsHiddenByDefault = false },
        new RestorePointEntry { DateTime = "10/10/2026 11:00 AM", Name = "Installed: Adobe Reader DC", Type = "Install",         IsHiddenByDefault = false },
        new RestorePointEntry { DateTime = "10/7/2026 9:30 AM",   Name = "Windows Update",             Type = "Critical Update", IsHiddenByDefault = false },
        new RestorePointEntry { DateTime = "10/3/2026 2:15 PM",   Name = "Manual Restore Point",       Type = "Manual",          IsHiddenByDefault = true  },
        new RestorePointEntry { DateTime = "9/28/2026 8:00 AM",   Name = "Device Driver Installation", Type = "Install",         IsHiddenByDefault = true  },
    };

    // ── Singleton ─────────────────────────────────────────────────────────────

    public static SystemRestoreController Instance { get; private set; }

    // ── Task State ────────────────────────────────────────────────────────────

    public bool RunPanelOpen       { get; private set; }
    public bool Panel1Open         { get; private set; }
    public bool SRPanel2Shown      { get; private set; }
    public bool ShowMoreToggled    { get; private set; }
    public bool ScanAffectedClicked { get; private set; }
    public bool SRConfirmShown     { get; private set; }
    public bool SRFinishClicked    { get; private set; }
    public bool SRRestartTriggered { get; private set; }

    // ── Runtime State ─────────────────────────────────────────────────────────

    private RestorePointRowUI                _selectedRow;
    private bool                             _cameFromRecommended;
    private readonly List<RestorePointRowUI> _spawnedRows = new List<RestorePointRowUI>();
    private bool                             _rowsInitialized;
    private Coroutine                        _restoreCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Run Panel
        runOKBtn?.onClick.AddListener(OnRunOK);
        runCancelBtn?.onClick.AddListener(CloseChain);
        if (runBrowseBtn != null) runBrowseBtn.interactable = false;
        runInput?.onValueChanged.AddListener(_ => HideRunError());

        // Panel 1
        if (p1BackBtn != null) p1BackBtn.interactable = false;
        p1NextBtn?.onClick.AddListener(OnPanel1Next);
        p1CancelBtn?.onClick.AddListener(CloseChain);

        // Panel 2
        showMoreToggle?.onValueChanged.AddListener(OnShowMoreChanged);
        scanAffectedBtn?.onClick.AddListener(OpenScanPopup);
        p2BackBtn?.onClick.AddListener(OnPanel2Back);
        p2NextBtn?.onClick.AddListener(OnPanel2Next);
        p2CancelBtn?.onClick.AddListener(CloseChain);

        // Panel 3
        p3BackBtn?.onClick.AddListener(OnPanel3Back);
        p3FinishBtn?.onClick.AddListener(OnPanel3Finish);
        p3CancelBtn?.onClick.AddListener(CloseChain);

        // Panel 4 — slider is display only
        if (panel4Slider != null) panel4Slider.interactable = false;

        // Scan Popup
        scanPopupCloseBtn?.onClick.AddListener(CloseScanPopup);

        HideAll();
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        if (_restoreCoroutine != null)
        {
            StopCoroutine(_restoreCoroutine);
            _restoreCoroutine = null;
        }

        _cameFromRecommended = false;
        ClearRows();
        showMoreToggle?.SetIsOnWithoutNotify(false);

        HideAll();
        gameObject.SetActive(true);
        ShowRunPanel();

        ActivityLogManager.Log("Opened System Restore", ActivityLogManager.EntryType.Action);
    }

    // ── Run Panel ─────────────────────────────────────────────────────────────

    private void ShowRunPanel()
    {
        RunPanelOpen = true;
        HideRunError();
        if (runInput != null) runInput.text = "";
        runPanel?.SetActive(true);
        runInput?.ActivateInputField();
    }

    private void OnRunOK()
    {
        string value = runInput != null ? runInput.text.Trim() : "";

        if (value.Equals("rstrui.exe", System.StringComparison.OrdinalIgnoreCase))
        {
            runPanel?.SetActive(false);
            ShowPanel1(resetToggle: true);
            ActivityLogManager.Log("Run: executed rstrui.exe", ActivityLogManager.EntryType.Action);
        }
        else
        {
            ShowRunError(string.IsNullOrEmpty(value) ? "(empty)" : value);
        }
    }

    private void ShowRunError(string value)
    {
        if (runErrorTMP == null) return;
        runErrorTMP.text = $"Windows cannot find '{value}'. Make sure you typed the name correctly, and then try again.";
        runErrorTMP.gameObject.SetActive(true);
    }

    private void HideRunError()
    {
        runErrorTMP?.gameObject.SetActive(false);
    }

    // ── Panel 1 — Welcome ─────────────────────────────────────────────────────

    private void ShowPanel1(bool resetToggle)
    {
        Panel1Open = true;

        if (resetToggle)
        {
            recommendedToggle?.SetIsOnWithoutNotify(true);
            chooseDifferentToggle?.SetIsOnWithoutNotify(false);
        }

        if (recommendedSubtitleTMP != null)
            recommendedSubtitleTMP.text = $"{RestorePoints[0].DateTime}: {RestorePoints[0].Name}";

        panel1?.SetActive(true);
    }

    private void OnPanel1Next()
    {
        bool useRecommended = recommendedToggle != null && recommendedToggle.isOn;

        if (useRecommended)
        {
            EnsureRowsInitialized();

            // Pre-select row 0 (most recent — same one shown in the subtitle)
            if (_spawnedRows.Count > 0)
            {
                if (_selectedRow != null && _selectedRow != _spawnedRows[0])
                    _selectedRow.SetSelected(false);

                _selectedRow = _spawnedRows[0];
                _selectedRow.SetSelected(true);
            }

            _cameFromRecommended = true;
            panel1?.SetActive(false);
            ShowPanel3();
        }
        else
        {
            _cameFromRecommended = false;
            panel1?.SetActive(false);
            ShowPanel2();
        }
    }

    // ── Panel 2 — Select ──────────────────────────────────────────────────────

    private void ShowPanel2()
    {
        SRPanel2Shown = true;
        EnsureRowsInitialized();
        UpdateP2ButtonStates();
        panel2?.SetActive(true);
    }

    private void EnsureRowsInitialized()
    {
        if (_rowsInitialized) return;

        foreach (RestorePointEntry entry in RestorePoints)
        {
            if (restoreRowPrefab == null || restorePointContent == null) break;

            GameObject go = Instantiate(restoreRowPrefab, restorePointContent);
            RestorePointRowUI row = go.GetComponent<RestorePointRowUI>();
            if (row == null) { Destroy(go); continue; }

            row.Setup(entry, OnRowSelected);
            _spawnedRows.Add(row);

            if (entry.IsHiddenByDefault)
                go.SetActive(false);
        }

        _rowsInitialized = true;
    }

    private void OnRowSelected(RestorePointRowUI row)
    {
        if (_selectedRow != null && _selectedRow != row)
            _selectedRow.SetSelected(false);

        _selectedRow = row;
        _selectedRow?.SetSelected(true);
        UpdateP2ButtonStates();
    }

    private void OnShowMoreChanged(bool isOn)
    {
        if (isOn) ShowMoreToggled = true;

        foreach (RestorePointRowUI row in _spawnedRows)
        {
            if (row == null || !row.Entry.IsHiddenByDefault) continue;
            row.gameObject.SetActive(isOn);
        }

        // Deselect if the currently selected row just became hidden
        if (!isOn && _selectedRow != null && _selectedRow.Entry.IsHiddenByDefault)
        {
            _selectedRow.SetSelected(false);
            _selectedRow = null;
            UpdateP2ButtonStates();
        }
    }

    private void UpdateP2ButtonStates()
    {
        bool hasSelection = _selectedRow != null;
        if (p2NextBtn       != null) p2NextBtn.interactable       = hasSelection;
        if (scanAffectedBtn != null) scanAffectedBtn.interactable = hasSelection;
    }

    private void OnPanel2Back()
    {
        panel2?.SetActive(false);
        ShowPanel1(resetToggle: false);
    }

    private void OnPanel2Next()
    {
        panel2?.SetActive(false);
        ShowPanel3();
    }

    // ── Scan Popup ────────────────────────────────────────────────────────────

    private void OpenScanPopup()
    {
        ScanAffectedClicked = true;
        scanPopup?.SetActive(true);
        ActivityLogManager.Log("Opened Scan for Affected Programs", ActivityLogManager.EntryType.Action);
    }

    private void CloseScanPopup()
    {
        scanPopup?.SetActive(false);
    }

    // ── Panel 3 — Confirm ─────────────────────────────────────────────────────

    private void ShowPanel3()
    {
        // Safety guard — should never be null at this point in the flow
        if (_selectedRow == null) return;

        SRConfirmShown = true;

        if (p3TimeTMP != null) p3TimeTMP.text = _selectedRow.Entry.DateTime;
        if (p3DescTMP != null) p3DescTMP.text = _selectedRow.Entry.Name;

        panel3?.SetActive(true);
    }

    private void OnPanel3Back()
    {
        panel3?.SetActive(false);

        if (_cameFromRecommended)
            ShowPanel1(resetToggle: false);
        else
            ShowPanel2();
    }

    private void OnPanel3Finish()
    {
        SRFinishClicked = true;
        panel3?.SetActive(false);
        ShowPanel4();
        ActivityLogManager.Log($"System Restore initiated — restore point: {_selectedRow?.Entry.Name}", ActivityLogManager.EntryType.Action);
    }

    // ── Panel 4 — Restoring ───────────────────────────────────────────────────

    private void ShowPanel4()
    {
        if (panel4Slider    != null) panel4Slider.value    = 0f;
        if (panel4StatusTMP != null) panel4StatusTMP.text  = "Restoring your files and settings...";

        panel4?.SetActive(true);
        _restoreCoroutine = StartCoroutine(RunRestore());
    }

    private IEnumerator RunRestore()
    {
        float elapsed = 0f;

        while (elapsed < restoreDuration)
        {
            elapsed += Time.deltaTime;
            if (panel4Slider != null)
                panel4Slider.value = Mathf.Clamp01(elapsed / restoreDuration);
            yield return null;
        }

        if (panel4Slider    != null) panel4Slider.value   = 1f;
        if (panel4StatusTMP != null) panel4StatusTMP.text = "System restore completed successfully.";

        ActivityLogManager.Log("System Restore completed successfully.", ActivityLogManager.EntryType.Action);

        yield return new WaitForSeconds(completionHoldSeconds);

        _restoreCoroutine = null;
        SRRestartTriggered = true;
        ServerVirtualOSManager.Instance?.TriggerRestart();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void HideAll()
    {
        runPanel?.SetActive(false);
        panel1?.SetActive(false);
        panel2?.SetActive(false);
        panel3?.SetActive(false);
        panel4?.SetActive(false);
        scanPopup?.SetActive(false);
    }

    private void ClearRows()
    {
        foreach (RestorePointRowUI row in _spawnedRows)
            if (row != null) Destroy(row.gameObject);

        _spawnedRows.Clear();
        _rowsInitialized = false;
        _selectedRow     = null;
    }

    private void CloseChain()
    {
        if (_restoreCoroutine != null)
        {
            StopCoroutine(_restoreCoroutine);
            _restoreCoroutine = null;
        }

        ClearRows();
        HideAll();
        gameObject.SetActive(false);

        ActivityLogManager.Log("System Restore closed.", ActivityLogManager.EntryType.Action);
    }
}
