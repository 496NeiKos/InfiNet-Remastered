/*
 * ================================================================
 *  UNITY SETUP GUIDE — T2TaskListManager
 * ================================================================
 *  STEP 1 — Create the manager GameObject
 *    - Create a GameObject "T2TaskListManager" inside Topic 2's
 *      taskListContainer (the container assigned in TopicManager).
 *    - Add component: T2TaskListManager.
 *
 *  STEP 2 — Create task text GameObjects (4 tasks)
 *    - Create 4 child TextMeshPro GameObjects for the task labels.
 *      Suggested text:
 *        Task 0: "Open the web browser"
 *        Task 1: "Download Rufus"
 *        Task 2: "Open Rufus"
 *        Task 3: "Finish the Rufus setup"   (tentative — see note)
 *    - Place them under a layout parent (taskParent) with a
 *      Vertical Layout Group so completed tasks slide up.
 *    - Create a separate off-screen parent (finishedParent) where
 *      completed tasks are parked.
 *
 *  STEP 3 — Wire the inspector
 *    T2TaskListManager:
 *      taskParent       → the layout parent holding active tasks
 *      finishedParent   → the off-screen parent for completed tasks
 *      taskObjects[0]   → "Open browser" TMP text GameObject
 *      taskObjects[1]   → "Download Rufus" TMP text GameObject
 *      taskObjects[2]   → "Open Rufus" TMP text GameObject
 *      taskObjects[3]   → "Finish Rufus setup" TMP text GameObject
 *      usbPort          → CablePort on the USB port (kept for future use
 *                         by Task 4 — not used by any active condition yet)
 *      monitorNavigator → T2MonitorNavigator on the T2 Monitor GameObject
 *
 *  TASK CONDITIONS
 *    Task 0 — open browser   : navigator.BrowserOpened
 *    Task 1 — download rufus : navigator.IsRufusDownloaded
 *    Task 2 — open rufus     : navigator.RufusOpened
 *    Task 3 — finish setup   : TENTATIVE — always false for now. When the
 *             final flash step is designed, replace the Task 3 condition
 *             (e.g. navigator.IsRufusComplete, possibly && usbPort.IsInstalled).
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
        if (taskObjects == null || taskObjects.Length < 4)
        {
            Debug.LogError("[T2TaskListManager] Assign all 4 task objects in the inspector.");
            return;
        }

        _tasks = new List<TaskEntry>
        {
            // Task 1 — open the web browser
            new TaskEntry
            {
                taskObject = taskObjects[0],
                originalIndex = 0,
                condition = () => monitorNavigator != null && monitorNavigator.BrowserOpened
            },
            // Task 2 — download Rufus (Rufus app becomes enabled on the desktop)
            new TaskEntry
            {
                taskObject = taskObjects[1],
                originalIndex = 1,
                condition = () => monitorNavigator != null && monitorNavigator.IsRufusDownloaded
            },
            // Task 3 — open Rufus
            new TaskEntry
            {
                taskObject = taskObjects[2],
                originalIndex = 2,
                condition = () => monitorNavigator != null && monitorNavigator.RufusOpened
            },
            // Task 4 — finish the Rufus setup (TENTATIVE — condition not wired yet)
            new TaskEntry
            {
                taskObject = taskObjects[3],
                originalIndex = 3,
                condition = () => false
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
    }

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
