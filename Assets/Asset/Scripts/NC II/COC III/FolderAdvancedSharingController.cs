/*
 * ================================================================
 *  UNITY SETUP GUIDE — FolderAdvancedSharingController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Advanced Sharing Panel" (child of File Explorer Panel, starts INACTIVE).
 *    Opened by FolderPropertiesController when "Advanced Sharing..." is clicked.
 *
 *  NOTE ON NAMING
 *    Named FolderAdvancedSharingController to avoid conflict with the existing
 *    AdvancedSharingController in COC II.
 *
 *  HIERARCHY
 *    Advanced Sharing Panel                   ← this script here
 *      ├── TitleBar (HorizontalLayoutGroup)
 *      │     ├── TitleLabel (TMP_Text)        "Advanced Sharing"
 *      │     └── CloseBtn (Button)            → closeBtn
 *      ├── ShareToggleRow (HorizontalLayoutGroup)
 *      │     ├── ShareToggle (Toggle)         → shareToggle
 *      │     └── Label (TMP_Text)             "Share this folder"
 *      ├── SettingsGroup                      → settingsGroup   (starts INACTIVE)
 *      │     ├── ShareNameSection
 *      │     │     ├── SectionLabel (TMP_Text) "Share name:"
 *      │     │     └── InputRow (HorizontalLayoutGroup)
 *      │     │           ├── ShareNameInput (TMP_InputField) → shareNameInput
 *      │     │           ├── AddShareNameBtn (Button)        → addShareNameBtn   "Add"
 *      │     │           │     interactable: OFF in Inspector
 *      │     │           └── RemoveShareNameBtn (Button)     → removeShareNameBtn "Remove"
 *      │     │                 interactable: OFF in Inspector
 *      │     ├── CommentsSection
 *      │     │     ├── SectionLabel (TMP_Text) "Comments:"
 *      │     │     └── CommentsInput (TMP_InputField) → commentsInput
 *      │     └── ActionRow (HorizontalLayoutGroup)
 *      │           ├── PermissionsBtn (Button)  → permissionsBtn   "Permissions"
 *      │           └── CachingBtn (Button)      → cachingBtn       "Caching"
 *      └── FooterBar (HorizontalLayoutGroup)
 *            ├── OKBtn (Button)               → okBtn
 *            ├── CancelBtn (Button)           → cancelBtn
 *            └── ApplyBtn (Button)            → applyBtn
 *
 *  INSPECTOR SETUP
 *    addShareNameBtn → interactable: OFF
 *    removeShareNameBtn → interactable: OFF
 *    permissionsController → FolderPermissionsController on the Permissions Panel GO
 *
 *  HOW IT WORKS
 *    Open(folder, onApplied): pre-fills ShareNameInput with the folder's share name
 *      (or folder name if not yet shared).
 *    shareToggle ON → enables SettingsGroup. OFF → disables it.
 *    Add and Remove buttons are disabled — single share name only.
 *    Apply: writes share state and share name from input to FolderData,
 *           upserts into ServerDeviceState.SharedFolders,
 *           fires onApplied callback so Properties Sharing tab refreshes.
 *    Cancel: closes without writing.
 *    OK: Apply then Close.
 *    Permissions → opens FolderPermissionsController.
 *    Caching → logs a message only.
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FolderAdvancedSharingController : MonoBehaviour
{
    [Header("Nav")]
    [SerializeField] private Button closeBtn;

    [Header("Share Toggle")]
    [SerializeField] private Toggle     shareToggle;
    [SerializeField] private GameObject settingsGroup;

    [Header("Share Name")]
    [SerializeField] private TMP_InputField shareNameInput;
    [SerializeField] private Button         addShareNameBtn;    // disabled — visual only
    [SerializeField] private Button         removeShareNameBtn; // disabled — visual only

    [Header("Comments")]
    [SerializeField] private TMP_InputField commentsInput;

    [Header("Actions")]
    [SerializeField] private Button permissionsBtn;
    [SerializeField] private Button cachingBtn;

    [Header("Footer")]
    [SerializeField] private Button okBtn;
    [SerializeField] private Button cancelBtn;
    [SerializeField] private Button applyBtn;

    [Header("Reference")]
    [SerializeField] private FolderPermissionsController permissionsController;

    // ── State ──────────────────────────────────────────────────────────────────
    private FolderData    _folder;
    private System.Action _onApplied;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        closeBtn?.onClick.AddListener(Cancel);
        shareToggle?.onValueChanged.AddListener(OnShareToggle);
        permissionsBtn?.onClick.AddListener(OpenPermissions);
        cachingBtn?.onClick.AddListener(OnCaching);
        okBtn?.onClick.AddListener(OK);
        cancelBtn?.onClick.AddListener(Cancel);
        applyBtn?.onClick.AddListener(Apply);

        if (addShareNameBtn    != null) addShareNameBtn.interactable    = false;
        if (removeShareNameBtn != null) removeShareNameBtn.interactable = false;

        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open(FolderData folder, System.Action onApplied)
    {
        _folder    = folder;
        _onApplied = onApplied;
        gameObject.SetActive(true);
        RefreshFromFolder();
    }

    public void Close() => gameObject.SetActive(false);

    // ── Initialise from FolderData ────────────────────────────────────────────

    private void RefreshFromFolder()
    {
        if (shareNameInput != null)
            shareNameInput.text = string.IsNullOrEmpty(_folder.ShareName) ? _folder.Name : _folder.ShareName;

        if (commentsInput != null)
            commentsInput.text = _folder.Comments ?? "";

        // Set toggle without triggering side-effects during init
        shareToggle.onValueChanged.RemoveListener(OnShareToggle);
        if (shareToggle != null) shareToggle.isOn = _folder.IsShared;
        shareToggle.onValueChanged.AddListener(OnShareToggle);

        settingsGroup?.SetActive(_folder.IsShared);
    }

    // ── Share Toggle ──────────────────────────────────────────────────────────

    private void OnShareToggle(bool isOn) => settingsGroup?.SetActive(isOn);

    // ── Apply / OK / Cancel ───────────────────────────────────────────────────

    private void Apply()
    {
        if (_folder == null) return;

        bool   isShared   = shareToggle != null && shareToggle.isOn;
        string shareName  = shareNameInput != null ? shareNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(shareName)) shareName = _folder.Name;

        _folder.IsShared  = isShared;
        _folder.ShareName = isShared ? shareName : "";
        _folder.Comments  = commentsInput != null ? commentsInput.text : "";

        if (isShared) UpsertSharedFolder(_folder);
        else          RemoveSharedFolder(_folder);

        _onApplied?.Invoke();
        ActivityLogManager.Log(
            isShared
                ? $"Shared '{_folder.Name}' as '\\\\SERVER\\{_folder.ShareName}'"
                : $"Removed share from '{_folder.Name}'",
            ActivityLogManager.EntryType.Action);
    }

    private void OK()     { Apply(); Close(); }
    private void Cancel() => Close();

    // ── Permissions / Caching ─────────────────────────────────────────────────

    private void OpenPermissions()
    {
        permissionsController?.Open(_folder);
        ActivityLogManager.Log("Opened Permissions panel", ActivityLogManager.EntryType.Action);
    }

    private void OnCaching() =>
        ActivityLogManager.Log("Caching configuration is not required for this task.",
            ActivityLogManager.EntryType.Action);

    // ── SharedFolders sync ────────────────────────────────────────────────────

    private void UpsertSharedFolder(FolderData folder)
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;

        string computer    = !string.IsNullOrEmpty(state.ComputerName) ? state.ComputerName : "SERVER";
        string networkPath = $"\\\\{computer}\\{folder.ShareName}";

        var existing = state.SharedFolders.Find(s => s.Path == folder.FullPath);
        if (existing != null)
        {
            existing.ShareName   = folder.ShareName;
            existing.NetworkPath = networkPath;
        }
        else
        {
            state.SharedFolders.Add(new SharedFolderData
            {
                FolderName  = folder.Name,
                Path        = folder.FullPath,
                ShareName   = folder.ShareName,
                NetworkPath = networkPath
            });
        }
    }

    private void RemoveSharedFolder(FolderData folder)
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        state?.SharedFolders.RemoveAll(s => s.Path == folder.FullPath);
    }
}
