/*
 * ================================================================
 *  UNITY SETUP GUIDE — FolderRedirectPropertiesController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Folder Redirect Properties Dialog" GameObject
 *    (child of GPO Editor Panel, starts INACTIVE).
 *    Opened by GPOEditorController when user right-clicks Desktop or
 *    Documents row → Properties.
 *
 *  HIERARCHY
 *    Folder Redirect Properties Dialog        ← this script here (INACTIVE)
 *      ├── TitleBar (HorizontalLayoutGroup)
 *      │     ├── TitleLabel (TMP_Text)        → titleTMP   e.g. "Desktop Properties"
 *      │     └── CloseBtn (Button)            → closeBtn
 *      ├── TabBar (HorizontalLayoutGroup)
 *      │     ├── TargetTabBtn (Button)        → targetTabBtn   label "Target"
 *      │     └── SettingsTabBtn (Button)      → settingsTabBtn label "Settings"
 *      ├── TargetTab (VerticalLayoutGroup, starts ACTIVE) → targetTab
 *      │     ├── SettingRow (HorizontalLayoutGroup)
 *      │     │     ├── Label (TMP_Text) "Setting:"
 *      │     │     └── SettingDropdown (TMP_Dropdown) → settingDropdown
 *      │     │           Options: "Not configured",
 *      │     │                    "Basic - Redirect everyone's folder to the same location"
 *      │     ├── RootPathRow (HorizontalLayoutGroup, starts INACTIVE) → rootPathRow
 *      │     │     ├── Label (TMP_Text) "Root Path:"
 *      │     │     └── RootPathInput (TMP_InputField) → rootPathInput
 *      │     │           Placeholder: "e.g. \\SERVER\Profiles"
 *      │     └── PathHint (TMP_Text) → pathHintTMP
 *      │           (auto-filled with available share paths)
 *      ├── SettingsTab (VerticalLayoutGroup, starts INACTIVE) → settingsTab
 *      │     ├── GrantExclusiveRow (HorizontalLayoutGroup)
 *      │     │     ├── GrantExclusiveToggle (Toggle)    → grantExclusiveToggle
 *      │     │     └── GrantExclusiveLabel (TMP_Text)   → grantExclusiveLabel
 *      │     │           text set at runtime per folder name
 *      │     └── MoveContentsRow (HorizontalLayoutGroup)
 *      │           ├── MoveContentsToggle (Toggle)      → moveContentsToggle
 *      │           └── MoveContentsLabel (TMP_Text)     → moveContentsLabel
 *      │                 text set at runtime per folder name
 *      ├── Footer (HorizontalLayoutGroup)
 *      │     ├── ApplyBtn (Button)   → applyBtn
 *      │     ├── OKBtn    (Button)   → okBtn
 *      │     └── CancelBtn (Button)  → cancelBtn
 *      └── ConfirmationPopup (Image panel, starts INACTIVE) → confirmPopup
 *            (Covers full dialog area; blocks interaction while shown)
 *            Panel size: stretch-stretch over parent, slight dark tint
 *            ├── MessageLabel (TMP_Text) → confirmMessageTMP
 *            │     (centered, wraps text)
 *            ├── YesBtn (Button)   → confirmYesBtn
 *            └── NoBtn  (Button)   → confirmNoBtn
 *
 *  CONFIRMATION POPUP SETUP
 *    • ConfirmationPopup RectTransform: anchor stretch-stretch, all offsets 0
 *    • Image: dark semi-transparent (alpha ~0.7), raycastTarget ON
 *      This blocks clicks on the parent dialog while popup is visible.
 *    • Place MessageLabel + buttons centered using a child VerticalLayoutGroup
 *
 *  INSPECTOR ASSIGNMENTS
 *    All fields as above.
 *    settingDropdown: options pre-set in Awake()
 *    grantExclusiveToggle.isOn  → true in inspector (default checked)
 *    moveContentsToggle.isOn    → true in inspector (default checked)
 *
 *  HOW IT WORKS
 *    Open(folderName, gpoIndex, callback): pre-fills from existing GPO state,
 *      shows "Target" tab. Label texts include the folder name.
 *    Setting dropdown value 1 ("Basic") → shows rootPathRow.
 *    Apply → Validate → show ConfirmationPopup.
 *    Confirm Yes → write to gpo.Desktop/DocumentsRedirectPath + Set flags,
 *      update ModifiedDate, fire callback (GPOEditorController.RefreshContent).
 *    OK = same as Apply but also closes dialog after confirm.
 *    Cancel / Close → dismiss without writing.
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FolderRedirectPropertiesController : MonoBehaviour
{
    [Header("Title")]
    [SerializeField] private TMP_Text titleTMP;
    [SerializeField] private Button   closeBtn;

    [Header("Tabs")]
    [SerializeField] private Button     targetTabBtn;
    [SerializeField] private Button     settingsTabBtn;
    [SerializeField] private GameObject targetTab;
    [SerializeField] private GameObject settingsTab;

    [Header("Target Tab")]
    [SerializeField] private TMP_Dropdown   settingDropdown;
    [SerializeField] private GameObject     rootPathRow;
    [SerializeField] private TMP_InputField rootPathInput;
    [SerializeField] private TMP_Text       pathHintTMP;

    [Header("Settings Tab")]
    [SerializeField] private Toggle   grantExclusiveToggle;
    [SerializeField] private TMP_Text grantExclusiveLabel;
    [SerializeField] private Toggle   moveContentsToggle;
    [SerializeField] private TMP_Text moveContentsLabel;

    [Header("Footer")]
    [SerializeField] private Button applyBtn;
    [SerializeField] private Button okBtn;
    [SerializeField] private Button cancelBtn;

    [Header("Confirmation Popup")]
    [SerializeField] private GameObject confirmPopup;
    [SerializeField] private TMP_Text   confirmMessageTMP;
    [SerializeField] private Button     confirmYesBtn;
    [SerializeField] private Button     confirmNoBtn;

    // ── State ──────────────────────────────────────────────────────────────────
    private string        _folderName       = "";
    private int           _gpoIndex         = -1;
    private bool          _closeAfterApply  = false;
    private System.Action _onConfigured;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        closeBtn    ?.onClick.AddListener(Close);
        targetTabBtn?.onClick.AddListener(ShowTargetTab);
        settingsTabBtn?.onClick.AddListener(ShowSettingsTab);
        applyBtn    ?.onClick.AddListener(OnApply);
        okBtn       ?.onClick.AddListener(OnOK);
        cancelBtn   ?.onClick.AddListener(Close);
        confirmYesBtn?.onClick.AddListener(OnConfirmYes);
        confirmNoBtn ?.onClick.AddListener(() => confirmPopup?.SetActive(false));

        settingDropdown?.onValueChanged.AddListener(OnSettingChanged);

        if (settingDropdown != null)
        {
            settingDropdown.ClearOptions();
            settingDropdown.AddOptions(new List<string>
            {
                "Not configured",
                "Basic - Redirect everyone's folder to the same location"
            });
        }

        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open(string folderName, int gpoIndex, System.Action onConfigured = null)
    {
        _folderName      = folderName;
        _gpoIndex        = gpoIndex;
        _onConfigured    = onConfigured;
        _closeAfterApply = false;

        gameObject.SetActive(true);
        confirmPopup?.SetActive(false);

        if (titleTMP != null)
            titleTMP.text = $"{folderName} Properties";

        if (grantExclusiveLabel != null)
            grantExclusiveLabel.text = $"Grant the user exclusive rights to {folderName}";
        if (moveContentsLabel != null)
            moveContentsLabel.text = $"Move the contents of {folderName} to the new location";

        ShowTargetTab();
        PreFill();
        RefreshPathHint();

        ActivityLogManager.Log($"Opened Folder Redirect Properties: {folderName}", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        confirmPopup?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Tab switching ─────────────────────────────────────────────────────────

    private void ShowTargetTab()
    {
        targetTab ?.SetActive(true);
        settingsTab?.SetActive(false);
    }

    private void ShowSettingsTab()
    {
        targetTab ?.SetActive(false);
        settingsTab?.SetActive(true);
    }

    // ── Dropdown ──────────────────────────────────────────────────────────────

    private void OnSettingChanged(int val)
    {
        // Show root path row only when "Basic" is selected
        rootPathRow?.SetActive(val == 1);
    }

    // ── Apply / OK ────────────────────────────────────────────────────────────

    private void OnApply() { _closeAfterApply = false; TryConfirm(); }
    private void OnOK()    { _closeAfterApply = true;  TryConfirm(); }

    private void TryConfirm()
    {
        if (!Validate()) return;
        if (confirmPopup != null && confirmPopup.activeSelf) return;

        if (confirmMessageTMP != null)
            confirmMessageTMP.text =
                $"If you change the location of {_folderName}, the contents will not be moved to the new location. Do you wish to continue?";

        confirmPopup?.SetActive(true);
    }

    private void OnConfirmYes()
    {
        confirmPopup?.SetActive(false);
        ApplySettings();
        if (_closeAfterApply) Close();
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private bool Validate()
    {
        if (settingDropdown != null && settingDropdown.value == 0)
        {
            Debug.LogWarning("[FolderRedirectProps] Select a redirect setting first.");
            return false;
        }
        string path = rootPathInput != null ? rootPathInput.text.Trim() : "";
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("[FolderRedirectProps] Root path is required.");
            return false;
        }
        return true;
    }

    // ── Apply settings to state ───────────────────────────────────────────────

    private void ApplySettings()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || _gpoIndex < 0 || _gpoIndex >= state.GroupPolicies.Count) return;

        string path = rootPathInput != null ? rootPathInput.text.Trim() : "";
        var gpo = state.GroupPolicies[_gpoIndex];

        if (_folderName == "Desktop")
        {
            gpo.DesktopRedirectPath = path;
            gpo.DesktopRedirectSet  = true;
        }
        else // Documents
        {
            gpo.DocumentsRedirectPath = path;
            gpo.DocumentsRedirectSet  = true;
        }

        gpo.ModifiedDate = System.DateTime.Now.ToString("M/d/yyyy");

        _onConfigured?.Invoke();
        ActivityLogManager.Log(
            $"Folder Redirect set: {_folderName} → {path}",
            ActivityLogManager.EntryType.Action);
    }

    // ── Pre-fill from existing state ──────────────────────────────────────────

    private void PreFill()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || _gpoIndex < 0 || _gpoIndex >= state.GroupPolicies.Count) return;

        var gpo = state.GroupPolicies[_gpoIndex];
        bool alreadySet = _folderName == "Desktop" ? gpo.DesktopRedirectSet : gpo.DocumentsRedirectSet;
        string existingPath = _folderName == "Desktop" ? gpo.DesktopRedirectPath : gpo.DocumentsRedirectPath;

        if (settingDropdown != null)
            settingDropdown.value = alreadySet ? 1 : 0;

        if (rootPathInput != null)
            rootPathInput.text = alreadySet ? existingPath : "";

        rootPathRow?.SetActive(alreadySet);

        // Real Windows defaults: both toggles are checked; TESDA task requires unchecking them
        if (grantExclusiveToggle != null) grantExclusiveToggle.isOn = true;
        if (moveContentsToggle   != null) moveContentsToggle.isOn   = true;
    }

    // ── Path hint ─────────────────────────────────────────────────────────────

    private void RefreshPathHint()
    {
        if (pathHintTMP == null) return;
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || state.SharedFolders.Count == 0)
        {
            pathHintTMP.text = "No shared folders found. Create and share a folder in File Explorer first.";
            return;
        }
        var hints = new List<string>();
        foreach (var folder in state.SharedFolders)
            hints.Add(folder.NetworkPath);
        pathHintTMP.text = "Available paths:\n" + string.Join("\n", hints);
    }
}
