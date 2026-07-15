/*
 * ================================================================
 *  UNITY SETUP GUIDE — NetworkCableTaskManager  (COC II)
 * ================================================================
 *
 *  PURPOSE
 *    Self-contained task tracker for the COC II network cable scene.
 *    Drives the NetworkCableTask panel (Task1TMP–Task4TMP) directly.
 *    Does NOT reference any COC I scripts.
 *
 *  TASKS
 *    [0] Expose both cable ends using the wire stripper
 *    [1] Arrange wires in T568A or T568B order on both ends
 *    [2] Install RJ45 connectors on both cable ends
 *    [3] Crimp the cable (placeholder — locked, always dimmed)
 *
 *  HIERARCHY EXPECTED
 *    NetworkCableTask (any layout group)
 *    ├── Task1TMP  (TextMeshProUGUI)
 *    ├── Task2TMP  (TextMeshProUGUI)
 *    ├── Task3TMP  (TextMeshProUGUI)
 *    └── Task4TMP  (TextMeshProUGUI)
 *
 *  INSPECTOR SETUP
 *    taskObjects[0..3]    → Task1TMP through Task4TMP GameObjects
 *    cableEnd1            → NetworkCableEndController on CableEnd1
 *    cableEnd2            → NetworkCableEndController on CableEnd2
 *    allTasksCompletedText → (optional) TMP shown after all tasks done
 *
 *  STEP 1  Attach this script to the Manager root in COC II scene.
 *  STEP 2  Fill in taskObjects, cableEnd1, cableEnd2 in the inspector.
 *  STEP 3  Set Task1TMP text: "Expose both cable ends using the wire stripper"
 *          Set Task2TMP text: "Arrange the wires in T568A or T568B order on both ends"
 *          Set Task3TMP text: "Install RJ45 connectors on both cable ends"
 *          Set Task4TMP text: "Crimp the cable connectors"
 * ================================================================
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class NetworkCableTaskManager : MonoBehaviour
{
    public static NetworkCableTaskManager Instance { get; private set; }

    public static event Action OnTasksUpdated;

    private static readonly Color ActiveColor    = new Color(1f, 0.843f, 0f); // gold
    private static readonly Color CompletedColor = Color.green;
    private static readonly Color LockedColor    = new Color(0.45f, 0.45f, 0.45f, 1f);

    private string _displayOverride      = null;
    private bool   _isCompletionOverride = false;

    [Header("Task UI")]
    [Tooltip("Assign Task1TMP through Task4TMP in order.")]
    [SerializeField] private GameObject[] taskObjects; // 4 entries

    [Header("Cable Ends")]
    [SerializeField] private NetworkCableEndController cableEnd1;
    [SerializeField] private NetworkCableEndController cableEnd2;

    [Header("Completion")]
    [Tooltip("(Optional) TMP shown after all applicable tasks are complete.")]
    [SerializeField] private TextMeshProUGUI allTasksCompletedText;

    private class TaskEntry
    {
        public GameObject   taskObject;
        public bool         isCompleted;
        public bool         isFlashing;
        public bool         isLocked;
        public Func<bool>   condition;
    }

    private List<TaskEntry> _tasks;
    private const int       WindowSize = 3;

    // ── Lifecycle ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (taskObjects == null || taskObjects.Length < 4)
        {
            Debug.LogError("[NetworkCableTaskManager] Assign all 4 task objects in the inspector.");
            return;
        }

        _tasks = new List<TaskEntry>
        {
            new TaskEntry
            {
                taskObject = taskObjects[0],
                condition  = () => cableEnd1 != null && cableEnd1.IsStripped
                                && cableEnd2 != null && cableEnd2.IsStripped
            },
            new TaskEntry
            {
                taskObject = taskObjects[1],
                condition  = () => cableEnd1 != null && cableEnd1.IsWireOrderCorrect()
                                && cableEnd2 != null && cableEnd2.IsWireOrderCorrect()
            },
            new TaskEntry
            {
                taskObject = taskObjects[2],
                condition  = () => cableEnd1 != null && cableEnd1.IsRJ45Installed
                                && cableEnd2 != null && cableEnd2.IsRJ45Installed
            },
            new TaskEntry
            {
                taskObject = taskObjects[3],
                isLocked   = true,
                condition  = () => false
            },
        };

        if (allTasksCompletedText != null)
            allTasksCompletedText.gameObject.SetActive(false);

        RefreshWindow();
    }

    // ── Public API (matches NCIITaskListManager / T3TaskListManager contract) ──────────

    public string GetNextIncompleteTaskText()
    {
        if (_displayOverride != null) return _displayOverride;
        if (_tasks == null) return null;
        var next = _tasks.FirstOrDefault(t => !t.isCompleted && !t.isLocked);
        if (next == null) return null;
        var tmp = next.taskObject.GetComponent<TextMeshProUGUI>();
        return tmp != null ? tmp.text : null;
    }

    public Color GetDisplayColor(Color fallback) =>
        (_isCompletionOverride && _displayOverride != null) ? Color.green : fallback;

    public static void CheckConditions()
    {
        if (Instance == null) return;
        Instance.EvaluateConditions();
    }

    // ── Evaluation ────────────────────────────────────────────────────────────────────

    private void EvaluateConditions()
    {
        if (_tasks == null || !gameObject.activeInHierarchy) return;

        bool anyChange = false;

        foreach (var task in _tasks)
        {
            if (task.isFlashing || task.isLocked) continue;

            bool met = task.condition();

            if (!task.isCompleted && met)
            {
                task.isCompleted = true;
                anyChange = true;
                StartCoroutine(FlashAndComplete(task));
            }
            else if (task.isCompleted && !met)
            {
                task.isCompleted = false;
                anyChange = true;
                StartCoroutine(FlashRevert(task));
            }
        }

        if (anyChange) RefreshWindow();

        if (_tasks.Where(t => !t.isLocked).All(t => t.isCompleted))
            StartCoroutine(ShowAllTasksCompleted());
    }

    // ── Window display ────────────────────────────────────────────────────────────────

    private void RefreshWindow()
    {
        if (_tasks == null) return;

        int firstIncomplete = _tasks.FindIndex(t => !t.isCompleted && !t.isLocked);
        int windowStart     = firstIncomplete < 0
            ? _tasks.Count - WindowSize
            : Mathf.Max(0, firstIncomplete - 1);
        windowStart = Mathf.Max(0, windowStart);

        for (int i = 0; i < _tasks.Count; i++)
        {
            var task     = _tasks[i];
            bool inWindow = i >= windowStart && i < windowStart + WindowSize;

            task.taskObject.SetActive(inWindow);

            var tmp = task.taskObject.GetComponent<TextMeshProUGUI>();
            if (tmp == null) continue;

            if (task.isCompleted)
                tmp.color = CompletedColor;
            else if (task.isLocked)
                tmp.color = LockedColor;
            else if (i == firstIncomplete)
                tmp.color = ActiveColor;
            else
                tmp.color = LockedColor;
        }

        OnTasksUpdated?.Invoke();
    }

    // ── Coroutines ────────────────────────────────────────────────────────────────────

    private IEnumerator FlashAndComplete(TaskEntry task)
    {
        task.isFlashing = true;
        var tmp = task.taskObject.GetComponent<TextMeshProUGUI>();

        float elapsed = 0f;
        while (elapsed < 0.6f)
        {
            elapsed += Time.deltaTime;
            if (tmp != null) tmp.color = elapsed % 0.2f < 0.1f ? CompletedColor : ActiveColor;
            yield return null;
        }

        if (tmp != null) tmp.color = CompletedColor;
        task.isFlashing = false;
        RefreshWindow();
    }

    private IEnumerator FlashRevert(TaskEntry task)
    {
        task.isFlashing = true;
        var tmp = task.taskObject.GetComponent<TextMeshProUGUI>();

        float elapsed = 0f;
        while (elapsed < 0.6f)
        {
            elapsed += Time.deltaTime;
            if (tmp != null) tmp.color = elapsed % 0.2f < 0.1f ? Color.red : ActiveColor;
            yield return null;
        }

        task.isFlashing = false;
        RefreshWindow();
    }

    private IEnumerator ShowAllTasksCompleted()
    {
        yield return new WaitForSeconds(0.5f);

        _displayOverride      = "Cable complete! Proceed to crimp and test.";
        _isCompletionOverride = true;

        if (allTasksCompletedText != null)
        {
            allTasksCompletedText.text = _displayOverride;
            allTasksCompletedText.gameObject.SetActive(true);
        }

        OnTasksUpdated?.Invoke();
    }
}
