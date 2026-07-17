/*
 * ================================================================
 *  UNITY SETUP GUIDE — T2TaskListManager  (12-task version)
 * ================================================================
 *  STEP 1 — Create the manager GameObject
 *    - Create a GameObject "T2TaskListManager" inside Topic 2's
 *      taskListContainer (the container assigned in TopicManager).
 *    - Add component: T2TaskListManager.
 *
 *  STEP 2 — Create 12 task text GameObjects
 *    - Create 12 child TextMeshPro GameObjects for the task labels.
 *      Suggested text (must match task order):
 *        taskObjects[0]  "Install Flashdrive to the System Unit USB Port"
 *        taskObjects[1]  "Access Virtual OS through Monitor: Right-Click"
 *        taskObjects[2]  "Open Browser and Install Rufus and Windows 10 ISO file"
 *        taskObjects[3]  "Execute Rufus application to configure"
 *        taskObjects[4]  "Include the ISO to the Boot Selection"
 *        taskObjects[5]  "Image option: Standard Windows Installation"
 *        taskObjects[6]  "Partition: MBR"
 *        taskObjects[7]  "Target System: UEFI(non CSM)"
 *        taskObjects[8]  "File System: NTFS"
 *        taskObjects[9]  "Cluster Size: Default(4096)"
 *        taskObjects[10] "Start the configuration"
 *        taskObjects[11] "Complete Rufus set up"
 *        taskObjects[12] "Close the Rufus application and click the task bar search field below"
 *        taskObjects[13] "Search command prompt: cmd, command, command prompt"
 *        taskObjects[14] "Click run as administrator"
 *        taskObjects[15] "Type: diskpart"
 *        taskObjects[16] "Type: list disk"
 *        taskObjects[17] "Type: select disk 1"
 *        taskObjects[18] "Type: clean"
 *        taskObjects[19] "Type: create partition primary"
 *        taskObjects[20] "Type: select partition 1"
 *        taskObjects[21] "Type: active"
 *        taskObjects[22] "Type: format fs=ntfs quick"
 *        taskObjects[23] "Type: assign"
 *        taskObjects[24] "Type: exit"
 *        taskObjects[25] "Type: xcopy e:\\*.* f:\\ /s /e /f"
 *    - Place them under a layout parent (taskParent) with a
 *      Vertical Layout Group so completed tasks slide up.
 *    - Create a separate off-screen parent (finishedParent) where
 *      completed tasks are parked.
 *
 *  STEP 3 — Wire the inspector
 *    T2TaskListManager:
 *      taskParent        → the layout parent holding active tasks
 *      finishedParent    → the off-screen parent for completed tasks
 *      taskObjects[0..11] → 12 TMP text GameObjects (in order above)
 *      usbPort           → CablePort on the USB port of T2SystemUnitFront
 *      monitorNavigator  → T2MonitorNavigator on the T2 Monitor GameObject
 *      rufusSetupManager → RufusSetupManager on the RufusSetUp panel
 *
 *  TASK CONDITIONS
 *    Task  1 — USB installed          : usbPort has a CableBehavior child
 *    Task  2 — canvas opened          : monitorNavigator.MonitorCanvasOpened
 *    Task  3 — both downloaded        : monitorNavigator.IsRufusDownloaded &&
 *                                        monitorNavigator.IsIsoDownloaded
 *                                        (progress bar must finish first)
 *    Task  4 — Rufus launched         : monitorNavigator.RufusOpened
 *    Task  5 — ISO in Boot Selection  : rufusSetupManager.IsIsoInBootSelection
 *    Task  6 — Image Option correct   : rufusSetupManager.IsImageOptionSet
 *    Task  7 — Partition correct      : rufusSetupManager.IsPartitionSet
 *    Task  8 — Target System correct  : rufusSetupManager.IsTargetSystemSet
 *    Task  9 — File System correct    : rufusSetupManager.IsFileSystemSet
 *    Task 10 — Cluster Size correct   : rufusSetupManager.IsClusterSizeSet
 *    Task 11 — Start clicked          : rufusSetupManager.FormattingStarted
 *    Task 12 — Rufus complete         : monitorNavigator.IsRufusComplete
 *    Task 13 — Taskbar search opened  : taskbarSearchManager.WindowIconContentOpened
 *    Task 14 — CMD searched           : taskbarSearchManager.SearchedPanelOpened
 *    Task 15 — Run as admin clicked   : taskbarSearchManager.CommandPromptOpened
 *    Task 16 — diskpart typed         : cmdManager.CurrentStep >= 1
 *    Task 17 — list disk typed        : cmdManager.CurrentStep >= 2
 *    Task 18 — select disk 1 typed    : cmdManager.CurrentStep >= 3
 *    Task 19 — clean typed            : cmdManager.CurrentStep >= 4
 *    Task 20 — create partition typed : cmdManager.CurrentStep >= 5
 *    Task 21 — select partition typed : cmdManager.CurrentStep >= 6
 *    Task 22 — active typed           : cmdManager.CurrentStep >= 7
 *    Task 23 — format typed           : cmdManager.CurrentStep >= 8
 *    Task 24 — assign typed           : cmdManager.CurrentStep >= 9
 *    Task 25 — exit typed             : cmdManager.CurrentStep >= 10
 *    Task 26 — xcopy typed            : cmdManager.IsCmdSequenceComplete
 *
 *  When all 26 tasks complete, TopicManager.MarkTopicComplete(1) is called
 *  automatically — this unlocks Topic 3 (UEFI Configuration).
 * ================================================================
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class T2TaskListManager : MonoBehaviour
{
    public static T2TaskListManager Instance { get; private set; }

    // Fires whenever a task completes or reverts — SingleTaskDisplay subscribes to this.
    public static event Action OnTasksUpdated;

    private static readonly Color GoldColor = new Color(1f, 0.843f, 0f);

    [Header("Task UI")]
    [SerializeField] private Transform taskParent;
    [SerializeField] private Transform finishedParent;
    [SerializeField] private GameObject[] taskObjects;

    [Header("Completion UI")]
    [SerializeField] private TextMeshProUGUI allTasksCompletedText;

    [Header("Condition References")]
    [SerializeField] private CablePort usbPort;
    [SerializeField] private T2MonitorNavigator monitorNavigator;
    [SerializeField] private RufusSetupManager rufusSetupManager;
    [SerializeField] private TaskbarSearchManager taskbarSearchManager;
    [SerializeField] private CommandPromptManager cmdManager;


    private class TaskEntry
    {
        public GameObject taskObject;
        public int originalIndex;
        public bool isCompleted;
        public bool isFlashing;
        public Func<bool> condition;
    }

    private List<TaskEntry> _tasks;
    private const int WindowSize = 3;

    private string _displayOverride      = null;
    private bool   _isCompletionOverride = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (taskObjects == null || taskObjects.Length < 26)
        {
            Debug.LogError("[T2TaskListManager] Assign all 26 task objects in the inspector.");
            return;
        }

        _tasks = new List<TaskEntry>
        {
            // Task 1 — install flash drive to USB port.
            // Check CableBehavior child rather than CablePort._isInstalled because the port
            // lives in an initially-inactive hierarchy whose Awake() may not have run yet.
            new TaskEntry
            {
                taskObject    = taskObjects[0],
                originalIndex = 0,
                condition     = () => usbPort != null
                                   && usbPort.GetComponentInChildren<CableBehavior>(true) != null
            },
            // Task 2 — right-click the T2 monitor to open the Virtual OS canvas.
            new TaskEntry
            {
                taskObject    = taskObjects[1],
                originalIndex = 1,
                condition     = () => monitorNavigator != null && monitorNavigator.MonitorCanvasOpened
            },
            // Task 3 — install both Rufus and the Windows 10 ISO via the browser.
            // Both download progress bars must finish before either flag is set.
            new TaskEntry
            {
                taskObject    = taskObjects[2],
                originalIndex = 2,
                condition     = () => monitorNavigator != null
                                   && monitorNavigator.IsRufusDownloaded
                                   && monitorNavigator.IsIsoDownloaded
            },
            // Task 4 — open the Rufus application from the Desktop panel.
            new TaskEntry
            {
                taskObject    = taskObjects[3],
                originalIndex = 3,
                condition     = () => monitorNavigator != null && monitorNavigator.RufusOpened
            },
            // Task 5 — select the ISO file in the Boot Selection dropdown inside Rufus.
            new TaskEntry
            {
                taskObject    = taskObjects[4],
                originalIndex = 4,
                condition     = () => rufusSetupManager != null && rufusSetupManager.IsIsoInBootSelection
            },
            // Task 6 — set Image Option to "Standard Windows Installation".
            new TaskEntry
            {
                taskObject    = taskObjects[5],
                originalIndex = 5,
                condition     = () => rufusSetupManager != null && rufusSetupManager.IsImageOptionSet
            },
            // Task 7 — set Partition Scheme to "MBR".
            new TaskEntry
            {
                taskObject    = taskObjects[6],
                originalIndex = 6,
                condition     = () => rufusSetupManager != null && rufusSetupManager.IsPartitionSet
            },
            // Task 8 — set Target System to "UEFI (non CSM)".
            new TaskEntry
            {
                taskObject    = taskObjects[7],
                originalIndex = 7,
                condition     = () => rufusSetupManager != null && rufusSetupManager.IsTargetSystemSet
            },
            // Task 9 — set File System to "NTFS".
            new TaskEntry
            {
                taskObject    = taskObjects[8],
                originalIndex = 8,
                condition     = () => rufusSetupManager != null && rufusSetupManager.IsFileSystemSet
            },
            // Task 10 — set Cluster Size to "4096 bytes (Default)".
            new TaskEntry
            {
                taskObject    = taskObjects[9],
                originalIndex = 9,
                condition     = () => rufusSetupManager != null && rufusSetupManager.IsClusterSizeSet
            },
            // Task 11 — click Start to begin formatting.
            new TaskEntry
            {
                taskObject    = taskObjects[10],
                originalIndex = 10,
                condition     = () => rufusSetupManager != null && rufusSetupManager.FormattingStarted
            },
            // Task 12 — formatting completes successfully.
            new TaskEntry
            {
                taskObject    = taskObjects[11],
                originalIndex = 11,
                condition     = () => monitorNavigator != null && monitorNavigator.IsRufusComplete
            },
            // Task 13 — close Rufus and open the taskbar search.
            new TaskEntry
            {
                taskObject    = taskObjects[12],
                originalIndex = 12,
                condition     = () => taskbarSearchManager != null && taskbarSearchManager.WindowIconContentOpened
            },
            // Task 14 — type a valid search term to find Command Prompt.
            new TaskEntry
            {
                taskObject    = taskObjects[13],
                originalIndex = 13,
                condition     = () => taskbarSearchManager != null && taskbarSearchManager.SearchedPanelOpened
            },
            // Task 15 — click Run as Administrator to open the CMD window.
            new TaskEntry
            {
                taskObject    = taskObjects[14],
                originalIndex = 14,
                condition     = () => taskbarSearchManager != null && taskbarSearchManager.CommandPromptOpened
            },
            // Task 16 — type "diskpart" to enter DiskPart.
            new TaskEntry
            {
                taskObject    = taskObjects[15],
                originalIndex = 15,
                condition     = () => cmdManager != null && cmdManager.CurrentStep >= 1
            },
            // Task 17 — type "list disk" to display connected disks.
            new TaskEntry
            {
                taskObject    = taskObjects[16],
                originalIndex = 16,
                condition     = () => cmdManager != null && cmdManager.CurrentStep >= 2
            },
            // Task 18 — type "select disk 1" to target the USB drive.
            new TaskEntry
            {
                taskObject    = taskObjects[17],
                originalIndex = 17,
                condition     = () => cmdManager != null && cmdManager.CurrentStep >= 3
            },
            // Task 19 — type "clean" to wipe the USB drive.
            new TaskEntry
            {
                taskObject    = taskObjects[18],
                originalIndex = 18,
                condition     = () => cmdManager != null && cmdManager.CurrentStep >= 4
            },
            // Task 20 — type "create partition primary".
            new TaskEntry
            {
                taskObject    = taskObjects[19],
                originalIndex = 19,
                condition     = () => cmdManager != null && cmdManager.CurrentStep >= 5
            },
            // Task 21 — type "select partition 1".
            new TaskEntry
            {
                taskObject    = taskObjects[20],
                originalIndex = 20,
                condition     = () => cmdManager != null && cmdManager.CurrentStep >= 6
            },
            // Task 22 — type "active" to mark the partition as bootable.
            new TaskEntry
            {
                taskObject    = taskObjects[21],
                originalIndex = 21,
                condition     = () => cmdManager != null && cmdManager.CurrentStep >= 7
            },
            // Task 23 — type "format fs=ntfs quick".
            new TaskEntry
            {
                taskObject    = taskObjects[22],
                originalIndex = 22,
                condition     = () => cmdManager != null && cmdManager.CurrentStep >= 8
            },
            // Task 24 — type "assign" to assign a drive letter.
            new TaskEntry
            {
                taskObject    = taskObjects[23],
                originalIndex = 23,
                condition     = () => cmdManager != null && cmdManager.CurrentStep >= 9
            },
            // Task 25 — type "exit" to leave DiskPart.
            new TaskEntry
            {
                taskObject    = taskObjects[24],
                originalIndex = 24,
                condition     = () => cmdManager != null && cmdManager.CurrentStep >= 10
            },
            // Task 26 — type the xcopy command to copy Windows files to the USB.
            new TaskEntry
            {
                taskObject    = taskObjects[25],
                originalIndex = 25,
                condition     = () => cmdManager != null && cmdManager.IsCmdSequenceComplete
            }
        };

        foreach (var task in _tasks)
        {
            task.taskObject.SetActive(false);
            task.taskObject.transform.SetParent(taskParent, false);
            task.taskObject.transform.SetSiblingIndex(task.originalIndex);
            var tmp = task.taskObject.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.color = GoldColor;
        }

        if (allTasksCompletedText != null)
            allTasksCompletedText.gameObject.SetActive(false);

        RefreshWindow();
        EvaluateConditions();
    }

    // Returns override text (completion banner) when set, otherwise the next incomplete task.
    public string GetNextIncompleteTaskText()
    {
        if (_displayOverride != null) return _displayOverride;
        if (_tasks == null) return null;
        var next = _tasks.FirstOrDefault(t => !t.isCompleted);
        if (next == null) return null;
        var tmp = next.taskObject.GetComponent<TextMeshProUGUI>();
        return tmp != null ? tmp.text : null;
    }

    public Color GetDisplayColor(Color fallback) =>
        (_isCompletionOverride && _displayOverride != null) ? Color.green : fallback;

    // Only re-evaluate on re-enable (tab switch back to Topic 2).
    private void OnEnable() { if (_tasks != null) EvaluateConditions(); }

    public static void CheckConditions()
    {
        if (Instance == null) return;
        Instance.EvaluateConditions();
    }

    private void EvaluateConditions()
    {
        if (!gameObject.activeInHierarchy) return;
        if (_tasks == null) return;

        foreach (var task in _tasks)
        {
            if (task.isFlashing) continue;

            if (!task.isCompleted)
            {
                // Only check completion for tasks currently visible in the active 3-task window.
                if (!task.taskObject.activeSelf) continue;

                if (task.condition())
                {
                    task.isCompleted = true;
                    StartCoroutine(FlashAndComplete(task));
                }
            }
            else
            {
                // Always check completed tasks for reversion.
                if (!task.condition())
                {
                    task.isCompleted = false;
                    task.taskObject.transform.SetParent(taskParent, false);
                    task.taskObject.transform.SetSiblingIndex(task.originalIndex);
                    task.taskObject.SetActive(false);
                    RefreshWindow();
                    if (task.taskObject.activeSelf)
                        StartCoroutine(FlashRevert(task));
                }
            }
        }
    }

    private IEnumerator FlashAndComplete(TaskEntry task)
    {
        task.isFlashing = true;
        var tmp = task.taskObject.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.color = Color.green;
        yield return new WaitForSeconds(0.6f);
        task.isFlashing = false;
        task.taskObject.transform.SetParent(finishedParent, false);
        task.taskObject.SetActive(false);
        RefreshWindow();
        OnTasksUpdated?.Invoke();

        if (_tasks.All(t => t.isCompleted))
        {
            ShowAllTasksCompleted();
            TopicManager.Instance?.MarkTopicComplete(1);
            Debug.Log("[T2TaskListManager] All tasks complete — Topic 2 marked complete.");
            yield break;
        }

        EvaluateConditions();
    }

    private IEnumerator FlashRevert(TaskEntry task)
    {
        task.isFlashing = true;
        var tmp = task.taskObject.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.color = new Color(1f, 0.647f, 0f);
        yield return new WaitForSeconds(0.6f);
        task.isFlashing = false;
        if (tmp != null) tmp.color = GoldColor;
        OnTasksUpdated?.Invoke();
        EvaluateConditions();
    }

    private void ShowAllTasksCompleted()
    {
        if (allTasksCompletedText != null)
        {
            allTasksCompletedText.text = "All task Completed!";
            allTasksCompletedText.color = Color.green;
            allTasksCompletedText.gameObject.SetActive(true);
        }
        _displayOverride      = "All task Completed!";
        _isCompletionOverride = true;
        OnTasksUpdated?.Invoke();
    }

    private void RefreshWindow()
    {
        if (_tasks == null) return;

        var incomplete = _tasks
            .Where(t => !t.isCompleted)
            .OrderBy(t => t.originalIndex)
            .ToList();

        for (int i = 0; i < incomplete.Count; i++)
            incomplete[i].taskObject.SetActive(i < WindowSize);
    }
}
