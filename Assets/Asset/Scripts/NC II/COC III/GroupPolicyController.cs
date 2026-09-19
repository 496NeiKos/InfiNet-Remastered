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
 *      │     │                       anchor:top-stretch, pivot (0.5,1)
 *      │     │                       ContentSizeFitter: Vertical=Preferred Size
 *      │     │                       ← tree rows spawn here → treeNodeParent
 *      │     └── RightPanel (flexible, VerticalLayoutGroup)
 *      │           ├── GPOListPanel (VerticalLayoutGroup, LE flexibleH:1) → gpoListPanel
 *      │           │     Starts ACTIVE. Wraps the existing GPO-list view.
 *      │           │     ├── ContentHeader (HorizontalLayoutGroup, height 24) — STATIC
 *      │           │     │     Eight TMP_Text labels in order:
 *      │           │     │     "Link Order"(w:50) | "GPO"(flexible) | "Enforced"(w:70)
 *      │           │     │     "Link Enabled"(w:90) | "GPO Status"(w:80) | "WMI Filter"(w:80)
 *      │           │     │     "Modified"(w:120) | "Domain"(w:100)
 *      │           │     └── ContentScrollView (ScrollView, fills remaining height)
 *      │           │           └── Viewport
 *      │           │                 └── ContentListContent (VLG + ContentSizeFitter)
 *      │           │                       ← gpoRowPrefab rows spawn here → contentListParent
 *      │           └── GPOScopePanel (VerticalLayoutGroup, LE flexibleH:1) → gpoScopePanel
 *      │                 Starts INACTIVE. Shown when a GPO node is selected.
 *      │                 ├── ScopeTabBar (HLG height:28, Image color:(0.2,0.2,0.2,1))
 *      │                 │     └── ScopeTabLabel (TMP "Scope" bold size:12)
 *      │                 └── ScopeScrollView (ScrollView, LE flexibleHeight:1)
 *      │                       └── Viewport
 *      │                             └── ScopeContent (VLG + ContentSizeFitter)
 *      │                                   padding T:6 B:6 L:8 R:8, spacing:4
 *      │                                   anchor:top-stretch, pivot (0.5,1)
 *      │                                   ├── LinksHeaderLabel (TMP "Links" bold size:12)
 *      │                                   │     LE preferredHeight:20
 *      │                                   ├── LinksDropdown (TMP_Dropdown LE preferredHeight:28
 *      │                                   │     interactable:OFF)             → scopeLinksDropdown
 *      │                                   ├── LinksColumnHeader (HLG height:20)
 *      │                                   │     childControlWidth:ON childForceExpandWidth:OFF
 *      │                                   │     ├── ColH_Location  (TMP "Location" LE flexibleWidth:1)
 *      │                                   │     ├── ColH_Enforced  (TMP "Enforced" LE preferredWidth:70)
 *      │                                   │     ├── ColH_LinkEnabled (TMP "Link Enabled" LE preferredWidth:90)
 *      │                                   │     └── ColH_Path      (TMP "Path" LE preferredWidth:150)
 *      │                                   │     All header TMP: size:11 bold color:(0.7,0.7,0.7,1)
 *      │                                   ├── LinksListParent (VLG + ContentSizeFitter)
 *      │                                   │     childControlWidth:ON childForceExpandWidth:ON
 *      │                                   │     ← linkRowPrefab rows spawn here → scopeLinksListParent
 *      │                                   ├── ScopeSeparator (Image height:1 color:(0.35,0.35,0.35,1))
 *      │                                   ├── SecurityFilterHeaderLabel (TMP "Security Filtering" bold size:12)
 *      │                                   │     LE preferredHeight:20
 *      │                                   ├── SecurityFilterScrollView (ScrollView LE preferredHeight:110)
 *      │                                   │     Image color:(0.1,0.1,0.1,1)
 *      │                                   │     └── Viewport
 *      │                                   │           └── SecurityFilterListParent (VLG + ContentSizeFitter)
 *      │                                   │                 anchor:top-stretch, pivot (0.5,1)
 *      │                                   │                 ← securityFilterRowPrefab → scopeSecurityListParent
 *      │                                   └── SecurityFilterButtons (HLG height:28 spacing:4)
 *      │                                         childForceExpandWidth:OFF
 *      │                                         ├── ScopeAddBtn (Button LE preferredWidth:80) → scopeAddBtn
 *      │                                         │     Label TMP "Add"
 *      │                                         ├── ScopeRemoveBtn (Button LE preferredWidth:80) → scopeRemoveBtn
 *      │                                         │     Label TMP "Remove"
 *      │                                         └── ScopePropertiesBtn (Button LE preferredWidth:80)
 *      │                                               → scopePropertiesBtn  interactable:OFF always
 *      │                                               Label TMP "Properties"
 *      ├── ContextMenu                        → contextMenu  (SharedContextMenuController)
 *      │     (see SharedContextMenuController setup guide — LAST sibling in panel)
 *      ├── Create GPO Dialog (starts INACTIVE) → createGPODialog
 *      │     Image panel, centered, ~300×160
 *      │     ├── Title (TMP_Text) "New GPO"
 *      │     ├── NameLabel (TMP_Text) "Name:"
 *      │     ├── GPONameInput (TMP_InputField)  → gpoNameInput
 *      │     ├── OKBtn (Button)                → gpoOKBtn
 *      │     └── CancelBtn (Button)            → gpoCancelBtn
 *      ├── AddUserPopup (starts INACTIVE)       → addUserPopup
 *      │     RectTransform: anchor center-center, pivot 0.5/0.5, width:380, height:320
 *      │     Image: color (0.13,0.13,0.13,1)
 *      │     VerticalLayoutGroup: padding T:10 B:10 L:10 R:10, spacing:6
 *      │     ├── PopupTitleLabel (TMP "Select Users, Computers, or Groups" bold size:12)
 *      │     │     LE preferredHeight:24
 *      │     ├── PopupDivider0 (Image height:1 color:(0.35,0.35,0.35,1))
 *      │     ├── LocationRow (HLG LE preferredHeight:26 spacing:6)
 *      │     │     ├── Lbl_Location (TMP "From this location:" LE preferredWidth:130 right-middle)
 *      │     │     └── LocationInput (TMP_InputField LE flexibleWidth:1) → addUserLocationInput
 *      │     ├── Lbl_ObjectNames (TMP "Enter the object names to select:" size:11)
 *      │     │     LE preferredHeight:20
 *      │     ├── SearchRow (HLG LE preferredHeight:26 spacing:6)
 *      │     │     ├── NameSearchInput (TMP_InputField LE flexibleWidth:1) → addUserSearchInput
 *      │     │     └── CheckNamesBtn (Button LE preferredWidth:100)        → addUserCheckNamesBtn
 *      │     │           Label TMP "Check Names"
 *      │     ├── AddResultsScrollView (ScrollView LE flexibleHeight:1)
 *      │     │     Image color:(0.1,0.1,0.1,1)
 *      │     │     └── Viewport
 *      │     │           └── AddResultsListParent (VLG + ContentSizeFitter)
 *      │     │                 anchor:top-stretch, pivot (0.5,1)
 *      │     │                 ← addUserResultRowPrefab → addUserResultsParent
 *      │     ├── PopupDivider1 (Image height:1 color:(0.35,0.35,0.35,1))
 *      │     └── PopupFooter (HLG LE preferredHeight:28 spacing:6 childForceExpandWidth:OFF)
 *      │           ├── PopupSpacer (LayoutElement flexibleWidth:1)
 *      │           ├── PopupOKBtn (Button LE preferredWidth:70)     → addUserOKBtn
 *      │           │     Label TMP "OK"
 *      │           └── PopupCancelBtn (Button LE preferredWidth:70) → addUserCancelBtn
 *      │                 Label TMP "Cancel"
 *      └── GPO Editor Panel (starts INACTIVE)  (GPOEditorController — separate script)
 *            ← must be placed BEFORE ContextMenu in sibling order
 *
 *  PREFABS NEEDED
 *
 *  treeNodePrefab  (unchanged — see original guide)
 *
 *  gpoRowPrefab  (unchanged — see original guide)
 *
 *  linkRowPrefab
 *    Root GO — Image (color:clear, raycastTarget:OFF) + LinkRowUI + LayoutElement preferredHeight:22
 *    └── HLG (stretch-stretch, childControlWidth:ON, childForceExpandWidth:OFF, spacing:0)
 *          ├── Col_Location   (TMP LE flexibleWidth:1    font-size:11 left-middle)
 *          ├── Col_Enforced   (TMP LE preferredWidth:70  font-size:11 center-middle)
 *          ├── Col_LinkEnabled (TMP LE preferredWidth:90 font-size:11 center-middle)
 *          └── Col_Path       (TMP LE preferredWidth:150 font-size:11 left-middle overflow:Truncate)
 *    Inspector assignments on LinkRowUI: assign all four TMP_Text fields
 *
 *  securityFilterRowPrefab
 *    Root GO — Image (color:clear, raycastTarget:ON) + LayoutElement preferredHeight:24
 *    └── HLG (padding L:4, childControlWidth:ON, childForceExpandWidth:ON, stretch-stretch)
 *          └── NameLabel (TMP font-size:11 left-middle overflow:Truncate)
 *    NodeClickHandler added at runtime — do NOT pre-place it.
 *
 *  addUserResultRowPrefab
 *    Identical structure to securityFilterRowPrefab.
 *    NodeClickHandler added at runtime.
 *
 *  INSPECTOR ASSIGNMENTS (on GroupPolicyController)
 *    closeBtn                → CloseBtn
 *    treeNodeParent          → TreeContent Transform
 *    treeNodePrefab          → treeNodePrefab
 *    contentListParent       → ContentListContent Transform  (inside GPOListPanel)
 *    gpoRowPrefab            → gpoRowPrefab
 *    contextMenu             → SharedContextMenuController on ContextMenu GO
 *    createGPODialog         → Create GPO Dialog GO
 *    gpoNameInput            → GPONameInput
 *    gpoOKBtn                → OKBtn
 *    gpoCancelBtn            → CancelBtn
 *    gpoEditor               → GPOEditorController on GPO Editor Panel
 *    gpoListPanel            → GPOListPanel GO
 *    gpoScopePanel           → GPOScopePanel GO
 *    scopeLinksDropdown      → LinksDropdown
 *    scopeLinksListParent    → LinksListParent Transform
 *    linkRowPrefab           → linkRowPrefab
 *    scopeSecurityListParent → SecurityFilterListParent Transform
 *    securityFilterRowPrefab → securityFilterRowPrefab
 *    scopeAddBtn             → ScopeAddBtn
 *    scopeRemoveBtn          → ScopeRemoveBtn
 *    scopePropertiesBtn      → ScopePropertiesBtn
 *    addUserPopup            → AddUserPopup GO
 *    addUserLocationInput    → LocationInput
 *    addUserSearchInput      → NameSearchInput
 *    addUserCheckNamesBtn    → CheckNamesBtn
 *    addUserResultsParent    → AddResultsListParent Transform
 *    addUserResultRowPrefab  → addUserResultRowPrefab
 *    addUserOKBtn            → PopupOKBtn
 *    addUserCancelBtn        → PopupCancelBtn
 *    selectedColor           → (0.2, 0.5, 0.9, 0.35) blue tint
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

    [Header("Content — GPO List")]
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

    [Header("Content Panel Routing")]
    [SerializeField] private GameObject gpoListPanel;
    [SerializeField] private GameObject gpoScopePanel;

    [Header("Scope — Links")]
    [SerializeField] private TMP_Dropdown scopeLinksDropdown;
    [SerializeField] private Transform    scopeLinksListParent;
    [SerializeField] private GameObject   linkRowPrefab;

    [Header("Scope — Security Filtering")]
    [SerializeField] private Transform    scopeSecurityListParent;
    [SerializeField] private GameObject   securityFilterRowPrefab;
    [SerializeField] private Button       scopeAddBtn;
    [SerializeField] private Button       scopeRemoveBtn;
    [SerializeField] private Button       scopePropertiesBtn;

    [Header("Add User Popup")]
    [SerializeField] private GameObject      addUserPopup;
    [SerializeField] private TMP_InputField  addUserLocationInput;
    [SerializeField] private TMP_InputField  addUserSearchInput;
    [SerializeField] private Button          addUserCheckNamesBtn;
    [SerializeField] private Transform       addUserResultsParent;
    [SerializeField] private GameObject      addUserResultRowPrefab;
    [SerializeField] private Button          addUserOKBtn;
    [SerializeField] private Button          addUserCancelBtn;

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

    private string _selectedSecurityFilter = "";
    private string _selectedAddUserResult  = "";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        closeBtn    ?.onClick.AddListener(Close);
        gpoOKBtn    ?.onClick.AddListener(ConfirmCreateGPO);
        gpoCancelBtn?.onClick.AddListener(() => createGPODialog?.SetActive(false));

        scopeAddBtn          ?.onClick.AddListener(OpenAddUserPopup);
        scopeRemoveBtn       ?.onClick.AddListener(OnRemoveClick);
        addUserCheckNamesBtn ?.onClick.AddListener(OnCheckNamesClick);
        addUserOKBtn         ?.onClick.AddListener(OnAddUserOK);
        addUserCancelBtn     ?.onClick.AddListener(CloseAddUserPopup);

        // Properties is always non-interactive — visual only
        if (scopePropertiesBtn != null) scopePropertiesBtn.interactable = false;

        createGPODialog?.SetActive(false);
        addUserPopup   ?.SetActive(false);
        gpoScopePanel  ?.SetActive(false);
        gpoListPanel   ?.SetActive(true);
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
        addUserPopup   ?.SetActive(false);

        gpoListPanel ?.SetActive(true);
        gpoScopePanel?.SetActive(false);

        RebuildTree();
        RefreshContentPanel();

        ActivityLogManager.Log("Opened Group Policy Management", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        contextMenu?.Hide();
        createGPODialog?.SetActive(false);
        addUserPopup   ?.SetActive(false);
        gpoEditor      ?.Close();
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

        bool forestExp = GetExpanded("Forest");
        SpawnNode("Forest", $"Forest: {domainName}", 0,
            isLeaf: false, isExpanded: forestExp, isSelected: false,
            leftClick: true, rightClick: false);

        if (!forestExp) { RebuildTreeLayout(); return; }

        bool domainExp = GetExpanded("Domain");
        SpawnNode("Domain", domainName, 1,
            isLeaf: false, isExpanded: domainExp, isSelected: false,
            leftClick: true, rightClick: false);

        if (!domainExp) { RebuildTreeLayout(); return; }

        foreach (var name in StaticLeaves)
            SpawnNode("Static_" + name, name, 2,
                isLeaf: true, isExpanded: false, isSelected: false,
                leftClick: false, rightClick: false);

        foreach (var ou in state.OrganizationalUnits)
        {
            string ouId = "OU:" + ou.Name;
            bool ouExp  = GetExpanded(ouId);
            bool ouSel  = _selType == SelectionType.OU && _selectedOU == ou.Name;

            SpawnNode(ouId, ou.Name, 2,
                isLeaf: false, isExpanded: ouExp, isSelected: ouSel,
                leftClick: true, rightClick: true);

            if (!ouExp) continue;

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
        var go = Instantiate(treeNodePrefab, treeNodeParent);
        var ui = go.GetComponent<TreeNodeUI>();
        if (ui == null) { Debug.LogError("[GPMC] treeNodePrefab missing TreeNodeUI."); return; }

        ui.indentSpacer.preferredWidth = depth * IndentWidth;
        ui.arrowLabel.gameObject.SetActive(!isLeaf);
        if (!isLeaf) ui.arrowLabel.text = isExpanded ? "▼" : "▶";
        ui.nodeLabel.text   = label;
        ui.background.color = isSelected ? selectedColor : Color.clear;

        if (!leftClick && !rightClick) return;

        var handler       = go.AddComponent<NodeClickHandler>();
        string capturedId = id;

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
            string ouName = id.Substring(3);
            _selType      = SelectionType.OU;
            _selectedOU   = ouName;
            _selectedGPO  = -1;
            _expanded[id] = !GetExpanded(id);
        }
        else if (id.StartsWith("GPO:"))
        {
            int idx      = int.Parse(id.Substring(4));
            _selType     = SelectionType.GPO;
            _selectedGPO = idx;

            var state = ServerVirtualOSManager.Instance?.ServerState;
            if (state != null && idx < state.GroupPolicies.Count)
                _selectedOU = state.GroupPolicies[idx].LinkedOUName;
        }
        else
        {
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
            var state   = ServerVirtualOSManager.Instance?.ServerState;
            bool enforced = state != null && _pendingGPO < state.GroupPolicies.Count
                            && state.GroupPolicies[_pendingGPO].Enforced;

            contextMenu?.ShowForGPO(screenPos, panelRect, enforced,
                onEdit:     () => OpenGPOEditor(_pendingGPO),
                onEnforced: () => ToggleEnforced(_pendingGPO));
        }
    }

    // ── Content panel routing ─────────────────────────────────────────────────

    private void RefreshContentPanel()
    {
        if (_selType == SelectionType.GPO)
        {
            gpoListPanel ?.SetActive(false);
            gpoScopePanel?.SetActive(true);
            RefreshScopePanel();
            return;
        }

        gpoListPanel ?.SetActive(true);
        gpoScopePanel?.SetActive(false);

        foreach (Transform t in contentListParent) Destroy(t.gameObject);

        if (_selType == SelectionType.None) { RebuildContentLayout(); return; }

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) { RebuildContentLayout(); return; }

        int linkOrder = 1;
        for (int i = 0; i < state.GroupPolicies.Count; i++)
        {
            var gpo = state.GroupPolicies[i];
            if (gpo.LinkedOUName != _selectedOU) continue;
            SpawnGPORow(gpo, i, linkOrder++, state.DomainName);
        }

        RebuildContentLayout();
    }

    private void SpawnGPORow(GPOData gpo, int gpoIndex, int linkOrder, string domainName)
    {
        var go = Instantiate(gpoRowPrefab, contentListParent);
        var ui = go.GetComponent<GPORowUI>();
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
        ui.background.color     = Color.clear;

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

    // ── GPO Scope panel ───────────────────────────────────────────────────────

    private void RefreshScopePanel()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || _selectedGPO < 0 || _selectedGPO >= state.GroupPolicies.Count) return;

        var gpo = state.GroupPolicies[_selectedGPO];

        // Links section
        if (scopeLinksDropdown != null)
        {
            scopeLinksDropdown.ClearOptions();
            scopeLinksDropdown.AddOptions(new List<string> { state.DomainName });
            scopeLinksDropdown.value        = 0;
            scopeLinksDropdown.interactable = false;
        }

        if (scopeLinksListParent != null)
        {
            foreach (Transform t in scopeLinksListParent) Destroy(t.gameObject);
            SpawnLinkRow(
                location:    gpo.LinkedOUName,
                enforced:    gpo.Enforced    ? "Yes" : "No",
                linkEnabled: gpo.LinkEnabled ? "Yes" : "No",
                path:        $"{state.DomainName}/{gpo.LinkedOUName}");
        }

        // Security Filtering section
        _selectedSecurityFilter = "";
        if (scopeSecurityListParent != null)
        {
            foreach (Transform t in scopeSecurityListParent) Destroy(t.gameObject);

            // "Authenticated Users" is always the first entry — static, non-removable
            SpawnSecurityFilterRow("Authenticated Users", isStatic: true);

            foreach (string username in gpo.SecurityFilterUsernames)
            {
                var u = state.UserAccounts.Find(x => x.Username == username);
                if (u == null) continue;
                string display = $"{u.FullName} ({u.Username}@{state.DomainName})";
                SpawnSecurityFilterRow(display, isStatic: false, username: username);
            }
        }

        if (scopeRemoveBtn != null) scopeRemoveBtn.interactable = false;

        RebuildScopeLayout();
    }

    private void SpawnLinkRow(string location, string enforced, string linkEnabled, string path)
    {
        var go = Instantiate(linkRowPrefab, scopeLinksListParent);
        var ui = go.GetComponent<LinkRowUI>();
        if (ui == null) { Debug.LogError("[GPMC] linkRowPrefab missing LinkRowUI."); return; }

        ui.col_location  .text = location;
        ui.col_enforced  .text = enforced;
        ui.col_linkEnabled.text = linkEnabled;
        ui.col_path      .text = path;
    }

    private void SpawnSecurityFilterRow(string displayName, bool isStatic, string username = "")
    {
        var go  = Instantiate(securityFilterRowPrefab, scopeSecurityListParent);
        var tmp = go.GetComponentInChildren<TMP_Text>();
        var img = go.GetComponent<Image>();

        if (tmp != null) tmp.text  = displayName;
        if (img != null) img.color = Color.clear;

        if (isStatic) return;

        string capturedUsername = username;
        var handler = go.AddComponent<NodeClickHandler>();
        handler.onLeftClick = () =>
        {
            _selectedSecurityFilter = capturedUsername;

            // Reset all sibling row backgrounds then highlight this one
            foreach (Transform t in scopeSecurityListParent)
            {
                var bg = t.GetComponent<Image>();
                if (bg != null) bg.color = Color.clear;
            }
            if (img != null) img.color = selectedColor;
            if (scopeRemoveBtn != null) scopeRemoveBtn.interactable = true;
        };
    }

    // ── Add User popup ────────────────────────────────────────────────────────

    private void OpenAddUserPopup()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || _selectedGPO < 0 || _selectedGPO >= state.GroupPolicies.Count) return;

        _selectedAddUserResult = "";

        if (addUserLocationInput != null) addUserLocationInput.text = state.DomainName;
        if (addUserSearchInput   != null) addUserSearchInput.text   = "";

        if (addUserResultsParent != null)
            foreach (Transform t in addUserResultsParent) Destroy(t.gameObject);

        addUserPopup?.SetActive(true);
    }

    private void OnCheckNamesClick()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || _selectedGPO < 0 || _selectedGPO >= state.GroupPolicies.Count) return;

        var gpo    = state.GroupPolicies[_selectedGPO];
        string query = (addUserSearchInput?.text.Trim() ?? "").ToLower();

        if (addUserResultsParent != null)
            foreach (Transform t in addUserResultsParent) Destroy(t.gameObject);

        _selectedAddUserResult = "";

        foreach (var u in state.UserAccounts)
        {
            if (u.OUName != gpo.LinkedOUName) continue;
            if (gpo.SecurityFilterUsernames.Contains(u.Username)) continue;

            bool match = string.IsNullOrEmpty(query)
                || u.Username.ToLower().Contains(query)
                || u.FullName.ToLower().Contains(query);
            if (!match) continue;

            SpawnAddResultRow(u, state.DomainName);
        }

        if (addUserResultsParent is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private void SpawnAddResultRow(UserData u, string domain)
    {
        string display = $"{u.FullName} ({u.Username}@{domain})";

        var go  = Instantiate(addUserResultRowPrefab, addUserResultsParent);
        var tmp = go.GetComponentInChildren<TMP_Text>();
        var img = go.GetComponent<Image>();

        if (tmp != null) tmp.text  = display;
        if (img != null) img.color = Color.clear;

        string capturedUsername = u.Username;
        var handler = go.AddComponent<NodeClickHandler>();
        handler.onLeftClick = () =>
        {
            _selectedAddUserResult = capturedUsername;

            foreach (Transform t in addUserResultsParent)
            {
                var bg = t.GetComponent<Image>();
                if (bg != null) bg.color = Color.clear;
            }
            if (img != null) img.color = selectedColor;
        };
    }

    private void OnAddUserOK()
    {
        if (string.IsNullOrEmpty(_selectedAddUserResult)) return;

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || _selectedGPO < 0 || _selectedGPO >= state.GroupPolicies.Count) return;

        var gpo = state.GroupPolicies[_selectedGPO];
        if (!gpo.SecurityFilterUsernames.Contains(_selectedAddUserResult))
        {
            gpo.SecurityFilterUsernames.Add(_selectedAddUserResult);
            ActivityLogManager.Log(
                $"Added '{_selectedAddUserResult}' to security filtering on GPO: {gpo.Name}",
                ActivityLogManager.EntryType.Action);
        }

        CloseAddUserPopup();
        RefreshScopePanel();
    }

    private void OnRemoveClick()
    {
        if (string.IsNullOrEmpty(_selectedSecurityFilter)) return;

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || _selectedGPO < 0 || _selectedGPO >= state.GroupPolicies.Count) return;

        var gpo = state.GroupPolicies[_selectedGPO];
        gpo.SecurityFilterUsernames.Remove(_selectedSecurityFilter);
        _selectedSecurityFilter = "";

        ActivityLogManager.Log(
            $"Removed user from security filtering on GPO: {gpo.Name}",
            ActivityLogManager.EntryType.Action);

        RefreshScopePanel();
    }

    private void CloseAddUserPopup()
    {
        _selectedAddUserResult = "";
        addUserPopup?.SetActive(false);
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

        string ouId = "OU:" + _pendingOU;
        _expanded[ouId] = true;
        _selType        = SelectionType.OU;
        _selectedOU     = _pendingOU;

        RebuildTree();
        RefreshContentPanel();

        ActivityLogManager.Log($"Created GPO: {name} linked to {_pendingOU}", ActivityLogManager.EntryType.Action);
    }

    // ── Open GPO editor ───────────────────────────────────────────────────────

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

        var gpo      = state.GroupPolicies[gpoIndex];
        gpo.Enforced     = !gpo.Enforced;
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
        if (treeNodeParent is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private void RebuildContentLayout()
    {
        if (contentListParent is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private void RebuildScopeLayout()
    {
        if (scopeLinksListParent is RectTransform lrt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(lrt);
        if (scopeSecurityListParent is RectTransform srt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(srt);
    }
}
