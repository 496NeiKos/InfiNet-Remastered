/*
 * ================================================================
 *  UNITY SETUP GUIDE — T3TaskListManager
 * ================================================================
 *  STEP 1 — Create the manager GameObject
 *    - Create a GameObject "T3TaskListManager" inside Topic 3's
 *      taskListContainer (the container assigned in TopicManager).
 *    - Add component: T3TaskListManager.
 *
 *  STEP 2 — Create task text GameObjects (4 tasks)
 *    - Create 4 child TextMeshPro GameObjects for the task labels.
 *      Suggested text:
 *        Task 0: "Open the UEFI setup"
 *        Task 1: "Navigate to the Boot tab"
 *        Task 2: "Configure the boot order"
 *        Task 3: "Save and exit UEFI"
 *    - Place them under a layout parent (taskParent) with a
 *      Vertical Layout Group so completed tasks slide up.
 *    - Create a separate off-screen parent (finishedParent) where
 *      completed tasks are parked.
 *
 *  STEP 3 — Wire the inspector
 *    T3TaskListManager:
 *      taskParent      → the layout parent holding active tasks
 *      finishedParent  → the off-screen parent for completed tasks
 *      taskObjects[0]  → "Open the UEFI setup" TMP text GameObject
 *      taskObjects[1]  → "Navigate to the Boot tab" TMP text GameObject
 *      taskObjects[2]  → "Configure the boot order" TMP text GameObject
 *      taskObjects[3]  → "Save and exit UEFI" TMP text GameObject
 *      uefiNavigator   → UEFINavigator on the UEFI Monitor GameObject
 *
 *  TASK CONDITIONS
 *    Task 0 — open UEFI        : navigator.UEFIOpened
 *    Task 1 — boot tab visited : navigator.BootTabVisited
 *    Task 2 — configure boot   : navigator.BootOrderConfigured
 *                                (wire SetBootOrderConfigured() when content is built)
 *    Task 3 — save and exit    : navigator.SavedAndExited
 *                                (wire SaveAndExit() to the Exit panel button)
 *
 *  When all 4 tasks complete, TopicManager.MarkTopicComplete(2) fires
 *  automatically — marking Topic 3 (UEFI Configuration) as finished.
 * ================================================================
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class T3TaskListManager : MonoBehaviour
{
    public static T3TaskListManager Instance { get; private set; }

    [Header("Task UI")]
    [SerializeField] private Transform taskParent;
    [SerializeField] private Transform finishedParent;
    [SerializeField] private GameObject[] taskObjects;

    [Header("Condition References")]
    [SerializeField] private UEFINavigator uefiNavigator;

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
            Debug.LogError("[T3TaskListManager] Assign all 4 task objects in the inspector.");
            return;
        }

        _tasks = new List<TaskEntry>
        {
            // Task 1 — open the UEFI setup
            new TaskEntry
            {
                taskObject    = taskObjects[0],
                originalIndex = 0,
                condition     = () => uefiNavigator != null && uefiNavigator.UEFIOpened
            },
            // Task 2 — navigate to the Boot tab
            new TaskEntry
            {
                taskObject    = taskObjects[1],
                originalIndex = 1,
                condition     = () => uefiNavigator != null && uefiNavigator.BootTabVisited
            },
            // Task 3 — configure the boot order
            // Wire UEFINavigator.SetBootOrderConfigured() to a button in Panel_Boot.
            new TaskEntry
            {
                taskObject    = taskObjects[2],
                originalIndex = 2,
                condition     = () => uefiNavigator != null && uefiNavigator.BootOrderConfigured
            },
            // Task 4 — save and exit UEFI
            // Wire UEFINavigator.SaveAndExit() to the Save & Exit button in Panel_Exit.
            new TaskEntry
            {
                taskObject    = taskObjects[3],
                originalIndex = 3,
                condition     = () => uefiNavigator != null && uefiNavigator.SavedAndExited
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

        if (_tasks.All(t => t.isCompleted))
        {
            TopicManager.Instance?.MarkTopicComplete(2);
            Debug.Log("[T3TaskListManager] All tasks complete — Topic 3 marked complete.");
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
