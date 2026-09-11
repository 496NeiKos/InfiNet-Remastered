/*
 * ================================================================
 *  UNITY SETUP GUIDE — GroupPolicyController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Group Policy Management Panel" (starts INACTIVE).
 *    Opened from ServerManagerController Tools → Group Policy Management.
 *
 *  HIERARCHY
 *    Group Policy Management Panel      ← this script here
 *      ├── TitleBar / CloseBtn          → closeBtn
 *      ├── TreePanel (left)
 *      │     ├── ForestNode (static label)
 *      │     ├── DomainNode             → domainNodeLabel (TMP)
 *      │     └── OU Nodes               → ouNodeParent (populated at runtime)
 *      ├── ContentPanel (right)
 *      │     ├── ContentTitle           → contentTitleTMP
 *      │     └── GPO List              → gpoListParent (GPO row prefabs)
 *      ├── Context Menu                 → contextMenu (starts INACTIVE)
 *      │     └── CreateGPOBtn          → ctxCreateGPOBtn
 *      ├── Create GPO Dialog            → createGPODialog (starts INACTIVE)
 *      │     ├── GPONameInput          → gpoNameInput
 *      │     ├── OKBtn                 → gpoOKBtn
 *      │     └── CancelBtn             → gpoCancelBtn
 *      └── GPO Editor Panel            → gpoEditorPanel (starts INACTIVE)
 *            ← GPOEditorController component also on this GameObject
 *
 *  INSPECTOR ASSIGNMENTS
 *    All fields as above.
 *    ouNodePrefab  → Button + TMP_Text for OU tree nodes
 *    gpoRowPrefab  → TMP_Text (GPO name) + "Edit" Button
 *
 *  HOW IT WORKS
 *    Tree shows Forest > domain > OUs (populated from state.OrganizationalUnits).
 *    Clicking an OU node → content panel shows GPOs linked to that OU.
 *    Right-clicking an OU → context menu → Create GPO → name dialog → creates GPO.
 *    Each GPO row has an "Edit" button → opens GPOEditorPanel (GPOEditorController).
 *    GPOEditorController writes state.GroupPolicies[i].RedirectSet = true when done.
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GroupPolicyController : MonoBehaviour
{
    public static GroupPolicyController Instance { get; private set; }

    [Header("Nav")]
    [SerializeField] private Button closeBtn;

    [Header("Tree")]
    [SerializeField] private TMP_Text domainNodeLabel;
    [SerializeField] private Transform ouNodeParent;
    [SerializeField] private GameObject ouNodePrefab;

    [Header("Content")]
    [SerializeField] private TMP_Text  contentTitleTMP;
    [SerializeField] private Transform gpoListParent;
    [SerializeField] private GameObject gpoRowPrefab;

    [Header("Context Menu")]
    [SerializeField] private GameObject contextMenu;
    [SerializeField] private Button     ctxCreateGPOBtn;

    [Header("Create GPO Dialog")]
    [SerializeField] private GameObject    createGPODialog;
    [SerializeField] private TMP_InputField gpoNameInput;
    [SerializeField] private Button        gpoOKBtn;
    [SerializeField] private Button        gpoCancelBtn;

    [Header("GPO Editor")]
    [SerializeField] private GPOEditorController gpoEditor;

    private string _selectedOUName = "";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        closeBtn?.onClick.AddListener(Close);
        ctxCreateGPOBtn?.onClick.AddListener(OpenCreateGPODialog);
        gpoOKBtn?.onClick.AddListener(ConfirmCreateGPO);
        gpoCancelBtn?.onClick.AddListener(() => createGPODialog?.SetActive(false));

        contextMenu?.SetActive(false);
        createGPODialog?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.GPMOpened = true;
        CloseContextMenu();
        RefreshTree();
        ActivityLogManager.Log("Opened Group Policy Management", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        CloseContextMenu();
        createGPODialog?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Tree ──────────────────────────────────────────────────────────────────

    private void RefreshTree()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (domainNodeLabel != null)
            domainNodeLabel.text = !string.IsNullOrEmpty(state?.DomainName)
                ? state.DomainName : "css.com";

        foreach (Transform child in ouNodeParent) Destroy(child.gameObject);
        if (state == null) return;
        foreach (var ou in state.OrganizationalUnits)
        {
            var node  = Instantiate(ouNodePrefab, ouNodeParent);
            var label = node.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = ou.Name;
            var btn = node.GetComponentInChildren<Button>();
            string captured = ou.Name;
            btn?.onClick.AddListener(() => OnOUNodeClicked(captured));
        }
    }

    private void OnOUNodeClicked(string ouName)
    {
        _selectedOUName = ouName;
        contextMenu?.SetActive(true);
    }

    private void CloseContextMenu() => contextMenu?.SetActive(false);

    // ── GPO list ──────────────────────────────────────────────────────────────

    private void RefreshGPOList(string ouName)
    {
        if (contentTitleTMP != null) contentTitleTMP.text = ouName;
        foreach (Transform child in gpoListParent) Destroy(child.gameObject);

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;

        for (int i = 0; i < state.GroupPolicies.Count; i++)
        {
            var gpo = state.GroupPolicies[i];
            if (gpo.LinkedOUName != ouName) continue;

            var row   = Instantiate(gpoRowPrefab, gpoListParent);
            var label = row.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = gpo.Name;

            var btn = row.GetComponentInChildren<Button>();
            int capturedIdx = i;
            btn?.onClick.AddListener(() => OpenGPOEditor(capturedIdx));
        }
    }

    // ── Create GPO dialog ─────────────────────────────────────────────────────

    private void OpenCreateGPODialog()
    {
        CloseContextMenu();
        if (gpoNameInput != null) gpoNameInput.text = "";
        createGPODialog?.SetActive(true);
    }

    private void ConfirmCreateGPO()
    {
        string name = gpoNameInput != null ? gpoNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(name)) { Debug.LogWarning("[GPM] GPO name required."); return; }

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;

        state.GroupPolicies.Add(new GPOData
        {
            Name         = name,
            LinkedOUName = _selectedOUName
        });

        createGPODialog?.SetActive(false);
        RefreshGPOList(_selectedOUName);
        ActivityLogManager.Log($"Created GPO: {name} linked to {_selectedOUName}", ActivityLogManager.EntryType.Action);
    }

    // ── GPO editor ────────────────────────────────────────────────────────────

    private void OpenGPOEditor(int gpoIndex)
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || gpoIndex >= state.GroupPolicies.Count) return;
        state.GroupPolicies[gpoIndex].EditorOpened = true;
        gpoEditor?.OpenForGPO(gpoIndex);
        ActivityLogManager.Log($"Opened GPO Editor for: {state.GroupPolicies[gpoIndex].Name}",
            ActivityLogManager.EntryType.Action);
    }
}
