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
 *      │     ├── ServerNode (label)
 *      │     ├── IPv4Node (Button)      → ipv4NodeBtn
 *      │     └── IPv6Node (Button)      → ipv6NodeBtn
 *      ├── ContentPanel (right)
 *      │     ├── ScopesHeader
 *      │     ├── ScopeRow (runtime)     (added after scope is created)
 *      │     └── NoScopesLabel         → noScopesLabel
 *      ├── Context Menu                 → contextMenu (starts INACTIVE)
 *      │     ├── NewScopeBtn           → ctxNewScopeBtn
 *      │     └── DisableIPv6Btn        → ctxDisableIPv6Btn
 *      └── New Scope Wizard            → scopeWizard (starts INACTIVE)
 *            ├── ScopeNameInput        → scopeNameInput
 *            ├── StartIPInput          → startIPInput
 *            ├── EndIPInput            → endIPInput
 *            ├── WizardNextBtn         → scopeNextBtn
 *            └── WizardFinishBtn       → scopeFinishBtn
 *
 *  INSPECTOR ASSIGNMENTS
 *    All fields as above. scopeWizard has simple two-step inline wizard:
 *      Step 0: ScopeName
 *      Step 1: IP Range (start + end)
 *      Finish: creates scope, activates it
 *
 *  SCOPE ROW PREFAB
 *    scopeRowPrefab → contains TMP_Text (scope name) + "Activate" Button +
 *                     status label. Wired at runtime.
 *
 *  HOW IT WORKS
 *    Right-clicking IPv4 node → context menu: New Scope.
 *    Right-clicking IPv6 node → context menu: Disable DHCPv6.
 *    New Scope wizard: name (step 0) → IP range (step 1) → Finish → scope created + activated.
 *    Disable DHCPv6: sets state.DHCPv6Disabled = true, shows "Disabled" label on IPv6 node.
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
            state.DHCPScopeStart  = start;
            state.DHCPScopeEnd    = end;
            state.DHCPScopeActive = true;
        }
        scopeWizard?.SetActive(false);
        RefreshContent();
        ActivityLogManager.Log($"DHCP scope created and activated: {start} – {end}", ActivityLogManager.EntryType.Action);
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
    }
}
