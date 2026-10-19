/*
 * ================================================================
 *  UNITY SETUP GUIDE — NCIITaskListManager (COC I — Topic 1)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to an always-active "Managers" GameObject in the COC I scene.
 *
 *  ── HIERARCHY ────────────────────────────────────────────────────
 *
 *  Disassembly Panel
 *    ├── Group Title Row
 *    │     ├── GroupTitleTMP   TMP_Text → disassemblyGroupTitleTMP
 *    │     └── Button          Button   → disassemblyGroupTitleBtn
 *    ├── Task List              → disassemblyTaskParent (VerticalLayoutGroup)
 *    └── Finished Bucket        → disassemblyFinishedParent (off-screen)
 *
 *  Assembly Panel               (starts INACTIVE)
 *    ├── Group Title Row
 *    │     ├── GroupTitleTMP   TMP_Text → assemblyGroupTitleTMP
 *    │     └── Button          Button   → assemblyGroupTitleBtn
 *    ├── Task List              → assemblyTaskParent (VerticalLayoutGroup)
 *    └── Finished Bucket        → assemblyFinishedParent (off-screen)
 *
 *  Disassembly Group Overview Popup   → disassemblyGroupOverviewPopup (INACTIVE)
 *    ├── Row Parent              → disassemblyGroupRowParent
 *    └── CloseBtn               Button → disassemblyClosePopupBtn
 *
 *  Assembly Group Overview Popup      → assemblyGroupOverviewPopup (INACTIVE)
 *    ├── Row Parent              → assemblyGroupRowParent
 *    └── CloseBtn               Button → assemblyClosePopupBtn
 *
 *  ── GROUP ROW PREFAB ─────────────────────────────────────────────
 *    Root: HorizontalLayoutGroup
 *      ├── NameTMP      TMP_Text [0]
 *      ├── ProgressTMP  TMP_Text [1]
 *      └── StatusTMP    TMP_Text [2]
 *
 *  ── TASK GROUPS — 43 disassembly + 43 assembly tasks ─────────────
 *    Configure both disassemblyTaskGroups[] and assemblyTaskGroups[]
 *    in the Inspector. FirstTask index is relative to 0 within each
 *    phase's 43-task array (not a combined index).
 *
 *  ── PATTERN ──────────────────────────────────────────────────────
 *    Latch-based: once a task condition is true it latches permanently.
 *    Sliding window: up to 3 unlatched tasks shown, capped to the
 *    current group. Disassembly completes → transition → Assembly.
 * ================================================================
 */

