/*
 * ================================================================
 *  UNITY SETUP GUIDE — PrintManagementController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Print Management Panel" (starts INACTIVE).
 *    Opened from ServerManagerController Tools → Print Management.
 *
 *  HIERARCHY
 *    Print Management Panel                   ← this script here
 *      ├── TitleBar
 *      │     ├── TitleLabel (TMP_Text) "Print Management"
 *      │     └── CloseBtn (Button)             → closeBtn
 *      ├── MainArea (HorizontalLayoutGroup)
 *      │     ├── TreePanel (fixed width ~220)
 *      │     │     └── TreeScrollView
 *      │     │           └── Viewport
 *      │     │                 └── TreeContent (VLG + ContentSizeFitter)
 *      │     │                       anchor:top-stretch, pivot(0.5,1)
 *      │     │                       ← treeNodePrefab rows spawn here → treeNodeParent
 *      │     └── ContentPanel (flexible)
 *      │           └── ContentScrollView
 *      │                 └── Viewport
 *      │                       └── ContentListContent (VLG + ContentSizeFitter)
 *      │                             anchor:top-stretch, pivot(0.5,1)
 *      │                             ← printContentRowPrefab rows spawn here → contentListParent
 *      ├── PrintContextMenu (Image, starts INACTIVE) → printContextMenu
 *      │     anchor:top-left, pivot(0,1)
 *      │     VerticalLayoutGroup padding:4, ContentSizeFitter Vertical:Preferred
 *      │     └── BtnAddPrinter (Button LE preferredHeight:28) → printContextMenuAddPrinterBtn
 *      │           TMP_Text "Add Printer..."
 *      └── WizardPanel (starts INACTIVE)             → wizardPanel
 *            VerticalLayoutGroup, fills panel
 *            ├── WizardTitleBar
 *            │     └── WizardTitle (TMP_Text) — label changes per step in-code
 *            ├── WizardStepArea (flexible, shows one step panel at a time)
 *            │     ├── WizardStep0 (INACTIVE by default) → wizardStep0
 *            │     │     "Printer Installation" — port method selection
 *            │     │     (no ToggleGroup needed — mutual exclusivity is managed in code)
 *            │     │     ├── Toggle1 (Toggle) "Search the network for printers"     → s1Toggle1
 *            │     │     ├── Toggle2 (Toggle) "Add a TCP/IP or Web Services printer" → s1Toggle2
 *            │     │     ├── Toggle3 (Toggle) "Add a new printer using existing port" → s1Toggle3
 *            │     │     │     └── PortDropdown (TMP_Dropdown)                       → s1PortDropdown
 *            │     │     └── Toggle4 (Toggle) "Create a new port and add a printer"  → s1Toggle4
 *            │     │           └── NewPortDropdown (TMP_Dropdown)                    → s1T4Dropdown
 *            │     ├── WizardStep1 (INACTIVE) → wizardStep1
 *            │     │     "Printer Driver" — driver source selection
 *            │     │     (no ToggleGroup needed — mutual exclusivity is managed in code)
 *            │     │     ├── Toggle1 (Toggle, interactable:OFF)
 *            │     │     │     "Use the printer driver the wizard selected"         → s2Toggle1
 *            │     │     │     └── StatusLabel (TMP_Text) "Compatible driver cannot be found"
 *            │     │     ├── Toggle2 (Toggle) "Use an existing printer driver"      → s2Toggle2
 *            │     │     │     └── ExistingDriverDropdown (TMP_Dropdown)            → s2ExistingDriverDropdown
 *            │     │     └── Toggle3 (Toggle) "Install a new driver"                → s2Toggle3
 *            │     ├── WizardStep2 (INACTIVE) → wizardStep2
 *            │     │     "Install Printer Software" — manufacturer/model selection
 *            │     │     HorizontalLayoutGroup
 *            │     │     ├── TabPanel (VLG, fixed width ~140)
 *            │     │     │     ├── ManufacturerLabel (TMP_Text "Manufacturer" bold) — static
 *            │     │     │     ├── GenericTabBtn (Button)     → s3GenericTabBtn
 *            │     │     │     └── MicrosoftTabBtn (Button)   → s3MicrosoftTabBtn
 *            │     │     └── DriverListPanel (flexible)
 *            │     │           └── DriverScrollView
 *            │     │                 └── Viewport
 *            │     │                       └── DriverListContent (VLG+ContentSizeFitter)
 *            │     │                             anchor:top-stretch, pivot(0.5,1)
 *            │     │                             ← driver buttons spawn here → s3DriverListParent
 *            │     ├── WizardStep3 (INACTIVE) → wizardStep3
 *            │     │     "Printer Name and Sharing Settings"
 *            │     │     ├── PrinterNameInput (TMP_InputField)   → s4PrinterNameInput
 *            │     │     ├── ShareToggle (Toggle)                → s4ShareToggle
 *            │     │     ├── ShareNameInput (TMP_InputField, interactable:OFF)
 *            │     │     ├── LocationInput  (TMP_InputField, interactable:OFF)
 *            │     │     └── CommentInput   (TMP_InputField, interactable:OFF)
 *            │     ├── WizardStep4 (INACTIVE) → wizardStep4
 *            │     │     "Printer Found" — summary
 *            │     │     └── SummaryText (TMP_Text, rich-text)  → s5SummaryText
 *            │     └── WizardStep5 (INACTIVE) → wizardStep5
 *            │           "Completing the Network Printer Installation Wizard"
 *            │           ├── DriverStatusText  (TMP_Text)       → s6DriverStatusText
 *            │           ├── PrinterStatusText (TMP_Text)       → s6PrinterStatusText
 *            │           ├── ResultText        (TMP_Text)       → s6ResultText
 *            │           ├── TestPageToggle    (Toggle, decoy display only)
 *            │           └── AnotherPrinterToggle (Toggle, decoy display only)
 *            └── WizardFooter (HorizontalLayoutGroup, fixed height)
 *                  ├── BackBtn   (Button)  → wizardBackBtn
 *                  ├── NextBtn   (Button)  → wizardNextBtn  (label changes to "Finish" on last step)
 *                  └── CancelBtn (Button)  → wizardCancelBtn
 *
 *  PREFABS NEEDED
 *    treeNodePrefab        — reuse the SAME prefab used by GroupPolicyController (TreeNodeUI)
 *    printContentRowPrefab — new simple row:
 *      Root GO: Image (color:clear, raycastTarget:OFF) + LayoutElement preferredHeight:22
 *      └── TMP_Text (font-size:11, left-middle, overflow:Truncate, stretch-stretch)
 *
 *  INSPECTOR ASSIGNMENTS (on PrintManagementController)
 *    closeBtn                      → CloseBtn
 *    treeNodeParent                → TreeContent Transform
 *    treeNodePrefab                → treeNodePrefab (same asset as GroupPolicyController)
 *    selectedColor                 → (0.2, 0.5, 0.9, 0.35) blue tint
 *    contentListParent             → ContentListContent Transform
 *    printContentRowPrefab         → printContentRowPrefab
 *    printContextMenu              → PrintContextMenu GO
 *    printContextMenuAddPrinterBtn → BtnAddPrinter
 *    wizardPanel                   → WizardPanel GO
 *    wizardStep0…wizardStep5       → WizardStep0…WizardStep5 GOs
 *    wizardBackBtn                 → BackBtn
 *    wizardNextBtn                 → NextBtn
 *    wizardCancelBtn               → CancelBtn
 *    s1Toggle1…s1Toggle4           → Toggle1…Toggle4 in WizardStep0
 *    s1PortDropdown                → PortDropdown (child of Toggle3)
 *    s1T4Dropdown                  → NewPortDropdown (child of Toggle4)
 *    s2Toggle1…s2Toggle3           → Toggle1…Toggle3 in WizardStep1
 *    s2ExistingDriverDropdown      → ExistingDriverDropdown (child of Toggle2)
 *    s3GenericTabBtn               → GenericTabBtn
 *    s3MicrosoftTabBtn             → MicrosoftTabBtn
 *    s3DriverListParent            → DriverListContent Transform
 *    s4PrinterNameInput            → PrinterNameInput
 *    s4ShareToggle                 → ShareToggle
 *    s5SummaryText                 → SummaryText
 *    s6DriverStatusText            → DriverStatusText
 *    s6PrinterStatusText           → PrinterStatusText
 *    s6ResultText                  → ResultText
 *
 *  TASK MANAGER LABEL UPDATES (Inspector — task row TMP_Text GameObjects)
 *    taskObjects[58] text → "Add a printer using the Network Printer Installation Wizard"
 *    taskObjects[59] text → "Finish the wizard to complete printer installation"
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrintManagementController : MonoBehaviour
{
    public static PrintManagementController Instance { get; private set; }

    // ── Serialized fields ─────────────────────────────────────────────────────

    [Header("Nav")]
    [SerializeField] private Button closeBtn;

    [Header("Tree")]
    [SerializeField] private Transform  treeNodeParent;
    [SerializeField] private GameObject treeNodePrefab;
    [SerializeField] private Color      selectedColor = new Color(0.2f, 0.5f, 0.9f, 0.35f);

    [Header("Content Panel")]
    [SerializeField] private Transform  contentListParent;
    [SerializeField] private GameObject printContentRowPrefab;

    [Header("Context Menu")]
    [SerializeField] private GameObject printContextMenu;
    [SerializeField] private Button     printContextMenuAddPrinterBtn;

    [Header("Wizard — Panel")]
    [SerializeField] private GameObject wizardPanel;
    [SerializeField] private GameObject wizardStep0;
    [SerializeField] private GameObject wizardStep1;
    [SerializeField] private GameObject wizardStep2;
    [SerializeField] private GameObject wizardStep3;
    [SerializeField] private GameObject wizardStep4;
    [SerializeField] private GameObject wizardStep5;

    [Header("Wizard — Footer")]
    [SerializeField] private Button wizardBackBtn;
    [SerializeField] private Button wizardNextBtn;
    [SerializeField] private Button wizardCancelBtn;

    [Header("Wizard — Step 1 (Port Selection)")]
    [SerializeField] private Toggle       s1Toggle1;
    [SerializeField] private Toggle       s1Toggle2;
    [SerializeField] private Toggle       s1Toggle3;
    [SerializeField] private Toggle       s1Toggle4;
    [SerializeField] private TMP_Dropdown s1PortDropdown;
    [SerializeField] private TMP_Dropdown s1T4Dropdown;

    [Header("Wizard — Step 2 (Driver Source)")]
    [SerializeField] private Toggle       s2Toggle1;
    [SerializeField] private Toggle       s2Toggle2;
    [SerializeField] private Toggle       s2Toggle3;
    [SerializeField] private TMP_Dropdown s2ExistingDriverDropdown;

    [Header("Wizard — Step 3 (Driver Selection)")]
    [SerializeField] private Button    s3GenericTabBtn;
    [SerializeField] private Button    s3MicrosoftTabBtn;
    [SerializeField] private Transform s3DriverListParent;

    [Header("Wizard — Step 4 (Name & Sharing)")]
    [SerializeField] private TMP_InputField s4PrinterNameInput;
    [SerializeField] private Toggle         s4ShareToggle;

    [Header("Wizard — Step 5 (Summary)")]
    [SerializeField] private TMP_Text s5SummaryText;

    [Header("Wizard — Step 6 (Completion)")]
    [SerializeField] private TMP_Text s6DriverStatusText;
    [SerializeField] private TMP_Text s6PrinterStatusText;
    [SerializeField] private TMP_Text s6ResultText;

    // ── Static data ───────────────────────────────────────────────────────────

    private const float  IndentWidth    = 16f;
    private const string RequiredDriver = "Microsoft Print To PDF";
    private const string RequiredPort   = "LPT1: (Printer Port)";

    private static readonly string[] WizardPortOptions =
    {
        "LPT1: (Printer Port)", "LPT2: (Printer Port)", "LPT3: (Printer Port)",
        "COM1: (Serial Port)",  "COM2: (Serial Port)",  "COM3: (Serial Port)", "COM4: (Serial Port)",
        "FILE: (Print to File)", "PORTPROMPT: (Local Port)", "XPSPort: (Local Port)", "SHRFAX: (Local Port)"
    };

    private static readonly string[] GenericDrivers =
    {
        "Generic / Text Only", "Generic IBM Graphics 9pin", "Generic IBM Graphics 9pin wide",
        "MS Publisher Color Printer", "MS Publisher Imagesetter", "Generic ESC/P",
        "Generic ESC/P 2", "HP LaserJet Series II", "Generic PCL", "Generic PostScript Printer"
    };

    private static readonly string[] MicrosoftDrivers =
    {
        "Microsoft Enterprise Cloud Print Class Driver", "Microsoft IPP Class Driver",
        "Microsoft MS-XPS Class Driver 2", "Microsoft Print To PDF",
        "Microsoft Shared Fax Driver", "Microsoft XPS Document Writer",
        "Microsoft XPS Document Writer v4"
    };

    private static readonly string[] FormNames =
    {
        "Letter", "Legal", "A4", "A3", "Tabloid", "Executive",
        "Envelope #10", "C5 Envelope", "B5 (JIS)", "Folio"
    };

    private static readonly string[] ContentPortNames =
    {
        "LPT1:", "LPT2:", "LPT3:",
        "COM1:", "COM2:", "COM3:", "COM4:",
        "FILE:", "PORTPROMPT:", "XPSPort:", "SHRFAX:"
    };

    // ── Private state ─────────────────────────────────────────────────────────

    private readonly Dictionary<string, bool> _expanded = new Dictionary<string, bool>();
    private string _selectedNodeId = "ROOT";

    private int    _wizardStep          = 0;
    private int    _step1Toggle         = -1; // 0-3 map to Toggle1-4
    private int    _step2Toggle         = -1;
    private int    _step3Tab            = -1; // 0=Generic, 1=Microsoft
    private string _step3SelectedDriver = "";
    private string _step4PrinterName    = "";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        closeBtn?.onClick.AddListener(Close);
        printContextMenuAddPrinterBtn?.onClick.AddListener(() => { HideContextMenu(); OpenWizard(); });

        // Step 1 toggle listeners — mutual exclusivity handled manually
        s1Toggle1?.onValueChanged.AddListener(on => { if (on) { _step1Toggle = 0; DeactivateStep1Others(s1Toggle1); UpdateStep1Dropdowns(); } });
        s1Toggle2?.onValueChanged.AddListener(on => { if (on) { _step1Toggle = 1; DeactivateStep1Others(s1Toggle2); UpdateStep1Dropdowns(); } });
        s1Toggle3?.onValueChanged.AddListener(on => { if (on) { _step1Toggle = 2; DeactivateStep1Others(s1Toggle3); UpdateStep1Dropdowns(); } });
        s1Toggle4?.onValueChanged.AddListener(on => { if (on) { _step1Toggle = 3; DeactivateStep1Others(s1Toggle4); UpdateStep1Dropdowns(); } });

        // Step 2 toggle listeners — mutual exclusivity handled manually
        s2Toggle1?.onValueChanged.AddListener(on => { if (on) { _step2Toggle = 0; DeactivateStep2Others(s2Toggle1); UpdateStep2Dropdown(); } });
        s2Toggle2?.onValueChanged.AddListener(on => { if (on) { _step2Toggle = 1; DeactivateStep2Others(s2Toggle2); UpdateStep2Dropdown(); } });
        s2Toggle3?.onValueChanged.AddListener(on => { if (on) { _step2Toggle = 2; DeactivateStep2Others(s2Toggle3); UpdateStep2Dropdown(); } });

        // Step 2 Toggle1 is always non-interactable (no driver auto-detected)
        if (s2Toggle1 != null) s2Toggle1.interactable = false;

        // Step 3 tab buttons
        s3GenericTabBtn  ?.onClick.AddListener(() => OnStep3TabClick(0));
        s3MicrosoftTabBtn?.onClick.AddListener(() => OnStep3TabClick(1));

        // Wizard footer
        wizardBackBtn  ?.onClick.AddListener(OnWizardBack);
        wizardNextBtn  ?.onClick.AddListener(OnWizardNext);
        wizardCancelBtn?.onClick.AddListener(OnWizardCancel);

        printContextMenu?.SetActive(false);
        wizardPanel     ?.SetActive(false);
        gameObject.SetActive(false);
    }

    private void Start()
    {
        // Populate step 1 port dropdowns
        var portList = new List<string>(WizardPortOptions);
        if (s1PortDropdown != null)
        {
            s1PortDropdown.ClearOptions();
            s1PortDropdown.AddOptions(portList);
            s1PortDropdown.value        = 0;
            s1PortDropdown.interactable = false;
        }
        if (s1T4Dropdown != null)
        {
            s1T4Dropdown.ClearOptions();
            s1T4Dropdown.AddOptions(portList);
            s1T4Dropdown.value        = 0;
            s1T4Dropdown.interactable = false;
        }

        // Populate step 2 existing-driver dropdown with placeholder
        if (s2ExistingDriverDropdown != null)
        {
            s2ExistingDriverDropdown.ClearOptions();
            s2ExistingDriverDropdown.AddOptions(new List<string> { "(No drivers available)" });
            s2ExistingDriverDropdown.interactable = false;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.PrintMgmtOpened = true;

        HideContextMenu();
        wizardPanel?.SetActive(false);

        _selectedNodeId = "ROOT";
        RebuildTree();
        RefreshContentPanel();

        ActivityLogManager.Log("Opened Print Management", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        HideContextMenu();
        wizardPanel?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Tree ──────────────────────────────────────────────────────────────────

    private void RebuildTree()
    {
        foreach (Transform t in treeNodeParent) Destroy(t.gameObject);

        bool rootExp = GetExpanded("ROOT");
        SpawnNode("ROOT", "Print Management", 0, isLeaf: false, isExpanded: rootExp,
            isSelected: _selectedNodeId == "ROOT", leftClick: true, rightClick: false);

        if (!rootExp) { RebuildTreeLayout(); return; }

        // Custom Filters
        bool cfExp = GetExpanded("CF");
        SpawnNode("CF", "Custom Filters", 1, isLeaf: false, isExpanded: cfExp,
            isSelected: _selectedNodeId == "CF", leftClick: true, rightClick: false);

        if (cfExp)
        {
            SpawnLeaf("CF_ALLPRINTERS", "All Printers",       2);
            SpawnLeaf("CF_ALLDRIVERS",  "All Drivers",        2);
            SpawnLeaf("CF_NOTREADY",    "Printers Not Ready", 2);
            SpawnLeaf("CF_WITHJOBS",    "Printers With Jobs", 2);
        }

        // Print Servers
        bool psExp = GetExpanded("PS");
        SpawnNode("PS", "Print Servers", 1, isLeaf: false, isExpanded: psExp,
            isSelected: _selectedNodeId == "PS", leftClick: true, rightClick: false);

        if (psExp)
        {
            bool srvExp = GetExpanded("PS_SERVER");
            SpawnNode("PS_SERVER", "SERVER (local)", 2, isLeaf: false, isExpanded: srvExp,
                isSelected: _selectedNodeId == "PS_SERVER", leftClick: true, rightClick: true);

            if (srvExp)
            {
                SpawnLeaf("PS_DRIVERS",  "Drivers",  3);
                SpawnLeaf("PS_FORMS",    "Forms",    3);
                SpawnLeaf("PS_PORTS",    "Ports",    3);
                SpawnLeaf("PS_PRINTERS", "Printers", 3);
            }
        }

        // Deployed Printers (leaf — no children)
        SpawnLeaf("DP", "Deployed Printers", 1);

        RebuildTreeLayout();
    }

    private void SpawnLeaf(string id, string label, int depth)
    {
        SpawnNode(id, label, depth, isLeaf: true, isExpanded: false,
            isSelected: _selectedNodeId == id, leftClick: true, rightClick: false);
    }

    private void SpawnNode(string id, string label, int depth,
                           bool isLeaf, bool isExpanded, bool isSelected,
                           bool leftClick, bool rightClick)
    {
        var go = Instantiate(treeNodePrefab, treeNodeParent);
        var ui = go.GetComponent<TreeNodeUI>();
        if (ui == null) { Debug.LogError("[PrintMgmt] treeNodePrefab missing TreeNodeUI."); return; }

        ui.indentSpacer.preferredWidth = depth * IndentWidth;
        ui.arrowLabel.gameObject.SetActive(!isLeaf);
        if (!isLeaf) ui.arrowLabel.text = isExpanded ? "▼" : "▶";
        ui.nodeLabel.text   = label;
        ui.background.color = isSelected ? selectedColor : Color.clear;

        if (!leftClick && !rightClick) return;

        var    handler    = go.AddComponent<NodeClickHandler>();
        string capturedId = id;

        if (leftClick)
            handler.onLeftClick = () => OnNodeLeftClick(capturedId);
        if (rightClick)
            handler.onRightClick = () => OnNodeRightClick(capturedId);
    }

    // ── Tree interaction ──────────────────────────────────────────────────────

    private void OnNodeLeftClick(string id)
    {
        HideContextMenu();

        bool isParent = id == "ROOT" || id == "CF" || id == "PS" || id == "PS_SERVER";
        if (isParent)
            _expanded[id] = !GetExpanded(id);

        _selectedNodeId = id;
        RebuildTree();
        RefreshContentPanel();
    }

    private void OnNodeRightClick(string id)
    {
        if (id != "PS_SERVER" || printContextMenu == null) return;
        printContextMenu.SetActive(true);
    }

    // ── Context menu ──────────────────────────────────────────────────────────

    private void HideContextMenu() => printContextMenu?.SetActive(false);

    // ── Content panel ─────────────────────────────────────────────────────────

    private void RefreshContentPanel()
    {
        if (contentListParent == null) return;
        foreach (Transform t in contentListParent) Destroy(t.gameObject);

        var  state            = ServerVirtualOSManager.Instance?.ServerState;
        bool printerInstalled = state != null && state.PrinterShared;

        switch (_selectedNodeId)
        {
            case "ROOT":
                SpawnContentRow("Custom Filters");
                SpawnContentRow("Print Servers");
                SpawnContentRow("Deployed Printers");
                break;

            case "CF":
                SpawnContentRow("All Printers");
                SpawnContentRow("All Drivers");
                SpawnContentRow("Printers Not Ready");
                SpawnContentRow("Printers With Jobs");
                break;

            case "CF_ALLPRINTERS":
            case "PS_PRINTERS":
                if (printerInstalled)
                    SpawnContentRow(state.InstalledPrinterName);
                break;

            case "CF_ALLDRIVERS":
            case "PS_DRIVERS":
                if (printerInstalled)
                    SpawnContentRow(state.InstalledDriverName);
                break;

            case "PS":
                SpawnContentRow("SERVER (local)");
                break;

            case "PS_SERVER":
                SpawnContentRow("Drivers");
                SpawnContentRow("Forms");
                SpawnContentRow("Ports");
                SpawnContentRow("Printers");
                break;

            case "PS_FORMS":
                foreach (string f in FormNames) SpawnContentRow(f);
                break;

            case "PS_PORTS":
                foreach (string p in ContentPortNames) SpawnContentRow(p);
                break;

            // CF_NOTREADY, CF_WITHJOBS, DP: intentionally empty
        }

        RebuildContentLayout();
    }

    private void SpawnContentRow(string label)
    {
        var go  = Instantiate(printContentRowPrefab, contentListParent);
        var tmp = go.GetComponentInChildren<TMP_Text>();
        if (tmp != null) tmp.text = label;
    }

    // ── Wizard — Open / Close ─────────────────────────────────────────────────

    private void OpenWizard()
    {
        // Reset all wizard state
        _wizardStep          = 0;
        _step1Toggle         = -1;
        _step2Toggle         = -1;
        _step3Tab            = -1;
        _step3SelectedDriver = "";
        _step4PrinterName    = "";

        foreach (var t in new[] { s1Toggle1, s1Toggle2, s1Toggle3, s1Toggle4 })
            if (t != null) t.isOn = false;
        foreach (var t in new[] { s2Toggle1, s2Toggle2, s2Toggle3 })
            if (t != null) t.isOn = false;

        if (s1PortDropdown != null) { s1PortDropdown.value = 0; s1PortDropdown.interactable = false; }
        if (s1T4Dropdown   != null) { s1T4Dropdown.value   = 0; s1T4Dropdown.interactable   = false; }
        if (s2ExistingDriverDropdown != null) s2ExistingDriverDropdown.interactable = false;

        if (s3DriverListParent != null)
            foreach (Transform t in s3DriverListParent) Destroy(t.gameObject);

        if (s4PrinterNameInput != null) s4PrinterNameInput.text = "";
        if (s4ShareToggle      != null) s4ShareToggle.isOn      = false;

        wizardPanel?.SetActive(true);
        ShowWizardStep(0);

        ActivityLogManager.Log("Opened Network Printer Installation Wizard", ActivityLogManager.EntryType.Action);
    }

    private void CloseWizard() => wizardPanel?.SetActive(false);

    // ── Wizard — Step display ─────────────────────────────────────────────────

    private void ShowWizardStep(int step)
    {
        _wizardStep = step;

        wizardStep0?.SetActive(step == 0);
        wizardStep1?.SetActive(step == 1);
        wizardStep2?.SetActive(step == 2);
        wizardStep3?.SetActive(step == 3);
        wizardStep4?.SetActive(step == 4);
        wizardStep5?.SetActive(step == 5);

        bool isFirst = step == 0;
        bool isLast  = step == 5;

        if (wizardBackBtn   != null) wizardBackBtn.interactable  = !isFirst && !isLast;
        if (wizardCancelBtn != null) wizardCancelBtn.gameObject.SetActive(!isLast);

        var nextLabel = wizardNextBtn?.GetComponentInChildren<TMP_Text>();
        if (nextLabel != null) nextLabel.text = isLast ? "Finish" : "Next >";

        // Re-apply interactable states when revisiting steps
        if (step == 0) UpdateStep1Dropdowns();
        if (step == 1) UpdateStep2Dropdown();

        // Populate read-only steps
        if (step == 4) PopulateSummary();
        if (step == 5) PopulateCompletion();
    }

    // ── Wizard — Step 1 logic ─────────────────────────────────────────────────

    private void UpdateStep1Dropdowns()
    {
        if (s1PortDropdown != null) s1PortDropdown.interactable = _step1Toggle == 2;
        if (s1T4Dropdown   != null) s1T4Dropdown.interactable   = _step1Toggle == 3;
    }

    // ── Wizard — Step 2 logic ─────────────────────────────────────────────────

    private void UpdateStep2Dropdown()
    {
        if (s2ExistingDriverDropdown != null)
            s2ExistingDriverDropdown.interactable = _step2Toggle == 1;
    }

    // ── Wizard — Toggle mutual exclusivity ────────────────────────────────────

    private void DeactivateStep1Others(Toggle active)
    {
        if (s1Toggle1 != null && s1Toggle1 != active) s1Toggle1.isOn = false;
        if (s1Toggle2 != null && s1Toggle2 != active) s1Toggle2.isOn = false;
        if (s1Toggle3 != null && s1Toggle3 != active) s1Toggle3.isOn = false;
        if (s1Toggle4 != null && s1Toggle4 != active) s1Toggle4.isOn = false;
    }

    private void DeactivateStep2Others(Toggle active)
    {
        if (s2Toggle1 != null && s2Toggle1 != active) s2Toggle1.isOn = false;
        if (s2Toggle2 != null && s2Toggle2 != active) s2Toggle2.isOn = false;
        if (s2Toggle3 != null && s2Toggle3 != active) s2Toggle3.isOn = false;
    }

    // ── Wizard — Step 3 logic ─────────────────────────────────────────────────

    private void OnStep3TabClick(int tab)
    {
        _step3Tab            = tab;
        _step3SelectedDriver = "";
        RefreshDriverList();
    }

    private void RefreshDriverList()
    {
        if (s3DriverListParent == null) return;
        foreach (Transform t in s3DriverListParent) Destroy(t.gameObject);

        string[] drivers = _step3Tab == 0 ? GenericDrivers
                         : _step3Tab == 1 ? MicrosoftDrivers
                         : System.Array.Empty<string>();

        foreach (string d in drivers)
            SpawnDriverButton(d);

        if (s3DriverListParent is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private void SpawnDriverButton(string driverName)
    {
        var go  = Instantiate(printContentRowPrefab, s3DriverListParent);
        var tmp = go.GetComponentInChildren<TMP_Text>();
        var img = go.GetComponent<Image>();

        if (tmp != null) tmp.text = driverName;
        if (img != null) img.color = Color.clear;

        // Make the row raycast-able
        if (img != null) img.raycastTarget = true;

        string capturedName = driverName;
        var handler = go.AddComponent<NodeClickHandler>();
        handler.onLeftClick = () =>
        {
            _step3SelectedDriver = capturedName;

            foreach (Transform t in s3DriverListParent)
            {
                var bg = t.GetComponent<Image>();
                if (bg != null) bg.color = Color.clear;
            }
            if (img != null) img.color = selectedColor;
        };
    }

    // ── Wizard — Step 5 (summary) ─────────────────────────────────────────────

    private void PopulateSummary()
    {
        if (s5SummaryText == null) return;
        s5SummaryText.text =
            $"Name:        {_step4PrinterName}\n" +
            $"Share name:  (not shared)\n" +
            $"Model:       {RequiredDriver}\n" +
            $"Port type:   Local\n" +
            $"Port name:   LPT1:\n" +
            $"Location:    -\n" +
            $"Publish:     No\n" +
            $"Comment:     -";
    }

    // ── Wizard — Step 6 (completion) ─────────────────────────────────────────

    private void PopulateCompletion()
    {
        if (s6DriverStatusText  != null) s6DriverStatusText.text  = "Driver installation succeeded.";
        if (s6PrinterStatusText != null) s6PrinterStatusText.text = "Printer installation succeeded.";
        if (s6ResultText        != null) s6ResultText.text        = "Your printer has been installed successfully.";
    }

    // ── Wizard — Navigation ───────────────────────────────────────────────────

    private void OnWizardNext()
    {
        if (_wizardStep == 5) { OnWizardFinish(); return; }
        if (!CanAdvance()) return;

        int nextStep = _wizardStep + 1;

        // Capture step 4 data before leaving
        if (_wizardStep == 3)
            _step4PrinterName = s4PrinterNameInput != null ? s4PrinterNameInput.text.Trim() : "";

        ShowWizardStep(nextStep);

        // Task 58 milestone: write PrinterDriverAdded when summary (step index 4) renders
        if (nextStep == 4)
        {
            var state = ServerVirtualOSManager.Instance?.ServerState;
            if (state != null) state.PrinterDriverAdded = true;
        }
    }

    private void OnWizardBack()
    {
        if (_wizardStep <= 0 || _wizardStep == 5) return;
        ShowWizardStep(_wizardStep - 1);
    }

    private void OnWizardCancel()
    {
        CloseWizard();
    }

    private void OnWizardFinish()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null)
        {
            state.PrinterShared        = true;
            state.PrinterShareName     = "";
            state.InstalledPrinterName = _step4PrinterName;
            state.InstalledDriverName  = RequiredDriver;
            state.InstalledPortName    = "LPT1:";
        }

        ActivityLogManager.Log(
            $"Printer installed: {_step4PrinterName} (Driver: {RequiredDriver}, Port: LPT1:)",
            ActivityLogManager.EntryType.Action);

        CloseWizard();

        // Auto-navigate to Printers node to show the newly added printer
        _expanded["PS"]        = true;
        _expanded["PS_SERVER"] = true;
        _selectedNodeId        = "PS_PRINTERS";
        RebuildTree();
        RefreshContentPanel();
    }

    // ── Wizard — Validation ───────────────────────────────────────────────────

    private bool CanAdvance()
    {
        return _wizardStep switch
        {
            0 => s1Toggle3 != null && s1Toggle3.isOn
                 && s1PortDropdown != null && s1PortDropdown.value == 0,
            1 => s2Toggle3 != null && s2Toggle3.isOn,
            2 => _step3Tab == 1 && _step3SelectedDriver == RequiredDriver,
            3 => s4PrinterNameInput != null
                 && !string.IsNullOrWhiteSpace(s4PrinterNameInput.text),
            4 => true,
            _ => false
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool GetExpanded(string id)
    {
        _expanded.TryGetValue(id, out bool val);
        return val;
    }

    private void RebuildTreeLayout()
    {
        if (treeNodeParent is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private void RebuildContentLayout()
    {
        if (contentListParent is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }
}
