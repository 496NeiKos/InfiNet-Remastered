/*
 * ================================================================
 *  UNITY SETUP GUIDE — FolderPermissionsController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Folder Permissions Panel" (child of File Explorer Panel, starts INACTIVE).
 *    Opened by FolderAdvancedSharingController when "Permissions" is clicked.
 *
 *  HIERARCHY
 *    Folder Permissions Panel                  ← this script here
 *      ├── TitleBar (HorizontalLayoutGroup)
 *      │     ├── TitleLabel (TMP_Text)         → titleTMP   "Permissions for [name]"
 *      │     └── CloseBtn (Button)             → closeBtn
 *      ├── Group1Section
 *      │     ├── HeaderLabel (TMP_Text)        "Group or user names:"
 *      │     ├── EveryoneLabel (TMP_Text)      → everyoneLabel   text: "Everyone"
 *      │     │     (styled like a list row — Image bg, padding, same height as a button)
 *      │     └── EntryButtons (HorizontalLayoutGroup)
 *      │           ├── AddEntryBtn (Button)    → addEntryBtn    "Add..."
 *      │           │     interactable: OFF in Inspector
 *      │           └── RemoveEntryBtn (Button) → removeEntryBtn "Remove"
 *      │                 interactable: OFF in Inspector
 *      ├── Divider (Image, H:1)
 *      ├── Group2Section                       → group2Panel
 *      │     ├── PermHeaderLabel (TMP_Text)    → permHeaderTMP  "Permissions for Everyone:"
 *      │     ├── ColumnHeaders (HorizontalLayoutGroup)
 *      │     │     ├── Spacer (LayoutElement Flexible W:1)
 *      │     │     ├── AllowLabel (TMP_Text)   "Allow"
 *      │     │     └── DenyLabel  (TMP_Text)   "Deny"
 *      │     ├── FullControlRow (HorizontalLayoutGroup)
 *      │     │     ├── Label (TMP_Text)        "Full Control"
 *      │     │     ├── AllowToggle (Toggle)    → fullControlAllowToggle
 *      │     │     └── DenyToggle  (Toggle)    → fullControlDenyToggle
 *      │     ├── ChangeRow (HorizontalLayoutGroup)
 *      │     │     ├── Label (TMP_Text)        "Change"
 *      │     │     ├── AllowToggle (Toggle)    → changeAllowToggle
 *      │     │     └── DenyToggle  (Toggle)    → changeDenyToggle
 *      │     └── ReadRow (HorizontalLayoutGroup)
 *      │           ├── Label (TMP_Text)        "Read"
 *      │           ├── AllowToggle (Toggle)    → readAllowToggle
 *      │           └── DenyToggle  (Toggle)    → readDenyToggle
 *      └── FooterBar (HorizontalLayoutGroup)
 *            ├── OKBtn (Button)                → okBtn
 *            ├── CancelBtn (Button)            → cancelBtn
 *            └── ApplyBtn (Button)             → applyBtn
 *
 *  INSPECTOR SETUP
 *    addEntryBtn  → interactable: OFF
 *    removeEntryBtn → interactable: OFF
 *    group2Panel → Group2Section GO (always active when panel is open)
 *    everyoneLabel → the static "Everyone" TMP_Text in Group1
 *
 *  HOW IT WORKS
 *    Open(folder): finds or creates the "Everyone" FolderPermissionEntry in folder.Permissions,
 *      deep-copies it to _workingEntry, and loads its values into the six toggles.
 *    Allow and Deny for each row are mutually exclusive (_suppressToggle flag).
 *    Apply: saves toggle states back to _workingEntry, then writes to folder.Permissions.
 *      Also sets SharedFolderData.PermissionsSet = true if the folder is shared.
 *    Cancel: closes without writing.
 *    OK: Apply then Close.
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FolderPermissionsController : MonoBehaviour
{
    [Header("Title")]
    [SerializeField] private TMP_Text titleTMP;
    [SerializeField] private Button   closeBtn;

    [Header("Group 1")]
    [SerializeField] private TMP_Text everyoneLabel; // static "Everyone" display
    [SerializeField] private Button   addEntryBtn;    // disabled — visual only
    [SerializeField] private Button   removeEntryBtn; // disabled — visual only

    [Header("Group 2")]
    [SerializeField] private GameObject group2Panel;
    [SerializeField] private TMP_Text   permHeaderTMP;

    [SerializeField] private Toggle fullControlAllowToggle;
    [SerializeField] private Toggle fullControlDenyToggle;
    [SerializeField] private Toggle changeAllowToggle;
    [SerializeField] private Toggle changeDenyToggle;
    [SerializeField] private Toggle readAllowToggle;
    [SerializeField] private Toggle readDenyToggle;

    [Header("Footer")]
    [SerializeField] private Button okBtn;
    [SerializeField] private Button cancelBtn;
    [SerializeField] private Button applyBtn;

    // ── State ──────────────────────────────────────────────────────────────────
    private FolderData           _folder;
    private FolderPermissionEntry _workingEntry;
    private bool                 _suppressToggle;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        closeBtn?.onClick.AddListener(Cancel);
        okBtn?.onClick.AddListener(OK);
        cancelBtn?.onClick.AddListener(Cancel);
        applyBtn?.onClick.AddListener(Apply);

        if (addEntryBtn    != null) addEntryBtn.interactable    = false;
        if (removeEntryBtn != null) removeEntryBtn.interactable = false;

        WireMutuallyExclusiveToggles(fullControlAllowToggle, fullControlDenyToggle);
        WireMutuallyExclusiveToggles(changeAllowToggle,      changeDenyToggle);
        WireMutuallyExclusiveToggles(readAllowToggle,        readDenyToggle);

        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open(FolderData folder)
    {
        _folder = folder;
        gameObject.SetActive(true);

        if (titleTMP     != null) titleTMP.text     = $"Permissions for {folder.Name}";
        if (permHeaderTMP != null) permHeaderTMP.text = "Permissions for Everyone:";
        if (everyoneLabel != null) everyoneLabel.text = "Everyone";

        group2Panel?.SetActive(true);

        // Find or create the "Everyone" entry
        var source = folder.Permissions.Find(e => e.GroupOrUser == "Everyone");
        if (source == null)
        {
            source = new FolderPermissionEntry { GroupOrUser = "Everyone", FullControlAllow = true };
            folder.Permissions.Add(source);
        }

        _workingEntry = CopyEntry(source);
        LoadToggles(_workingEntry);

        ActivityLogManager.Log($"Opened Permissions: {folder.Name}", ActivityLogManager.EntryType.Action);
    }

    public void Close() => gameObject.SetActive(false);

    // ── Toggles ───────────────────────────────────────────────────────────────

    private void LoadToggles(FolderPermissionEntry e)
    {
        _suppressToggle = true;
        SetToggle(fullControlAllowToggle, e.FullControlAllow);
        SetToggle(fullControlDenyToggle,  e.FullControlDeny);
        SetToggle(changeAllowToggle,      e.ChangeAllow);
        SetToggle(changeDenyToggle,       e.ChangeDeny);
        SetToggle(readAllowToggle,        e.ReadAllow);
        SetToggle(readDenyToggle,         e.ReadDeny);
        _suppressToggle = false;
    }

    private void SaveToggles(FolderPermissionEntry e)
    {
        e.FullControlAllow = fullControlAllowToggle != null && fullControlAllowToggle.isOn;
        e.FullControlDeny  = fullControlDenyToggle  != null && fullControlDenyToggle.isOn;
        e.ChangeAllow      = changeAllowToggle      != null && changeAllowToggle.isOn;
        e.ChangeDeny       = changeDenyToggle       != null && changeDenyToggle.isOn;
        e.ReadAllow        = readAllowToggle        != null && readAllowToggle.isOn;
        e.ReadDeny         = readDenyToggle         != null && readDenyToggle.isOn;
    }

    private void SetToggle(Toggle toggle, bool value)
    {
        if (toggle != null) toggle.isOn = value;
    }

    private void WireMutuallyExclusiveToggles(Toggle a, Toggle b)
    {
        a?.onValueChanged.AddListener(isOn =>
        {
            if (_suppressToggle) return;
            if (isOn && b != null) { _suppressToggle = true; b.isOn = false; _suppressToggle = false; }
        });
        b?.onValueChanged.AddListener(isOn =>
        {
            if (_suppressToggle) return;
            if (isOn && a != null) { _suppressToggle = true; a.isOn = false; _suppressToggle = false; }
        });
    }

    // ── Apply / OK / Cancel ───────────────────────────────────────────────────

    private void Apply()
    {
        if (_folder == null) return;

        SaveToggles(_workingEntry);

        var existing = _folder.Permissions.Find(e => e.GroupOrUser == "Everyone");
        if (existing != null)
        {
            existing.FullControlAllow = _workingEntry.FullControlAllow;
            existing.FullControlDeny  = _workingEntry.FullControlDeny;
            existing.ChangeAllow      = _workingEntry.ChangeAllow;
            existing.ChangeDeny       = _workingEntry.ChangeDeny;
            existing.ReadAllow        = _workingEntry.ReadAllow;
            existing.ReadDeny         = _workingEntry.ReadDeny;
        }

        if (_folder.IsShared)
        {
            var state  = ServerVirtualOSManager.Instance?.ServerState;
            var shared = state?.SharedFolders.Find(s => s.Path == _folder.FullPath);
            if (shared != null) shared.PermissionsSet = true;
        }

        ActivityLogManager.Log($"Permissions applied to '{_folder.Name}'",
            ActivityLogManager.EntryType.Action);
    }

    private void OK()     { Apply(); Close(); }
    private void Cancel() => Close();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static FolderPermissionEntry CopyEntry(FolderPermissionEntry src) =>
        new FolderPermissionEntry
        {
            GroupOrUser      = src.GroupOrUser,
            FullControlAllow = src.FullControlAllow,
            FullControlDeny  = src.FullControlDeny,
            ChangeAllow      = src.ChangeAllow,
            ChangeDeny       = src.ChangeDeny,
            ReadAllow        = src.ReadAllow,
            ReadDeny         = src.ReadDeny
        };
}
