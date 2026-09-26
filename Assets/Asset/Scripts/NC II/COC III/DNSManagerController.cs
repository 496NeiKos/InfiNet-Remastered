/*
 * ================================================================
 *  UNITY SETUP GUIDE — DNSManagerController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "DNS Manager Panel" (starts ACTIVE, self-deactivates in Awake).
 *    Opened via ServerManagerController Tools → DNS Manager.
 *
 *  HIERARCHY  (match exactly — field names in brackets)
 *
 *    DNS Manager Panel                       ← this script
 *      ├── TitleBar
 *      │     ├── TitleTMP (static)
 *      │     └── CloseBtn (Button)           → closeBtn
 *      ├── MainArea (HorizontalLayoutGroup, child force expand H:ON)
 *      │     ├── TreePanel (fixed width ~220, VerticalLayoutGroup)
 *      │     │     └── TreeScrollView (ScrollView, flex height)
 *      │     │           └── Viewport
 *      │     │                 └── TreeContent
 *      │     │                       VerticalLayoutGroup, ContentSizeFitter Vertical=Preferred
 *      │     │                       anchor top-stretch, pivot (0.5,1)
 *      │     │                       → treeNodeParent
 *      │     └── ContentPanel (flexible, VerticalLayoutGroup)
 *      │           └── ContentScrollView (ScrollView, flex height)
 *      │                 └── Viewport
 *      │                       └── ContentListContent
 *      │                             VerticalLayoutGroup, ContentSizeFitter Vertical=Preferred
 *      │                             anchor top-stretch, pivot (0.5,1)
 *      │                             → contentListParent
 *      ├── ContextMenu (starts INACTIVE)     → contextMenu
 *      │     └── NewZoneBtn (Button)         → ctxNewZoneBtn
 *      └── New Zone Wizard Panel             → newZoneWizard
 *            (NewZoneWizardController — starts ACTIVE, self-deactivates in its own Awake)
 *
 *  PREFABS (assign in Inspector)
 *    treeNodePrefab    — shared with GroupPolicyController / PrintManagementController
 *    contentRowPrefab  — shared with DHCPConsoleController / ADUCController
 *
 *  INSPECTOR ASSIGNMENTS
 *    closeBtn          → CloseBtn
 *    treeNodeParent    → TreeContent Transform
 *    treeNodePrefab    → treeNodePrefab asset
 *    contentListParent → ContentListContent Transform
 *    contentRowPrefab  → contentRowPrefab asset
 *    contextMenu       → ContextMenu GO
 *    ctxNewZoneBtn     → NewZoneBtn
 *    newZoneWizard     → NewZoneWizardController on wizard panel
 *    selectedColor     → (0.2, 0.5, 0.9, 0.35) blue tint
 * ================================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DNSManagerController : MonoBehaviour
{
    public static DNSManagerController Instance { get; private set; }

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
    [SerializeField] private Button     ctxNewZoneBtn;

    [Header("Wizard")]
    [SerializeField] private NewZoneWizardController newZoneWizard;

    [Header("Colors")]
    [SerializeField] private Color selectedColor = new Color(0.2f, 0.5f, 0.9f, 0.35f);

    // ── Private state ─────────────────────────────────────────────────────────

    private const float IndentWidth = 16f;

    private readonly Dictionary<string, bool> _expanded      = new Dictionary<string, bool>();
    private string                            _selectedNodeId = "";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        closeBtn    ?.onClick.AddListener(Close);
        ctxNewZoneBtn?.onClick.AddListener(OnNewZoneClicked);

        contextMenu?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        _expanded.Clear();
        _expanded["Server"] = true;   // server node starts expanded
        _selectedNodeId     = "";

        contextMenu?.SetActive(false);

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.DNSManagerOpened = true;

        gameObject.SetActive(true);
        RebuildTree();
        RefreshContentPanel();

        ActivityLogManager.Log("Opened DNS Manager", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        contextMenu?.SetActive(false);
        gameObject.SetActive(false);
    }

    // Called by NewZoneWizardController on Finish
    public void OnWizardComplete()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || state.DNSZones.Count == 0) return;

        var newest = state.DNSZones[state.DNSZones.Count - 1];
        _expanded["Server"] = true;
        _expanded[newest.LookupType == "Forward" ? "L1:ForwardLZ" : "L1:ReverseLZ"] = true;

        RebuildTree();
        RefreshContentPanel();
        ActivityLogManager.Log("DNS zone created: " + newest.ZoneName, ActivityLogManager.EntryType.Action);
    }

    // ── Tree ──────────────────────────────────────────────────────────────────

    private void RebuildTree()
    {
        foreach (Transform t in treeNodeParent) Destroy(t.gameObject);

        var state      = ServerVirtualOSManager.Instance?.ServerState;
        string server  = state != null && !string.IsNullOrEmpty(state.ComputerName)
                         ? state.ComputerName : "SERVER";

        bool serverExp = GetExpanded("Server");
        SpawnNode("Server", server, 0,
            isLeaf: false, isExpanded: serverExp,
            isSelected: _selectedNodeId == "Server",
            leftClick: true, rightClick: true);

        if (!serverExp) { RebuildTreeLayout(); return; }

        // Forward Lookup Zones
        bool fwdHasChildren = state != null && state.DNSZones.Exists(z => z.LookupType == "Forward");
        bool fwdExp         = GetExpanded("L1:ForwardLZ");
        SpawnNode("L1:ForwardLZ", "Forward Lookup Zones", 1,
            isLeaf: !fwdHasChildren, isExpanded: fwdExp,
            isSelected: _selectedNodeId == "L1:ForwardLZ",
            leftClick: true, rightClick: false);

        if (fwdExp && state != null)
        {
            foreach (var z in state.DNSZones)
            {
                if (z.LookupType != "Forward") continue;
                string zid = "Zone:Forward:" + z.ZoneName;
                SpawnNode(zid, z.ZoneName, 2,
                    isLeaf: true, isExpanded: false,
                    isSelected: _selectedNodeId == zid,
                    leftClick: true, rightClick: false);
            }
        }

        // Reverse Lookup Zones
        bool revHasChildren = state != null && state.DNSZones.Exists(z => z.LookupType == "Reverse");
        bool revExp         = GetExpanded("L1:ReverseLZ");
        SpawnNode("L1:ReverseLZ", "Reverse Lookup Zones", 1,
            isLeaf: !revHasChildren, isExpanded: revExp,
            isSelected: _selectedNodeId == "L1:ReverseLZ",
            leftClick: true, rightClick: false);

        if (revExp && state != null)
        {
            foreach (var z in state.DNSZones)
            {
                if (z.LookupType != "Reverse") continue;
                string zid = "Zone:Reverse:" + z.ZoneName;
                SpawnNode(zid, z.ZoneName, 2,
                    isLeaf: true, isExpanded: false,
                    isSelected: _selectedNodeId == zid,
                    leftClick: true, rightClick: false);
            }
        }

        // Trust Points and Conditional Forwarders (always leaf)
        SpawnNode("L1:TrustPoints", "Trust Points", 1,
            isLeaf: true, isExpanded: false,
            isSelected: _selectedNodeId == "L1:TrustPoints",
            leftClick: true, rightClick: false);

        SpawnNode("L1:ConditionalForwarders", "Conditional Forwarders", 1,
            isLeaf: true, isExpanded: false,
            isSelected: _selectedNodeId == "L1:ConditionalForwarders",
            leftClick: true, rightClick: false);

        RebuildTreeLayout();
    }

    private void SpawnNode(string id, string label, int depth,
                           bool isLeaf, bool isExpanded, bool isSelected,
                           bool leftClick, bool rightClick)
    {
        var go = Instantiate(treeNodePrefab, treeNodeParent);
        var ui = go.GetComponent<TreeNodeUI>();
        if (ui == null) { Debug.LogError("[DNS] treeNodePrefab missing TreeNodeUI."); return; }

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

        if (id == "Server" || id == "L1:ForwardLZ" || id == "L1:ReverseLZ")
            _expanded[id] = !GetExpanded(id);

        _selectedNodeId = id;
        RebuildTree();
        RefreshContentPanel();
    }

    private void OnNodeRightClick(string id)
    {
        if (id != "Server") return;
        contextMenu?.SetActive(true);
    }

    private void OnNewZoneClicked()
    {
        contextMenu?.SetActive(false);
        if (newZoneWizard == null) { Debug.LogWarning("[DNS] newZoneWizard not assigned."); return; }
        newZoneWizard.Open();
    }

    // ── Content panel ─────────────────────────────────────────────────────────

    private void RefreshContentPanel()
    {
        foreach (Transform t in contentListParent) Destroy(t.gameObject);

        var state = ServerVirtualOSManager.Instance?.ServerState;

        switch (_selectedNodeId)
        {
            case "Server":
                SpawnContentRow("Forward Lookup Zones");
                SpawnContentRow("Reverse Lookup Zones");
                SpawnContentRow("Trust Points");
                SpawnContentRow("Conditional Forwarders");
                break;

            case "L1:ForwardLZ":
                if (state != null)
                    foreach (var z in state.DNSZones)
                        if (z.LookupType == "Forward")
                            SpawnContentRow(z.ZoneName, z.ZoneType);
                break;

            case "L1:ReverseLZ":
                if (state != null)
                    foreach (var z in state.DNSZones)
                        if (z.LookupType == "Reverse")
                            SpawnContentRow(z.ZoneName, z.ZoneType);
                break;

            // Trust Points, Conditional Forwarders, zone nodes: empty content
        }

        RebuildContentLayout();
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
