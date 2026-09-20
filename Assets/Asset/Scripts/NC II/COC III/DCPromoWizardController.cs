/*
 * ================================================================
 *  UNITY SETUP GUIDE — DCPromoWizardController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "DCPromo Wizard Panel" (start ACTIVE so Awake fires;
 *    the script deactivates itself). Child of the server OS canvas.
 *
 *  HIERARCHY
 *    DCPromo Wizard Panel                        ← this script here
 *      │
 *      ├── Left Tracker Panel
 *      │     ├── TrackerEntry_0  (always active)     → trackerEntries[0]
 *      │     │     └── TMP "Deployment Configuration"    → trackerLabels[0]
 *      │     ├── TrackerEntry_1  (always active)     → trackerEntries[1]
 *      │     │     └── TMP "Domain Controller Options"   → trackerLabels[1]
 *      │     ├── TrackerEntry_2  (STARTS INACTIVE)   → trackerEntries[2]
 *      │     │     └── TMP "DNS Options"                 → trackerLabels[2]
 *      │     │     (activates/deactivates with DNS Server toggle in Step 1)
 *      │     ├── TrackerEntry_3  (always active)     → trackerEntries[3]
 *      │     │     └── TMP "Additional Options"          → trackerLabels[3]
 *      │     ├── TrackerEntry_4  (always active)     → trackerEntries[4]
 *      │     │     └── TMP "Paths"                       → trackerLabels[4]
 *      │     ├── TrackerEntry_5  (always active)     → trackerEntries[5]
 *      │     │     └── TMP "Review Options"              → trackerLabels[5]
 *      │     └── TrackerEntry_6  (always active)     → trackerEntries[6]
 *      │           └── TMP "Prerequisites Check"         → trackerLabels[6]
 *      │
 *      ├── Step — Deployment Config              → deploymentConfigPanel
 *      │     ├── AddDCToggle                     → addDCToggle
 *      │     │     Label: "Add a domain controller to an existing domain"
 *      │     ├── AddDomainToggle                 → addDomainToggle
 *      │     │     Label: "Add a new domain to an existing forest"
 *      │     ├── AddForestToggle                 → addForestToggle   (default ON)
 *      │     │     Label: "Add a new forest"
 *      │     ├── AddDCSub                        → addDCSub          (shown when AddDC selected)
 *      │     │     └── ExistingDomainInput       → existingDomainInput
 *      │     │           Placeholder: "Domain name"
 *      │     ├── AddDomainSub                    → addDomainSub      (shown when AddDomain selected)
 *      │     │     └── TMP (static notice)
 *      │     │           Text: "Adding a new domain to an existing forest is not applicable in this task.
 *      │     │                  Select 'Add a new forest' to continue."
 *      │     └── AddForestSub                    → addForestSub      (shown when AddForest selected; default)
 *      │           └── RootDomainInput           → rootDomainInput
 *      │                 Placeholder: "Root domain name (e.g. test.edu.ph)"
 *      │
 *      ├── Step — DC Options                     → dcOptionsPanel
 *      │     ├── TMP "Forest functional level:"  (static label)
 *      │     ├── ForestLevelDropdown             → forestLevelDropdown
 *      │     ├── TMP "Domain functional level:"  (static label)
 *      │     ├── DomainLevelDropdown             → domainLevelDropdown
 *      │     │     Both dropdowns share functionalLevelOptions[]; default = last item (WS 2016)
 *      │     ├── DNSToggle                       → dnsToggle          (default ON)
 *      │     │     Label: "Domain Name System (DNS) server"
 *      │     ├── GCToggle                        → gcToggle           (default ON, interactable=false)
 *      │     │     Label: "Global Catalog (GC)"
 *      │     ├── RODCToggle                      → rodcToggle         (default OFF, interactable=false)
 *      │     │     Label: "Read only domain controller (RODC)"
 *      │     ├── TMP "Type the Directory Services Restore Mode (DSRM) password:"  (static)
 *      │     ├── DSRMPasswordInput               → dsrmPasswordInput
 *      │     │     Content Type: Password (set in Inspector)
 *      │     │     Placeholder: "Password"
 *      │     └── DSRMConfirmInput                → dsrmConfirmInput
 *      │           Content Type: Password (set in Inspector)
 *      │           Placeholder: "Confirm password"
 *      │
 *      ├── Step — DNS Options                    → dnsOptionsPanel
 *      │     ├── DelegationNoticeTMP             → delegationNoticeTMP   (static, set at runtime)
 *      │     └── DNSDelegationToggle             → dnsDelegationToggle
 *      │           Label: "Create a DNS delegation"
 *      │           Default: OFF, interactable = false
 *      │
 *      ├── Step — Additional Options             → additionalOptionsPanel
 *      │     ├── TMP "NetBIOS domain name:"      (static label)
 *      │     └── NetBIOSInput                    → netBIOSInput
 *      │           interactable = false (read-only; auto-filled at runtime)
 *      │
 *      ├── Step — Paths                          → pathsPanel
 *      │     ├── TMP "Database folder:"          (static label)
 *      │     ├── DatabasePathInput               → databasePathInput   (interactable=false)
 *      │     ├── TMP "Log files folder:"         (static label)
 *      │     ├── LogPathInput                    → logPathInput        (interactable=false)
 *      │     ├── TMP "SYSVOL folder:"            (static label)
 *      │     └── SysvolPathInput                 → sysvolPathInput     (interactable=false)
 *      │
 *      ├── Step — Review Options                 → reviewOptionsPanel
 *      │     ├── ReviewTMP                       → reviewTMP
 *      │     ├── ViewScriptBtn                   → viewScriptBtn
 *      │     │     Label: "View script"
 *      │     └── Script Popup                   → scriptPopup          (STARTS INACTIVE)
 *      │           ├── ScriptTMP                 → scriptTMP
 *      │           └── CloseScriptBtn            → closeScriptBtn
 *      │                 Label: "Close"
 *      │
 *      ├── Step — Prerequisites Check            → prerequisitesPanel
 *      │     ├── OperationNoticeTMP              → operationNoticeTMP   (filled at runtime)
 *      │     ├── VerifyingStatusTMP              → verifyingStatusTMP   (filled at runtime)
 *      │     ├── ResultsTMP                      → resultsTMP           (STARTS INACTIVE)
 *      │     └── RebootWarningTMP               → rebootWarningTMP     (STARTS INACTIVE)
 *      │
 *      ├── Step — Install Progress               → installProgressPanel
 *      │     ├── ProgressStatusTMP               → progressStatusTMP
 *      │     └── ProgressBar (Slider)            → progressBar
 *      │           interactable = false; Min=0, Max=1
 *      │
 *      ├── Step — Reboot                         → rebootPanel
 *      │     └── TMP "Restarting..."             (static)
 *      │
 *      ├── PrevBtn                               → prevBtn    (persistent, outside all panels)
 *      ├── NextBtn                               → nextBtn    (label → "Install" on Prerequisites step)
 *      └── CancelBtn                             → cancelBtn  (persistent)
 *
 *  INSPECTOR SETTINGS
 *    functionalLevelOptions  — populate both dropdowns (same array):
 *      {"Windows Server 2008 R2","Windows Server 2012","Windows Server 2012 R2","Windows Server 2016"}
 *    expectedDomainName      — if non-empty, validates root domain input against this (case-insensitive).
 *                              Leave blank to skip validation.
 *    prerequisiteDelay       — seconds before prerequisites results appear (default 10)
 *    installProgressDuration — total seconds for installation progress animation (default 8)
 *    rebootDuration          — seconds to show reboot screen before closing (default 3)
 *
 *  TRACKER BEHAVIOR
 *    trackerEntries[2] (DNS Options) STARTS INACTIVE in the scene.
 *    It shows/hides in real-time as the DNS Server toggle in Step 1 is toggled.
 *    Steps InstallProgress and Reboot have no tracker highlight (index -1).
 *
 *  STEP FLOW
 *    DeploymentConfig → DCOptions
 *      if DNS ON  → DNSOptions → AdditionalOptions
 *      if DNS OFF → AdditionalOptions  (DNS Options skipped entirely)
 *    AdditionalOptions → Paths → ReviewOptions → PrerequisitesCheck
 *    → [Install clicked] → InstallProgress → Reboot → (wizard closes, state committed)
 *
 *  STATE WRITES
 *    All ServerDeviceState fields are written only in FinishPromotion(),
 *    after the reboot timer completes. Nothing is written mid-wizard.
 * ================================================================
 */

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DCPromoWizardController : MonoBehaviour
{
    public static DCPromoWizardController Instance { get; private set; }

    private enum DCPromoStep
    {
        DeploymentConfig,
        DCOptions,
        DNSOptions,
        AdditionalOptions,
        Paths,
        ReviewOptions,
        PrerequisitesCheck,
        InstallProgress,
        Reboot
    }

    // ── Tracker ───────────────────────────────────────────────────────────────

    [Header("Step Tracker")]
    [SerializeField] private GameObject[] trackerEntries = new GameObject[7];
    [SerializeField] private TMP_Text[]   trackerLabels  = new TMP_Text[7];
    [SerializeField] private Color trackerActiveColor   = Color.white;
    [SerializeField] private Color trackerInactiveColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    // ── Panels ────────────────────────────────────────────────────────────────

    [Header("Panels")]
    [SerializeField] private GameObject deploymentConfigPanel;
    [SerializeField] private GameObject dcOptionsPanel;
    [SerializeField] private GameObject dnsOptionsPanel;
    [SerializeField] private GameObject additionalOptionsPanel;
    [SerializeField] private GameObject pathsPanel;
    [SerializeField] private GameObject reviewOptionsPanel;
    [SerializeField] private GameObject prerequisitesPanel;
    [SerializeField] private GameObject installProgressPanel;
    [SerializeField] private GameObject rebootPanel;

    // ── Step 0 — Deployment Config ────────────────────────────────────────────

    [Header("Step 0 — Deployment Config")]
    [SerializeField] private Toggle         addDCToggle;
    [SerializeField] private Toggle         addDomainToggle;
    [SerializeField] private Toggle         addForestToggle;
    [SerializeField] private GameObject     addDCSub;
    [SerializeField] private GameObject     addDomainSub;
    [SerializeField] private GameObject     addForestSub;
    [SerializeField] private TMP_InputField existingDomainInput;
    [SerializeField] private TMP_InputField rootDomainInput;
    [Tooltip("If non-empty, root domain input is validated against this value (case-insensitive). Leave blank to skip.")]
    [SerializeField] private string         expectedDomainName = "";

    // ── Step 1 — DC Options ───────────────────────────────────────────────────

    [Header("Step 1 — DC Options")]
    [SerializeField] private TMP_Dropdown   forestLevelDropdown;
    [SerializeField] private TMP_Dropdown   domainLevelDropdown;
    [Tooltip("Options shown in both functional level dropdowns.")]
    [SerializeField] private string[]       functionalLevelOptions =
    {
        "Windows Server 2008 R2",
        "Windows Server 2012",
        "Windows Server 2012 R2",
        "Windows Server 2016"
    };
    [SerializeField] private Toggle         dnsToggle;
    [SerializeField] private Toggle         gcToggle;
    [SerializeField] private Toggle         rodcToggle;
    [SerializeField] private TMP_InputField dsrmPasswordInput;
    [SerializeField] private TMP_InputField dsrmConfirmInput;

    // ── Step 1.5 — DNS Options ────────────────────────────────────────────────

    [Header("Step 1.5 — DNS Options")]
    [SerializeField] private TMP_Text delegationNoticeTMP;
    [SerializeField] private Toggle   dnsDelegationToggle;

    // ── Step 2 — Additional Options ───────────────────────────────────────────

    [Header("Step 2 — Additional Options")]
    [SerializeField] private TMP_InputField netBIOSInput;

    // ── Step 3 — Paths ────────────────────────────────────────────────────────

    [Header("Step 3 — Paths")]
    [SerializeField] private TMP_InputField databasePathInput;
    [SerializeField] private TMP_InputField logPathInput;
    [SerializeField] private TMP_InputField sysvolPathInput;

    // ── Step 4 — Review Options ───────────────────────────────────────────────

    [Header("Step 4 — Review Options")]
    [SerializeField] private TMP_Text   reviewTMP;
    [SerializeField] private Button     viewScriptBtn;
    [SerializeField] private GameObject scriptPopup;
    [SerializeField] private TMP_Text   scriptTMP;
    [SerializeField] private Button     closeScriptBtn;

    // ── Step 5 — Prerequisites Check ─────────────────────────────────────────

    [Header("Step 5 — Prerequisites Check")]
    [SerializeField] private TMP_Text operationNoticeTMP;
    [SerializeField] private TMP_Text verifyingStatusTMP;
    [SerializeField] private TMP_Text resultsTMP;
    [SerializeField] private TMP_Text rebootWarningTMP;
    [Tooltip("Seconds to wait before showing prerequisite results.")]
    [SerializeField] private float    prerequisiteDelay = 10f;

    // ── Step 6 — Install Progress ─────────────────────────────────────────────

    [Header("Step 6 — Install Progress")]
    [SerializeField] private TMP_Text progressStatusTMP;
    [SerializeField] private Slider   progressBar;
    [Tooltip("Total seconds for the installation progress animation.")]
    [SerializeField] private float    installProgressDuration = 8f;

    // ── Step 7 — Reboot ───────────────────────────────────────────────────────

    [Header("Step 7 — Reboot")]
    [Tooltip("Seconds to show reboot screen before handing off to RestartUIController. " +
             "Set to 0 to skip the DCPromo reboot panel entirely (RestartUIController handles the visual).")]
    [SerializeField] private float rebootDuration = 0f;

    // ── Persistent Navigation ─────────────────────────────────────────────────

    [Header("Persistent Navigation")]
    [SerializeField] private Button prevBtn;
    [SerializeField] private Button nextBtn;
    [SerializeField] private Button cancelBtn;

    // ── Runtime State ─────────────────────────────────────────────────────────

    private DCPromoStep _currentStep;
    private bool        _dnsInstallSelected    = true;
    private bool        _prerequisitesComplete = false;
    private string      _enteredDomainName     = "";
    private string      _computedNetBIOS       = "";
    private TMP_Text    _nextBtnLabel;

    private static readonly string[] InstallProgressMessages =
    {
        "Configuring Active Directory Domain Services...",
        "Creating domain partition...",
        "Installing DNS Server...",
        "Replicating SYSVOL...",
        "Completing domain controller promotion..."
    };

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _nextBtnLabel = nextBtn?.GetComponentInChildren<TMP_Text>();

        prevBtn?.onClick.AddListener(GoBack);
        nextBtn?.onClick.AddListener(GoNext);
        cancelBtn?.onClick.AddListener(CloseWizard);

        viewScriptBtn?.onClick.AddListener(() => scriptPopup?.SetActive(true));
        closeScriptBtn?.onClick.AddListener(() => scriptPopup?.SetActive(false));

        WireDeploymentToggles();

        dnsToggle?.onValueChanged.AddListener(on =>
        {
            if (trackerEntries.Length > 2) trackerEntries[2]?.SetActive(on);
        });

        gameObject.SetActive(false);
    }

    private void Start()
    {
        PopulateDropdown(forestLevelDropdown, functionalLevelOptions);
        PopulateDropdown(domainLevelDropdown, functionalLevelOptions);

        if (gcToggle              != null) { gcToggle.isOn   = true;  gcToggle.interactable   = false; }
        if (rodcToggle            != null) { rodcToggle.isOn  = false; rodcToggle.interactable = false; }
        if (dnsDelegationToggle   != null) { dnsDelegationToggle.isOn = false; dnsDelegationToggle.interactable = false; }
        if (netBIOSInput          != null) netBIOSInput.interactable      = false;
        if (databasePathInput     != null) databasePathInput.interactable = false;
        if (logPathInput          != null) logPathInput.interactable      = false;
        if (sysvolPathInput       != null) sysvolPathInput.interactable   = false;

        if (delegationNoticeTMP != null)
            delegationNoticeTMP.text =
                "A delegation for this DNS server cannot be created because the authoritative " +
                "parent zone cannot be found or it does not run Windows DNS server.\n\n" +
                "If you are integrating with an existing DNS infrastructure, you should manually " +
                "create a delegation to this DNS server in the parent zone to ensure reliable name " +
                "resolution from outside the domain. Otherwise, no action is required.";
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        StopAllCoroutines();

        _dnsInstallSelected    = true;
        _prerequisitesComplete = false;
        _enteredDomainName     = "";
        _computedNetBIOS       = "";

        // Deployment config defaults — forest is the correct path for this task
        addForestToggle?.SetIsOnWithoutNotify(true);
        addDCToggle?.SetIsOnWithoutNotify(false);
        addDomainToggle?.SetIsOnWithoutNotify(false);
        RefreshDeploymentSubPanels();

        // DC options defaults
        if (forestLevelDropdown != null) forestLevelDropdown.value = forestLevelDropdown.options.Count - 1;
        if (domainLevelDropdown != null) domainLevelDropdown.value = domainLevelDropdown.options.Count - 1;
        if (dnsToggle           != null) dnsToggle.isOn            = true;
        if (gcToggle            != null) { gcToggle.isOn   = true;  gcToggle.interactable   = false; }
        if (rodcToggle          != null) { rodcToggle.isOn  = false; rodcToggle.interactable = false; }
        if (dsrmPasswordInput   != null) dsrmPasswordInput.text    = "";
        if (dsrmConfirmInput    != null) dsrmConfirmInput.text      = "";

        // DNS options default
        if (dnsDelegationToggle != null) { dnsDelegationToggle.isOn = false; dnsDelegationToggle.interactable = false; }

        // Clear user inputs
        if (existingDomainInput != null) existingDomainInput.text = "";
        if (rootDomainInput     != null) rootDomainInput.text     = "";
        if (netBIOSInput        != null) netBIOSInput.text        = "";
        if (databasePathInput   != null) databasePathInput.text   = "";
        if (logPathInput        != null) logPathInput.text        = "";
        if (sysvolPathInput     != null) sysvolPathInput.text     = "";

        // Reset prerequisites result panels
        resultsTMP?.gameObject.SetActive(false);
        rebootWarningTMP?.gameObject.SetActive(false);
        scriptPopup?.SetActive(false);

        // Tracker entry 2 mirrors DNS toggle default (ON)
        if (trackerEntries.Length > 2) trackerEntries[2]?.SetActive(true);

        gameObject.SetActive(true);
        ShowStep(DCPromoStep.DeploymentConfig);
        ActivityLogManager.Log("Opened DCPromo Wizard", ActivityLogManager.EntryType.Action);
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private void GoNext()
    {
        switch (_currentStep)
        {
            case DCPromoStep.DeploymentConfig:
                if (!ValidateDeploymentConfig()) return;
                ShowStep(DCPromoStep.DCOptions);
                break;

            case DCPromoStep.DCOptions:
                if (!ValidateDCOptions()) return;
                _dnsInstallSelected = dnsToggle != null && dnsToggle.isOn;
                ShowStep(_dnsInstallSelected ? DCPromoStep.DNSOptions : DCPromoStep.AdditionalOptions);
                break;

            case DCPromoStep.DNSOptions:
                ShowStep(DCPromoStep.AdditionalOptions);
                break;

            case DCPromoStep.AdditionalOptions:
                ShowStep(DCPromoStep.Paths);
                break;

            case DCPromoStep.Paths:
                BuildReviewText();
                BuildScriptText();
                ShowStep(DCPromoStep.ReviewOptions);
                break;

            case DCPromoStep.ReviewOptions:
                ShowStep(DCPromoStep.PrerequisitesCheck);
                break;

            case DCPromoStep.PrerequisitesCheck:
                if (!_prerequisitesComplete) return;
                StartInstall();
                break;
        }
    }

    private void GoBack()
    {
        switch (_currentStep)
        {
            case DCPromoStep.DCOptions:
                ShowStep(DCPromoStep.DeploymentConfig);
                break;

            case DCPromoStep.DNSOptions:
                ShowStep(DCPromoStep.DCOptions);
                break;

            case DCPromoStep.AdditionalOptions:
                ShowStep(_dnsInstallSelected ? DCPromoStep.DNSOptions : DCPromoStep.DCOptions);
                break;

            case DCPromoStep.Paths:
                ShowStep(DCPromoStep.AdditionalOptions);
                break;

            case DCPromoStep.ReviewOptions:
                ShowStep(DCPromoStep.Paths);
                break;

            case DCPromoStep.PrerequisitesCheck:
                StopAllCoroutines();
                ResetPrerequisitesPanel();
                ShowStep(DCPromoStep.ReviewOptions);
                break;
        }
    }

    private void CloseWizard()
    {
        StopAllCoroutines();
        scriptPopup?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Step Display ──────────────────────────────────────────────────────────

    private void ShowStep(DCPromoStep step)
    {
        _currentStep = step;

        deploymentConfigPanel?.SetActive(step  == DCPromoStep.DeploymentConfig);
        dcOptionsPanel?.SetActive(step         == DCPromoStep.DCOptions);
        dnsOptionsPanel?.SetActive(step        == DCPromoStep.DNSOptions);
        additionalOptionsPanel?.SetActive(step == DCPromoStep.AdditionalOptions);
        pathsPanel?.SetActive(step             == DCPromoStep.Paths);
        reviewOptionsPanel?.SetActive(step     == DCPromoStep.ReviewOptions);
        prerequisitesPanel?.SetActive(step     == DCPromoStep.PrerequisitesCheck);
        installProgressPanel?.SetActive(step   == DCPromoStep.InstallProgress);
        rebootPanel?.SetActive(step            == DCPromoStep.Reboot);

        if (step == DCPromoStep.AdditionalOptions)  OnEnterAdditionalOptions();
        if (step == DCPromoStep.Paths)              OnEnterPaths();
        if (step == DCPromoStep.PrerequisitesCheck) OnEnterPrerequisites();

        RefreshButtons();
        RefreshTracker(step);
    }

    private void RefreshButtons()
    {
        bool duringProcess = _currentStep == DCPromoStep.InstallProgress
                          || _currentStep == DCPromoStep.Reboot;

        bool isFirst = _currentStep == DCPromoStep.DeploymentConfig;

        prevBtn?.gameObject.SetActive(!duringProcess && !isFirst);
        nextBtn?.gameObject.SetActive(!duringProcess);
        cancelBtn?.gameObject.SetActive(!duringProcess);

        if (_nextBtnLabel != null)
            _nextBtnLabel.text = _currentStep == DCPromoStep.PrerequisitesCheck ? "Install" : "Next >";

        if (nextBtn != null)
            nextBtn.interactable = _currentStep != DCPromoStep.PrerequisitesCheck || _prerequisitesComplete;
    }

    private void RefreshTracker(DCPromoStep step)
    {
        int activeIndex = StepToTrackerIndex(step);
        for (int i = 0; i < trackerLabels.Length; i++)
        {
            if (trackerLabels[i] == null) continue;
            bool isActive = i == activeIndex;
            trackerLabels[i].color     = isActive ? trackerActiveColor   : trackerInactiveColor;
            trackerLabels[i].fontStyle = isActive ? FontStyles.Bold      : FontStyles.Normal;
        }
    }

    private static int StepToTrackerIndex(DCPromoStep step) => step switch
    {
        DCPromoStep.DeploymentConfig   => 0,
        DCPromoStep.DCOptions          => 1,
        DCPromoStep.DNSOptions         => 2,
        DCPromoStep.AdditionalOptions  => 3,
        DCPromoStep.Paths              => 4,
        DCPromoStep.ReviewOptions      => 5,
        DCPromoStep.PrerequisitesCheck => 6,
        _                              => -1
    };

    // ── Step Entry Hooks ──────────────────────────────────────────────────────

    private void OnEnterAdditionalOptions()
    {
        string domain = _enteredDomainName;
        int dot = domain.IndexOf('.');
        string first = dot > 0 ? domain.Substring(0, dot) : domain;
        first = first.ToUpper();
        if (first.Length > 15) first = first.Substring(0, 15);
        _computedNetBIOS = first;
        if (netBIOSInput != null) netBIOSInput.text = _computedNetBIOS;
    }

    private void OnEnterPaths()
    {
        if (databasePathInput != null && string.IsNullOrEmpty(databasePathInput.text))
            databasePathInput.text = @"C:\Windows\NTDS";
        if (logPathInput != null && string.IsNullOrEmpty(logPathInput.text))
            logPathInput.text = @"C:\Windows\NTDS";
        if (sysvolPathInput != null && string.IsNullOrEmpty(sysvolPathInput.text))
            sysvolPathInput.text = @"C:\Windows\SYSVOL";
    }

    private void OnEnterPrerequisites()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        string computerName = (state != null && !string.IsNullOrEmpty(state.ComputerName))
            ? state.ComputerName : "This computer";

        if (operationNoticeTMP != null)
            operationNoticeTMP.text =
                $"{computerName}\n" +
                "Active Directory Domain Services is installed on this computer.";

        if (verifyingStatusTMP != null)
            verifyingStatusTMP.text = "Verifying prerequisites for domain controller operation...";

        resultsTMP?.gameObject.SetActive(false);
        rebootWarningTMP?.gameObject.SetActive(false);

        _prerequisitesComplete = false;
        RefreshButtons();

        StartCoroutine(RunPrerequisitesCheck());
    }

    private void ResetPrerequisitesPanel()
    {
        _prerequisitesComplete = false;
        resultsTMP?.gameObject.SetActive(false);
        rebootWarningTMP?.gameObject.SetActive(false);
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator RunPrerequisitesCheck()
    {
        yield return new WaitForSeconds(prerequisiteDelay);

        if (_currentStep != DCPromoStep.PrerequisitesCheck) yield break;

        if (resultsTMP != null)
        {
            resultsTMP.text =
                "<color=#FFD700>⚠ Windows Server 2019 domain controllers have a default for the " +
                "security setting \"Allow cryptography algorithms compatible with Windows NT 4.0\" that " +
                "prevents weaker cryptography algorithms when establishing Security Channel sessions. " +
                "For more information about this setting, see Knowledge Base article 942564.</color>\n\n" +
                "<color=#FFD700>⚠ A delegation for this DNS server cannot be created because the " +
                "authoritative parent zone cannot be found or it does not run Windows DNS server. " +
                "If you are integrating with an existing DNS infrastructure, you should manually create " +
                "a delegation to this DNS server in the parent zone to ensure reliable name resolution " +
                "from outside the domain. Otherwise, no action is required.</color>\n\n" +
                "<color=#00CC00>✓ All prerequisite checks passed successfully. " +
                "Click 'Install' to begin installation.</color>";
            resultsTMP.gameObject.SetActive(true);
        }

        if (rebootWarningTMP != null)
        {
            rebootWarningTMP.text =
                "If you click Install, the server will automatically reboot at the end of the promotion operation.";
            rebootWarningTMP.gameObject.SetActive(true);
        }

        _prerequisitesComplete = true;
        RefreshButtons();
        ActivityLogManager.Log("DCPromo prerequisites check passed", ActivityLogManager.EntryType.Action);
    }

    private void StartInstall()
    {
        ShowStep(DCPromoStep.InstallProgress);
        StartCoroutine(RunInstallProgress());
    }

    private IEnumerator RunInstallProgress()
    {
        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value    = 0f;
        }

        float interval = installProgressDuration / InstallProgressMessages.Length;

        for (int i = 0; i < InstallProgressMessages.Length; i++)
        {
            if (progressStatusTMP != null)
                progressStatusTMP.text = InstallProgressMessages[i];

            if (progressBar != null)
                progressBar.value = (float)(i + 1) / InstallProgressMessages.Length;

            yield return new WaitForSeconds(interval);
        }

        ShowStep(DCPromoStep.Reboot);
        StartCoroutine(RunReboot());
    }

    private IEnumerator RunReboot()
    {
        yield return new WaitForSeconds(rebootDuration);
        FinishPromotion();
    }

    private void FinishPromotion()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null)
        {
            state.IsDomainController    = true;
            state.DCPromoCompleted      = true;
            state.DomainName            = _enteredDomainName;
            state.NetBIOSName           = _computedNetBIOS;
            state.DCPromoDeploymentType = GetDeploymentTypeString();
            state.DNSCheckedInDCPromo   = _dnsInstallSelected;
            state.DNSInstalled          = _dnsInstallSelected;
            state.ForestFunctionalLevel = GetDropdownText(forestLevelDropdown);
            state.DomainFunctionalLevel = GetDropdownText(domainLevelDropdown);
            state.DSRMPassword          = dsrmPasswordInput != null ? dsrmPasswordInput.text : "";
            state.DCDatabasePath        = databasePathInput != null ? databasePathInput.text : @"C:\Windows\NTDS";
            state.DCLogPath             = logPathInput      != null ? logPathInput.text      : @"C:\Windows\NTDS";
            state.DCSysvolPath          = sysvolPathInput   != null ? sysvolPathInput.text   : @"C:\Windows\SYSVOL";
        }

        ActivityLogManager.Log(
            $"Server promoted to Domain Controller — " +
            $"Domain: {_enteredDomainName}, NetBIOS: {_computedNetBIOS}, " +
            $"Forest Level: {GetDropdownText(forestLevelDropdown)}",
            ActivityLogManager.EntryType.Action);

        // Hand off to RestartUIController — deactivating desktopPanel closes this wizard too.
        ServerVirtualOSManager.Instance?.TriggerRestart();
    }

    // ── Deployment Toggle Wiring ──────────────────────────────────────────────

    private void WireDeploymentToggles()
    {
        addDCToggle?.onValueChanged.AddListener(on =>
        {
            if (on)
            {
                addDomainToggle?.SetIsOnWithoutNotify(false);
                addForestToggle?.SetIsOnWithoutNotify(false);
            }
            else if (!AnyDeploymentToggleOn())
            {
                addForestToggle?.SetIsOnWithoutNotify(true);
            }
            RefreshDeploymentSubPanels();
        });

        addDomainToggle?.onValueChanged.AddListener(on =>
        {
            if (on)
            {
                addDCToggle?.SetIsOnWithoutNotify(false);
                addForestToggle?.SetIsOnWithoutNotify(false);
            }
            else if (!AnyDeploymentToggleOn())
            {
                addForestToggle?.SetIsOnWithoutNotify(true);
            }
            RefreshDeploymentSubPanels();
        });

        addForestToggle?.onValueChanged.AddListener(on =>
        {
            if (on)
            {
                addDCToggle?.SetIsOnWithoutNotify(false);
                addDomainToggle?.SetIsOnWithoutNotify(false);
            }
            else if (!AnyDeploymentToggleOn())
            {
                addForestToggle?.SetIsOnWithoutNotify(true);
            }
            RefreshDeploymentSubPanels();
        });
    }

    private bool AnyDeploymentToggleOn() =>
        (addDCToggle     != null && addDCToggle.isOn)
     || (addDomainToggle != null && addDomainToggle.isOn)
     || (addForestToggle != null && addForestToggle.isOn);

    private void RefreshDeploymentSubPanels()
    {
        addDCSub?.SetActive(addDCToggle     != null && addDCToggle.isOn);
        addDomainSub?.SetActive(addDomainToggle != null && addDomainToggle.isOn);
        addForestSub?.SetActive(addForestToggle != null && addForestToggle.isOn);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private bool ValidateDeploymentConfig()
    {
        if (addDomainToggle != null && addDomainToggle.isOn)
        {
            Debug.LogWarning("[DCPromo] 'Add a new domain to an existing forest' is not applicable in this task.");
            return false;
        }

        string input;

        if (addDCToggle != null && addDCToggle.isOn)
        {
            input = existingDomainInput != null ? existingDomainInput.text.Trim() : "";
            if (string.IsNullOrEmpty(input))
            {
                Debug.LogWarning("[DCPromo] Domain name is required.");
                return false;
            }
        }
        else
        {
            input = rootDomainInput != null ? rootDomainInput.text.Trim() : "";
            if (string.IsNullOrEmpty(input))
            {
                Debug.LogWarning("[DCPromo] Root domain name is required.");
                return false;
            }
            if (!string.IsNullOrEmpty(expectedDomainName) &&
                !string.Equals(input, expectedDomainName.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[DCPromo] Domain name '{input}' does not match expected: {expectedDomainName}");
                return false;
            }
        }

        _enteredDomainName = input;
        return true;
    }

    private bool ValidateDCOptions()
    {
        string pw  = dsrmPasswordInput != null ? dsrmPasswordInput.text : "";
        string cfm = dsrmConfirmInput  != null ? dsrmConfirmInput.text  : "";

        if (string.IsNullOrEmpty(pw))
        {
            Debug.LogWarning("[DCPromo] DSRM password is required.");
            return false;
        }
        if (pw != cfm)
        {
            Debug.LogWarning("[DCPromo] DSRM passwords do not match.");
            return false;
        }
        return true;
    }

    // ── Review & Script Text ──────────────────────────────────────────────────

    private void BuildReviewText()
    {
        if (reviewTMP == null) return;

        string forestLevel = GetDropdownText(forestLevelDropdown);
        string domainLevel = GetDropdownText(domainLevelDropdown);
        string db          = databasePathInput != null ? databasePathInput.text : @"C:\Windows\NTDS";
        string log         = logPathInput      != null ? logPathInput.text      : @"C:\Windows\NTDS";
        string sysvol      = sysvolPathInput   != null ? sysvolPathInput.text   : @"C:\Windows\SYSVOL";

        reviewTMP.text =
            "Domain and forest information\n" +
            $"  New domain name: {_enteredDomainName}\n" +
            $"  Deployment operation: {GetDeploymentTypeLabel()}\n\n" +
            "Domain controller options\n" +
            $"  Forest functional level: {forestLevel}\n" +
            $"  Domain functional level: {domainLevel}\n" +
            $"  DNS server: {(_dnsInstallSelected ? "Yes" : "No")}\n" +
            "  Global Catalog: Yes\n" +
            "  Site name: Default-First-Site-Name\n\n" +
            "DNS Options\n" +
            "  Create DNS delegation: No\n\n" +
            "Additional Options\n" +
            $"  NetBIOS domain name: {_computedNetBIOS}\n\n" +
            "Paths\n" +
            $"  Database folder: {db}\n" +
            $"  Log files folder: {log}\n" +
            $"  SYSVOL folder: {sysvol}";
    }

    private void BuildScriptText()
    {
        if (scriptTMP == null) return;

        string forestLevel = GetDropdownText(forestLevelDropdown);
        string domainLevel = GetDropdownText(domainLevelDropdown);
        string db          = databasePathInput != null ? databasePathInput.text : @"C:\Windows\NTDS";
        string log         = logPathInput      != null ? logPathInput.text      : @"C:\Windows\NTDS";
        string sysvol      = sysvolPathInput   != null ? sysvolPathInput.text   : @"C:\Windows\SYSVOL";

        scriptTMP.text =
            "Install-ADDSForest `\n" +
            $"    -DomainName \"{_enteredDomainName}\" `\n" +
            $"    -DomainNetBIOSName \"{_computedNetBIOS}\" `\n" +
            $"    -ForestMode \"{FunctionalLevelToMode(forestLevel)}\" `\n" +
            $"    -DomainMode \"{FunctionalLevelToMode(domainLevel)}\" `\n" +
            $"    -InstallDNS:${(_dnsInstallSelected ? "true" : "false")} `\n" +
            $"    -DatabasePath \"{db}\" `\n" +
            $"    -LogPath \"{log}\" `\n" +
            $"    -SYSVOLPath \"{sysvol}\" `\n" +
            "    -Force:$true";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string GetDeploymentTypeString()
    {
        if (addDCToggle     != null && addDCToggle.isOn)     return "AddDC";
        if (addDomainToggle != null && addDomainToggle.isOn) return "AddDomain";
        return "NewForest";
    }

    private string GetDeploymentTypeLabel()
    {
        if (addDCToggle     != null && addDCToggle.isOn)     return "Add a domain controller to an existing domain";
        if (addDomainToggle != null && addDomainToggle.isOn) return "Add a new domain to an existing forest";
        return "New forest installation";
    }

    private static string GetDropdownText(TMP_Dropdown dd)
    {
        if (dd == null || dd.options.Count == 0) return "";
        return dd.options[dd.value].text;
    }

    private static string FunctionalLevelToMode(string level) => level switch
    {
        "Windows Server 2008 R2" => "Win2008R2",
        "Windows Server 2012"    => "Win2012",
        "Windows Server 2012 R2" => "Win2012R2",
        "Windows Server 2016"    => "WinThreshold",
        _                        => "WinThreshold"
    };

    private static void PopulateDropdown(TMP_Dropdown dd, string[] options)
    {
        if (dd == null) return;
        dd.ClearOptions();
        dd.AddOptions(new List<string>(options));
        dd.value = options.Length - 1;
    }
}
