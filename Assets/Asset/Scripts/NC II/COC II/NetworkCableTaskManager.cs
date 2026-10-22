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
 *      │     └── [taskObjects[0..24] reparented here at runtime]
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
 *      ├── ProgressTMP  TMP_Text  [1] — "2 / 13"
 *      └── StatusTMP    TMP_Text  [2] — "Pending" / "Active" / "Done"
 *    GetComponentsInChildren<TMP_Text>() must return them in order [0][1][2].
 *
 *  ── TASK GROUPS — 2 groups, 25 tasks total ───────────────────────
 *    Group 0  "Phase 1 — Straight-Through Cable"  tasks  0 – 12  (13 tasks)
 *    Group 1  "Phase 2 — Crossover Cable"         tasks 13 – 24  (12 tasks)
 *
 *  ── INSPECTOR ASSIGNMENTS ────────────────────────────────────────
 *    taskParent           → VerticalLayoutGroup parent for active task rows
 *    finishedParent       → Off-screen parent for completed task GameObjects
 *    taskObjects[0..24]   → Task row GameObjects (TMP_Text labels)
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
 *    topicManagerCompletionIndex → -1 to disable TopicManager call on finish
 *
 *    networkCableHolder   → NetworkHardwareHolder on the UTP cable icon proxy
 *    cableEnd1            → NetworkCableEndController on CableEnd1
 *    cableEnd2            → NetworkCableEndController on CableEnd2
 *    lanTesterHolder      → NetworkHardwareHolder on the LAN tester icon proxy
 *    lanTesterPort        → LanTesterPortController
 *    lanTesterSwitch      → LanTesterSwitchController
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
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkCableTaskManager : MonoBehaviour, ITaskCategory
{
    public static NetworkCableTaskManager Instance { get; private set; }

    public static event System.Action OnTasksUpdated;

    private const int TASK_COUNT = 25;

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
    [SerializeField] private LanTesterPortController   lanTesterPort;
    [SerializeField] private LanTesterSwitchController lanTesterSwitch;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private bool[]       _latched           = new bool[TASK_COUNT];
    private int          _currentTask       = 0;
    private int          _currentGroupIndex = 0;
    private bool         _allDone           = false;
    private bool         _isFlashing        = false;
    private TMP_Text[][] _popupRowTMPs;

    // Guards Task 13 from firing during the drag-from-LAN-tester-port phase.
    // Step 1: wait for cable to reach storage after Task 12; Step 2: re-deploy fires the latch.
    private bool _cableStoredAfterT12 = false;

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
             0 => networkCableHolder?.hardwarePrefab != null &&
                  networkCableHolder.hardwarePrefab.activeSelf &&
                  GameManager.Instance != null &&
                  networkCableHolder.hardwarePrefab.transform.IsChildOf(GameManager.Instance.ActiveWorldContainer),
             1 => GameManager.Instance != null && GameManager.Instance.IsEditorOpen,
             2 => (cableEnd1 != null && cableEnd1.IsStripped) || (cableEnd2 != null && cableEnd2.IsStripped),
             3 => WireSwapManager.Instance != null && WireSwapManager.Instance.TotalSwapCount >= 1,
             4 => cableEnd1 != null && cableEnd1.IsStripped &&
                  cableEnd2 != null && cableEnd2.IsStripped &&
                  IsStraightThrough(),
             5 => (cableEnd1 != null && cableEnd1.IsRJ45Installed) || (cableEnd2 != null && cableEnd2.IsRJ45Installed),
             6 => (cableEnd1 != null && cableEnd1.IsCrimped) || (cableEnd2 != null && cableEnd2.IsCrimped),
             7 => networkCableHolder != null && networkCableHolder.IsAvailable(),
             8 => lanTesterHolder?.hardwarePrefab != null && lanTesterHolder.hardwarePrefab.activeSelf,
             9 => lanTesterPort != null && lanTesterPort.IsCableInstalled,
            10 => lanTesterSwitch != null && lanTesterSwitch.IsOn,
            11 => _latched[11], // set by OnLEDSequenceComplete when straight-through LED cycle confirms
            12 => lanTesterSwitch != null && !lanTesterSwitch.IsOn &&
                  lanTesterPort   != null && !lanTesterPort.IsCableInstalled,

            // ── Group 1: Phase 2 — Crossover Cable ───────────────────────────
            13 => _cableStoredAfterT12 &&
                  networkCableHolder?.hardwarePrefab != null &&
                  networkCableHolder.hardwarePrefab.activeSelf,
            14 => GameManager.Instance != null && GameManager.Instance.IsEditorOpen,
            15 => (cableEnd1 != null && !cableEnd1.IsStripped && cableEnd1.StripCycleCount >= 1) ||
                  (cableEnd2 != null && !cableEnd2.IsStripped && cableEnd2.StripCycleCount >= 1),
            16 => (cableEnd1 != null && cableEnd1.IsStripped && cableEnd1.StripCycleCount >= 2) ||
                  (cableEnd2 != null && cableEnd2.IsStripped && cableEnd2.StripCycleCount >= 2),
            17 => cableEnd1 != null && cableEnd1.IsStripped && cableEnd1.StripCycleCount >= 2 &&
                  cableEnd2 != null && cableEnd2.IsStripped && cableEnd2.StripCycleCount >= 2 &&
                  IsCrossover(),
            18 => (cableEnd1 != null && cableEnd1.StripCycleCount >= 2 && cableEnd1.IsRJ45Installed) ||
                  (cableEnd2 != null && cableEnd2.StripCycleCount >= 2 && cableEnd2.IsRJ45Installed),
            19 => (cableEnd1 != null && cableEnd1.StripCycleCount >= 2 && cableEnd1.IsCrimped) ||
                  (cableEnd2 != null && cableEnd2.StripCycleCount >= 2 && cableEnd2.IsCrimped),
            20 => networkCableHolder != null && networkCableHolder.IsAvailable(),
            21 => lanTesterHolder?.hardwarePrefab != null && lanTesterHolder.hardwarePrefab.activeSelf,
            22 => lanTesterPort != null && lanTesterPort.IsCableInstalled,
            23 => lanTesterSwitch != null && lanTesterSwitch.IsOn,
            24 => _latched[24], // set by OnLEDSequenceComplete when crossover LED cycle confirms

            _ => false
        };
    }

    // ── Latch & window logic ──────────────────────────────────────────────────

    private void CheckCurrentTask()
    {
        while (_currentTask < TASK_COUNT && !_allDone)
        {
            // Update the Phase-2 cable-storage guard while waiting on Task 13.
            if (_currentTask == 13 && !_cableStoredAfterT12 &&
                networkCableHolder != null && networkCableHolder.IsAvailable())
                _cableStoredAfterT12 = true;

            if (_latched[_currentTask]) { AdvanceIfPossible(); continue; }
            if (!CheckCondition(_currentTask)) return;
            LatchTask(_currentTask);
        }
    }

    private void LatchTask(int index)
    {
        _latched[index] = true;
        ActivityLogManager.Log($"Task completed: {GetTaskLabel(index)}", ActivityLogManager.EntryType.Action);

        if (taskObjects[index] != null && finishedParent != null)
            taskObjects[index].transform.SetParent(finishedParent, false);

        AdvanceIfPossible();
        OnTasksUpdated?.Invoke();
        RefreshWindow();
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
            Debug.Log("[NetworkCableTaskManager] All 25 tasks completed.");
        }
    }

    private void RefreshWindow()
    {
        if (taskGroups == null || taskGroups.Length == 0)
        {
            int shown = 0;
            for (int i = _currentTask; i < TASK_COUNT && shown < 3; i++)
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
        for (int i = _currentTask; i <= groupEnd && displayed < 3; i++)
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
        if (groupTitleTMP != null) groupTitleTMP.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        if (groupTitleTMP != null) groupTitleTMP.color = original;
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
        if (!_latched[11] && _latched[10] &&
            lanTesterPort != null && lanTesterPort.IsCableInstalled &&
            IsStraightThrough())
            _latched[11] = true;

        if (!_latched[24] && _latched[23] &&
            lanTesterPort != null && lanTesterPort.IsCableInstalled &&
            IsCrossover())
            _latched[24] = true;
    }

    // ── Cable Type Helpers ────────────────────────────────────────────────────

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

    // ── Task Labels ───────────────────────────────────────────────────────────

    private static string GetTaskLabel(int index)
    {
        return index switch
        {
            // ── Group 0 ───────────────────────────────────────────────────────
             0 => "Deploy the UTP Cable to the Workspace.",
             1 => "Right-click to enter \"Detail View\" to start the configuration.",
             2 => "Use the Wire Stripper tool to cut the cable jacket.",
             3 => "Click two different wires to swap the color sequence (swap tutorial).",
             4 => "Create a Straight-Through: T-568B / T-568A (both ends, wire order checker).",
             5 => "Install RJ45 to the cable.",
             6 => "Use Crimping tool to crimp the RJ45 to the cable wires.",
             7 => "Exit detail view and store the cable back to the storage area.",
             8 => "Deploy LAN Tester to the workspace.",
             9 => "Install the network cable to the LAN tester port.",
            10 => "Turn on the power switch of the LAN tester.",
            11 => "Observe the LAN tester LED sequence to see if it matches the Straight-through.",
            12 => "Turn off the power and unplug the Straight-through cable.",

            // ── Group 1 ───────────────────────────────────────────────────────
            13 => "Deploy the Straight-through cable to the workspace.",
            14 => "Right-click to enter \"Detail View\" to start the configuration.",
            15 => "Use the Crimping tool to cut the straight-through.",
            16 => "Use Wire Stripper to cut the cable jacket.",
            17 => "Create a Cross-Over: T-568B / T-568A (both ends, wire order checker).",
            18 => "Install RJ45 to the cable.",
            19 => "Use Crimping tool to crimp the RJ45 to the cable wires.",
            20 => "Exit detail view and store the cable back to the storage area.",
            21 => "Deploy LAN Tester to the workspace.",
            22 => "Install the network cable to the LAN tester port.",
            23 => "Turn on the power switch of the LAN tester.",
            24 => "Observe the LAN tester LED sequence to see if it matches the Cross-over.",

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
