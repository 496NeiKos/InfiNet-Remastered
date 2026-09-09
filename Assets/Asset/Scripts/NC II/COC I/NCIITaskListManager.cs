using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class NCIITaskListManager : MonoBehaviour
{
    public static NCIITaskListManager Instance { get; private set; }

    public static event Action OnTasksUpdated;

    private static readonly Color GoldColor = new Color(1f, 0.843f, 0f);

    [Header("Disassembly UI")]
    [SerializeField] private Transform disassemblyTaskParent;
    [SerializeField] private Transform disassemblyFinishedParent;
    [SerializeField] private GameObject[] disassemblyTaskObjects; // 43 entries

    [Header("Assembly UI")]
    [SerializeField] private Transform assemblyTaskParent;
    [SerializeField] private Transform assemblyFinishedParent;
    [SerializeField] private GameObject[] assemblyTaskObjects; // 43 entries

    [Header("Transition UI")]
    [SerializeField] private TextMeshProUGUI transitionText;
    [SerializeField] private float transitionDuration = 3f;
    [SerializeField] private string transitionMessage = "Disassembly task completed! now transitioning to Assembly task";

    [Header("Completion UI")]
    [SerializeField] private TextMeshProUGUI allTasksCompletedText;

    [Header("Hardware Controllers")]
    [SerializeField] private CoverController coverController;
    [SerializeField] private MotherboardController motherboardController;
    [SerializeField] private CPUSlotController cpuSlotController;
    [SerializeField] private GPUController gpuController;
    [SerializeField] private FrontPanelConnectorController frontPanelConnectorController;

    [Header("Front Panel Detail View")]
    [SerializeField] private FrontPanelDetailedView _frontPanelDetailView;

    [Header("Hardware Holders")]
    [SerializeField] private HardwareHolder psuHolder;
    [SerializeField] private HardwareHolder hddHolder;
    [SerializeField] private HardwareHolder cpuHolder;
    [SerializeField] private HardwareHolder heatsinkHolder;
    [SerializeField] private HardwareHolder ram1Holder;
    [SerializeField] private HardwareHolder ram2Holder;
    [SerializeField] private HardwareHolder cmosHolder;
    [SerializeField] private HardwareHolder ssdHolder;

    [Header("PPE Slots")]
    [SerializeField] private PPEItemSlot _ppeWristStrap;
    [SerializeField] private PPEItemSlot _ppeGoggles;
    [SerializeField] private PPEItemSlot _ppeGloves;
    [SerializeField] private PPEItemSlot _ppeSafetyShoes;
    [SerializeField] private PPEItemSlot _ppeDustMask;

    [Header("Power Switches")]
    [SerializeField] private PowerButton suPowerButton;
    [SerializeField] private AVRPowerButton avrPowerButton;
    [SerializeField] private MonitorPowerButton monitorPowerButton;
    [SerializeField] private PSUSwitchController psuSwitch;

    [Header("Back Cable Ports — System Unit")]
    [SerializeField] private CablePort[] _suBackPorts;

    [Header("Back Cable Ports — Monitor")]
    [SerializeField] private CablePort[] _monitorBackPorts;

    [Header("Back Cable Ports — AVR")]
    [SerializeField] private CablePort[] _avrBackPorts;

    [Header("Motherboard Cable Ports")]
    [SerializeField] private CablePort _mbSATADataPort;
    [SerializeField] private CablePort _mb4PinATX12VPort;
    [SerializeField] private CablePort _mb24PinATXPort;

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
    private bool _showingAssembly = false;
    private const int WindowSize = 3;

    // Persistent flags
    private bool _frontPanelDetailViewOpened         = false;
    private bool _mbOpenedFromWorkspace              = false;
    private bool _mbAssemblyReturnedToWorkspace      = false;
    private bool _frontPanelDetailViewOpenedAssembly = false;
    private bool _coverScrewedLatched               = false;

    private string _displayOverride      = null;
    private bool   _isCompletionOverride = false;

    // Cached controller refs
    private CPUController      _cpuController;
    private HDDController      _hddController;
    private HeatsinkController _heatsinkController;
    private SSDController      _ssdController;
    private RAMController      _ram1Controller;
    private RAMController      _ram2Controller;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _cpuController      = cpuHolder?.hardwarePrefab?.GetComponent<CPUController>();
        _hddController      = hddHolder?.hardwarePrefab?.GetComponent<HDDController>();
        _heatsinkController = heatsinkHolder?.hardwarePrefab?.GetComponent<HeatsinkController>();
        _ssdController      = ssdHolder?.hardwarePrefab?.GetComponent<SSDController>();
        _ram1Controller     = ram1Holder?.hardwarePrefab?.GetComponent<RAMController>();
        _ram2Controller     = ram2Holder?.hardwarePrefab?.GetComponent<RAMController>();

        if (disassemblyTaskObjects == null || disassemblyTaskObjects.Length < 43)
        {
            Debug.LogError("[NCIITaskListManager] Assign all 43 disassembly task objects in the Inspector.");
            return;
        }
        if (assemblyTaskObjects == null || assemblyTaskObjects.Length < 43)
        {
            Debug.LogError("[NCIITaskListManager] Assign all 43 assembly task objects in the Inspector.");
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

    // ── Disassembly Phase (43 tasks) ─────────────────────────────────────────

    private void BuildDisassemblyPhase()
    {
        _disassembly = new TaskPhase
        {
            taskParent     = disassemblyTaskParent,
            finishedParent = disassemblyFinishedParent,
            tasks = new List<TaskEntry>
            {
                // D1: Equip PPE — Anti-Static Wrist Strap
                new TaskEntry { taskObject = disassemblyTaskObjects[0], originalIndex = 0,
                    condition = () => _ppeWristStrap != null && _ppeWristStrap.IsActive },

                // D2: Equip PPE — Safety Goggles
                new TaskEntry { taskObject = disassemblyTaskObjects[1], originalIndex = 1,
                    condition = () => _ppeGoggles != null && _ppeGoggles.IsActive },

                // D3: Equip PPE — Anti-Static Gloves
                new TaskEntry { taskObject = disassemblyTaskObjects[2], originalIndex = 2,
                    condition = () => _ppeGloves != null && _ppeGloves.IsActive },

                // D4: Equip PPE — Electrical Hazard Safety Shoes
                new TaskEntry { taskObject = disassemblyTaskObjects[3], originalIndex = 3,
                    condition = () => _ppeSafetyShoes != null && _ppeSafetyShoes.IsActive },

                // D5: Equip PPE — Dust Mask
                new TaskEntry { taskObject = disassemblyTaskObjects[4], originalIndex = 4,
                    condition = () => _ppeDustMask != null && _ppeDustMask.IsActive },

                // D6: Turn off System Unit power switch
                new TaskEntry { taskObject = disassemblyTaskObjects[5], originalIndex = 5,
                    condition = () => suPowerButton == null || !suPowerButton.IsPoweredOn },

                // D7: Turn off Monitor power switch
                new TaskEntry { taskObject = disassemblyTaskObjects[6], originalIndex = 6,
                    condition = () => monitorPowerButton == null || !monitorPowerButton.IsPoweredOn },

                // D8: Turn off AVR power switch
                new TaskEntry { taskObject = disassemblyTaskObjects[7], originalIndex = 7,
                    condition = () => avrPowerButton == null || !avrPowerButton.IsPoweredOn },

                // D9: Turn off PSU switch
                new TaskEntry { taskObject = disassemblyTaskObjects[8], originalIndex = 8,
                    condition = () => psuSwitch == null || !psuSwitch.IsOn },

                // D10: Unplug System Unit back cables
                new TaskEntry { taskObject = disassemblyTaskObjects[9], originalIndex = 9,
                    condition = () => _suBackPorts != null && _suBackPorts.Length > 0 && _suBackPorts.All(p => p.IsUninstalled) },

                // D11: Unplug Monitor back cables
                new TaskEntry { taskObject = disassemblyTaskObjects[10], originalIndex = 10,
                    condition = () => _monitorBackPorts != null && _monitorBackPorts.Length > 0 && _monitorBackPorts.All(p => p.IsUninstalled) },

                // D12: Unplug AVR back cables
                new TaskEntry { taskObject = disassemblyTaskObjects[11], originalIndex = 11,
                    condition = () => _avrBackPorts != null && _avrBackPorts.Length > 0 && _avrBackPorts.All(p => p.IsUninstalled) },

                // D13: Unscrew System Unit back screws
                new TaskEntry { taskObject = disassemblyTaskObjects[12], originalIndex = 12,
                    condition = () => coverController != null && coverController.AreAllScrewsUnscrewed() },

                // D14: Slide panel cover open (to right)
                new TaskEntry { taskObject = disassemblyTaskObjects[13], originalIndex = 13,
                    condition = () => coverController != null && coverController.IsOpen() },

                // D15: Unplug HDD SATA Data Cable
                new TaskEntry { taskObject = disassemblyTaskObjects[14], originalIndex = 14,
                    condition = () => _hddController != null && _hddController.SATADataPort != null && !_hddController.SATADataPort.IsInstalled },

                // D16: Unplug HDD 15-pin SATA Power Cable
                new TaskEntry { taskObject = disassemblyTaskObjects[15], originalIndex = 15,
                    condition = () => _hddController != null && _hddController.SATAPowerPort != null && !_hddController.SATAPowerPort.IsInstalled },

                // D17: Unscrew HDD screws
                new TaskEntry { taskObject = disassemblyTaskObjects[16], originalIndex = 16,
                    condition = () => _hddController != null &&
                        _hddController.GetComponentsInChildren<ScrewController>(true).All(s => s.IsUnscrewed()) },

                // D18: Open Front-Panel Connector Detail View (persistent)
                new TaskEntry { taskObject = disassemblyTaskObjects[17], originalIndex = 17, canRevert = false,
                    condition = () =>
                    {
                        if (!_frontPanelDetailViewOpened &&
                            _frontPanelDetailView != null &&
                            _frontPanelDetailView.gameObject.activeSelf)
                            _frontPanelDetailViewOpened = true;
                        return _frontPanelDetailViewOpened;
                    }
                },

                // D19: Unplug each front panel sub-pin cable
                new TaskEntry { taskObject = disassemblyTaskObjects[18], originalIndex = 18,
                    condition = () => frontPanelConnectorController != null && frontPanelConnectorController.CanDetach() },

                // D20: Unplug FrontPanelConnector_Cable
                new TaskEntry { taskObject = disassemblyTaskObjects[19], originalIndex = 19,
                    condition = () => frontPanelConnectorController != null && !frontPanelConnectorController.IsPhase1Installed },

                // D21: Unplug MB SATA Data Cable
                new TaskEntry { taskObject = disassemblyTaskObjects[20], originalIndex = 20,
                    condition = () => _mbSATADataPort != null && !_mbSATADataPort.IsInstalled },

                // D22: Unplug MB 4-pin ATX12V CPU Power Cable
                new TaskEntry { taskObject = disassemblyTaskObjects[21], originalIndex = 21,
                    condition = () => _mb4PinATX12VPort != null && !_mb4PinATX12VPort.IsInstalled },

                // D23: Unplug MB 24-pin ATX Power Cable
                new TaskEntry { taskObject = disassemblyTaskObjects[22], originalIndex = 22,
                    condition = () => _mb24PinATXPort != null && !_mb24PinATXPort.IsInstalled },

                // D24: Unscrew Motherboard mounting screws (excludes GPU screws)
                new TaskEntry { taskObject = disassemblyTaskObjects[23], originalIndex = 23,
                    condition = () => MBScrewsUnscrewed() },

                // D25: Unplug GPU 8-pin PCIe Power Cable
                new TaskEntry { taskObject = disassemblyTaskObjects[24], originalIndex = 24,
                    condition = () => gpuController != null &&
                        gpuController.GetComponentsInChildren<CablePort>(true).All(c => !c.IsInstalled) },

                // D26: Unscrew GPU screw
                new TaskEntry { taskObject = disassemblyTaskObjects[25], originalIndex = 25,
                    condition = () => gpuController != null &&
                        gpuController.GetComponentsInChildren<ScrewController>(true).All(s => s.IsUnscrewed()) },

                // D27: Unlatch GPU from slot
                new TaskEntry { taskObject = disassemblyTaskObjects[26], originalIndex = 26,
                    condition = () => gpuController != null && !gpuController.IsLatched },

                // D28: Remove GPU from slot
                new TaskEntry { taskObject = disassemblyTaskObjects[27], originalIndex = 27,
                    condition = () => gpuController != null && !gpuController.IsInSlot },

                // D29: Remove Motherboard from System Unit
                new TaskEntry { taskObject = disassemblyTaskObjects[28], originalIndex = 28,
                    condition = () => motherboardController != null && motherboardController.IsUninstalledFromSystemUnit },

                // D30: Remove HDD from slot
                new TaskEntry { taskObject = disassemblyTaskObjects[29], originalIndex = 29,
                    condition = () => _hddController != null && !_hddController.IsInSlot },

                // D31: Remove PSU from slot
                new TaskEntry { taskObject = disassemblyTaskObjects[30], originalIndex = 30,
                    condition = () => psuHolder != null && psuHolder.hardwarePrefab != null &&
                        psuHolder.hardwarePrefab.GetComponentInParent<SlotContainer>() == null },

                // D32: Place Motherboard to Workspace and open its detail view (persistent)
                new TaskEntry { taskObject = disassemblyTaskObjects[31], originalIndex = 31, canRevert = false,
                    condition = () =>
                    {
                        if (!_mbOpenedFromWorkspace &&
                            motherboardController != null &&
                            motherboardController.IsUninstalledFromSystemUnit &&
                            GameManager.Instance?.IsEditorOpen == true &&
                            GameManager.Instance?.firstLayer != null &&
                            motherboardController.transform.parent == GameManager.Instance.firstLayer.transform)
                            _mbOpenedFromWorkspace = true;
                        return _mbOpenedFromWorkspace;
                    }
                },

                // D33: Unlatch both RAM ends from DIMM slot (2x)
                new TaskEntry { taskObject = disassemblyTaskObjects[32], originalIndex = 32,
                    condition = () => _ram1Controller != null && !_ram1Controller.IsInstalled &&
                        _ram2Controller != null && !_ram2Controller.IsInstalled },

                // D34: Remove both RAM sticks from slot
                new TaskEntry { taskObject = disassemblyTaskObjects[33], originalIndex = 33,
                    condition = () => ram1Holder != null && ram1Holder.IsAvailable() &&
                        ram2Holder != null && ram2Holder.IsAvailable() },

                // D35: Unscrew SSD screw
                new TaskEntry { taskObject = disassemblyTaskObjects[34], originalIndex = 34,
                    condition = () => _ssdController != null &&
                        _ssdController.GetComponentsInChildren<ScrewController>(true).All(s => s.IsUnscrewed()) },

                // D36: Remove SSD from slot
                new TaskEntry { taskObject = disassemblyTaskObjects[35], originalIndex = 35,
                    condition = () => ssdHolder != null && ssdHolder.IsAvailable() },

                // D37: Unplug Heatsink fan cable
                new TaskEntry { taskObject = disassemblyTaskObjects[36], originalIndex = 36,
                    condition = () => _heatsinkController != null && _heatsinkController.CanBeRemoved },

                // D38: Unscrew Heatsink screw
                new TaskEntry { taskObject = disassemblyTaskObjects[37], originalIndex = 37,
                    condition = () => _heatsinkController != null &&
                        _heatsinkController.GetComponentsInChildren<ScrewController>(true).All(s => s.IsUnscrewed()) },

                // D39: Remove Heatsink from slot
                new TaskEntry { taskObject = disassemblyTaskObjects[38], originalIndex = 38,
                    condition = () => _heatsinkController != null && !_heatsinkController.IsInstalledInSlot },

                // D40: Open CPU lock lever (slide to right)
                new TaskEntry { taskObject = disassemblyTaskObjects[39], originalIndex = 39,
                    condition = () => cpuSlotController != null && !cpuSlotController.IsLockClosed },

                // D41: Wipe thermal paste residue with cloth
                new TaskEntry { taskObject = disassemblyTaskObjects[40], originalIndex = 40,
                    condition = () => _cpuController != null &&
                        _cpuController.CurrentPasteState == CPUController.PasteState.NoPaste },

                // D42: Remove CPU from slot
                new TaskEntry { taskObject = disassemblyTaskObjects[41], originalIndex = 41,
                    condition = () => cpuSlotController != null && !cpuSlotController.IsCPUInstalled },

                // D43: Remove CMOS battery from slot
                new TaskEntry { taskObject = disassemblyTaskObjects[42], originalIndex = 42,
                    condition = () => cmosHolder != null && cmosHolder.IsAvailable() },
            }
        };
    }

    // ── Assembly Phase (43 tasks) ────────────────────────────────────────────

    private void BuildAssemblyPhase()
    {
        _assembly = new TaskPhase
        {
            taskParent     = assemblyTaskParent,
            finishedParent = assemblyFinishedParent,
            tasks = new List<TaskEntry>
            {
                // A1: Install CMOS battery
                new TaskEntry { taskObject = assemblyTaskObjects[0], originalIndex = 0,
                    condition = () => cmosHolder != null && !cmosHolder.IsAvailable() },

                // A2: Install CPU into slot
                new TaskEntry { taskObject = assemblyTaskObjects[1], originalIndex = 1,
                    condition = () => cpuSlotController != null && cpuSlotController.IsCPUInstalled },

                // A3: Apply thermal paste to CPU
                new TaskEntry { taskObject = assemblyTaskObjects[2], originalIndex = 2,
                    condition = () => _cpuController != null &&
                        _cpuController.CurrentPasteState == CPUController.PasteState.PasteApplied },

                // A4: Close CPU lock lever
                new TaskEntry { taskObject = assemblyTaskObjects[3], originalIndex = 3,
                    condition = () => cpuSlotController != null && cpuSlotController.IsLockClosed },

                // A5: Install Heatsink into slot
                new TaskEntry { taskObject = assemblyTaskObjects[4], originalIndex = 4,
                    condition = () => _heatsinkController != null && _heatsinkController.IsInstalledInSlot },

                // A6: Screw Heatsink screw
                new TaskEntry { taskObject = assemblyTaskObjects[5], originalIndex = 5,
                    condition = () => _heatsinkController != null &&
                        _heatsinkController.GetComponentsInChildren<ScrewController>(true).All(s => s.IsScrewed()) },

                // A7: Plug Heatsink fan cable
                new TaskEntry { taskObject = assemblyTaskObjects[6], originalIndex = 6,
                    condition = () => _heatsinkController != null && !_heatsinkController.CanBeRemoved },

                // A8: Install SSD into slot
                new TaskEntry { taskObject = assemblyTaskObjects[7], originalIndex = 7,
                    condition = () => _ssdController != null && _ssdController.IsInSlot },

                // A9: Screw SSD screw
                new TaskEntry { taskObject = assemblyTaskObjects[8], originalIndex = 8,
                    condition = () => _ssdController != null &&
                        _ssdController.GetComponentsInChildren<ScrewController>(true).All(s => s.IsScrewed()) },

                // A10: Install both RAM sticks into slots
                new TaskEntry { taskObject = assemblyTaskObjects[9], originalIndex = 9,
                    condition = () => ram1Holder != null && !ram1Holder.IsAvailable() &&
                        ram2Holder != null && !ram2Holder.IsAvailable() },

                // A11: Latch both ends of both RAM sticks (2x)
                new TaskEntry { taskObject = assemblyTaskObjects[10], originalIndex = 10,
                    condition = () => _ram1Controller != null && _ram1Controller.IsLeftLatched && _ram1Controller.IsRightLatched &&
                        _ram2Controller != null && _ram2Controller.IsLeftLatched && _ram2Controller.IsRightLatched },

                // A12: Return Motherboard to hardware storage (persistent)
                new TaskEntry { taskObject = assemblyTaskObjects[11], originalIndex = 11, canRevert = false,
                    condition = () =>
                    {
                        if (!_mbAssemblyReturnedToWorkspace &&
                            motherboardController != null &&
                            motherboardController.IsUninstalledFromSystemUnit &&
                            GameManager.Instance?.IsEditorOpen != true)
                            _mbAssemblyReturnedToWorkspace = true;
                        return _mbAssemblyReturnedToWorkspace;
                    }
                },

                // A13: Install PSU into slot
                new TaskEntry { taskObject = assemblyTaskObjects[12], originalIndex = 12,
                    condition = () => psuHolder != null && psuHolder.hardwarePrefab != null &&
                        psuHolder.hardwarePrefab.GetComponentInParent<SlotContainer>() != null },

                // A14: Install HDD into slot
                new TaskEntry { taskObject = assemblyTaskObjects[13], originalIndex = 13,
                    condition = () => _hddController != null && _hddController.IsInSlot },

                // A15: Install Motherboard into System Unit
                new TaskEntry { taskObject = assemblyTaskObjects[14], originalIndex = 14,
                    condition = () => motherboardController != null && !motherboardController.IsUninstalledFromSystemUnit },

                // A16: Install GPU into slot
                new TaskEntry { taskObject = assemblyTaskObjects[15], originalIndex = 15,
                    condition = () => gpuController != null && gpuController.IsInSlot },

                // A17: Latch GPU into slot
                new TaskEntry { taskObject = assemblyTaskObjects[16], originalIndex = 16,
                    condition = () => gpuController != null && gpuController.IsLatched },

                // A18: Screw GPU screw
                new TaskEntry { taskObject = assemblyTaskObjects[17], originalIndex = 17,
                    condition = () => gpuController != null &&
                        gpuController.GetComponentsInChildren<ScrewController>(true).All(s => s.IsScrewed()) },

                // A19: Plug GPU 8-pin PCIe Power Cable
                new TaskEntry { taskObject = assemblyTaskObjects[18], originalIndex = 18,
                    condition = () => gpuController != null &&
                        gpuController.GetComponentsInChildren<CablePort>(true).All(c => c.IsInstalled) },

                // A20: Screw Motherboard mounting screws (excludes GPU screws)
                new TaskEntry { taskObject = assemblyTaskObjects[19], originalIndex = 19,
                    condition = () => MBScrewsScrewed() },

                // A21: Plug MB 24-pin ATX Power Cable
                new TaskEntry { taskObject = assemblyTaskObjects[20], originalIndex = 20,
                    condition = () => _mb24PinATXPort != null && _mb24PinATXPort.IsInstalled },

                // A22: Plug MB 4-pin ATX12V CPU Power Cable
                new TaskEntry { taskObject = assemblyTaskObjects[21], originalIndex = 21,
                    condition = () => _mb4PinATX12VPort != null && _mb4PinATX12VPort.IsInstalled },

                // A23: Plug MB SATA Data Cable
                new TaskEntry { taskObject = assemblyTaskObjects[22], originalIndex = 22,
                    condition = () => _mbSATADataPort != null && _mbSATADataPort.IsInstalled },

                // A24: Plug FrontPanelConnector_Cable
                new TaskEntry { taskObject = assemblyTaskObjects[23], originalIndex = 23,
                    condition = () => frontPanelConnectorController != null && frontPanelConnectorController.IsPhase1Installed },

                // A25: Open Front-Panel Detail View for sub-pin wiring (persistent)
                new TaskEntry { taskObject = assemblyTaskObjects[24], originalIndex = 24, canRevert = false,
                    condition = () =>
                    {
                        if (!_frontPanelDetailViewOpenedAssembly &&
                            _frontPanelDetailView != null &&
                            _frontPanelDetailView.gameObject.activeSelf)
                            _frontPanelDetailViewOpenedAssembly = true;
                        return _frontPanelDetailViewOpenedAssembly;
                    }
                },

                // A26: Plug all front panel sub-pin cables
                new TaskEntry { taskObject = assemblyTaskObjects[25], originalIndex = 25,
                    condition = () => frontPanelConnectorController != null && frontPanelConnectorController.IsPhase2FullyInstalled },

                // A27: Screw HDD screws
                new TaskEntry { taskObject = assemblyTaskObjects[26], originalIndex = 26,
                    condition = () => _hddController != null && _hddController.IsInSlot &&
                        _hddController.GetComponentsInChildren<ScrewController>(true).All(s => s.IsScrewed()) },

                // A28: Plug HDD 15-pin SATA Power Cable
                new TaskEntry { taskObject = assemblyTaskObjects[27], originalIndex = 27,
                    condition = () => _hddController != null && _hddController.SATAPowerPort != null && _hddController.SATAPowerPort.IsInstalled },

                // A29: Plug HDD SATA Data Cable
                new TaskEntry { taskObject = assemblyTaskObjects[28], originalIndex = 28,
                    condition = () => _hddController != null && _hddController.SATADataPort != null && _hddController.SATADataPort.IsInstalled },

                // A30: Slide cover closed (to left)
                new TaskEntry { taskObject = assemblyTaskObjects[29], originalIndex = 29,
                    condition = () => coverController != null && !coverController.IsOpen() },

                // A31: Screw System Unit back screws (persistent — never reverts if cover re-opened)
                new TaskEntry { taskObject = assemblyTaskObjects[30], originalIndex = 30, canRevert = false,
                    condition = () =>
                    {
                        if (!_coverScrewedLatched &&
                            coverController != null &&
                            !coverController.IsOpen() &&
                            coverController.AreAllScrewsScrewed())
                            _coverScrewedLatched = true;
                        return _coverScrewedLatched;
                    }
                },

                // A32: Plug AVR back cables
                new TaskEntry { taskObject = assemblyTaskObjects[31], originalIndex = 31,
                    condition = () => _avrBackPorts != null && _avrBackPorts.Length > 0 && _avrBackPorts.All(p => p.IsInstalled) },

                // A33: Plug Monitor back cables
                new TaskEntry { taskObject = assemblyTaskObjects[32], originalIndex = 32,
                    condition = () => _monitorBackPorts != null && _monitorBackPorts.Length > 0 && _monitorBackPorts.All(p => p.IsInstalled) },

                // A34: Plug System Unit back cables
                new TaskEntry { taskObject = assemblyTaskObjects[33], originalIndex = 33,
                    condition = () => _suBackPorts != null && _suBackPorts.Length > 0 && _suBackPorts.All(p => p.IsInstalled) },

                // A35: Turn on PSU switch
                new TaskEntry { taskObject = assemblyTaskObjects[34], originalIndex = 34,
                    condition = () => psuSwitch != null && psuSwitch.IsOn },

                // A36: Turn on AVR power switch
                new TaskEntry { taskObject = assemblyTaskObjects[35], originalIndex = 35,
                    condition = () => avrPowerButton != null && avrPowerButton.IsPoweredOn },

                // A37: Turn on Monitor power switch
                new TaskEntry { taskObject = assemblyTaskObjects[36], originalIndex = 36,
                    condition = () => monitorPowerButton != null && monitorPowerButton.IsPoweredOn },

                // A38: Turn on System Unit power switch
                new TaskEntry { taskObject = assemblyTaskObjects[37], originalIndex = 37,
                    condition = () => suPowerButton != null && suPowerButton.IsPoweredOn },

                // A39: Unequip Dust Mask
                new TaskEntry { taskObject = assemblyTaskObjects[38], originalIndex = 38,
                    condition = () => _ppeDustMask == null || !_ppeDustMask.IsActive },

                // A40: Unequip Electrical Hazard Safety Shoes
                new TaskEntry { taskObject = assemblyTaskObjects[39], originalIndex = 39,
                    condition = () => _ppeSafetyShoes == null || !_ppeSafetyShoes.IsActive },

                // A41: Unequip Anti-Static Gloves
                new TaskEntry { taskObject = assemblyTaskObjects[40], originalIndex = 40,
                    condition = () => _ppeGloves == null || !_ppeGloves.IsActive },

                // A42: Unequip Safety Goggles
                new TaskEntry { taskObject = assemblyTaskObjects[41], originalIndex = 41,
                    condition = () => _ppeGoggles == null || !_ppeGoggles.IsActive },

                // A43: Unequip Anti-Static Wrist Strap
                new TaskEntry { taskObject = assemblyTaskObjects[42], originalIndex = 42,
                    condition = () => _ppeWristStrap == null || !_ppeWristStrap.IsActive },
            }
        };
    }

    // ── Shared Helpers ────────────────────────────────────────────────────────

    // Returns the phase-1 root of the motherboard (falls back to motherboard transform).
    private Transform GetMBPhase1Root()
    {
        if (motherboardController == null) return null;
        MotherboardPhaseManager pm = motherboardController.GetComponent<MotherboardPhaseManager>();
        Transform root = pm != null ? pm.GetPhase1Root() : null;
        return root != null ? root : motherboardController.transform;
    }

    // True when all MB mounting screws (non-GPU) are unscrewed.
    private bool MBScrewsUnscrewed()
    {
        Transform root = GetMBPhase1Root();
        if (root == null) return false;
        foreach (var s in root.GetComponentsInChildren<ScrewController>(true))
        {
            if (s.GetComponentInParent<GPUController>(true) != null) continue;
            if (!s.IsUnscrewed()) return false;
        }
        return true;
    }

    // True when all MB mounting screws (non-GPU) are fully tightened and MB is in the SU.
    private bool MBScrewsScrewed()
    {
        if (motherboardController == null || motherboardController.IsUninstalledFromSystemUnit) return false;
        Transform root = GetMBPhase1Root();
        if (root == null) return false;
        foreach (var s in root.GetComponentsInChildren<ScrewController>(true))
        {
            if (s.GetComponentInParent<GPUController>(true) != null) continue;
            if (!s.IsScrewed()) return false;
        }
        return true;
    }

    // ── Public API ────────────────────────────────────────────────────────────

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

    public Color GetDisplayColor(Color fallback) =>
        (_isCompletionOverride && _displayOverride != null) ? Color.green : fallback;

    public int GetCompletedTaskCount()
    {
        int count = 0;
        if (_disassembly?.tasks != null) count += _disassembly.tasks.Count(t => t.isCompleted);
        if (_assembly?.tasks != null)    count += _assembly.tasks.Count(t => t.isCompleted);
        return count;
    }

    public static void CheckConditions()
    {
        if (Instance == null) return;
        Instance.EvaluateConditions();
    }

    // ── Internal Logic ────────────────────────────────────────────────────────

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
        EvaluatePhase(_showingAssembly ? _assembly : _disassembly);
    }

    private void EvaluatePhase(TaskPhase phase)
    {
        if (phase == null || phase.tasks == null) return;

        foreach (var task in phase.tasks)
        {
            if (task.isFlashing) continue;

            if (!task.isCompleted)
            {
                if (!task.taskObject.activeSelf) continue;

                if (task.condition())
                {
                    task.isCompleted = true;
                    StartCoroutine(FlashAndComplete(phase, task));
                }
            }
            else
            {
                if (task.canRevert && !task.condition())
                {
                    task.isCompleted = false;
                    task.taskObject.transform.SetParent(phase.taskParent, false);
                    task.taskObject.transform.SetSiblingIndex(task.originalIndex);
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

    public bool IsTransitioningToAssembly { get; private set; }

    private IEnumerator TransitionToAssembly()
    {
        IsTransitioningToAssembly = true;

        if (transitionText != null)
        {
            transitionText.text = transitionMessage;
            transitionText.gameObject.SetActive(true);
        }
        _displayOverride      = transitionMessage;
        _isCompletionOverride = false;
        OnTasksUpdated?.Invoke();

        yield return new WaitForSeconds(transitionDuration);

        IsTransitioningToAssembly = false;

        if (transitionText != null)
            transitionText.gameObject.SetActive(false);

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

    private const string TopicCompleteMsg    = "All task and objective has been completed on this topic. Go to settings and navigate other topic for the COC I.";
    private const string AllTopicsCompleteMsg = "All topic has been completed, objective for COC I has been met. Proceed to COC II.";

    private void OnDestroy()
    {
        TopicManager.OnAllTopicsComplete -= OnAllTopicsComplete;
    }

    private void ShowAllTasksCompleted()
    {
        TopicManager.OnAllTopicsComplete += OnAllTopicsComplete;

        if (allTasksCompletedText != null)
        {
            allTasksCompletedText.text  = TopicCompleteMsg;
            allTasksCompletedText.color = Color.green;
            allTasksCompletedText.gameObject.SetActive(true);
        }
        _displayOverride      = TopicCompleteMsg;
        _isCompletionOverride = true;
        OnTasksUpdated?.Invoke();
    }

    private void OnAllTopicsComplete()
    {
        TopicManager.OnAllTopicsComplete -= OnAllTopicsComplete;

        if (allTasksCompletedText != null)
            allTasksCompletedText.text = AllTopicsCompleteMsg;
        _displayOverride = AllTopicsCompleteMsg;
        OnTasksUpdated?.Invoke();
    }
}
