/*
 * ================================================================
 *  UNITY SETUP GUIDE — DHCPConsoleController (COC III)
 * ================================================================
 *
 *  COMPONENT PLACEMENT
 *    Add to "DHCP Console Panel". START ACTIVE in the scene so Awake fires;
 *    the script deactivates itself at the end of Awake.
 *    Opened via ServerManagerController Tools → DHCP.
 *
 * ────────────────────────────────────────────────────────────────
 *  MAIN HIERARCHY
 * ────────────────────────────────────────────────────────────────
 *
 *    DHCP Console Panel                  ← this script (DHCPConsoleController)
 *      ├── TitleBar
 *      │     └── CloseBtn               → closeBtn
 *      ├── TreePanel
 *      │     ├── ServerNodeLabel        (static TMP — no reference needed)
 *      │     ├── IPv4Group
 *      │     │     ├── IPv4NodeBtn      → ipv4NodeBtn
 *      │     │     └── ScopeNodesParent → scopeNodesParent
 *      │     │           (VerticalLayoutGroup + ContentSizeFitter Vertical=PreferredSize)
 *      │     │           [scope node prefabs are instantiated here at runtime]
 *      │     └── IPv6Group
 *      │           ├── IPv6NodeBtn      → ipv6NodeBtn
 *      │           └── IPv6StatusLabel  → ipv6StatusLabel  (TMP_Text)
 *      ├── ContentPanel
 *      │     ├── ScopesHeaderTMP        (static TMP — no reference needed)
 *      │     ├── ScopeListParent        → scopeListParent
 *      │     │     (VerticalLayoutGroup + ContentSizeFitter Vertical=PreferredSize)
 *      │     │     [scope row prefab is instantiated here at runtime]
 *      │     └── NoScopesLabel          → noScopesLabel  (starts ACTIVE)
 *      ├── ContextMenu                  → contextMenu  (starts INACTIVE)
 *      │     ├── NewScopeBtn            → ctxNewScopeBtn
 *      │     └── DisableIPv6Btn         → ctxDisableIPv6Btn
 *      └── New Scope Wizard Panel       → newScopeWizard
 *            (NewScopeWizardController — START ACTIVE, self-deactivates in its own Awake)
 *
 * ────────────────────────────────────────────────────────────────
 *  PREFAB 1 — scopeNodePrefab  (Tree Panel entry)
 * ────────────────────────────────────────────────────────────────
 *
 *  PURPOSE
 *    Appears as a sub-node under IPv4 in the tree when a scope is created.
 *    Mirrors zoneFileEntryPrefab used by DNSManagerController exactly.
 *    Read-only — no click handler needed.
 *
 *  HIERARCHY
 *    ScopeNode  [ROOT]
 *      ├── Spacer
 *      └── ScopeLabel
 *
 *  COMPONENTS & VALUES
 *
 *    ScopeNode  [ROOT]
 *      └── RectTransform
 *      └── HorizontalLayoutGroup
 *            Child Alignment      : Middle Left
 *            Spacing              : 4
 *            Child Control Width  : false
 *            Child Control Height : true
 *            Child Force Expand W : false
 *            Child Force Expand H : true
 *      └── LayoutElement
 *            Preferred Height     : 22
 *            Flexible Width       : 1
 *
 *    Spacer  [first child]
 *      └── RectTransform
 *      └── Image
 *            Color                : white, Alpha = 0  (fully transparent — invisible)
 *            Raycast Target       : false
 *      └── LayoutElement
 *            Preferred Width      : 20
 *            Flexible Width       : 0
 *
 *    ScopeLabel  [second child]   ← GetComponentInChildren<TMP_Text>() finds this
 *      └── RectTransform
 *      └── TextMeshProUGUI
 *            Text                 : ""  (set at runtime to scope name)
 *            Font Size            : 12
 *            Alignment            : Middle Left
 *            Overflow             : Ellipsis
 *            Color                : match tree text style
 *            Raycast Target       : false
 *      └── LayoutElement
 *            Flexible Width       : 1
 *
 * ────────────────────────────────────────────────────────────────
 *  PREFAB 2 — scopeRowPrefab  (Content Panel entry)
 * ────────────────────────────────────────────────────────────────
 *
 *  PURPOSE
 *    The right-side listing of the created scope. Shows scope name, active/inactive
 *    status, and an Activate button the student clicks to activate the scope.
 *    One instance exists at a time; destroyed and recreated on each RefreshContent call.
 *
 *  CHILD ORDER IS CRITICAL
 *    GetComponentsInChildren<TMP_Text>(true) reads by index:
 *      [0] = ScopeNameTMP  → receives scope name
 *      [1] = StatusTMP     → receives "Active" or "Inactive"
 *      [2] = ActivateBtnLabel (inside ActivateBtn) → never read, but present
 *    GetComponentsInChildren<Button>(true) reads by index:
 *      [0] = ActivateBtn   → wired to ActivateScope() at runtime
 *    Do NOT add any other TMP_Text or Button outside this order.
 *
 *  HIERARCHY
 *    ScopeRow  [ROOT]
 *      ├── ScopeNameTMP
 *      ├── StatusTMP
 *      └── ActivateBtn
 *            └── ActivateBtnLabel
 *
 *  COMPONENTS & VALUES
 *
 *    ScopeRow  [ROOT]
 *      └── RectTransform
 *      └── HorizontalLayoutGroup
 *            Child Alignment      : Middle Left
 *            Spacing              : 8
 *            Child Control Width  : false
 *            Child Control Height : true
 *            Child Force Expand W : false
 *            Child Force Expand H : true
 *      └── LayoutElement
 *            Preferred Height     : 28
 *            Flexible Width       : 1
 *
 *    ScopeNameTMP  [first child]   ← TMP index [0]
 *      └── RectTransform
 *      └── TextMeshProUGUI
 *            Text                 : ""  (set at runtime to scope name)
 *            Font Size            : 13
 *            Alignment            : Middle Left
 *            Overflow             : Ellipsis
 *            Raycast Target       : false
 *      └── LayoutElement
 *            Preferred Width      : 180
 *            Flexible Width       : 1
 *
 *    StatusTMP  [second child]     ← TMP index [1]
 *      └── RectTransform
 *      └── TextMeshProUGUI
 *            Text                 : ""  (set at runtime: "Active" or "Inactive")
 *            Font Size            : 13
 *            Alignment            : Middle Center
 *            Raycast Target       : false
 *      └── LayoutElement
 *            Preferred Width      : 80
 *            Flexible Width       : 0
 *
 *    ActivateBtn  [third child]    ← Button index [0]; starts INACTIVE in prefab
 *      └── RectTransform
 *      └── Image  (button background)
 *      └── Button  (no OnClick set in prefab — wired at runtime by RefreshContent)
 *      └── LayoutElement
 *            Preferred Width      : 80
 *            Flexible Width       : 0
 *
 *      └── ActivateBtnLabel  [child of ActivateBtn]   ← TMP index [2] (never read)
 *            └── RectTransform
 *            └── TextMeshProUGUI
 *                  Text           : "Activate"
 *                  Font Size      : 12
 *                  Alignment      : Middle Center
 *                  Raycast Target : false
 *
 *  IMPORTANT — ActivateBtn starts INACTIVE in the prefab.
 *    RefreshContent uses GetComponentsInChildren<Button>(true) — the (true) argument
 *    includes inactive children. Without it, ActivateBtn would never be found.
 *    The code then calls SetActive(!state.DHCPScopeActive) to show it when inactive.
 *
 * ────────────────────────────────────────────────────────────────
 *  INSPECTOR ASSIGNMENTS
 * ────────────────────────────────────────────────────────────────
 *    closeBtn
 *    ipv4NodeBtn, ipv6NodeBtn, ipv6StatusLabel, scopeNodesParent
 *    scopeListParent, scopeNodePrefab, scopeRowPrefab, noScopesLabel
 *    contextMenu, ctxNewScopeBtn, ctxDisableIPv6Btn
 *    newScopeWizard
 * ================================================================
 */

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DHCPConsoleController : MonoBehaviour
{
    public static DHCPConsoleController Instance { get; private set; }

    [Header("Nav")]
    [SerializeField] private Button closeBtn;

    [Header("Tree")]
    [SerializeField] private Button    ipv4NodeBtn;
    [SerializeField] private Button    ipv6NodeBtn;
    [SerializeField] private TMP_Text  ipv6StatusLabel;
    [SerializeField] private Transform scopeNodesParent;

    [Header("Content")]
    [SerializeField] private Transform  scopeListParent;
    [SerializeField] private GameObject scopeNodePrefab;
    [SerializeField] private GameObject scopeRowPrefab;
    [SerializeField] private GameObject noScopesLabel;

    [Header("Context Menu")]
    [SerializeField] private GameObject contextMenu;
    [SerializeField] private Button     ctxNewScopeBtn;
    [SerializeField] private Button     ctxDisableIPv6Btn;

    [Header("Wizard")]
    [SerializeField] private NewScopeWizardController newScopeWizard;

    private bool _scopeNodeSpawned = false;

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

        contextMenu?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.DHCPConsoleOpened = true;

        CloseContextMenu();

        if (state != null && state.DHCPScopeNameSet && !_scopeNodeSpawned)
            SpawnScopeNode(state);

        RefreshContent();
        RefreshLayout();
        ActivityLogManager.Log("Opened DHCP Console", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        CloseContextMenu();
        newScopeWizard?.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Called by NewScopeWizardController on Finish ──────────────────────────

    public void OnWizardComplete()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;

        SpawnScopeNode(state);
        RefreshContent();
        RefreshLayout();
    }

    // ── Tree node clicks ──────────────────────────────────────────────────────

    private void OnIPv4Clicked()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null && state.DHCPScopeNameSet) return;

        ctxNewScopeBtn?.gameObject.SetActive(true);
        ctxDisableIPv6Btn?.gameObject.SetActive(false);
        contextMenu?.SetActive(true);
    }

    private void OnIPv6Clicked()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null && state.DHCPv6Disabled) return;

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
        if (newScopeWizard == null)
        {
            Debug.LogWarning("[DHCPConsole] newScopeWizard is not assigned in the Inspector.");
            return;
        }
        newScopeWizard.Open();
    }

    // ── Scope activation (scope row Activate button) ──────────────────────────

    private void ActivateScope()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;
        state.DHCPScopeActive = true;
        RefreshContent();
        ActivityLogManager.Log("DHCP scope activated.", ActivityLogManager.EntryType.Action);
    }

    // ── Tree population ───────────────────────────────────────────────────────

    private void SpawnScopeNode(ServerDeviceState state)
    {
        if (_scopeNodeSpawned || scopeNodePrefab == null || scopeNodesParent == null) return;

        var node = Instantiate(scopeNodePrefab, scopeNodesParent);
        var tmp  = node.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) tmp.text = state.DHCPScopeName;

        _scopeNodeSpawned = true;
    }

    // ── Content refresh ───────────────────────────────────────────────────────

    private void RefreshContent()
    {
        var state     = ServerVirtualOSManager.Instance?.ServerState;
        bool hasScope = state != null && state.DHCPScopeNameSet;

        noScopesLabel?.SetActive(!hasScope);

        foreach (Transform child in scopeListParent) Destroy(child.gameObject);
        if (!hasScope || scopeRowPrefab == null) return;

        var row     = Instantiate(scopeRowPrefab, scopeListParent);
        var labels  = row.GetComponentsInChildren<TMP_Text>(true);
        if (labels.Length > 0) labels[0].text = state.DHCPScopeName;
        if (labels.Length > 1) labels[1].text = state.DHCPScopeActive ? "Active" : "Inactive";

        var buttons = row.GetComponentsInChildren<Button>(true);
        if (buttons.Length > 0)
        {
            buttons[0].gameObject.SetActive(!state.DHCPScopeActive);
            buttons[0].onClick.AddListener(ActivateScope);
        }
    }

    // ── Layout refresh ────────────────────────────────────────────────────────

    private void RefreshLayout() => StartCoroutine(RefreshLayoutCoroutine());

    private IEnumerator RefreshLayoutCoroutine()
    {
        yield return null;

        if (scopeNodesParent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)scopeNodesParent);

        var ipv4Group = scopeNodesParent?.parent as RectTransform;
        if (ipv4Group != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(ipv4Group);

        if (scopeListParent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)scopeListParent);
    }
}
