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
 *
 *  When all 12 tasks complete, TopicManager.MarkTopicComplete(1) is called
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

    [Header("Task UI")]
    [SerializeField] private Transform taskParent;
    [SerializeField] private Transform finishedParent;
    [SerializeField] private GameObject[] taskObjects;

    [Header("Condition References")]
    [SerializeField] private CablePort usbPort;
    [SerializeField] private T2MonitorNavigator monitorNavigator;
    [SerializeField] private RufusSetupManager rufusSetupManager;

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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (taskObjects == null || taskObjects.Length < 12)
        {
            Debug.LogError("[T2TaskListManager] Assign all 12 task objects in the inspector.");
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
            }
        };

        foreach (var task in _tasks)
        {
            task.taskObject.SetActive(false);
            task.taskObject.transform.SetParent(taskParent, false);
            task.taskObject.transform.SetSiblingIndex(task.originalIndex);
            var tmp = task.taskObject.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.color = Color.white;
        }

        RefreshWindow();
        EvaluateConditions();
    }

    // Returns the text of the next incomplete task, or null if all done / not yet initialised.
    public string GetNextIncompleteTaskText()
    {
        if (_tasks == null) return null;
        var next = _tasks.FirstOrDefault(t => !t.isCompleted);
        if (next == null) return null;
        var tmp = next.taskObject.GetComponent<TextMeshProUGUI>();
        return tmp != null ? tmp.text : null;
    }

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

            bool met = task.condition();

            if (!task.isCompleted && met)
            {
                task.isCompleted = true;
                StartCoroutine(FlashAndComplete(task));
            }
            else if (task.isCompleted && !met)
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
        if (tmp != null) tmp.color = Color.white;
        OnTasksUpdated?.Invoke();
        EvaluateConditions();
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