using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NCIITaskListManager : MonoBehaviour
{
    public static NCIITaskListManager Instance { get; private set; }

    public static event System.Action OnTasksUpdated;

    private const int DISASSEMBLY_COUNT = 43;
    private const int ASSEMBLY_COUNT    = 43;

    // ── Disassembly Task UI ───────────────────────────────────────────────────

    [Header("Disassembly UI")]
    [SerializeField] private Transform    disassemblyTaskParent;
    [SerializeField] private Transform    disassemblyFinishedParent;
    [SerializeField] private GameObject[] disassemblyTaskObjects = new GameObject[DISASSEMBLY_COUNT];

    // ── Assembly Task UI ──────────────────────────────────────────────────────

    [Header("Assembly UI")]
    [SerializeField] private Transform    assemblyTaskParent;
    [SerializeField] private Transform    assemblyFinishedParent;
    [SerializeField] private GameObject[] assemblyTaskObjects = new GameObject[ASSEMBLY_COUNT];

    // ── Transition & Completion UI ────────────────────────────────────────────

    [Header("Transition UI")]
    [SerializeField] private TextMeshProUGUI transitionText;
    [SerializeField] private float           transitionDuration = 3f;
    [SerializeField] private string          transitionMessage  = "Disassembly task completed! now transitioning to Assembly task";

    [Header("Completion UI")]
    [SerializeField] private TextMeshProUGUI allTasksCompletedText;

    // ── Disassembly Group Title ───────────────────────────────────────────────

    [Header("Disassembly Group Title")]
    [SerializeField] private TMP_Text disassemblyGroupTitleTMP;
    [SerializeField] private Button   disassemblyGroupTitleBtn;

    // ── Assembly Group Title ──────────────────────────────────────────────────

    [Header("Assembly Group Title")]
    [SerializeField] private TMP_Text assemblyGroupTitleTMP;
    [SerializeField] private Button   assemblyGroupTitleBtn;

    // ── Group Flash (shared) ──────────────────────────────────────────────────

    [Header("Group Flash")]
    [SerializeField] private Color flashColor    = Color.green;
    [SerializeField] private float flashDuration = 1f;

    // ── Disassembly Group Overview Popup ──────────────────────────────────────

    [Header("Disassembly Group Overview Popup")]
    [SerializeField] private GameObject disassemblyGroupOverviewPopup;
    [SerializeField] private Transform  disassemblyGroupRowParent;
    [SerializeField] private Button     disassemblyClosePopupBtn;

    // ── Assembly Group Overview Popup ─────────────────────────────────────────

    [Header("Assembly Group Overview Popup")]
    [SerializeField] private GameObject assemblyGroupOverviewPopup;
    [SerializeField] private Transform  assemblyGroupRowParent;
    [SerializeField] private Button     assemblyClosePopupBtn;

    // ── Shared Popup Assets ───────────────────────────────────────────────────

    [Header("Group Popup Shared Assets")]
    [SerializeField] private GameObject groupRowPrefab;
    [SerializeField] private Color      groupPendingColor = Color.gray;
    [SerializeField] private Color      groupActiveColor  = Color.yellow;
    [SerializeField] private Color      groupDoneColor    = Color.green;

    // ── Task Groups ───────────────────────────────────────────────────────────

    [Header("Disassembly Task Groups (configure in Inspector)")]
    [SerializeField] private TaskGroup[] disassemblyTaskGroups;

    [Header("Assembly Task Groups (configure in Inspector)")]
    [SerializeField] private TaskGroup[] assemblyTaskGroups;

    // ── Hardware Controllers ──────────────────────────────────────────────────

    [Header("Hardware Controllers")]
    [SerializeField] private CoverController               coverController;
    [SerializeField] private MotherboardController         motherboardController;
    [SerializeField] private CPUSlotController             cpuSlotController;
    [SerializeField] private GPUController                 gpuController;
    [SerializeField] private FrontPanelConnectorController frontPanelConnectorController;
    [SerializeField] private FrontPanelDetailedView        _frontPanelDetailView;

    // ── Hardware Holders ──────────────────────────────────────────────────────

    [Header("Hardware Holders")]
    [SerializeField] private HardwareHolder psuHolder;
    [SerializeField] private HardwareHolder hddHolder;
    [SerializeField] private HardwareHolder cpuHolder;
    [SerializeField] private HardwareHolder heatsinkHolder;
    [SerializeField] private HardwareHolder ram1Holder;
    [SerializeField] private HardwareHolder ram2Holder;
    [SerializeField] private HardwareHolder cmosHolder;
    [SerializeField] private HardwareHolder ssdHolder;

    // ── PPE Slots ─────────────────────────────────────────────────────────────

    [Header("PPE Slots")]
    [SerializeField] private PPEItemSlot _ppeWristStrap;
    [SerializeField] private PPEItemSlot _ppeGoggles;
    [SerializeField] private PPEItemSlot _ppeGloves;
    [SerializeField] private PPEItemSlot _ppeSafetyShoes;
    [SerializeField] private PPEItemSlot _ppeDustMask;

    // ── Power Switches ────────────────────────────────────────────────────────

    [Header("Power Switches")]
    [SerializeField] private PowerButton       suPowerButton;
    [SerializeField] private AVRPowerButton    avrPowerButton;
    [SerializeField] private MonitorPowerButton monitorPowerButton;
    [SerializeField] private PSUSwitchController psuSwitch;

    // ── Cable Ports ───────────────────────────────────────────────────────────

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

    // ── Runtime ───────────────────────────────────────────────────────────────

    // Disassembly state
    private bool[] _disassemblyLatched      = new bool[DISASSEMBLY_COUNT];
    private int    _currentDisassemblyTask  = 0;
    private int    _currentDisassemblyGroup = 0;
    private bool   _disassemblyAllDone      = false;
    private bool   _isDisassemblyFlashing   = false;
    private TMP_Text[][] _disassemblyPopupRowTMPs;

    // Assembly state
    private bool[] _assemblyLatched      = new bool[ASSEMBLY_COUNT];
    private int    _currentAssemblyTask  = 0;
    private int    _currentAssemblyGroup = 0;
    private bool   _assemblyAllDone      = false;
    private bool   _isAssemblyFlashing   = false;
    private TMP_Text[][] _assemblyPopupRowTMPs;

    // Phase control
    private bool   _showingAssembly           = false;
    private bool   _isTransitioningToAssembly = false;
    private string _displayOverride           = null;
    private bool   _isCompletionOverride      = false;

    // Cached controller refs
    private CPUController      _cpuController;
    private HDDController      _hddController;
    private HeatsinkController _heatsinkController;
    private SSDController      _ssdController;
    private RAMController      _ram1Controller;
    private RAMController      _ram2Controller;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        disassemblyGroupTitleBtn?.onClick.AddListener(OpenDisassemblyPopup);
        disassemblyClosePopupBtn?.onClick.AddListener(CloseDisassemblyPopup);
        assemblyGroupTitleBtn?.onClick.AddListener(OpenAssemblyPopup);
        assemblyClosePopupBtn?.onClick.AddListener(CloseAssemblyPopup);
    }

    private void Start()
    {
        _cpuController      = cpuHolder?.hardwarePrefab?.GetComponent<CPUController>();
        _hddController      = hddHolder?.hardwarePrefab?.GetComponent<HDDController>();
        _heatsinkController = heatsinkHolder?.hardwarePrefab?.GetComponent<HeatsinkController>();
        _ssdController      = ssdHolder?.hardwarePrefab?.GetComponent<SSDController>();
        _ram1Controller     = ram1Holder?.hardwarePrefab?.GetComponent<RAMController>();
        _ram2Controller     = ram2Holder?.hardwarePrefab?.GetComponent<RAMController>();

        if (disassemblyTaskObjects == null || disassemblyTaskObjects.Length < DISASSEMBLY_COUNT)
        { Debug.LogError("[NCIITaskListManager] Assign all 43 disassembly task objects."); return; }
        if (assemblyTaskObjects == null || assemblyTaskObjects.Length < ASSEMBLY_COUNT)
        { Debug.LogError("[NCIITaskListManager] Assign all 43 assembly task objects."); return; }

        for (int i = 0; i < DISASSEMBLY_COUNT; i++)
            if (disassemblyTaskObjects[i] != null) disassemblyTaskObjects[i].SetActive(false);
        for (int i = 0; i < ASSEMBLY_COUNT; i++)
            if (assemblyTaskObjects[i] != null) assemblyTaskObjects[i].SetActive(false);

        disassemblyGroupOverviewPopup?.SetActive(false);
        assemblyGroupOverviewPopup?.SetActive(false);
        if (assemblyTaskParent != null) assemblyTaskParent.gameObject.SetActive(false);
        if (transitionText != null)     transitionText.gameObject.SetActive(false);
        if (allTasksCompletedText != null) allTasksCompletedText.gameObject.SetActive(false);

        BuildDisassemblyPopupRows();
        BuildAssemblyPopupRows();
        RefreshDisassemblyGroupTitle();
        RefreshAssemblyGroupTitle();
        RefreshDisassemblyPopup();
        RefreshAssemblyPopup();
        RefreshDisassemblyWindow();
    }

    private void Update()
    {
        if (_isTransitioningToAssembly) return;

        if (_showingAssembly)
        {
            if (!_assemblyAllDone) CheckCurrentAssemblyTask();
        }
        else
        {
            if (!_disassemblyAllDone) CheckCurrentDisassemblyTask();
        }
    }

    // ── Disassembly condition checks ──────────────────────────────────────────

    private bool CheckDisassemblyCondition(int index)
    {
        return index switch
        {
             0 => _ppeWristStrap  != null && _ppeWristStrap.IsActive,
             1 => _ppeGoggles     != null && _ppeGoggles.IsActive,
             2 => _ppeGloves      != null && _ppeGloves.IsActive,
             3 => _ppeSafetyShoes != null && _ppeSafetyShoes.IsActive,
             4 => _ppeDustMask    != null && _ppeDustMask.IsActive,
             5 => suPowerButton      == null || !suPowerButton.IsPoweredOn,
             6 => monitorPowerButton == null || !monitorPowerButton.IsPoweredOn,
             7 => avrPowerButton     == null || !avrPowerButton.IsPoweredOn,
             8 => psuSwitch          == null || !psuSwitch.IsOn,
             9 => _suBackPorts      != null && _suBackPorts.Length      > 0 && _suBackPorts.All(p => p.IsUninstalled),
            10 => _monitorBackPorts != null && _monitorBackPorts.Length > 0 && _monitorBackPorts.All(p => p.IsUninstalled),
            11 => _avrBackPorts    != null && _avrBackPorts.Length    > 0 && _avrBackPorts.All(p => p.IsUninstalled),
            12 => coverController != null && coverController.AreAllScrewsUnscrewed(),
            13 => coverController != null && coverController.IsOpen(),
            14 => _hddController != null && _hddController.SATADataPort  != null && !_hddController.SATADataPort.IsInstalled,
            15 => _hddController != null && _hddController.SATAPowerPort != null && !_hddController.SATAPowerPort.IsInstalled,
            16 => _hddController != null && _hddController.GetComponentsInChildren<ScrewController>(true).All(s => s.IsUnscrewed()),
            17 => _frontPanelDetailView != null && _frontPanelDetailView.gameObject.activeSelf,
            18 => frontPanelConnectorController != null && frontPanelConnectorController.CanDetach(),
            19 => frontPanelConnectorController != null && !frontPanelConnectorController.IsPhase1Installed,
            20 => _mbSATADataPort   != null && !_mbSATADataPort.IsInstalled,
            21 => _mb4PinATX12VPort != null && !_mb4PinATX12VPort.IsInstalled,
            22 => _mb24PinATXPort   != null && !_mb24PinATXPort.IsInstalled,
            23 => MBScrewsUnscrewed(),
            24 => gpuController != null && gpuController.GetComponentsInChildren<CablePort>(true).All(c => !c.IsInstalled),
            25 => gpuController != null && gpuController.GetComponentsInChildren<ScrewController>(true).All(s => s.IsUnscrewed()),
            26 => gpuController != null && !gpuController.IsLatched,
            27 => gpuController != null && !gpuController.IsInSlot,
            28 => motherboardController != null && motherboardController.IsUninstalledFromSystemUnit,
            29 => _hddController != null && !_hddController.IsInSlot,
            30 => psuHolder != null && psuHolder.hardwarePrefab != null && psuHolder.hardwarePrefab.GetComponentInParent<SlotContainer>() == null,
            31 => motherboardController != null
               && motherboardController.IsUninstalledFromSystemUnit
               && GameManager.Instance?.IsEditorOpen == true
               && motherboardController.transform.parent == GameManager.Instance.firstLayer.transform,
            32 => _ram1Controller != null && !_ram1Controller.IsInstalled
               && _ram2Controller != null && !_ram2Controller.IsInstalled,
            33 => ram1Holder != null && ram1Holder.IsAvailable() && ram2Holder != null && ram2Holder.IsAvailable(),
            34 => _ssdController != null && _ssdController.GetComponentsInChildren<ScrewController>(true).All(s => s.IsUnscrewed()),
            35 => ssdHolder != null && ssdHolder.IsAvailable(),
            36 => _heatsinkController != null && _heatsinkController.CanBeRemoved,
            37 => _heatsinkController != null && _heatsinkController.GetComponentsInChildren<ScrewController>(true).All(s => s.IsUnscrewed()),
            38 => _heatsinkController != null && !_heatsinkController.IsInstalledInSlot,
            39 => cpuSlotController != null && !cpuSlotController.IsLockClosed,
            40 => _cpuController != null && _cpuController.CurrentPasteState == CPUController.PasteState.NoPaste,
            41 => cpuSlotController != null && !cpuSlotController.IsCPUInstalled,
            42 => cmosHolder != null && cmosHolder.IsAvailable(),
            _  => false
        };
    }

    // ── Assembly condition checks ─────────────────────────────────────────────

    private bool CheckAssemblyCondition(int index)
    {
        return index switch
        {
             0 => cmosHolder != null && !cmosHolder.IsAvailable(),
             1 => cpuSlotController != null && cpuSlotController.IsCPUInstalled,
             2 => _cpuController != null && _cpuController.CurrentPasteState == CPUController.PasteState.PasteApplied,
             3 => cpuSlotController != null && cpuSlotController.IsLockClosed,
             4 => _heatsinkController != null && _heatsinkController.IsInstalledInSlot,
             5 => _heatsinkController != null && _heatsinkController.GetComponentsInChildren<ScrewController>(true).All(s => s.IsScrewed()),
             6 => _heatsinkController != null && !_heatsinkController.CanBeRemoved,
             7 => _ssdController != null && _ssdController.IsInSlot,
             8 => _ssdController != null && _ssdController.GetComponentsInChildren<ScrewController>(true).All(s => s.IsScrewed()),
             9 => ram1Holder != null && !ram1Holder.IsAvailable() && ram2Holder != null && !ram2Holder.IsAvailable(),
            10 => _ram1Controller != null && _ram1Controller.IsLeftLatched && _ram1Controller.IsRightLatched
               && _ram2Controller != null && _ram2Controller.IsLeftLatched && _ram2Controller.IsRightLatched,
            11 => motherboardController != null
               && motherboardController.IsUninstalledFromSystemUnit
               && GameManager.Instance?.IsEditorOpen != true,
            12 => psuHolder != null && psuHolder.hardwarePrefab != null && psuHolder.hardwarePrefab.GetComponentInParent<SlotContainer>() != null,
            13 => _hddController != null && _hddController.IsInSlot,
            14 => motherboardController != null && !motherboardController.IsUninstalledFromSystemUnit,
            15 => gpuController != null && gpuController.IsInSlot,
            16 => gpuController != null && gpuController.IsLatched,
            17 => gpuController != null && gpuController.GetComponentsInChildren<ScrewController>(true).All(s => s.IsScrewed()),
            18 => gpuController != null && gpuController.GetComponentsInChildren<CablePort>(true).All(c => c.IsInstalled),
            19 => MBScrewsScrewed(),
            20 => _mb24PinATXPort   != null && _mb24PinATXPort.IsInstalled,
            21 => _mb4PinATX12VPort != null && _mb4PinATX12VPort.IsInstalled,
            22 => _mbSATADataPort   != null && _mbSATADataPort.IsInstalled,
            23 => frontPanelConnectorController != null && frontPanelConnectorController.IsPhase1Installed,
            24 => _frontPanelDetailView != null && _frontPanelDetailView.gameObject.activeSelf,
            25 => frontPanelConnectorController != null && frontPanelConnectorController.IsPhase2FullyInstalled,
            26 => _hddController != null && _hddController.IsInSlot
               && _hddController.GetComponentsInChildren<ScrewController>(true).All(s => s.IsScrewed()),
            27 => _hddController != null && _hddController.SATAPowerPort != null && _hddController.SATAPowerPort.IsInstalled,
            28 => _hddController != null && _hddController.SATADataPort  != null && _hddController.SATADataPort.IsInstalled,
            29 => coverController != null && !coverController.IsOpen(),
            30 => coverController != null && !coverController.IsOpen() && coverController.AreAllScrewsScrewed(),
            31 => _avrBackPorts    != null && _avrBackPorts.Length    > 0 && _avrBackPorts.All(p => p.IsInstalled),
            32 => _monitorBackPorts != null && _monitorBackPorts.Length > 0 && _monitorBackPorts.All(p => p.IsInstalled),
            33 => _suBackPorts     != null && _suBackPorts.Length     > 0 && _suBackPorts.All(p => p.IsInstalled),
            34 => psuSwitch          != null && psuSwitch.IsOn,
            35 => avrPowerButton     != null && avrPowerButton.IsPoweredOn,
            36 => monitorPowerButton != null && monitorPowerButton.IsPoweredOn,
            37 => suPowerButton      != null && suPowerButton.IsPoweredOn,
            38 => _ppeDustMask    == null || !_ppeDustMask.IsActive,
            39 => _ppeSafetyShoes == null || !_ppeSafetyShoes.IsActive,
            40 => _ppeGloves      == null || !_ppeGloves.IsActive,
            41 => _ppeGoggles     == null || !_ppeGoggles.IsActive,
            42 => _ppeWristStrap  == null || !_ppeWristStrap.IsActive,
            _  => false
        };
    }

    // ── Disassembly latch & window ────────────────────────────────────────────

    private void CheckCurrentDisassemblyTask()
    {
        while (_currentDisassemblyTask < DISASSEMBLY_COUNT && !_disassemblyAllDone)
        {
            if (_disassemblyLatched[_currentDisassemblyTask]) { AdvanceDisassemblyIfPossible(); continue; }
            if (!CheckDisassemblyCondition(_currentDisassemblyTask)) return;
            LatchDisassemblyTask(_currentDisassemblyTask);
        }
    }

    private void LatchDisassemblyTask(int index)
    {
        _disassemblyLatched[index] = true;

        if (disassemblyTaskObjects[index] != null && disassemblyFinishedParent != null)
            disassemblyTaskObjects[index].transform.SetParent(disassemblyFinishedParent, false);

        AdvanceDisassemblyIfPossible();
        OnTasksUpdated?.Invoke();
        RefreshDisassemblyWindow();
        RefreshDisassemblyGroupTitle();

        if (disassemblyGroupOverviewPopup != null && disassemblyGroupOverviewPopup.activeSelf)
            RefreshDisassemblyPopup();

        if (!_isDisassemblyFlashing)
            CheckDisassemblyGroupCompletion();
    }

    private void AdvanceDisassemblyIfPossible()
    {
        while (_currentDisassemblyTask < DISASSEMBLY_COUNT && _disassemblyLatched[_currentDisassemblyTask])
            _currentDisassemblyTask++;

        if (_currentDisassemblyTask >= DISASSEMBLY_COUNT)
        {
            _disassemblyAllDone = true;
            StartCoroutine(TransitionToAssembly());
            Debug.Log("[NCIITaskListManager] Disassembly complete — transitioning to assembly.");
        }
    }

    private void RefreshDisassemblyWindow()
    {
        if (disassemblyTaskGroups == null || disassemblyTaskGroups.Length == 0)
        {
            int shown = 0;
            for (int i = _currentDisassemblyTask; i < DISASSEMBLY_COUNT && shown < 3; i++)
            {
                if (_disassemblyLatched[i]) continue;
                if (disassemblyTaskObjects[i] != null)
                {
                    disassemblyTaskObjects[i].transform.SetParent(disassemblyTaskParent, false);
                    disassemblyTaskObjects[i].SetActive(true);
                    shown++;
                }
            }
            return;
        }

        int groupEnd  = GetDisassemblyGroupLastTask();
        int displayed = 0;
        for (int i = _currentDisassemblyTask; i <= groupEnd && displayed < 3; i++)
        {
            if (_disassemblyLatched[i]) continue;
            if (disassemblyTaskObjects[i] != null)
            {
                disassemblyTaskObjects[i].transform.SetParent(disassemblyTaskParent, false);
                disassemblyTaskObjects[i].SetActive(true);
                displayed++;
            }
        }
    }

    // ── Assembly latch & window ───────────────────────────────────────────────

    private void CheckCurrentAssemblyTask()
    {
        while (_currentAssemblyTask < ASSEMBLY_COUNT && !_assemblyAllDone)
        {
            if (_assemblyLatched[_currentAssemblyTask]) { AdvanceAssemblyIfPossible(); continue; }
            if (!CheckAssemblyCondition(_currentAssemblyTask)) return;
            LatchAssemblyTask(_currentAssemblyTask);
        }
    }

    private void LatchAssemblyTask(int index)
    {
        _assemblyLatched[index] = true;

        if (assemblyTaskObjects[index] != null && assemblyFinishedParent != null)
            assemblyTaskObjects[index].transform.SetParent(assemblyFinishedParent, false);

        AdvanceAssemblyIfPossible();
        OnTasksUpdated?.Invoke();
        RefreshAssemblyWindow();
        RefreshAssemblyGroupTitle();

        if (assemblyGroupOverviewPopup != null && assemblyGroupOverviewPopup.activeSelf)
            RefreshAssemblyPopup();

        if (!_isAssemblyFlashing)
            CheckAssemblyGroupCompletion();
    }

    private void AdvanceAssemblyIfPossible()
    {
        while (_currentAssemblyTask < ASSEMBLY_COUNT && _assemblyLatched[_currentAssemblyTask])
            _currentAssemblyTask++;

        if (_currentAssemblyTask >= ASSEMBLY_COUNT)
        {
            _assemblyAllDone = true;
            ShowAllTasksCompleted();
            TopicManager.Instance?.MarkTopicComplete(0);
            Debug.Log("[NCIITaskListManager] Assembly complete — Topic 1 marked complete.");
        }
    }

    private void RefreshAssemblyWindow()
    {
        if (assemblyTaskGroups == null || assemblyTaskGroups.Length == 0)
        {
            int shown = 0;
            for (int i = _currentAssemblyTask; i < ASSEMBLY_COUNT && shown < 3; i++)
            {
                if (_assemblyLatched[i]) continue;
                if (assemblyTaskObjects[i] != null)
                {
                    assemblyTaskObjects[i].transform.SetParent(assemblyTaskParent, false);
                    assemblyTaskObjects[i].SetActive(true);
                    shown++;
                }
            }
            return;
        }

        int groupEnd  = GetAssemblyGroupLastTask();
        int displayed = 0;
        for (int i = _currentAssemblyTask; i <= groupEnd && displayed < 3; i++)
        {
            if (_assemblyLatched[i]) continue;
            if (assemblyTaskObjects[i] != null)
            {
                assemblyTaskObjects[i].transform.SetParent(assemblyTaskParent, false);
                assemblyTaskObjects[i].SetActive(true);
                displayed++;
            }
        }
    }

    // ── Disassembly group logic ───────────────────────────────────────────────

    private int GetDisassemblyGroupLastTask()
    {
        if (disassemblyTaskGroups == null || _currentDisassemblyGroup >= disassemblyTaskGroups.Length)
            return DISASSEMBLY_COUNT - 1;
        var g = disassemblyTaskGroups[_currentDisassemblyGroup];
        return Mathf.Min(g.FirstTask + g.TaskCount - 1, DISASSEMBLY_COUNT - 1);
    }

    private int CountDisassemblyCompletedInGroup(int groupIndex)
    {
        if (disassemblyTaskGroups == null || groupIndex >= disassemblyTaskGroups.Length) return 0;
        var g    = disassemblyTaskGroups[groupIndex];
        int last = Mathf.Min(g.FirstTask + g.TaskCount - 1, DISASSEMBLY_COUNT - 1);
        int count = 0;
        for (int i = g.FirstTask; i <= last; i++)
            if (_disassemblyLatched[i]) count++;
        return count;
    }

    private void CheckDisassemblyGroupCompletion()
    {
        if (disassemblyTaskGroups == null || _currentDisassemblyGroup >= disassemblyTaskGroups.Length) return;
        var g = disassemblyTaskGroups[_currentDisassemblyGroup];
        if (CountDisassemblyCompletedInGroup(_currentDisassemblyGroup) >= g.TaskCount)
            StartCoroutine(FlashAndAdvanceDisassembly());
    }

    private IEnumerator FlashAndAdvanceDisassembly()
    {
        _isDisassemblyFlashing = true;

        Color original = disassemblyGroupTitleTMP != null ? disassemblyGroupTitleTMP.color : Color.white;
        if (disassemblyGroupTitleTMP != null) disassemblyGroupTitleTMP.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        if (disassemblyGroupTitleTMP != null) disassemblyGroupTitleTMP.color = original;
        _isDisassemblyFlashing = false;

        if (!_disassemblyAllDone)
        {
            _currentDisassemblyGroup++;
            RefreshDisassemblyGroupTitle();
            RefreshDisassemblyWindow();
        }

        if (disassemblyGroupOverviewPopup != null && disassemblyGroupOverviewPopup.activeSelf)
            RefreshDisassemblyPopup();
    }

    private void RefreshDisassemblyGroupTitle()
    {
        if (disassemblyGroupTitleTMP == null || disassemblyTaskGroups == null || _currentDisassemblyGroup >= disassemblyTaskGroups.Length) return;
        var g         = disassemblyTaskGroups[_currentDisassemblyGroup];
        int completed = CountDisassemblyCompletedInGroup(_currentDisassemblyGroup);
        disassemblyGroupTitleTMP.text = $"{g.Title}  {completed} / {g.TaskCount}";
    }

    // ── Assembly group logic ──────────────────────────────────────────────────

    private int GetAssemblyGroupLastTask()
    {
        if (assemblyTaskGroups == null || _currentAssemblyGroup >= assemblyTaskGroups.Length)
            return ASSEMBLY_COUNT - 1;
        var g = assemblyTaskGroups[_currentAssemblyGroup];
        return Mathf.Min(g.FirstTask + g.TaskCount - 1, ASSEMBLY_COUNT - 1);
    }

    private int CountAssemblyCompletedInGroup(int groupIndex)
    {
        if (assemblyTaskGroups == null || groupIndex >= assemblyTaskGroups.Length) return 0;
        var g    = assemblyTaskGroups[groupIndex];
        int last = Mathf.Min(g.FirstTask + g.TaskCount - 1, ASSEMBLY_COUNT - 1);
        int count = 0;
        for (int i = g.FirstTask; i <= last; i++)
            if (_assemblyLatched[i]) count++;
        return count;
    }

    private void CheckAssemblyGroupCompletion()
    {
        if (assemblyTaskGroups == null || _currentAssemblyGroup >= assemblyTaskGroups.Length) return;
        var g = assemblyTaskGroups[_currentAssemblyGroup];
        if (CountAssemblyCompletedInGroup(_currentAssemblyGroup) >= g.TaskCount)
            StartCoroutine(FlashAndAdvanceAssembly());
    }

    private IEnumerator FlashAndAdvanceAssembly()
    {
        _isAssemblyFlashing = true;

        Color original = assemblyGroupTitleTMP != null ? assemblyGroupTitleTMP.color : Color.white;
        if (assemblyGroupTitleTMP != null) assemblyGroupTitleTMP.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        if (assemblyGroupTitleTMP != null) assemblyGroupTitleTMP.color = original;
        _isAssemblyFlashing = false;

        if (_assemblyAllDone)
        {
            if (assemblyGroupTitleTMP != null)
                assemblyGroupTitleTMP.text = "Objective in COC I Topic 1 is met, all tasks have been completed.";
        }
        else
        {
            _currentAssemblyGroup++;
            RefreshAssemblyGroupTitle();
            RefreshAssemblyWindow();
        }

        if (assemblyGroupOverviewPopup != null && assemblyGroupOverviewPopup.activeSelf)
            RefreshAssemblyPopup();
    }

    private void RefreshAssemblyGroupTitle()
    {
        if (assemblyGroupTitleTMP == null || assemblyTaskGroups == null || _currentAssemblyGroup >= assemblyTaskGroups.Length) return;
        var g         = assemblyTaskGroups[_currentAssemblyGroup];
        int completed = CountAssemblyCompletedInGroup(_currentAssemblyGroup);
        assemblyGroupTitleTMP.text = $"{g.Title}  {completed} / {g.TaskCount}";
    }

    // ── Disassembly Popup ─────────────────────────────────────────────────────

    private void OpenDisassemblyPopup()  { RefreshDisassemblyPopup(); disassemblyGroupOverviewPopup?.SetActive(true); }
    private void CloseDisassemblyPopup() { disassemblyGroupOverviewPopup?.SetActive(false); }

    private void BuildDisassemblyPopupRows()
    {
        if (disassemblyGroupRowParent == null || groupRowPrefab == null || disassemblyTaskGroups == null) return;

        foreach (Transform child in disassemblyGroupRowParent)
            Destroy(child.gameObject);

        _disassemblyPopupRowTMPs = new TMP_Text[disassemblyTaskGroups.Length][];
        for (int g = 0; g < disassemblyTaskGroups.Length; g++)
        {
            var row  = Instantiate(groupRowPrefab, disassemblyGroupRowParent);
            var tmps = row.GetComponentsInChildren<TMP_Text>();
            _disassemblyPopupRowTMPs[g] = new TMP_Text[3];
            if (tmps.Length >= 3)
            {
                _disassemblyPopupRowTMPs[g][0] = tmps[0];
                _disassemblyPopupRowTMPs[g][1] = tmps[1];
                _disassemblyPopupRowTMPs[g][2] = tmps[2];
            }
        }
    }

    private void RefreshDisassemblyPopup()
    {
        if (_disassemblyPopupRowTMPs == null || disassemblyTaskGroups == null) return;

        for (int g = 0; g < disassemblyTaskGroups.Length; g++)
        {
            if (_disassemblyPopupRowTMPs[g] == null) continue;

            var   group     = disassemblyTaskGroups[g];
            int   completed = CountDisassemblyCompletedInGroup(g);
            bool  isDone    = completed >= group.TaskCount;

            string statusText;
            Color  rowColor;

            if (isDone)                              { statusText = "Done";    rowColor = groupDoneColor; }
            else if (g == _currentDisassemblyGroup)  { statusText = "Active";  rowColor = groupActiveColor; }
            else                                     { statusText = "Pending"; rowColor = groupPendingColor; }

            if (_disassemblyPopupRowTMPs[g][0] != null) { _disassemblyPopupRowTMPs[g][0].text = group.Title;                         _disassemblyPopupRowTMPs[g][0].color = rowColor; }
            if (_disassemblyPopupRowTMPs[g][1] != null) { _disassemblyPopupRowTMPs[g][1].text = $"{completed} / {group.TaskCount}";  _disassemblyPopupRowTMPs[g][1].color = rowColor; }
            if (_disassemblyPopupRowTMPs[g][2] != null) { _disassemblyPopupRowTMPs[g][2].text = statusText;                          _disassemblyPopupRowTMPs[g][2].color = rowColor; }
        }
    }

    // ── Assembly Popup ────────────────────────────────────────────────────────

    private void OpenAssemblyPopup()  { RefreshAssemblyPopup(); assemblyGroupOverviewPopup?.SetActive(true); }
    private void CloseAssemblyPopup() { assemblyGroupOverviewPopup?.SetActive(false); }

    private void BuildAssemblyPopupRows()
    {
        if (assemblyGroupRowParent == null || groupRowPrefab == null || assemblyTaskGroups == null) return;

        foreach (Transform child in assemblyGroupRowParent)
            Destroy(child.gameObject);

        _assemblyPopupRowTMPs = new TMP_Text[assemblyTaskGroups.Length][];
        for (int g = 0; g < assemblyTaskGroups.Length; g++)
        {
            var row  = Instantiate(groupRowPrefab, assemblyGroupRowParent);
            var tmps = row.GetComponentsInChildren<TMP_Text>();
            _assemblyPopupRowTMPs[g] = new TMP_Text[3];
            if (tmps.Length >= 3)
            {
                _assemblyPopupRowTMPs[g][0] = tmps[0];
                _assemblyPopupRowTMPs[g][1] = tmps[1];
                _assemblyPopupRowTMPs[g][2] = tmps[2];
            }
        }
    }

    private void RefreshAssemblyPopup()
    {
        if (_assemblyPopupRowTMPs == null || assemblyTaskGroups == null) return;

        for (int g = 0; g < assemblyTaskGroups.Length; g++)
        {
            if (_assemblyPopupRowTMPs[g] == null) continue;

            var   group     = assemblyTaskGroups[g];
            int   completed = CountAssemblyCompletedInGroup(g);
            bool  isDone    = completed >= group.TaskCount;

            string statusText;
            Color  rowColor;

            if (isDone)                          { statusText = "Done";    rowColor = groupDoneColor; }
            else if (g == _currentAssemblyGroup) { statusText = "Active";  rowColor = groupActiveColor; }
            else                                 { statusText = "Pending"; rowColor = groupPendingColor; }

            if (_assemblyPopupRowTMPs[g][0] != null) { _assemblyPopupRowTMPs[g][0].text = group.Title;                         _assemblyPopupRowTMPs[g][0].color = rowColor; }
            if (_assemblyPopupRowTMPs[g][1] != null) { _assemblyPopupRowTMPs[g][1].text = $"{completed} / {group.TaskCount}";  _assemblyPopupRowTMPs[g][1].color = rowColor; }
            if (_assemblyPopupRowTMPs[g][2] != null) { _assemblyPopupRowTMPs[g][2].text = statusText;                          _assemblyPopupRowTMPs[g][2].color = rowColor; }
        }
    }

    // ── Phase transition ──────────────────────────────────────────────────────

    public bool IsTransitioningToAssembly => _isTransitioningToAssembly;

    private IEnumerator TransitionToAssembly()
    {
        _isTransitioningToAssembly = true;

        if (transitionText != null)
        {
            transitionText.text = transitionMessage;
            transitionText.gameObject.SetActive(true);
        }
        _displayOverride      = transitionMessage;
        _isCompletionOverride = false;
        OnTasksUpdated?.Invoke();

        yield return new WaitForSeconds(transitionDuration);

        _isTransitioningToAssembly = false;

        if (transitionText != null) transitionText.gameObject.SetActive(false);

        _displayOverride      = null;
        _isCompletionOverride = false;
        _showingAssembly      = true;

        if (disassemblyTaskParent != null) disassemblyTaskParent.gameObject.SetActive(false);
        if (assemblyTaskParent   != null) assemblyTaskParent.gameObject.SetActive(true);

        RefreshAssemblyWindow();
        OnTasksUpdated?.Invoke();
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

    private void OnDestroy()
    {
        TopicManager.OnAllTopicsComplete -= OnAllTopicsComplete;
    }

    // ── Shared Helpers ────────────────────────────────────────────────────────

    private Transform GetMBPhase1Root()
    {
        if (motherboardController == null) return null;
        MotherboardPhaseManager pm = motherboardController.GetComponent<MotherboardPhaseManager>();
        Transform root = pm != null ? pm.GetPhase1Root() : null;
        return root != null ? root : motherboardController.transform;
    }

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

        if (_showingAssembly)
        {
            if (_assemblyAllDone || _currentAssemblyTask >= ASSEMBLY_COUNT) return null;
            var obj = assemblyTaskObjects[_currentAssemblyTask];
            if (obj == null) return null;
            var tmp = obj.GetComponent<TMP_Text>();
            return tmp != null ? tmp.text : null;
        }
        else
        {
            if (_disassemblyAllDone || _currentDisassemblyTask >= DISASSEMBLY_COUNT) return null;
            var obj = disassemblyTaskObjects[_currentDisassemblyTask];
            if (obj == null) return null;
            var tmp = obj.GetComponent<TMP_Text>();
            return tmp != null ? tmp.text : null;
        }
    }

    public Color GetDisplayColor(Color fallback) =>
        (_isCompletionOverride && _displayOverride != null) ? Color.green : fallback;

    public int GetCompletedTaskCount()
    {
        int count = 0;
        for (int i = 0; i < DISASSEMBLY_COUNT; i++) if (_disassemblyLatched[i]) count++;
        for (int i = 0; i < ASSEMBLY_COUNT;    i++) if (_assemblyLatched[i])    count++;
        return count;
    }

    public static void CheckConditions() { } // No-op; Update() handles polling
}
