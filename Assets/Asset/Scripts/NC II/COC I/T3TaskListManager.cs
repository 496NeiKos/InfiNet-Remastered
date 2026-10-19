/*
 * ================================================================
 *  UNITY SETUP GUIDE — T3TaskListManager (COC I — Topic 3)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to the Topic 3 task container GameObject in the COC I scene.
 *
 *  ── HIERARCHY ────────────────────────────────────────────────────
 *
 *    Task Panel
 *      ├── Group Title Row
 *      │     ├── GroupTitleTMP     TMP_Text → groupTitleTMP
 *      │     └── Button            Button   → groupTitleBtn
 *      ├── Task List               → taskParent (VerticalLayoutGroup)
 *      └── Finished Bucket         → finishedParent (off-screen)
 *
 *    Group Overview Popup          → groupOverviewPopup (INACTIVE)
 *      ├── Row Parent              → groupRowParent (VerticalLayoutGroup)
 *      └── CloseBtn               Button → closePopupBtn
 *
 *  ── GROUP ROW PREFAB ─────────────────────────────────────────────
 *    Root: HorizontalLayoutGroup
 *      ├── NameTMP      TMP_Text [0]
 *      ├── ProgressTMP  TMP_Text [1]
 *      └── StatusTMP    TMP_Text [2]
 *
 *  ── TASK GROUPS — 20 tasks total (suggested, configure in Inspector)
 *    Group 0  "UEFI Setup"          tasks  0 –  4
 *    Group 1  "Windows Setup"       tasks  5 – 13
 *    Group 2  "Driver & Software"   tasks 14 – 19
 *
 *  ── PATTERN ──────────────────────────────────────────────────────
 *    Latch-based: once a task condition is true it latches permanently.
 *    Sliding window: up to 3 unlatched tasks shown, capped to the
 *    current group. Group advances after all its tasks latch.
 * ================================================================
 */

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class T3TaskListManager : MonoBehaviour
{
    public static T3TaskListManager Instance { get; private set; }

    public static event System.Action OnTasksUpdated;

    private const int TASK_COUNT = 20;

    // ── Task UI ───────────────────────────────────────────────────────────────

    [Header("Task UI")]
    [SerializeField] private Transform       taskParent;
    [SerializeField] private Transform       finishedParent;
    [SerializeField] private GameObject[]    taskObjects = new GameObject[TASK_COUNT];
    [SerializeField] private TextMeshProUGUI allTasksCompletedText;

    // ── Group Title ───────────────────────────────────────────────────────────

    [Header("Group Title")]
    [SerializeField] private TMP_Text groupTitleTMP;
    [SerializeField] private Button   groupTitleBtn;
    [SerializeField] private Color    flashColor    = Color.green;
    [SerializeField] private float    flashDuration = 1f;

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

    // ── Condition References ──────────────────────────────────────────────────

    [Header("Condition References")]
    [SerializeField] private CablePort                usbPort;
    [SerializeField] private T3SystemUnitController   systemUnit;
    [SerializeField] private T3MonitorInteraction     monitorInteraction;
    [SerializeField] private UEFINavigator            uefiNavigator;
    [Tooltip("Boot Option UEFISettingButton in Panel_Boot")]
    [SerializeField] private UEFISettingButton        bootOptionButton;
    [Tooltip("OS Type UEFISettingButton — requires 'Windows UEFI Mode'")]
    [SerializeField] private UEFISettingButton        osTypeButton;
    [Tooltip("Legacy USB Support UEFISettingButton — requires 'Enabled'")]
    [SerializeField] private UEFISettingButton        legacyUSBButton;
    [SerializeField] private WindowsSetupNavigator    windowsSetupNavigator;
    [SerializeField] private Windows10Manager         windows10Manager;
    [SerializeField] private SettingPanelController   settingController;
    [SerializeField] private DeviceManagerController  deviceManagerController;
    [SerializeField] private WU_DriverPanelManager    driverPanelManager;
    [SerializeField] private DesktopManager           desktopManager;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private bool[]       _latched           = new bool[TASK_COUNT];
    private int          _currentTask       = 0;
    private int          _currentGroupIndex = 0;
    private bool         _allDone           = false;
    private bool         _isFlashing        = false;
    private TMP_Text[][] _popupRowTMPs;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Windows10Manager.ResetAll();
        DesktopManager.ResetAll();
        FifthPhaseManager.ResetAll();

        groupTitleBtn?.onClick.AddListener(OpenPopup);
        closePopupBtn?.onClick.AddListener(ClosePopup);
    }

    private void Start()
    {
        for (int i = 0; i < TASK_COUNT; i++)
            if (taskObjects[i] != null) taskObjects[i].SetActive(false);

        groupOverviewPopup?.SetActive(false);
        if (allTasksCompletedText != null) allTasksCompletedText.gameObject.SetActive(false);

        BuildPopupRows();
        RefreshGroupTitle();
        RefreshPopup();
        RefreshWindow();
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
             0 => usbPort != null && usbPort.IsInstalled && systemUnit != null && systemUnit.IsPoweredOn,
             1 => monitorInteraction != null && monitorInteraction.CanvasOpenCount >= 1,
             2 => uefiNavigator != null && uefiNavigator.UEFIOpened,
             3 => bootOptionButton != null && bootOptionButton.CurrentValue != "None"
               && osTypeButton    != null && osTypeButton.CurrentValue    == "Windows UEFI Mode"
               && legacyUSBButton != null && legacyUSBButton.CurrentValue == "Enabled",
             4 => _latched[3] && uefiNavigator != null && uefiNavigator.BootStateSaved,
             5 => systemUnit != null && systemUnit.HasPowerCycled
               && monitorInteraction != null && monitorInteraction.CanvasOpenCount >= 2,
             6 => windowsSetupNavigator != null && windowsSetupNavigator.SetupInitializeAccessed,
             7 => windowsSetupNavigator != null && windowsSetupNavigator.LicenseFirstPhaseNextClicked,
             8 => windowsSetupNavigator != null && windowsSetupNavigator.PartitionNextClicked,
             9 => windowsSetupNavigator != null && windowsSetupNavigator.FifthPhaseAccessed,
            10 => FifthPhaseManager.SecondKeyboardSkipped,
            11 => FifthPhaseManager.LimitedSetupClicked,
            12 => FifthPhaseManager.PrivacyAccepted,
            13 => Windows10Manager.Windows10DesktopAccessed,
            14 => deviceManagerController != null && deviceManagerController.WasOpened,
            15 => settingController != null && settingController.WindowsUpdateAccessed,
            16 => driverPanelManager != null && driverPanelManager.AllDriversDownloaded,
            17 => desktopManager != null && desktopManager.MicrosoftEdgeOpened,
            18 => DesktopManager.IsInstalled(AppType.GoogleChrome) && DesktopManager.IsInstalled(AppType.WinRar),
            19 => desktopManager != null && desktopManager.ChromeExecuted && desktopManager.WinrarExecuted,
            _  => false
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

        if (_currentTask >= TASK_COUNT)
        {
            _allDone = true;
            ShowAllTasksCompleted();
            TopicManager.Instance?.MarkTopicComplete(2);
            Debug.Log("[T3TaskListManager] All tasks completed — Topic 3 marked complete.");
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
                groupTitleTMP.text = "Objective in COC I Topic 3 is met, all tasks have been completed.";
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

    private void OpenPopup()  { RefreshPopup(); groupOverviewPopup?.SetActive(true); }
    private void ClosePopup() { groupOverviewPopup?.SetActive(false); }

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

            if (isDone)                       { statusText = "Done";    rowColor = groupDoneColor; }
            else if (g == _currentGroupIndex) { statusText = "Active";  rowColor = groupActiveColor; }
            else                              { statusText = "Pending"; rowColor = groupPendingColor; }

            if (_popupRowTMPs[g][0] != null) { _popupRowTMPs[g][0].text = group.Title;                         _popupRowTMPs[g][0].color = rowColor; }
            if (_popupRowTMPs[g][1] != null) { _popupRowTMPs[g][1].text = $"{completed} / {group.TaskCount}";  _popupRowTMPs[g][1].color = rowColor; }
            if (_popupRowTMPs[g][2] != null) { _popupRowTMPs[g][2].text = statusText;                          _popupRowTMPs[g][2].color = rowColor; }
        }
    }

    // ── Completion ────────────────────────────────────────────────────────────

    private const string TopicCompleteMsg     = "All task and objective has been completed on this topic. Go to settings and navigate other topic for the COC I.";
    private const string AllTopicsCompleteMsg = "All topic has been completed, objective for COC I has been met. Proceed to COC II.";

    private void ShowAllTasksCompleted()
    {
        TopicManager.OnAllTopicsComplete += OnAllTopicsComplete;

        if (allTasksCompletedText != null)
        {
            allTasksCompletedText.text  = TopicCompleteMsg;
            allTasksCompletedText.color = Color.green;
            allTasksCompletedText.gameObject.SetActive(true);
        }
        OnTasksUpdated?.Invoke();
    }

    private void OnAllTopicsComplete()
    {
        TopicManager.OnAllTopicsComplete -= OnAllTopicsComplete;
        if (allTasksCompletedText != null)
            allTasksCompletedText.text = AllTopicsCompleteMsg;
        OnTasksUpdated?.Invoke();
    }

    private void OnDestroy()
    {
        TopicManager.OnAllTopicsComplete -= OnAllTopicsComplete;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public string GetNextIncompleteTaskText()
    {
        if (_allDone || _currentTask >= TASK_COUNT) return null;
        var obj = taskObjects[_currentTask];
        if (obj == null) return null;
        var tmp = obj.GetComponent<TMP_Text>();
        return tmp != null ? tmp.text : null;
    }

    public Color GetDisplayColor(Color fallback) => fallback;

    public int GetCompletedTaskCount()
    {
        int count = 0;
        for (int i = 0; i < TASK_COUNT; i++)
            if (_latched[i]) count++;
        return count;
    }

    public static void CheckConditions() { } // No-op; Update() handles polling
}
