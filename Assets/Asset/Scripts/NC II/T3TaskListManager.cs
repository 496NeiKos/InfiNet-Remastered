/*
 * ================================================================
 *  UNITY SETUP GUIDE — T3TaskListManager  (20-task version)
 * ================================================================
 *  STEP 1 — Create 20 task text GameObjects (in order)
 *    taskObjects[0]  "Install Flashdrive to the System Unit then Turn on the power switch"
 *    taskObjects[1]  "Enter Virtual OS via Monitor (Right-click)"
 *    taskObjects[2]  "Wait for the loading then press F2/DEL to enter and configure UEFI Boot Settings"
 *    taskObjects[3]  "Go to Advance then configure Boot Option to the installed Bootable flashdrive"
 *    taskObjects[4]  "Save UEFI Boot Configuration: F10"
 *    taskObjects[5]  "Restart the Computer and enter Virtual OS again"
 *    taskObjects[6]  "Wait for the loading to enter Windows Setup wizard"
 *    taskObjects[7]  "Install Windows setup initialization. Read the terms & conditions before accepting"
 *    taskObjects[8]  "Create/Choose Windows installation path (Install on primary disk)"
 *    taskObjects[9]  "Wait for installation process"
 *    taskObjects[10] "Setup: Region & Keyboard"
 *    taskObjects[11] "Continue without internet connection"
 *    taskObjects[12] "Configure personal privacy setting"
 *    taskObjects[13] "Set up a Password"
 *    taskObjects[14] "Check the current status of drivers in the Device Manager from the Windows Taskbar"
 *    taskObjects[15] "Check the driver's installation status in Windows Setting: Windows Update"
 *    taskObjects[16] "Click Retry on drivers that did not automatically download"
 *    taskObjects[17] "Open default Windows browser application: Microsoft Edge"
 *    taskObjects[18] "Search and download Windows applications: Google Chrome & WinRar"
 *    taskObjects[19] "Execute all downloaded Windows Applications"
 *    Place them under taskParent with a Vertical Layout Group.
 *    Create a separate off-screen finishedParent for completed tasks.
 *
 *  STEP 2 — Wire the inspector
 *    T3TaskListManager:
 *      taskParent              → layout parent holding active tasks
 *      finishedParent          → off-screen parent for completed tasks
 *      taskObjects[0..19]      → 20 TMP text GameObjects (in order above)
 *      usbPort                 → T3SystemUnit > T3SystemUnitFront > USBPort (CablePort)
 *      systemUnit              → T3SystemUnitController on the T3 System Unit root
 *      monitorInteraction      → T3MonitorInteraction on the UEFI Monitor root
 *      uefiNavigator           → UEFINavigator on the UEFI Monitor root
 *      bootOptionButton        → UEFISettingButton on the Boot Option field inside Panel_Boot
 *      windowsSetupNavigator   → WindowsSetupNavigator on WindowsSetupPanel
 *      windows10Manager        → Windows10Manager on Windows10Panel
 *      settingController       → SettingPanelController on Windows10Desktop > WindowsContent > SettingPanel
 *      deviceManagerController → DeviceManagerController on Windows10Desktop > WindowsContent > DeviceManagerPanel
 *      driverPanelManager      → WU_DriverPanelManager on Windows10Desktop
 *      desktopManager          → DesktopManager on DesktopContent
 *      requiredTaskCount       → 20
 *
 *  TASK CONDITIONS (auto-evaluated; no manual wiring needed beyond inspector)
 *    Task  1 — USB installed + powered on (latch; reverts only on USB removal)
 *    Task  2 — monitorInteraction.CanvasOpenCount >= 1
 *    Task  3 — uefiNavigator.UEFIOpened
 *    Task  4 — bootOptionButton.CurrentValue != "None"  (Kingston USB selected)
 *    Task  5 — uefiNavigator.BootStateSaved AND Task 4 complete
 *    Task  6 — systemUnit.HasPowerCycled AND monitorInteraction.CanvasOpenCount >= 2
 *    Task  7 — windowsSetupNavigator.SetupInitializeAccessed
 *    Task  8 — windowsSetupNavigator.LicenseFirstPhaseNextClicked
 *    Task  9 — windowsSetupNavigator.PartitionNextClicked
 *    Task 10 — windowsSetupNavigator.FifthPhaseAccessed
 *    Task 11 — FifthPhaseManager.SecondKeyboardSkipped
 *    Task 12 — FifthPhaseManager.LimitedSetupClicked
 *    Task 13 — FifthPhaseManager.PrivacyAccepted
 *    Task 14 — Windows10Manager.Windows10DesktopAccessed
 *    Task 15 — deviceManagerController.WasOpened
 *    Task 16 — settingController.WindowsUpdateAccessed
 *    Task 17 — driverPanelManager.AllDriversDownloaded
 *    Task 18 — desktopManager.MicrosoftEdgeOpened
 *    Task 19 — DesktopManager.IsInstalled(GoogleChrome) AND IsInstalled(WinRar)
 *    Task 20 — desktopManager.ChromeExecuted AND desktopManager.WinrarExecuted
 *
 *  When all 20 tasks complete, TopicManager.MarkTopicComplete(2) fires.
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

    // Fires whenever a task completes or reverts — SingleTaskDisplay subscribes to this.
    public static event System.Action OnTasksUpdated;

    private static readonly Color GoldColor = new Color(1f, 0.843f, 0f);

    [Header("Task UI")]
    [SerializeField] private Transform taskParent;
    [SerializeField] private Transform finishedParent;
    [SerializeField] private GameObject[] taskObjects;

    [Header("Completion UI")]
    [SerializeField] private TextMeshProUGUI allTasksCompletedText;

    [Header("Condition References")]
    [SerializeField] private CablePort usbPort;
    [SerializeField] private T3SystemUnitController systemUnit;
    [SerializeField] private T3MonitorInteraction monitorInteraction;
    [SerializeField] private UEFINavigator uefiNavigator;
    [Tooltip("The Boot Option UEFISettingButton in Panel_Boot — Task 4 reads its CurrentValue.")]
    [SerializeField] private UEFISettingButton bootOptionButton;
    [SerializeField] private WindowsSetupNavigator windowsSetupNavigator;
    [SerializeField] private Windows10Manager windows10Manager;
    [SerializeField] private SettingPanelController settingController;
    [SerializeField] private DeviceManagerController deviceManagerController;
    [SerializeField] private WU_DriverPanelManager driverPanelManager;
    [SerializeField] private DesktopManager desktopManager;

    [Tooltip("How many tasks (from index 0) must complete to mark Topic 3 done.")]
    [SerializeField] private int requiredTaskCount = 20;

    // Task 1 latch: once USB+power are met simultaneously, only USB removal can revert the task.
    private bool _task1Latched;

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

    private string _displayOverride      = null;
    private bool   _isCompletionOverride = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Reset all Topic 3 static state on scene load.
        Windows10Manager.ResetAll();
        DesktopManager.ResetAll();
        FifthPhaseManager.ResetAll();
    }

    private void Start()
    {
        if (taskObjects == null || taskObjects.Length < 20)
        {
            Debug.LogError("[T3TaskListManager] Assign all 20 task objects in the inspector.");
            return;
        }

        _tasks = new List<TaskEntry>
        {
            // Task 1 — install flash drive + power on system unit
            new TaskEntry
            {
                taskObject    = taskObjects[0],
                originalIndex = 0,
                condition     = () =>
                {
                    bool usbIn = usbPort != null && usbPort.IsInstalled;
                    bool on    = systemUnit != null && systemUnit.IsPoweredOn;
                    if (usbIn && on) _task1Latched = true;
                    return _task1Latched && usbIn;
                }
            },
            // Task 2 — right-click monitor to enter Virtual OS (canvas)
            new TaskEntry
            {
                taskObject    = taskObjects[1],
                originalIndex = 1,
                condition     = () => monitorInteraction != null && monitorInteraction.CanvasOpenCount >= 1
            },
            // Task 3 — press F2/DEL during loading to open UEFI panel
            new TaskEntry
            {
                taskObject    = taskObjects[2],
                originalIndex = 2,
                condition     = () => uefiNavigator != null && uefiNavigator.UEFIOpened
            },
            // Task 4 — Boot Option field changed from "None" to the Kingston USB device
            new TaskEntry
            {
                taskObject    = taskObjects[3],
                originalIndex = 3,
                condition     = () => bootOptionButton != null && bootOptionButton.CurrentValue != "None"
            },
            // Task 5 — press F10 and confirm save (Task 4 must be done first)
            new TaskEntry
            {
                taskObject    = taskObjects[4],
                originalIndex = 4,
                condition     = () => _tasks != null && _tasks[3].isCompleted
                                   && uefiNavigator != null && uefiNavigator.BootStateSaved
            },
            // Task 6 — power-cycle system unit then re-enter Virtual OS
            new TaskEntry
            {
                taskObject    = taskObjects[5],
                originalIndex = 5,
                condition     = () => systemUnit != null && systemUnit.HasPowerCycled
                                   && monitorInteraction != null && monitorInteraction.CanvasOpenCount >= 2
            },
            // Task 7 — Windows Setup wizard opened (SetUpInitialize shown)
            new TaskEntry
            {
                taskObject    = taskObjects[6],
                originalIndex = 6,
                condition     = () => windowsSetupNavigator != null && windowsSetupNavigator.SetupInitializeAccessed
            },
            // Task 8 — accepted license terms (FirstPhase Next clicked with checkbox ticked)
            new TaskEntry
            {
                taskObject    = taskObjects[7],
                originalIndex = 7,
                condition     = () => windowsSetupNavigator != null && windowsSetupNavigator.LicenseFirstPhaseNextClicked
            },
            // Task 9 — chose installation path and advanced from ThirdPhase
            new TaskEntry
            {
                taskObject    = taskObjects[8],
                originalIndex = 8,
                condition     = () => windowsSetupNavigator != null && windowsSetupNavigator.PartitionNextClicked
            },
            // Task 10 — installation complete, FifthPhase (Region) accessed
            new TaskEntry
            {
                taskObject    = taskObjects[9],
                originalIndex = 9,
                condition     = () => windowsSetupNavigator != null && windowsSetupNavigator.FifthPhaseAccessed
            },
            // Task 11 — Region & Keyboard done (SecondKeyboardLayout Skip clicked)
            new TaskEntry
            {
                taskObject    = taskObjects[10],
                originalIndex = 10,
                condition     = () => FifthPhaseManager.SecondKeyboardSkipped
            },
            // Task 12 — LimitedSetup clicked (continue without internet)
            new TaskEntry
            {
                taskObject    = taskObjects[11],
                originalIndex = 11,
                condition     = () => FifthPhaseManager.LimitedSetupClicked
            },
            // Task 13 — PrivacySetting Accept clicked
            new TaskEntry
            {
                taskObject    = taskObjects[12],
                originalIndex = 12,
                condition     = () => FifthPhaseManager.PrivacyAccepted
            },
            // Task 14 — Windows10Desktop accessed (password set up and entered)
            new TaskEntry
            {
                taskObject    = taskObjects[13],
                originalIndex = 13,
                condition     = () => Windows10Manager.Windows10DesktopAccessed
            },
            // Task 15 — Device Manager panel opened from Taskbar
            new TaskEntry
            {
                taskObject    = taskObjects[14],
                originalIndex = 14,
                condition     = () => deviceManagerController != null && deviceManagerController.WasOpened
            },
            // Task 16 — Windows Update panel opened in Settings (2ndLevel)
            new TaskEntry
            {
                taskObject    = taskObjects[15],
                originalIndex = 15,
                condition     = () => settingController != null && settingController.WindowsUpdateAccessed
            },
            // Task 17 — all drivers reached Downloaded state
            new TaskEntry
            {
                taskObject    = taskObjects[16],
                originalIndex = 16,
                condition     = () => driverPanelManager != null && driverPanelManager.AllDriversDownloaded
            },
            // Task 18 — Microsoft Edge opened from desktop
            new TaskEntry
            {
                taskObject    = taskObjects[17],
                originalIndex = 17,
                condition     = () => desktopManager != null && desktopManager.MicrosoftEdgeOpened
            },
            // Task 19 — Google Chrome and WinRar downloaded via Microsoft Edge
            new TaskEntry
            {
                taskObject    = taskObjects[18],
                originalIndex = 18,
                condition     = () => DesktopManager.IsInstalled(AppType.GoogleChrome)
                                   && DesktopManager.IsInstalled(AppType.WinRar)
            },
            // Task 20 — both apps executed from the desktop
            new TaskEntry
            {
                taskObject    = taskObjects[19],
                originalIndex = 19,
                condition     = () => desktopManager != null
                                   && desktopManager.ChromeExecuted && desktopManager.WinrarExecuted
            },
        };

        // Subscribe to instant-fire events so CheckConditions runs without waiting for a poll.
        if (usbPort    != null) usbPort.OnInstalled    += CheckConditions;
        if (usbPort    != null) usbPort.OnUninstalled  += CheckConditions;
        if (systemUnit != null) systemUnit.OnPoweredOn += CheckConditions;

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

    private void OnDestroy()
    {
        if (usbPort    != null) usbPort.OnInstalled    -= CheckConditions;
        if (usbPort    != null) usbPort.OnUninstalled  -= CheckConditions;
        if (systemUnit != null) systemUnit.OnPoweredOn -= CheckConditions;
    }

    // Returns override text (completion banner) when set, otherwise the next incomplete task.
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
        OnTasksUpdated?.Invoke();

        int required = Mathf.Clamp(requiredTaskCount, 1, _tasks.Count);
        if (_tasks.Take(required).All(t => t.isCompleted))
        {
            ShowAllTasksCompleted();
            TopicManager.Instance?.MarkTopicComplete(2);
            Debug.Log("[T3TaskListManager] All required tasks complete — Topic 3 marked complete.");
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

    private void ShowAllTasksCompleted()
    {
        if (allTasksCompletedText != null)
        {
            allTasksCompletedText.text = "All task Completed!";
            allTasksCompletedText.color = Color.green;
            allTasksCompletedText.gameObject.SetActive(true);
        }
        _displayOverride      = "All task Completed!";
        _isCompletionOverride = true;
        OnTasksUpdated?.Invoke();
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
