/*
 * ================================================================
 *  UNITY SETUP GUIDE — COCIVMaintenanceTaskManager (COC IV)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to an always-active "Managers" GameObject in the COC IV scene.
 *
 *  TASK LIST (33 tasks, 0–32)
 *  ── System Protection — Create Restore Point ───────────────────
 *  [ 0] Deploy the computer to the workspace
 *  [ 1] Enter the Virtual OS and login to access the desktop
 *  [ 2] Search "Restore Point" and open System Protection
 *  [ 3] Click Configure, adjust settings, then hit OK
 *  [ 4] Click Create, name the restore point, then hit Create
 *  [ 5] Wait for the restore point to be created, then close the success panel
 *  ── System Protection — Built-in System Restore ─────────────────
 *  [ 6] Click System Restore button, toggle "Choose a different restore point"
 *  [ 7] Select a restore point from the list, then hit Next
 *  [ 8] Read to confirm your selected restore point, then hit Finish
 *  [ 9] Wait till the computer boots to load the selected restore point
 *  ── Disk Cleanup ────────────────────────────────────────────────
 *  [10] Log in and search "Disk Cleanup", then open the application
 *  [11] Select the drive you want to clean up, then hit OK
 *  [12] Wait for the scan — select files in the list, then click "Clean up system files"
 *  [13] Choose the drive again, then hit OK
 *  [14] Wait for the scan to complete
 *  [15] Press OK then click "Delete Files" in the confirmation pop-up
 *  [16] Wait for the file cleanup to finish
 *  ── Defragment and Optimize Drives ──────────────────────────────
 *  [17] Search "Defragment and Optimize Drives", then open the application
 *  [18] Select a drive in the list, then hit Analyze
 *  [19] Press Optimize to start defragging and optimizing the drive
 *  [20] Wait for it to finish, then close the application
 *  ── Windows Security ────────────────────────────────────────────
 *  [21] Search "Windows Security", then open the application
 *  [22] Navigate to "Virus & threat protection"
 *  [23] Read the current state of the computer, then do a Quick Scan
 *  [24] Observe the scan procedure and wait for it to finish
 *  [25] Close the scan pop-up below, then close the application
 *  ── System Restore via Run Dialog ───────────────────────────────
 *  [26] Search "Run", then open the Run dialog
 *  [27] Type "rstrui.exe", then open the System Restore application
 *  [28] Toggle "Choose a different restore point", then hit Next
 *  [29] Browse the restore point list — toggle Show More and click Scan
 *  [30] Select a restore point you want, then hit Next
 *  [31] Read to confirm your selected restore point, then hit Finish
 *  [32] Wait for the process to complete and the computer to boot
 *
 *  INSPECTOR SETUP
 *    taskParent            → VerticalLayoutGroup parent for active task rows
 *    finishedParent        → Off-screen parent for completed task GameObjects
 *    taskObjects[0..32]    → 33 task row GameObjects (TMP_Text labels)
 *    computerHolder        → NetworkHardwareHolder on the COC IV computer icon
 *    allTasksCompletedText → (optional) TMP shown when all tasks done
 *
 *  PATTERN
 *    Same latch-based sliding-window pattern as ServerSetupTaskManager.
 *    Each task is checked every Update; once the condition is true the
 *    task is latched and never reverts. Three tasks visible at a time.
 * ================================================================
 */

using System;
using UnityEngine;

public class COCIVMaintenanceTaskManager : MonoBehaviour
{
    public static COCIVMaintenanceTaskManager Instance { get; private set; }

    public static event Action OnTasksUpdated;

    private const int TASK_COUNT = 33;

    [Header("Task UI")]
    [SerializeField] private Transform    taskParent;
    [SerializeField] private Transform    finishedParent;
    [SerializeField] private GameObject[] taskObjects = new GameObject[TASK_COUNT];
    [SerializeField] private GameObject   allTasksCompletedText;

    [Header("Hardware")]
    [Tooltip("NetworkHardwareHolder for the COC IV computer — exposes hardwarePrefab.activeSelf.")]
    [SerializeField] private NetworkHardwareHolder computerHolder;

    // ── Internal state ────────────────────────────────────────────────────────

