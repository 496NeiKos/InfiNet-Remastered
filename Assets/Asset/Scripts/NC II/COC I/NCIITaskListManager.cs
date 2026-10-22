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
 *  ── TASK GROUPS — 86 disassembly + 65 assembly tasks ─────────────
 *    Configure both disassemblyTaskGroups[] and assemblyTaskGroups[]
 *    in the Inspector. FirstTask index is relative to 0 within each
 *    phase's task array (not a combined index).
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

    private const int DISASSEMBLY_COUNT = 85;
    private const int ASSEMBLY_COUNT    = 65;

    // ── Disassembly Task UI ───────────────────────────────────────────────────

    [Header("Disassembly UI")]
    [SerializeField] private Transform    disassemblyTaskParent;
    [SerializeField] private Transform    disassemblyFinishedParent;
    [SerializeField] private GameObject[] disassemblyTaskObjects = new GameObject[85];

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
    [SerializeField] private HardwareHolder motherboardHolder;

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

    // ── New Fields (wire once in Inspector) ───────────────────────────────────

    [Header("System Unit Controller (for view tracking)")]
    [SerializeField] private SystemUnitController systemUnitController;

    [Header("Peripheral Controllers")]
    [SerializeField] private AVRController     _avrController;
    [SerializeField] private MonitorController _monitorController;

    [Header("Front Panel Sub-Pins")]
    [SerializeField] private CablePort _fpPinPowerSW;
    [SerializeField] private CablePort _fpPinResetSW;
    [SerializeField] private CablePort _fpPinHddLED;
    [SerializeField] private CablePort _fpPinPowerLEDPos;
    [SerializeField] private CablePort _fpPinPowerLEDNeg;

    [Header("PSU Screws")]
    [SerializeField] private ScrewController[] _psuScrews;

    [Header("UI Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject ppeInventoryPanel;
    [SerializeField] private GameObject gpuLatchSideView;

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
    private bool   _mbTakenOutForAssembly     = false;

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

        // All four buttons route through the phase-aware dispatcher so a single
        // shared popup panel works correctly for both disassembly and assembly phases.
        disassemblyGroupTitleBtn?.onClick.AddListener(OpenCurrentGroupPopup);
        disassemblyClosePopupBtn?.onClick.AddListener(CloseCurrentGroupPopup);
        assemblyGroupTitleBtn?.onClick.AddListener(OpenCurrentGroupPopup);
        assemblyClosePopupBtn?.onClick.AddListener(CloseCurrentGroupPopup);
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
        { Debug.LogError("[NCIITaskListManager] Assign all 86 disassembly task objects."); return; }
        if (assemblyTaskObjects == null || assemblyTaskObjects.Length < ASSEMBLY_COUNT)
        { Debug.LogError("[NCIITaskListManager] Assign all 65 assembly task objects."); return; }

        for (int i = 0; i < DISASSEMBLY_COUNT; i++)
            if (disassemblyTaskObjects[i] != null) disassemblyTaskObjects[i].SetActive(false);
        for (int i = 0; i < ASSEMBLY_COUNT; i++)
            if (assemblyTaskObjects[i] != null) assemblyTaskObjects[i].SetActive(false);

        disassemblyGroupOverviewPopup?.SetActive(false);
        assemblyGroupOverviewPopup?.SetActive(false);
        if (assemblyTaskParent != null) assemblyTaskParent.gameObject.SetActive(false);
        if (transitionText != null)     transitionText.gameObject.SetActive(false);
        if (allTasksCompletedText != null) allTasksCompletedText.gameObject.SetActive(false);

        RefreshDisassemblyGroupTitle();
        RefreshAssemblyGroupTitle();
        RefreshDisassemblyWindow();
    }

    private void Update()
    {
        if (_isTransitioningToAssembly) return;

        if (_showingAssembly)
        {
            if (!_mbTakenOutForAssembly && motherboardHolder != null && !motherboardHolder.IsAvailable())
                _mbTakenOutForAssembly = true;

            if (!_assemblyAllDone) CheckCurrentAssemblyTask();
        }
        else
        {
            if (!_disassemblyAllDone) CheckCurrentDisassemblyTask();
        }
    }

    // ── Shared Condition Helpers ──────────────────────────────────────────────

    // True when the given transform is currently displayed in a detail layer.
    private bool IsInDetailView(Transform t)
    {
        if (t == null || GameManager.Instance == null || !GameManager.Instance.IsEditorOpen) return false;
        var fl = GameManager.Instance.firstLayer?.transform;
        var sl = GameManager.Instance.secondLayer?.transform;
        var tl = GameManager.Instance.thirdLayer?.transform;
        return (fl != null && t.parent == fl)
            || (sl != null && t.parent == sl)
            || (tl != null && t.parent == tl);
    }

    // True when any System Unit view panel is active (SU editor is open).
    private bool IsSUEditorOpen() =>
        systemUnitController != null &&
        (systemUnitController.systemUnitFront.activeSelf ||
         systemUnitController.systemUnitSide.activeSelf  ||
         systemUnitController.systemUnitBack.activeSelf);

    private static bool AllUnscrewed(ScrewController[] sc) =>
        sc != null && sc.Length > 0 && sc.All(s => s.IsUnscrewed());

    private static bool AllScrewed(ScrewController[] sc) =>
        sc != null && sc.Length > 0 && sc.All(s => s.IsScrewed());

    // ── Disassembly condition checks ──────────────────────────────────────────

    private bool CheckDisassemblyCondition(int index)
    {
        return index switch
        {
            // ── Group 1: Put On PPE (D1–D7) ──────────────────────────────────
             0 => settingsPanel     != null && settingsPanel.activeSelf,
             1 => ppeInventoryPanel != null && ppeInventoryPanel.activeSelf,
             2 => _ppeWristStrap    != null && _ppeWristStrap.IsActive,
             3 => _ppeGoggles       != null && _ppeGoggles.IsActive,
             4 => _ppeGloves        != null && _ppeGloves.IsActive,
             5 => _ppeSafetyShoes   != null && _ppeSafetyShoes.IsActive,
             6 => _ppeDustMask      != null && _ppeDustMask.IsActive,

            // ── Group 2: Terminate Power Sources (D8–D15) ────────────────────
             7 => ppeInventoryPanel == null || !ppeInventoryPanel.activeSelf,
             8 => IsSUEditorOpen(),
             9 => suPowerButton   == null || !suPowerButton.IsPoweredOn,
            10 => systemUnitController != null && systemUnitController.systemUnitBack.activeSelf,
            11 => psuSwitch       == null || !psuSwitch.IsOn,
            12 => GameManager.Instance == null || !GameManager.Instance.IsEditorOpen,
            13 => _avrController  != null && IsInDetailView(_avrController.transform),
            14 => avrPowerButton  == null || !avrPowerButton.IsPoweredOn,

            // ── Group 3: Remove External Cables (D16–D28) ────────────────────
            15 => GameManager.Instance == null || !GameManager.Instance.IsEditorOpen,
            16 => IsSUEditorOpen(),
            17 => systemUnitController != null && systemUnitController.systemUnitBack.activeSelf,
            18 => _suBackPorts != null && _suBackPorts.Length > 0 && _suBackPorts[0] != null && _suBackPorts[0].IsUninstalled,
            19 => _suBackPorts != null && _suBackPorts.Length > 1 && _suBackPorts[1] != null && _suBackPorts[1].IsUninstalled,
            20 => GameManager.Instance == null || !GameManager.Instance.IsEditorOpen,
            21 => _monitorController != null && IsInDetailView(_monitorController.transform),
            22 => _monitorController != null && IsInDetailView(_monitorController.transform),
            23 => _monitorBackPorts != null && _monitorBackPorts.Length > 0 && _monitorBackPorts[0] != null && _monitorBackPorts[0].IsUninstalled,
            24 => _monitorBackPorts != null && _monitorBackPorts.Length > 1 && _monitorBackPorts[1] != null && _monitorBackPorts[1].IsUninstalled,
            25 => GameManager.Instance == null || !GameManager.Instance.IsEditorOpen,
            26 => _avrController != null && IsInDetailView(_avrController.transform),
            27 => _avrBackPorts  != null && _avrBackPorts.Length > 0 && _avrBackPorts.All(p => p.IsUninstalled),

            // ── Group 4: Remove Case Cover (D29–D34) ─────────────────────────
            28 => GameManager.Instance == null || !GameManager.Instance.IsEditorOpen,
            29 => IsSUEditorOpen(),
            30 => systemUnitController != null && systemUnitController.systemUnitBack.activeSelf,
            31 => coverController != null && coverController.AreAllScrewsUnscrewed(),
            32 => systemUnitController != null && systemUnitController.systemUnitSide.activeSelf,
            33 => coverController != null && coverController.IsOpen(),

            // ── Group 5: Remove HDD (D35–D40) ────────────────────────────────
            34 => _hddController != null && IsInDetailView(_hddController.transform),
            35 => _hddController != null && _hddController.SATADataPort  != null && !_hddController.SATADataPort.IsInstalled,
            36 => _hddController != null && _hddController.SATAPowerPort != null && !_hddController.SATAPowerPort.IsInstalled,
            37 => _hddController != null && AllUnscrewed(_hddController.GetComponentsInChildren<ScrewController>(true)),
            38 => systemUnitController != null && systemUnitController.systemUnitSide.activeSelf
               && (_hddController == null || !IsInDetailView(_hddController.transform)),
            39 => hddHolder != null && hddHolder.IsAvailable(),

            // ── Group 6: Remove GPU (D41–D48) ────────────────────────────────
            40 => motherboardController != null && !motherboardController.IsUninstalledFromSystemUnit
               && IsInDetailView(motherboardController.transform),
            41 => gpuController != null && IsInDetailView(gpuController.transform),
            42 => gpuController != null
               && gpuController.GetComponentsInChildren<CablePort>(true) is var gpuCablesD
               && (gpuCablesD.Length == 0 || gpuCablesD.All(c => !c.IsInstalled)),
            43 => gpuController != null && AllUnscrewed(gpuController.GetComponentsInChildren<ScrewController>(true)),
            44 => gpuLatchSideView != null && gpuLatchSideView.activeSelf,
            45 => gpuController != null && !gpuController.IsLatched,
            46 => motherboardController != null && !motherboardController.IsUninstalledFromSystemUnit
               && IsInDetailView(motherboardController.transform)
               && (gpuController == null || !IsInDetailView(gpuController.transform)),
            47 => gpuController != null && !gpuController.IsInSlot,

            // ── Group 7: Remove MB Cables & Motherboard (D49–D62) ────────────
            48 => _frontPanelDetailView != null && _frontPanelDetailView.gameObject.activeSelf,
            49 => _fpPinPowerSW     != null && !_fpPinPowerSW.IsInstalled,
            50 => _fpPinResetSW     != null && !_fpPinResetSW.IsInstalled,
            51 => _fpPinHddLED      != null && !_fpPinHddLED.IsInstalled,
            52 => _fpPinPowerLEDPos != null && !_fpPinPowerLEDPos.IsInstalled,
            53 => _fpPinPowerLEDNeg != null && !_fpPinPowerLEDNeg.IsInstalled,
            54 => motherboardController != null && !motherboardController.IsUninstalledFromSystemUnit
               && IsInDetailView(motherboardController.transform)
               && (_frontPanelDetailView == null || !_frontPanelDetailView.gameObject.activeSelf),
            55 => frontPanelConnectorController != null && !frontPanelConnectorController.IsPhase1Installed,
            56 => _mbSATADataPort   != null && !_mbSATADataPort.IsInstalled,
            57 => _mb4PinATX12VPort != null && !_mb4PinATX12VPort.IsInstalled,
            58 => _mb24PinATXPort   != null && !_mb24PinATXPort.IsInstalled,
            59 => MBScrewsUnscrewed(),
            60 => systemUnitController != null && systemUnitController.systemUnitSide.activeSelf
               && (motherboardController == null || !IsInDetailView(motherboardController.transform)),
            61 => motherboardController != null && motherboardController.IsUninstalledFromSystemUnit,

            // ── Group 8: Remove PSU (D63–D66) ────────────────────────────────
            62 => systemUnitController != null && systemUnitController.systemUnitBack.activeSelf,
            63 => _psuScrews != null && _psuScrews.Length > 0 && _psuScrews.All(s => s.IsUnscrewed()),
            64 => systemUnitController != null && systemUnitController.systemUnitSide.activeSelf,
            65 => psuHolder != null && psuHolder.IsAvailable(),

            // ── Group 9: Dismantle MB Components on Workspace (D67–D84) ──────
            66 => GameManager.Instance == null || !GameManager.Instance.IsEditorOpen,
            67 => motherboardController != null
               && motherboardController.IsUninstalledFromSystemUnit
               && motherboardController.gameObject.activeSelf,
            68 => motherboardController != null
               && motherboardController.IsUninstalledFromSystemUnit
               && IsInDetailView(motherboardController.transform),
            69 => motherboardController != null && motherboardController.IsUninstalledFromSystemUnit
               && ((_ram1Controller != null && IsInDetailView(_ram1Controller.transform))
                || (_ram2Controller != null && IsInDetailView(_ram2Controller.transform))),
            // either RAM unlatched/removed from slot
            70 => (_ram1Controller != null && !_ram1Controller.IsInstalled)
               || (_ram2Controller != null && !_ram2Controller.IsInstalled),
            // first RAM (whichever) dragged to storage
            71 => (ram1Holder != null && ram1Holder.IsAvailable())
               || (ram2Holder != null && ram2Holder.IsAvailable()),
            // both RAMs now in storage
            72 => ram1Holder != null && ram1Holder.IsAvailable()
               && ram2Holder != null && ram2Holder.IsAvailable(),
            73 => _heatsinkController != null && IsInDetailView(_heatsinkController.transform),
            74 => _heatsinkController != null && _heatsinkController.CanBeRemoved,
            75 => _heatsinkController != null && AllUnscrewed(_heatsinkController.GetComponentsInChildren<ScrewController>(true)),
            76 => _heatsinkController != null && !_heatsinkController.IsInstalledInSlot,
            77 => cpuSlotController != null && !cpuSlotController.IsLockClosed,
            78 => _cpuController != null && IsInDetailView(_cpuController.transform),
            79 => _cpuController != null && _cpuController.CurrentPasteState == CPUController.PasteState.NoPaste,
            80 => cpuSlotController != null && !cpuSlotController.IsCPUInstalled,
            81 => _ssdController != null && IsInDetailView(_ssdController.transform),
            82 => _ssdController != null && AllUnscrewed(_ssdController.GetComponentsInChildren<ScrewController>(true)),
            83 => _ssdController != null && !_ssdController.IsInSlot,
            84 => cmosHolder != null && cmosHolder.IsAvailable(),
            _  => false
        };
    }

    // ── Assembly condition checks ─────────────────────────────────────────────

    private bool CheckAssemblyCondition(int index)
    {
        return index switch
        {
            // ── Group 1: Install MB Components (A1–A11) ───────────────────────
             0 => cmosHolder != null && !cmosHolder.IsAvailable(),
             1 => _ssdController != null && _ssdController.IsInSlot,
             2 => _ssdController != null && AllScrewed(_ssdController.GetComponentsInChildren<ScrewController>(true)),
             3 => cpuSlotController != null && cpuSlotController.IsCPUInstalled,
             4 => _cpuController != null && _cpuController.CurrentPasteState == CPUController.PasteState.PasteApplied,
             5 => cpuSlotController != null && cpuSlotController.IsLockClosed,
             6 => _heatsinkController != null && _heatsinkController.IsInstalledInSlot,
             7 => _heatsinkController != null && _heatsinkController.IsFullyInstalled,
             // either RAM placed into its slot
             8 => (ram1Holder != null && !ram1Holder.IsAvailable())
               || (ram2Holder != null && !ram2Holder.IsAvailable()),
             // first RAM (whichever) fully latched
             9 => (_ram1Controller != null && _ram1Controller.IsLeftLatched && _ram1Controller.IsRightLatched)
               || (_ram2Controller != null && _ram2Controller.IsLeftLatched && _ram2Controller.IsRightLatched),
             // both RAMs fully latched
            10 => _ram1Controller != null && _ram1Controller.IsLeftLatched && _ram1Controller.IsRightLatched
               && _ram2Controller != null && _ram2Controller.IsLeftLatched && _ram2Controller.IsRightLatched,

            // ── Group 2: Install PSU (A12–A16) ────────────────────────────────
            11 => _mbTakenOutForAssembly && motherboardHolder != null && motherboardHolder.IsAvailable(),
            12 => systemUnitController != null && systemUnitController.systemUnitSide.activeSelf,
            13 => psuHolder != null && psuHolder.hardwarePrefab != null
               && psuHolder.hardwarePrefab.GetComponentInParent<SlotContainer>() != null,
            14 => systemUnitController != null && systemUnitController.systemUnitBack.activeSelf,
            15 => _psuScrews != null && _psuScrews.Length > 0 && _psuScrews.All(s => s.IsScrewed()),

            // ── Group 3: Install Motherboard & Cables (A17–A30) ───────────────
            16 => systemUnitController != null && systemUnitController.systemUnitSide.activeSelf,
            17 => motherboardController != null && !motherboardController.IsUninstalledFromSystemUnit,
            18 => motherboardController != null && !motherboardController.IsUninstalledFromSystemUnit
               && IsInDetailView(motherboardController.transform),
            19 => MBScrewsScrewed(),
            20 => _mb24PinATXPort   != null && _mb24PinATXPort.IsInstalled,
            21 => _mb4PinATX12VPort != null && _mb4PinATX12VPort.IsInstalled,
            22 => _mbSATADataPort   != null && _mbSATADataPort.IsInstalled,
            23 => frontPanelConnectorController != null && frontPanelConnectorController.IsPhase1Installed,
            24 => _frontPanelDetailView != null && _frontPanelDetailView.gameObject.activeSelf,
            25 => _fpPinPowerLEDNeg != null && _fpPinPowerLEDNeg.IsInstalled,
            26 => _fpPinPowerLEDPos != null && _fpPinPowerLEDPos.IsInstalled,
            27 => _fpPinHddLED      != null && _fpPinHddLED.IsInstalled,
            28 => _fpPinResetSW     != null && _fpPinResetSW.IsInstalled,
            29 => _fpPinPowerSW     != null && _fpPinPowerSW.IsInstalled,

            // ── Group 4: Install GPU (A31–A38) ────────────────────────────────
            30 => motherboardController != null && !motherboardController.IsUninstalledFromSystemUnit
               && IsInDetailView(motherboardController.transform)
               && (_frontPanelDetailView == null || !_frontPanelDetailView.gameObject.activeSelf),
            31 => gpuController != null && gpuController.IsInSlot,
            32 => gpuController != null && IsInDetailView(gpuController.transform),
            33 => gpuLatchSideView != null && gpuLatchSideView.activeSelf,
            34 => gpuController != null && gpuController.IsLatched,
            35 => gpuController != null && IsInDetailView(gpuController.transform)
               && (gpuLatchSideView == null || !gpuLatchSideView.activeSelf),
            36 => gpuController != null
               && gpuController.GetComponentsInChildren<CablePort>(true) is var gpuCablesA
               && gpuCablesA.Length > 0
               && gpuCablesA.All(c => c.IsInstalled),
            37 => gpuController != null && AllScrewed(gpuController.GetComponentsInChildren<ScrewController>(true)),

            // ── Group 5: Install HDD (A39–A44) ────────────────────────────────
            38 => systemUnitController != null && systemUnitController.systemUnitSide.activeSelf
               && (motherboardController == null || !IsInDetailView(motherboardController.transform)),
            39 => _hddController != null && _hddController.IsInSlot,
            40 => _hddController != null && IsInDetailView(_hddController.transform),
            41 => _hddController != null && AllScrewed(_hddController.GetComponentsInChildren<ScrewController>(true)),
            42 => _hddController != null && _hddController.SATAPowerPort != null && _hddController.SATAPowerPort.IsInstalled,
            43 => _hddController != null && _hddController.SATADataPort  != null && _hddController.SATADataPort.IsInstalled,

            // ── Group 6: Close Case Cover (A45–A48) ───────────────────────────
            44 => systemUnitController != null && systemUnitController.systemUnitSide.activeSelf
               && (_hddController == null || !IsInDetailView(_hddController.transform)),
            45 => coverController != null && !coverController.IsOpen(),
            46 => systemUnitController != null && systemUnitController.systemUnitBack.activeSelf,
            47 => coverController != null && coverController.AreAllScrewsScrewed(),

            // ── Group 7: Connect External Cables (A49–A55) ────────────────────
            48 => _suBackPorts != null && _suBackPorts.Length > 0 && _suBackPorts[0] != null && _suBackPorts[0].IsInstalled,
            49 => _suBackPorts != null && _suBackPorts.Length > 1 && _suBackPorts[1] != null && _suBackPorts[1].IsInstalled,
            50 => _monitorController != null && IsInDetailView(_monitorController.transform),
            51 => _monitorBackPorts != null && _monitorBackPorts.Length > 0 && _monitorBackPorts[0] != null && _monitorBackPorts[0].IsInstalled,
            52 => _monitorBackPorts != null && _monitorBackPorts.Length > 1 && _monitorBackPorts[1] != null && _monitorBackPorts[1].IsInstalled,
            53 => _avrController != null && IsInDetailView(_avrController.transform),
            54 => _avrBackPorts != null && _avrBackPorts.Length > 0 && _avrBackPorts.All(p => p.IsInstalled),

            // ── Group 8: Power On (A56–A58) ───────────────────────────────────
            55 => avrPowerButton != null && avrPowerButton.IsPoweredOn,
            56 => psuSwitch      != null && psuSwitch.IsOn,
            57 => suPowerButton  != null && suPowerButton.IsPoweredOn,

            // ── Group 9: Remove PPE (A59–A65) ─────────────────────────────────
            58 => ppeInventoryPanel != null && ppeInventoryPanel.activeSelf,
            59 => _ppeDustMask    == null || !_ppeDustMask.IsActive,
            60 => _ppeSafetyShoes == null || !_ppeSafetyShoes.IsActive,
            61 => _ppeGloves      == null || !_ppeGloves.IsActive,
            62 => _ppeGoggles     == null || !_ppeGoggles.IsActive,
            63 => _ppeWristStrap  == null || !_ppeWristStrap.IsActive,
            64 => ppeInventoryPanel == null || !ppeInventoryPanel.activeSelf,
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

        if (ActiveGroupPopup != null && ActiveGroupPopup.activeSelf)
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

        if (ActiveGroupPopup != null && ActiveGroupPopup.activeSelf)
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

        if (ActiveGroupPopup != null && ActiveGroupPopup.activeSelf)
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

        if (ActiveGroupPopup != null && ActiveGroupPopup.activeSelf)
            RefreshAssemblyPopup();
    }

    private void RefreshAssemblyGroupTitle()
    {
        if (assemblyGroupTitleTMP == null || assemblyTaskGroups == null || _currentAssemblyGroup >= assemblyTaskGroups.Length) return;
        var g         = assemblyTaskGroups[_currentAssemblyGroup];
        int completed = CountAssemblyCompletedInGroup(_currentAssemblyGroup);
        assemblyGroupTitleTMP.text = $"{g.Title}  {completed} / {g.TaskCount}";
    }

    // ── Phase-aware group popup ───────────────────────────────────────────────

    // Returns the popup panel relevant to the current phase. Falls back to the
    // other phase's panel when a dedicated panel isn't wired — supports a single
    // shared popup for both phases.
    private GameObject ActiveGroupPopup =>
        _showingAssembly
            ? (assemblyGroupOverviewPopup  != null ? assemblyGroupOverviewPopup  : disassemblyGroupOverviewPopup)
            : (disassemblyGroupOverviewPopup != null ? disassemblyGroupOverviewPopup : assemblyGroupOverviewPopup);

    private void OpenCurrentGroupPopup()
    {
        if (_showingAssembly)
        {
            BuildAssemblyPopupRows();
            RefreshAssemblyPopup();
        }
        else
        {
            BuildDisassemblyPopupRows();
            RefreshDisassemblyPopup();
        }
        ActiveGroupPopup?.SetActive(true);
    }

    private void CloseCurrentGroupPopup()
    {
        disassemblyGroupOverviewPopup?.SetActive(false);
        assemblyGroupOverviewPopup?.SetActive(false);
    }

    // ── Disassembly Popup ─────────────────────────────────────────────────────

    private void BuildDisassemblyPopupRows()
    {
        // Fall back to assembly row parent when no dedicated disassembly parent is wired.
        Transform parent = disassemblyGroupRowParent != null ? disassemblyGroupRowParent : assemblyGroupRowParent;
        if (parent == null || groupRowPrefab == null || disassemblyTaskGroups == null) return;

        foreach (Transform child in parent)
            Destroy(child.gameObject);

        _disassemblyPopupRowTMPs = new TMP_Text[disassemblyTaskGroups.Length][];
        for (int g = 0; g < disassemblyTaskGroups.Length; g++)
        {
            var row  = Instantiate(groupRowPrefab, parent);
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

    private void BuildAssemblyPopupRows()
    {
        // Fall back to disassembly row parent when no dedicated assembly parent is wired.
        Transform parent = assemblyGroupRowParent != null ? assemblyGroupRowParent : disassemblyGroupRowParent;
        if (parent == null || groupRowPrefab == null || assemblyTaskGroups == null || assemblyTaskGroups.Length == 0) return;

        foreach (Transform child in parent)
            Destroy(child.gameObject);

        _assemblyPopupRowTMPs = new TMP_Text[assemblyTaskGroups.Length][];
        for (int g = 0; g < assemblyTaskGroups.Length; g++)
        {
            var row  = Instantiate(groupRowPrefab, parent);
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
        _mbTakenOutForAssembly = false;

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

        BuildAssemblyPopupRows();
        RefreshAssemblyGroupTitle();
        RefreshAssemblyPopup();
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
