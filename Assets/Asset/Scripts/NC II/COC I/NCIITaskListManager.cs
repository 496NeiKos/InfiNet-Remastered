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

    private static readonly Color GoldColor = new Color(1f, 0.843f, 0f);

    [Header("Disassembly UI")]
    [SerializeField] private Transform disassemblyTaskParent;
    [SerializeField] private Transform disassemblyFinishedParent;
    [SerializeField] private GameObject[] disassemblyTaskObjects; // 15 entries

    [Header("Assembly UI")]
    [SerializeField] private Transform assemblyTaskParent;
    [SerializeField] private Transform assemblyFinishedParent;
    [SerializeField] private GameObject[] assemblyTaskObjects; // 14 entries

    [Header("Transition UI")]
    [SerializeField] private TextMeshProUGUI transitionText;
    [Tooltip("Seconds the transition message is shown before the assembly task list appears.")]
    [SerializeField] private float transitionDuration = 3f;
    [SerializeField] private string transitionMessage = "Disassembly task completed! now transitioning to Assembly task";

    [Header("Completion UI")]
    [SerializeField] private TextMeshProUGUI allTasksCompletedText;

    [Header("Hardware Controllers")]
    [SerializeField] private CoverController coverController;
    [SerializeField] private MotherboardController motherboardController;
    [SerializeField] private CPUSlotController cpuSlotController;
    [SerializeField] private GPUController gpuController;

    [Header("Hardware Holders")]
    [SerializeField] private HardwareHolder psuHolder;
    [SerializeField] private HardwareHolder hddHolder;
    [SerializeField] private HardwareHolder cpuHolder;
    [SerializeField] private HardwareHolder heatsinkHolder;
    [SerializeField] private HardwareHolder ram1Holder;
    [SerializeField] private HardwareHolder ram2Holder;
    [SerializeField] private HardwareHolder cmosHolder;
    [SerializeField] private HardwareHolder ssdHolder;

    [Header("Power Switches")]
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
        public bool canRevert = true;
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

    // Persistent flags for one-way transient-state conditions
    private bool _gpuPreparedForRemoval = false;
    private bool _mbOpenedFromWorkspace = false;
    private bool _mbAssemblyReturnedToWorkspace = false;
    private bool _task8Latched = false;   // MB + HDD + PSU all installed — never reverts
    private bool _task10Latched = false;  // MB phase-1 screws + phase-2 cables all installed — never reverts
    private bool _task12Latched = false;  // cover closed and all screws in — never reverts

    // Override text fed to SingleTaskDisplay (transition message or completion banner).
    private string _displayOverride      = null;
    private bool   _isCompletionOverride = false; // true → SingleTaskDisplay shows it in green

    // Cached component refs resolved from HardwareHolder.hardwarePrefab in Start
    private CPUController _cpuController;
    private HDDController _hddController;
    private HeatsinkController _heatsinkController;
    private SSDController _ssdController;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _allBackPortSlots = FindObjectsOfType<BackPortSlot>(true);

        _cpuController     = cpuHolder?.hardwarePrefab?.GetComponent<CPUController>();
        _hddController     = hddHolder?.hardwarePrefab?.GetComponent<HDDController>();
        _heatsinkController = heatsinkHolder?.hardwarePrefab?.GetComponent<HeatsinkController>();
        _ssdController     = ssdHolder?.hardwarePrefab?.GetComponent<SSDController>();

        if (disassemblyTaskObjects == null || disassemblyTaskObjects.Length < 16)
        {
            Debug.LogError("[NCIITaskListManager] Assign all 16 disassembly task objects in the inspector.");
            return;
        }

        BuildDisassemblyPhase();
        BuildAssemblyPhase();

        InitPhase(_disassembly);
        InitPhase(_assembly);

        if (assemblyTaskParent != null)
            assemblyTaskParent.gameObject.SetActive(false);

        if (transitionText != null)
            transitionText.gameObject.SetActive(false);
        if (allTasksCompletedText != null)
            allTasksCompletedText.gameObject.SetActive(false);

        RefreshWindow(_disassembly);
        RefreshWindow(_assembly);
    }

    // ── Build Disassembly Phase (16 tasks) ───────────────────────────────────

    private void BuildDisassemblyPhase()
    {
        _disassembly = new TaskPhase
        {
            taskParent    = disassemblyTaskParent,
            finishedParent = disassemblyFinishedParent,
            tasks = new List<TaskEntry>
            {
                // D-Task 1: Equip all 7 PPE items before starting work
                new TaskEntry
                {
                    taskObject    = disassemblyTaskObjects[0],
                    originalIndex = 0,
                    condition = () => PPEInventoryManager.Instance != null && PPEInventoryManager.Instance.AreAllPPEEquipped()
                },

                // D-Task 2: Turn off all power switches (SU front, monitor back, AVR, PSU switch)
                new TaskEntry
                {
                    taskObject    = disassemblyTaskObjects[1],
                    originalIndex = 1,
                    condition = () =>
                        (suPowerButton != null || monitorPowerButton != null || avrPowerButton != null || psuSwitch != null) &&
                        (suPowerButton      == null || !suPowerButton.IsPoweredOn) &&
                        (monitorPowerButton == null || !monitorPowerButton.IsPoweredOn) &&
                        (avrPowerButton     == null || !avrPowerButton.IsPoweredOn) &&
                        (psuSwitch          == null || !psuSwitch.IsOn)
                },

                // D-Task 3: Unplug all back cables (SU, monitor, AVR)
                new TaskEntry
                {
                    taskObject    = disassemblyTaskObjects[2],
                    originalIndex = 2,
                    condition = () =>
                        _allBackPortSlots != null &&
                        _allBackPortSlots.Length > 0 &&
                        _allBackPortSlots.All(p => p.IsUninstalled)
                },

                // D-Task 4: Unscrew the system unit side cover screw
                new TaskEntry
                {
                    taskObject    = disassemblyTaskObjects[3],
                    originalIndex = 3,
                    condition = () => coverController != null && coverController.AreAllScrewsUnscrewed()
                },

                // D-Task 5: Slide cover right to open
                new TaskEntry
                {
                    taskObject    = disassemblyTaskObjects[4],
                    originalIndex = 4,
                    condition = () => coverController != null && coverController.IsOpen()
                },

                // D-Task 6: Unscrew and unplug all HDD cables and screws
                new TaskEntry
                {
                    taskObject    = disassemblyTaskObjects[5],
                    originalIndex = 5,
                    condition = () =>
                    {
                        if (_hddController == null) return false;
                        if (!_hddController.CanBeRemoved) return false;
                        return _hddController.GetComponentsInChildren<ScrewController>(true)
                                             .All(s => s.IsUnscrewed());
                    }
                },

                // D-Task 7: Unscrew and unplug motherboard phase-1 cables and screws (GPU excluded — that's D-Task 8)
                new TaskEntry
                {
                    taskObject    = disassemblyTaskObjects[6],
                    originalIndex = 6,
                    condition = () =>
                    {
                        if (motherboardController == null) return false;
                        MotherboardPhaseManager pm = motherboardController.GetComponent<MotherboardPhaseManager>();
                        Transform p1 = pm != null ? pm.GetPhase1Root() : motherboardController.transform;
                        if (p1 == null) p1 = motherboardController.transform;
                        foreach (var s in p1.GetComponentsInChildren<ScrewController>(true))
                        {
                            if (s.GetComponentInParent<GPUController>(true) != null) continue;
                            if (!s.IsUnscrewed()) return false;
                        }
                        foreach (var c in p1.GetComponentsInChildren<CablePort>(true))
                        {
                            if (c.GetComponentInParent<GPUController>(true) != null) continue;
                            if (c.IsInstalled) return false;
                        }
                        return true;
                    }
                },

                // D-Task 8: GPU — unscrew, unplug cable, unlatch (GPU still in slot)
                // Persistent flag: once the GPU is fully prepped while in slot, removing it must not revert the task.
                new TaskEntry
                {
                    taskObject    = disassemblyTaskObjects[7],
                    originalIndex = 7,
                    condition = () =>
                    {
                        if (!_gpuPreparedForRemoval &&
                            gpuController != null &&
                            gpuController.IsInSlot &&
                            !gpuController.IsLatched &&
                            gpuController.GetComponentsInChildren<CablePort>(true).All(c => !c.IsInstalled) &&
                            gpuController.GetComponentsInChildren<ScrewController>(true).All(s => s.IsUnscrewed()))
                        {
                            _gpuPreparedForRemoval = true;
                        }
                        return _gpuPreparedForRemoval;
                    }
                },

                // D-Task 9: Remove GPU, Motherboard, HDD and PSU from the system unit
                new TaskEntry
                {
                    taskObject    = disassemblyTaskObjects[8],
                    originalIndex = 8,
                    condition = () =>
                        (gpuController       == null || !gpuController.IsInSlot) &&
                        (motherboardController == null || motherboardController.IsUninstalledFromSystemUnit) &&
                        (_hddController      == null || !_hddController.IsInSlot) &&
                        (psuHolder           == null || psuHolder.hardwarePrefab == null ||
                         psuHolder.hardwarePrefab.GetComponentInParent<SlotContainer>() == null)
                },

                // D-Task 10: Open motherboard detail view from the workspace (persistent flag)
                new TaskEntry
                {
                    taskObject    = disassemblyTaskObjects[9],
                    originalIndex = 9,
                    condition = () =>
                    {
                        if (!_mbOpenedFromWorkspace &&
                            motherboardController != null &&
                            motherboardController.IsUninstalledFromSystemUnit &&
                            GameManager.Instance?.IsEditorOpen == true &&
                            GameManager.Instance?.firstLayer != null &&
                            motherboardController.transform.parent == GameManager.Instance.firstLayer.transform)
                        {
                            _mbOpenedFromWorkspace = true;
                        }
                        return _mbOpenedFromWorkspace;
                    }
                },

                // D-Task 11: Uninstall heatsink (unscrew and unplug cable, then remove)
                new TaskEntry
                {
                    taskObject    = disassemblyTaskObjects[10],
                    originalIndex = 10,
                    condition = () => _heatsinkController != null && !_heatsinkController.IsInstalledInSlot
                },

                // D-Task 12: Uninstall SSD (unscrew and remove)
                new TaskEntry
                {
                    taskObject    = disassemblyTaskObjects[11],
                    originalIndex = 11,
                    condition = () => ssdHolder != null && ssdHolder.IsAvailable()
                },

                // D-Task 13: Uninstall both RAM sticks (unlatch and remove) — both holders must be wired and available
                new TaskEntry
                {
                    taskObject    = disassemblyTaskObjects[12],
                    originalIndex = 12,
                    condition = () =>
                        ram1Holder != null && ram1Holder.IsAvailable() &&
                        ram2Holder != null && ram2Holder.IsAvailable()
                },

                // D-Task 14: Open the CPU lock lever (slide right)
                new TaskEntry
                {
                    taskObject    = disassemblyTaskObjects[13],
                    originalIndex = 13,
                    condition = () => cpuSlotController != null && !cpuSlotController.IsLockClosed
                },

                // D-Task 15: Wipe thermal paste with cloth then uninstall CPU
                new TaskEntry
                {
                    taskObject    = disassemblyTaskObjects[14],
                    originalIndex = 14,
                    condition = () =>
                        cpuSlotController != null && !cpuSlotController.IsCPUInstalled &&
                        _cpuController != null &&
                        _cpuController.CurrentPasteState == CPUController.PasteState.NoPaste
                },

                // D-Task 16: Uninstall CMOS battery
                new TaskEntry
                {
                    taskObject    = disassemblyTaskObjects[15],
                    originalIndex = 15,
                    condition = () => cmosHolder != null && cmosHolder.IsAvailable()
                }
            }
        };
    }

    // ── Build Assembly Phase (14 tasks) ──────────────────────────────────────

    private void BuildAssemblyPhase()
    {
        if (assemblyTaskObjects == null || assemblyTaskObjects.Length < 14)
        {
            Debug.LogWarning("[NCIITaskListManager] Assign all 14 assembly task objects in the inspector — assembly tab will be empty.");
            return;
        }

        _assembly = new TaskPhase
        {
            taskParent    = assemblyTaskParent,
            finishedParent = assemblyFinishedParent,
            tasks = new List<TaskEntry>
            {
                // A-Task 1: Install CPU and apply thermal paste
                new TaskEntry
                {
                    taskObject    = assemblyTaskObjects[0],
                    originalIndex = 0,
                    condition = () =>
                        cpuSlotController != null && cpuSlotController.IsCPUInstalled &&
                        _cpuController != null &&
                        _cpuController.CurrentPasteState == CPUController.PasteState.PasteApplied
                },

                // A-Task 2: Lock the CPU in place (close the lock lever)
                new TaskEntry
                {
                    taskObject    = assemblyTaskObjects[1],
                    originalIndex = 1,
                    condition = () => cpuSlotController != null && cpuSlotController.IsLockClosed
                },

                // A-Task 3: Install heatsink — screw and plug cable
                new TaskEntry
                {
                    taskObject    = assemblyTaskObjects[2],
                    originalIndex = 2,
                    condition = () => _heatsinkController != null && _heatsinkController.IsFullyInstalled
                },

                // A-Task 4: Install and latch 1 RAM stick
                new TaskEntry
                {
                    taskObject    = assemblyTaskObjects[3],
                    originalIndex = 3,
                    condition = () =>
                    {
                        var ram1 = ram1Holder?.hardwarePrefab?.GetComponent<RAMController>();
                        var ram2 = ram2Holder?.hardwarePrefab?.GetComponent<RAMController>();
                        return (ram1 != null && ram1.IsInstalled) ||
                               (ram2 != null && ram2.IsInstalled);
                    }
                },

                // A-Task 5: Install SSD and screw it in place
                new TaskEntry
                {
                    taskObject    = assemblyTaskObjects[4],
                    originalIndex = 4,
                    condition = () =>
                    {
                        if (_ssdController == null || !_ssdController.IsInSlot) return false;
                        return _ssdController.GetComponentsInChildren<ScrewController>(true)
                                             .All(s => s.IsScrewed());
                    }
                },

                // A-Task 6: Install CMOS battery
                new TaskEntry
                {
                    taskObject    = assemblyTaskObjects[5],
                    originalIndex = 5,
                    condition = () => cmosHolder != null && !cmosHolder.IsAvailable()
                },

                // A-Task 7: Drop motherboard to the hardware area to store it (persistent flag)
                // Gate: A-Tasks 1–6 (indices 0–5) must all be complete first.
                // Guard: only latch when the editor is closed — the scene MB goes inactive during
                // detail-view editing, which would fire this prematurely while tasks 1–6 complete.
                new TaskEntry
                {
                    taskObject    = assemblyTaskObjects[6],
                    originalIndex = 6,
                    condition = () =>
                    {
                        for (int i = 0; i < 6; i++)
                            if (!_assembly.tasks[i].isCompleted) return false;

                        if (!_mbAssemblyReturnedToWorkspace &&
                            motherboardController != null &&
                            motherboardController.IsUninstalledFromSystemUnit &&
                            !motherboardController.gameObject.activeSelf &&
                            GameManager.Instance?.IsEditorOpen != true)
                        {
                            _mbAssemblyReturnedToWorkspace = true;
                        }
                        return _mbAssemblyReturnedToWorkspace;
                    }
                },

                // A-Task 8: Install Motherboard, HDD and PSU into the system unit
                // Latched: detail-view reparenting can temporarily make the live hierarchy checks
                // return false, causing the task to revert. Once all three are confirmed installed
                // the flag sticks and the task never resurfaces.
                new TaskEntry
                {
                    taskObject    = assemblyTaskObjects[7],
                    originalIndex = 7,
                    condition = () =>
                    {
                        if (!_task8Latched &&
                            (motherboardController == null || !motherboardController.IsUninstalledFromSystemUnit) &&
                            (_hddController        == null || _hddController.IsInSlot) &&
                            (psuHolder             == null || psuHolder.hardwarePrefab == null ||
                             psuHolder.hardwarePrefab.GetComponentInParent<SlotContainer>() != null))
                        {
                            _task8Latched = true;
                        }
                        return _task8Latched;
                    }
                },

                // A-Task 9: Install GPU — screw, plug cable, lock the latch
                new TaskEntry
                {
                    taskObject    = assemblyTaskObjects[8],
                    originalIndex = 8,
                    condition = () => gpuController != null && gpuController.IsFullyInstalled
                },

                // A-Task 10: Install all four phase-1 motherboard screws AND all three phase-2 cables.
                // Uses _task10Latched (set eagerly by UpdateAssemblyLatches on every EvaluatePhase
                // call, even while the task is outside the window) so pre-completing the screws/cables
                // before this task enters the 3-task window is still recognised immediately.
                // Still requires A-Task 9 (GPU) to be formally done first so the GPU's own CablePort
                // is not mistaken for a phase-2 MB cable at evaluation time.
                new TaskEntry
                {
                    taskObject    = assemblyTaskObjects[9],
                    originalIndex = 9,
                    condition = () => _assembly != null && _assembly.tasks[8].isCompleted && _task10Latched
                },

                // A-Task 11: Install HDD screws and cables
                new TaskEntry
                {
                    taskObject    = assemblyTaskObjects[10],
                    originalIndex = 10,
                    condition = () => _hddController != null && _hddController.IsFullyInstalled
                },

                // A-Task 12: Close system unit cover and screw it in place.
                // Requires the cover to actually be closed (not just screws in Screwed state from
                // a prior assembly) and all four screws tightened. Latched once met so it never
                // reverts if the player re-opens the cover for any reason afterward.
                new TaskEntry
                {
                    taskObject    = assemblyTaskObjects[11],
                    originalIndex = 11,
                    condition = () =>
                    {
                        if (!_task12Latched &&
                            coverController != null &&
                            !coverController.IsOpen() &&
                            coverController.AreAllScrewsScrewed())
                        {
                            _task12Latched = true;
                        }
                        return _task12Latched;
                    }
                },

                // A-Task 13: Plug all back cables (SU x2, monitor x3, AVR x2)
                new TaskEntry
                {
                    taskObject    = assemblyTaskObjects[12],
                    originalIndex = 12,
                    condition = () =>
                        _allBackPortSlots != null &&
                        _allBackPortSlots.Length > 0 &&
                        _allBackPortSlots.All(p => p.IsInstalled)
                },

                // A-Task 14: Turn on the system unit power button
                new TaskEntry
                {
                    taskObject    = assemblyTaskObjects[13],
                    originalIndex = 13,
                    condition = () => suPowerButton != null && suPowerButton.IsPoweredOn
                }
            }
        };
    }

    // ── Public API ────────────────────────────────────────────────────────────

    // Returns override text (transition / completion) when set, otherwise the next incomplete task.
    public string GetNextIncompleteTaskText()
    {
        if (_displayOverride != null) return _displayOverride;
        TaskPhase active = _showingAssembly ? _assembly : _disassembly;
        if (active?.tasks == null) return null;
        var next = active.tasks.FirstOrDefault(t => !t.isCompleted);
        if (next == null) return null;
        var tmp = next.taskObject.GetComponent<TextMeshProUGUI>();
        return tmp != null ? tmp.text : null;
    }

    // Returns Color.green while the completion banner is active, otherwise the caller's fallback.
    public Color GetDisplayColor(Color fallback) =>
        (_isCompletionOverride && _displayOverride != null) ? Color.green : fallback;

    public static void CheckConditions()
    {
        if (Instance == null) return;
        Instance.EvaluateConditions();
    }

    // ── Internal Logic ────────────────────────────────────────────────────────

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
            if (tmp != null) tmp.color = GoldColor;
        }
    }

    private void EvaluateConditions()
    {
        if (!gameObject.activeInHierarchy) return;

        // Only the visible tab is evaluated. Assembly conditions start all true at scene load
        // (scene starts assembled), so gating on the active tab prevents auto-completion.
        EvaluatePhase(_showingAssembly ? _assembly : _disassembly);
    }

    // Captures assembly latches that must be set regardless of whether the owning task is
    // currently visible in the 3-task window.  Called at the top of every EvaluatePhase so
    // that pre-completing actions (e.g. installing MB cables before task 10 enters the window)
    // are recorded and the task completes the moment it becomes active.
    private void UpdateAssemblyLatches()
    {
        if (_assembly == null) return;

        if (!_task10Latched &&
            motherboardController != null &&
            motherboardController.IsPhase1ScrewsAndPhase2CablesInstalled())
        {
            _task10Latched = true;
        }
    }

    private void EvaluatePhase(TaskPhase phase)
    {
        if (phase == null || phase.tasks == null) return;

        if (phase == _assembly) UpdateAssemblyLatches();

        foreach (var task in phase.tasks)
        {
            if (task.isFlashing) continue;

            if (!task.isCompleted)
            {
                // Only check completion for tasks currently visible in the active 3-task window.
                if (!task.taskObject.activeSelf) continue;

                if (task.condition())
                {
                    task.isCompleted = true;
                    StartCoroutine(FlashAndComplete(phase, task));
                }
            }
            else
            {
                // Check completed tasks for reversion only if the task allows it.
                if (task.canRevert && !task.condition())
                {
                    task.isCompleted = false;
                    task.taskObject.transform.SetParent(phase.taskParent, false);
                    task.taskObject.transform.SetSiblingIndex(task.originalIndex);
                    // Reset color immediately so the task doesn't reappear with its green flash color.
                    var revertTmp = task.taskObject.GetComponent<TextMeshProUGUI>();
                    if (revertTmp != null) revertTmp.color = GoldColor;
                    task.taskObject.SetActive(false);
                    RefreshWindow(phase);
                    if (task.taskObject.activeSelf)
                        StartCoroutine(FlashRevert(task));
                }
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
            StartCoroutine(TransitionToAssembly());
        else if (phase == _assembly && IsAssemblyComplete())
        {
            ShowAllTasksCompleted();
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
        if (tmp != null) tmp.color = GoldColor;
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

    private IEnumerator TransitionToAssembly()
    {
        // Show on the task panel TMP and mirror to SingleTaskDisplay.
        if (transitionText != null)
        {
            transitionText.text = transitionMessage;
            transitionText.gameObject.SetActive(true);
        }
        _displayOverride      = transitionMessage;
        _isCompletionOverride = false;
        OnTasksUpdated?.Invoke();

        yield return new WaitForSeconds(transitionDuration);

        if (transitionText != null)
            transitionText.gameObject.SetActive(false);

        // Clear override so the first assembly task takes over in SingleTaskDisplay.
        _displayOverride      = null;
        _isCompletionOverride = false;

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

    private void ShowAllTasksCompleted()
    {
        // Show on the task panel TMP and mirror to SingleTaskDisplay.
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
}
