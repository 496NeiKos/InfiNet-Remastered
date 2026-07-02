using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class NCIITaskListManager : MonoBehaviour
{
    public static NCIITaskListManager Instance { get; private set; }

    // Fires whenever a task completes or reverts — SingleTaskDisplay subscribes to this.
    public static event Action OnTasksUpdated;

    [Header("Disassembly UI")]
    [SerializeField] private Transform disassemblyTaskParent;
    [SerializeField] private Transform disassemblyFinishedParent;
    [SerializeField] private GameObject[] disassemblyTaskObjects;

    [Header("Assembly UI")]
    [SerializeField] private Transform assemblyTaskParent;
    [SerializeField] private Transform assemblyFinishedParent;
    [SerializeField] private GameObject[] assemblyTaskObjects;

    [Header("Condition References")]
    [SerializeField] private CoverController coverController;
    [SerializeField] private MotherboardController motherboardController;
    [SerializeField] private HardwareHolder psuHolder;
    [SerializeField] private HardwareHolder hddHolder;
    [SerializeField] private HardwareHolder cpuHolder;
    [SerializeField] private HardwareHolder heatsinkHolder;
    [SerializeField] private HardwareHolder ram1Holder;
    [SerializeField] private HardwareHolder ram2Holder;
    [SerializeField] private HardwareHolder cmosHolder;
    [SerializeField] private HardwareHolder ssdHolder;

    [Header("Assembly Power Switches")]
    [SerializeField] private PowerButton suPowerButton;
    [SerializeField] private AVRPowerButton avrPowerButton;
    [SerializeField] private MonitorPowerButton monitorPowerButton;
    [SerializeField] private PSUSwitchController psuSwitch;

    private class TaskEntry
    {
        public GameObject taskObject;
        public int originalIndex;
        public bool isCompleted;
        public bool isFlashing;
        public Func<bool> condition;
    }

    private class TaskPhase
    {
        public List<TaskEntry> tasks;
        public Transform taskParent;
        public Transform finishedParent;
    }

    private TaskPhase _disassembly;
    private TaskPhase _assembly;
    private BackPortSlot[] _allBackPortSlots;
    private bool _showingAssembly = false;
    private const int WindowSize = 3;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _allBackPortSlots = FindObjectsOfType<BackPortSlot>(true);

        if (disassemblyTaskObjects == null || disassemblyTaskObjects.Length < 4)
        {
            Debug.LogError("[NCIITaskListManager] Assign all 4 disassembly task objects in the inspector.");
            return;
        }

        BuildDisassemblyPhase();
        BuildAssemblyPhase();

        InitPhase(_disassembly);
        InitPhase(_assembly);

        if (assemblyTaskParent != null)
            assemblyTaskParent.gameObject.SetActive(false);

        RefreshWindow(_disassembly);
        RefreshWindow(_assembly);
    }

    private void BuildDisassemblyPhase()
    {
        _disassembly = new TaskPhase
        {
            taskParent = disassemblyTaskParent,
            finishedParent = disassemblyFinishedParent,
            tasks = new List<TaskEntry>
            {
                // Task 1: Unplug all back-panel cables
                new TaskEntry
                {
                    taskObject = disassemblyTaskObjects[0],
                    originalIndex = 0,
                    condition = () => _allBackPortSlots != null
                                   && _allBackPortSlots.Length > 0
                                   && _allBackPortSlots.All(p => p.IsUninstalled)
                },
                // Task 2: Open the side cover
                new TaskEntry
                {
                    taskObject = disassemblyTaskObjects[1],
                    originalIndex = 1,
                    condition = () => coverController != null && coverController.IsOpen()
                },
                // Task 3: Remove system unit components (motherboard, PSU, HDD)
                new TaskEntry
                {
                    taskObject = disassemblyTaskObjects[2],
                    originalIndex = 2,
                    condition = () =>
                        (motherboardController == null || motherboardController.IsUninstalledFromSystemUnit) &&
                        (psuHolder == null || psuHolder.IsAvailable()) &&
                        (hddHolder == null || hddHolder.IsAvailable())
                },
                // Task 4: Remove motherboard components (CPU, heatsink, RAM, CMOS, SSD)
                new TaskEntry
                {
                    taskObject = disassemblyTaskObjects[3],
                    originalIndex = 3,
                    condition = () =>
                        (cpuHolder == null || cpuHolder.IsAvailable()) &&
                        (heatsinkHolder == null || heatsinkHolder.IsAvailable()) &&
                        (ram1Holder == null || ram1Holder.IsAvailable()) &&
                        (ram2Holder == null || ram2Holder.IsAvailable()) &&
                        (cmosHolder == null || cmosHolder.IsAvailable()) &&
                        (ssdHolder == null || ssdHolder.IsAvailable())
                }
            }
        };
    }

    private void BuildAssemblyPhase()
    {
        if (assemblyTaskObjects == null || assemblyTaskObjects.Length < 4)
        {
            Debug.LogWarning("[NCIITaskListManager] Assign all 4 assembly task objects in the inspector — assembly tab will be empty.");
            return;
        }

        _assembly = new TaskPhase
        {
            taskParent = assemblyTaskParent,
            finishedParent = assemblyFinishedParent,
            tasks = new List<TaskEntry>
            {
                // Task 1: Install all motherboard components (CPU, heatsink, RAM, CMOS, SSD).
                // "Installed" = the storage holder is no longer available (prefab is active in the scene),
                // which is the exact inverse of the disassembly removal check.
                new TaskEntry
                {
                    taskObject = assemblyTaskObjects[0],
                    originalIndex = 0,
                    condition = () =>
                        (cpuHolder == null || !cpuHolder.IsAvailable()) &&
                        (heatsinkHolder == null || !heatsinkHolder.IsAvailable()) &&
                        (ram1Holder == null || !ram1Holder.IsAvailable()) &&
                        (ram2Holder == null || !ram2Holder.IsAvailable()) &&
                        (cmosHolder == null || !cmosHolder.IsAvailable()) &&
                        (ssdHolder == null || !ssdHolder.IsAvailable())
                },
                // Task 2: Install system unit components (motherboard back in the case, PSU, HDD).
                new TaskEntry
                {
                    taskObject = assemblyTaskObjects[1],
                    originalIndex = 1,
                    condition = () =>
                        (motherboardController == null || !motherboardController.IsUninstalledFromSystemUnit) &&
                        (psuHolder == null || !psuHolder.IsAvailable()) &&
                        (hddHolder == null || !hddHolder.IsAvailable())
                },
                // Task 3: Plug all the cables (every back-panel port reports installed).
                new TaskEntry
                {
                    taskObject = assemblyTaskObjects[2],
                    originalIndex = 2,
                    condition = () => _allBackPortSlots != null
                                   && _allBackPortSlots.Length > 0
                                   && _allBackPortSlots.All(p => p.IsInstalled)
                },
                // Task 4: Turn on all power button switches (PSU switch, AVR, monitor, system unit).
                new TaskEntry
                {
                    taskObject = assemblyTaskObjects[3],
                    originalIndex = 3,
                    condition = () => AnyPowerSwitchAssigned() &&
                        (suPowerButton == null || suPowerButton.IsPoweredOn) &&
                        (avrPowerButton == null || avrPowerButton.IsPoweredOn) &&
                        (monitorPowerButton == null || monitorPowerButton.IsPoweredOn) &&
                        (psuSwitch == null || psuSwitch.IsOn)
                }
            }
        };
    }

    // Returns the text of the next incomplete task in the currently active phase.
    // Returns null if all tasks are done or tasks haven't initialised yet.
    public string GetNextIncompleteTaskText()
    {
        TaskPhase active = _showingAssembly ? _assembly : _disassembly;
        if (active?.tasks == null) return null;
        var next = active.tasks.FirstOrDefault(t => !t.isCompleted);
        if (next == null) return null;
        var tmp = next.taskObject.GetComponent<TextMeshProUGUI>();
        return tmp != null ? tmp.text : null;
    }

    private bool AnyPowerSwitchAssigned() =>
        suPowerButton != null || avrPowerButton != null || monitorPowerButton != null || psuSwitch != null;

    private void InitPhase(TaskPhase phase)
    {
        if (phase == null || phase.tasks == null) return;

        foreach (var task in phase.tasks)
        {
            task.taskObject.SetActive(false);
            task.taskObject.transform.SetParent(phase.taskParent, false);
            task.taskObject.transform.SetSiblingIndex(task.originalIndex);
            var tmp = task.taskObject.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.color = Color.white;
        }
    }

    public static void CheckConditions()
    {
        if (Instance == null) return;
        Instance.EvaluateConditions();
    }

    private void EvaluateConditions()
    {
        if (!gameObject.activeInHierarchy) return;

        // Only the visible tab is evaluated. The scene starts fully assembled, so the
        // assembly conditions are all true at startup — gating on the active tab keeps
        // them from auto-completing before the player has actually disassembled anything.
        EvaluatePhase(_showingAssembly ? _assembly : _disassembly);
    }

    private void EvaluatePhase(TaskPhase phase)
    {
        if (phase == null || phase.tasks == null) return;

        foreach (var task in phase.tasks)
        {
            if (task.isFlashing) continue;

            bool met = task.condition();

            if (!task.isCompleted && met)
            {
                task.isCompleted = true;
                StartCoroutine(FlashAndComplete(phase, task));
            }
            else if (task.isCompleted && !met)
            {
                task.isCompleted = false;
                task.taskObject.transform.SetParent(phase.taskParent, false);
                task.taskObject.transform.SetSiblingIndex(task.originalIndex);
                task.taskObject.SetActive(false);
                RefreshWindow(phase);
                if (task.taskObject.activeSelf)
                    StartCoroutine(FlashRevert(task));
            }
        }
    }

    private IEnumerator FlashAndComplete(TaskPhase phase, TaskEntry task)
    {
        task.isFlashing = true;
        var tmp = task.taskObject.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.color = Color.green;
        yield return new WaitForSeconds(0.6f);
        task.isFlashing = false;
        task.taskObject.transform.SetParent(phase.finishedParent, false);
        task.taskObject.SetActive(false);
        RefreshWindow(phase);
        OnTasksUpdated?.Invoke();

        if (phase == _disassembly && IsDisassemblyComplete())
            SwitchToAssembly();
        else if (phase == _assembly && IsAssemblyComplete())
        {
            TopicManager.Instance?.MarkTopicComplete(0);
            Debug.Log("[NCIITaskListManager] Assembly complete — Topic 1 marked complete.");
        }
        else
            EvaluatePhase(phase);
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

    private void RefreshWindow(TaskPhase phase)
    {
        if (phase == null || phase.tasks == null) return;

        var incomplete = phase.tasks
            .Where(t => !t.isCompleted)
            .OrderBy(t => t.originalIndex)
            .ToList();

        for (int i = 0; i < incomplete.Count; i++)
            incomplete[i].taskObject.SetActive(i < WindowSize);
    }

    private bool IsDisassemblyComplete() =>
        _disassembly != null && _disassembly.tasks.All(t => t.isCompleted);

    private bool IsAssemblyComplete() =>
        _assembly != null && _assembly.tasks.All(t => t.isCompleted);

    private void SwitchToAssembly()
    {
        _showingAssembly = true;

        if (disassemblyTaskParent != null)
            disassemblyTaskParent.gameObject.SetActive(false);
        if (assemblyTaskParent != null)
            assemblyTaskParent.gameObject.SetActive(true);

        RefreshWindow(_assembly);
        EvaluatePhase(_assembly);
        OnTasksUpdated?.Invoke();

        Debug.Log("[NCIITaskListManager] Disassembly complete — switching to assembly phase.");
    }
}
