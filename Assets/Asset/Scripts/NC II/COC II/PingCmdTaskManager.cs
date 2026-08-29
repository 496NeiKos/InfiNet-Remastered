/*
 * ================================================================
 *  UNITY SETUP GUIDE — PingCmdTaskManager  (COC II, Category 3)
 * ================================================================
 *
 *  PURPOSE
 *    Drives the 37-task "Ping Test - CMD" sequence for COC II.
 *    Mirrors the IPConfigTaskManager pattern: 3-task sliding window,
 *    latch-based conditions, flash-on-complete.
 *
 *    Requires IPConfigTaskManager to be fully complete before any
 *    task in this category can progress.
 *
 *  TASKS — SERVER DESKTOP (indices 0–12)
 *    [ 0] Execute Command Prompt of the Server Desktop device through Virtual OS
 *    [ 1] Input "ipconfig" in the CMD
 *    [ 2] Ping this Server desktop's IP address
 *    [ 3] Observe the ping request feedback if the reply is correct
 *    [ 4] Ping the other Desktop device's IP address
 *    [ 5] Observe the ping request feedback if the reply is correct
 *    [ 6] Ping the Laptop device's IP address
 *    [ 7] Observe the ping request feedback if the reply is correct
 *    [ 8] Ping the Router device's IP address
 *    [ 9] Observe the ping request feedback if the reply is correct
 *    [10] Ping the Access Point device's IP address
 *    [11] Observe the ping request feedback if the reply is correct
 *    [12] Server desktop ping test complete. Close the CMD and Server's Virtual OS
 *
 *  TASKS — OTHER DESKTOP (indices 13–24)
 *    [13] Execute Command Prompt of the other Desktop device through Virtual OS
 *    [14] Ping this Desktop's IP address
 *    [15] Observe the ping request feedback if the reply is correct
 *    [16] Ping the Server Desktop device's IP address
 *    [17] Observe the ping request feedback if the reply is correct
 *    [18] Ping the Laptop device's IP address
 *    [19] Observe the ping request feedback if the reply is correct
 *    [20] Ping the Router device's IP address
 *    [21] Observe the ping request feedback if the reply is correct
 *    [22] Ping the Access Point device's IP address
 *    [23] Observe the ping request feedback if the reply is correct
 *    [24] Desktop ping test complete. Close the CMD and Desktop's Virtual OS
 *
 *  TASKS — LAPTOP (indices 25–36)
 *    [25] Execute Command Prompt of the Laptop device through Virtual OS
 *    [26] Ping this Laptop's IP address
 *    [27] Observe the ping request feedback if the reply is correct
 *    [28] Ping the Server Desktop device's IP address
 *    [29] Observe the ping request feedback if the reply is correct
 *    [30] Ping the other Desktop device's IP address
 *    [31] Observe the ping request feedback if the reply is correct
 *    [32] Ping the Router device's IP address
 *    [33] Observe the ping request feedback if the reply is correct
 *    [34] Ping the Access Point device's IP address
 *    [35] Observe the ping request feedback if the reply is correct
 *    [36] Laptop ping test complete. Close the CMD and Laptop's Virtual OS
 *
 *  INSPECTOR SETUP
 *    Task UI
 *      taskParent          → Vertical Layout Group parent inside PingCmd panel
 *      finishedParent      → off-screen sibling of taskParent (NOT nested inside it)
 *      taskObjects[0..36]  → 37 TMP text GameObjects in the order above
 *    Completion UI
 *      allTasksCompletedText → (optional) TMP shown when all 37 tasks done
 *    Completion Routing
 *      topicManagerCompletionIndex → -1 to disable
 *    CMD Reference
 *      pingCmdManager      → PingCmdManager on the CMD Panel inside Virtual OS
 * ================================================================
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class PingCmdTaskManager : MonoBehaviour, ITaskCategory
{
    public static PingCmdTaskManager Instance { get; private set; }

    public static event Action OnTasksUpdated;

    private static readonly Color GoldColor = new Color(1f, 0.843f, 0f);

    // PingResults indices — must match PingCmdManager constants
    private const int IdxRouter    = 0;
    private const int IdxAP        = 1;
    private const int IdxComputer1 = 2;
    private const int IdxComputer2 = 3;
    private const int IdxLaptop    = 4;

    // ── Inspector ──────────────────────────────────────────────────────────────────────

    [Header("Task UI")]
    [SerializeField] private Transform    taskParent;
    [Tooltip("Off-screen sibling of taskParent — do NOT nest inside taskParent.")]
    [SerializeField] private Transform    finishedParent;
    [Tooltip("Exactly 37 task GameObjects in the order listed in the header comment.")]
    [SerializeField] private GameObject[] taskObjects;

    [Header("Completion UI")]
    [Tooltip("(Optional) TMP shown after all 37 tasks are complete.")]
    [SerializeField] private TextMeshProUGUI allTasksCompletedText;

    [Header("Completion Routing")]
    [Tooltip("Index passed to TopicManager.MarkTopicComplete() on full completion. -1 = disabled.")]
    [SerializeField] private int topicManagerCompletionIndex = -1;

    [Header("CMD Reference")]
    [SerializeField] private PingCmdManager pingCmdManager;

    // ── State ──────────────────────────────────────────────────────────────────────────

    private readonly bool[] _latched = new bool[37];

    // Captured when task [0] latches — identifies which desktop is server and which is second.
    private DeviceID _serverDevice;
    private DeviceID _secondDevice;

    // Captured when task [0] latches — baseline ipconfig run count for the server device.
    // Task [1] fires only when the count exceeds this baseline, preventing stale CMD history
    // from auto-completing the ipconfig task.
    private int _serverIpconfigBaseline;

    private class TaskEntry
    {
        public GameObject taskObject;
        public int        originalIndex;
        public bool       isCompleted;
        public bool       isFlashing;
        public Func<bool> condition;
    }

    private List<TaskEntry> _tasks;
    private const int       WindowSize = 3;

    private string _displayOverride      = null;
    private bool   _isCompletionOverride = false;

    // ── Lifecycle ──────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (taskObjects == null || taskObjects.Length < 37)
        {
            Debug.LogError("[PingCmdTaskManager] Assign all 37 task objects in the inspector.");
            return;
        }

        BuildTasks();

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
    }

    // ── Task Definitions ───────────────────────────────────────────────────────────────

    private void BuildTasks()
    {
        _tasks = new List<TaskEntry>
        {
            // ── SERVER DESKTOP FLOW (indices 0–12) ────────────────────────────────────
            // _serverDevice and _secondDevice are captured when task [0] latches.

            // [0] Open CMD on the Server Desktop
            new TaskEntry
            {
                taskObject    = taskObjects[0],
                originalIndex = 0,
                condition     = () =>
                {
                    if (!_latched[0] &&
                        IPConfigTaskManager.Instance != null &&
                        IPConfigTaskManager.Instance.IsFullyComplete &&
                        VirtualOSManager.Instance != null &&
                        VirtualOSManager.Instance.IsOpen &&
                        (VirtualOSManager.Instance.CurrentDevice == DeviceID.Computer1 ||
                         VirtualOSManager.Instance.CurrentDevice == DeviceID.Computer2) &&
                        pingCmdManager != null && pingCmdManager.gameObject.activeSelf)
                    {
                        _serverDevice = IPConfigTaskManager.Instance.ServerDevice;
                        _secondDevice = IPConfigTaskManager.Instance.SecondDesktopDevice;
                        _serverIpconfigBaseline = VirtualOSManager.Instance
                            .GetState(_serverDevice).IpconfigRunCount;
                        _latched[0] = true;
                    }
                    return _latched[0];
                }
            },

            // [1] Input "ipconfig" in the CMD — fires only when ipconfig is newly executed
            //     on the server device after task [0] latched (baseline prevents stale history)
            new TaskEntry
            {
                taskObject    = taskObjects[1],
                originalIndex = 1,
                condition     = () =>
                {
                    if (!_latched[1] && _latched[0])
                    {
                        var s = VirtualOSManager.Instance?.GetState(_serverDevice);
                        if (s != null && s.IpconfigRunCount > _serverIpconfigBaseline)
                            _latched[1] = true;
                    }
                    return _latched[1];
                }
            },

            // [2] Ping the server desktop's own IP address
            new TaskEntry
            {
                taskObject    = taskObjects[2],
                originalIndex = 2,
                condition     = () =>
                {
                    if (!_latched[2] && _latched[1])
                    {
                        var s = VirtualOSManager.Instance?.GetState(_serverDevice);
                        if (s != null && s.PingResults[DeviceIndex(_serverDevice)])
                            _latched[2] = true;
                    }
                    return _latched[2];
                }
            },

            // [3] Observe — auto-latches after the preceding ping task
            new TaskEntry
            {
                taskObject    = taskObjects[3],
                originalIndex = 3,
                condition     = () =>
                {
                    if (!_latched[3] && _latched[2]) _latched[3] = true;
                    return _latched[3];
                }
            },

            // [4] Ping the other Desktop's IP address from the server
            new TaskEntry
            {
                taskObject    = taskObjects[4],
                originalIndex = 4,
                condition     = () =>
                {
                    if (!_latched[4] && _latched[3])
                    {
                        var s = VirtualOSManager.Instance?.GetState(_serverDevice);
                        if (s != null && s.PingResults[DeviceIndex(_secondDevice)])
                            _latched[4] = true;
                    }
                    return _latched[4];
                }
            },

            // [5] Observe
            new TaskEntry
            {
                taskObject    = taskObjects[5],
                originalIndex = 5,
                condition     = () =>
                {
                    if (!_latched[5] && _latched[4]) _latched[5] = true;
                    return _latched[5];
                }
            },

            // [6] Ping the Laptop's IP address from the server
            new TaskEntry
            {
                taskObject    = taskObjects[6],
                originalIndex = 6,
                condition     = () =>
                {
                    if (!_latched[6] && _latched[5])
                    {
                        var s = VirtualOSManager.Instance?.GetState(_serverDevice);
                        if (s != null && s.PingResults[IdxLaptop])
                            _latched[6] = true;
                    }
                    return _latched[6];
                }
            },

            // [7] Observe
            new TaskEntry
            {
                taskObject    = taskObjects[7],
                originalIndex = 7,
                condition     = () =>
                {
                    if (!_latched[7] && _latched[6]) _latched[7] = true;
                    return _latched[7];
                }
            },

            // [8] Ping the Router's IP address from the server
            new TaskEntry
            {
                taskObject    = taskObjects[8],
                originalIndex = 8,
                condition     = () =>
                {
                    if (!_latched[8] && _latched[7])
                    {
                        var s = VirtualOSManager.Instance?.GetState(_serverDevice);
                        if (s != null && s.PingResults[IdxRouter])
                            _latched[8] = true;
                    }
                    return _latched[8];
                }
            },

            // [9] Observe
            new TaskEntry
            {
                taskObject    = taskObjects[9],
                originalIndex = 9,
                condition     = () =>
                {
                    if (!_latched[9] && _latched[8]) _latched[9] = true;
                    return _latched[9];
                }
            },

            // [10] Ping the Access Point's IP address from the server
            new TaskEntry
            {
                taskObject    = taskObjects[10],
                originalIndex = 10,
                condition     = () =>
                {
                    if (!_latched[10] && _latched[9])
                    {
                        var s = VirtualOSManager.Instance?.GetState(_serverDevice);
                        if (s != null && s.PingResults[IdxAP])
                            _latched[10] = true;
                    }
                    return _latched[10];
                }
            },

            // [11] Observe
            new TaskEntry
            {
                taskObject    = taskObjects[11],
                originalIndex = 11,
                condition     = () =>
                {
                    if (!_latched[11] && _latched[10]) _latched[11] = true;
                    return _latched[11];
                }
            },

            // [12] Close the CMD and Server's Virtual OS
            new TaskEntry
            {
                taskObject    = taskObjects[12],
                originalIndex = 12,
                condition     = () =>
                {
                    if (!_latched[12] && _latched[11] &&
                        pingCmdManager != null && !pingCmdManager.gameObject.activeSelf &&
                        VirtualOSManager.Instance != null && !VirtualOSManager.Instance.IsOpen)
                        _latched[12] = true;
                    return _latched[12];
                }
            },

            // ── OTHER DESKTOP FLOW (indices 13–24) ────────────────────────────────────

            // [13] Open CMD on the other Desktop
            new TaskEntry
            {
                taskObject    = taskObjects[13],
                originalIndex = 13,
                condition     = () =>
                {
                    if (!_latched[13] && _latched[12] &&
                        VirtualOSManager.Instance != null &&
                        VirtualOSManager.Instance.IsOpen &&
                        VirtualOSManager.Instance.CurrentDevice == _secondDevice &&
                        pingCmdManager != null && pingCmdManager.gameObject.activeSelf)
                        _latched[13] = true;
                    return _latched[13];
                }
            },

            // [14] Ping the other Desktop's own IP address
            new TaskEntry
            {
                taskObject    = taskObjects[14],
                originalIndex = 14,
                condition     = () =>
                {
                    if (!_latched[14] && _latched[13])
                    {
                        var s = VirtualOSManager.Instance?.GetState(_secondDevice);
                        if (s != null && s.PingResults[DeviceIndex(_secondDevice)])
                            _latched[14] = true;
                    }
                    return _latched[14];
                }
            },

            // [15] Observe
            new TaskEntry
            {
                taskObject    = taskObjects[15],
                originalIndex = 15,
                condition     = () =>
                {
                    if (!_latched[15] && _latched[14]) _latched[15] = true;
                    return _latched[15];
                }
            },

            // [16] Ping the Server Desktop's IP address from the other Desktop
            new TaskEntry
            {
                taskObject    = taskObjects[16],
                originalIndex = 16,
                condition     = () =>
                {
                    if (!_latched[16] && _latched[15])
                    {
                        var s = VirtualOSManager.Instance?.GetState(_secondDevice);
                        if (s != null && s.PingResults[DeviceIndex(_serverDevice)])
                            _latched[16] = true;
                    }
                    return _latched[16];
                }
            },

            // [17] Observe
            new TaskEntry
            {
                taskObject    = taskObjects[17],
                originalIndex = 17,
                condition     = () =>
                {
                    if (!_latched[17] && _latched[16]) _latched[17] = true;
                    return _latched[17];
                }
            },

            // [18] Ping the Laptop's IP address from the other Desktop
            new TaskEntry
            {
                taskObject    = taskObjects[18],
                originalIndex = 18,
                condition     = () =>
                {
                    if (!_latched[18] && _latched[17])
                    {
                        var s = VirtualOSManager.Instance?.GetState(_secondDevice);
                        if (s != null && s.PingResults[IdxLaptop])
                            _latched[18] = true;
                    }
                    return _latched[18];
                }
            },

            // [19] Observe
            new TaskEntry
            {
                taskObject    = taskObjects[19],
                originalIndex = 19,
                condition     = () =>
                {
                    if (!_latched[19] && _latched[18]) _latched[19] = true;
                    return _latched[19];
                }
            },

            // [20] Ping the Router's IP address from the other Desktop
            new TaskEntry
            {
                taskObject    = taskObjects[20],
                originalIndex = 20,
                condition     = () =>
                {
                    if (!_latched[20] && _latched[19])
                    {
                        var s = VirtualOSManager.Instance?.GetState(_secondDevice);
                        if (s != null && s.PingResults[IdxRouter])
                            _latched[20] = true;
                    }
                    return _latched[20];
                }
            },

            // [21] Observe
            new TaskEntry
            {
                taskObject    = taskObjects[21],
                originalIndex = 21,
                condition     = () =>
                {
                    if (!_latched[21] && _latched[20]) _latched[21] = true;
                    return _latched[21];
                }
            },

            // [22] Ping the Access Point's IP address from the other Desktop
            new TaskEntry
            {
                taskObject    = taskObjects[22],
                originalIndex = 22,
                condition     = () =>
                {
                    if (!_latched[22] && _latched[21])
                    {
                        var s = VirtualOSManager.Instance?.GetState(_secondDevice);
                        if (s != null && s.PingResults[IdxAP])
                            _latched[22] = true;
                    }
                    return _latched[22];
                }
            },

            // [23] Observe
            new TaskEntry
            {
                taskObject    = taskObjects[23],
                originalIndex = 23,
                condition     = () =>
                {
                    if (!_latched[23] && _latched[22]) _latched[23] = true;
                    return _latched[23];
                }
            },

            // [24] Close the CMD and other Desktop's Virtual OS
            new TaskEntry
            {
                taskObject    = taskObjects[24],
                originalIndex = 24,
                condition     = () =>
                {
                    if (!_latched[24] && _latched[23] &&
                        pingCmdManager != null && !pingCmdManager.gameObject.activeSelf &&
                        VirtualOSManager.Instance != null && !VirtualOSManager.Instance.IsOpen)
                        _latched[24] = true;
                    return _latched[24];
                }
            },

            // ── LAPTOP FLOW (indices 25–36) ────────────────────────────────────────────

            // [25] Open CMD on the Laptop
            new TaskEntry
            {
                taskObject    = taskObjects[25],
                originalIndex = 25,
                condition     = () =>
                {
                    if (!_latched[25] && _latched[24] &&
                        VirtualOSManager.Instance != null &&
                        VirtualOSManager.Instance.IsOpen &&
                        VirtualOSManager.Instance.CurrentDevice == DeviceID.Laptop &&
                        pingCmdManager != null && pingCmdManager.gameObject.activeSelf)
                        _latched[25] = true;
                    return _latched[25];
                }
            },

            // [26] Ping the Laptop's own IP address
            new TaskEntry
            {
                taskObject    = taskObjects[26],
                originalIndex = 26,
                condition     = () =>
                {
                    if (!_latched[26] && _latched[25])
                    {
                        var s = VirtualOSManager.Instance?.GetState(DeviceID.Laptop);
                        if (s != null && s.PingResults[IdxLaptop])
                            _latched[26] = true;
                    }
                    return _latched[26];
                }
            },

            // [27] Observe
            new TaskEntry
            {
                taskObject    = taskObjects[27],
                originalIndex = 27,
                condition     = () =>
                {
                    if (!_latched[27] && _latched[26]) _latched[27] = true;
                    return _latched[27];
                }
            },

            // [28] Ping the Server Desktop's IP address from the Laptop
            new TaskEntry
            {
                taskObject    = taskObjects[28],
                originalIndex = 28,
                condition     = () =>
                {
                    if (!_latched[28] && _latched[27])
                    {
                        var s = VirtualOSManager.Instance?.GetState(DeviceID.Laptop);
                        if (s != null && s.PingResults[DeviceIndex(_serverDevice)])
                            _latched[28] = true;
                    }
                    return _latched[28];
                }
            },

            // [29] Observe
            new TaskEntry
            {
                taskObject    = taskObjects[29],
                originalIndex = 29,
                condition     = () =>
                {
                    if (!_latched[29] && _latched[28]) _latched[29] = true;
                    return _latched[29];
                }
            },

            // [30] Ping the other Desktop's IP address from the Laptop
            new TaskEntry
            {
                taskObject    = taskObjects[30],
                originalIndex = 30,
                condition     = () =>
                {
                    if (!_latched[30] && _latched[29])
                    {
                        var s = VirtualOSManager.Instance?.GetState(DeviceID.Laptop);
                        if (s != null && s.PingResults[DeviceIndex(_secondDevice)])
                            _latched[30] = true;
                    }
                    return _latched[30];
                }
            },

            // [31] Observe
            new TaskEntry
            {
                taskObject    = taskObjects[31],
                originalIndex = 31,
                condition     = () =>
                {
                    if (!_latched[31] && _latched[30]) _latched[31] = true;
                    return _latched[31];
                }
            },

            // [32] Ping the Router's IP address from the Laptop
            new TaskEntry
            {
                taskObject    = taskObjects[32],
                originalIndex = 32,
                condition     = () =>
                {
                    if (!_latched[32] && _latched[31])
                    {
                        var s = VirtualOSManager.Instance?.GetState(DeviceID.Laptop);
                        if (s != null && s.PingResults[IdxRouter])
                            _latched[32] = true;
                    }
                    return _latched[32];
                }
            },

            // [33] Observe
            new TaskEntry
            {
                taskObject    = taskObjects[33],
                originalIndex = 33,
                condition     = () =>
                {
                    if (!_latched[33] && _latched[32]) _latched[33] = true;
                    return _latched[33];
                }
            },

            // [34] Ping the Access Point's IP address from the Laptop
            new TaskEntry
            {
                taskObject    = taskObjects[34],
                originalIndex = 34,
                condition     = () =>
                {
                    if (!_latched[34] && _latched[33])
                    {
                        var s = VirtualOSManager.Instance?.GetState(DeviceID.Laptop);
                        if (s != null && s.PingResults[IdxAP])
                            _latched[34] = true;
                    }
                    return _latched[34];
                }
            },

            // [35] Observe
            new TaskEntry
            {
                taskObject    = taskObjects[35],
                originalIndex = 35,
                condition     = () =>
                {
                    if (!_latched[35] && _latched[34]) _latched[35] = true;
                    return _latched[35];
                }
            },

            // [36] Close the CMD and Laptop's Virtual OS
            new TaskEntry
            {
                taskObject    = taskObjects[36],
                originalIndex = 36,
                condition     = () =>
                {
                    if (!_latched[36] && _latched[35] &&
                        pingCmdManager != null && !pingCmdManager.gameObject.activeSelf &&
                        VirtualOSManager.Instance != null && !VirtualOSManager.Instance.IsOpen)
                        _latched[36] = true;
                    return _latched[36];
                }
            },
        };
    }

    // ── Public API (ITaskCategory) ────────────────────────────────────────────────────

    public string GetNextIncompleteTaskText()
    {
        if (_displayOverride != null) return _displayOverride;
        if (_tasks == null || _tasks.Count == 0) return null;
        var next = _tasks.FirstOrDefault(t => !t.isCompleted);
        if (next == null) return null;
        var tmp = next.taskObject.GetComponent<TextMeshProUGUI>();
        return tmp != null ? tmp.text : null;
    }

    public Color GetDisplayColor(Color fallback) =>
        (_isCompletionOverride && _displayOverride != null) ? Color.green : fallback;

    public void HideCompletionBanner()
    {
        if (allTasksCompletedText != null)
            allTasksCompletedText.gameObject.SetActive(false);
    }

    public static void CheckConditions()
    {
        if (Instance == null) return;
        Instance.EvaluateConditions();
    }

    // ── Evaluation ────────────────────────────────────────────────────────────────────

    private void Update() => EvaluateConditions();

    private void EvaluateConditions()
    {
        if (_tasks == null || !gameObject.activeInHierarchy) return;

        foreach (var task in _tasks)
        {
            if (task.isFlashing) continue;

            if (!task.isCompleted)
            {
                if (!task.taskObject.activeSelf) continue;

                if (task.condition())
                {
                    task.isCompleted = true;
                    StartCoroutine(FlashAndComplete(task));
                }
            }
            else
            {
                if (!task.condition())
                {
                    task.isCompleted = false;
                    task.taskObject.transform.SetParent(taskParent, false);
                    task.taskObject.transform.SetSiblingIndex(task.originalIndex);
                    var revertTmp = task.taskObject.GetComponent<TextMeshProUGUI>();
                    if (revertTmp != null) revertTmp.color = GoldColor;
                    task.taskObject.SetActive(false);
                    RefreshWindow();
                    if (task.taskObject.activeSelf)
                        StartCoroutine(FlashRevert(task));
                }
            }
        }
    }

    // ── Window ────────────────────────────────────────────────────────────────────────

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

    // ── Coroutines ────────────────────────────────────────────────────────────────────

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

    // ── Completion ────────────────────────────────────────────────────────────────────

    private void ShowAllTasksCompleted()
    {
        _displayOverride      = "All tasks completed!";
        _isCompletionOverride = true;

        if (allTasksCompletedText != null)
        {
            allTasksCompletedText.text  = _displayOverride;
            allTasksCompletedText.color = Color.green;
            allTasksCompletedText.gameObject.SetActive(true);
        }

        OnTasksUpdated?.Invoke();

        if (topicManagerCompletionIndex >= 0)
            TopicManager.Instance?.MarkTopicComplete(topicManagerCompletionIndex);

        Debug.Log("[PingCmdTaskManager] All 37 Ping Test tasks complete.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────────

    // Maps a DeviceID to its index in DeviceOSState.PingResults[5].
    // Must match PingCmdManager's IdxComputer1 / IdxComputer2 / IdxLaptop constants.
    private static int DeviceIndex(DeviceID d) => d switch
    {
        DeviceID.Computer1 => IdxComputer1,
        DeviceID.Computer2 => IdxComputer2,
        DeviceID.Laptop    => IdxLaptop,
        _                  => -1
    };
}
