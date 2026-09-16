/*
 * ================================================================
 *  UNITY SETUP GUIDE — NewScopeWizardController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to the "New Scope Wizard Panel" GameObject (child of DHCP Console Panel).
 *    START ACTIVE in the scene so Awake fires; the script deactivates itself.
 *    DHCPConsoleController holds a reference and calls Open() on "New Scope" click.
 *
 *  PANEL LIST  (10 GameObjects, all start INACTIVE — shown one at a time)
 *
 *    New Scope Wizard Panel              ← this script here
 *      ├── Step0_Welcome                 → welcomePanel
 *      │     ├── TitleTMP               (static: "Welcome to the New Scope Wizard")
 *      │     └── DescTMP                (static: explains what the wizard does)
 *      │
 *      ├── Step1_ScopeName              → scopeNamePanel
 *      │     ├── ScopeNameInput         → scopeNameInput     (required)
 *      │     └── DescriptionInput       → descriptionInput   (optional)
 *      │
 *      ├── Step2_IPRange                → ipRangePanel
 *      │     ├── [Group 1 — DHCP Server Configuration]
 *      │     │     ├── StartIPInput     → startIPInput       (required; triggers subnet auto-fill on end-edit)
 *      │     │     └── EndIPInput       → endIPInput         (required)
 *      │     └── [Group 2 — Subnet Configuration]
 *      │           ├── SubnetLengthInput → subnetLengthInput (required; syncs with SubnetMaskInput)
 *      │           └── SubnetMaskInput  → subnetMaskInput    (required; syncs with SubnetLengthInput)
 *      │
 *      ├── Step3_Exclusions             → exclusionsPanel    (pass-through, no required input)
 *      │     ├── ExclStartIPInput       → exclStartIPInput
 *      │     ├── ExclEndIPInput         → exclEndIPInput
 *      │     ├── SubnetDelayInput       → subnetDelayInput   (ms, numeric)
 *      │     ├── ExclusionListTMP       → exclusionListTMP   (TMP_Text, read-only display)
 *      │     ├── AddExclusionBtn        → addExclusionBtn
 *      │     └── RemoveExclusionBtn     → removeExclusionBtn (removes last entry)
 *      │
 *      ├── Step4_LeaseDuration          → leaseDurationPanel
 *      │     ├── LeaseDaysInput         → leaseDaysInput     (default "8", pre-filled on entry)
 *      │     ├── LeaseHoursInput        → leaseHoursInput    (default "0")
 *      │     └── LeaseMinutesInput      → leaseMinutesInput  (default "0")
 *      │
 *      ├── Step5_ConfigureOptions       → configureOptionsPanel
 *      │     ├── YesConfigNowToggle     → yesConfigNowToggle    (default ON)
 *      │     └── NoConfigLaterToggle    → noConfigLaterToggle
 *      │           Mutually exclusive. "No" skips Steps 6/7/8 entirely.
 *      │
 *      ├── Step6_Router                 → routerPanel        (skipped when Step5 = No)
 *      │     ├── RouterIPInput          → routerIPInput
 *      │     ├── RouterListTMP          → routerListTMP      (TMP_Text, read-only display)
 *      │     ├── AddRouterBtn           → addRouterBtn
 *      │     ├── RemoveRouterBtn        → removeRouterBtn    (removes last entry)
 *      │     ├── MoveUpRouterBtn        → moveUpRouterBtn    (enabled when list > 1)
 *      │     └── MoveDownRouterBtn      → moveDownRouterBtn  (enabled when list > 1)
 *      │
 *      ├── Step7_DNS                    → dnsPanel           (skipped when Step5 = No)
 *      │     ├── ParentDomainInput      → parentDomainInput  (auto-filled from state.DomainName on entry)
 *      │     ├── DNSServerNameInput     → dnsServerNameInput (for Resolve)
 *      │     ├── DNSIPInput             → dnsIPInput
 *      │     ├── DNSListTMP             → dnsListTMP         (TMP_Text; pre-populated with server IP on entry)
 *      │     ├── ResolveDNSBtn          → resolveDNSBtn
 *      │     ├── AddDNSBtn              → addDNSBtn
 *      │     ├── RemoveDNSBtn           → removeDNSBtn       (removes last entry)
 *      │     ├── MoveUpDNSBtn           → moveUpDNSBtn       (enabled when list > 1)
 *      │     └── MoveDownDNSBtn         → moveDownDNSBtn     (enabled when list > 1)
 *      │
 *      ├── Step8_WINS                   → winsPanel          (skipped when Step5 = No; pass-through)
 *      │     ├── WINSServerNameInput    → winsServerNameInput (for Resolve)
 *      │     ├── WINSIPInput            → winsIPInput
 *      │     ├── WINSListTMP            → winsListTMP        (TMP_Text, read-only display)
 *      │     ├── ResolveWINSBtn         → resolveWINSBtn
 *      │     ├── AddWINSBtn             → addWINSBtn
 *      │     ├── RemoveWINSBtn          → removeWINSBtn      (removes last entry)
 *      │     ├── MoveUpWINSBtn          → moveUpWINSBtn      (enabled when list > 1)
 *      │     └── MoveDownWINSBtn        → moveDownWINSBtn    (enabled when list > 1)
 *      │
 *      └── Step9_Activate               → activatePanel
 *            ├── ActivateNowToggle      → activateNowToggle    (default ON)
 *            └── ActivateLaterToggle    → activateLaterToggle
 *
 *  PERSISTENT BUTTONS  (outside all panels, always visible while wizard is open)
 *    PrevBtn    → prevBtn    (hidden on Step0_Welcome)
 *    NextBtn    → nextBtn    (label changes to "Finish" on Step9_Activate)
 *    CancelBtn  → cancelBtn  (always visible)
 *
 *  NAVIGATION FLOW
 *    Step0 → Step1 → Step2 → Step3 → Step4 → Step5
 *      Step5 Yes → Step6 → Step7 → Step8 → Step9
 *      Step5 No  ────────────────────────→ Step9
 *    GoBack from Step9:
 *      if _configureOptionsNow → Step8, else → Step5
 *
 *  SUBNET AUTO-FILL (Step2)
 *    StartIPInput.onEndEdit → detects address class → fills SubnetLength + SubnetMask if empty.
 *    SubnetLengthInput.onEndEdit → recomputes SubnetMask.
 *    SubnetMaskInput.onEndEdit   → recomputes SubnetLength.
 *
 *  LIST MANAGEMENT (Steps 3, 6, 7, 8)
 *    Add: validates input → appends to List<string> → refreshes TMP_Text display.
 *    Remove: removes last entry → refreshes display.
 *    Up/Down: enabled only when list has > 1 item (cosmetic; no reordering logic needed
 *    in practice since students add exactly one entry per list in this simulation).
 *
 *  DNS PRE-POPULATION (Step7)
 *    On entering Step7: ParentDomainInput auto-filled from state.DomainName (if empty).
 *    DNS list pre-filled with string.Join(".", state.IPOctets) if list is empty.
 *
 *  RESOLVE BUTTONS (Steps 7, 8)
 *    If typed server name matches state.ComputerName (case-insensitive) → fills IP input
 *    with string.Join(".", state.IPOctets).
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewScopeWizardController : MonoBehaviour
{
    private enum WizardPanel
    {
        Welcome,
        ScopeName,
        IPRange,
        Exclusions,
        LeaseDuration,
        ConfigureOptions,
        Router,
        DNS,
        WINS,
        Activate
    }

    [Header("Panels")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private GameObject scopeNamePanel;
    [SerializeField] private GameObject ipRangePanel;
    [SerializeField] private GameObject exclusionsPanel;
    [SerializeField] private GameObject leaseDurationPanel;
    [SerializeField] private GameObject configureOptionsPanel;
    [SerializeField] private GameObject routerPanel;
    [SerializeField] private GameObject dnsPanel;
    [SerializeField] private GameObject winsPanel;
    [SerializeField] private GameObject activatePanel;

    [Header("Step 1 — Scope Name")]
    [SerializeField] private TMP_InputField scopeNameInput;
    [SerializeField] private TMP_InputField descriptionInput;

    [Header("Step 2 — IP Range")]
    [SerializeField] private TMP_InputField startIPInput;
    [SerializeField] private TMP_InputField endIPInput;
    [SerializeField] private TMP_InputField subnetLengthInput;
    [SerializeField] private TMP_InputField subnetMaskInput;

    [Header("Step 3 — Exclusions")]
    [SerializeField] private TMP_InputField exclStartIPInput;
    [SerializeField] private TMP_InputField exclEndIPInput;
    [SerializeField] private TMP_InputField subnetDelayInput;
    [SerializeField] private TMP_Text       exclusionListTMP;
    [SerializeField] private Button         addExclusionBtn;
    [SerializeField] private Button         removeExclusionBtn;

    [Header("Step 4 — Lease Duration")]
    [SerializeField] private TMP_InputField leaseDaysInput;
    [SerializeField] private TMP_InputField leaseHoursInput;
    [SerializeField] private TMP_InputField leaseMinutesInput;

    [Header("Step 5 — Configure Options")]
    [SerializeField] private Toggle yesConfigNowToggle;
    [SerializeField] private Toggle noConfigLaterToggle;

    [Header("Step 6 — Router")]
    [SerializeField] private TMP_InputField routerIPInput;
    [SerializeField] private TMP_Text       routerListTMP;
    [SerializeField] private Button         addRouterBtn;
    [SerializeField] private Button         removeRouterBtn;
    [SerializeField] private Button         moveUpRouterBtn;
    [SerializeField] private Button         moveDownRouterBtn;

    [Header("Step 7 — DNS")]
    [SerializeField] private TMP_InputField parentDomainInput;
    [SerializeField] private TMP_InputField dnsServerNameInput;
    [SerializeField] private TMP_InputField dnsIPInput;
    [SerializeField] private TMP_Text       dnsListTMP;
    [SerializeField] private Button         resolveDNSBtn;
    [SerializeField] private Button         addDNSBtn;
    [SerializeField] private Button         removeDNSBtn;
    [SerializeField] private Button         moveUpDNSBtn;
    [SerializeField] private Button         moveDownDNSBtn;

    [Header("Step 8 — WINS")]
    [SerializeField] private TMP_InputField winsServerNameInput;
    [SerializeField] private TMP_InputField winsIPInput;
    [SerializeField] private TMP_Text       winsListTMP;
    [SerializeField] private Button         resolveWINSBtn;
    [SerializeField] private Button         addWINSBtn;
    [SerializeField] private Button         removeWINSBtn;
    [SerializeField] private Button         moveUpWINSBtn;
    [SerializeField] private Button         moveDownWINSBtn;

    [Header("Step 9 — Activate")]
    [SerializeField] private Toggle activateNowToggle;
    [SerializeField] private Toggle activateLaterToggle;

    [Header("Persistent Navigation")]
    [SerializeField] private Button prevBtn;
    [SerializeField] private Button nextBtn;
    [SerializeField] private Button cancelBtn;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private WizardPanel _currentPanel;
    private bool        _configureOptionsNow = true;
    private bool        _syncingSubnet       = false;
    private TMP_Text    _nextBtnLabel;

    private readonly List<string> _exclusionList = new List<string>();
    private readonly List<string> _routerList    = new List<string>();
    private readonly List<string> _dnsList       = new List<string>();
    private readonly List<string> _winsList      = new List<string>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _nextBtnLabel = nextBtn?.GetComponentInChildren<TMP_Text>();

        prevBtn?.onClick.AddListener(GoBack);
        nextBtn?.onClick.AddListener(GoNext);
        cancelBtn?.onClick.AddListener(Cancel);

        startIPInput?.onEndEdit.AddListener(OnStartIPEndEdit);
        subnetLengthInput?.onEndEdit.AddListener(OnLengthEndEdit);
        subnetMaskInput?.onEndEdit.AddListener(OnMaskEndEdit);

        addExclusionBtn?.onClick.AddListener(OnAddExclusion);
        removeExclusionBtn?.onClick.AddListener(OnRemoveExclusion);

        WireConfigOptionsToggles();

        addRouterBtn?.onClick.AddListener(OnAddRouter);
        removeRouterBtn?.onClick.AddListener(OnRemoveRouter);

        resolveDNSBtn?.onClick.AddListener(OnResolveDNS);
        addDNSBtn?.onClick.AddListener(OnAddDNS);
        removeDNSBtn?.onClick.AddListener(OnRemoveDNS);

        resolveWINSBtn?.onClick.AddListener(OnResolveWINS);
        addWINSBtn?.onClick.AddListener(OnAddWINS);
        removeWINSBtn?.onClick.AddListener(OnRemoveWINS);

        WireActivateToggles();

        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        ResetAllState();
        gameObject.SetActive(true);
        ShowPanel(WizardPanel.Welcome);
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private void GoNext()
    {
        switch (_currentPanel)
        {
            case WizardPanel.Welcome:
                ShowPanel(WizardPanel.ScopeName);
                break;
            case WizardPanel.ScopeName:
                if (!ValidateScopeName()) return;
                ShowPanel(WizardPanel.IPRange);
                break;
            case WizardPanel.IPRange:
                if (!ValidateIPRange()) return;
                ShowPanel(WizardPanel.Exclusions);
                break;
            case WizardPanel.Exclusions:
                ShowPanel(WizardPanel.LeaseDuration);
                break;
            case WizardPanel.LeaseDuration:
                if (!ValidateLeaseDuration()) return;
                ShowPanel(WizardPanel.ConfigureOptions);
                break;
            case WizardPanel.ConfigureOptions:
                _configureOptionsNow = yesConfigNowToggle != null && yesConfigNowToggle.isOn;
                ShowPanel(_configureOptionsNow ? WizardPanel.Router : WizardPanel.Activate);
                break;
            case WizardPanel.Router:
                if (!ValidateRouterList()) return;
                ShowPanel(WizardPanel.DNS);
                break;
            case WizardPanel.DNS:
                if (!ValidateDNSList()) return;
                ShowPanel(WizardPanel.WINS);
                break;
            case WizardPanel.WINS:
                ShowPanel(WizardPanel.Activate);
                break;
            case WizardPanel.Activate:
                Finish();
                break;
        }
    }

    private void GoBack()
    {
        switch (_currentPanel)
        {
            case WizardPanel.ScopeName:
                ShowPanel(WizardPanel.Welcome);
                break;
            case WizardPanel.IPRange:
                ShowPanel(WizardPanel.ScopeName);
                break;
            case WizardPanel.Exclusions:
                ShowPanel(WizardPanel.IPRange);
                break;
            case WizardPanel.LeaseDuration:
                ShowPanel(WizardPanel.Exclusions);
                break;
            case WizardPanel.ConfigureOptions:
                ShowPanel(WizardPanel.LeaseDuration);
                break;
            case WizardPanel.Router:
                ShowPanel(WizardPanel.ConfigureOptions);
                break;
            case WizardPanel.DNS:
                ShowPanel(WizardPanel.Router);
                break;
            case WizardPanel.WINS:
                ShowPanel(WizardPanel.DNS);
                break;
            case WizardPanel.Activate:
                ShowPanel(_configureOptionsNow ? WizardPanel.WINS : WizardPanel.ConfigureOptions);
                break;
        }
    }

    private void Cancel()
    {
        gameObject.SetActive(false);
    }

    private void Finish()
    {
        SaveToState();
        gameObject.SetActive(false);
        DHCPConsoleController.Instance?.OnWizardComplete();

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null)
        {
            string status = state.DHCPScopeActive ? "Active" : "Inactive";
            ActivityLogManager.Log(
                $"DHCP scope created: \"{state.DHCPScopeName}\" " +
                $"({state.DHCPScopeStart} – {state.DHCPScopeEnd}), " +
                $"Lease: {state.DHCPLeaseDays}d {state.DHCPLeaseHours}h {state.DHCPLeaseMinutes}m, " +
                $"Status: {status}",
                ActivityLogManager.EntryType.Action);
        }
    }

    // ── Panel display ─────────────────────────────────────────────────────────

    private void ShowPanel(WizardPanel panel)
    {
        welcomePanel?.SetActive(panel          == WizardPanel.Welcome);
        scopeNamePanel?.SetActive(panel        == WizardPanel.ScopeName);
        ipRangePanel?.SetActive(panel          == WizardPanel.IPRange);
        exclusionsPanel?.SetActive(panel       == WizardPanel.Exclusions);
        leaseDurationPanel?.SetActive(panel    == WizardPanel.LeaseDuration);
        configureOptionsPanel?.SetActive(panel == WizardPanel.ConfigureOptions);
        routerPanel?.SetActive(panel           == WizardPanel.Router);
        dnsPanel?.SetActive(panel              == WizardPanel.DNS);
        winsPanel?.SetActive(panel             == WizardPanel.WINS);
        activatePanel?.SetActive(panel         == WizardPanel.Activate);

        _currentPanel = panel;

        if (panel == WizardPanel.LeaseDuration) OnEnterLeaseDurationPanel();
        if (panel == WizardPanel.DNS)           OnEnterDNSPanel();

        RefreshButtons();
    }

    private void RefreshButtons()
    {
        bool isFirst = _currentPanel == WizardPanel.Welcome;
        bool isLast  = _currentPanel == WizardPanel.Activate;

        prevBtn?.gameObject.SetActive(!isFirst);
        nextBtn?.gameObject.SetActive(true);
        cancelBtn?.gameObject.SetActive(true);

        if (_nextBtnLabel != null)
            _nextBtnLabel.text = isLast ? "Finish" : "Next >";
    }

    // ── Panel entry hooks ─────────────────────────────────────────────────────

    private void OnEnterLeaseDurationPanel()
    {
        if (leaseDaysInput    != null && string.IsNullOrEmpty(leaseDaysInput.text))    leaseDaysInput.text    = "8";
        if (leaseHoursInput   != null && string.IsNullOrEmpty(leaseHoursInput.text))   leaseHoursInput.text   = "0";
        if (leaseMinutesInput != null && string.IsNullOrEmpty(leaseMinutesInput.text)) leaseMinutesInput.text = "0";
    }

    private void OnEnterDNSPanel()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;

        if (parentDomainInput != null && string.IsNullOrEmpty(parentDomainInput.text))
            parentDomainInput.text = state.DomainName;

        if (_dnsList.Count == 0 && IsOctetsValid(state.IPOctets))
        {
            _dnsList.Add(string.Join(".", state.IPOctets));
            RefreshDNSListDisplay();
            RefreshDNSUpDown();
        }
    }

    // ── Subnet auto-fill (Step 2) ─────────────────────────────────────────────

    private void OnStartIPEndEdit(string value)
    {
        string[] parts = value.Trim().Split('.');
        if (parts.Length != 4 || !int.TryParse(parts[0], out int first)) return;

        int length;
        if      (first >= 1   && first <= 126) length = 8;
        else if (first >= 128 && first <= 191) length = 16;
        else if (first >= 192 && first <= 223) length = 24;
        else return;

        if (subnetLengthInput != null && string.IsNullOrEmpty(subnetLengthInput.text))
            subnetLengthInput.text = length.ToString();
        if (subnetMaskInput != null && string.IsNullOrEmpty(subnetMaskInput.text))
            subnetMaskInput.text = CIDRToMask(length);
    }

    private void OnLengthEndEdit(string value)
    {
        if (_syncingSubnet) return;
        if (!int.TryParse(value.Trim(), out int len) || len < 0 || len > 32) return;
        _syncingSubnet = true;
        if (subnetMaskInput != null) subnetMaskInput.text = CIDRToMask(len);
        _syncingSubnet = false;
    }

    private void OnMaskEndEdit(string value)
    {
        if (_syncingSubnet) return;
        int cidr = MaskToCIDR(value.Trim());
        if (cidr < 0) return;
        _syncingSubnet = true;
        if (subnetLengthInput != null) subnetLengthInput.text = cidr.ToString();
        _syncingSubnet = false;
    }

    private static string CIDRToMask(int length)
    {
        if (length < 0 || length > 32) return "";
        uint mask = length == 0 ? 0u : (0xFFFFFFFFu << (32 - length));
        return $"{(mask >> 24) & 0xFF}.{(mask >> 16) & 0xFF}.{(mask >> 8) & 0xFF}.{mask & 0xFF}";
    }

    private static int MaskToCIDR(string mask)
    {
        string[] parts = mask.Split('.');
        if (parts.Length != 4) return -1;
        uint val = 0;
        foreach (string p in parts)
        {
            if (!uint.TryParse(p, out uint b) || b > 255) return -1;
            val = (val << 8) | b;
        }
        int count = 0;
        for (uint v = val; v != 0; v >>= 1) count += (int)(v & 1u);
        return count;
    }

    // ── Exclusion list (Step 3) ───────────────────────────────────────────────

    private void OnAddExclusion()
    {
        string start = exclStartIPInput != null ? exclStartIPInput.text.Trim() : "";
        string end   = exclEndIPInput   != null ? exclEndIPInput.text.Trim()   : "";

        if (!IsValidIPString(start) || !IsValidIPString(end))
        {
            Debug.LogWarning("[DHCP] Exclusion: both Start and End IP must be valid.");
            return;
        }
        if (IPToUint(start) > IPToUint(end))
        {
            Debug.LogWarning("[DHCP] Exclusion: Start IP must be less than or equal to End IP.");
            return;
        }

        string entry = $"{start} - {end}";
        if (_exclusionList.Contains(entry)) return;

        _exclusionList.Add(entry);
        RefreshExclusionListDisplay();
        if (exclStartIPInput != null) exclStartIPInput.text = "";
        if (exclEndIPInput   != null) exclEndIPInput.text   = "";
    }

    private void OnRemoveExclusion()
    {
        if (_exclusionList.Count == 0) return;
        _exclusionList.RemoveAt(_exclusionList.Count - 1);
        RefreshExclusionListDisplay();
    }

    private void RefreshExclusionListDisplay()
    {
        if (exclusionListTMP != null)
            exclusionListTMP.text = string.Join("\n", _exclusionList);
    }

    // ── Router list (Step 6) ──────────────────────────────────────────────────

    private void OnAddRouter()
    {
        string ip = routerIPInput != null ? routerIPInput.text.Trim() : "";
        if (!IsValidIPString(ip)) { Debug.LogWarning("[DHCP] Router: invalid IP address."); return; }
        if (_routerList.Contains(ip)) return;

        _routerList.Add(ip);
        RefreshRouterListDisplay();
        RefreshRouterUpDown();
        if (routerIPInput != null) routerIPInput.text = "";
    }

    private void OnRemoveRouter()
    {
        if (_routerList.Count == 0) return;
        _routerList.RemoveAt(_routerList.Count - 1);
        RefreshRouterListDisplay();
        RefreshRouterUpDown();
    }

    private void RefreshRouterListDisplay()
    {
        if (routerListTMP != null) routerListTMP.text = string.Join("\n", _routerList);
    }

    private void RefreshRouterUpDown()
    {
        bool multi = _routerList.Count > 1;
        if (moveUpRouterBtn   != null) moveUpRouterBtn.interactable   = multi;
        if (moveDownRouterBtn != null) moveDownRouterBtn.interactable = multi;
    }

    // ── DNS list (Step 7) ─────────────────────────────────────────────────────

    private void OnResolveDNS()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || dnsServerNameInput == null) return;
        string name = dnsServerNameInput.text.Trim();
        if (string.Equals(name, state.ComputerName, System.StringComparison.OrdinalIgnoreCase)
            && IsOctetsValid(state.IPOctets))
        {
            if (dnsIPInput != null)
                dnsIPInput.text = string.Join(".", state.IPOctets);
        }
    }

    private void OnAddDNS()
    {
        string ip = dnsIPInput != null ? dnsIPInput.text.Trim() : "";
        if (!IsValidIPString(ip)) { Debug.LogWarning("[DHCP] DNS: invalid IP address."); return; }
        if (_dnsList.Contains(ip)) return;

        _dnsList.Add(ip);
        RefreshDNSListDisplay();
        RefreshDNSUpDown();
        if (dnsIPInput != null) dnsIPInput.text = "";
    }

    private void OnRemoveDNS()
    {
        if (_dnsList.Count == 0) return;
        _dnsList.RemoveAt(_dnsList.Count - 1);
        RefreshDNSListDisplay();
        RefreshDNSUpDown();
    }

    private void RefreshDNSListDisplay()
    {
        if (dnsListTMP != null) dnsListTMP.text = string.Join("\n", _dnsList);
    }

    private void RefreshDNSUpDown()
    {
        bool multi = _dnsList.Count > 1;
        if (moveUpDNSBtn   != null) moveUpDNSBtn.interactable   = multi;
        if (moveDownDNSBtn != null) moveDownDNSBtn.interactable = multi;
    }

    // ── WINS list (Step 8) ────────────────────────────────────────────────────

    private void OnResolveWINS()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || winsServerNameInput == null) return;
        string name = winsServerNameInput.text.Trim();
        if (string.Equals(name, state.ComputerName, System.StringComparison.OrdinalIgnoreCase)
            && IsOctetsValid(state.IPOctets))
        {
            if (winsIPInput != null)
                winsIPInput.text = string.Join(".", state.IPOctets);
        }
    }

    private void OnAddWINS()
    {
        string ip = winsIPInput != null ? winsIPInput.text.Trim() : "";
        if (!IsValidIPString(ip)) { Debug.LogWarning("[DHCP] WINS: invalid IP address."); return; }
        if (_winsList.Contains(ip)) return;

        _winsList.Add(ip);
        RefreshWINSListDisplay();
        RefreshWINSUpDown();
        if (winsIPInput != null) winsIPInput.text = "";
    }

    private void OnRemoveWINS()
    {
        if (_winsList.Count == 0) return;
        _winsList.RemoveAt(_winsList.Count - 1);
        RefreshWINSListDisplay();
        RefreshWINSUpDown();
    }

    private void RefreshWINSListDisplay()
    {
        if (winsListTMP != null) winsListTMP.text = string.Join("\n", _winsList);
    }

    private void RefreshWINSUpDown()
    {
        bool multi = _winsList.Count > 1;
        if (moveUpWINSBtn   != null) moveUpWINSBtn.interactable   = multi;
        if (moveDownWINSBtn != null) moveDownWINSBtn.interactable = multi;
    }

    // ── Toggle wiring ─────────────────────────────────────────────────────────

    private void WireConfigOptionsToggles()
    {
        yesConfigNowToggle?.onValueChanged.AddListener(on =>
        {
            if (on)  noConfigLaterToggle?.SetIsOnWithoutNotify(false);
            else if (noConfigLaterToggle != null && !noConfigLaterToggle.isOn)
                yesConfigNowToggle?.SetIsOnWithoutNotify(true);
        });
        noConfigLaterToggle?.onValueChanged.AddListener(on =>
        {
            if (on)  yesConfigNowToggle?.SetIsOnWithoutNotify(false);
            else if (yesConfigNowToggle != null && !yesConfigNowToggle.isOn)
                noConfigLaterToggle?.SetIsOnWithoutNotify(true);
        });
    }

    private void WireActivateToggles()
    {
        activateNowToggle?.onValueChanged.AddListener(on =>
        {
            if (on)  activateLaterToggle?.SetIsOnWithoutNotify(false);
            else if (activateLaterToggle != null && !activateLaterToggle.isOn)
                activateNowToggle?.SetIsOnWithoutNotify(true);
        });
        activateLaterToggle?.onValueChanged.AddListener(on =>
        {
            if (on)  activateNowToggle?.SetIsOnWithoutNotify(false);
            else if (activateNowToggle != null && !activateNowToggle.isOn)
                activateLaterToggle?.SetIsOnWithoutNotify(true);
        });
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private bool ValidateScopeName()
    {
        if (scopeNameInput == null || string.IsNullOrEmpty(scopeNameInput.text.Trim()))
        {
            Debug.LogWarning("[DHCP] Scope name is required.");
            return false;
        }
        return true;
    }

    private bool ValidateIPRange()
    {
        string start = startIPInput      != null ? startIPInput.text.Trim()      : "";
        string end   = endIPInput        != null ? endIPInput.text.Trim()        : "";
        string len   = subnetLengthInput != null ? subnetLengthInput.text.Trim() : "";
        string mask  = subnetMaskInput   != null ? subnetMaskInput.text.Trim()   : "";

        if (!IsValidIPString(start) || !IsValidIPString(end))
        {
            Debug.LogWarning("[DHCP] Valid Start IP and End IP are required.");
            return false;
        }
        if (string.IsNullOrEmpty(len) || string.IsNullOrEmpty(mask))
        {
            Debug.LogWarning("[DHCP] Subnet length and subnet mask are required.");
            return false;
        }
        if (IPToUint(start) > IPToUint(end))
        {
            Debug.LogWarning("[DHCP] Start IP must be less than or equal to End IP.");
            return false;
        }
        return true;
    }

    private bool ValidateLeaseDuration()
    {
        int days    = int.TryParse(leaseDaysInput?.text,    out int d) ? d : 0;
        int hours   = int.TryParse(leaseHoursInput?.text,   out int h) ? h : 0;
        int minutes = int.TryParse(leaseMinutesInput?.text, out int m) ? m : 0;
        if (days + hours + minutes <= 0)
        {
            Debug.LogWarning("[DHCP] Lease duration must be greater than zero.");
            return false;
        }
        return true;
    }

    private bool ValidateRouterList()
    {
        if (_routerList.Count == 0)
        {
            Debug.LogWarning("[DHCP] At least one router (default gateway) IP is required.");
            return false;
        }
        return true;
    }

    private bool ValidateDNSList()
    {
        if (_dnsList.Count == 0)
        {
            Debug.LogWarning("[DHCP] At least one DNS server IP is required.");
            return false;
        }
        return true;
    }

    // ── State persistence ─────────────────────────────────────────────────────

    private void SaveToState()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;

        state.DHCPScopeName        = scopeNameInput      != null ? scopeNameInput.text.Trim()      : "";
        state.DHCPScopeDescription = descriptionInput    != null ? descriptionInput.text.Trim()    : "";
        state.DHCPScopeStart       = startIPInput        != null ? startIPInput.text.Trim()        : "";
        state.DHCPScopeEnd         = endIPInput          != null ? endIPInput.text.Trim()          : "";
        state.DHCPSubnetLength     = subnetLengthInput   != null ? subnetLengthInput.text.Trim()   : "";
        state.DHCPSubnetMask       = subnetMaskInput     != null ? subnetMaskInput.text.Trim()     : "";
        state.DHCPSubnetDelay      = subnetDelayInput    != null ? subnetDelayInput.text.Trim()    : "0";
        state.DHCPExclusions       = new List<string>(_exclusionList);

        int.TryParse(leaseDaysInput?.text,    out state.DHCPLeaseDays);
        int.TryParse(leaseHoursInput?.text,   out state.DHCPLeaseHours);
        int.TryParse(leaseMinutesInput?.text, out state.DHCPLeaseMinutes);

        state.DHCPConfigureNow = _configureOptionsNow;

        state.DHCPRouterList   = new List<string>(_routerList);
        state.DHCPParentDomain = parentDomainInput != null ? parentDomainInput.text.Trim() : "";
        state.DHCPDNSList      = new List<string>(_dnsList);

        bool activateNow = activateNowToggle != null && activateNowToggle.isOn;
        state.DHCPScopeActive       = activateNow;
        state.DHCPActivatedOnFinish = activateNow;
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    private void ResetAllState()
    {
        _configureOptionsNow = true;
        _syncingSubnet       = false;

        _exclusionList.Clear();
        _routerList.Clear();
        _dnsList.Clear();
        _winsList.Clear();

        if (scopeNameInput      != null) scopeNameInput.text      = "";
        if (descriptionInput    != null) descriptionInput.text    = "";
        if (startIPInput        != null) startIPInput.text        = "";
        if (endIPInput          != null) endIPInput.text          = "";
        if (subnetLengthInput   != null) subnetLengthInput.text   = "";
        if (subnetMaskInput     != null) subnetMaskInput.text     = "";
        if (exclStartIPInput    != null) exclStartIPInput.text    = "";
        if (exclEndIPInput      != null) exclEndIPInput.text      = "";
        if (subnetDelayInput    != null) subnetDelayInput.text    = "";
        if (leaseDaysInput      != null) leaseDaysInput.text      = "";
        if (leaseHoursInput     != null) leaseHoursInput.text     = "";
        if (leaseMinutesInput   != null) leaseMinutesInput.text   = "";
        if (routerIPInput       != null) routerIPInput.text       = "";
        if (parentDomainInput   != null) parentDomainInput.text   = "";
        if (dnsServerNameInput  != null) dnsServerNameInput.text  = "";
        if (dnsIPInput          != null) dnsIPInput.text          = "";
        if (winsServerNameInput != null) winsServerNameInput.text = "";
        if (winsIPInput         != null) winsIPInput.text         = "";

        if (exclusionListTMP != null) exclusionListTMP.text = "";
        if (routerListTMP    != null) routerListTMP.text    = "";
        if (dnsListTMP       != null) dnsListTMP.text       = "";
        if (winsListTMP      != null) winsListTMP.text      = "";

        yesConfigNowToggle?.SetIsOnWithoutNotify(true);
        noConfigLaterToggle?.SetIsOnWithoutNotify(false);
        activateNowToggle?.SetIsOnWithoutNotify(true);
        activateLaterToggle?.SetIsOnWithoutNotify(false);

        RefreshRouterUpDown();
        RefreshDNSUpDown();
        RefreshWINSUpDown();
    }

    // ── Static helpers ────────────────────────────────────────────────────────

    private static bool IsValidIPString(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return false;
        string[] parts = ip.Split('.');
        if (parts.Length != 4) return false;
        foreach (string p in parts)
            if (!int.TryParse(p, out int n) || n < 0 || n > 255) return false;
        return true;
    }

    private static uint IPToUint(string ip)
    {
        uint result = 0;
        foreach (string p in ip.Split('.'))
        {
            result <<= 8;
            if (uint.TryParse(p, out uint b)) result |= b;
        }
        return result;
    }

    private static bool IsOctetsValid(string[] octets)
    {
        if (octets == null || octets.Length != 4) return false;
        foreach (string o in octets)
            if (string.IsNullOrEmpty(o)) return false;
        return true;
    }
}
