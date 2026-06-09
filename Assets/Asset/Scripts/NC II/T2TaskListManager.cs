/*
 * ================================================================
 *  UNITY SETUP GUIDE — T2TaskListManager
 * ================================================================
 *  STEP 1 — Create the manager GameObject
 *    - Create a GameObject "T2TaskListManager" inside Topic 2's
 *      taskListContainer (the container assigned in TopicManager).
 *    - Add component: T2TaskListManager.
 *
 *  STEP 2 — Create task text GameObjects (5 tasks)
 *    - Create 5 child TextMeshPro GameObjects for the task labels.
 *      Suggested text:
 *        Task 0: "Install the flash drive to the USB port"
 *        Task 1: "Click Chrome to open the web browser"
 *        Task 2: "Download Rufus and the Windows 10 ISO"
 *        Task 3: "Open Rufus from the desktop"
 *        Task 4: "Configure Rufus to create a bootable flash drive"
 *    - Place them under a layout parent (taskParent) with a
 *      Vertical Layout Group so completed tasks slide up.
 *    - Create a separate off-screen parent (finishedParent) where
 *      completed tasks are parked.
 *
 *  STEP 3 — Wire the inspector
 *    T2TaskListManager:
 *      taskParent       → the layout parent holding active tasks
 *      finishedParent   → the off-screen parent for completed tasks
 *      taskObjects[0]   → "Install flash drive" TMP text GameObject
 *      taskObjects[1]   → "Open browser" TMP text GameObject
 *      taskObjects[2]   → "Download Rufus and ISO" TMP text GameObject
 *      taskObjects[3]   → "Open Rufus" TMP text GameObject
 *      taskObjects[4]   → "Configure Rufus" TMP text GameObject
 *      usbPort          → CablePort on the USB port of T2SystemUnitFront
 *      monitorNavigator → T2MonitorNavigator on the T2 Monitor GameObject
 *
 *  TASK CONDITIONS
 *    Task 0 — install flash drive : usbPort.IsInstalled
 *    Task 1 — open browser        : navigator.BrowserOpened  (BrowserApp button → GoTo(1))
 *    Task 2 — download both       : navigator.IsRufusDownloaded && navigator.IsIsoDownloaded
 *    Task 3 — open rufus          : navigator.RufusOpened    (RufusApp button on Desktop)
 *    Task 4 — configure rufus     : navigator.IsRufusComplete
 *
 *  When all 5 tasks complete, TopicManager.MarkTopicComplete(1) is called
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

    [Header("Task UI")]
    [SerializeField] private Transform taskParent;
    [SerializeField] private Transform finishedParent;
    [SerializeField] private GameObject[] taskObjects;

    [Header("Condition References")]
    [SerializeField] private CablePort usbPort;
    [SerializeField] private T2MonitorNavigator monitorNavigator;

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
        if (taskObjects == null || taskObjects.Length < 5)
        {
            Debug.LogError("[T2TaskListManager] Assign all 5 task objects in the inspector.");
            return;
        }

        _tasks = new List<TaskEntry>
        {
            // Task 1 — install flash drive to USB port.
            // CablePort._isInstalled can't be trusted here: T2SystemUnitFront starts
            // inactive so CablePort.Awake() (which sets _isInstalled = !startEmpty) may
            // never have run. Instead check whether a CableBehavior is a child of the
            // port — InstallToPort() reparents it there, so this is always accurate.
            new TaskEntry
            {
                taskObject = taskObjects[0],
                originalIndex = 0,
                condition = () => usbPort != null && usbPort.GetComponentInChildren<CableBehavior>(true) != null
            },
            // Task 2 — click Chrome to open the browser
            new TaskEntry
            {
                taskObject = taskObjects[1],
                originalIndex = 1,
                condition = () => monitorNavigator != null && monitorNavigator.BrowserOpened
            },
            // Task 3 — download both Rufus and the Windows 10 ISO
            new TaskEntry
            {
                taskObject = taskObjects[2],
                originalIndex = 2,
                condition = () => monitorNavigator != null && monitorNavigator.IsRufusDownloaded && monitorNavigator.IsIsoDownloaded
            },
            // Task 4 — open Rufus from the desktop
            new TaskEntry
            {
                taskObject = taskObjects[3],
                originalIndex = 3,
                condition = () => monitorNavigator != null && monitorNavigator.RufusOpened
            },
            // Task 5 — configure Rufus to create a bootable flash drive
            new TaskEntry
            {
                taskObject = taskObjects[4],
                originalIndex = 4,
                condition = () => monitorNavigator != null && monitorNavigator.IsRufusComplete
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

    // Only re-evaluate on re-enable (tab switch back to Topic 2).
    // _tasks is null until Start() runs, so we skip the very first activation where
    // other components' Awake() may not have fired yet and CablePort._isInstalled
    // would still be true (its field initializer) before Awake sets it to !startEmpty.
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
