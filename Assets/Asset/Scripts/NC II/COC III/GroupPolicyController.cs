/*
 * ================================================================
 *  UNITY SETUP GUIDE — GroupPolicyController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Group Policy Management Panel" (starts INACTIVE).
 *    Opened from ServerManagerController Tools → Group Policy Management.
 *
 *  HIERARCHY
 *    Group Policy Management Panel            ← this script here
 *      ├── TitleBar (HorizontalLayoutGroup)
 *      │     ├── TitleLabel (TMP_Text)         "Group Policy Management"
 *      │     └── CloseBtn (Button)             → closeBtn
 *      ├── MainArea (HorizontalLayoutGroup, child force expand H:ON)
 *      │     ├── TreePanel (fixed width ~220, VerticalLayoutGroup)
 *      │     │     └── TreeScrollView (ScrollView)
 *      │     │           └── Viewport
 *      │     │                 └── TreeContent (VerticalLayoutGroup + ContentSizeFitter)
 *      │     │                       anchor: top-stretch, pivot (0.5,1)
 *      │     │                       ContentSizeFitter: Vertical=Preferred Size
 *      │     │                       ← tree rows spawn here → treeNodeParent
 *      │     └── RightPanel (flexible, VerticalLayoutGroup)
 *      │           ├── ContentHeader (HorizontalLayoutGroup, height 24) — STATIC
 *      │           │     Eight TMP_Text labels in order:
 *      │           │     "Link Order" (w:50) | "GPO" (flexible) | "Enforced" (w:70)
 *      │           │     "Link Enabled" (w:90) | "GPO Status" (w:80) | "WMI Filter" (w:80)
 *      │           │     "Modified" (w:120) | "Domain" (w:100)
 *      │           │     HLG: childControlWidth ON, childForceExpandWidth OFF
 *      │           │     Each label: LayoutElement with matching preferredWidth
 *      │           │     (GPO label: LayoutElement flexibleWidth=1)
 *      │           └── ContentScrollView (ScrollView, fills remaining height)
 *      │                 └── Viewport
 *      │                       └── ContentListContent (VerticalLayoutGroup + ContentSizeFitter)
 *      │                             ← GPO rows spawn here → contentListParent
 *      ├── ContextMenu                        → contextMenu  (SharedContextMenuController)
 *      │     (see SharedContextMenuController setup guide — LAST sibling in panel)
 *      ├── Create GPO Dialog (starts INACTIVE) → createGPODialog
 *      │     Image panel, centered, ~300×160
 *      │     ├── Title (TMP_Text) "New GPO"
 *      │     ├── NameLabel (TMP_Text) "Name:"
 *      │     ├── GPONameInput (TMP_InputField)  → gpoNameInput
 *      │     ├── OKBtn (Button)                → gpoOKBtn
 *      │     └── CancelBtn (Button)            → gpoCancelBtn
 *      └── GPO Editor Panel (starts INACTIVE)  (GPOEditorController — separate script)
 *            ← must be placed BEFORE ContextMenu in sibling order
 *
 *  PREFABS NEEDED
 *
 *  treeNodePrefab
 *    Root GO — Image (color:clear, raycastTarget:ON) + TreeNodeUI (assign all fields)
 *    Height: 26px (LayoutElement preferredHeight:26)
 *    └── HLG (HorizontalLayoutGroup child, RectTransform stretch-stretch, offsets 0)
 *          childControlWidth:ON, childForceExpandWidth:OFF, spacing:4
 *          ├── IndentSpacer — LayoutElement (preferredWidth:0, set at runtime)
 *          │     No Image, no TMP. Just a LayoutElement placeholder.
 *          ├── ArrowLabel — TMP_Text, LayoutElement preferredWidth:16
 *          │     Font size:12, text:"▶", alignment:center
 *          │     TreeNodeUI.arrowLabel → this
 *          └── NodeLabel — TMP_Text, LayoutElement flexibleWidth:1
 *                Font size:12, overflow:Truncate, alignment:left-middle
 *                TreeNodeUI.nodeLabel → this
 *    Inspector assignments on TreeNodeUI:
 *      background    → root GO's Image component
 *      indentSpacer  → IndentSpacer's LayoutElement
 *      arrowLabel    → ArrowLabel's TMP_Text
 *      nodeLabel     → NodeLabel's TMP_Text
 *
 *  gpoRowPrefab
 *    Root GO — Image (color:clear, raycastTarget:ON) + GPORowUI (assign all fields)
 *    Height: 26px (LayoutElement preferredHeight:26)
 *    └── HLG (HorizontalLayoutGroup child, stretch-stretch)
 *          childControlWidth:ON, childForceExpandWidth:OFF, spacing:0
 *          ├── Col_LinkOrder  — TMP_Text, LayoutElement preferredWidth:50
 *          ├── Col_GPO        — TMP_Text, LayoutElement flexibleWidth:1
 *          ├── Col_Enforced   — TMP_Text, LayoutElement preferredWidth:70
 *          ├── Col_LinkEnabled— TMP_Text, LayoutElement preferredWidth:90
 *          ├── Col_Status     — TMP_Text, LayoutElement preferredWidth:80
 *          ├── Col_WMIFilter  — TMP_Text, LayoutElement preferredWidth:80
 *          ├── Col_Modified   — TMP_Text, LayoutElement preferredWidth:120
 *          └── Col_Domain     — TMP_Text, LayoutElement preferredWidth:100
 *    Inspector assignments on GPORowUI: background + all 8 col_ TMP_Texts
 *
 *  INSPECTOR ASSIGNMENTS (on GroupPolicyController)
 *    closeBtn          → CloseBtn
 *    treeNodeParent    → TreeContent Transform
 *    treeNodePrefab    → treeNodePrefab
 *    contentListParent → ContentListContent Transform
 *    gpoRowPrefab      → gpoRowPrefab
 *    contextMenu       → SharedContextMenuController on ContextMenu GO
 *    createGPODialog   → Create GPO Dialog GO
 *    gpoNameInput      → GPONameInput
 *    gpoOKBtn          → OKBtn
 *    gpoCancelBtn      → CancelBtn
 *    gpoEditor         → GPOEditorController on GPO Editor Panel
 *    selectedColor     → (0.2, 0.5, 0.9, 0.35) blue tint
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GroupPolicyController : MonoBehaviour
{
    public static GroupPolicyController Instance { get; private set; }

    // ── Serialized fields ─────────────────────────────────────────────────────

    [Header("Nav")]
    [SerializeField] private Button closeBtn;

    [Header("Tree")]
    [SerializeField] private Transform  treeNodeParent;
    [SerializeField] private GameObject treeNodePrefab;

    [Header("Content")]
    [SerializeField] private Transform  contentListParent;
    [SerializeField] private GameObject gpoRowPrefab;

    [Header("Context Menu")]
    [SerializeField] private SharedContextMenuController contextMenu;

    [Header("Create GPO Dialog")]
    [SerializeField] private GameObject     createGPODialog;
    [SerializeField] private TMP_InputField gpoNameInput;
    [SerializeField] private Button         gpoOKBtn;
    [SerializeField] private Button         gpoCancelBtn;

    [Header("GPO Editor")]
    [SerializeField] private GPOEditorController gpoEditor;

    [Header("Colors")]
    [SerializeField] private Color selectedColor = new Color(0.2f, 0.5f, 0.9f, 0.35f);

    // ── Private state ─────────────────────────────────────────────────────────

    private enum SelectionType { None, OU, GPO }

    private const float IndentWidth = 16f;

    private static readonly string[] StaticLeaves =
        { "Default Domain Policy", "Domain Controllers" };

    private readonly Dictionary<string, bool> _expanded = new Dictionary<string, bool>();

    private SelectionType _selType       = SelectionType.None;
    private string        _selectedOU    = "";
    private int           _selectedGPO   = -1;
    private string        _pendingOU     = "";
    private int           _pendingGPO    = -1;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        closeBtn    ?.onClick.AddListener(Close);
        gpoOKBtn    ?.onClick.AddListener(ConfirmCreateGPO);
        gpoCancelBtn?.onClick.AddListener(() => createGPODialog?.SetActive(false));

        createGPODialog?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.GPMOpened = true;

        contextMenu?.Hide();
        createGPODialog?.SetActive(false);
        RebuildTree();
        RefreshContentPanel();

        ActivityLogManager.Log("Opened Group Policy Management", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        contextMenu?.Hide();
        createGPODialog?.SetActive(false);
        gpoEditor?.Close();
        gameObject.SetActive(false);
    }

    /// <summary>Called by GPOEditorController when its panel closes.</summary>
    public void OnGPOEditorClosed()
    {
        RebuildTree();
        RefreshContentPanel();
    }

    // ── Tree ──────────────────────────────────────────────────────────────────

    private void RebuildTree()
    {
        foreach (Transform t in treeNodeParent) Destroy(t.gameObject);

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) { RebuildTreeLayout(); return; }

        string domainName = !string.IsNullOrEmpty(state.DomainName) ? state.DomainName : "tesda.com";

        // Level 0 — Forest
        bool forestExp = GetExpanded("Forest");
        SpawnNode("Forest", $"Forest: {domainName}", 0,
            isLeaf: false, isExpanded: forestExp, isSelected: false,
            leftClick: true, rightClick: false);

        if (!forestExp) { RebuildTreeLayout(); return; }

        // Level 1 — Domain
        bool domainExp = GetExpanded("Domain");
        SpawnNode("Domain", domainName, 1,
            isLeaf: false, isExpanded: domainExp, isSelected: false,
            leftClick: true, rightClick: false);

        if (!domainExp) { RebuildTreeLayout(); return; }

        // Level 2 — Static leaves
        foreach (var name in StaticLeaves)
            SpawnNode("Static_" + name, name, 2,
                isLeaf: true, isExpanded: false, isSelected: false,
                leftClick: false, rightClick: false);

        // Level 2 — OU nodes (dynamic)
        foreach (var ou in state.OrganizationalUnits)
        {
            string ouId = "OU:" + ou.Name;
            bool ouExp  = GetExpanded(ouId);
            bool ouSel  = _selType == SelectionType.OU && _selectedOU == ou.Name;

            SpawnNode(ouId, ou.Name, 2,
                isLeaf: false, isExpanded: ouExp, isSelected: ouSel,
                leftClick: true, rightClick: true);

            if (!ouExp) continue;

            // Level 3 — GPO nodes (children of this OU)
            for (int i = 0; i < state.GroupPolicies.Count; i++)
            {
                if (state.GroupPolicies[i].LinkedOUName != ou.Name) continue;

                string gpoId = "GPO:" + i;
                bool gpoSel  = _selType == SelectionType.GPO && _selectedGPO == i;

                SpawnNode(gpoId, state.GroupPolicies[i].Name, 3,
                    isLeaf: true, isExpanded: false, isSelected: gpoSel,
                    leftClick: true, rightClick: true);
            }
        }

        RebuildTreeLayout();
    }

    private void SpawnNode(string id, string label, int depth,
                           bool isLeaf, bool isExpanded, bool isSelected,
                           bool leftClick, bool rightClick)
    {
        var go  = Instantiate(treeNodePrefab, treeNodeParent);
        var ui  = go.GetComponent<TreeNodeUI>();
        if (ui == null) { Debug.LogError("[GPMC] treeNodePrefab missing TreeNodeUI."); return; }

        ui.indentSpacer.preferredWidth = depth * IndentWidth;
        ui.arrowLabel.gameObject.SetActive(!isLeaf);
        if (!isLeaf) ui.arrowLabel.text = isExpanded ? "▼" : "▶";
        ui.nodeLabel.text  = label;
        ui.background.color = isSelected ? selectedColor : Color.clear;

        if (!leftClick && !rightClick) return;

        var handler        = go.AddComponent<NodeClickHandler>();
        string capturedId  = id;

        if (leftClick)
            handler.onLeftClick = () => OnNodeLeftClick(capturedId);

        if (rightClick)
            handler.onRightClickWithPos = screenPos => OnNodeRightClick(capturedId, screenPos);
    }

    // ── Tree interaction ──────────────────────────────────────────────────────

    private void OnNodeLeftClick(string id)
    {
        contextMenu?.Hide();

        if (id.StartsWith("OU:"))
        {
            string ouName   = id.Substring(3);
            _selType        = SelectionType.OU;
            _selectedOU     = ouName;
            _selectedGPO    = -1;
            _expanded[id]   = !GetExpanded(id);
        }
        else if (id.StartsWith("GPO:"))
        {
            int idx         = int.Parse(id.Substring(4));
            _selType        = SelectionType.GPO;
            _selectedGPO    = idx;

            var state = ServerVirtualOSManager.Instance?.ServerState;
            if (state != null && idx < state.GroupPolicies.Count)
                _selectedOU = state.GroupPolicies[idx].LinkedOUName;
        }
        else
        {
            // Forest / Domain — toggle expand only, no content selection change
            _expanded[id] = !GetExpanded(id);
        }

        RebuildTree();
        RefreshContentPanel();
    }

    private void OnNodeRightClick(string id, Vector2 screenPos)
    {
        var panelRect = transform as RectTransform;

        if (id.StartsWith("OU:"))
        {
            _pendingOU = id.Substring(3);
            contextMenu?.ShowForOU(screenPos, panelRect, OpenCreateGPODialog);
        }
        else if (id.StartsWith("GPO:"))
        {
            _pendingGPO = int.Parse(id.Substring(4));
            var state = ServerVirtualOSManager.Instance?.ServerState;
            bool enforced = state != null && _pendingGPO < state.GroupPolicies.Count
                            && state.GroupPolicies[_pendingGPO].Enforced;

            contextMenu?.ShowForGPO(screenPos, panelRect, enforced,
                onEdit:     () => OpenGPOEditor(_pendingGPO),
                onEnforced: () => ToggleEnforced(_pendingGPO));
        }
    }

    // ── Content panel ─────────────────────────────────────────────────────────

    private void RefreshContentPanel()
    {
        foreach (Transform t in contentListParent) Destroy(t.gameObject);

        if (_selType == SelectionType.None) { RebuildContentLayout(); return; }

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) { RebuildContentLayout(); return; }

        // Both OU and GPO selection show the same OU's GPO list
        string ouName = _selType == SelectionType.OU ? _selectedOU
                      : (_selectedGPO >= 0 && _selectedGPO < state.GroupPolicies.Count
                         ? state.GroupPolicies[_selectedGPO].LinkedOUName : "");

        int linkOrder = 1;
        for (int i = 0; i < state.GroupPolicies.Count; i++)
        {
            var gpo = state.GroupPolicies[i];
            if (gpo.LinkedOUName != ouName) continue;

            bool isHighlighted = _selType == SelectionType.GPO && _selectedGPO == i;
            SpawnGPORow(gpo, i, linkOrder++, isHighlighted, state.DomainName);
        }

        RebuildContentLayout();
    }

    private void SpawnGPORow(GPOData gpo, int gpoIndex, int linkOrder,
                             bool isHighlighted, string domainName)
    {
        var go  = Instantiate(gpoRowPrefab, contentListParent);
        var ui  = go.GetComponent<GPORowUI>();
        if (ui == null) { Debug.LogError("[GPMC] gpoRowPrefab missing GPORowUI."); return; }

        ui.col_linkOrder  .text = linkOrder.ToString();
        ui.col_gpo        .text = gpo.Name;
        ui.col_enforced   .text = gpo.Enforced ? "Yes" : "No";
        ui.col_linkEnabled.text = "Yes";
        ui.col_status     .text = "Enabled";
        ui.col_wmiFilter  .text = "None";
        ui.col_modified   .text = !string.IsNullOrEmpty(gpo.ModifiedDate)
                                  ? gpo.ModifiedDate
                                  : System.DateTime.Now.ToString("M/d/yyyy");
        ui.col_domain     .text = domainName;
        ui.background.color     = isHighlighted ? selectedColor : Color.clear;

        int capturedIdx = gpoIndex;
        var handler = go.AddComponent<NodeClickHandler>();
        handler.onLeftClick = () =>
        {
            _selType     = SelectionType.GPO;
            _selectedGPO = capturedIdx;
            var s = ServerVirtualOSManager.Instance?.ServerState;
            if (s != null && capturedIdx < s.GroupPolicies.Count)
                _selectedOU = s.GroupPolicies[capturedIdx].LinkedOUName;
            RebuildTree();
            RefreshContentPanel();
        };
        handler.onRightClickWithPos = screenPos =>
        {
            _pendingGPO = capturedIdx;
            var s = ServerVirtualOSManager.Instance?.ServerState;
            bool enforced = s != null && capturedIdx < s.GroupPolicies.Count
                            && s.GroupPolicies[capturedIdx].Enforced;
            contextMenu?.ShowForGPO(screenPos, transform as RectTransform, enforced,
                onEdit:     () => OpenGPOEditor(capturedIdx),
                onEnforced: () => ToggleEnforced(capturedIdx));
        };
    }

    // ── Create GPO ────────────────────────────────────────────────────────────

    private void OpenCreateGPODialog()
    {
        if (gpoNameInput != null) gpoNameInput.text = "";
        createGPODialog?.SetActive(true);
    }

    private void ConfirmCreateGPO()
    {
        string name = gpoNameInput != null ? gpoNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(name)) { Debug.LogWarning("[GPMC] GPO name required."); return; }

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;

        int order = state.GroupPolicies.Count + 1;
        state.GroupPolicies.Add(new GPOData
        {
            Name         = name,
            LinkedOUName = _pendingOU,
            LinkOrder    = order,
            ModifiedDate = System.DateTime.Now.ToString("M/d/yyyy")
        });

        createGPODialog?.SetActive(false);

        // Auto-expand the OU so the new GPO node is visible
        string ouId = "OU:" + _pendingOU;
        _expanded[ouId] = true;
        _selType        = SelectionType.OU;
        _selectedOU     = _pendingOU;

        RebuildTree();
        RefreshContentPanel();

        ActivityLogManager.Log($"Created GPO: {name} linked to {_pendingOU}", ActivityLogManager.EntryType.Action);
    }

    // ── Open editor ───────────────────────────────────────────────────────────

    private void OpenGPOEditor(int gpoIndex)
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || gpoIndex < 0 || gpoIndex >= state.GroupPolicies.Count) return;

        state.GroupPolicies[gpoIndex].EditorOpened = true;
        gpoEditor?.OpenForGPO(gpoIndex);

        ActivityLogManager.Log(
            $"Opened GPO Editor: {state.GroupPolicies[gpoIndex].Name}",
            ActivityLogManager.EntryType.Action);
    }

    // ── Enforced toggle ───────────────────────────────────────────────────────

    private void ToggleEnforced(int gpoIndex)
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || gpoIndex < 0 || gpoIndex >= state.GroupPolicies.Count) return;

        var gpo     = state.GroupPolicies[gpoIndex];
        gpo.Enforced = !gpo.Enforced;
        gpo.ModifiedDate = System.DateTime.Now.ToString("M/d/yyyy");

        RebuildTree();
        RefreshContentPanel();

        ActivityLogManager.Log(
            $"GPO '{gpo.Name}' Enforced → {(gpo.Enforced ? "Yes" : "No")}",
            ActivityLogManager.EntryType.Action);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool GetExpanded(string id)
    {
        _expanded.TryGetValue(id, out bool val);
        return val;
    }

    private void RebuildTreeLayout()
    {
        var rt = treeNodeParent as RectTransform;
        if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private void RebuildContentLayout()
    {
        var rt = contentListParent as RectTransform;
        if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }
}
