/*
 * ================================================================
 *  UNITY SETUP GUIDE — NetworkCableTaskManager  (COC II, Topic 1)
 * ================================================================
 *
 *  PURPOSE
 *    Drives the 26-task "Install Network Cable" sequence for COC II.
 *    Matches the T3TaskListManager pattern: 3-task sliding window,
 *    flash-on-complete, flash-on-revert, SingleTaskDisplay event.
 *
 *  ALL TASKS USE LATCHES — once a condition is met, the task never
 *  reverts even if the underlying state changes (e.g. Phase-2 actions
 *  resetting Phase-1 wire state). This keeps completed tasks stable.
 *
 *  TASKS
 *    Phase 1 — Straight-through cable
 *    [ 0] Deploy the UTP Cable to the Workspace
 *    [ 1] Right-click to enter "Detail View" to start the configuration
 *    [ 2] Use the Wire Stripper tool to cut the cable jacket
 *    [ 3] Arrange the color sequence: create a Straight-through (T568A / T568B)
 *    [ 4] Install RJ45 to the cable
 *    [ 5] Use Crimping tool to crimp the RJ45 to the cable wires
 *    [ 6] Do the same configuration to the other end of the same cable
 *    [ 7] Exit detail view and store the cable back to the storage area
 *    [ 8] Deploy LAN Tester to the workspace
 *    [ 9] Install the network cable to the LAN tester port
 *    [10] Turn on the power switch of the LAN tester
 *    [11] Observe the LAN tester LED sequence to see if it matches the Straight-through
 *    [12] Turn off the power and unplug the Straight-through cable
 *    Phase 2 — Crossover cable
 *    [13] Deploy the Straight-through cable to the workspace
 *    [14] Right-click to enter "Detail View" to start the configuration
 *    [15] Use the Crimping tool to cut the straight-through
 *    [16] Use Wire Stripper to cut the cable jacket
 *    [17] Arrange the color sequence: create a Cross-over (T568A <--> T568B)
 *    [18] Install RJ45 to the cable
 *    [19] Use Crimping tool to crimp the RJ45 to the cable wires
 *    [20] Do the same configuration to the other end of the same cable
 *    [21] Exit detail view and store the cable back to the storage area
 *    [22] Deploy LAN Tester to the workspace
 *    [23] Install the network cable to the LAN tester port
 *    [24] Turn on the power switch of the LAN tester
 *    [25] Observe the LAN tester LED sequence to see if it matches the Cross-over
 *
 *  INSPECTOR SETUP
 *    Task UI
 *      taskParent          → Vertical Layout Group parent for active tasks
 *      finishedParent      → off-screen parent for completed task objects
 *      taskObjects[0..25]  → 26 TMP text GameObjects in the order above
 *    Network Cable
 *      networkCableHolder  → NetworkHardwareHolder on the UTP cable icon proxy
 *      cableEnd1           → NetworkCableEndController on CableEnd1
 *      cableEnd2           → NetworkCableEndController on CableEnd2
 *    LAN Tester
 *      lanTesterHolder     → NetworkHardwareHolder on the LAN tester icon proxy
 *      lanTesterPort       → LanTesterPortController
 *      lanTesterSwitch     → LanTesterSwitchController
 *      masterLEDPanel      → LanTesterLEDDisplay (Port1 / master panel)
 *    Completion UI
 *      allTasksCompletedText → (optional) TMP shown after all 26 tasks done
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

    // Fires whenever a task completes or reverts — NetworkCableTaskDisplay subscribes.
    public static event Action OnTasksUpdated;

    private static readonly Color GoldColor = new Color(1f, 0.843f, 0f);

    // T568 standards for straight-through / crossover identification.
    private static readonly int[] T568A = { 0, 1, 2, 3, 4, 5, 6, 7 };
    private static readonly int[] T568B = { 2, 5, 0, 3, 4, 1, 6, 7 };

    // ── Inspector ──────────────────────────────────────────────────────────────────────

    [Header("Task UI")]
    [SerializeField] private Transform  taskParent;
    [SerializeField] private Transform  finishedParent;
    [Tooltip("Exactly 26 task GameObjects in the order listed in the header comment.")]
    [SerializeField] private GameObject[] taskObjects;

    [Header("Network Cable")]
    [SerializeField] private NetworkHardwareHolder     networkCableHolder;
    [SerializeField] private NetworkCableEndController cableEnd1;
    [SerializeField] private NetworkCableEndController cableEnd2;

    [Header("LAN Tester")]
    [SerializeField] private NetworkHardwareHolder     lanTesterHolder;
    [SerializeField] private LanTesterPortController   lanTesterPort;
    [SerializeField] private LanTesterSwitchController lanTesterSwitch;

    [Header("Completion UI")]
    [Tooltip("(Optional) TMP shown after all 26 tasks are complete.")]
    [SerializeField] private TextMeshProUGUI allTasksCompletedText;

    // ── State ──────────────────────────────────────────────────────────────────────────

    // One latch per task. Once set true the condition permanently returns true,
    // preventing reversion when Phase-2 actions undo Phase-1 hardware state.
    private readonly bool[] _latched = new bool[26];

    // Set true when the cable passes through storage (icon proxy visible) after Task 13
    // completes. Guards Task 14 from firing during the drag-from-port phase, where the
    // cable is active in the world but has not yet been returned to the hardware area.
    private bool _cableStoredAfterT13 = false;

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
        if (taskObjects == null || taskObjects.Length < 26)
        {
            Debug.LogError("[NetworkCableTaskManager] Assign all 26 task objects in the inspector.");
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

        LanTesterLEDDisplay.OnSequenceComplete += OnLEDSequenceComplete;
    }

    private void OnDestroy()
    {
        LanTesterLEDDisplay.OnSequenceComplete -= OnLEDSequenceComplete;
    }

    // ── Task Definitions ───────────────────────────────────────────────────────────────

    private void BuildTasks()
    {
        _tasks = new List<TaskEntry>
        {
            // ── PHASE 1: Straight-through ──────────────────────────────────────────────

            // Task 1: Deploy the UTP Cable to the Workspace
            new TaskEntry
            {
                taskObject    = taskObjects[0],
                originalIndex = 0,
                condition     = () =>
                {
                    if (!_latched[0] &&
                        networkCableHolder?.hardwarePrefab != null &&
                        networkCableHolder.hardwarePrefab.activeSelf)
                        _latched[0] = true;
                    return _latched[0];
                }
            },

            // Task 2: Right-click to enter "Detail View" to start the configuration
            new TaskEntry
            {
                taskObject    = taskObjects[1],
                originalIndex = 1,
                condition     = () =>
                {
                    if (!_latched[1] && _latched[0] &&
                        GameManager.Instance != null && GameManager.Instance.IsEditorOpen)
                        _latched[1] = true;
                    return _latched[1];
                }
            },

            // Task 3: Use the Wire Stripper tool to cut the cable jacket (at least one end stripped)
            new TaskEntry
            {
                taskObject    = taskObjects[2],
                originalIndex = 2,
                condition     = () =>
                {
                    if (!_latched[2] &&
                        ((cableEnd1 != null && cableEnd1.IsStripped) ||
                         (cableEnd2 != null && cableEnd2.IsStripped)))
                        _latched[2] = true;
                    return _latched[2];
                }
            },

            // Task 4: Arrange color sequence — at least one stripped end matches T568A or T568B
            new TaskEntry
            {
                taskObject    = taskObjects[3],
                originalIndex = 3,
                condition     = () =>
                {
                    if (!_latched[3] &&
                        ((cableEnd1 != null && cableEnd1.IsStripped && cableEnd1.IsWireOrderCorrect()) ||
                         (cableEnd2 != null && cableEnd2.IsStripped && cableEnd2.IsWireOrderCorrect())))
                        _latched[3] = true;
                    return _latched[3];
                }
            },

            // Task 5: Install RJ45 to the cable (at least one end)
            new TaskEntry
            {
                taskObject    = taskObjects[4],
                originalIndex = 4,
                condition     = () =>
                {
                    if (!_latched[4] &&
                        ((cableEnd1 != null && cableEnd1.IsRJ45Installed) ||
                         (cableEnd2 != null && cableEnd2.IsRJ45Installed)))
                        _latched[4] = true;
                    return _latched[4];
                }
            },

            // Task 6: Use Crimping tool to crimp the RJ45 (at least one end)
            new TaskEntry
            {
                taskObject    = taskObjects[5],
                originalIndex = 5,
                condition     = () =>
                {
                    if (!_latched[5] &&
                        ((cableEnd1 != null && cableEnd1.IsCrimped) ||
                         (cableEnd2 != null && cableEnd2.IsCrimped)))
                        _latched[5] = true;
                    return _latched[5];
                }
            },

            // Task 7: Both ends fully configured and cable forms a valid Straight-through
            new TaskEntry
            {
                taskObject    = taskObjects[6],
                originalIndex = 6,
                condition     = () =>
                {
                    if (!_latched[6] &&
                        cableEnd1 != null && cableEnd1.IsCrimped &&
                        cableEnd2 != null && cableEnd2.IsCrimped &&
                        IsStraightThrough())
                        _latched[6] = true;
                    return _latched[6];
                }
            },

            // Task 8: Exit detail view and store the cable back to the storage area
            new TaskEntry
            {
                taskObject    = taskObjects[7],
                originalIndex = 7,
                condition     = () =>
                {
                    // _latched[6] guard prevents false positive from initial scene state
                    // (cable is in storage before task 1 deploys it).
                    if (!_latched[7] && _latched[6] &&
                        networkCableHolder != null && networkCableHolder.IsAvailable())
                        _latched[7] = true;
                    return _latched[7];
                }
            },

            // Task 9: Deploy LAN Tester to the workspace
            new TaskEntry
            {
                taskObject    = taskObjects[8],
                originalIndex = 8,
                condition     = () =>
                {
                    if (!_latched[8] &&
                        lanTesterHolder?.hardwarePrefab != null &&
                        lanTesterHolder.hardwarePrefab.activeSelf)
                        _latched[8] = true;
                    return _latched[8];
                }
            },

            // Task 10: Install the network cable to the LAN tester port
            new TaskEntry
            {
                taskObject    = taskObjects[9],
                originalIndex = 9,
                condition     = () =>
                {
                    if (!_latched[9] && lanTesterPort != null && lanTesterPort.IsCableInstalled)
                        _latched[9] = true;
                    return _latched[9];
                }
            },

            // Task 11: Turn on the power switch of the LAN tester
            new TaskEntry
            {
                taskObject    = taskObjects[10],
                originalIndex = 10,
                condition     = () =>
                {
                    if (!_latched[10] && lanTesterSwitch != null && lanTesterSwitch.IsOn)
                        _latched[10] = true;
                    return _latched[10];
                }
            },

            // Task 12: Observe LED sequence — straight-through confirmed after a full cycle.
            // Latch is set externally by OnLEDSequenceComplete when IsStraightThrough() is true.
            new TaskEntry
            {
                taskObject    = taskObjects[11],
                originalIndex = 11,
                condition     = () => _latched[11]
            },

            // Task 13: Turn off the power AND unplug the Straight-through cable from the tester
            new TaskEntry
            {
                taskObject    = taskObjects[12],
                originalIndex = 12,
                condition     = () =>
                {
                    if (!_latched[12] && _latched[11] &&
                        lanTesterSwitch != null && !lanTesterSwitch.IsOn &&
                        lanTesterPort   != null && !lanTesterPort.IsCableInstalled)
                        _latched[12] = true;
                    return _latched[12];
                }
            },

            // ── PHASE 2: Crossover ─────────────────────────────────────────────────────

            // Task 14: Deploy the Straight-through cable to the workspace
            // Requires the cable to first return to storage (icon proxy visible) after Task 13,
            // so that the drag-from-LAN-tester-port phase does not falsely complete this task.
            new TaskEntry
            {
                taskObject    = taskObjects[13],
                originalIndex = 13,
                condition     = () =>
                {
                    if (!_latched[13] && _latched[12])
                    {
                        // Step 1 — wait for cable to reach storage (icon proxy showing).
                        if (!_cableStoredAfterT13 &&
                            networkCableHolder != null && networkCableHolder.IsAvailable())
                            _cableStoredAfterT13 = true;

                        // Step 2 — only then count a re-deploy as completing this task.
                        if (_cableStoredAfterT13 &&
                            networkCableHolder?.hardwarePrefab != null &&
                            networkCableHolder.hardwarePrefab.activeSelf)
                            _latched[13] = true;
                    }
                    return _latched[13];
                }
            },

            // Task 15: Right-click to enter "Detail View" to start the configuration
            new TaskEntry
            {
                taskObject    = taskObjects[14],
                originalIndex = 14,
                condition     = () =>
                {
                    if (!_latched[14] && _latched[13] &&
                        GameManager.Instance != null && GameManager.Instance.IsEditorOpen)
                        _latched[14] = true;
                    return _latched[14];
                }
            },

            // Task 16: Use the Crimping tool to cut the straight-through
            // Detected when at least one previously-stripped (Phase-1) end is now reset (IsStripped=false).
            // StripCycleCount >= 1 guards against false positive from initial unstripped state.
            new TaskEntry
            {
                taskObject    = taskObjects[15],
                originalIndex = 15,
                condition     = () =>
                {
                    if (!_latched[15] && _latched[14] &&
                        ((cableEnd1 != null && !cableEnd1.IsStripped && cableEnd1.StripCycleCount >= 1) ||
                         (cableEnd2 != null && !cableEnd2.IsStripped && cableEnd2.StripCycleCount >= 1)))
                        _latched[15] = true;
                    return _latched[15];
                }
            },

            // Task 17: Use Wire Stripper to cut the cable jacket (Phase-2 re-strip, cycle >= 2)
            new TaskEntry
            {
                taskObject    = taskObjects[16],
                originalIndex = 16,
                condition     = () =>
                {
                    if (!_latched[16] && _latched[15] &&
                        ((cableEnd1 != null && cableEnd1.IsStripped && cableEnd1.StripCycleCount >= 2) ||
                         (cableEnd2 != null && cableEnd2.IsStripped && cableEnd2.StripCycleCount >= 2)))
                        _latched[16] = true;
                    return _latched[16];
                }
            },

            // Task 18: Arrange color sequence — at least one Phase-2 stripped end has correct order
            new TaskEntry
            {
                taskObject    = taskObjects[17],
                originalIndex = 17,
                condition     = () =>
                {
                    if (!_latched[17] && _latched[16] &&
                        ((cableEnd1 != null && cableEnd1.StripCycleCount >= 2 && cableEnd1.IsStripped && cableEnd1.IsWireOrderCorrect()) ||
                         (cableEnd2 != null && cableEnd2.StripCycleCount >= 2 && cableEnd2.IsStripped && cableEnd2.IsWireOrderCorrect())))
                        _latched[17] = true;
                    return _latched[17];
                }
            },

            // Task 19: Install RJ45 to the cable (Phase-2 end)
            new TaskEntry
            {
                taskObject    = taskObjects[18],
                originalIndex = 18,
                condition     = () =>
                {
                    if (!_latched[18] && _latched[17] &&
                        ((cableEnd1 != null && cableEnd1.StripCycleCount >= 2 && cableEnd1.IsRJ45Installed) ||
                         (cableEnd2 != null && cableEnd2.StripCycleCount >= 2 && cableEnd2.IsRJ45Installed)))
                        _latched[18] = true;
                    return _latched[18];
                }
            },

            // Task 20: Use Crimping tool to crimp the RJ45 (Phase-2 end)
            new TaskEntry
            {
                taskObject    = taskObjects[19],
                originalIndex = 19,
                condition     = () =>
                {
                    if (!_latched[19] && _latched[18] &&
                        ((cableEnd1 != null && cableEnd1.StripCycleCount >= 2 && cableEnd1.IsCrimped) ||
                         (cableEnd2 != null && cableEnd2.StripCycleCount >= 2 && cableEnd2.IsCrimped)))
                        _latched[19] = true;
                    return _latched[19];
                }
            },

            // Task 21: Both ends Phase-2 configured and cable forms a valid Crossover
            new TaskEntry
            {
                taskObject    = taskObjects[20],
                originalIndex = 20,
                condition     = () =>
                {
                    if (!_latched[20] &&
                        cableEnd1 != null && cableEnd1.StripCycleCount >= 2 && cableEnd1.IsCrimped &&
                        cableEnd2 != null && cableEnd2.StripCycleCount >= 2 && cableEnd2.IsCrimped &&
                        IsCrossover())
                        _latched[20] = true;
                    return _latched[20];
                }
            },

            // Task 22: Exit detail view and store the cable back to the storage area
            new TaskEntry
            {
                taskObject    = taskObjects[21],
                originalIndex = 21,
                condition     = () =>
                {
                    if (!_latched[21] && _latched[20] &&
                        networkCableHolder != null && networkCableHolder.IsAvailable())
                        _latched[21] = true;
                    return _latched[21];
                }
            },

            // Task 23: Deploy LAN Tester to the workspace
            // If the tester was never stored after Phase 1, this auto-completes when it becomes visible.
            new TaskEntry
            {
                taskObject    = taskObjects[22],
                originalIndex = 22,
                condition     = () =>
                {
                    if (!_latched[22] && _latched[21] &&
                        lanTesterHolder?.hardwarePrefab != null &&
                        lanTesterHolder.hardwarePrefab.activeSelf)
                        _latched[22] = true;
                    return _latched[22];
                }
            },

            // Task 24: Install the network cable to the LAN tester port
            new TaskEntry
            {
                taskObject    = taskObjects[23],
                originalIndex = 23,
                condition     = () =>
                {
                    if (!_latched[23] && _latched[22] &&
                        lanTesterPort != null && lanTesterPort.IsCableInstalled)
                        _latched[23] = true;
                    return _latched[23];
                }
            },

            // Task 25: Turn on the power switch of the LAN tester
            new TaskEntry
            {
                taskObject    = taskObjects[24],
                originalIndex = 24,
                condition     = () =>
                {
                    if (!_latched[24] && _latched[23] &&
                        lanTesterSwitch != null && lanTesterSwitch.IsOn)
                        _latched[24] = true;
                    return _latched[24];
                }
            },

            // Task 26: Observe LED sequence — crossover confirmed after a full cycle.
            // Latch is set externally by OnLEDSequenceComplete when IsCrossover() is true.
            new TaskEntry
            {
                taskObject    = taskObjects[25],
                originalIndex = 25,
                condition     = () => _latched[25]
            },
        };
    }

    // ── LED Sequence Handler ───────────────────────────────────────────────────────────

    private void OnLEDSequenceComplete()
    {
        // Task 12 — straight-through confirmed after Phase-1 test
        if (!_latched[11] && _latched[10] &&
            lanTesterPort != null && lanTesterPort.IsCableInstalled &&
            IsStraightThrough())
        {
            _latched[11] = true;
            EvaluateConditions();
        }

        // Task 26 — crossover confirmed after Phase-2 test
        if (!_latched[25] && _latched[24] &&
            lanTesterPort != null && lanTesterPort.IsCableInstalled &&
            IsCrossover())
        {
            _latched[25] = true;
            EvaluateConditions();
        }
    }

    // ── Cable Type Helpers ────────────────────────────────────────────────────────────

    private bool EndMatchesStandard(NetworkCableEndController end, int[] standard)
    {
        for (int i = 0; i < 8; i++)
            if (end.GetWireColorAtSlot(i) != standard[i]) return false;
        return true;
    }

    private bool IsStraightThrough()
    {
        if (cableEnd1 == null || cableEnd2 == null) return false;
        return (EndMatchesStandard(cableEnd1, T568A) && EndMatchesStandard(cableEnd2, T568A)) ||
               (EndMatchesStandard(cableEnd1, T568B) && EndMatchesStandard(cableEnd2, T568B));
    }

    private bool IsCrossover()
    {
        if (cableEnd1 == null || cableEnd2 == null) return false;
        return (EndMatchesStandard(cableEnd1, T568A) && EndMatchesStandard(cableEnd2, T568B)) ||
               (EndMatchesStandard(cableEnd1, T568B) && EndMatchesStandard(cableEnd2, T568A));
    }

    // ── Public API ────────────────────────────────────────────────────────────────────

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
                // Only evaluate tasks currently visible in the 3-task window.
                if (!task.taskObject.activeSelf) continue;

                if (task.condition())
                {
                    task.isCompleted = true;
                    StartCoroutine(FlashAndComplete(task));
                }
            }
            else
            {
                // All conditions are latched, so this branch is structural parity with COC I managers.
                // Latched conditions always return true — completed tasks never revert in practice.
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
        TopicManager.Instance?.MarkTopicComplete(1); // Adjust index to match COC II's slot in TopicManager.
        Debug.Log("[NetworkCableTaskManager] All 26 tasks complete — Install Network Cable done.");
    }
}
