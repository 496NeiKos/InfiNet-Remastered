/*
 * ================================================================
 *  UNITY SETUP GUIDE — NetworkCableTaskManager  (COC II, Topic 1)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to an always-active "Managers" GameObject in the COC II scene.
 *
 *  ── HIERARCHY ────────────────────────────────────────────────────
 *
 *    Task Panel (NetworkCablePanel)
 *      ├── Group Title Row                   (always visible above task list)
 *      │     ├── GroupTitleTMP               TMP_Text  → groupTitleTMP
 *      │     └── Button component            Button  → groupTitleBtn
 *      │           OnClick → opens Group Overview Popup
 *      │
 *      ├── Task List                         (active task rows parented here)
 *      │     └── [taskObjects[0..44] reparented here at runtime]
 *      │
 *      └── Finished Bucket                  (off-screen; latched tasks move here)
 *
 *    Group Overview Popup                   → groupOverviewPopup (INACTIVE)
 *      ├── Row Parent                       → groupRowParent  (VerticalLayoutGroup)
 *      │     └── [GroupRowPrefab × 2 spawned here at runtime]
 *      └── CloseBtn                         Button  → closePopupBtn
 *
 *  ── GROUP ROW PREFAB ─────────────────────────────────────────────
 *    Root: any layout container (e.g. HorizontalLayoutGroup)
 *      ├── NameTMP      TMP_Text  [0] — group title
 *      ├── ProgressTMP  TMP_Text  [1] — "2 / 23"
 *      └── StatusTMP    TMP_Text  [2] — "Pending" / "Active" / "Done"
 *    GetComponentsInChildren<TMP_Text>() must return them in order [0][1][2].
 *
 *  ── TASK GROUPS — 2 groups, 45 tasks total ───────────────────────
 *    Group 0  "Phase 1 — Straight-Through Cable"  tasks  0 – 22  (23 tasks)
 *    Group 1  "Phase 2 — Crossover Cable"         tasks 23 – 44  (22 tasks)
 *
 *  ── INSPECTOR ASSIGNMENTS ────────────────────────────────────────
 *    taskParent           → VerticalLayoutGroup parent for active task rows
 *    finishedParent       → Off-screen parent for completed task GameObjects
 *    taskObjects[0..44]   → Task row GameObjects (TMP_Text labels)
 *    allTasksCompletedText→ (optional) GameObject shown when all tasks done
 *
 *    groupTitleTMP        → TMP_Text above the task list
 *    groupTitleBtn        → Button on / wrapping the group title TMP
 *    flashColor           → Color flashed on group complete (default green)
 *    flashDuration        → Seconds the flash lasts (default 1)
 *    cocLabel             → e.g. "COC II - Network Cable"
 *
 *    groupOverviewPopup   → Popup panel GameObject (starts INACTIVE)
 *    groupRowParent       → Transform inside popup where rows are spawned
 *    groupRowPrefab       → Prefab for one popup row (3 TMP children)
 *    closePopupBtn        → Button to close the popup
 *    groupPendingColor    → Row color for groups not yet started
 *    groupActiveColor     → Row color for the current active group
 *    groupDoneColor       → Row color for completed groups
 *
 *    taskGroups[]         → TaskGroup array (2 entries, configure in Inspector)
 *                           Group 0: Title="Phase 1 — Straight-Through Cable", FirstTask=0,  TaskCount=23
 *                           Group 1: Title="Phase 2 — Crossover Cable",         FirstTask=23, TaskCount=22
 *    topicManagerCompletionIndex → -1 to disable TopicManager call on finish
 *
 *    networkCableHolder   → NetworkHardwareHolder on the UTP cable icon proxy
 *    cableEnd1            → NetworkCableEndController on CableEnd1
 *    cableEnd2            → NetworkCableEndController on CableEnd2
 *    lanTesterHolder      → NetworkHardwareHolder on the LAN tester icon proxy
 *    lanTesterTopDetail   → TopDetail GameObject inside the LAN Tester's detail hierarchy
 *                           (used with activeInHierarchy to detect Top view)
 *    lanTesterFrontDetail → FrontDetail GameObject inside the LAN Tester's detail hierarchy
 *                           (used with activeInHierarchy to detect Front view)
 *    lanTesterPort        → LanTesterPortController
 *    lanTesterSwitch      → LanTesterSwitchController
 *
 *  ── PATTERN ──────────────────────────────────────────────────────
 *    Latch-based: once a task condition is true it latches permanently.
 *    Sliding window: up to 3 unlatched tasks shown at a time, capped to
 *    the current active group — tasks from the next group never bleed in.
 *    One active group at a time; group advances only after all its tasks
 *    are latched (after a 1-second green flash on the group title TMP).
 *
 *    View detection (tasks 14, 16, 37, 39) uses activeInHierarchy — NOT
 *    activeSelf — because the detail view container is disabled when the
 *    editor is closed, which would leave individual view activeSelf flags
 *    stale and cause false positives.
 * ================================================================
 */

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkCableTaskManager : MonoBehaviour, ITaskCategory
{
    public static NetworkCableTaskManager Instance { get; private set; }

    public static event System.Action OnTasksUpdated;

    private const int TASK_COUNT = 45;

    private static readonly int[] T568A = { 0, 1, 2, 3, 4, 5, 6, 7 };
    private static readonly int[] T568B = { 2, 5, 0, 3, 4, 1, 6, 7 };

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
    [SerializeField] private string   cocLabel      = "COC II - Network Cable";

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

    // ── Hardware ──────────────────────────────────────────────────────────────

    [Header("Network Cable")]
    [SerializeField] private NetworkHardwareHolder     networkCableHolder;
    [SerializeField] private NetworkCableEndController cableEnd1;
    [SerializeField] private NetworkCableEndController cableEnd2;

    [Header("LAN Tester")]
    [SerializeField] private NetworkHardwareHolder     lanTesterHolder;
    [Tooltip("TopDetail GameObject inside the LAN Tester detail hierarchy. Checked with activeInHierarchy.")]
    [SerializeField] private GameObject                lanTesterTopDetail;
    [Tooltip("FrontDetail GameObject inside the LAN Tester detail hierarchy. Checked with activeInHierarchy.")]
    [SerializeField] private GameObject                lanTesterFrontDetail;
    [SerializeField] private LanTesterPortController   lanTesterPort;
    [SerializeField] private LanTesterSwitchController lanTesterSwitch;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private bool[]       _latched           = new bool[TASK_COUNT];
    private int          _currentTask       = 0;
    private int          _currentGroupIndex = 0;
    private bool         _allDone           = false;
    private bool         _isFlashing        = false;
    private TMP_Text[][] _popupRowTMPs;

    private static readonly Color DefaultTaskColor  = new Color(1.000f, 0.886f, 0.596f);
    private static readonly Color DefaultTitleColor = new Color(0.953f, 0.839f, 0.020f);

    private Queue<int> _taskFlashQueue  = new Queue<int>();
    private bool       _isTaskFlashing  = false;

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

        LanTesterLEDDisplay.OnSequenceComplete += OnLEDSequenceComplete;
    }

    private void OnEnable()
    {
        RefreshGroupTitle();
        if (groupOverviewPopup != null && groupOverviewPopup.activeSelf)
            RefreshPopup();
    }

    private void OnDestroy()
    {
        LanTesterLEDDisplay.OnSequenceComplete -= OnLEDSequenceComplete;
    }

    private void Update()
    {
        if (_allDone) return;
        CheckCurrentTask();
    }

    // ── Task condition checks ─────────────────────────────────────────────────

    private bool CheckCondition(int index)
    {
        return index switch
        {
            // ── Group 0: Phase 1 — Straight-Through Cable ─────────────────────

            // 0: Deploy UTP Cable
             0 => networkCableHolder?.hardwarePrefab != null &&
                  networkCableHolder.hardwarePrefab.activeSelf &&
                  GameManager.Instance != null &&
                  networkCableHolder.hardwarePrefab.transform.IsChildOf(GameManager.Instance.ActiveWorldContainer),

            // 1: Open detail view
             1 => GameManager.Instance != null && GameManager.Instance.IsEditorOpen,

            // 2: Strip any end
             2 => (cableEnd1 != null && cableEnd1.IsStripped) ||
                  (cableEnd2 != null && cableEnd2.IsStripped),

            // 3: Swap tutorial
             3 => WireSwapManager.Instance != null && WireSwapManager.Instance.TotalSwapCount >= 1,

            // 4: First end has a valid T-568 standard
             4 => (cableEnd1 != null && cableEnd1.IsStripped  && EndMatchesAnyStandard(cableEnd1)) ||
                  (cableEnd2 != null && cableEnd2.IsStripped  && EndMatchesAnyStandard(cableEnd2)),

            // 5: RJ45 installed on an end that has correct wiring
             5 => (cableEnd1 != null && cableEnd1.IsRJ45Installed && EndMatchesAnyStandard(cableEnd1)) ||
                  (cableEnd2 != null && cableEnd2.IsRJ45Installed && EndMatchesAnyStandard(cableEnd2)),

            // 6: First end crimped with correct wiring
             6 => (cableEnd1 != null && cableEnd1.IsCrimped && EndMatchesAnyStandard(cableEnd1)) ||
                  (cableEnd2 != null && cableEnd2.IsCrimped && EndMatchesAnyStandard(cableEnd2)),

            // 7: Other end also stripped (both stripped)
             7 => cableEnd1 != null && cableEnd1.IsStripped &&
                  cableEnd2 != null && cableEnd2.IsStripped,

            // 8: Both ends match the same standard (straight-through)
             8 => IsStraightThrough(),

            // 9: Both RJ45 installed and straight-through confirmed
             9 => cableEnd1 != null && cableEnd1.IsRJ45Installed &&
                  cableEnd2 != null && cableEnd2.IsRJ45Installed &&
                  IsStraightThrough(),

            // 10: Both ends crimped and straight-through confirmed
            10 => cableEnd1 != null && cableEnd1.IsCrimped &&
                  cableEnd2 != null && cableEnd2.IsCrimped &&
                  IsStraightThrough(),

            // 11: Exit detail view
            11 => GameManager.Instance != null && !GameManager.Instance.IsEditorOpen,

            // 12: Cable stored
            12 => networkCableHolder != null && networkCableHolder.IsAvailable(),

            // 13: Deploy LAN Tester
            13 => lanTesterHolder?.hardwarePrefab != null &&
                  lanTesterHolder.hardwarePrefab.activeSelf &&
                  GameManager.Instance != null &&
                  lanTesterHolder.hardwarePrefab.transform.IsChildOf(GameManager.Instance.ActiveWorldContainer),

            // 14: Top view of LAN Tester active (activeInHierarchy guards against stale activeSelf)
            14 => lanTesterTopDetail != null && lanTesterTopDetail.activeInHierarchy,

            // 15: Cable installed to port
            15 => lanTesterPort != null && lanTesterPort.IsCableInstalled,

            // 16: Front view of LAN Tester active
            16 => lanTesterFrontDetail != null && lanTesterFrontDetail.activeInHierarchy,

            // 17: Power switch on
            17 => lanTesterSwitch != null && lanTesterSwitch.IsOn,

            // 18: LED sequence confirmed straight-through (latched by event handler)
            18 => _latched[18],

            // 19: Power switch off
            19 => lanTesterSwitch != null && !lanTesterSwitch.IsOn,

            // 20: Cable unplugged from port
            20 => lanTesterPort != null && !lanTesterPort.IsCableInstalled,

            // 21: Exit LAN Tester detail view
            21 => GameManager.Instance != null && !GameManager.Instance.IsEditorOpen,

            // 22: LAN Tester stored
            22 => lanTesterHolder != null && lanTesterHolder.IsAvailable(),

            // ── Group 1: Phase 2 — Crossover Cable ───────────────────────────

            // 23: Re-deploy UTP Cable
            23 => networkCableHolder?.hardwarePrefab != null &&
                  networkCableHolder.hardwarePrefab.activeSelf &&
                  GameManager.Instance != null &&
                  networkCableHolder.hardwarePrefab.transform.IsChildOf(GameManager.Instance.ActiveWorldContainer),

            // 24: Open detail view
            24 => GameManager.Instance != null && GameManager.Instance.IsEditorOpen,

            // 25: Both ends cut by crimper (reset from Phase 1 crimp)
            25 => cableEnd1 != null && !cableEnd1.IsStripped && cableEnd1.StripCycleCount >= 1 &&
                  cableEnd2 != null && !cableEnd2.IsStripped && cableEnd2.StripCycleCount >= 1,

            // 26: Strip any end (Phase 2 — StripCycleCount will become 2)
            26 => (cableEnd1 != null && cableEnd1.IsStripped) ||
                  (cableEnd2 != null && cableEnd2.IsStripped),

            // 27: First end arranged to T-568 standard (Phase 2 strip confirmed)
            27 => (cableEnd1 != null && cableEnd1.IsStripped && cableEnd1.StripCycleCount >= 2 && EndMatchesAnyStandard(cableEnd1)) ||
                  (cableEnd2 != null && cableEnd2.IsStripped && cableEnd2.StripCycleCount >= 2 && EndMatchesAnyStandard(cableEnd2)),

            // 28: RJ45 installed on Phase 2 end with correct wiring
            28 => (cableEnd1 != null && cableEnd1.IsRJ45Installed && cableEnd1.StripCycleCount >= 2 && EndMatchesAnyStandard(cableEnd1)) ||
                  (cableEnd2 != null && cableEnd2.IsRJ45Installed && cableEnd2.StripCycleCount >= 2 && EndMatchesAnyStandard(cableEnd2)),

            // 29: First end crimped with correct wiring (Phase 2)
            29 => (cableEnd1 != null && cableEnd1.IsCrimped && cableEnd1.StripCycleCount >= 2 && EndMatchesAnyStandard(cableEnd1)) ||
                  (cableEnd2 != null && cableEnd2.IsCrimped && cableEnd2.StripCycleCount >= 2 && EndMatchesAnyStandard(cableEnd2)),

            // 30: Both ends stripped at Phase 2 strip count
            30 => cableEnd1 != null && cableEnd1.IsStripped && cableEnd1.StripCycleCount >= 2 &&
                  cableEnd2 != null && cableEnd2.IsStripped && cableEnd2.StripCycleCount >= 2,

            // 31: Both ends form a crossover (opposite standards)
            31 => IsCrossover(),

            // 32: Both RJ45 installed and crossover confirmed
            32 => cableEnd1 != null && cableEnd1.IsRJ45Installed &&
                  cableEnd2 != null && cableEnd2.IsRJ45Installed &&
                  IsCrossover(),

            // 33: Both ends crimped, crossover confirmed, Phase 2 strip confirmed
            33 => cableEnd1 != null && cableEnd1.IsCrimped && cableEnd1.StripCycleCount >= 2 &&
                  cableEnd2 != null && cableEnd2.IsCrimped && cableEnd2.StripCycleCount >= 2 &&
                  IsCrossover(),

            // 34: Exit detail view
            34 => GameManager.Instance != null && !GameManager.Instance.IsEditorOpen,

            // 35: Cable stored
            35 => networkCableHolder != null && networkCableHolder.IsAvailable(),

            // 36: Deploy LAN Tester
            36 => lanTesterHolder?.hardwarePrefab != null &&
                  lanTesterHolder.hardwarePrefab.activeSelf &&
                  GameManager.Instance != null &&
                  lanTesterHolder.hardwarePrefab.transform.IsChildOf(GameManager.Instance.ActiveWorldContainer),

            // 37: Top view of LAN Tester active
            37 => lanTesterTopDetail != null && lanTesterTopDetail.activeInHierarchy,

            // 38: Cable installed to port
            38 => lanTesterPort != null && lanTesterPort.IsCableInstalled,

            // 39: Front view of LAN Tester active
            39 => lanTesterFrontDetail != null && lanTesterFrontDetail.activeInHierarchy,

            // 40: Power switch on
            40 => lanTesterSwitch != null && lanTesterSwitch.IsOn,

            // 41: LED sequence confirmed crossover (latched by event handler)
            41 => _latched[41],

            // 42: Power switch off
            42 => lanTesterSwitch != null && !lanTesterSwitch.IsOn,

            // 43: Cable unplugged from port
            43 => lanTesterPort != null && !lanTesterPort.IsCableInstalled,

            // 44: LAN Tester stored
            44 => lanTesterHolder != null && lanTesterHolder.IsAvailable(),

            _ => false
        };
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
            Debug.Log("[NetworkCableTaskManager] All 45 tasks completed.");
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

    // ── LED Sequence Handler ──────────────────────────────────────────────────

    private void OnLEDSequenceComplete()
    {
        // Phase 1: latch task 18 when straight-through is confirmed
        if (!_latched[18] && _latched[17] &&
            lanTesterPort != null && lanTesterPort.IsCableInstalled &&
            IsStraightThrough())
            LatchTask(18);

        // Phase 2: latch task 41 when crossover is confirmed
        if (!_latched[41] && _latched[40] &&
            lanTesterPort != null && lanTesterPort.IsCableInstalled &&
            IsCrossover())
            LatchTask(41);
    }

    // ── Cable Type Helpers ────────────────────────────────────────────────────

    private bool EndMatchesStandard(NetworkCableEndController end, int[] standard)
    {
        for (int i = 0; i < 8; i++)
            if (end.GetWireColorAtSlot(i) != standard[i]) return false;
        return true;
    }

    private bool EndMatchesAnyStandard(NetworkCableEndController end) =>
        EndMatchesStandard(end, T568A) || EndMatchesStandard(end, T568B);

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

    // ── Task Labels ───────────────────────────────────────────────────────────

    private static string GetTaskLabel(int index)
    {
        return index switch
        {
            // ── Group 0: Phase 1 — Straight-Through ──────────────────────────
             0 => "Deploy the UTP Cable to the Workspace.",
             1 => "Interact with the UTP Cable's detail view.",
             2 => "Use the Wire Stripper to cut the cable jacket (any end).",
             3 => "Click two different wires to swap the color sequence.",
             4 => "Create T-568A or T-568B wiring on the first cable end.",
             5 => "Install RJ45 to the cable's first end.",
             6 => "Use Crimper to crimp the RJ45 on the first end.",
             7 => "Do the same to the other end: Use Wire Stripper to cut the cable jacket.",
             8 => "Do the same to the other end: Create the SAME wiring standard (Straight-Through).",
             9 => "Do the same to the other end: Install RJ45.",
            10 => "Do the same to the other end: Use Crimper to crimp the RJ45.",
            11 => "Exit the UTP Cable's detail view.",
            12 => "Store the UTP Cable back to hardware storage.",
            13 => "Deploy LAN Tester to the workspace.",
            14 => "Go to Top view angle of the LAN Tester.",
            15 => "Install the UTP Cable to the LAN Tester port.",
            16 => "Go to Front view angle of the LAN Tester.",
            17 => "Turn on the power switch of the LAN Tester.",
            18 => "Observe the LAN Tester LED sequence — confirm Straight-Through pattern.",
            19 => "You have finished the Straight-Through cable. Turn off the LAN Tester power switch.",
            20 => "Go to Top view angle of LAN Tester then unplug the UTP Cable.",
            21 => "Exit the LAN Tester's detail view.",
            22 => "Store the LAN Tester back to hardware storage.",

            // ── Group 1: Phase 2 — Crossover ─────────────────────────────────
            23 => "Re-deploy the UTP Cable to the workspace to start Cross-Over.",
            24 => "Interact with the UTP Cable's detail view.",
            25 => "Use the Crimper to cut the Straight-Through ends (both ends).",
            26 => "Use the Wire Stripper to cut the cable jacket.",
            27 => "Create T-568A or T-568B wiring on the first cable end.",
            28 => "Install RJ45 on the cable's first end.",
            29 => "Use Crimper to crimp the RJ45 on the first end.",
            30 => "Do the same to the other end: Use Wire Stripper to cut the cable jacket.",
            31 => "Do the same to the other end: Create the DIFFERENT wiring standard (Cross-Over).",
            32 => "Do the same to the other end: Install RJ45.",
            33 => "Do the same to the other end: Use Crimper to crimp the RJ45.",
            34 => "Exit the UTP Cable's detail view.",
            35 => "Store the UTP Cable back to hardware storage.",
            36 => "Deploy LAN Tester to the workspace.",
            37 => "Go to Top view angle of the LAN Tester.",
            38 => "Install the UTP Cable to the LAN Tester port.",
            39 => "Go to Front view angle of the LAN Tester.",
            40 => "Turn on the power switch of the LAN Tester.",
            41 => "Observe the LAN Tester LED sequence — confirm Cross-Over pattern.",
            42 => "Cross-Over cable complete. Turn off the LAN Tester power switch.",
            43 => "Unplug the UTP Cable from the LAN Tester port.",
            44 => "Store the LAN Tester back to hardware storage.",

            _  => $"Task {index + 1}"
        };
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public static void CheckConditions()
    {
        if (Instance == null || Instance._allDone) return;
        Instance.CheckCurrentTask();
    }

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
