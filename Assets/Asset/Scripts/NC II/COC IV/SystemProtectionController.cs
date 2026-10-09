/*
 * ================================================================
 *  UNITY SETUP GUIDE — SystemProtectionController (COC IV)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "System Protection Chain" GameObject (starts INACTIVE).
 *    Place as a DIRECT CHILD of App Panels (appPanelsRoot) so that
 *    ServerVirtualOSManager.CloseAllApps() auto-deactivates it on restart.
 *
 *  ── FULL HIERARCHY ──────────────────────────────────────────────
 *
 *    System Protection Chain            ← this script here (starts INACTIVE)
 *      │
 *      ├── System Properties Panel      → systemPropertiesPanel (INACTIVE)
 *      │     ├── TitleBar
 *      │     │     └── TitleTMP                   (static: "System Properties")
 *      │     ├── Tab Header                        (static: "System Protection")
 *      │     ├── System Restore Section
 *      │     │     ├── SystemRestoreHeaderTMP      (static: "System Restore")
 *      │     │     ├── SystemRestoreDescTMP        (static: "You can undo system changes by
 *      │     │     │                                reverting your computer to a previous restore point.")
 *      │     │     └── SystemRestoreBtn            → systemRestoreBtn
 *      │     │           Label: "System Restore..."
 *      │     ├── Protection Settings Section
 *      │     │     ├── SectionHeaderTMP            (static: "Protection Settings")
 *      │     │     ├── Column Header Row
 *      │     │     │     ├── AvailableDrivesTMP    (static: "Available Drives")
 *      │     │     │     └── ProtectionHeaderTMP   (static: "Protection")
 *      │     │     └── Drive Row
 *      │     │           ├── DriveNameTMP          (static: "Local Disk (C:) (System)")
 *      │     │           └── ProtectionStatusTMP   → protectionStatusTMP  (dynamic: "On"/"Off")
 *      │     ├── ConfigureBtn                      → configureBtn
 *      │     ├── CreateBtn                         → createBtn
 *      │     └── Footer
 *      │           ├── OKBtn                       → sysPropsOKBtn     (closes chain)
 *      │           ├── CancelBtn                   → sysPropsCancelBtn (closes chain)
 *      │           └── ApplyBtn                    → sysPropsApplyBtn  (no-op)
 *      │
 *      ├── Config Panel                 → configPanel (INACTIVE)
 *      │     ├── TitleTMP               (static: "System Protection for Local Disk (C:)")
 *      │     ├── Restore Settings Section
 *      │     │     ├── SectionLabelTMP  (static: "Restore Settings")
 *      │     │     ├── TurnOnToggle     → turnOnToggle   (ToggleGroup — default ON)
 *      │     │     │     Label: "Turn on system protection"
 *      │     │     └── DisableToggle    → disableToggle  (ToggleGroup)
 *      │     │           Label: "Disable system protection"
 *      │     │     NOTE: Add a ToggleGroup component to "Restore Settings Section".
 *      │     │           Assign the group to both toggles' "Group" field.
 *      │     │           Set ToggleGroup "Allow Switch Off" = false.
 *      │     ├── Disk Space Usage Section
 *      │     │     ├── SectionLabelTMP  (static: "Disk Space Usage")
 *      │     │     ├── CurrentUsage Row
 *      │     │     │     ├── CurrentUsageLabelTMP  (static: "Current Usage:")
 *      │     │     │     └── CurrentUsageTMP       → currentUsageTMP  (dynamic)
 *      │     │     ├── MaxUsageLabelTMP  (static: "Max Usage:")
 *      │     │     ├── MaxUsageSlider    → maxUsageSlider
 *      │     │     │     Min: 0.01  Max: 0.15  Default: 0.05  Interactable: true
 *      │     │     └── MaxUsageValueTMP  → maxUsageValueTMP  (dynamic: "5% (2.50 GB)")
 *      │     ├── DeleteBtn              → configDeleteBtn
 *      │     │     Label: "Delete"
 *      │     └── Footer
 *      │           ├── OKBtn            → configOKBtn
 *      │           ├── CancelBtn        → configCancelBtn
 *      │           └── ApplyBtn         → configApplyBtn  (no-op)
 *      │
 *      ├── Create Dialog Panel          → createDialogPanel (INACTIVE)
 *      │     ├── TitleTMP               (static: "System Protection")
 *      │     ├── CreateLabelTMP         (static: "Create a restore point")
 *      │     ├── DescriptionLabelTMP    (static: "Type a description to help you identify
 *      │     │                          the restore point. The current date and time are
 *      │     │                          added automatically.")
 *      │     ├── NameInput              → nameInput  (TMP_InputField)
 *      │     └── Footer
 *      │           ├── CreateBtn        → createDialogCreateBtn  (starts interactable = false)
 *      │           └── CancelBtn        → createDialogCancelBtn
 *      │
 *      ├── Loading Panel                → loadingPanel (INACTIVE)
 *      │     ├── LoadingLabelTMP        (static: "Creating restore point...")
 *      │     └── LoadingProgressBar     → loadingProgressBar
 *      │           (Slider — Interactable: false, Min: 0, Max: 1, Value: 0)
 *      │
 *      ├── Success Panel                → successPanel (INACTIVE)
 *      │     ├── SuccessTMP             (static: "The restore point was created successfully.")
 *      │     └── CloseBtn               → successCloseBtn
 *      │
 *      └── System Restore Wizard        → wizardPanel (INACTIVE)
 *            │
 *            ├── Phase 1 Panel          → wizardPhase1 (INACTIVE)
 *            │     ├── TitleTMP         (static: "Restore system files and settings")
 *            │     ├── DescTMP          (static: "System Restore can help fix problems that
 *            │     │                    might be making your computer run slowly or stop
 *            │     │                    responding. System Restore does not affect your
 *            │     │                    documents, pictures, or other personal data.")
 *            │     ├── RecommendedRestoreToggle  → recommendedRestoreToggle  (ToggleGroup)
 *            │     │     ├── Label: "Recommended restore:"
 *            │     │     └── RecommendedDetailsTMP  → recommendedDetailsTMP
 *            │     │           (dynamic: shows date + description of most recent point)
 *            │     ├── ChooseDifferentToggle     → chooseDifferentToggle  (ToggleGroup)
 *            │     │     Label: "Choose a different restore point"
 *            │     │     NOTE: Same ToggleGroup as RecommendedRestoreToggle.
 *            │     │           Add ToggleGroup to Phase 1 Panel, Allow Switch Off = false.
 *            │     └── Footer
 *            │           ├── BackBtn    → phase1BackBtn   (closes wizard, back to System Properties)
 *            │           ├── NextBtn    → phase1NextBtn
 *            │           └── CancelBtn  → phase1CancelBtn (closes wizard)
 *            │
 *            ├── Phase 2 Panel          → wizardPhase2 (INACTIVE)
 *            │     ├── TitleTMP         (static: "Select a restore point")
 *            │     ├── Column Headers Row
 *            │     │     ├── DateTimeHeaderTMP     (static: "Date and Time")
 *            │     │     ├── DescriptionHeaderTMP  (static: "Description")
 *            │     │     └── TypeHeaderTMP         (static: "Type")
 *            │     ├── Scroll View
 *            │     │     └── Content    → restorePointListContent  (Transform)
 *            │     └── Footer
 *            │           ├── BackBtn    → phase2BackBtn
 *            │           ├── NextBtn    → phase2NextBtn  (starts interactable = false)
 *            │           └── CancelBtn  → phase2CancelBtn (closes wizard)
 *            │
 *            ├── Phase 3 Panel          → wizardPhase3 (INACTIVE)
 *            │     ├── TitleTMP         (static: "Confirm your restore point")
 *            │     ├── ConfirmDescTMP   (static: "Your computer's system files and
 *            │     │                    settings will be restored to the state they were
 *            │     │                    at the time of the selected restore point.")
 *            │     ├── Phase3DateTimeTMP    → phase3DateTimeTMP    (dynamic)
 *            │     ├── Phase3DescriptionTMP → phase3DescriptionTMP (dynamic)
 *            │     ├── Phase3DriveTMP   (static: "Drives: Local Disk (C:)")
 *            │     └── Footer
 *            │           ├── BackBtn    → phase3BackBtn
 *            │           ├── FinishBtn  → phase3FinishBtn
 *            │           └── CancelBtn  → phase3CancelBtn (closes wizard)
 *            │
 *            └── Warning Dialog         → warningDialog (INACTIVE)
 *                  ├── WarningTMP       (static: "Once started, System Restore cannot
 *                  │                    be interrupted. Do you want to continue?")
 *                  ├── YesBtn           → warningYesBtn
 *                  └── NoBtn            → warningNoBtn
 *
 *  INSPECTOR ASSIGNMENTS
 *    restorePointRowPrefab   The RestorePointRowUI prefab (see RestorePointRowUI.cs)
 *    All other fields as listed above.
 *
 *  WIRING
 *    SearchWindowController.restorePointBtn OnClick
 *      → (handled in code via SearchWindowController.OpenRestorePoint())
 *    ServerVirtualOSManager.appPanelsRoot must include this GameObject
 *      as a direct child so CloseAllApps() deactivates it on restart.
 *
 *  HOW IT WORKS
 *    Opened by SearchWindowController when "Create a restore point" is clicked.
 *    Open() activates the root GO, shows the System Properties sub-panel.
 *    Protection is ON by default. Configure panel uses temp variables —
 *    changes only commit on OK, discarded on Cancel.
 *    Each created restore point adds 320 MB to the simulated current usage
 *    and appends a RestorePointEntry to the session list.
 *    System Restore wizard is enabled only when the list is non-empty.
 *    Completing the wizard (Finish → Yes in warning) triggers
 *    ServerVirtualOSManager.TriggerRestart() — visual-only restore.
 * ================================================================
 */

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SystemProtectionController : MonoBehaviour
{
    // ── Constants ─────────────────────────────────────────────────────────────

    private const float TotalDiskGB             = 50f;
    private const float UsagePerRestorePointMB  = 320f;
    private const float LoadingDuration         = 2f;

    // ── System Properties Panel ───────────────────────────────────────────────

    [Header("System Properties Panel")]
    [SerializeField] private GameObject systemPropertiesPanel;
    [SerializeField] private TMP_Text   protectionStatusTMP;
    [SerializeField] private Button     systemRestoreBtn;
    [SerializeField] private Button     configureBtn;
    [SerializeField] private Button     createBtn;
    [SerializeField] private Button     sysPropsOKBtn;
    [SerializeField] private Button     sysPropsCancelBtn;
    [SerializeField] private Button     sysPropsApplyBtn;

    // ── Config Panel ──────────────────────────────────────────────────────────

    [Header("Config Panel")]
    [SerializeField] private GameObject configPanel;
    [SerializeField] private Toggle     turnOnToggle;
    [SerializeField] private Toggle     disableToggle;
    [SerializeField] private TMP_Text   currentUsageTMP;
    [SerializeField] private Slider     maxUsageSlider;
    [SerializeField] private TMP_Text   maxUsageValueTMP;
    [SerializeField] private Button     configDeleteBtn;
    [SerializeField] private Button     configOKBtn;
    [SerializeField] private Button     configCancelBtn;
    [SerializeField] private Button     configApplyBtn;

    // ── Create Dialog Panel ───────────────────────────────────────────────────

    [Header("Create Dialog Panel")]
    [SerializeField] private GameObject     createDialogPanel;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button         createDialogCreateBtn;
    [SerializeField] private Button         createDialogCancelBtn;

    // ── Loading Panel ─────────────────────────────────────────────────────────

    [Header("Loading Panel")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider     loadingProgressBar;

    // ── Success Panel ─────────────────────────────────────────────────────────

    [Header("Success Panel")]
    [SerializeField] private GameObject successPanel;
    [SerializeField] private Button     successCloseBtn;

    // ── Wizard ────────────────────────────────────────────────────────────────

    [Header("System Restore Wizard")]
    [SerializeField] private GameObject wizardPanel;

    [Header("  Phase 1")]
    [SerializeField] private GameObject wizardPhase1;
    [SerializeField] private Toggle     recommendedRestoreToggle;
    [SerializeField] private Toggle     chooseDifferentToggle;
    [SerializeField] private TMP_Text   recommendedDetailsTMP;
    [SerializeField] private Button     phase1BackBtn;
    [SerializeField] private Button     phase1NextBtn;
    [SerializeField] private Button     phase1CancelBtn;

    [Header("  Phase 2")]
    [SerializeField] private GameObject wizardPhase2;
    [SerializeField] private Transform  restorePointListContent;
    [SerializeField] private Button     phase2BackBtn;
    [SerializeField] private Button     phase2NextBtn;
    [SerializeField] private Button     phase2CancelBtn;

    [Header("  Phase 3")]
    [SerializeField] private GameObject wizardPhase3;
    [SerializeField] private TMP_Text   phase3DateTimeTMP;
    [SerializeField] private TMP_Text   phase3DescriptionTMP;
    [SerializeField] private Button     phase3BackBtn;
    [SerializeField] private Button     phase3FinishBtn;
    [SerializeField] private Button     phase3CancelBtn;

    [Header("  Warning Dialog")]
    [SerializeField] private GameObject warningDialog;
    [SerializeField] private Button     warningYesBtn;
    [SerializeField] private Button     warningNoBtn;

    // ── Prefab ────────────────────────────────────────────────────────────────

    [Header("Prefab")]
    [SerializeField] private GameObject restorePointRowPrefab;

    // ── Runtime State ─────────────────────────────────────────────────────────

    private bool  _protectionEnabled = true;
    private float _maxUsagePercent   = 0.05f;
    private float _currentUsageMB    = 0f;

    private readonly List<RestorePointEntry> _restorePoints = new List<RestorePointEntry>();
    private readonly List<RestorePointRowUI> _spawnedRows   = new List<RestorePointRowUI>();

    private RestorePointEntry _selectedEntry;
    private RestorePointRowUI _selectedRow;
    private bool              _cameFromRecommended;

    // Temp state for Config panel — committed on OK, discarded on Cancel
    private float _tempMaxUsagePercent;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        // System Properties
        sysPropsOKBtn?.onClick.AddListener(CloseChain);
        sysPropsCancelBtn?.onClick.AddListener(CloseChain);
        sysPropsApplyBtn?.onClick.AddListener(() => { });
        configureBtn?.onClick.AddListener(OpenConfigPanel);
        createBtn?.onClick.AddListener(OpenCreateDialog);
        systemRestoreBtn?.onClick.AddListener(OpenWizard);

        // Config Panel
        maxUsageSlider?.onValueChanged.AddListener(OnMaxUsageSliderChanged);
        configDeleteBtn?.onClick.AddListener(DeleteAllRestorePoints);
        configOKBtn?.onClick.AddListener(CommitConfig);
        configCancelBtn?.onClick.AddListener(CloseConfigPanel);
        configApplyBtn?.onClick.AddListener(() => { });

        // Create Dialog
        nameInput?.onValueChanged.AddListener(v =>
            SetInteractable(createDialogCreateBtn, !string.IsNullOrWhiteSpace(v)));
        createDialogCreateBtn?.onClick.AddListener(OnCreateClicked);
        createDialogCancelBtn?.onClick.AddListener(CloseCreateDialog);

        // Success Panel
        successCloseBtn?.onClick.AddListener(CloseSuccessPanel);

        // Wizard — Phase 1
        phase1BackBtn?.onClick.AddListener(CloseWizard);
        phase1CancelBtn?.onClick.AddListener(CloseWizard);
        phase1NextBtn?.onClick.AddListener(OnPhase1Next);
        recommendedRestoreToggle?.onValueChanged.AddListener(_ => RefreshPhase1Next());
        chooseDifferentToggle?.onValueChanged.AddListener(_ => RefreshPhase1Next());

        // Wizard — Phase 2
        phase2BackBtn?.onClick.AddListener(ShowPhase1);
        phase2CancelBtn?.onClick.AddListener(CloseWizard);
        phase2NextBtn?.onClick.AddListener(OnPhase2Next);

        // Wizard — Phase 3
        phase3BackBtn?.onClick.AddListener(OnPhase3Back);
        phase3CancelBtn?.onClick.AddListener(CloseWizard);
        phase3FinishBtn?.onClick.AddListener(OnPhase3Finish);

        // Warning Dialog
        warningYesBtn?.onClick.AddListener(OnWarningYes);
        warningNoBtn?.onClick.AddListener(OnWarningNo);

        HideAll();
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        HideAll();
        RefreshSystemPropertiesPanel();
        systemPropertiesPanel?.SetActive(true);
        ActivityLogManager.Log("Opened System Properties (System Protection)", ActivityLogManager.EntryType.Action);
    }

    // ── System Properties Panel ───────────────────────────────────────────────

    private void RefreshSystemPropertiesPanel()
    {
        if (protectionStatusTMP != null)
            protectionStatusTMP.text = _protectionEnabled ? "On" : "Off";

        SetInteractable(createBtn,        _protectionEnabled);
        SetInteractable(systemRestoreBtn, _restorePoints.Count > 0);
    }

    private void CloseChain()
    {
        HideAll();
        gameObject.SetActive(false);
    }

    // ── Config Panel ──────────────────────────────────────────────────────────

    private void OpenConfigPanel()
    {
        // Snapshot current state into temp — changes are NOT applied until OK
        _tempMaxUsagePercent = _maxUsagePercent;

        turnOnToggle?.SetIsOnWithoutNotify(_protectionEnabled);
        disableToggle?.SetIsOnWithoutNotify(!_protectionEnabled);

        if (maxUsageSlider != null)
            maxUsageSlider.SetValueWithoutNotify(_tempMaxUsagePercent);

        RefreshMaxUsageDisplay(_tempMaxUsagePercent);
        RefreshCurrentUsageDisplay();

        systemPropertiesPanel?.SetActive(false);
        configPanel?.SetActive(true);
    }

    private void OnMaxUsageSliderChanged(float value)
    {
        _tempMaxUsagePercent = value;
        RefreshMaxUsageDisplay(value);
    }

    private void RefreshMaxUsageDisplay(float percent)
    {
        if (maxUsageValueTMP == null) return;
        float gb = TotalDiskGB * percent;
        maxUsageValueTMP.text = $"{percent * 100f:F0}% ({gb:F2} GB)";
    }

    private void RefreshCurrentUsageDisplay()
    {
        if (currentUsageTMP == null) return;
        currentUsageTMP.text = _currentUsageMB >= 1024f
            ? $"{_currentUsageMB / 1024f:F2} GB"
            : $"{_currentUsageMB:F2} MB";
    }

    private void DeleteAllRestorePoints()
    {
        _restorePoints.Clear();
        _currentUsageMB = 0f;
        RefreshCurrentUsageDisplay();
        ActivityLogManager.Log("All restore points deleted.", ActivityLogManager.EntryType.Action);
    }

    private void CommitConfig()
    {
        _protectionEnabled = turnOnToggle != null && turnOnToggle.isOn;
        _maxUsagePercent   = _tempMaxUsagePercent;
        CloseConfigPanel();
        RefreshSystemPropertiesPanel();
        ActivityLogManager.Log(
            $"System protection {(_protectionEnabled ? "enabled" : "disabled")}. Max usage: {_maxUsagePercent * 100f:F0}%",
            ActivityLogManager.EntryType.Action);
    }

    private void CloseConfigPanel()
    {
        configPanel?.SetActive(false);
        systemPropertiesPanel?.SetActive(true);
    }

    // ── Create Dialog ─────────────────────────────────────────────────────────

    private void OpenCreateDialog()
    {
        if (nameInput != null) nameInput.text = "";
        SetInteractable(createDialogCreateBtn, false);
        systemPropertiesPanel?.SetActive(false);
        createDialogPanel?.SetActive(true);
    }

    private void OnCreateClicked()
    {
        string name = nameInput != null ? nameInput.text.Trim() : "Restore Point";
        createDialogPanel?.SetActive(false);
        loadingPanel?.SetActive(true);
        if (loadingProgressBar != null) loadingProgressBar.value = 0f;
        StartCoroutine(RunLoadingBar(name));
    }

    private IEnumerator RunLoadingBar(string entryName)
    {
        float elapsed = 0f;
        while (elapsed < LoadingDuration)
        {
            elapsed += Time.deltaTime;
            if (loadingProgressBar != null)
                loadingProgressBar.value = Mathf.Clamp01(elapsed / LoadingDuration);
            yield return null;
        }

        _currentUsageMB += UsagePerRestorePointMB;
        _restorePoints.Add(new RestorePointEntry
        {
            Name     = entryName,
            DateTime = System.DateTime.Now.ToString("M/d/yyyy h:mm:ss tt"),
            Type     = "Manual"
        });

        loadingPanel?.SetActive(false);
        successPanel?.SetActive(true);
        ActivityLogManager.Log($"Restore point created: \"{entryName}\"", ActivityLogManager.EntryType.Action);
    }

    private void CloseCreateDialog()
    {
        createDialogPanel?.SetActive(false);
        systemPropertiesPanel?.SetActive(true);
    }

    private void CloseSuccessPanel()
    {
        successPanel?.SetActive(false);
        RefreshSystemPropertiesPanel();
        systemPropertiesPanel?.SetActive(true);
    }

    // ── System Restore Wizard ─────────────────────────────────────────────────

    private void OpenWizard()
    {
        systemPropertiesPanel?.SetActive(false);
        wizardPanel?.SetActive(true);
        ShowPhase1();
    }

    private void CloseWizard()
    {
        wizardPanel?.SetActive(false);
        CleanupPhase2Rows();
        _selectedEntry = null;
        _selectedRow   = null;
        RefreshSystemPropertiesPanel();
        systemPropertiesPanel?.SetActive(true);
    }

    // Phase 1 ─────────────────────────────────────────────────────────────────

    private void ShowPhase1()
    {
        wizardPhase2?.SetActive(false);
        wizardPhase3?.SetActive(false);
        warningDialog?.SetActive(false);

        RestorePointEntry latest = _restorePoints.Count > 0
            ? _restorePoints[_restorePoints.Count - 1] : null;

        if (recommendedDetailsTMP != null && latest != null)
            recommendedDetailsTMP.text = $"Date: {latest.DateTime}    Description: {latest.Name}";

        // Default Phase 1 to "Recommended restore" selected
        recommendedRestoreToggle?.SetIsOnWithoutNotify(true);
        chooseDifferentToggle?.SetIsOnWithoutNotify(false);

        RefreshPhase1Next();
        wizardPhase1?.SetActive(true);
    }

    private void RefreshPhase1Next()
    {
        bool hasSelection = (recommendedRestoreToggle != null && recommendedRestoreToggle.isOn)
                         || (chooseDifferentToggle    != null && chooseDifferentToggle.isOn);
        SetInteractable(phase1NextBtn, hasSelection);
    }

    private void OnPhase1Next()
    {
        if (recommendedRestoreToggle != null && recommendedRestoreToggle.isOn)
        {
            _cameFromRecommended = true;
            _selectedEntry = _restorePoints[_restorePoints.Count - 1];
            wizardPhase1?.SetActive(false);
            ShowPhase3();
        }
        else
        {
            _cameFromRecommended = false;
            wizardPhase1?.SetActive(false);
            ShowPhase2();
        }
    }

    // Phase 2 ─────────────────────────────────────────────────────────────────

    private void ShowPhase2()
    {
        CleanupPhase2Rows();

        foreach (RestorePointEntry entry in _restorePoints)
        {
            GameObject rowGO = Instantiate(restorePointRowPrefab, restorePointListContent);
            RestorePointRowUI row = rowGO.GetComponent<RestorePointRowUI>();
            if (row != null)
            {
                row.Setup(entry, OnRowSelected);
                _spawnedRows.Add(row);
            }
        }

        _selectedEntry = null;
        _selectedRow   = null;
        SetInteractable(phase2NextBtn, false);
        wizardPhase2?.SetActive(true);
    }

    private void OnRowSelected(RestorePointRowUI row)
    {
        _selectedRow?.SetSelected(false);
        _selectedRow   = row;
        _selectedEntry = row.Entry;
        _selectedRow.SetSelected(true);
        SetInteractable(phase2NextBtn, true);
    }

    private void OnPhase2Next()
    {
        if (_selectedEntry == null) return;
        wizardPhase2?.SetActive(false);
        ShowPhase3();
    }

    private void CleanupPhase2Rows()
    {
        foreach (RestorePointRowUI row in _spawnedRows)
            if (row != null) Destroy(row.gameObject);
        _spawnedRows.Clear();
    }

    // Phase 3 ─────────────────────────────────────────────────────────────────

    private void ShowPhase3()
    {
        if (phase3DateTimeTMP    != null && _selectedEntry != null)
            phase3DateTimeTMP.text    = _selectedEntry.DateTime;
        if (phase3DescriptionTMP != null && _selectedEntry != null)
            phase3DescriptionTMP.text = _selectedEntry.Name;

        wizardPhase3?.SetActive(true);
    }

    private void OnPhase3Back()
    {
        wizardPhase3?.SetActive(false);
        if (_cameFromRecommended)
            ShowPhase1();
        else
            ShowPhase2();
    }

    private void OnPhase3Finish()
    {
        wizardPhase3?.SetActive(false);
        warningDialog?.SetActive(true);
    }

    // Warning Dialog ──────────────────────────────────────────────────────────

    private void OnWarningYes()
    {
        CleanupPhase2Rows();
        HideAll();
        gameObject.SetActive(false);
        ActivityLogManager.Log("System Restore initiated — restarting.", ActivityLogManager.EntryType.Action);
        ServerVirtualOSManager.Instance?.TriggerRestart();
    }

    private void OnWarningNo()
    {
        warningDialog?.SetActive(false);
        wizardPhase3?.SetActive(true);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SetInteractable(Button btn, bool interactable)
    {
        if (btn != null) btn.interactable = interactable;
    }

    private void HideAll()
    {
        systemPropertiesPanel?.SetActive(false);
        configPanel?.SetActive(false);
        createDialogPanel?.SetActive(false);
        loadingPanel?.SetActive(false);
        successPanel?.SetActive(false);
        wizardPanel?.SetActive(false);
    }
}
