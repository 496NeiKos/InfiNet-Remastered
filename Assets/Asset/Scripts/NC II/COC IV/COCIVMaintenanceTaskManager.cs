/*
 * ================================================================
 *  UNITY SETUP GUIDE — COCIVMaintenanceTaskManager (COC IV)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to an always-active "Managers" GameObject in the COC IV scene.
 *
 *  ── HIERARCHY ────────────────────────────────────────────────────
 *
 *    Task Panel
 *      ├── Group Title Row                   (always visible above task list)
 *      │     ├── GroupTitleTMP               TMP_Text  → groupTitleTMP
 *      │     │     Text auto-set: "Disk Cleanup  2 / 7"
 *      │     └── Button component            Button  → groupTitleBtn
 *      │           OnClick → opens Group Overview Popup
 *      │
 *      ├── Task List                         (active task rows parented here)
 *      │     └── [taskObjects[0..32] reparented here at runtime]
 *      │
 *      └── Finished Bucket                  (off-screen; latched tasks move here)
 *
 *    Group Overview Popup                   → groupOverviewPopup (INACTIVE)
 *      ├── Header / Title TMP               (static "Task Groups" label, optional)
 *      ├── Row Parent                       → groupRowParent  (VerticalLayoutGroup)
 *      │     └── [GroupRowPrefab × 6 spawned here at runtime]
 *      └── CloseBtn                         Button  → closePopupBtn
 *
 *  ── GROUP ROW PREFAB ─────────────────────────────────────────────
 *    Root: any layout container (e.g. HorizontalLayoutGroup)
 *      ├── NameTMP      TMP_Text  [0] — group title  (left-aligned)
 *      ├── ProgressTMP  TMP_Text  [1] — "2 / 7"      (center-aligned)
 *      └── StatusTMP    TMP_Text  [2] — "Pending" / "Active" / "Done"
 *    GetComponentsInChildren<TMP_Text>() must return them in order [0][1][2].
 *
 *  ── TASK GROUPS — 6 groups, 33 tasks total ───────────────────────
 *    Group 0  "System Protection — Create Restore Point"      tasks  0 –  5
 *    Group 1  "System Protection — Built-in System Restore"   tasks  6 –  9
 *    Group 2  "Disk Cleanup"                                  tasks 10 – 16
 *    Group 3  "Defragment and Optimize Drives"                tasks 17 – 20
 *    Group 4  "Windows Security"                              tasks 21 – 25
 *    Group 5  "System Restore via Run Dialog"                 tasks 26 – 32
 *
 *  ── INSPECTOR ASSIGNMENTS ────────────────────────────────────────
 *    taskParent           → VerticalLayoutGroup parent for active task rows
 *    finishedParent       → Off-screen parent for completed task GameObjects
 *    taskObjects[0..32]   → Task row GameObjects (TMP_Text labels)
 *    allTasksCompletedText→ (optional) GameObject shown when all tasks done
 *
 *    groupTitleTMP        → TMP_Text above the task list
 *    groupTitleBtn        → Button on / wrapping the group title TMP
 *    flashColor           → Color flashed on group complete (default green)
 *    flashDuration        → Seconds the flash lasts (default 1)
 *    cocLabel             → e.g. "COC IV" — used in completion message
 *
 *    groupOverviewPopup   → Popup panel GameObject (starts INACTIVE)
 *    groupRowParent       → Transform inside popup where rows are spawned
 *    groupRowPrefab       → Prefab for one popup row (3 TMP children)
 *    closePopupBtn        → Button to close the popup
 *    groupPendingColor    → Row color for groups not yet started
 *    groupActiveColor     → Row color for the current active group
 *    groupDoneColor       → Row color for completed groups
 *
 *    taskGroups[]         → TaskGroup array (6 entries, configure in Inspector)
 *    computerHolder       → NetworkHardwareHolder for the COC IV computer
 *
 *  ── PATTERN ──────────────────────────────────────────────────────
 *    Latch-based: once a task condition is true it latches permanently.
 *    Sliding window: up to 3 unlatched tasks shown at a time, capped to
 *    the current active group — tasks from the next group never bleed in.
 *    One active group at a time; group advances only after all its tasks
 *    are latched (after a 1-second green flash on the group title TMP).
 * ================================================================
 */

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class COCIVMaintenanceTaskManager : MonoBehaviour
{
    public static COCIVMaintenanceTaskManager Instance { get; private set; }

    private const int TASK_COUNT = 33;

    // ── Task UI ───────────────────────────────────────────────────────────────

    [Header("Task UI")]
    [SerializeField] private Transform    taskParent;
    [SerializeField] private Transform    finishedParent;
    [SerializeField] private GameObject[] taskObjects = new GameObject[TASK_COUNT];
    [SerializeField] private GameObject   allTasksCompletedText;

    // ── Group Title ───────────────────────────────────────────────────────────

    [Header("Group Title")]
    [SerializeField] private TMP_Text groupTitleTMP;
    [SerializeField] private Button   groupTitleBtn;
    [SerializeField] private Color    flashColor    = Color.green;
    [SerializeField] private float    flashDuration = 1f;
    [SerializeField] private string   cocLabel      = "COC IV";

    // ── Group Overview Popup ──────────────────────────────────────────────────

    [Header("Group Overview Popup")]
    [SerializeField] private GameObject groupOverviewPopup;
    [SerializeField] private Transform  groupRowParent;
    [SerializeField] private GameObject groupRowPrefab;
    [SerializeField] private Button     closePopupBtn;
    [SerializeField] private Color      groupPendingColor = Color.gray;
    [SerializeField] private Color      groupActiveColor  = Color.yellow;
    [SerializeField] private Color      groupDoneColor    = Color.green;

    // ── Task Groups ───────────────────────────────────────────────────────────

    [Header("Task Groups (configure all entries in Inspector)")]
    [SerializeField] private TaskGroup[] taskGroups;

    // ── Hardware ──────────────────────────────────────────────────────────────

    [Header("Hardware")]
    [SerializeField] private NetworkHardwareHolder computerHolder;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private bool[]       _latched           = new bool[TASK_COUNT];
    private int          _currentTask       = 0;
    private int          _currentGroupIndex = 0;
    private bool         _allDone           = false;
    private bool         _isFlashing        = false;
    private TMP_Text[][] _popupRowTMPs;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        groupTitleBtn?.onClick.AddListener(OpenPopup);
        closePopupBtn?.onClick.AddListener(ClosePopup);
    }

    private void Start()
    {
        groupOverviewPopup?.SetActive(false);
        BuildPopupRows();
        RefreshGroupTitle();
        RefreshPopup();
        RefreshWindow();
    }

    private void Update()
    {
        if (_allDone) return;
        CheckCurrentTask();
    }

    // ── Task condition checks ─────────────────────────────────────────────────

    private bool CheckCondition(int index)
    {
        return index switch
        {
            // ── Group 0: System Protection — Create Restore Point ─────────────
             0 => computerHolder != null && computerHolder.hardwarePrefab != null && computerHolder.hardwarePrefab.activeSelf,
             1 => ServerVirtualOSManager.Instance != null
                  && ServerVirtualOSManager.Instance.IsOpen
                  && !string.IsNullOrEmpty(ServerVirtualOSManager.Instance.ServerState?.CurrentLoggedInUser),
             2 => SystemProtectionController.Instance != null && SystemProtectionController.Instance.gameObject.activeSelf,
             3 => SystemProtectionController.Instance?.ConfigureOKClicked == true,
             4 => SystemProtectionController.Instance?.CreateClicked == true,
             5 => SystemProtectionController.Instance?.SuccessPanelClosed == true,

            // ── Group 1: System Protection — Built-in System Restore ──────────
             6 => SystemProtectionController.Instance?.SPCChooseDifferentOn == true,
             7 => SystemProtectionController.Instance?.SPCPhase2NextClicked == true,
             8 => SystemProtectionController.Instance?.SPCFinishClicked == true,
             9 => SystemProtectionController.Instance?.SPCRestartTriggered == true,

            // ── Group 2: Disk Cleanup ─────────────────────────────────────────
            10 => DiskCleanupController.Instance != null && DiskCleanupController.Instance.gameObject.activeSelf,
            11 => DiskCleanupController.Instance?.DriveSelectedAndOKClicked == true,
            12 => DiskCleanupController.Instance?.CleanupSystemFilesClicked == true,
            13 => DiskCleanupController.Instance?.SecondPassDriveOKClicked == true,
            14 => DiskCleanupController.Instance?.SystemAnalysisComplete == true,
            15 => DiskCleanupController.Instance?.DeleteFilesClicked == true,
            16 => DiskCleanupController.Instance?.CleanupFinished == true,

            // ── Group 3: Defragment and Optimize Drives ───────────────────────
            17 => DefragController.Instance != null && DefragController.Instance.gameObject.activeSelf,
            18 => DefragController.Instance?.AnalyzeStarted == true,
            19 => DefragController.Instance?.OptimizeStarted == true,
            20 => DefragController.Instance?.OptimizeCompleted == true && DefragController.Instance.gameObject.activeSelf == false,

            // ── Group 4: Windows Security ─────────────────────────────────────
            21 => WindowsSecurityController.Instance != null && WindowsSecurityController.Instance.gameObject.activeSelf,
            22 => WindowsSecurityController.Instance?.VTPSectionOpened == true,
            23 => WindowsSecurityController.Instance?.QuickScanStarted == true,
            24 => WindowsSecurityController.Instance?.QuickScanCompleted == true,
            25 => WindowsSecurityController.Instance?.ToastDismissed == true && WindowsSecurityController.Instance.gameObject.activeSelf == false,

            // ── Group 5: System Restore via Run Dialog ────────────────────────
            26 => SystemRestoreController.Instance?.RunPanelOpen == true,
            27 => SystemRestoreController.Instance?.Panel1Open == true,
            28 => SystemRestoreController.Instance?.SRPanel2Shown == true,
            29 => SystemRestoreController.Instance?.ShowMoreToggled == true && SystemRestoreController.Instance?.ScanAffectedClicked == true,
            30 => SystemRestoreController.Instance?.SRConfirmShown == true,
            31 => SystemRestoreController.Instance?.SRFinishClicked == true,
            32 => SystemRestoreController.Instance?.SRRestartTriggered == true,

            _ => false
        };
    }

    // ── Latch & window logic ──────────────────────────────────────────────────

    private void CheckCurrentTask()
    {
        while (_currentTask < TASK_COUNT && !_allDone)
        {
            if (_latched[_currentTask]) { AdvanceIfPossible(); continue; }
            if (!CheckCondition(_currentTask)) return;
            LatchTask(_currentTask);
        }
    }

    private void LatchTask(int index)
    {
        _latched[index] = true;
        ActivityLogManager.Log($"Task completed: {GetTaskLabel(index)}", ActivityLogManager.EntryType.Action);

        if (taskObjects[index] != null && finishedParent != null)
            taskObjects[index].transform.SetParent(finishedParent, false);

        AdvanceIfPossible();
        OnTasksUpdated?.Invoke();
        RefreshWindow();
        RefreshGroupTitle();

        if (groupOverviewPopup != null && groupOverviewPopup.activeSelf)
            RefreshPopup();

        if (!_isFlashing)
            CheckGroupCompletion();
    }

    private void AdvanceIfPossible()
    {
        while (_currentTask < TASK_COUNT && _latched[_currentTask])
            _currentTask++;

        if (_currentTask >= TASK_COUNT)
        {
            _allDone = true;
            if (allTasksCompletedText != null) allTasksCompletedText.SetActive(true);
            Debug.Log("[COCIVMaintenanceTaskManager] All tasks completed.");
        }
    }

    private void RefreshWindow()
    {
        if (taskGroups == null || taskGroups.Length == 0)
        {
            int shown = 0;
            for (int i = _currentTask; i < TASK_COUNT && shown < 3; i++)
            {
                if (_latched[i]) continue;
                if (taskObjects[i] != null)
                {
                    taskObjects[i].transform.SetParent(taskParent, false);
                    taskObjects[i].SetActive(true);
                    shown++;
                }
            }
            return;
        }

        int groupEnd  = GetCurrentGroupLastTask();
        int displayed = 0;
        for (int i = _currentTask; i <= groupEnd && displayed < 3; i++)
        {
            if (_latched[i]) continue;
            if (taskObjects[i] != null)
            {
                taskObjects[i].transform.SetParent(taskParent, false);
                taskObjects[i].SetActive(true);
                displayed++;
            }
        }
    }

    // ── Group logic ───────────────────────────────────────────────────────────

    private int GetCurrentGroupLastTask()
    {
        if (taskGroups == null || _currentGroupIndex >= taskGroups.Length)
            return TASK_COUNT - 1;
        var g = taskGroups[_currentGroupIndex];
        return Mathf.Min(g.FirstTask + g.TaskCount - 1, TASK_COUNT - 1);
    }

    private int CountCompletedInGroup(int groupIndex)
    {
        if (taskGroups == null || groupIndex >= taskGroups.Length) return 0;
        var g    = taskGroups[groupIndex];
        int last = Mathf.Min(g.FirstTask + g.TaskCount - 1, TASK_COUNT - 1);
        int count = 0;
        for (int i = g.FirstTask; i <= last; i++)
            if (_latched[i]) count++;
        return count;
    }

    private void CheckGroupCompletion()
    {
        if (taskGroups == null || _currentGroupIndex >= taskGroups.Length) return;
        var g = taskGroups[_currentGroupIndex];
        if (CountCompletedInGroup(_currentGroupIndex) >= g.TaskCount)
            StartCoroutine(FlashAndAdvance());
    }

    private IEnumerator FlashAndAdvance()
    {
        _isFlashing = true;

        Color original = groupTitleTMP != null ? groupTitleTMP.color : Color.white;
        if (groupTitleTMP != null) groupTitleTMP.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        if (groupTitleTMP != null) groupTitleTMP.color = original;
        _isFlashing = false;

        if (_allDone)
        {
            if (groupTitleTMP != null)
                groupTitleTMP.text = $"Objective in {cocLabel} is met, all tasks have been completed.";
        }
        else
        {
            _currentGroupIndex++;
            RefreshGroupTitle();
            RefreshWindow();
        }

        if (groupOverviewPopup != null && groupOverviewPopup.activeSelf)
            RefreshPopup();
    }

    private void RefreshGroupTitle()
    {
        if (groupTitleTMP == null || taskGroups == null || _currentGroupIndex >= taskGroups.Length) return;
        var g         = taskGroups[_currentGroupIndex];
        int completed = CountCompletedInGroup(_currentGroupIndex);
        groupTitleTMP.text = $"{g.Title}  {completed} / {g.TaskCount}";
    }

    // ── Group Overview Popup ──────────────────────────────────────────────────

    private void OpenPopup()
    {
        RefreshPopup();
        groupOverviewPopup?.SetActive(true);
    }

    private void ClosePopup()
    {
        groupOverviewPopup?.SetActive(false);
    }

    private void BuildPopupRows()
    {
        if (groupRowParent == null || groupRowPrefab == null || taskGroups == null) return;

        foreach (Transform child in groupRowParent)
            Destroy(child.gameObject);

        _popupRowTMPs = new TMP_Text[taskGroups.Length][];
        for (int g = 0; g < taskGroups.Length; g++)
        {
            var row  = Instantiate(groupRowPrefab, groupRowParent);
            var tmps = row.GetComponentsInChildren<TMP_Text>();
            _popupRowTMPs[g] = new TMP_Text[3];
            if (tmps.Length >= 3)
            {
                _popupRowTMPs[g][0] = tmps[0]; // group name
                _popupRowTMPs[g][1] = tmps[1]; // progress
                _popupRowTMPs[g][2] = tmps[2]; // status
            }
        }
    }

    private void RefreshPopup()
    {
        if (_popupRowTMPs == null || taskGroups == null) return;

        for (int g = 0; g < taskGroups.Length; g++)
        {
            if (_popupRowTMPs[g] == null) continue;

            var   group     = taskGroups[g];
            int   completed = CountCompletedInGroup(g);
            bool  isDone    = completed >= group.TaskCount;

            string statusText;
            Color  rowColor;

            if (isDone)
            {
                statusText = "Done";
                rowColor   = groupDoneColor;
            }
            else if (g == _currentGroupIndex)
            {
                statusText = "Active";
                rowColor   = groupActiveColor;
            }
            else
            {
                statusText = "Pending";
                rowColor   = groupPendingColor;
            }

            if (_popupRowTMPs[g][0] != null) { _popupRowTMPs[g][0].text = group.Title;                        _popupRowTMPs[g][0].color = rowColor; }
            if (_popupRowTMPs[g][1] != null) { _popupRowTMPs[g][1].text = $"{completed} / {group.TaskCount}"; _popupRowTMPs[g][1].color = rowColor; }
            if (_popupRowTMPs[g][2] != null) { _popupRowTMPs[g][2].text = statusText;                         _popupRowTMPs[g][2].color = rowColor; }
        }
    }

    // ── Task labels ───────────────────────────────────────────────────────────

    private static string GetTaskLabel(int index)
    {
        return index switch
        {
            // ── Group 0 ───────────────────────────────────────────────────────
             0 => "Deploy the computer to the workspace.",
             1 => "Enter the Virtual OS and login to access the desktop.",
             2 => "Search \"Restore Point\" and open System Protection.",
             3 => "Click Configure, adjust settings, then hit OK.",
             4 => "Click Create, name the restore point, then hit Create.",
             5 => "Wait for the restore point to be created, then close the success panel.",

            // ── Group 1 ───────────────────────────────────────────────────────
             6 => "Click System Restore button, toggle \"Choose a different restore point\".",
             7 => "Select a restore point from the list, then hit Next.",
             8 => "Read to confirm your selected restore point, then hit Finish.",
             9 => "Wait till the computer boots to load the selected restore point.",

            // ── Group 2 ───────────────────────────────────────────────────────
            10 => "Log in and search \"Disk Cleanup\", then open the application.",
            11 => "Select the drive you want to clean up, then hit OK.",
            12 => "Wait for the scan — select files in the list, then click \"Clean up system files\".",
            13 => "Choose the drive again, then hit OK.",
            14 => "Wait for the scan to complete.",
            15 => "Press OK then click \"Delete Files\" in the confirmation pop-up.",
            16 => "Wait for the file cleanup to finish.",

            // ── Group 3 ───────────────────────────────────────────────────────
            17 => "Search \"Defragment and Optimize Drives\", then open the application.",
            18 => "Select a drive in the list, then hit Analyze.",
            19 => "Press Optimize to start defragging and optimizing the drive.",
            20 => "Wait for it to finish, then close the application.",

            // ── Group 4 ───────────────────────────────────────────────────────
            21 => "Search \"Windows Security\", then open the application.",
            22 => "Navigate to \"Virus & threat protection\".",
            23 => "Read the current state of the computer, then do a Quick Scan.",
            24 => "Observe the scan procedure and wait for it to finish.",
            25 => "Close the scan pop-up below, then close the application.",

            // ── Group 5 ───────────────────────────────────────────────────────
            26 => "Search \"Run\", then open the Run dialog.",
            27 => "Type \"rstrui.exe\", then open the System Restore application.",
            28 => "Toggle \"Choose a different restore point\", then hit Next.",
            29 => "Browse the restore point list — toggle Show More and click Scan.",
            30 => "Select a restore point you want, then hit Next.",
            31 => "Read to confirm your selected restore point, then hit Finish.",
            32 => "Wait for the process to complete and the computer to boot.",

            _  => $"Task {index + 1}"
        };
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public static event System.Action OnTasksUpdated;

    public string GetCurrentTaskLabel()
    {
        if (_allDone || _currentTask >= TASK_COUNT) return null;
        return GetTaskLabel(_currentTask);
    }

    public string GetNextIncompleteTaskText() => GetCurrentTaskLabel();

    public void NotifyStateChanged() { }
}
