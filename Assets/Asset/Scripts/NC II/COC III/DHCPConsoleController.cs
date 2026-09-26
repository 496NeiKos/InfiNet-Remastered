/*
 * ================================================================
 *  UNITY SETUP GUIDE — DHCPConsoleController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "DHCP Console Panel" (starts ACTIVE, self-deactivates in Awake).
 *    Opened via ServerManagerController Tools → DHCP.
 *
 *  HIERARCHY  (match exactly)
 *
 *    DHCP Console Panel                      ← this script
 *      ├── TitleBar
 *      │     └── CloseBtn (Button)           → closeBtn
 *      ├── MainArea (HorizontalLayoutGroup, child force expand H:ON)
 *      │     ├── TreePanel (fixed width ~220, VerticalLayoutGroup)
 *      │     │     └── TreeScrollView (ScrollView)
 *      │     │           └── Viewport
 *      │     │                 └── TreeContent
 *      │     │                       VLG + ContentSizeFitter Vertical=Preferred
 *      │     │                       anchor top-stretch, pivot (0.5,1)
 *      │     │                       → treeNodeParent
 *      │     └── ContentPanel (flexible, VerticalLayoutGroup)
 *      │           └── ContentScrollView (ScrollView)
 *      │                 └── Viewport
 *      │                       └── ContentListContent
 *      │                             VLG + ContentSizeFitter Vertical=Preferred
 *      │                             anchor top-stretch, pivot (0.5,1)
 *      │                             → contentListParent
 *      ├── ContextMenu (starts INACTIVE)     → contextMenu
 *      │     ├── NewScopeBtn (Button)        → ctxNewScopeBtn
 *      │     └── AuthorizeBtn (Button)       → ctxAuthorizeBtn
 *      └── New Scope Wizard Panel            → newScopeWizard
 *            (NewScopeWizardController — starts ACTIVE, self-deactivates in its own Awake)
 *
 *  PREFABS
 *    treeNodePrefab   — shared
 *    contentRowPrefab — shared
 *
 *  INSPECTOR ASSIGNMENTS
 *    closeBtn          → CloseBtn
 *    treeNodeParent    → TreeContent Transform
 *    treeNodePrefab    → treeNodePrefab asset
 *    contentListParent → ContentListContent Transform
 *    contentRowPrefab  → contentRowPrefab asset
 *    contextMenu       → ContextMenu GO
 *    ctxNewScopeBtn    → NewScopeBtn
 *    ctxAuthorizeBtn   → AuthorizeBtn
 *    newScopeWizard    → NewScopeWizardController on wizard panel
 *    selectedColor     → (0.2, 0.5, 0.9, 0.35) blue tint
 * ================================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DHCPConsoleController : MonoBehaviour
{
    public static DHCPConsoleController Instance { get; private set; }

    [Header("Nav")]
    [SerializeField] private Button closeBtn;

    [Header("Tree")]
    [SerializeField] private Transform  treeNodeParent;
    [SerializeField] private GameObject treeNodePrefab;

    [Header("Content")]
    [SerializeField] private Transform  contentListParent;
    [SerializeField] private GameObject contentRowPrefab;

    [Header("Context Menu")]
    [SerializeField] private GameObject contextMenu;
    [SerializeField] private Button     ctxNewScopeBtn;
    [SerializeField] private Button     ctxAuthorizeBtn;

    [Header("Wizard")]
    [SerializeField] private NewScopeWizardController newScopeWizard;

    [Header("Colors")]
    [SerializeField] private Color selectedColor = new Color(0.2f, 0.5f, 0.9f, 0.35f);

    // ── Private state ─────────────────────────────────────────────────────────

    private const float IndentWidth = 16f;

    private readonly Dictionary<string, bool> _expanded       = new Dictionary<string, bool>();
    private string                            _selectedNodeId  = "";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        closeBtn       ?.onClick.AddListener(Close);
        ctxNewScopeBtn ?.onClick.AddListener(OnNewScopeClicked);
        ctxAuthorizeBtn?.onClick.AddListener(OnAuthorizeClicked);

        contextMenu?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        _expanded.Clear();
        _expanded["Server"] = true;
        _expanded["IPv4"]   = true;
        _selectedNodeId     = "";

        contextMenu?.SetActive(false);

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.DHCPConsoleOpened = true;

        gameObject.SetActive(true);
        RebuildTree();
        RefreshContentPanel();

        ActivityLogManager.Log("Opened DHCP Console", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        contextMenu?.SetActive(false);
        newScopeWizard?.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    // Called by NewScopeWizardController on Finish
    public void OnWizardComplete()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;

        _expanded["Server"] = true;
        _expanded["IPv4"]   = true;
        _selectedNodeId     = "IPv4";

        RebuildTree();
        RefreshContentPanel();
    }

    // ── Tree ──────────────────────────────────────────────────────────────────

    private void RebuildTree()
    {
        foreach (Transform t in treeNodeParent) Destroy(t.gameObject);

        var state = ServerVirtualOSManager.Instance?.ServerState;

        bool serverExp = GetExpanded("Server");
        SpawnNode("Server", BuildServerFQDN(state), 0,
            isLeaf: false, isExpanded: serverExp,
            isSelected: _selectedNodeId == "Server",
            leftClick: true, rightClick: true);

        if (!serverExp) { RebuildTreeLayout(); return; }

        // ── IPv4 ──────────────────────────────────────────────────────────────
        bool ipv4Exp = GetExpanded("IPv4");
        SpawnNode("IPv4", "IPv4", 1,
            isLeaf: false, isExpanded: ipv4Exp,
            isSelected: _selectedNodeId == "IPv4",
            leftClick: true, rightClick: true);

        if (ipv4Exp)
        {
            bool hasScope = state != null && state.DHCPScopeNameSet;
            if (hasScope)
            {
                string scopeId  = "Scope:" + state.DHCPScopeName;
                bool   scopeExp = GetExpanded(scopeId);
                SpawnNode(scopeId, state.DHCPScopeName, 2,
                    isLeaf: false, isExpanded: scopeExp,
                    isSelected: _selectedNodeId == scopeId,
                    leftClick: true, rightClick: false);

                if (scopeExp)
                {
                    SpawnScopeChild("Scope:" + state.DHCPScopeName + ":AddressPool",    "Address Pool",    state);
                    SpawnScopeChild("Scope:" + state.DHCPScopeName + ":AddressLeases",  "Address Leases",  state);
                    SpawnScopeChild("Scope:" + state.DHCPScopeName + ":Reservations",   "Reservations",    state);
                    SpawnScopeChild("Scope:" + state.DHCPScopeName + ":ScopeOptions",   "Scope Options",   state);
                    SpawnScopeChild("Scope:" + state.DHCPScopeName + ":Policies",       "Policies",        state);
                }
            }

            SpawnIPv4Static("IPv4:ServerOptions", "Server Options");
            SpawnIPv4Static("IPv4:Policies",      "Policies");
            SpawnIPv4Static("IPv4:Filters",       "Filters");
        }

        // ── IPv6 ──────────────────────────────────────────────────────────────
        bool ipv6Exp = GetExpanded("IPv6");
        SpawnNode("IPv6", "IPv6", 1,
            isLeaf: false, isExpanded: ipv6Exp,
            isSelected: _selectedNodeId == "IPv6",
            leftClick: true, rightClick: false);

        if (ipv6Exp)
        {
            SpawnIPv4Static("IPv6:ServerOptions", "Server Options");
            SpawnIPv4Static("IPv6:Policies",      "Policies");
            SpawnIPv4Static("IPv6:Filters",       "Filters");
        }

        RebuildTreeLayout();
    }

    private void SpawnScopeChild(string id, string label, ServerDeviceState state)
    {
        SpawnNode(id, label, 3,
            isLeaf: true, isExpanded: false,
            isSelected: _selectedNodeId == id,
            leftClick: true, rightClick: false);
    }

    private void SpawnIPv4Static(string id, string label)
    {
        SpawnNode(id, label, 2,
            isLeaf: true, isExpanded: false,
            isSelected: _selectedNodeId == id,
            leftClick: true, rightClick: false);
    }

    private void SpawnNode(string id, string label, int depth,
                           bool isLeaf, bool isExpanded, bool isSelected,
                           bool leftClick, bool rightClick)
    {
        var go = Instantiate(treeNodePrefab, treeNodeParent);
        var ui = go.GetComponent<TreeNodeUI>();
        if (ui == null) { Debug.LogError("[DHCP] treeNodePrefab missing TreeNodeUI."); return; }

        ui.indentSpacer.preferredWidth = depth * IndentWidth;
        ui.arrowLabel.gameObject.SetActive(!isLeaf);
        if (!isLeaf) ui.arrowLabel.text = isExpanded ? "▼" : "▶";
        ui.nodeLabel.text   = label;
        ui.background.color = isSelected ? selectedColor : Color.clear;

        if (!leftClick && !rightClick) return;

        var handler       = go.AddComponent<NodeClickHandler>();
        string capturedId = id;

        if (leftClick)  handler.onLeftClick  = () => OnNodeLeftClick(capturedId);
        if (rightClick) handler.onRightClick = () => OnNodeRightClick(capturedId);
    }

    // ── Tree interaction ──────────────────────────────────────────────────────

    private void OnNodeLeftClick(string id)
    {
        contextMenu?.SetActive(false);

        // Toggle expansion for non-leaf nodes
        if (id == "Server" || id == "IPv4" || id == "IPv6" || IsScopeRootId(id))
            _expanded[id] = !GetExpanded(id);

        _selectedNodeId = id;
        RebuildTree();
        RefreshContentPanel();
    }

    private void OnNodeRightClick(string id)
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;

        if (id == "Server")
        {
            if (state == null || state.DHCPAuthorized) return;
            ctxAuthorizeBtn?.gameObject.SetActive(true);
            ctxNewScopeBtn?.gameObject.SetActive(false);
            contextMenu?.SetActive(true);
        }
        else if (id == "IPv4")
        {
            if (state == null || state.DHCPScopeNameSet) return;
            ctxNewScopeBtn?.gameObject.SetActive(true);
            ctxAuthorizeBtn?.gameObject.SetActive(false);
            contextMenu?.SetActive(true);
        }
    }

    private void OnAuthorizeClicked()
    {
        contextMenu?.SetActive(false);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;
        state.DHCPAuthorized = true;
        RefreshContentPanel();
        ActivityLogManager.Log("DHCP Server authorized.", ActivityLogManager.EntryType.Action);
    }

    private void OnNewScopeClicked()
    {
        contextMenu?.SetActive(false);
        if (newScopeWizard == null) { Debug.LogWarning("[DHCP] newScopeWizard not assigned."); return; }
        newScopeWizard.Open();
    }

    // ── Content panel ─────────────────────────────────────────────────────────

    private void RefreshContentPanel()
    {
        foreach (Transform t in contentListParent) Destroy(t.gameObject);

        var state = ServerVirtualOSManager.Instance?.ServerState;

        if (_selectedNodeId == "Server")
        {
            bool auth  = state != null && state.DHCPAuthorized;
            bool ipv6d = state != null && state.DHCPv6Disabled;
            SpawnContentRow("IPv4", auth  ? "Active"   : "Disabled");
            SpawnContentRow("IPv6", ipv6d ? "Disabled" : "Active");
        }
        else if (_selectedNodeId == "IPv4")
        {
            if (state != null && state.DHCPScopeNameSet)
            {
                bool active = state.DHCPScopeActive;
                SpawnContentRow(state.DHCPScopeName, active ? "Active" : "Inactive",
                    showBtn: !active, btnLabel: "Activate",
                    onBtnClick: ActivateScope);
            }
        }
        else if (_selectedNodeId == "IPv6")
        {
            bool disabled = state != null && state.DHCPv6Disabled;
            SpawnContentRow("IPv6", disabled ? "Disabled" : "Active");
        }
        else if (state != null && _selectedNodeId == "Scope:" + state.DHCPScopeName)
        {
            SpawnContentRow(state.DHCPScopeName, state.DHCPScopeActive ? "Active" : "Inactive");
        }
        else if (state != null && _selectedNodeId == "Scope:" + state.DHCPScopeName + ":AddressPool")
        {
            if (!string.IsNullOrEmpty(state.DHCPScopeStart))
                SpawnContentRow("Start Address", state.DHCPScopeStart);
            if (!string.IsNullOrEmpty(state.DHCPScopeEnd))
                SpawnContentRow("End Address", state.DHCPScopeEnd);
            if (!string.IsNullOrEmpty(state.DHCPSubnetMask))
                SpawnContentRow("Subnet Mask", state.DHCPSubnetMask);
        }
        else if (state != null && _selectedNodeId == "Scope:" + state.DHCPScopeName + ":ScopeOptions")
        {
            foreach (string r in state.DHCPRouterList)
                if (!string.IsNullOrEmpty(r)) SpawnContentRow("003 Router", r);
            foreach (string d in state.DHCPDNSList)
                if (!string.IsNullOrEmpty(d)) SpawnContentRow("006 DNS Servers", d);
            if (!string.IsNullOrEmpty(state.DHCPParentDomain))
                SpawnContentRow("015 DNS Domain Name", state.DHCPParentDomain);
        }
        // All other IDs: empty content panel

        RebuildContentLayout();
    }

    private void ActivateScope()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;
        state.DHCPScopeActive = true;
        RefreshContentPanel();
        ActivityLogManager.Log("DHCP scope activated.", ActivityLogManager.EntryType.Action);
    }

    private void SpawnContentRow(string col1, string col2 = "",
                                  bool showBtn = false, string btnLabel = "",
                                  System.Action onBtnClick = null)
    {
        var go = Instantiate(contentRowPrefab, contentListParent);
        var ui = go.GetComponent<ContentRowUI>();
        if (ui == null) return;

        ui.col1TMP.text = col1;

        bool hasCol2 = !string.IsNullOrEmpty(col2);
        ui.col2TMP?.gameObject.SetActive(hasCol2);
        if (hasCol2 && ui.col2TMP != null) ui.col2TMP.text = col2;

        ui.actionBtn?.gameObject.SetActive(showBtn);
        if (showBtn)
        {
            if (ui.actionBtnLabel != null) ui.actionBtnLabel.text = btnLabel;
            if (onBtnClick != null)        ui.actionBtn?.onClick.AddListener(() => onBtnClick());
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool IsScopeRootId(string id)
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        return state != null && id == "Scope:" + state.DHCPScopeName;
    }

    private static string BuildServerFQDN(ServerDeviceState state)
    {
        string name   = state != null && !string.IsNullOrEmpty(state.ComputerName) ? state.ComputerName : "SERVER";
        string domain = state != null && !string.IsNullOrEmpty(state.DomainName)   ? state.DomainName   : "domain.local";
        return $"{name}.{domain}";
    }

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
