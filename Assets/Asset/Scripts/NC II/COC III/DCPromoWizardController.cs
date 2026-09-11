/*
 * ================================================================
 *  UNITY SETUP GUIDE — DCPromoWizardController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "DCPromo Wizard Panel" (starts INACTIVE).
 *
 *  HIERARCHY
 *    DCPromo Wizard Panel               ← this script here
 *      ├── Step 0 — Deployment Config   → steps[0]
 *      │     └── (shows "Add new forest" pre-selected label)
 *      │     └── NextBtn
 *      ├── Step 1 — Domain Name         → steps[1]
 *      │     ├── DomainInput            → domainInput
 *      │     ├── PrevBtn
 *      │     └── NextBtn
 *      ├── Step 2 — DC Options          → steps[2]
 *      │     ├── DNSCheckbox            → dnsToggle
 *      │     ├── ForestLevelDropdown    → forestLevelDropdown
 *      │     ├── PrevBtn
 *      │     └── NextBtn
 *      ├── Step 3 — Review              → steps[3]
 *      │     ├── ReviewLabel            → reviewLabelTMP
 *      │     ├── PrevBtn
 *      │     └── InstallBtn             → installBtn
 *      └── Step 4 — Reboot Screen       → steps[4]
 *            └── (static restarting animation / label)
 *            (auto-closes after rebootDuration seconds)
 *
 *  INSPECTOR ASSIGNMENTS
 *    steps[0..4]         → step panel GameObjects
 *    domainInput         → TMP_InputField for domain name
 *    dnsToggle           → Toggle for DNS Server checkbox (default ON)
 *    forestLevelDropdown → TMP_Dropdown for forest functional level
 *    forestLevelOptions  → string[] populated in Inspector
 *                          e.g. {"Windows Server 2016","Windows Server 2019"}
 *    reviewLabelTMP      → TMP showing summary before install
 *    installBtn          → Install button in step 3
 *    rebootDuration      → seconds to show reboot screen (e.g. 3f)
 *
 *  HOW IT WORKS
 *    Each step writes to ServerDeviceState as the player fills fields.
 *    On Install: state.IsDomainController = true, state.DCPromoCompleted = true.
 *    Step 4 (reboot) auto-advances after rebootDuration, then closes wizard.
 *    ServerManagerController's Update() detects IsDomainController = true and
 *    hides the promotion notification flag automatically.
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DCPromoWizardController : MonoBehaviour
{
    public static DCPromoWizardController Instance { get; private set; }

    [Header("Steps")]
    [SerializeField] private GameObject[] steps = new GameObject[5];

    [Header("Step 1 — Domain Name")]
    [SerializeField] private TMP_InputField domainInput;

    [Header("Step 2 — DC Options")]
    [SerializeField] private Toggle     dnsToggle;
    [SerializeField] private TMP_Dropdown forestLevelDropdown;
    [Tooltip("Forest functional level options shown in the dropdown.")]
    [SerializeField] private string[]   forestLevelOptions = { "Windows Server 2016", "Windows Server 2019" };

    [Header("Step 3 — Review")]
    [SerializeField] private TMP_Text reviewLabelTMP;
    [SerializeField] private Button   installBtn;

    [Header("Navigation")]
    [SerializeField] private Button[] nextBtns;
    [SerializeField] private Button[] prevBtns;

    [Header("Reboot")]
    [SerializeField] private float rebootDuration = 3f;

    private int   _step           = 0;
    private float _rebootTimer    = 0f;
    private bool  _inReboot       = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        foreach (var btn in nextBtns) btn?.onClick.AddListener(GoNext);
        foreach (var btn in prevBtns) btn?.onClick.AddListener(GoBack);
        installBtn?.onClick.AddListener(Install);

        gameObject.SetActive(false);
    }

    private void Start()
    {
        if (forestLevelDropdown != null)
        {
            forestLevelDropdown.ClearOptions();
            var opts = new System.Collections.Generic.List<string>(forestLevelOptions);
            forestLevelDropdown.AddOptions(opts);
        }

        if (dnsToggle != null) dnsToggle.isOn = true;
    }

    private void Update()
    {
        if (!_inReboot) return;
        _rebootTimer -= Time.deltaTime;
        if (_rebootTimer <= 0f) FinishReboot();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        _step     = 0;
        _inReboot = false;
        if (domainInput != null) domainInput.text = "";
        if (dnsToggle   != null) dnsToggle.isOn   = true;
        gameObject.SetActive(true);
        ShowStep(0);
        ActivityLogManager.Log("Opened dcpromo wizard", ActivityLogManager.EntryType.Action);
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private void GoNext()
    {
        if (_step == 1)
        {
            string domain = domainInput != null ? domainInput.text.Trim() : "";
            if (string.IsNullOrEmpty(domain)) { Debug.LogWarning("[DCPromo] Domain name required."); return; }
            var state = ServerVirtualOSManager.Instance?.ServerState;
            if (state != null) state.DomainName = domain;
            ActivityLogManager.Log($"Domain name entered: {domain}", ActivityLogManager.EntryType.Action);
        }
        if (_step == 2)
        {
            var state = ServerVirtualOSManager.Instance?.ServerState;
            if (state != null)
            {
                state.DNSCheckedInDCPromo = dnsToggle != null && dnsToggle.isOn;
                state.DNSInstalled        = state.DNSCheckedInDCPromo;
                int idx = forestLevelDropdown != null ? forestLevelDropdown.value : 0;
                state.ForestFunctionalLevel = forestLevelOptions.Length > idx
                    ? forestLevelOptions[idx] : "";
            }
            ActivityLogManager.Log("DNS checkbox and forest level set", ActivityLogManager.EntryType.Action);
        }
        if (_step == 3) return; // Install button handles step 3

        if (_step < 3)
        {
            if (_step == 2) BuildReviewLabel();
            ShowStep(_step + 1);
        }
    }

    private void GoBack()
    {
        if (_step > 0) ShowStep(_step - 1);
    }

    private void BuildReviewLabel()
    {
        if (reviewLabelTMP == null) return;
        var state = ServerVirtualOSManager.Instance?.ServerState;
        string dns   = (state?.DNSCheckedInDCPromo == true) ? "Yes" : "No";
        string level = state?.ForestFunctionalLevel ?? "";
        reviewLabelTMP.text =
            $"Domain Name: {state?.DomainName}\n" +
            $"Install DNS: {dns}\n" +
            $"Forest Functional Level: {level}\n\n" +
            "Click Install to promote this server to a domain controller.";
    }

    private void Install()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null)
        {
            state.IsDomainController = true;
            state.DCPromoCompleted   = true;
        }
        ActivityLogManager.Log($"Server promoted to Domain Controller for domain: {state?.DomainName}",
            ActivityLogManager.EntryType.Action);
        ShowStep(4);
        _rebootTimer = rebootDuration;
        _inReboot    = true;
    }

    private void FinishReboot()
    {
        _inReboot = false;
        gameObject.SetActive(false);
        ActivityLogManager.Log("Server reboot complete — domain controller active.", ActivityLogManager.EntryType.Action);
    }

    // ── Step display ──────────────────────────────────────────────────────────

    private void ShowStep(int index)
    {
        for (int i = 0; i < steps.Length; i++)
            steps[i]?.SetActive(i == index);
        _step = index;
    }
}
