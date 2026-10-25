/*
 * ================================================================
 *  UNITY SETUP GUIDE — PingCmdTaskManager  (COC II, Topic 3)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to an always-active "Managers" GameObject in the COC II scene.
 *
 *  ── HIERARCHY ────────────────────────────────────────────────────
 *
 *    Task Panel (PingCmdPanel)
 *      ├── Group Title Row                   (always visible above task list)
 *      │     ├── GroupTitleTMP               TMP_Text  → groupTitleTMP
 *      │     └── Button component            Button  → groupTitleBtn
 *      │           OnClick → opens Group Overview Popup
 *      │
 *      ├── Task List                         (active task rows parented here)
 *      │     └── [taskObjects[0..36] reparented here at runtime]
 *      │
 *      └── Finished Bucket                  (off-screen; latched tasks move here)
 *
 *    Group Overview Popup                   → groupOverviewPopup (INACTIVE)
 *      ├── Row Parent                       → groupRowParent  (VerticalLayoutGroup)
 *      │     └── [GroupRowPrefab × 3 spawned here at runtime]
 *      └── CloseBtn                         Button  → closePopupBtn
 *
 *  ── GROUP ROW PREFAB ─────────────────────────────────────────────
 *    Root: any layout container (e.g. HorizontalLayoutGroup)
 *      ├── NameTMP      TMP_Text  [0] — group title
 *      ├── ProgressTMP  TMP_Text  [1] — "2 / 13"
 *      └── StatusTMP    TMP_Text  [2] — "Pending" / "Active" / "Done"
 *    GetComponentsInChildren<TMP_Text>() must return them in order [0][1][2].
 *
 *  ── TASK GROUPS — 3 groups, 37 tasks total ───────────────────────
 *    Group 0  "Server Desktop — CMD Ping Test"  tasks  0 – 12  (13 tasks)
 *    Group 1  "Other Desktop — CMD Ping Test"   tasks 13 – 24  (12 tasks)
 *    Group 2  "Laptop — CMD Ping Test"          tasks 25 – 36  (12 tasks)
 *
 *  ── INSPECTOR ASSIGNMENTS ────────────────────────────────────────
 *    taskParent           → VerticalLayoutGroup parent for active task rows
 *    finishedParent       → Off-screen parent for completed task GameObjects
 *    taskObjects[0..36]   → Task row GameObjects (TMP_Text labels)
 *    allTasksCompletedText→ (optional) GameObject shown when all tasks done
 *
 *    groupTitleTMP        → TMP_Text above the task list
 *    groupTitleBtn        → Button on / wrapping the group title TMP
 *    flashColor           → Color flashed on group complete (default green)
 *    flashDuration        → Seconds the flash lasts (default 1)
 *    cocLabel             → e.g. "COC II - Ping Test"
 *
 *    groupOverviewPopup   → Popup panel GameObject (starts INACTIVE)
 *    groupRowParent       → Transform inside popup where rows are spawned
 *    groupRowPrefab       → Prefab for one popup row (3 TMP children)
 *    closePopupBtn        → Button to close the popup
 *    groupPendingColor    → Row color for groups not yet started
 *    groupActiveColor     → Row color for the current active group
 *    groupDoneColor       → Row color for completed groups
 *
 *    taskGroups[]         → TaskGroup array (3 entries, configure in Inspector)
 *    topicManagerCompletionIndex → -1 to disable TopicManager call on finish
 *
 *    pingCmdManager       → PingCmdManager on the CMD Panel inside Virtual OS
 *
 *  ── PREREQUISITES ────────────────────────────────────────────────
 *    Requires IPConfigTaskManager.IsFullyComplete before task 0 can latch.
 *    _serverDevice and _secondDevice are read from IPConfigTaskManager at
 *    the moment task 0 latches.
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
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PingCmdTaskManager : MonoBehaviour, ITaskCategory
{
    public static PingCmdTaskManager Instance { get; private set; }

    public static event System.Action OnTasksUpdated;

    private const int TASK_COUNT = 37;

    // PingResults indices — must match PingCmdManager constants.
    private const int IdxRouter    = 0;
    private const int IdxAP        = 1;
    private const int IdxComputer1 = 2;
    private const int IdxComputer2 = 3;
    private const int IdxLaptop    = 4;

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
    [SerializeField] private string   cocLabel      = "COC II - Ping Test";

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

    // ── Completion Routing ────────────────────────────────────────────────────

    [Header("Completion Routing")]
    [Tooltip("Index passed to TopicManager.MarkTopicComplete() on full completion. -1 = disabled.")]
    [SerializeField] private int topicManagerCompletionIndex = -1;

    // ── CMD Reference ─────────────────────────────────────────────────────────

    [Header("CMD Reference")]
    [SerializeField] private PingCmdManager pingCmdManager;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private bool[]       _latched           = new bool[TASK_COUNT];
    private int          _currentTask       = 0;
    private int          _currentGroupIndex = 0;
    private bool         _allDone           = false;
    private bool         _isFlashing        = false;
    private TMP_Text[][] _popupRowTMPs;

    private static readonly Color DefaultTaskColor  = new Color(1.000f, 0.886f, 0.596f);
    private static readonly Color DefaultTitleColor = new Color(0.953f, 0.839f, 0.020f);

    private Queue<int>   _taskFlashQueue = new Queue<int>();
    private bool         _isTaskFlashing = false;

    // Captured at task [0] latch — identifies which desktop is server and which is second.
    private DeviceID _serverDevice;
    private DeviceID _secondDevice;

    // Baseline ipconfig run count captured when task [0] latches.
    // Task [1] fires only when count exceeds baseline, preventing stale CMD history.
    private int _serverIpconfigBaseline;

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
        for (int i = 0; i < TASK_COUNT; i++)
        {
            if (taskObjects[i] == null) continue;
            var tmp = taskObjects[i].GetComponent<TMP_Text>();
            if (tmp != null) tmp.color = DefaultTaskColor;
        }
        if (groupTitleTMP != null) groupTitleTMP.color = DefaultTitleColor;

        groupOverviewPopup?.SetActive(false);
        BuildPopupRows();
        RefreshGroupTitle();
        RefreshPopup();
        RefreshWindow();
    }

    private void OnEnable()
    {
        RefreshGroupTitle();
        if (groupOverviewPopup != null && groupOverviewPopup.activeSelf)
            RefreshPopup();
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
            // ── Group 0: Server Desktop — CMD Ping Test ───────────────────────
            case 0:
                return IPConfigTaskManager.Instance != null &&
                       IPConfigTaskManager.Instance.IsFullyComplete &&
                       VirtualOSManager.Instance != null &&
                       VirtualOSManager.Instance.IsOpen &&
                       (VirtualOSManager.Instance.CurrentDevice == DeviceID.Computer1 ||
                        VirtualOSManager.Instance.CurrentDevice == DeviceID.Computer2) &&
                       pingCmdManager != null && pingCmdManager.gameObject.activeSelf;
            case 1: {
                var s = VirtualOSManager.Instance?.GetState(_serverDevice);
                return s != null && s.IpconfigRunCount > _serverIpconfigBaseline;
            }
            case 2: {
                var s = VirtualOSManager.Instance?.GetState(_serverDevice);
                return s != null && s.PingResults[DeviceIndex(_serverDevice)];
            }
            case 3:  return true; // observe — auto-latches once task 2 is done
            case 4: {
                var s = VirtualOSManager.Instance?.GetState(_serverDevice);
                return s != null && s.PingResults[DeviceIndex(_secondDevice)];
            }
            case 5:  return true; // observe
            case 6: {
                var s = VirtualOSManager.Instance?.GetState(_serverDevice);
                return s != null && s.PingResults[IdxLaptop];
            }
            case 7:  return true; // observe
            case 8: {
                var s = VirtualOSManager.Instance?.GetState(_serverDevice);
                return s != null && s.PingResults[IdxRouter];
            }
            case 9:  return true; // observe
            case 10: {
                var s = VirtualOSManager.Instance?.GetState(_serverDevice);
                return s != null && s.PingResults[IdxAP];
            }
            case 11: return true; // observe
            case 12:
                return pingCmdManager != null && !pingCmdManager.gameObject.activeSelf &&
                       VirtualOSManager.Instance != null && !VirtualOSManager.Instance.IsOpen;

            // ── Group 1: Other Desktop — CMD Ping Test ────────────────────────
            case 13:
                return VirtualOSManager.Instance != null &&
                       VirtualOSManager.Instance.IsOpen &&
                       VirtualOSManager.Instance.CurrentDevice == _secondDevice &&
                       pingCmdManager != null && pingCmdManager.gameObject.activeSelf;
            case 14: {
                var s = VirtualOSManager.Instance?.GetState(_secondDevice);
                return s != null && s.PingResults[DeviceIndex(_secondDevice)];
            }
            case 15: return true; // observe
            case 16: {
                var s = VirtualOSManager.Instance?.GetState(_secondDevice);
                return s != null && s.PingResults[DeviceIndex(_serverDevice)];
            }
            case 17: return true; // observe
            case 18: {
                var s = VirtualOSManager.Instance?.GetState(_secondDevice);
                return s != null && s.PingResults[IdxLaptop];
            }
            case 19: return true; // observe
            case 20: {
                var s = VirtualOSManager.Instance?.GetState(_secondDevice);
                return s != null && s.PingResults[IdxRouter];
            }
            case 21: return true; // observe
            case 22: {
                var s = VirtualOSManager.Instance?.GetState(_secondDevice);
                return s != null && s.PingResults[IdxAP];
            }
            case 23: return true; // observe
            case 24:
                return pingCmdManager != null && !pingCmdManager.gameObject.activeSelf &&
                       VirtualOSManager.Instance != null && !VirtualOSManager.Instance.IsOpen;

            // ── Group 2: Laptop — CMD Ping Test ───────────────────────────────
            case 25:
                return VirtualOSManager.Instance != null &&
                       VirtualOSManager.Instance.IsOpen &&
                       VirtualOSManager.Instance.CurrentDevice == DeviceID.Laptop &&
                       pingCmdManager != null && pingCmdManager.gameObject.activeSelf;
            case 26: {
                var s = VirtualOSManager.Instance?.GetState(DeviceID.Laptop);
                return s != null && s.PingResults[IdxLaptop];
            }
            case 27: return true; // observe
            case 28: {
                var s = VirtualOSManager.Instance?.GetState(DeviceID.Laptop);
                return s != null && s.PingResults[DeviceIndex(_serverDevice)];
            }
            case 29: return true; // observe
            case 30: {
                var s = VirtualOSManager.Instance?.GetState(DeviceID.Laptop);
                return s != null && s.PingResults[DeviceIndex(_secondDevice)];
            }
            case 31: return true; // observe
            case 32: {
                var s = VirtualOSManager.Instance?.GetState(DeviceID.Laptop);
                return s != null && s.PingResults[IdxRouter];
            }
            case 33: return true; // observe
            case 34: {
                var s = VirtualOSManager.Instance?.GetState(DeviceID.Laptop);
                return s != null && s.PingResults[IdxAP];
            }
            case 35: return true; // observe
            case 36:
                return pingCmdManager != null && !pingCmdManager.gameObject.activeSelf &&
                       VirtualOSManager.Instance != null && !VirtualOSManager.Instance.IsOpen;

            default: return false;
        }
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
        // Capture device context and baseline before setting the latch flag.
        if (index == 0 && IPConfigTaskManager.Instance != null && VirtualOSManager.Instance != null)
        {
            _serverDevice             = IPConfigTaskManager.Instance.ServerDevice;
            _secondDevice             = IPConfigTaskManager.Instance.SecondDesktopDevice;
            _serverIpconfigBaseline   = VirtualOSManager.Instance.GetState(_serverDevice).IpconfigRunCount;
        }

        _latched[index] = true;
        ActivityLogManager.Log($"Task completed: {GetTaskLabel(index)}", ActivityLogManager.EntryType.Action);

        bool wasVisible = taskObjects[index] != null
                       && taskObjects[index].activeSelf
                       && taskObjects[index].transform.parent == taskParent;

        AdvanceIfPossible();
        OnTasksUpdated?.Invoke();

        if (wasVisible)
        {
            _taskFlashQueue.Enqueue(index);
            if (!_isTaskFlashing) StartCoroutine(ProcessTaskFlashQueue());
        }
        else
        {
            if (taskObjects[index] != null && finishedParent != null)
                taskObjects[index].transform.SetParent(finishedParent, false);
            RefreshWindow();
        }

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

        if (_currentTask >= TASK_COUNT && !_allDone)
        {
            _allDone = true;
            if (allTasksCompletedText != null) allTasksCompletedText.SetActive(true);
            if (topicManagerCompletionIndex >= 0)
                TopicManager.Instance?.MarkTopicComplete(topicManagerCompletionIndex);
            Debug.Log("[PingCmdTaskManager] All 37 Ping Test tasks completed.");
        }
    }

    private void RefreshWindow()
    {
        int occupied   = (_isTaskFlashing ? 1 : 0) + _taskFlashQueue.Count;
        int maxDisplay = Mathf.Max(0, 3 - occupied);

        if (taskGroups == null || taskGroups.Length == 0)
        {
            int shown = 0;
            for (int i = _currentTask; i < TASK_COUNT && shown < maxDisplay; i++)
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
        for (int i = _currentTask; i <= groupEnd && displayed < maxDisplay; i++)
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

    private IEnumerator ProcessTaskFlashQueue()
    {
        _isTaskFlashing = true;
        while (_taskFlashQueue.Count > 0)
        {
            int idx = _taskFlashQueue.Dequeue();
            RefreshWindow();
            var tmp = taskObjects[idx]?.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.color     = Color.green;
                tmp.fontStyle |= FontStyles.Strikethrough;
            }
            yield return new WaitForSeconds(flashDuration);
            if (taskObjects[idx] != null && finishedParent != null)
                taskObjects[idx].transform.SetParent(finishedParent, false);
        }
        _isTaskFlashing = false;
        RefreshWindow();
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
        if (groupTitleTMP != null)
        {
            groupTitleTMP.color     = flashColor;
            groupTitleTMP.fontStyle |= FontStyles.Strikethrough;
        }

        yield return new WaitForSeconds(flashDuration);

        if (groupTitleTMP != null)
        {
            groupTitleTMP.color     = original;
            groupTitleTMP.fontStyle &= ~FontStyles.Strikethrough;
        }
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
                _popupRowTMPs[g][0] = tmps[0];
                _popupRowTMPs[g][1] = tmps[1];
                _popupRowTMPs[g][2] = tmps[2];
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

    // ── Task Labels ───────────────────────────────────────────────────────────

    private static string GetTaskLabel(int index)
    {
        return index switch
        {
            // ── Group 0 ───────────────────────────────────────────────────────
             0 => "Execute Command Prompt of the Server Desktop device through Virtual OS.",
             1 => "Input \"ipconfig\" in the CMD.",
             2 => "Ping this Server desktop's IP address.",
             3 => "Observe the ping request feedback if the reply is correct.",
             4 => "Ping the other Desktop device's IP address.",
             5 => "Observe the ping request feedback if the reply is correct.",
             6 => "Ping the Laptop device's IP address.",
             7 => "Observe the ping request feedback if the reply is correct.",
             8 => "Ping the Router device's IP address.",
             9 => "Observe the ping request feedback if the reply is correct.",
            10 => "Ping the Access Point device's IP address.",
            11 => "Observe the ping request feedback if the reply is correct.",
            12 => "Server desktop ping test complete. Close the CMD and Server's Virtual OS.",

            // ── Group 1 ───────────────────────────────────────────────────────
            13 => "Execute Command Prompt of the other Desktop device through Virtual OS.",
            14 => "Ping this Desktop's IP address.",
            15 => "Observe the ping request feedback if the reply is correct.",
            16 => "Ping the Server Desktop device's IP address.",
            17 => "Observe the ping request feedback if the reply is correct.",
            18 => "Ping the Laptop device's IP address.",
            19 => "Observe the ping request feedback if the reply is correct.",
            20 => "Ping the Router device's IP address.",
            21 => "Observe the ping request feedback if the reply is correct.",
            22 => "Ping the Access Point device's IP address.",
            23 => "Observe the ping request feedback if the reply is correct.",
            24 => "Desktop ping test complete. Close the CMD and Desktop's Virtual OS.",

            // ── Group 2 ───────────────────────────────────────────────────────
            25 => "Execute Command Prompt of the Laptop device through Virtual OS.",
            26 => "Ping this Laptop's IP address.",
            27 => "Observe the ping request feedback if the reply is correct.",
            28 => "Ping the Server Desktop device's IP address.",
            29 => "Observe the ping request feedback if the reply is correct.",
            30 => "Ping the other Desktop device's IP address.",
            31 => "Observe the ping request feedback if the reply is correct.",
            32 => "Ping the Router device's IP address.",
            33 => "Observe the ping request feedback if the reply is correct.",
            34 => "Ping the Access Point device's IP address.",
            35 => "Observe the ping request feedback if the reply is correct.",
            36 => "Laptop ping test complete. Close the CMD and Laptop's Virtual OS.",

            _  => $"Task {index + 1}"
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int DeviceIndex(DeviceID d) => d switch
    {
        DeviceID.Computer1 => IdxComputer1,
        DeviceID.Computer2 => IdxComputer2,
        DeviceID.Laptop    => IdxLaptop,
        _                  => -1
    };

    // ── ITaskCategory ─────────────────────────────────────────────────────────

    public string GetNextIncompleteTaskText()
    {
        if (_allDone || _currentTask >= TASK_COUNT) return "All tasks completed!";
        return GetTaskLabel(_currentTask);
    }

    public Color GetDisplayColor(Color fallback) => _allDone ? Color.green : fallback;

    public void HideCompletionBanner()
    {
        if (allTasksCompletedText != null)
            allTasksCompletedText.SetActive(false);
    }
}
