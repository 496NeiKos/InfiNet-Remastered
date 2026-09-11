/*
 * ================================================================
 *  UNITY SETUP GUIDE — FileServerRMController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "FSRM Panel" (starts INACTIVE).
 *    Opened from ServerManagerController Tools → File Server Resource Manager.
 *    Absorbs SharedFolderController — folder creation lives here.
 *
 *  HIERARCHY
 *    FSRM Panel                         ← this script here
 *      ├── TitleBar / CloseBtn          → closeBtn
 *      ├── TreePanel (left)
 *      │     ├── SharedFoldersNode      → sharedFoldersNodeBtn
 *      │     └── FileScreeningNode      → fileScreeningNodeBtn
 *      ├── ContentPanel (right)
 *      │     ├── SharedFolders View     → sharedFoldersView (starts INACTIVE)
 *      │     │     ├── FolderList       → folderListParent
 *      │     │     ├── FolderRowPrefab  → folderRowPrefab
 *      │     │     └── CreateFolderBtn  → createFolderBtn
 *      │     └── File Screening View    → fileScreeningView (starts INACTIVE)
 *      │           ├── FileGroupList    → fileGroupListParent
 *      │           ├── FileGroupRow     → fileGroupRowPrefab
 *      │           └── CreateGroupBtn  → createFileGroupBtn
 *      ├── Create Folder Dialog         → createFolderDialog (starts INACTIVE)
 *      │     ├── FolderNameInput       → folderNameInput
 *      │     ├── OKBtn                 → folderOKBtn
 *      │     └── CancelBtn             → folderCancelBtn
 *      ├── Set Permissions Dialog       → permissionsDialog (starts INACTIVE)
 *      │     ├── PermissionLabel       (static: "Everyone — Read/Write")
 *      │     ├── ApplyBtn              → applyPermBtn
 *      │     └── CancelBtn             → cancelPermBtn
 *      ├── Create File Group Dialog     → createFileGroupDialog (starts INACTIVE)
 *      │     ├── GroupNameInput        → groupNameInput
 *      │     ├── ExtensionInput        → extensionInput
 *      │     │     (player types one extension at a time, e.g. "mp3")
 *      │     ├── AddExtBtn             → addExtBtn
 *      │     ├── ExtensionList         → extensionListTMP (shows added extensions)
 *      │     ├── TargetFolderDropdown  → targetFolderDropdown
*      │     ├── OKBtn                 → fileGroupOKBtn
 *      │     └── CancelBtn             → fileGroupCancelBtn
 *      └── Apply Screen Dialog          → applyScreenDialog (starts INACTIVE)
 *            ├── FolderDropdown        → screenFolderDropdown
 *            ├── ApplyBtn              → applyScreenBtn
 *            └── CancelBtn             → cancelScreenBtn
 *
 *  INSPECTOR ASSIGNMENTS
 *    All fields as listed above.
 *    blockableExtensions → string[] Inspector list of suggested extensions
 *                          e.g. { "mp3", "mp4", "avi", "exe", "bat" }
 *
 *  HOW IT WORKS
 *    SharedFolders view: Create folder → dialog → name → OK → creates SharedFolderData.
 *    Each folder row has a "Set Permissions" button → opens permissions dialog → Apply.
 *    File Screening view: Create File Group → dialog → name + add extensions → target folder → OK.
 *    The file group's target folder links it to the shared folder (FileScreenData.TargetFolderName).
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FileServerRMController : MonoBehaviour
{
    public static FileServerRMController Instance { get; private set; }

    [Header("Nav")]
    [SerializeField] private Button closeBtn;

    [Header("Tree")]
    [SerializeField] private Button sharedFoldersNodeBtn;
    [SerializeField] private Button fileScreeningNodeBtn;

    [Header("Views")]
    [SerializeField] private GameObject sharedFoldersView;
    [SerializeField] private GameObject fileScreeningView;

    [Header("Folder List")]
    [SerializeField] private Transform  folderListParent;
    [SerializeField] private GameObject folderRowPrefab;
    [SerializeField] private Button     createFolderBtn;

    [Header("File Group List")]
    [SerializeField] private Transform  fileGroupListParent;
    [SerializeField] private GameObject fileGroupRowPrefab;
    [SerializeField] private Button     createFileGroupBtn;
    [SerializeField] private Button     applyFileScreenOpenBtn; // opens applyScreenDialog

    [Header("Create Folder Dialog")]
    [SerializeField] private GameObject    createFolderDialog;
    [SerializeField] private TMP_InputField folderNameInput;
    [SerializeField] private Button        folderOKBtn;
    [SerializeField] private Button        folderCancelBtn;

    [Header("Permissions Dialog")]
    [SerializeField] private GameObject permissionsDialog;
    [SerializeField] private Button     applyPermBtn;
    [SerializeField] private Button     cancelPermBtn;

    [Header("Create File Group Dialog")]
    [SerializeField] private GameObject     createFileGroupDialog;
    [SerializeField] private TMP_InputField  groupNameInput;
    [SerializeField] private TMP_InputField  extensionInput;
    [SerializeField] private Button         addExtBtn;
    [SerializeField] private TMP_Text       extensionListTMP;
    [SerializeField] private TMP_Dropdown   targetFolderDropdown;
    [SerializeField] private Button         fileGroupOKBtn;
    [SerializeField] private Button         fileGroupCancelBtn;

    [Header("Apply Screen Dialog")]
    [SerializeField] private GameObject  applyScreenDialog;
    [SerializeField] private TMP_Dropdown screenFolderDropdown;
    [SerializeField] private Button      applyScreenBtn;
    [SerializeField] private Button      cancelScreenBtn;

    [Header("Suggested Extensions (Inspector)")]
    [SerializeField] private string[] blockableExtensions = { "mp3", "mp4", "avi", "exe", "bat" };

    private string _pendingPermFolder = "";
    private readonly List<string> _pendingExtensions = new List<string>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        closeBtn?.onClick.AddListener(Close);
        sharedFoldersNodeBtn?.onClick.AddListener(ShowSharedFolders);
        fileScreeningNodeBtn?.onClick.AddListener(ShowFileScreening);
        createFolderBtn?.onClick.AddListener(OpenCreateFolderDialog);
        folderOKBtn?.onClick.AddListener(ConfirmCreateFolder);
        folderCancelBtn?.onClick.AddListener(() => createFolderDialog?.SetActive(false));
        applyPermBtn?.onClick.AddListener(ConfirmPermissions);
        cancelPermBtn?.onClick.AddListener(() => permissionsDialog?.SetActive(false));
        createFileGroupBtn?.onClick.AddListener(OpenCreateFileGroupDialog);
        applyFileScreenOpenBtn?.onClick.AddListener(OpenApplyScreenDialog);
        addExtBtn?.onClick.AddListener(AddExtension);
        fileGroupOKBtn?.onClick.AddListener(ConfirmCreateFileGroup);
        fileGroupCancelBtn?.onClick.AddListener(() => createFileGroupDialog?.SetActive(false));
        applyScreenBtn?.onClick.AddListener(ConfirmApplyScreen);
        cancelScreenBtn?.onClick.AddListener(() => applyScreenDialog?.SetActive(false));

        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.FSRMOpened = true;
        ShowSharedFolders();
        ActivityLogManager.Log("Opened File Server Resource Manager", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        createFolderDialog?.SetActive(false);
        permissionsDialog?.SetActive(false);
        createFileGroupDialog?.SetActive(false);
        applyScreenDialog?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── View switching ────────────────────────────────────────────────────────

    private void ShowSharedFolders()
    {
        sharedFoldersView?.SetActive(true);
        fileScreeningView?.SetActive(false);
        RefreshFolderList();
    }

    private void ShowFileScreening()
    {
        sharedFoldersView?.SetActive(false);
        fileScreeningView?.SetActive(true);
        RefreshFileGroupList();
    }

    // ── Create Folder ─────────────────────────────────────────────────────────

    private void OpenCreateFolderDialog()
    {
        if (folderNameInput != null) folderNameInput.text = "";
        createFolderDialog?.SetActive(true);
    }

    private void ConfirmCreateFolder()
    {
        string name = folderNameInput != null ? folderNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(name)) { Debug.LogWarning("[FSRM] Folder name required."); return; }

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;

        if (state.SharedFolders.Exists(f => f.FolderName == name))
        {
            Debug.LogWarning($"[FSRM] Folder '{name}' already exists.");
            return;
        }

        state.SharedFolders.Add(new SharedFolderData { FolderName = name, Path = $"D:\\{name}" });
        createFolderDialog?.SetActive(false);
        RefreshFolderList();
        ActivityLogManager.Log($"Shared folder created: {name}", ActivityLogManager.EntryType.Action);
    }

    // ── Permissions ───────────────────────────────────────────────────────────

    private void OpenPermissionsDialog(string folderName)
    {
        _pendingPermFolder = folderName;
        permissionsDialog?.SetActive(true);
    }

    private void ConfirmPermissions()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;
        var folder = state.SharedFolders.Find(f => f.FolderName == _pendingPermFolder);
        if (folder != null) folder.PermissionsSet = true;
        permissionsDialog?.SetActive(false);
        RefreshFolderList();
        ActivityLogManager.Log($"Permissions set for folder: {_pendingPermFolder} (Everyone — Read/Write)",
            ActivityLogManager.EntryType.Action);
    }

    // ── Create File Group ─────────────────────────────────────────────────────

    private void OpenCreateFileGroupDialog()
    {
        _pendingExtensions.Clear();
        if (groupNameInput   != null) groupNameInput.text   = "";
        if (extensionInput   != null) extensionInput.text   = "";
        if (extensionListTMP != null) extensionListTMP.text = "No extensions added.";
        RefreshTargetFolderDropdown();
        createFileGroupDialog?.SetActive(true);
    }

    private void AddExtension()
    {
        string ext = extensionInput != null ? extensionInput.text.Trim().ToLower() : "";
        if (string.IsNullOrEmpty(ext)) return;
        if (!ext.StartsWith(".")) ext = "." + ext;
        if (!_pendingExtensions.Contains(ext))
        {
            _pendingExtensions.Add(ext);
            if (extensionInput != null) extensionInput.text = "";
        }
        if (extensionListTMP != null)
            extensionListTMP.text = string.Join(", ", _pendingExtensions);
    }

    private void ConfirmCreateFileGroup()
    {
        string name = groupNameInput != null ? groupNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(name)) { Debug.LogWarning("[FSRM] Group name required."); return; }
        if (_pendingExtensions.Count == 0) { Debug.LogWarning("[FSRM] Add at least one extension."); return; }

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;

        // TargetFolderName is left empty — the player applies the screen separately (task 49)
        var screen = new FileScreenData
        {
            GroupName         = name,
            TargetFolderName  = "",
            BlockedExtensions = new List<string>(_pendingExtensions)
        };
        state.FileScreens.Add(screen);

        createFileGroupDialog?.SetActive(false);
        RefreshFileGroupList();
        ActivityLogManager.Log($"File group '{name}' created with {_pendingExtensions.Count} blocked extension(s)",
            ActivityLogManager.EntryType.Action);
    }

    private void RefreshTargetFolderDropdown()
    {
        if (targetFolderDropdown == null) return;
        targetFolderDropdown.ClearOptions();
        var state = ServerVirtualOSManager.Instance?.ServerState;
        var opts  = new List<string>();
        if (state != null)
            foreach (var f in state.SharedFolders) opts.Add(f.FolderName);
        targetFolderDropdown.AddOptions(opts);
    }

    // ── Apply Screen ──────────────────────────────────────────────────────────

    private void OpenApplyScreenDialog()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || state.FileScreens.Count == 0)
        {
            Debug.LogWarning("[FSRM] Create a File Group first before applying a screen.");
            return;
        }
        RefreshScreenFolderDropdown();
        applyScreenDialog?.SetActive(true);
    }

    private void RefreshScreenFolderDropdown()
    {
        if (screenFolderDropdown == null) return;
        screenFolderDropdown.ClearOptions();
        var state = ServerVirtualOSManager.Instance?.ServerState;
        var opts  = new System.Collections.Generic.List<string>();
        if (state != null)
            foreach (var f in state.SharedFolders) opts.Add(f.FolderName);
        screenFolderDropdown.AddOptions(opts);
    }

    private void ConfirmApplyScreen()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || state.FileScreens.Count == 0) return;

        string folder = "";
        if (screenFolderDropdown != null && screenFolderDropdown.options.Count > 0)
            folder = screenFolderDropdown.options[screenFolderDropdown.value].text;

        if (string.IsNullOrEmpty(folder)) { Debug.LogWarning("[FSRM] Select a target folder."); return; }

        state.FileScreens[0].TargetFolderName = folder;
        applyScreenDialog?.SetActive(false);
        RefreshFileGroupList();
        ActivityLogManager.Log($"File Screen applied to folder: {folder}", ActivityLogManager.EntryType.Action);
    }

    // ── Refresh lists ─────────────────────────────────────────────────────────

    private void RefreshFolderList()
    {
        foreach (Transform child in folderListParent) Destroy(child.gameObject);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;

        foreach (var folder in state.SharedFolders)
        {
            var row    = Instantiate(folderRowPrefab, folderListParent);
            var labels = row.GetComponentsInChildren<TMP_Text>();
            if (labels.Length > 0) labels[0].text = folder.FolderName;
            if (labels.Length > 1) labels[1].text = folder.PermissionsSet ? "Shared" : "Not shared";

            var buttons = row.GetComponentsInChildren<Button>();
            string captured = folder.FolderName;
            if (buttons.Length > 0) buttons[0].onClick.AddListener(() => OpenPermissionsDialog(captured));
        }
    }

    private void RefreshFileGroupList()
    {
        foreach (Transform child in fileGroupListParent) Destroy(child.gameObject);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;

        foreach (var screen in state.FileScreens)
        {
            var row   = Instantiate(fileGroupRowPrefab, fileGroupListParent);
            var label = row.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = $"{screen.GroupName} — {string.Join(", ", screen.BlockedExtensions)}";
        }
    }
}