    private readonly bool[] _latched     = new bool[TASK_COUNT];
    private int             _currentTask = 0;
    private bool            _allDone     = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
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
        switch (index)
        {
            // ── Deploy + Login ────────────────────────────────────────────────
            case 0:
                return computerHolder != null
                    && computerHolder.hardwarePrefab != null
                    && computerHolder.hardwarePrefab.activeSelf;

            case 1:
                return ServerVirtualOSManager.Instance != null
                    && ServerVirtualOSManager.Instance.IsOpen
                    && !string.IsNullOrEmpty(ServerVirtualOSManager.Instance.ServerState?.CurrentLoggedInUser);

            // ── System Protection — Create ────────────────────────────────────
            case 2:
                return SystemProtectionController.Instance != null
                    && SystemProtectionController.Instance.gameObject.activeSelf;

            case 3:
                return SystemProtectionController.Instance?.ConfigureOKClicked == true;

            case 4:
                return SystemProtectionController.Instance?.CreateClicked == true;

            case 5:
                return SystemProtectionController.Instance?.SuccessPanelClosed == true;

            // ── System Protection — Built-in Restore ──────────────────────────
            case 6:
                return SystemProtectionController.Instance?.SPCChooseDifferentOn == true;

            case 7:
                return SystemProtectionController.Instance?.SPCPhase2NextClicked == true;

            case 8:
                return SystemProtectionController.Instance?.SPCFinishClicked == true;

            case 9:
                return SystemProtectionController.Instance?.SPCRestartTriggered == true;

            // ── Disk Cleanup ──────────────────────────────────────────────────
            case 10:
                return DiskCleanupController.Instance != null
                    && DiskCleanupController.Instance.gameObject.activeSelf;

            case 11:
                return DiskCleanupController.Instance?.DriveSelectedAndOKClicked == true;

            case 12:
                return DiskCleanupController.Instance?.CleanupSystemFilesClicked == true;

            case 13:
                return DiskCleanupController.Instance?.SecondPassDriveOKClicked == true;

            case 14:
                return DiskCleanupController.Instance?.SystemAnalysisComplete == true;

            case 15:
                return DiskCleanupController.Instance?.DeleteFilesClicked == true;

            case 16:
                return DiskCleanupController.Instance?.CleanupFinished == true;

            // ── Defrag ────────────────────────────────────────────────────────
            case 17:
                return DefragController.Instance != null
                    && DefragController.Instance.gameObject.activeSelf;

            case 18:
                return DefragController.Instance?.AnalyzeStarted == true;

            case 19:
                return DefragController.Instance?.OptimizeStarted == true;

            case 20:
                return DefragController.Instance?.OptimizeCompleted == true
                    && DefragController.Instance.gameObject.activeSelf == false;

            // ── Windows Security ──────────────────────────────────────────────
            case 21:
                return WindowsSecurityController.Instance != null
                    && WindowsSecurityController.Instance.gameObject.activeSelf;

            case 22:
                return WindowsSecurityController.Instance?.VTPSectionOpened == true;

            case 23:
                return WindowsSecurityController.Instance?.QuickScanStarted == true;

            case 24:
                return WindowsSecurityController.Instance?.QuickScanCompleted == true;

            case 25:
                return WindowsSecurityController.Instance?.ToastDismissed == true
                    && WindowsSecurityController.Instance.gameObject.activeSelf == false;

            // ── System Restore via Run Dialog ─────────────────────────────────
            case 26:
                return SystemRestoreController.Instance?.RunPanelOpen == true;

            case 27:
                return SystemRestoreController.Instance?.Panel1Open == true;

            case 28:
                return SystemRestoreController.Instance?.SRPanel2Shown == true;

            case 29:
                return SystemRestoreController.Instance?.ShowMoreToggled == true
                    && SystemRestoreController.Instance?.ScanAffectedClicked == true;

            case 30:
                return SystemRestoreController.Instance?.SRConfirmShown == true;

            case 31:
                return SystemRestoreController.Instance?.SRFinishClicked == true;

            case 32:
                return SystemRestoreController.Instance?.SRRestartTriggered == true;

            default:
                return false;
        }
    }

    // ── Latch & window logic ──────────────────────────────────────────────────

    private void CheckCurrentTask()
    {
        if (_currentTask >= TASK_COUNT) return;

        if (_latched[_currentTask]) { AdvanceIfPossible(); return; }

        if (!CheckCondition(_currentTask)) return;

        LatchTask(_currentTask);
    }

    private void LatchTask(int index)
    {
        _latched[index] = true;
        ActivityLogManager.Log($"Task {index} completed.", ActivityLogManager.EntryType.Action);

        if (taskObjects[index] != null && finishedParent != null)
            taskObjects[index].transform.SetParent(finishedParent, false);

        AdvanceIfPossible();
        RefreshWindow();
        OnTasksUpdated?.Invoke();
    }

    // ── Public API (consumed by COCIVTaskDisplay) ─────────────────────────────

    public string GetNextIncompleteTaskText()
    {
        if (_allDone || _currentTask >= TASK_COUNT) return null;
        var go = taskObjects[_currentTask];
        if (go == null) return null;
        var tmp = go.GetComponent<TMPro.TMP_Text>();
        return tmp != null ? tmp.text : null;
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
    }

}
