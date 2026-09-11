/*
 * ================================================================
 *  UNITY SETUP GUIDE — GPOEditorController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "GPO Editor Panel" GameObject (child of Group Policy Management Panel,
 *    starts INACTIVE). This is the nested editor window that opens when the player
 *    clicks "Edit" on a GPO in GroupPolicyController.
 *
 *  HIERARCHY
 *    GPO Editor Panel                   ← this script here
 *      ├── TitleBar
 *      │     ├── TitleLabel            → titleLabelTMP (shows GPO name)
 *      │     └── CloseBtn             → closeBtn
 *      ├── TreePanel (left — static nav)
 *      │     └── FolderRedirectionBtn  → folderRedirectionBtn
 *      │           (path: User Config > Policies > Windows Settings > Folder Redirection)
 *      └── ContentPanel (right)
 *            ├── DefaultView           → defaultView (shown before nav item is clicked)
 *            └── Folder Redirection Panel → folderRedirectPanel (starts INACTIVE)
 *                  ├── DocumentsRow
 *                  │     ├── Label ("Documents")
 *                  │     └── SettingBtn → documentsSettingBtn
 *                  └── Documents Properties Dialog → docPropsDialog (starts INACTIVE)
 *                        ├── SettingDropdown  → settingDropdown
 *                        │     Options: "Not configured", "Basic - Redirect everyone's..."
 *                        ├── RootPathInput   → rootPathInput
 *                        │     (e.g. \\SERVER\SharedFolder)
 *                        ├── PathHint        → pathHintTMP (shows available share paths)
 *                        ├── OKBtn           → propsOKBtn
 *                        └── CancelBtn       → propsCancelBtn
 *
 *  INSPECTOR ASSIGNMENTS
 *    All fields as above.
 *    settingDropdown options: "Not configured", "Basic - Redirect everyone's folder to the same location"
 *
 *  HOW IT WORKS
 *    OpenForGPO(index) stores the GPO index and opens the panel.
 *    Player clicks Folder Redirection in tree → folder redirect panel opens.
 *    Player clicks Documents row settings → Documents Properties dialog opens.
 *    Player selects "Basic" in dropdown, enters UNC path → OK →
 *      state.GroupPolicies[index].RedirectPath = path
 *      state.GroupPolicies[index].RedirectSet  = true
 *    PathHint auto-populates from state.SharedFolders for player reference.
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GPOEditorController : MonoBehaviour
{
    [Header("Nav")]
    [SerializeField] private Button   closeBtn;
    [SerializeField] private TMP_Text titleLabelTMP;

    [Header("Tree")]
    [SerializeField] private Button folderRedirectionBtn;

    [Header("Content")]
    [SerializeField] private GameObject defaultView;
    [SerializeField] private GameObject folderRedirectPanel;
    [SerializeField] private Button     documentsSettingBtn;

    [Header("Documents Properties Dialog")]
    [SerializeField] private GameObject     docPropsDialog;
    [SerializeField] private TMP_Dropdown   settingDropdown;
    [SerializeField] private TMP_InputField rootPathInput;
    [SerializeField] private TMP_Text       pathHintTMP;
    [SerializeField] private Button         propsOKBtn;
    [SerializeField] private Button         propsCancelBtn;

    private int _gpoIndex = -1;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        closeBtn?.onClick.AddListener(Close);
        folderRedirectionBtn?.onClick.AddListener(OpenFolderRedirection);
        documentsSettingBtn?.onClick.AddListener(OpenDocumentsProperties);
        propsOKBtn?.onClick.AddListener(ConfirmRedirectSettings);
        propsCancelBtn?.onClick.AddListener(() => docPropsDialog?.SetActive(false));

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

    public void OpenForGPO(int gpoIndex)
    {
        _gpoIndex = gpoIndex;
        gameObject.SetActive(true);

        var state = ServerVirtualOSManager.Instance?.ServerState;
        string gpoName = (state != null && gpoIndex < state.GroupPolicies.Count)
            ? state.GroupPolicies[gpoIndex].Name : "GPO";

        if (titleLabelTMP != null) titleLabelTMP.text = $"Group Policy Object Editor — {gpoName}";

        defaultView?.SetActive(true);
        folderRedirectPanel?.SetActive(false);
        docPropsDialog?.SetActive(false);
    }

    public void Close()
    {
        docPropsDialog?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private void OpenFolderRedirection()
    {
        defaultView?.SetActive(false);
        folderRedirectPanel?.SetActive(true);
    }

    private void OpenDocumentsProperties()
    {
        if (rootPathInput != null) rootPathInput.text = "";
        if (settingDropdown != null) settingDropdown.value = 0;
        RefreshPathHint();
        docPropsDialog?.SetActive(true);
    }

    // ── Confirm redirect ──────────────────────────────────────────────────────

    private void ConfirmRedirectSettings()
    {
        if (settingDropdown != null && settingDropdown.value == 0)
        {
            Debug.LogWarning("[GPOEditor] Please select a redirect setting.");
            return;
        }
        string path = rootPathInput != null ? rootPathInput.text.Trim() : "";
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("[GPOEditor] Root path required.");
            return;
        }

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null && _gpoIndex >= 0 && _gpoIndex < state.GroupPolicies.Count)
        {
            state.GroupPolicies[_gpoIndex].RedirectPath = path;
            state.GroupPolicies[_gpoIndex].RedirectSet  = true;
        }
        docPropsDialog?.SetActive(false);
        ActivityLogManager.Log($"Folder Redirection configured → {path}", ActivityLogManager.EntryType.Action);
    }

    private void RefreshPathHint()
    {
        if (pathHintTMP == null) return;
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || state.SharedFolders.Count == 0)
        {
            pathHintTMP.text = "No shared folders found. Create one in File Server Resource Manager first.";
            return;
        }
        string serverName = !string.IsNullOrEmpty(state.ComputerName) ? state.ComputerName : "SERVER";
        var hints = new List<string>();
        foreach (var folder in state.SharedFolders)
            hints.Add($"\\\\{serverName}\\{folder.FolderName}");
        pathHintTMP.text = "Available paths:\n" + string.Join("\n", hints);
    }
}
