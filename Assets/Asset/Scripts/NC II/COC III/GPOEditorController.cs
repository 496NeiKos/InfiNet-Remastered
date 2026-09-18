/*
 * ================================================================
 *  UNITY SETUP GUIDE — GPOEditorController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "GPO Editor Panel" (child of Group Policy Management Panel,
 *    starts INACTIVE, placed BEFORE ContextMenu in sibling order).
 *
 *  HIERARCHY
 *    GPO Editor Panel                           ← this script here
 *      ├── TitleBar (HorizontalLayoutGroup)
 *      │     ├── TitleLabel (TMP_Text)           "Group Policy Management Editor"
 *      │     └── CloseBtn (Button)               → closeBtn
 *      ├── GPORootLabel (TMP_Text)               → rootLabelTMP
 *      │     (static header showing "[GPO Name] [SERVER.DOMAIN] Policy")
 *      │     Font: bold, size 11, padding L:4
 *      ├── MainArea (HorizontalLayoutGroup, child force expand H:ON)
 *      │     ├── EditorTreePanel (fixed width ~220, VerticalLayoutGroup)
 *      │     │     └── TreeScrollView (ScrollView)
 *      │     │           └── Viewport
 *      │     │                 └── EditorTreeContent (VLG + ContentSizeFitter)
 *      │     │                       anchor: top-stretch, pivot (0.5,1)
 *      │     │                       ← editor tree rows spawn here → editorTreeParent
 *      │     └── EditorContentPanel (flexible width)
 *      │           └── ContentScrollView (ScrollView)
 *      │                 └── Viewport
 *      │                       └── EditorContentListContent (VLG + ContentSizeFitter)
 *      │                             ← content rows spawn here → editorContentParent
 *      └── Folder Redirect Props Dialog           (FolderRedirectPropertiesController)
 *            (starts INACTIVE — see FolderRedirectPropertiesController guide)
 *
 *  PREFABS NEEDED
 *    treeNodePrefab    — same as GroupPolicyController (can share same asset)
 *    contentRowPrefab  — see below
 *
 *  contentRowPrefab
 *    Root GO — Image (color:clear, raycastTarget:ON) + height 26
 *    (LayoutElement preferredHeight:26)
 *    └── HLG (HorizontalLayoutGroup, stretch-stretch, padding L:4)
 *          childControlWidth:ON, childForceExpandWidth:OFF
 *          └── NameLabel (TMP_Text, LayoutElement flexibleWidth:1)
 *                Font size:12, overflow:Truncate, alignment:left-middle
 *
 *  INSPECTOR ASSIGNMENTS
 *    closeBtn             → CloseBtn
 *    rootLabelTMP         → GPORootLabel TMP_Text
 *    editorTreeParent     → EditorTreeContent Transform
 *    treeNodePrefab       → treeNodePrefab prefab
 *    editorContentParent  → EditorContentListContent Transform
 *    contentRowPrefab     → contentRowPrefab prefab
 *    contextMenu          → SharedContextMenuController on ContextMenu GO
 *    folderPropsController→ FolderRedirectPropertiesController on its panel
 *    gpmc                 → GroupPolicyController on Group Policy Management Panel
 *    selectedColor        → (0.2, 0.5, 0.9, 0.35)
 *
 *  HOW IT WORKS
 *    OpenForGPO(index): stores GPO index, resets _editorExpanded, updates root label,
 *      rebuilds tree, clears content panel.
 *    Left-click any non-leaf tree node → toggle expand in tree + show its children
 *      as content rows (contentRowPrefab). Left-click a content row → same as clicking
 *      that node in the tree (two-way nav).
 *    Folder Redirection node selected → content panel shows folder list rows
 *      (AppData, Desktop, etc.). Desktop and Documents rows are right-clickable
 *      → context menu "Properties" → FolderRedirectPropertiesController.Open().
 *    Close → calls GroupPolicyController.OnGPOEditorClosed() so GPMC refreshes.
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GPOEditorController : MonoBehaviour
{
    // ── Serialized fields ─────────────────────────────────────────────────────

    [Header("Nav")]
    [SerializeField] private Button   closeBtn;
    [SerializeField] private TMP_Text rootLabelTMP;

    [Header("Editor Tree")]
    [SerializeField] private Transform  editorTreeParent;
    [SerializeField] private GameObject treeNodePrefab;

    [Header("Editor Content")]
    [SerializeField] private Transform  editorContentParent;
    [SerializeField] private GameObject contentRowPrefab;

    [Header("References")]
    [SerializeField] private SharedContextMenuController    contextMenu;
    [SerializeField] private FolderRedirectPropertiesController folderPropsController;
    [SerializeField] private GroupPolicyController          gpmc;

    [Header("Colors")]
    [SerializeField] private Color selectedColor = new Color(0.2f, 0.5f, 0.9f, 0.35f);

    // ── Static tree definition ────────────────────────────────────────────────

    private struct EditorNode
    {
        public string Id;
        public string Label;
        public int    Depth;
        public string ParentId;
        public bool   IsLeaf;
        public bool   IsFolderRedirection;
    }

    private static readonly EditorNode[] s_tree =
    {
        new EditorNode { Id="CC",         Label="Computer Configuration",   Depth=0, ParentId="",        IsLeaf=false },
        new EditorNode { Id="CC.P",       Label="Policies",                 Depth=1, ParentId="CC",      IsLeaf=false },
        new EditorNode { Id="CC.P.SS",    Label="Software Settings",        Depth=2, ParentId="CC.P",    IsLeaf=true  },
        new EditorNode { Id="CC.P.WS",    Label="Windows Settings",         Depth=2, ParentId="CC.P",    IsLeaf=true  },
        new EditorNode { Id="CC.P.AT",    Label="Administrative Templates",  Depth=2, ParentId="CC.P",    IsLeaf=true  },
        new EditorNode { Id="CC.PR",      Label="Preferences",              Depth=1, ParentId="CC",      IsLeaf=true  },
        new EditorNode { Id="UC",         Label="User Configuration",       Depth=0, ParentId="",        IsLeaf=false },
        new EditorNode { Id="UC.P",       Label="Policies",                 Depth=1, ParentId="UC",      IsLeaf=false },
        new EditorNode { Id="UC.P.SS",    Label="Software Settings",        Depth=2, ParentId="UC.P",    IsLeaf=true  },
        new EditorNode { Id="UC.P.WS",    Label="Windows Settings",         Depth=2, ParentId="UC.P",    IsLeaf=false },
        new EditorNode { Id="UC.P.WS.SC", Label="Scripts (Logon/Logoff)",   Depth=3, ParentId="UC.P.WS", IsLeaf=true  },
        new EditorNode { Id="UC.P.WS.SE", Label="Security Settings",        Depth=3, ParentId="UC.P.WS", IsLeaf=true  },
        new EditorNode { Id="UC.P.WS.FR", Label="Folder Redirection",       Depth=3, ParentId="UC.P.WS", IsLeaf=true,
                         IsFolderRedirection=true },
        new EditorNode { Id="UC.P.AT",    Label="Administrative Templates",  Depth=2, ParentId="UC.P",    IsLeaf=true  },
        new EditorNode { Id="UC.PR",      Label="Preferences",              Depth=1, ParentId="UC",      IsLeaf=true  },
    };

    private static readonly string[] FolderRedirectionItems =
    {
        "AppData (Roaming)", "Desktop", "Start Menu", "Documents",
        "Pictures", "Music", "Videos", "Favorites"
    };

    private static readonly HashSet<string> InteractiveFolders =
        new HashSet<string> { "Desktop", "Documents" };

    // ── Private state ─────────────────────────────────────────────────────────

    private const float IndentWidth = 16f;

    private readonly Dictionary<string, bool> _editorExpanded = new Dictionary<string, bool>();
    private string _selectedNodeId = "";
    private int    _gpoIndex       = -1;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        closeBtn?.onClick.AddListener(Close);
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void OpenForGPO(int gpoIndex)
    {
        _gpoIndex = gpoIndex;
        _editorExpanded.Clear();
        _selectedNodeId = "";

        gameObject.SetActive(true);

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null && gpoIndex < state.GroupPolicies.Count && rootLabelTMP != null)
        {
            string gpoName = state.GroupPolicies[gpoIndex].Name;
            string server  = !string.IsNullOrEmpty(state.ComputerName)
                             ? state.ComputerName.ToUpper() : "SERVER";
            string domain  = !string.IsNullOrEmpty(state.DomainName)
                             ? state.DomainName.ToUpper() : "DOMAIN";
            rootLabelTMP.text = $"{gpoName} [{server}.{domain}] Policy";
        }

        RebuildEditorTree();
        ClearContentPanel();

        ActivityLogManager.Log($"GPO Editor opened for index {gpoIndex}", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        folderPropsController?.Close();
        gameObject.SetActive(false);
        gpmc?.OnGPOEditorClosed();
    }

    /// <summary>Called by FolderRedirectPropertiesController after applying settings.</summary>
    public void RefreshContent()
    {
        ShowContentForNode(_selectedNodeId);
    }

    // ── Editor tree ───────────────────────────────────────────────────────────

    private void RebuildEditorTree()
    {
        foreach (Transform t in editorTreeParent) Destroy(t.gameObject);

        foreach (var node in s_tree)
        {
            if (!AreAncestorsExpanded(node)) continue;

            bool nodeExp = !node.IsLeaf && GetEditorExpanded(node.Id);
            bool nodeSel = _selectedNodeId == node.Id;

            SpawnEditorNode(node, nodeExp, nodeSel);
        }

        RebuildEditorTreeLayout();
    }

    private bool AreAncestorsExpanded(EditorNode node)
    {
        if (string.IsNullOrEmpty(node.ParentId)) return true;
        if (!GetEditorExpanded(node.ParentId))   return false;

        foreach (var n in s_tree)
        {
            if (n.Id == node.ParentId)
                return AreAncestorsExpanded(n);
        }
        return true;
    }

    private void SpawnEditorNode(EditorNode node, bool isExpanded, bool isSelected)
    {
        var go = Instantiate(treeNodePrefab, editorTreeParent);
        var ui = go.GetComponent<TreeNodeUI>();
        if (ui == null) { Debug.LogError("[GPOEditor] treeNodePrefab missing TreeNodeUI."); return; }

        ui.indentSpacer.preferredWidth = node.Depth * IndentWidth;
        ui.arrowLabel.gameObject.SetActive(!node.IsLeaf);
        if (!node.IsLeaf) ui.arrowLabel.text = isExpanded ? "▼" : "▶";
        ui.nodeLabel.text    = node.Label;
        ui.background.color  = isSelected ? selectedColor : Color.clear;

        string capturedId = node.Id;
        var handler       = go.AddComponent<NodeClickHandler>();
        handler.onLeftClick = () => OnEditorNodeLeftClick(capturedId);
    }

    // ── Editor tree interaction ───────────────────────────────────────────────

    private void OnEditorNodeLeftClick(string id)
    {
        contextMenu?.Hide();

        // Find the node definition
        EditorNode? found = null;
        foreach (var n in s_tree)
        {
            if (n.Id == id) { found = n; break; }
        }
        if (found == null) return;

        var node = found.Value;

        // Toggle expand on non-leaf nodes; collapse children on collapse
        if (!node.IsLeaf)
        {
            bool nowExpanded = !GetEditorExpanded(id);
            _editorExpanded[id] = nowExpanded;

            if (!nowExpanded)
            {
                // Collapse all descendants
                foreach (var n in s_tree)
                    if (IsDescendantOf(n.Id, id))
                        _editorExpanded[n.Id] = false;

                // Deselect if selected node was a descendant
                if (_selectedNodeId != id && IsDescendantOf(_selectedNodeId, id))
                    _selectedNodeId = "";
            }
        }

        _selectedNodeId = id;

        RebuildEditorTree();
        ShowContentForNode(id);
    }

    // ── Content panel ─────────────────────────────────────────────────────────

    private void ShowContentForNode(string nodeId)
    {
        ClearContentPanel();

        if (string.IsNullOrEmpty(nodeId)) return;

        EditorNode? found = null;
        foreach (var n in s_tree)
        {
            if (n.Id == nodeId) { found = n; break; }
        }
        if (found == null) return;

        var node = found.Value;

        if (node.IsFolderRedirection)
        {
            ShowFolderRedirectionContent();
            return;
        }

        // Show immediate children as navigable content rows
        bool anyChild = false;
        foreach (var n in s_tree)
        {
            if (n.ParentId != nodeId) continue;
            SpawnContentRow(n.Label, n.Id, interactive: true);
            anyChild = true;
        }

        if (!anyChild && node.IsLeaf)
        {
            // Leaf with no special content — show empty placeholder
            SpawnContentRow("(No items to display)", "", interactive: false);
        }

        RebuildEditorContentLayout();
    }

    private void ShowFolderRedirectionContent()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;

        foreach (var folderName in FolderRedirectionItems)
        {
            bool isInteractive = InteractiveFolders.Contains(folderName);
            bool isSet = false;

            if (state != null && _gpoIndex >= 0 && _gpoIndex < state.GroupPolicies.Count)
            {
                var gpo = state.GroupPolicies[_gpoIndex];
                isSet = folderName == "Desktop"   ? gpo.DesktopRedirectSet
                      : folderName == "Documents" ? gpo.DocumentsRedirectSet
                      : false;
            }

            string label = isSet ? $"✓ {folderName}" : folderName;
            SpawnFolderRow(label, folderName, isInteractive);
        }

        RebuildEditorContentLayout();
    }

    private void SpawnContentRow(string label, string nodeId, bool interactive)
    {
        var go    = Instantiate(contentRowPrefab, editorContentParent);
        var tmpArr = go.GetComponentsInChildren<TMP_Text>();
        if (tmpArr.Length > 0) tmpArr[0].text = label;

        if (!interactive || string.IsNullOrEmpty(nodeId)) return;

        string capturedId = nodeId;
        var handler       = go.AddComponent<NodeClickHandler>();
        handler.onLeftClick = () =>
        {
            // Clicking a content row acts like clicking that node in the tree
            // Ensure its parent chain is expanded
            ExpandPathTo(capturedId);
            _selectedNodeId = capturedId;
            RebuildEditorTree();
            ShowContentForNode(capturedId);
        };
    }

    private void SpawnFolderRow(string label, string folderName, bool rightClickable)
    {
        var go    = Instantiate(contentRowPrefab, editorContentParent);
        var imgBg = go.GetComponent<Image>();

        var tmpArr = go.GetComponentsInChildren<TMP_Text>();
        if (tmpArr.Length > 0) tmpArr[0].text = label;

        if (!rightClickable) return;

        string capturedFolder = folderName;
        var handler = go.AddComponent<NodeClickHandler>();
        handler.onRightClickWithPos = screenPos =>
        {
            var panelRect = (gpmc != null ? gpmc.transform : transform) as RectTransform;
            contextMenu?.ShowForFolderRow(screenPos, panelRect,
                onProperties: () => OpenFolderProperties(capturedFolder));
        };
    }

    private void ClearContentPanel()
    {
        foreach (Transform t in editorContentParent) Destroy(t.gameObject);
    }

    // ── Folder properties ─────────────────────────────────────────────────────

    private void OpenFolderProperties(string folderName)
    {
        folderPropsController?.Open(folderName, _gpoIndex, RefreshContent);
        ActivityLogManager.Log($"Opened Properties: {folderName}", ActivityLogManager.EntryType.Action);
    }

    // ── Tree helpers ──────────────────────────────────────────────────────────

    private bool GetEditorExpanded(string id)
    {
        _editorExpanded.TryGetValue(id, out bool val);
        return val;
    }

    private bool IsDescendantOf(string nodeId, string ancestorId)
    {
        if (string.IsNullOrEmpty(nodeId)) return false;
        foreach (var n in s_tree)
        {
            if (n.Id != nodeId) continue;
            if (string.IsNullOrEmpty(n.ParentId)) return false;
            if (n.ParentId == ancestorId) return true;
            return IsDescendantOf(n.ParentId, ancestorId);
        }
        return false;
    }

    private void ExpandPathTo(string nodeId)
    {
        foreach (var n in s_tree)
        {
            if (n.Id != nodeId) continue;
            if (!string.IsNullOrEmpty(n.ParentId))
            {
                _editorExpanded[n.ParentId] = true;
                ExpandPathTo(n.ParentId);
            }
            return;
        }
    }

    private void RebuildEditorTreeLayout()
    {
        var rt = editorTreeParent as RectTransform;
        if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private void RebuildEditorContentLayout()
    {
        var rt = editorContentParent as RectTransform;
        if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }
}
