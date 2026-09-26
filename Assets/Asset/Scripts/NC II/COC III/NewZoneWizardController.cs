/*
 * ================================================================
 *  UNITY SETUP GUIDE — NewZoneWizardController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to the "New Zone Wizard Panel" GameObject (child of DNS Manager Panel, INACTIVE).
 *    DNSManagerController holds a reference and calls Open() when "New Zone" is clicked.
 *
 *  PANEL LIST (9 GameObjects, all start INACTIVE — shown one at a time)
 *
 *    New Zone Wizard Panel               ← this script here
 *      ├── WelcomePanel                  → welcomePanel
 *      │     ├── TMP (welcome line 1)    static text
 *      │     └── TMP (welcome line 2)    static text
 *      │
 *      ├── ZoneTypePanel                 → zoneTypePanel
 *      │     ├── PrimaryToggle           → primaryToggle
 *      │     ├── SecondaryToggle         → secondaryToggle
 *      │     ├── StubToggle              → stubToggle
 *      │     └── StoreInADToggle         → storeInADToggle  (interactable only when Primary selected)
 *      │
 *      ├── LookupChoicePanel             → lookupChoicePanel
 *      │     ├── ForwardToggle           → forwardToggle
 *      │     └── ReverseToggle           → reverseToggle
 *      │
 *      ├── ForwardZoneNamePanel          → forwardZoneNamePanel  (Forward path only)
 *      │     ├── OverviewTMP             static description text
 *      │     └── ZoneNameInput           → forwardZoneNameInput
 *      │
 *      ├── ReverseIPVersionPanel         → reverseIPVersionPanel  (Reverse path only)
 *      │     ├── IPv4Toggle              → ipv4Toggle
 *      │     └── IPv6Toggle              → ipv6Toggle
 *      │
 *      ├── ReverseNetworkIDPanel         → reverseNetworkIDPanel  (Reverse path only)
 *      │     ├── NetworkIDToggle         → networkIDToggle
 *      │     ├── NetworkIDInput          → networkIDInput  (IP-style text input)
 *      │     ├── ReverseZoneNameToggle   → reverseZoneNameToggle
 *      │     └── ReverseZoneNameInput    → reverseZoneNameInput
 *      │
 *      ├── ZoneFilePanel                 → zoneFilePanel  (SHARED — used by both paths)
 *      │     ├── CreateNewFileToggle     → createNewFileToggle
 *      │     ├── CreateNewFileInput      → createNewFileInput  (auto-populated: zonename.dns)
 *      │     ├── UseExistingFileToggle   → useExistingFileToggle
 *      │     └── UseExistingFileInput    → useExistingFileInput
 *      │
 *      ├── DynamicUpdatesPanel           → dynamicUpdatesPanel  (SHARED)
 *      │     ├── SecureOnlyToggle        → secureOnlyToggle  (always interactable = false)
 *      │     ├── BothUpdatesToggle       → bothUpdatesToggle
 *      │     └── NoUpdatesToggle         → noUpdatesToggle  (default ON)
 *      │
 *      └── SummaryPanel                 → summaryPanel  (SHARED)
 *            ├── SummaryTMP             → summaryTMP  (populated at runtime)
 *            └── FinishBtn              → finishBtn
 *
 *  PERSISTENT BUTTONS (outside all panels, always visible while wizard is open)
 *    PrevBtn    → prevBtn   (hidden on Welcome)
 *    NextBtn    → nextBtn   (hidden on Summary)
 *    CancelBtn  → cancelBtn (always visible)
 *
 *  STEP FLOW
 *    Welcome → ZoneType → LookupChoice
 *      → [Forward] ForwardZoneName → ZoneFile → DynamicUpdates → Summary
 *      → [Reverse] ReverseIPVersion → ReverseNetworkID → ZoneFile → DynamicUpdates → Summary
 *
 *  ZONE TYPE BEHAVIOUR
 *    Primary selected   → storeInADToggle.interactable = true
 *    Secondary or Stub  → storeInADToggle.interactable = false, unchecked
 *
 *  ZONE FILE BEHAVIOUR
 *    On entering ZoneFile panel, createNewFileInput auto-populates with "[zoneName].dns".
 *    createNewFileToggle is ON by default; switching to useExistingFileToggle clears the
 *    auto-fill and enables useExistingFileInput instead.
 *
 *  DYNAMIC UPDATES BEHAVIOUR
 *    secureOnlyToggle is always disabled (requires AD integration).
 *    bothUpdatesToggle and noUpdatesToggle are mutually exclusive.
 *    Default: noUpdatesToggle ON.
 *
 *  SUMMARY
 *    Populated on entering SummaryPanel. Shows Name, Type, Lookup Type, File Name.
 *    Type mapping: Primary → "Standard Primary", Secondary → "Standard Secondary", Stub → "Stub".
 *    File Name = createNewFileInput.text or useExistingFileInput.text depending on toggle.
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewZoneWizardController : MonoBehaviour
{
    private enum WizardPanel
    {
        Welcome,
        ZoneType,
        LookupChoice,
        ForwardZoneName,
        ReverseIPVersion,
        ReverseNetworkID,
        ZoneFile,
        DynamicUpdates,
        Summary
    }

    [Header("Panels")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private GameObject zoneTypePanel;
    [SerializeField] private GameObject lookupChoicePanel;
    [SerializeField] private GameObject forwardZoneNamePanel;
    [SerializeField] private GameObject reverseIPVersionPanel;
    [SerializeField] private GameObject reverseNetworkIDPanel;
    [SerializeField] private GameObject zoneFilePanel;
    [SerializeField] private GameObject dynamicUpdatesPanel;
    [SerializeField] private GameObject summaryPanel;

    [Header("Zone Type")]
    [SerializeField] private Toggle primaryToggle;
    [SerializeField] private Toggle secondaryToggle;
    [SerializeField] private Toggle stubToggle;
    [SerializeField] private Toggle storeInADToggle;

    [Header("Lookup Choice")]
    [SerializeField] private Toggle forwardToggle;
    [SerializeField] private Toggle reverseToggle;

    [Header("Forward — Zone Name")]
    [SerializeField] private TMP_InputField forwardZoneNameInput;

    [Header("Reverse — IP Version")]
    [SerializeField] private Toggle ipv4Toggle;
    [SerializeField] private Toggle ipv6Toggle;

    [Header("Reverse — Network ID")]
    [SerializeField] private Toggle          networkIDToggle;
    [SerializeField] private TMP_InputField  networkIDInput;
    [SerializeField] private Toggle          reverseZoneNameToggle;
    [SerializeField] private TMP_InputField  reverseZoneNameInput;

    [Header("Zone File (shared)")]
    [SerializeField] private Toggle          createNewFileToggle;
    [SerializeField] private TMP_InputField  createNewFileInput;
    [SerializeField] private Toggle          useExistingFileToggle;
    [SerializeField] private TMP_InputField  useExistingFileInput;

    [Header("Dynamic Updates (shared)")]
    [SerializeField] private Toggle secureOnlyToggle;
    [SerializeField] private Toggle bothUpdatesToggle;
    [SerializeField] private Toggle noUpdatesToggle;

    [Header("Summary (shared)")]
    [SerializeField] private TMP_Text summaryTMP;
    [SerializeField] private Button   finishBtn;

    [Header("Navigation Buttons (persistent)")]
    [SerializeField] private Button prevBtn;
    [SerializeField] private Button nextBtn;
    [SerializeField] private Button cancelBtn;

    // ── State ─────────────────────────────────────────────────────────────────

    private WizardPanel _currentPanel;
    private bool        _isForwardPath = true;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        prevBtn?.onClick.AddListener(GoBack);
        nextBtn?.onClick.AddListener(GoNext);
        cancelBtn?.onClick.AddListener(Cancel);
        finishBtn?.onClick.AddListener(Finish);

        WireZoneTypeToggles();
        WireLookupChoiceToggles();
        WireReverseIPVersionToggles();
        WireReverseNetworkIDToggles();
        WireZoneFileToggles();
        WireDynamicUpdatesToggle();

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
                ShowPanel(WizardPanel.ZoneType);
                break;
            case WizardPanel.ZoneType:
                ShowPanel(WizardPanel.LookupChoice);
                break;
            case WizardPanel.LookupChoice:
                _isForwardPath = forwardToggle != null && forwardToggle.isOn;
                ShowPanel(_isForwardPath ? WizardPanel.ForwardZoneName : WizardPanel.ReverseIPVersion);
                break;
            case WizardPanel.ForwardZoneName:
                ShowPanel(WizardPanel.ZoneFile);
                break;
            case WizardPanel.ReverseIPVersion:
                ShowPanel(WizardPanel.ReverseNetworkID);
                break;
            case WizardPanel.ReverseNetworkID:
                ShowPanel(WizardPanel.ZoneFile);
                break;
            case WizardPanel.ZoneFile:
                ShowPanel(WizardPanel.DynamicUpdates);
                break;
            case WizardPanel.DynamicUpdates:
                ShowPanel(WizardPanel.Summary);
                break;
        }
    }

    private void GoBack()
    {
        switch (_currentPanel)
        {
            case WizardPanel.ZoneType:
                ShowPanel(WizardPanel.Welcome);
                break;
            case WizardPanel.LookupChoice:
                ShowPanel(WizardPanel.ZoneType);
                break;
            case WizardPanel.ForwardZoneName:
                ShowPanel(WizardPanel.LookupChoice);
                break;
            case WizardPanel.ReverseIPVersion:
                ShowPanel(WizardPanel.LookupChoice);
                break;
            case WizardPanel.ReverseNetworkID:
                ShowPanel(WizardPanel.ReverseIPVersion);
                break;
            case WizardPanel.ZoneFile:
                ShowPanel(_isForwardPath ? WizardPanel.ForwardZoneName : WizardPanel.ReverseNetworkID);
                break;
            case WizardPanel.DynamicUpdates:
                ShowPanel(WizardPanel.ZoneFile);
                break;
            case WizardPanel.Summary:
                ShowPanel(WizardPanel.DynamicUpdates);
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
        DNSManagerController.Instance?.OnWizardComplete();
        ActivityLogManager.Log("DNS zone created: " + GetCurrentZoneName(), ActivityLogManager.EntryType.Action);
    }

    // ── Panel Display ─────────────────────────────────────────────────────────

    private void ShowPanel(WizardPanel panel)
    {
        welcomePanel?.SetActive(panel          == WizardPanel.Welcome);
        zoneTypePanel?.SetActive(panel         == WizardPanel.ZoneType);
        lookupChoicePanel?.SetActive(panel     == WizardPanel.LookupChoice);
        forwardZoneNamePanel?.SetActive(panel  == WizardPanel.ForwardZoneName);
        reverseIPVersionPanel?.SetActive(panel == WizardPanel.ReverseIPVersion);
        reverseNetworkIDPanel?.SetActive(panel == WizardPanel.ReverseNetworkID);
        zoneFilePanel?.SetActive(panel         == WizardPanel.ZoneFile);
        dynamicUpdatesPanel?.SetActive(panel   == WizardPanel.DynamicUpdates);
        summaryPanel?.SetActive(panel          == WizardPanel.Summary);

        _currentPanel = panel;

        if (panel == WizardPanel.ZoneFile)    OnEnterZoneFilePanel();
        if (panel == WizardPanel.Summary)     BuildSummary();

        RefreshButtons();
    }

    private void RefreshButtons()
    {
        bool isFirst = _currentPanel == WizardPanel.Welcome;
        bool isLast  = _currentPanel == WizardPanel.Summary;

        prevBtn?.gameObject.SetActive(!isFirst);
        nextBtn?.gameObject.SetActive(!isLast);
        finishBtn?.gameObject.SetActive(isLast);
        cancelBtn?.gameObject.SetActive(true);
    }

    // ── Zone File Panel ───────────────────────────────────────────────────────

    private void OnEnterZoneFilePanel()
    {
        string zoneName = GetCurrentZoneName();

        createNewFileToggle?.SetIsOnWithoutNotify(true);
        useExistingFileToggle?.SetIsOnWithoutNotify(false);

        if (createNewFileInput != null)
        {
            createNewFileInput.text          = string.IsNullOrEmpty(zoneName) ? "" : $"{zoneName}.dns";
            createNewFileInput.interactable  = true;
        }
        if (useExistingFileInput != null)
        {
            useExistingFileInput.text         = "";
            useExistingFileInput.interactable = false;
        }
    }

    // ── Summary Panel ─────────────────────────────────────────────────────────

    private void BuildSummary()
    {
        if (summaryTMP == null) return;

        string name       = GetCurrentZoneName();
        string type       = primaryToggle   != null && primaryToggle.isOn   ? "Standard Primary"
                          : secondaryToggle != null && secondaryToggle.isOn ? "Standard Secondary"
                          : "Stub";
        string lookupType = _isForwardPath ? "Forward lookup zone" : "Reverse lookup zone";
        string fileName   = createNewFileToggle != null && createNewFileToggle.isOn
                          ? (createNewFileInput  != null ? createNewFileInput.text  : "")
                          : (useExistingFileInput != null ? useExistingFileInput.text : "");

        summaryTMP.text =
            "The wizard will create a zone with the following settings:\n\n" +
            $"Name:           {name}\n" +
            $"Type:           {type}\n" +
            $"Lookup type:    {lookupType}\n" +
            $"File name:      {fileName}";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string GetCurrentZoneName()
    {
        if (_isForwardPath)
            return forwardZoneNameInput != null ? forwardZoneNameInput.text.Trim() : "";

        if (networkIDToggle != null && networkIDToggle.isOn)
            return networkIDInput != null ? networkIDInput.text.Trim() : "";

        return reverseZoneNameInput != null ? reverseZoneNameInput.text.Trim() : "";
    }

    private string GetZoneFileName()
    {
        if (createNewFileToggle != null && createNewFileToggle.isOn)
            return createNewFileInput != null ? createNewFileInput.text.Trim() : "";
        return useExistingFileInput != null ? useExistingFileInput.text.Trim() : "";
    }

    // ── State Persistence ─────────────────────────────────────────────────────

    private void SaveToState()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;

        string zoneName   = GetCurrentZoneName();
        string lookupType = _isForwardPath ? "Forward" : "Reverse";

        if (string.IsNullOrEmpty(zoneName)) return;

        // Enforce 5-zone-per-direction limit
        int existing = 0;
        foreach (var z in state.DNSZones)
            if (z.LookupType == lookupType) existing++;
        if (existing >= 5) return;

        // Prevent duplicate zone names within the same direction
        foreach (var z in state.DNSZones)
            if (z.LookupType == lookupType && z.ZoneName == zoneName) return;

        state.DNSZones.Add(new DNSZoneData
        {
            ZoneName       = zoneName,
            ZoneType       = primaryToggle   != null && primaryToggle.isOn   ? "Standard Primary"
                           : secondaryToggle != null && secondaryToggle.isOn ? "Standard Secondary"
                           : "Stub",
            LookupType     = lookupType,
            FileName       = GetZoneFileName(),
            StoreInAD      = storeInADToggle != null && storeInADToggle.isOn,
            DynamicUpdates = bothUpdatesToggle != null && bothUpdatesToggle.isOn ? "Both"
                           : noUpdatesToggle   != null && noUpdatesToggle.isOn   ? "DoNotAllow"
                           : "SecureOnly",
        });
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    private void ResetAllState()
    {
        _isForwardPath = true;

        primaryToggle?.SetIsOnWithoutNotify(true);
        secondaryToggle?.SetIsOnWithoutNotify(false);
        stubToggle?.SetIsOnWithoutNotify(false);
        if (storeInADToggle != null) { storeInADToggle.SetIsOnWithoutNotify(false); storeInADToggle.interactable = true; }

        forwardToggle?.SetIsOnWithoutNotify(true);
        reverseToggle?.SetIsOnWithoutNotify(false);

        if (forwardZoneNameInput != null) forwardZoneNameInput.text = "";

        ipv4Toggle?.SetIsOnWithoutNotify(true);
        ipv6Toggle?.SetIsOnWithoutNotify(false);

        networkIDToggle?.SetIsOnWithoutNotify(true);
        reverseZoneNameToggle?.SetIsOnWithoutNotify(false);
        if (networkIDInput != null)      { networkIDInput.text = "";      networkIDInput.interactable = true; }
        if (reverseZoneNameInput != null){ reverseZoneNameInput.text = ""; reverseZoneNameInput.interactable = false; }

        if (createNewFileInput   != null) createNewFileInput.text = "";
        if (useExistingFileInput != null) useExistingFileInput.text = "";

        if (secureOnlyToggle  != null) { secureOnlyToggle.SetIsOnWithoutNotify(false);  secureOnlyToggle.interactable = false; }
        bothUpdatesToggle?.SetIsOnWithoutNotify(false);
        noUpdatesToggle?.SetIsOnWithoutNotify(true);
    }

    // ── Toggle Wiring ─────────────────────────────────────────────────────────

    private void WireZoneTypeToggles()
    {
        primaryToggle?.onValueChanged.AddListener(on =>
        {
            if (!on) return;
            secondaryToggle?.SetIsOnWithoutNotify(false);
            stubToggle?.SetIsOnWithoutNotify(false);
            if (storeInADToggle != null) storeInADToggle.interactable = true;
        });
        secondaryToggle?.onValueChanged.AddListener(on =>
        {
            if (!on) return;
            primaryToggle?.SetIsOnWithoutNotify(false);
            stubToggle?.SetIsOnWithoutNotify(false);
            if (storeInADToggle != null) { storeInADToggle.interactable = false; storeInADToggle.SetIsOnWithoutNotify(false); }
        });
        stubToggle?.onValueChanged.AddListener(on =>
        {
            if (!on) return;
            primaryToggle?.SetIsOnWithoutNotify(false);
            secondaryToggle?.SetIsOnWithoutNotify(false);
            if (storeInADToggle != null) { storeInADToggle.interactable = false; storeInADToggle.SetIsOnWithoutNotify(false); }
        });
    }

    private void WireLookupChoiceToggles()
    {
        forwardToggle?.onValueChanged.AddListener(on =>
        {
            if (on) reverseToggle?.SetIsOnWithoutNotify(false);
            else if (reverseToggle != null && !reverseToggle.isOn) forwardToggle?.SetIsOnWithoutNotify(true);
        });
        reverseToggle?.onValueChanged.AddListener(on =>
        {
            if (on) forwardToggle?.SetIsOnWithoutNotify(false);
            else if (forwardToggle != null && !forwardToggle.isOn) reverseToggle?.SetIsOnWithoutNotify(true);
        });
    }

    private void WireReverseIPVersionToggles()
    {
        ipv4Toggle?.onValueChanged.AddListener(on =>
        {
            if (on) ipv6Toggle?.SetIsOnWithoutNotify(false);
            else if (ipv6Toggle != null && !ipv6Toggle.isOn) ipv4Toggle?.SetIsOnWithoutNotify(true);
        });
        ipv6Toggle?.onValueChanged.AddListener(on =>
        {
            if (on) ipv4Toggle?.SetIsOnWithoutNotify(false);
            else if (ipv4Toggle != null && !ipv4Toggle.isOn) ipv6Toggle?.SetIsOnWithoutNotify(true);
        });
    }

    private void WireReverseNetworkIDToggles()
    {
        networkIDToggle?.onValueChanged.AddListener(on =>
        {
            if (on)
            {
                reverseZoneNameToggle?.SetIsOnWithoutNotify(false);
                if (networkIDInput      != null) networkIDInput.interactable      = true;
                if (reverseZoneNameInput != null) reverseZoneNameInput.interactable = false;
            }
            else if (reverseZoneNameToggle != null && !reverseZoneNameToggle.isOn)
                networkIDToggle?.SetIsOnWithoutNotify(true);
        });
        reverseZoneNameToggle?.onValueChanged.AddListener(on =>
        {
            if (on)
            {
                networkIDToggle?.SetIsOnWithoutNotify(false);
                if (reverseZoneNameInput != null) reverseZoneNameInput.interactable = true;
                if (networkIDInput       != null) networkIDInput.interactable       = false;
            }
            else if (networkIDToggle != null && !networkIDToggle.isOn)
                reverseZoneNameToggle?.SetIsOnWithoutNotify(true);
        });
    }

    private void WireZoneFileToggles()
    {
        createNewFileToggle?.onValueChanged.AddListener(on =>
        {
            if (on)
            {
                useExistingFileToggle?.SetIsOnWithoutNotify(false);
                if (createNewFileInput   != null) createNewFileInput.interactable   = true;
                if (useExistingFileInput != null) useExistingFileInput.interactable = false;
            }
            else if (useExistingFileToggle != null && !useExistingFileToggle.isOn)
                createNewFileToggle?.SetIsOnWithoutNotify(true);
        });
        useExistingFileToggle?.onValueChanged.AddListener(on =>
        {
            if (on)
            {
                createNewFileToggle?.SetIsOnWithoutNotify(false);
                if (useExistingFileInput != null) useExistingFileInput.interactable = true;
                if (createNewFileInput   != null) createNewFileInput.interactable   = false;
            }
            else if (createNewFileToggle != null && !createNewFileToggle.isOn)
                useExistingFileToggle?.SetIsOnWithoutNotify(true);
        });
    }

    private void WireDynamicUpdatesToggle()
    {
        if (secureOnlyToggle != null) secureOnlyToggle.interactable = false;

        bothUpdatesToggle?.onValueChanged.AddListener(on =>
        {
            if (on) noUpdatesToggle?.SetIsOnWithoutNotify(false);
            else if (noUpdatesToggle != null && !noUpdatesToggle.isOn) bothUpdatesToggle?.SetIsOnWithoutNotify(true);
        });
        noUpdatesToggle?.onValueChanged.AddListener(on =>
        {
            if (on) bothUpdatesToggle?.SetIsOnWithoutNotify(false);
            else if (bothUpdatesToggle != null && !bothUpdatesToggle.isOn) noUpdatesToggle?.SetIsOnWithoutNotify(true);
        });
    }
}
