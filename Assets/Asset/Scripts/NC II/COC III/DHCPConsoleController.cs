/*
 * ================================================================
 *  UNITY SETUP GUIDE — DHCPConsoleController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "DHCP Console Panel" (starts INACTIVE).
 *    Opened from ServerManagerController Tools → DHCP.
 *
 *  HIERARCHY
 *    DHCP Console Panel                 ← this script here
 *      ├── TitleBar / CloseBtn          → closeBtn
 *      ├── TreePanel (left)
 *      │     ├── ServerNode (static label)
 *      │     ├── IPv4Node (Button)      → ipv4NodeBtn
 *      │     └── IPv6Node              (contains Button + status label)
 *      │           ├── IPv6Button       → ipv6NodeBtn
 *      │           └── IPv6StatusLabel  → ipv6StatusLabel
 *      ├── ContentPanel (right)
 *      │     ├── ScopesHeader (static label)
 *      │     ├── ScopeListParent        → scopeListParent  (VerticalLayoutGroup; empty at start)
 *      │     └── NoScopesLabel         → noScopesLabel     (shown when no scope exists)
 *      ├── Context Menu                 → contextMenu (starts INACTIVE)
 *      │     ├── NewScopeBtn           → ctxNewScopeBtn
 *      │     └── DisableIPv6Btn        → ctxDisableIPv6Btn
 *      └── New Scope Wizard            → scopeWizard (starts INACTIVE)
 *            ├── WizardStep0           → wizardStep0  (Scope Name step — starts ACTIVE inside wizard)
 *            │     ├── ScopeNameLabel  (static label "Scope Name:")
 *            │     ├── ScopeNameInput  → scopeNameInput  (TMP_InputField)
 *            │     └── NextBtn         → scopeNextBtn    (Button — advances to IP range step)
 *            └── WizardStep1           → wizardStep1  (IP Range step — starts INACTIVE)
 *                  ├── StartIPLabel    (static label "Start IP Address:")
 *                  ├── StartIPInput    → startIPInput    (TMP_InputField)
 *                  ├── EndIPLabel      (static label "End IP Address:")
 *                  ├── EndIPInput      → endIPInput      (TMP_InputField)
 *                  ├── BackBtn         → scopeBackBtn    (Button — returns to step 0)
 *                  └── FinishBtn       → scopeFinishBtn  (Button — creates scope)
 *
 *  SCOPE ROW PREFAB  (scopeRowPrefab)
 *    Contains (in order as children):
 *      ├── ScopeNameTMP   (TMP_Text — shows scope name;  index 0 in GetComponentsInChildren)
 *      ├── StatusTMP      (TMP_Text — shows Active/Inactive; index 1)
 *      └── ActivateBtn    (Button — wired at runtime; hidden once scope is active)
 *
 *  INSPECTOR ASSIGNMENTS
 *    closeBtn, ipv4NodeBtn, ipv6NodeBtn, ipv6StatusLabel
 *    scopeListParent, scopeRowPrefab, noScopesLabel
 *    contextMenu, ctxNewScopeBtn, ctxDisableIPv6Btn
 *    scopeWizard, wizardStep0, wizardStep1
 *    scopeNameInput, startIPInput, endIPInput
 *    scopeNextBtn, scopeBackBtn, scopeFinishBtn
 *
 *  HOW IT WORKS
 *    Click IPv4 node → context menu → "New Scope" → opens scopeWizard on step 0.
 *    Step 0: enter scope name → Next → step 1.
 *    Step 1: enter Start IP + End IP → Finish → scope row appears in ContentPanel (Inactive).
 *    Player clicks "Activate" on the scope row → scope becomes Active (task 43).
 *    Click IPv6 node → context menu → "Disable DHCPv6" → sets state flag + shows "Disabled" label.
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DHCPConsoleController : MonoBehaviour
{
    public static DHCPConsoleController Instance { get; private set; }

    [Header("Nav")]
    [SerializeField] private Button closeBtn;

    [Header("Tree")]
    [SerializeField] private Button ipv4NodeBtn;
    [SerializeField] private Button ipv6NodeBtn;
    [SerializeField] private TMP_Text ipv6StatusLabel;

    [Header("Content")]
    [SerializeField] private Transform  scopeListParent;
    [SerializeField] private GameObject scopeRowPrefab;
    [SerializeField] private GameObject noScopesLabel;

    [Header("Context Menu")]
    [SerializeField] private GameObject contextMenu;
    [SerializeField] private Button     ctxNewScopeBtn;
    [SerializeField] private Button     ctxDisableIPv6Btn;

    [Header("Scope Wizard")]
    [SerializeField] private GameObject     scopeWizard;
    [SerializeField] private TMP_InputField scopeNameInput;
    [SerializeField] private TMP_InputField startIPInput;
    [SerializeField] private TMP_InputField endIPInput;
    [SerializeField] private Button         scopeNextBtn;
    [SerializeField] private Button         scopeBackBtn;
    [SerializeField] private Button         scopeFinishBtn;

    [Header("Wizard steps")]
    [SerializeField] private GameObject wizardStep0; // name
    [SerializeField] private GameObject wizardStep1; // IP range

    private int _wizardStep = 0;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        closeBtn?.onClick.AddListener(Close);
        ipv4NodeBtn?.onClick.AddListener(OnIPv4Clicked);
        ipv6NodeBtn?.onClick.AddListener(OnIPv6Clicked);
        ctxNewScopeBtn?.onClick.AddListener(OpenScopeWizard);
        ctxDisableIPv6Btn?.onClick.AddListener(DisableIPv6);
        scopeNextBtn?.onClick.AddListener(WizardNext);
        scopeBackBtn?.onClick.AddListener(WizardBack);
        scopeFinishBtn?.onClick.AddListener(WizardFinish);

        contextMenu?.SetActive(false);
        scopeWizard?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.DHCPConsoleOpened = true;
        CloseContextMenu();
        RefreshContent();
        ActivityLogManager.Log("Opened DHCP Console", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        CloseContextMenu();
        scopeWizard?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Tree node clicks ──────────────────────────────────────────────────────

    private void OnIPv4Clicked()
    {
        ctxNewScopeBtn?.gameObject.SetActive(true);
        ctxDisableIPv6Btn?.gameObject.SetActive(false);
        contextMenu?.SetActive(true);
    }

    private void OnIPv6Clicked()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null && state.DHCPv6Disabled) return; // already disabled
        ctxNewScopeBtn?.gameObject.SetActive(false);
        ctxDisableIPv6Btn?.gameObject.SetActive(true);
        contextMenu?.SetActive(true);
    }

    private void CloseContextMenu() => contextMenu?.SetActive(false);

    // ── Disable DHCPv6 ────────────────────────────────────────────────────────

    private void DisableIPv6()
    {
        CloseContextMenu();
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.DHCPv6Disabled = true;
        if (ipv6StatusLabel != null) ipv6StatusLabel.text = "IPv6 [Disabled]";
        ActivityLogManager.Log("DHCPv6 disabled", ActivityLogManager.EntryType.Action);
    }

    // ── Scope wizard ──────────────────────────────────────────────────────────

    private void OpenScopeWizard()
    {
        CloseContextMenu();
        _wizardStep = 0;
        if (scopeNameInput != null) scopeNameInput.text = "";
        if (startIPInput   != null) startIPInput.text   = "";
        if (endIPInput     != null) endIPInput.text     = "";
        scopeWizard?.SetActive(true);
        ShowWizardStep(0);
    }

    private void WizardNext()
    {
        if (_wizardStep == 0)
        {
            string name = scopeNameInput != null ? scopeNameInput.text.Trim() : "";
            if (string.IsNullOrEmpty(name)) { Debug.LogWarning("[DHCP] Scope name required."); return; }
            var state = ServerVirtualOSManager.Instance?.ServerState;
            if (state != null) state.DHCPScopeName = name;
            ActivityLogManager.Log($"DHCP scope name entered: {name}", ActivityLogManager.EntryType.Action);
            ShowWizardStep(1);
        }
    }

    private void WizardBack()
    {
        if (_wizardStep > 0) ShowWizardStep(_wizardStep - 1);
    }

    private void WizardFinish()
    {
        string start = startIPInput != null ? startIPInput.text.Trim() : "";
        string end   = endIPInput   != null ? endIPInput.text.Trim()   : "";
        if (string.IsNullOrEmpty(start) || string.IsNullOrEmpty(end))
        {
            Debug.LogWarning("[DHCP] Start and end IP required.");
            return;
        }
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null)
        {
            state.DHCPScopeStart = start;
            state.DHCPScopeEnd   = end;
            // Scope is created but not yet active — player must click Activate on the scope row (task 43)
        }
        scopeWizard?.SetActive(false);
        RefreshContent();
        ActivityLogManager.Log($"DHCP scope created: {start} – {end}. Click Activate to enable it.", ActivityLogManager.EntryType.Action);
    }

    private void ActivateScope()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;
        state.DHCPScopeActive = true;
        RefreshContent();
        ActivityLogManager.Log("DHCP scope activated.", ActivityLogManager.EntryType.Action);
    }

    private void ShowWizardStep(int step)
    {
        _wizardStep = step;
        wizardStep0?.SetActive(step == 0);
        wizardStep1?.SetActive(step == 1);
        scopeNextBtn?.gameObject.SetActive(step == 0);
        scopeBackBtn?.gameObject.SetActive(step == 1);
        scopeFinishBtn?.gameObject.SetActive(step == 1);
    }

    // ── Content refresh ───────────────────────────────────────────────────────

    private void RefreshContent()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        bool hasScope = state != null && state.DHCPScopeNameSet;

        noScopesLabel?.SetActive(!hasScope);

        foreach (Transform child in scopeListParent) Destroy(child.gameObject);
        if (!hasScope || scopeRowPrefab == null) return;

        var row = Instantiate(scopeRowPrefab, scopeListParent);
        var labels = row.GetComponentsInChildren<TMP_Text>();
        if (labels.Length > 0) labels[0].text = state.DHCPScopeName;
        if (labels.Length > 1)
            labels[1].text = state.DHCPScopeActive ? "Active" : "Inactive";

        // Activate button on the scope row — visible only while scope is inactive
        var buttons = row.GetComponentsInChildren<Button>();
        if (buttons.Length > 0)
        {
            buttons[0].gameObject.SetActive(!state.DHCPScopeActive);
            buttons[0].onClick.AddListener(ActivateScope);
        }
    }
}
