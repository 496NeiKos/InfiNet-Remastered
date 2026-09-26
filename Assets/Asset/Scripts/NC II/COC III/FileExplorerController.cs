/*
 * ================================================================
 *  UNITY SETUP GUIDE — FileExplorerController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "File Explorer Panel" GameObject (starts INACTIVE).
 *    Opened from the OS taskbar button — always accessible.
 *
 *  HIERARCHY
 *    File Explorer Panel                       ← this script here
 *      ├── TitleBar (HorizontalLayoutGroup)
 *      │     ├── TitleLabel (TMP_Text)         "File Explorer"
 *      │     └── CloseBtn (Button)             → closeBtn
 *      ├── TopBar (HorizontalLayoutGroup)
 *      │     ├── BackBtn (Button)              → backBtn        text "←"
 *      │     ├── PathLabel (TMP_Text)          → pathLabel      (flexible width)
 *      │     └── NewFolderBtn (Button)         → newFolderBtn   text "New folder"
 *      ├── MainArea (HorizontalLayoutGroup, child force expand H: ON)
 *      │     ├── TreePanel (fixed width ~160)
 *      │     │     └── Scroll View
 *      │     │           └── Viewport
 *      │     │                 └── TreeContent (VerticalLayoutGroup)
 *      │     │                       └── ThisPCBtn (Button)    → thisPCBtn
 *      │     │                             └── ThisPCLabel (TMP_Text) "This PC"
 *      │     └── ContentPanel (flexible width)
 *      │           └── Scroll View
 *      │                 └── Viewport
 *      │                       └── ContentList                → contentListParent
 *      │                           (VerticalLayoutGroup + ContentSizeFitter)
 *      │
 *      ├── ContextMenu (starts INACTIVE)       → contextMenu
 *      │     ├── ContextBackdrop (Button)      → contextBackdrop   (fullscreen transparent)
 *      │     └── ContextPopup (Image panel)    → contextPopup  (RectTransform)
 *      │           └── PropertiesBtn (Button)  → ctxPropertiesBtn
 *      │                 └── Label (TMP_Text)  "Properties"
 *      │
 *      ├── Folder Properties Panel (INACTIVE)  → propertiesPanel  (FolderPropertiesController)
 *      ├── Advanced Sharing Panel  (INACTIVE)  (AdvancedSharingController — assigned in PropertiesPanel)
 *      └── Folder Permissions Panel(INACTIVE)  (FolderPermissionsController — assigned in AdvancedSharing)
 *
 *  CONTENT LIST PARENT SETUP (ContentList GO)
 *    • VerticalLayoutGroup — spacing:2, padding L:4 R:4 T:4 B:4, child force expand W: ON
 *    • ContentSizeFitter   — Vertical Fit: Preferred Size
 *    • RectTransform       — anchor: top-stretch, pivot: (0.5, 1)
 *
 *  CONTEXT BACKDROP SETUP
 *    • Stretch to fill entire File Explorer Panel (anchor: stretch-stretch, offsets: 0)
 *    • Image — Color alpha: 0  (fully transparent)
 *    • Button — Transition: None, Raycast Target: ON
 *    • Place BEFORE ContextPopup in sibling order (backdrop behind popup)
 *
 *  CONTEXT POPUP SETUP
 *    • Image panel — small, e.g. 160×30
 *    • Anchor: top-left (pivot 0,1) so anchoredPosition places top-left corner
 *    • VerticalLayoutGroup on the popup to stack options
 *
 *  PREFABS NEEDED
 *    contentNodePrefab:
 *      • RectTransform — Height:28
 *      • HorizontalLayoutGroup — padding L:4, spacing:6, child force expand W: ON
 *      • Image (optional folder/drive icon)
 *      • TMP_Text (node name label, flexible layout element)
 *      • Button (covers whole GO, no child graphic needed if TMP handles it)
 *      • ContentNodeUI (add to root)
 *      • Do NOT pre-add NodeClickHandler — added at runtime for interactive nodes only
 *
 *    folderCreationPrefab:
 *      • Same root as contentNodePrefab
 *      • TMP_InputField instead of TMP_Text
 *      • Button (confirmedButton, starts INACTIVE)
 *      • FolderCreationNodeUI (add to root)
 *
 *  INSPECTOR ASSIGNMENTS
 *    closeBtn           → CloseBtn
 *    backBtn            → BackBtn
 *    pathLabel          → PathLabel
 *    newFolderBtn       → NewFolderBtn
 *    thisPCBtn          → ThisPCBtn
 *    contentListParent  → ContentList Transform
 *    contentNodePrefab  → assign prefab
 *    folderCreationPrefab → assign prefab
 *    predefinedFolders  → { "Perflogs","Program Files","Program Files (x86)","Users","Windows","Temp" }
 *    contextMenu        → ContextMenu GO
 *    contextBackdrop    → ContextBackdrop Button
 *    contextPopup       → ContextPopup RectTransform
 *    ctxPropertiesBtn   → PropertiesBtn
 *    propertiesPanel    → FolderPropertiesController on File Explorer Panel child
 *    uiCamera           → leave None for Screen Space Overlay; assign UI camera for SS-Camera canvas
 *
 *  TASKBAR BUTTON WIRING
 *    OS Desktop Taskbar → File Explorer Button → OnClick → FileExplorerController.Open()
 *
 *  HOW IT WORKS
 *    Open() resets to This PC level every time.
 *    This PC (tree) → shows C: drive in content panel. Back disabled.
 *    C: drive (content, left-click) → shows predefined folders + user-created folders at C:\
 *    Predefined folders: left-click navigates, no right-click.
 *    User-created folders: left-click navigates, right-click → context menu → Properties.
 *    New Folder: disabled at This PC level. Enabled at C:\ and deeper.
 *    New Folder disabled while an InputField creation is active.
 *    Duplicate names at same path auto-increment: "New folder (2)", etc.
 *    Context menu positions at mouse cursor in panel local space.
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FileExplorerController : MonoBehaviour
{
    public static FileExplorerController Instance { get; private set; }

    private const string PATH_THIS_PC = "ThisPC";
    private const string PATH_C_DRIVE = "C:\\";

    [Header("Nav")]
    [SerializeField] private Button   closeBtn;
    [SerializeField] private Button   backBtn;
    [SerializeField] private TMP_Text pathLabel;
    [SerializeField] private Button   newFolderBtn;

    [Header("Tree")]
    [SerializeField] private Button thisPCBtn;

    [Header("Content")]
    [SerializeField] private Transform contentListParent;

    [Header("Prefabs")]
    [SerializeField] private GameObject contentNodePrefab;
    [SerializeField] private GameObject folderCreationPrefab;

    [Header("Predefined C: Folders")]
    [SerializeField] private string[] predefinedFolders =
        { "Perflogs", "Program Files", "Program Files (x86)", "Users", "Windows", "Temp" };

    [Header("Context Menu")]
    [SerializeField] private GameObject contextMenu;       // IS the popup panel itself
    [SerializeField] private Button     ctxPropertiesBtn;

    [Header("Sub Panels")]
    [SerializeField] private FolderPropertiesController propertiesPanel;

    [Header("Canvas")]
    [SerializeField] private Camera uiCamera; // leave None for Screen Space Overlay

    // ── State ──────────────────────────────────────────────────────────────────
    private readonly List<string> _history = new List<string>();
    private string     _currentPath = PATH_THIS_PC;
    private bool       _isNaming;
    private FolderData _contextTarget;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        closeBtn?.onClick.AddListener(Close);
        backBtn?.onClick.AddListener(OnBack);
        newFolderBtn?.onClick.AddListener(OnNewFolder);
        thisPCBtn?.onClick.AddListener(NavigateToRoot);
        ctxPropertiesBtn?.onClick.AddListener(OnPropertiesClicked);

        if (newFolderBtn != null) newFolderBtn.interactable = false;
        contextMenu?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        _isNaming = false;
        HideContextMenu();
        NavigateToRoot();

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.FileExplorerOpened = true;

        ActivityLogManager.Log("Opened File Explorer", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        HideContextMenu();
        gameObject.SetActive(false);
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private void NavigateToRoot()
    {
        _history.Clear();
        PopulateLevel(PATH_THIS_PC);
    }

    private void NavigateTo(string path)
    {
        _history.Add(_currentPath);
        PopulateLevel(path);
    }

    private void OnBack()
    {
        if (_history.Count == 0) return;
        string prev = _history[_history.Count - 1];
        _history.RemoveAt(_history.Count - 1);
        PopulateLevel(prev);
    }

    private void PopulateLevel(string path)
    {
        _currentPath = path;
        HideContextMenu();
        ClearContent();

        var state = ServerVirtualOSManager.Instance?.ServerState;

        if (path == PATH_THIS_PC)
        {
            SpawnDriveNode("Local Disk (C:)", PATH_C_DRIVE);
        }
        else if (path == PATH_C_DRIVE)
        {
            foreach (var name in predefinedFolders)
                SpawnFolderNode(name, PATH_C_DRIVE + name, false, null);

            if (state != null)
                foreach (var f in state.UserCreatedFolders)
                    if (f.ParentPath == PATH_C_DRIVE)
                        SpawnFolderNode(f.Name, f.FullPath, true, f);
        }
        else
        {
            if (state != null)
                foreach (var f in state.UserCreatedFolders)
                    if (f.ParentPath == path)
                        SpawnFolderNode(f.Name, f.FullPath, true, f);
        }

        UpdateNavButtons();
        UpdatePathLabel();
        RebuildContentLayout();
    }

    // ── Content Spawning ──────────────────────────────────────────────────────

    private void SpawnDriveNode(string label, string drivePath)
    {
        var go  = Instantiate(contentNodePrefab, contentListParent);
        var ui  = go.GetComponent<ContentNodeUI>();
        if (ui == null) { Debug.LogError("[FileExplorer] contentNodePrefab is missing ContentNodeUI component."); return; }
        ui.NodeName      = label;
        ui.NodeFullPath  = drivePath;
        ui.IsInteractive = false;
        ui.Data          = null;

        var tmp = go.GetComponentInChildren<TMP_Text>();
        if (tmp != null) tmp.text = label;

        var btn = go.GetComponentInChildren<Button>();
        if (btn == null) Debug.LogWarning("[FileExplorer] contentNodePrefab has no Button component.");
        btn?.onClick.AddListener(() => NavigateTo(drivePath));
    }

    private void SpawnFolderNode(string name, string fullPath, bool isInteractive, FolderData data)
    {
        var go  = Instantiate(contentNodePrefab, contentListParent);
        var ui  = go.GetComponent<ContentNodeUI>();
        if (ui == null) { Debug.LogError("[FileExplorer] contentNodePrefab is missing ContentNodeUI component."); return; }
        ui.NodeName      = name;
        ui.NodeFullPath  = fullPath;
        ui.IsInteractive = isInteractive;
        ui.Data          = data;

        var tmp = go.GetComponentInChildren<TMP_Text>();
        if (tmp != null) tmp.text = name;

        if (isInteractive)
        {
            FolderData capData = data;
            var handler = CreateClickOverlay(go);
            handler.onLeftClick         = () => NavigateTo(fullPath);
            handler.onRightClickWithPos = screenPos => ShowContextMenu(capData, screenPos);
        }
        else
        {
            var btn = go.GetComponentInChildren<Button>();
            if (btn == null) Debug.LogWarning("[FileExplorer] contentNodePrefab has no Button component.");
            btn?.onClick.AddListener(() => NavigateTo(fullPath));
        }
    }

    private NodeClickHandler CreateClickOverlay(GameObject nodeGO)
    {
        var overlay = new GameObject("ClickOverlay");
        overlay.transform.SetParent(nodeGO.transform, false);

        var rect = overlay.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img = overlay.AddComponent<Image>();
        img.color         = new Color(0, 0, 0, 0);
        img.raycastTarget = true;

        overlay.AddComponent<LayoutElement>().ignoreLayout = true;
        overlay.transform.SetAsLastSibling();

        return overlay.AddComponent<NodeClickHandler>();
    }

    // ── New Folder ────────────────────────────────────────────────────────────

    private void OnNewFolder()
    {
        if (_isNaming) return;
        _isNaming = true;
        if (newFolderBtn != null) newFolderBtn.interactable = false;

        string parentPath = _currentPath;
        var go = Instantiate(folderCreationPrefab, contentListParent);
        var creation = go.GetComponent<FolderCreationNodeUI>();
        creation.onConfirmed = name => ConfirmFolderCreation(name, parentPath, go);
        RebuildContentLayout();
    }

    private void ConfirmFolderCreation(string name, string parentPath, GameObject creationGO)
    {
        Destroy(creationGO);
        _isNaming = false;

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) { UpdateNavButtons(); return; }

        name = ResolveDuplicateName(name, parentPath, state);
        string fullPath = parentPath.TrimEnd('\\') + "\\" + name;

        var data = new FolderData
        {
            Name        = name,
            ParentPath  = parentPath,
            FullPath    = fullPath,
            DateCreated = System.DateTime.Now.ToString("M/d/yyyy h:mm tt"),
            Permissions = new List<FolderPermissionEntry>
            {
                new FolderPermissionEntry { GroupOrUser = "Everyone", FullControlAllow = true }
            }
        };
        state.UserCreatedFolders.Add(data);

        SpawnFolderNode(name, fullPath, true, data);
        UpdateNavButtons();
        RebuildContentLayout();

        ActivityLogManager.Log($"Created folder: {fullPath}", ActivityLogManager.EntryType.Action);
    }

    private string ResolveDuplicateName(string name, string parentPath, ServerDeviceState state)
    {
        var taken = new HashSet<string>();
        foreach (var f in state.UserCreatedFolders)
            if (f.ParentPath == parentPath) taken.Add(f.Name);
        if (parentPath == PATH_C_DRIVE)
            foreach (var s in predefinedFolders) taken.Add(s);

        if (!taken.Contains(name)) return name;
        int i = 2;
        while (taken.Contains($"{name} ({i})")) i++;
        return $"{name} ({i})";
    }

    // ── Context Menu ──────────────────────────────────────────────────────────

    private void ShowContextMenu(FolderData target, Vector2 screenPos)
    {
        _contextTarget = target;
        contextMenu?.SetActive(true);

        var rect = contextMenu?.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform, screenPos, uiCamera, out localPos);
            rect.anchoredPosition = localPos;
        }
    }

    private void HideContextMenu() => contextMenu?.SetActive(false);

    private void OnPropertiesClicked()
    {
        HideContextMenu();
        if (_contextTarget == null) return;
        propertiesPanel?.Open(_contextTarget);
        ActivityLogManager.Log($"Opened Properties: {_contextTarget.FullPath}", ActivityLogManager.EntryType.Action);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ClearContent()
    {
        foreach (Transform child in contentListParent) Destroy(child.gameObject);
    }

    private void UpdateNavButtons()
    {
        if (backBtn != null)      backBtn.interactable      = _history.Count > 0;
        if (newFolderBtn != null) newFolderBtn.interactable = (_currentPath != PATH_THIS_PC) && !_isNaming;
    }

    private void UpdatePathLabel()
    {
        if (pathLabel == null) return;
        pathLabel.text = _currentPath == PATH_THIS_PC ? "This PC" : _currentPath;
    }

    private void RebuildContentLayout()
    {
        var rect = contentListParent as RectTransform;
        if (rect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }
}
