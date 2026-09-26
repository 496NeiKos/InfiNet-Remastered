/*
 * ================================================================
 *  UNITY SETUP GUIDE — FolderPropertiesController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Folder Properties Panel" (child of File Explorer Panel, starts INACTIVE).
 *    Opened by FileExplorerController when right-click → Properties is selected.
 *
 *  HIERARCHY
 *    Folder Properties Panel                  ← this script here
 *      ├── TitleBar (HorizontalLayoutGroup)
 *      │     ├── TitleLabel (TMP_Text)        → titleTMP     "[folder] Properties"
 *      │     └── CloseBtn (Button)            → closeBtn
 *      ├── TabBar (HorizontalLayoutGroup)
 *      │     ├── GeneralTabBtn (Button)       → generalTabBtn
 *      │     │     └── Label (TMP_Text) "General"
 *      │     └── SharingTabBtn (Button)       → sharingTabBtn
 *      │           └── Label (TMP_Text) "Sharing"
 *      ├── GeneralTab (starts ACTIVE)         → generalTab
 *      │     ├── FolderIcon (Image)
 *      │     ├── NameRow (HorizontalLayoutGroup)
 *      │     │     ├── Label (TMP_Text) "Name:"
 *      │     │     └── ValueTMP (TMP_Text)    → gen_nameTMP
 *      │     ├── TypeRow
 *      │     │     ├── Label (TMP_Text) "Type:"
 *      │     │     └── ValueTMP (TMP_Text)    static text "File folder"
 *      │     ├── LocationRow
 *      │     │     ├── Label (TMP_Text) "Location:"
 *      │     │     └── ValueTMP (TMP_Text)    → gen_locationTMP
 *      │     ├── DateRow
 *      │     │     ├── Label (TMP_Text) "Date created:"
 *      │     │     └── ValueTMP (TMP_Text)    → gen_dateCreatedTMP
 *      │     └── AttributesRow
 *      │           ├── Label (TMP_Text) "Attributes:"
 *      │           └── ValueTMP (TMP_Text)    static text "(None)"
 *      ├── SharingTab (starts INACTIVE)       → sharingTab
 *      │     ├── NetworkFileSharing section
 *      │     │     ├── SectionLabel (TMP_Text) "Network File and Folder Sharing"
 *      │     │     ├── NameRow
 *      │     │     │     ├── Label (TMP_Text) "Folder name:"
 *      │     │     │     └── ValueTMP (TMP_Text) → shr_nameTMP
 *      │     │     ├── StatusRow
 *      │     │     │     ├── Label (TMP_Text) "Share status:"
 *      │     │     │     └── ValueTMP (TMP_Text) → shr_statusTMP
 *      │     │     ├── PathRow
 *      │     │     │     ├── Label (TMP_Text) "Network path:"
 *      │     │     │     └── ValueTMP (TMP_Text) → shr_networkPathTMP
 *      │     │     └── AdvancedSharingBtn (Button) → advancedSharingBtn
 *      │     │           └── Label "Advanced Sharing..."
 *      │     └── Note (TMP_Text) "Enable Advanced Sharing to share this folder."
 *      └── FooterBar (HorizontalLayoutGroup)
 *            ├── OKBtn (Button)               → okBtn
 *            ├── CancelBtn (Button)           → cancelBtn
 *            └── ApplyBtn (Button)            → applyBtn
 *
 *  INSPECTOR ASSIGNMENTS
 *    titleTMP, closeBtn, generalTabBtn, sharingTabBtn → as above
 *    generalTab, sharingTab → tab root GOs
 *    gen_nameTMP, gen_locationTMP, gen_dateCreatedTMP → General tab value labels
 *    shr_nameTMP, shr_statusTMP, shr_networkPathTMP → Sharing tab value labels
 *    advancedSharingBtn → AdvancedSharingBtn
 *    okBtn, cancelBtn, applyBtn → footer buttons
 *    advancedSharingController → AdvancedSharingController on the Advanced Sharing Panel GO
 *
 *  HOW IT WORKS
 *    Open(FolderData) initialises all fields from the FolderData reference.
 *    General tab is always active on open.
 *    Sharing tab refreshes whenever called by AdvancedSharingController after Apply.
 *    OK and Cancel both close the panel (no direct editable fields here).
 *    Apply is a no-op (all editing is done inside Advanced Sharing).
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FolderPropertiesController : MonoBehaviour
{
    [Header("Title")]
    [SerializeField] private TMP_Text titleTMP;
    [SerializeField] private Button   closeBtn;

    [Header("Tabs")]
    [SerializeField] private Button     generalTabBtn;
    [SerializeField] private Button     sharingTabBtn;
    [SerializeField] private GameObject generalTab;
    [SerializeField] private GameObject sharingTab;

    [Header("General Tab")]
    [SerializeField] private TMP_Text gen_nameTMP;
    [SerializeField] private TMP_Text gen_locationTMP;
    [SerializeField] private TMP_Text gen_dateCreatedTMP;

    [Header("Sharing Tab")]
    [SerializeField] private TMP_Text shr_nameTMP;
    [SerializeField] private TMP_Text shr_statusTMP;
    [SerializeField] private TMP_Text shr_networkPathTMP;
    [SerializeField] private Button   advancedSharingBtn;

    [Header("Footer")]
    [SerializeField] private Button okBtn;
    [SerializeField] private Button cancelBtn;
    [SerializeField] private Button applyBtn;

    [Header("Reference")]
    [SerializeField] private FolderAdvancedSharingController advancedSharingController;

    private FolderData _folder;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        closeBtn?.onClick.AddListener(Close);
        generalTabBtn?.onClick.AddListener(ShowGeneralTab);
        sharingTabBtn?.onClick.AddListener(ShowSharingTab);
        advancedSharingBtn?.onClick.AddListener(OpenAdvancedSharing);
        okBtn?.onClick.AddListener(Close);
        cancelBtn?.onClick.AddListener(Close);
        applyBtn?.onClick.AddListener(() => { }); // no-op: editing done in Advanced Sharing

        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open(FolderData folder)
    {
        _folder = folder;
        gameObject.SetActive(true);
        ShowGeneralTab();
        RefreshAll();
        ActivityLogManager.Log($"Properties opened: {folder.FullPath}", ActivityLogManager.EntryType.Action);
    }

    public void Close() => gameObject.SetActive(false);

    public void RefreshSharingTab()
    {
        if (_folder == null) return;
        if (shr_nameTMP        != null) shr_nameTMP.text        = _folder.Name;
        if (shr_statusTMP      != null) shr_statusTMP.text      = _folder.IsShared ? "Shared" : "Not Shared";
        if (shr_networkPathTMP != null) shr_networkPathTMP.text = BuildNetworkPath();
    }

    // ── Tabs ──────────────────────────────────────────────────────────────────

    private void ShowGeneralTab()
    {
        generalTab?.SetActive(true);
        sharingTab?.SetActive(false);
    }

    private void ShowSharingTab()
    {
        generalTab?.SetActive(false);
        sharingTab?.SetActive(true);
        RefreshSharingTab();
        ActivityLogManager.Log($"Viewing Sharing tab: {_folder?.Name}", ActivityLogManager.EntryType.Action);
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    private void RefreshAll()
    {
        if (_folder == null) return;

        if (titleTMP            != null) titleTMP.text            = $"{_folder.Name} Properties";
        if (gen_nameTMP         != null) gen_nameTMP.text         = _folder.Name;
        if (gen_locationTMP     != null) gen_locationTMP.text     = _folder.ParentPath;
        if (gen_dateCreatedTMP  != null) gen_dateCreatedTMP.text  = _folder.DateCreated;

        RefreshSharingTab();
    }

    private string BuildNetworkPath()
    {
        if (!_folder.IsShared || string.IsNullOrEmpty(_folder.ShareName)) return "Not Shared";
        var state = ServerVirtualOSManager.Instance?.ServerState;
        string computer = !string.IsNullOrEmpty(state?.ComputerName) ? state.ComputerName : "SERVER";
        return $"\\\\{computer}\\{_folder.ShareName}";
    }

    // ── Advanced Sharing ──────────────────────────────────────────────────────

    private void OpenAdvancedSharing()
    {
        advancedSharingController?.Open(_folder, RefreshSharingTab);
        ActivityLogManager.Log("Opened Advanced Sharing", ActivityLogManager.EntryType.Action);
    }
}
