/*
 * ================================================================
 *  UNITY SETUP GUIDE — T3TaskListManager  (11-task version)
 * ================================================================
 *  STEP 1 — Manager GameObject
 *    - Keep the existing T3TaskListManager GameObject inside Topic 3's
 *      taskListContainer.  Swap the script for this updated version.
 *
 *  STEP 2 — Create 11 task text GameObjects
 *    Delete the old task TMP children and create 11 new ones:
 *      taskObjects[0]  "Install Flashdrive to the System Unit and Press Power On"
 *      taskObjects[1]  "Enter Virtual OS via Monitor"
 *      taskObjects[2]  "Press f2/del to enter UEFI Boot Settings"
 *      taskObjects[3]  "Save UEFI Configuration: f10"
 *      taskObjects[4]  "Reset System Unit and Enter Virtual OS again"
 *      taskObjects[5]  "Complete Windows Installation set up"
 *      taskObjects[6]  "Check and Install Drivers Installation status in the Windows Setting"
 *      taskObjects[7]  "Open Device Manager and Check Drivers"
 *      taskObjects[8]  "Download Windows Application through Microsoft Edge: Google Chrome, Winrar"
 *      taskObjects[9]  "Execute Installed Windows Application"
 *      taskObjects[10] "Shutdown the Computer"
 *    Place them under taskParent with a Vertical Layout Group.
 *    Create a separate off-screen finishedParent for completed tasks.
 *
 *  STEP 3 — Wire the inspector
 *    T3TaskListManager:
 *      taskParent              → layout parent holding active tasks
 *      finishedParent          → off-screen parent for completed tasks
 *      taskObjects[0..10]      → 11 TMP text GameObjects (in order above)
 *      usbPort                 → T3SystemUnit > T3SystemUnitFront > USBPort (CablePort)
 *      systemUnit              → T3SystemUnitController on the T3 System Unit root
 *      monitorInteraction      → T3MonitorInteraction on the UEFI Monitor root
 *      uefiNavigator           → UEFINavigator on the UEFI Monitor root
 *      bootStateValidator      → UEFIBootStateValidator on the UEFI Monitor root
 *      windows10Manager        → Windows10Manager on Windows10Panel
 *      driverPanelManager      → WU_DriverPanelManager on Windows10Desktop
 *      deviceManagerController → DeviceManagerController on DeviceManagerPanel
 *      desktopManager          → DesktopManager on DesktopContent
 *      requiredTaskCount       → 11
 *
 *  TASK CONDITIONS (auto-evaluated; no manual wiring needed beyond inspector)
 *    Task 1  — USB installed + powered on (reverts ONLY if USB removed, not on power-off)
 *    Task 2  — monitorInteraction.CanvasOpenCount >= 1
 *    Task 3  — uefiNavigator.UEFIOpened
 *    Task 4  — uefiNavigator.BootStateSaved && bootStateValidator.AllCorrect()
 *    Task 5  — systemUnit.HasPowerCycled && monitorInteraction.CanvasOpenCount >= 2
 *    Task 6  — Windows10Manager.PasswordLoginShown  (static latch)
 *    Task 7  — driverPanelManager.AllDriversDownloaded
 *    Task 8  — tasks 0-6 all complete && deviceManagerController.WasOpened
 *    Task 9  — DesktopManager.IsInstalled(GoogleChrome) && IsInstalled(WinRar)
 *    Task 10 — desktopManager.ChromeExecuted && desktopManager.WinrarExecuted
 *    Task 11 — Windows10Manager.ShutdownTriggered  (static latch)
 *
 *  When all 11 tasks complete, TopicManager.MarkTopicComplete(2) fires.
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

    [Header("Task UI")]
    [SerializeField] private Transform taskParent;
    [SerializeField] private Transform finishedParent;
    [SerializeField] private GameObject[] taskObjects;

    [Header("Condition References")]
    [SerializeField] private CablePort usbPort;
    [SerializeField] private T3SystemUnitController systemUnit;
    [SerializeField] private T3MonitorInteraction monitorInteraction;
    [SerializeField] private UEFINavigator uefiNavigator;
    [SerializeField] private UEFIBootStateValidator bootStateValidator;
    [SerializeField] private Windows10Manager windows10Manager;
    [SerializeField] private WU_DriverPanelManager driverPanelManager;
    [SerializeField] private DeviceManagerController deviceManagerController;
    [SerializeField] private DesktopManager desktopManager;

    [Tooltip("How many tasks (from index 0) must complete to mark Topic 3 done.")]
    [SerializeField] private int requiredTaskCount = 11;

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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (taskObjects == null || taskObjects.Length < 11)
        {
            Debug.LogError("[T3TaskListManager] Assign all 11 task objects in the inspector.");
            return;
        }

        _tasks = new List<TaskEntry>
        {
            // Task 1 — install flash drive + power on system unit.
            // Reverts ONLY if the USB is removed; powering the unit off does NOT revert it.
            new TaskEntry
            {
                taskObject    = taskObjects[0],
                originalIndex = 0,
                condition     = () =>
                {
                    bool usbIn = usbPort != null && usbPort.IsInstalled;
                    bool on    = systemUnit != null && systemUnit.IsPoweredOn;
                    // Latch once both conditions are simultaneously true.
                    if (usbIn && on) _task1Latched = true;
                    // After latching, only USB removal can revert.
                    return _task1Latched && usbIn;
                }
            },
            // Task 2 — right-click UEFI monitor to enter canvas (Virtual OS)
            new TaskEntry
            {
                taskObject    = taskObjects[1],
                originalIndex = 1,
                condition     = () => monitorInteraction != null && monitorInteraction.CanvasOpenCount >= 1
            },
            // Task 3 — press F2/Del to open UEFI panel
            new TaskEntry
            {
                taskObject    = taskObjects[2],
                originalIndex = 2,
                condition     = () => uefiNavigator != null && uefiNavigator.UEFIOpened
            },
            // Task 4 — save UEFI via F10 AND configuration must be correctly set.
            // Mirrors the gate in T3MonitorController that unlocks WindowsSetupPanel.
            new TaskEntry
            {
                taskObject    = taskObjects[3],
                originalIndex = 3,
                condition     = () => uefiNavigator != null && uefiNavigator.BootStateSaved
                                   && bootStateValidator != null && bootStateValidator.AllCorrect()
            },
            // Task 5 — power-cycle system unit then re-enter virtual OS
            new TaskEntry
            {
                taskObject    = taskObjects[4],
                originalIndex = 4,
                condition     = () => systemUnit != null && systemUnit.HasPowerCycled
                                   && monitorInteraction != null && monitorInteraction.CanvasOpenCount >= 2
            },
            // Task 6 — complete Windows setup (password login panel becomes active)
            new TaskEntry
            {
                taskObject    = taskObjects[5],
                originalIndex = 5,
                condition     = () => Windows10Manager.PasswordLoginShown
            },
            // Task 7 — all drivers reach "Downloaded" state
            new TaskEntry
            {
                taskObject    = taskObjects[6],
                originalIndex = 6,
                condition     = () => driverPanelManager != null && driverPanelManager.AllDriversDownloaded
            },
            // Task 8 — open Device Manager (gated: tasks 1-7 must be complete first)
            new TaskEntry
            {
                taskObject    = taskObjects[7],
                originalIndex = 7,
                condition     = () => _tasks != null && _tasks.Take(7).All(t => t.isCompleted)
                                   && deviceManagerController != null && deviceManagerController.WasOpened
            },
            // Task 9 — download both Chrome and WinRar via Microsoft Edge
            new TaskEntry
            {
                taskObject    = taskObjects[8],
                originalIndex = 8,
                condition     = () => DesktopManager.IsInstalled(AppType.GoogleChrome)
                                   && DesktopManager.IsInstalled(AppType.WinRar)
            },
            // Task 10 — execute both installed apps (open ChromePanel and WinrarPanel)
            new TaskEntry
            {
                taskObject    = taskObjects[9],
                originalIndex = 9,
                condition     = () => desktopManager != null
                                   && desktopManager.ChromeExecuted && desktopManager.WinrarExecuted
            },
            // Task 11 — shutdown the computer via the TaskBar shutdown button
            new TaskEntry
            {
                taskObject    = taskObjects[10],
                originalIndex = 10,
                condition     = () => Windows10Manager.ShutdownTriggered
            }
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
            if (tmp != null) tmp.color = Color.white;
        }

        RefreshWindow();
    }

    private void OnDestroy()
    {
        if (usbPort    != null) usbPort.OnInstalled    -= CheckConditions;
        if (usbPort    != null) usbPort.OnUninstalled  -= CheckConditions;
        if (systemUnit != null) systemUnit.OnPoweredOn -= CheckConditions;
    }

    // Returns the text of the next incomplete task, or null if all done / not yet initialised.
    public string GetNextIncompleteTaskText()
    {
        if (_tasks == null) return null;
        var next = _tasks.FirstOrDefault(t => !t.isCompleted);
        if (next == null) return null;
        var tmp = next.taskObject.GetComponent<TextMeshProUGUI>();
        return tmp != null ? tmp.text : null;
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
        OnTasksUpdated?.Invoke();

        int required = Mathf.Clamp(requiredTaskCount, 1, _tasks.Count);
        if (_tasks.Take(required).All(t => t.isCompleted))
        {
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
        if (tmp != null) tmp.color = Color.white;
        OnTasksUpdated?.Invoke();
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
