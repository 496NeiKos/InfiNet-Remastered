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
 *  ── TASK GROUPS — 2 groups, 49 tasks total ───────────────────────
 *    Group 0  "Phase 1 — Straight-Through Cable"  tasks  0 – 24  (25 tasks)
 *    Group 1  "Phase 2 — Crossover Cable"         tasks 25 – 48  (24 tasks)
 *
 *  ── INSPECTOR ASSIGNMENTS ────────────────────────────────────────
 *    taskParent           → VerticalLayoutGroup parent for active task rows
 *    finishedParent       → Off-screen parent for completed task GameObjects
 *    taskObjects[0..48]   → Task row GameObjects (TMP_Text labels)
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
 *                           Group 0: Title="Phase 1 — Straight-Through Cable", FirstTask=0,  TaskCount=25
 *                           Group 1: Title="Phase 2 — Crossover Cable",         FirstTask=25, TaskCount=24
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
 *    View detection (tasks 16, 18, 41, 43) uses activeInHierarchy — NOT
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

    private const int TASK_COUNT = 49;

    private static readonly int[] T568A = { 0, 1, 2, 3, 4, 5, 6, 7 };
    private static readonly int[] T568B = { 2, 5, 0, 3, 4, 1, 6, 7 };

    // ── Task UI ───────────────────────────────────────────────────────────────

    [Header("Task UI")]
    [SerializeField] private Transform    taskParent;
    [SerializeField] private Transform    finishedParent;
    [SerializeField] private GameObject[] taskObjects = new GameObject[49];
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
             4 => (cableEnd1 != null && cableEnd1.IsStripped && EndMatchesAnyStandard(cableEnd1)) ||
                  (cableEnd2 != null && cableEnd2.IsStripped && EndMatchesAnyStandard(cableEnd2)),

            // 5: First end trimmed with valid T-568 standard
             5 => (cableEnd1 != null && cableEnd1.IsTrimmed && EndMatchesAnyStandard(cableEnd1)) ||
                  (cableEnd2 != null && cableEnd2.IsTrimmed && EndMatchesAnyStandard(cableEnd2)),

            // 6: RJ45 installed on an end that has correct wiring
             6 => (cableEnd1 != null && cableEnd1.IsRJ45Installed && EndMatchesAnyStandard(cableEnd1)) ||
                  (cableEnd2 != null && cableEnd2.IsRJ45Installed && EndMatchesAnyStandard(cableEnd2)),

            // 7: First end crimped with correct wiring
             7 => (cableEnd1 != null && cableEnd1.IsCrimped && EndMatchesAnyStandard(cableEnd1)) ||
                  (cableEnd2 != null && cableEnd2.IsCrimped && EndMatchesAnyStandard(cableEnd2)),

            // 8: Other end also stripped (both stripped)
             8 => cableEnd1 != null && cableEnd1.IsStripped &&
                  cableEnd2 != null && cableEnd2.IsStripped,

            // 9: Both ends match the same standard (straight-through)
             9 => IsStraightThrough(),

            // 10: Both ends trimmed and straight-through confirmed
            10 => cableEnd1 != null && cableEnd1.IsTrimmed &&
                  cableEnd2 != null && cableEnd2.IsTrimmed &&
                  IsStraightThrough(),

            // 11: Both RJ45 installed and straight-through confirmed
            11 => cableEnd1 != null && cableEnd1.IsRJ45Installed &&
                  cableEnd2 != null && cableEnd2.IsRJ45Installed &&
                  IsStraightThrough(),

            // 12: Both ends crimped and straight-through confirmed
            12 => cableEnd1 != null && cableEnd1.IsCrimped &&
                  cableEnd2 != null && cableEnd2.IsCrimped &&
                  IsStraightThrough(),

            // 13: Exit detail view
            13 => GameManager.Instance != null && !GameManager.Instance.IsEditorOpen,

            // 14: Cable stored
            14 => networkCableHolder != null && networkCableHolder.IsAvailable(),

            // 15: Deploy LAN Tester
            15 => lanTesterHolder?.hardwarePrefab != null &&
                  lanTesterHolder.hardwarePrefab.activeSelf &&
                  GameManager.Instance != null &&
                  lanTesterHolder.hardwarePrefab.transform.IsChildOf(GameManager.Instance.ActiveWorldContainer),

            // 16: Top view of LAN Tester active (activeInHierarchy guards against stale activeSelf)
            16 => lanTesterTopDetail != null && lanTesterTopDetail.activeInHierarchy,

            // 17: Cable installed to port
            17 => lanTesterPort != null && lanTesterPort.IsCableInstalled,

            // 18: Front view of LAN Tester active
            18 => lanTesterFrontDetail != null && lanTesterFrontDetail.activeInHierarchy,

            // 19: Power switch on
            19 => lanTesterSwitch != null && lanTesterSwitch.IsOn,

            // 20: LED sequence confirmed straight-through (latched by event handler)
            20 => _latched[20],

            // 21: Power switch off
            21 => lanTesterSwitch != null && !lanTesterSwitch.IsOn,

            // 22: Cable unplugged from port
            22 => lanTesterPort != null && !lanTesterPort.IsCableInstalled,

            // 23: Exit LAN Tester detail view
            23 => GameManager.Instance != null && !GameManager.Instance.IsEditorOpen,

            // 24: LAN Tester stored
            24 => lanTesterHolder != null && lanTesterHolder.IsAvailable(),

            // ── Group 1: Phase 2 — Crossover Cable ───────────────────────────

            // 25: Re-deploy UTP Cable
            25 => networkCableHolder?.hardwarePrefab != null &&
                  networkCableHolder.hardwarePrefab.activeSelf &&
                  GameManager.Instance != null &&
                  networkCableHolder.hardwarePrefab.transform.IsChildOf(GameManager.Instance.ActiveWorldContainer),

            // 26: Open detail view
            26 => GameManager.Instance != null && GameManager.Instance.IsEditorOpen,

            // 27: Both ends cut by crimper (reset from Phase 1 crimp)
            27 => cableEnd1 != null && !cableEnd1.IsStripped && cableEnd1.StripCycleCount >= 1 &&
                  cableEnd2 != null && !cableEnd2.IsStripped && cableEnd2.StripCycleCount >= 1,

            // 28: Strip any end (Phase 2 — StripCycleCount will become 2)
            28 => (cableEnd1 != null && cableEnd1.IsStripped) ||
                  (cableEnd2 != null && cableEnd2.IsStripped),

            // 29: First end arranged to T-568 standard (Phase 2 strip confirmed)
            29 => (cableEnd1 != null && cableEnd1.IsStripped && cableEnd1.StripCycleCount >= 2 && EndMatchesAnyStandard(cableEnd1)) ||
                  (cableEnd2 != null && cableEnd2.IsStripped && cableEnd2.StripCycleCount >= 2 && EndMatchesAnyStandard(cableEnd2)),

            // 30: First end trimmed with valid T-568 standard (Phase 2)
            30 => (cableEnd1 != null && cableEnd1.IsTrimmed && cableEnd1.StripCycleCount >= 2 && EndMatchesAnyStandard(cableEnd1)) ||
                  (cableEnd2 != null && cableEnd2.IsTrimmed && cableEnd2.StripCycleCount >= 2 && EndMatchesAnyStandard(cableEnd2)),

            // 31: RJ45 installed on Phase 2 end with correct wiring
            31 => (cableEnd1 != null && cableEnd1.IsRJ45Installed && cableEnd1.StripCycleCount >= 2 && EndMatchesAnyStandard(cableEnd1)) ||
                  (cableEnd2 != null && cableEnd2.IsRJ45Installed && cableEnd2.StripCycleCount >= 2 && EndMatchesAnyStandard(cableEnd2)),

            // 32: First end crimped with correct wiring (Phase 2)
            32 => (cableEnd1 != null && cableEnd1.IsCrimped && cableEnd1.StripCycleCount >= 2 && EndMatchesAnyStandard(cableEnd1)) ||
                  (cableEnd2 != null && cableEnd2.IsCrimped && cableEnd2.StripCycleCount >= 2 && EndMatchesAnyStandard(cableEnd2)),

            // 33: Both ends stripped at Phase 2 strip count
            33 => cableEnd1 != null && cableEnd1.IsStripped && cableEnd1.StripCycleCount >= 2 &&
                  cableEnd2 != null && cableEnd2.IsStripped && cableEnd2.StripCycleCount >= 2,

            // 34: Both ends form a crossover (opposite standards)
            34 => IsCrossover(),

            // 35: Both ends trimmed, Phase 2, crossover confirmed
            35 => cableEnd1 != null && cableEnd1.IsTrimmed && cableEnd1.StripCycleCount >= 2 &&
                  cableEnd2 != null && cableEnd2.IsTrimmed && cableEnd2.StripCycleCount >= 2 &&
                  IsCrossover(),

            // 36: Both RJ45 installed and crossover confirmed
            36 => cableEnd1 != null && cableEnd1.IsRJ45Installed &&
                  cableEnd2 != null && cableEnd2.IsRJ45Installed &&
                  IsCrossover(),

            // 37: Both ends crimped, crossover confirmed, Phase 2 strip confirmed
            37 => cableEnd1 != null && cableEnd1.IsCrimped && cableEnd1.StripCycleCount >= 2 &&
                  cableEnd2 != null && cableEnd2.IsCrimped && cableEnd2.StripCycleCount >= 2 &&
                  IsCrossover(),

            // 38: Exit detail view
            38 => GameManager.Instance != null && !GameManager.Instance.IsEditorOpen,

            // 39: Cable stored
            39 => networkCableHolder != null && networkCableHolder.IsAvailable(),

            // 40: Deploy LAN Tester
            40 => lanTesterHolder?.hardwarePrefab != null &&
                  lanTesterHolder.hardwarePrefab.activeSelf &&
                  GameManager.Instance != null &&
                  lanTesterHolder.hardwarePrefab.transform.IsChildOf(GameManager.Instance.ActiveWorldContainer),

            // 41: Top view of LAN Tester active
            41 => lanTesterTopDetail != null && lanTesterTopDetail.activeInHierarchy,

            // 42: Cable installed to port
            42 => lanTesterPort != null && lanTesterPort.IsCableInstalled,

            // 43: Front view of LAN Tester active
            43 => lanTesterFrontDetail != null && lanTesterFrontDetail.activeInHierarchy,

            // 44: Power switch on
            44 => lanTesterSwitch != null && lanTesterSwitch.IsOn,

            // 45: LED sequence confirmed crossover (latched by event handler)
            45 => _latched[45],

            // 46: Power switch off
            46 => lanTesterSwitch != null && !lanTesterSwitch.IsOn,

            // 47: Cable unplugged from port
            47 => lanTesterPort != null && !lanTesterPort.IsCableInstalled,

            // 48: LAN Tester stored
            48 => lanTesterHolder != null && lanTesterHolder.IsAvailable(),

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
        // Phase 1: latch task 20 when straight-through is confirmed
        if (!_latched[20] && _latched[19] &&
            lanTesterPort != null && lanTesterPort.IsCableInstalled &&
            IsStraightThrough())
            LatchTask(20);

        // Phase 2: latch task 45 when crossover is confirmed
        if (!_latched[45] && _latched[44] &&
            lanTesterPort != null && lanTesterPort.IsCableInstalled &&
            IsCrossover())
            LatchTask(45);
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
             5 => "Trim the length of all wires using Crimper.",
             6 => "Install RJ45 to the cable's first end.",
             7 => "Use Crimper to crimp the RJ45 on the first end.",
             8 => "Do the same to the other end: Use Wire Stripper to cut the cable jacket.",
             9 => "Do the same to the other end: Create the SAME wiring standard (Straight-Through).",
            10 => "Do the same to the other end: Trim the length of all wires using Crimper.",
            11 => "Do the same to the other end: Install RJ45.",
            12 => "Do the same to the other end: Use Crimper to crimp the RJ45.",
            13 => "Exit the UTP Cable's detail view.",
            14 => "Store the UTP Cable back to hardware storage.",
            15 => "Deploy LAN Tester to the workspace.",
            16 => "Go to Top view angle of the LAN Tester.",
            17 => "Install the UTP Cable to the LAN Tester port.",
            18 => "Go to Front view angle of the LAN Tester.",
            19 => "Turn on the power switch of the LAN Tester.",
            20 => "Observe the LAN Tester LED sequence — confirm Straight-Through pattern.",
            21 => "You have finished the Straight-Through cable. Turn off the LAN Tester power switch.",
            22 => "Go to Top view angle of LAN Tester then unplug the UTP Cable.",
            23 => "Exit the LAN Tester's detail view.",
            24 => "Store the LAN Tester back to hardware storage.",

            // ── Group 1: Phase 2 — Crossover ─────────────────────────────────
            25 => "Re-deploy the UTP Cable to the workspace to start Cross-Over.",
            26 => "Interact with the UTP Cable's detail view.",
            27 => "Use the Crimper to cut the Straight-Through ends (both ends).",
            28 => "Use the Wire Stripper to cut the cable jacket.",
            29 => "Create T-568A or T-568B wiring on the first cable end.",
            30 => "Trim the length of all wires using Crimper.",
            31 => "Install RJ45 on the cable's first end.",
            32 => "Use Crimper to crimp the RJ45 on the first end.",
            33 => "Do the same to the other end: Use Wire Stripper to cut the cable jacket.",
            34 => "Do the same to the other end: Create the DIFFERENT wiring standard (Cross-Over).",
            35 => "Do the same to the other end: Trim the length of all wires using Crimper.",
            36 => "Do the same to the other end: Install RJ45.",
            37 => "Do the same to the other end: Use Crimper to crimp the RJ45.",
            38 => "Exit the UTP Cable's detail view.",
            39 => "Store the UTP Cable back to hardware storage.",
            40 => "Deploy LAN Tester to the workspace.",
            41 => "Go to Top view angle of the LAN Tester.",
            42 => "Install the UTP Cable to the LAN Tester port.",
            43 => "Go to Front view angle of the LAN Tester.",
            44 => "Turn on the power switch of the LAN Tester.",
            45 => "Observe the LAN Tester LED sequence — confirm Cross-Over pattern.",
            46 => "Cross-Over cable complete. Turn off the LAN Tester power switch.",
            47 => "Unplug the UTP Cable from the LAN Tester port.",
            48 => "Store the LAN Tester back to hardware storage.",

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

    public int GetCompletedTaskCount()
    {
        int count = 0;
        for (int i = 0; i < TASK_COUNT; i++) if (_latched[i]) count++;
        return count;
    }
}
