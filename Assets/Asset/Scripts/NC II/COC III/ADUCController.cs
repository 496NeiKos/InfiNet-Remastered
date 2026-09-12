/*
 * ================================================================
 *  UNITY SETUP GUIDE — ADUCController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "ADUC Panel" GameObject (starts INACTIVE).
 *    Opened from ServerManagerController Tools → Active Directory Users and Computers.
 *
 *  HIERARCHY
 *    ADUC Panel                         ← this script here
 *      ├── TitleBar / CloseBtn          → closeBtn
 *      ├── TreePanel (left)
 *      │     ├── DomainNode             → domainNodeLabel (TMP — shows domain name)
 *      │     └── (OU nodes added at runtime under ouNodeParent)
 *      │           ouNodeParent         → parent Transform for OU node buttons
 *      ├── ContentPanel (right)
 *      │     ├── ContentTitle           → contentTitleTMP
 *      │     ├── ItemList               → itemListParent (entries added at runtime)
 *      │     └── ItemRowPrefab          → itemRowPrefab (TMP_Text label)
 *      ├── Context Menu                 → contextMenu (starts INACTIVE)
 *      │     ├── NewOUBtn              → ctxNewOUBtn  (visible on domain node right-click)
 *      │     └── NewUserBtn            → ctxNewUserBtn (visible on OU node right-click)
 *      ├── New OU Dialog               → newOUDialog (starts INACTIVE)
 *      │     ├── OUNameInput           → ouNameInput
 *      │     ├── OKBtn                 → ouOKBtn
 *      │     └── CancelBtn             → ouCancelBtn
 *      └── New User Dialog             → newUserDialog (starts INACTIVE)
 *            ├── FirstNameInput        → firstNameInput
 *            ├── LastNameInput         → lastNameInput
 *            ├── UsernameInput         → usernameInput
 *            ├── PasswordInput         → userPasswordInput
 *            ├── AdminToggle           → isAdminToggle
 *            ├── OUDropdown            → ouDropdown  (lists existing OUs)
 *            ├── OKBtn                 → userOKBtn
 *            └── CancelBtn             → userCancelBtn
 *
 *  INSPECTOR ASSIGNMENTS
 *    All fields as listed above.
 *    ouNodePrefab → prefab with Button + TMP_Text for OU tree nodes
 *
 *  HOW IT WORKS
 *    Right-clicking the domain node → context menu shows "New → Organizational Unit".
 *    Right-clicking an OU node → context menu shows "New → User".
 *    New OU dialog: player types OU name → OK → added to state.OrganizationalUnits.
 *    New User dialog: player fills fields + selects OU + optional Admin toggle →
 *      OK → added to state.UserAccounts.
 *    Tree refreshes after every change.
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ADUCController : MonoBehaviour
{
    public static ADUCController Instance { get; private set; }

    [Header("Nav")]
    [SerializeField] private Button closeBtn;

    [Header("Tree Panel")]
    [SerializeField] private TMP_Text domainNodeLabel;
    [SerializeField] private Transform ouNodeParent;
    [SerializeField] private GameObject ouNodePrefab;
    [SerializeField] private Button domainNodeBtn;

    [Header("Content Panel")]
    [SerializeField] private TMP_Text  contentTitleTMP;
    [SerializeField] private Transform itemListParent;
    [SerializeField] private GameObject itemRowPrefab;

    [Header("Context Menu")]
    [SerializeField] private GameObject contextMenu;
    [SerializeField] private Button     ctxNewOUBtn;
    [SerializeField] private Button     ctxNewUserBtn;

    [Header("New OU Dialog")]
    [SerializeField] private GameObject    newOUDialog;
    [SerializeField] private TMP_InputField ouNameInput;
    [SerializeField] private Button        ouOKBtn;
    [SerializeField] private Button        ouCancelBtn;

    [Header("New User Dialog")]
    [SerializeField] private GameObject    newUserDialog;
    [SerializeField] private TMP_InputField firstNameInput;
    [SerializeField] private TMP_InputField lastNameInput;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField userPasswordInput;
    [SerializeField] private Toggle        isAdminToggle;
    [SerializeField] private TMP_Dropdown  ouDropdown;
    [SerializeField] private Button        userOKBtn;
    [SerializeField] private Button        userCancelBtn;

    private string _selectedOU = ""; // which OU node is currently selected
    private bool   _contextOnDomain = true; // true = right-click was on domain, false = on OU

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        closeBtn?.onClick.AddListener(Close);
        domainNodeBtn?.onClick.AddListener(OnDomainNodeClicked);

        ctxNewOUBtn?.onClick.AddListener(OpenNewOUDialog);
        ctxNewUserBtn?.onClick.AddListener(OpenNewUserDialog);

        ouOKBtn?.onClick.AddListener(ConfirmNewOU);
        ouCancelBtn?.onClick.AddListener(() => newOUDialog?.SetActive(false));

        userOKBtn?.onClick.AddListener(ConfirmNewUser);
        userCancelBtn?.onClick.AddListener(() => newUserDialog?.SetActive(false));

        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.ADUCOpened = true;
        CloseContextMenu();
        RefreshAll();
        ActivityLogManager.Log("Opened Active Directory Users and Computers", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        CloseContextMenu();
        newOUDialog?.SetActive(false);
        newUserDialog?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Domain node ───────────────────────────────────────────────────────────

    private void OnDomainNodeClicked()
    {
        _contextOnDomain = true;
        ctxNewOUBtn?.gameObject.SetActive(true);
        ctxNewUserBtn?.gameObject.SetActive(false);
        contextMenu?.SetActive(true);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        ShowContent("Domain", GetAllItems(state));
    }

    // ── OU node ───────────────────────────────────────────────────────────────

    private void OnOUNodeClicked(string ouName)
    {
        _selectedOU      = ouName;
        _contextOnDomain = false;
        ctxNewOUBtn?.gameObject.SetActive(false);
        ctxNewUserBtn?.gameObject.SetActive(true);
        contextMenu?.SetActive(true);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        ShowContent(ouName, GetUsersInOU(state, ouName));
    }

    private void CloseContextMenu() => contextMenu?.SetActive(false);

    // ── New OU ────────────────────────────────────────────────────────────────

    private void OpenNewOUDialog()
    {
        CloseContextMenu();
        if (ouNameInput != null) ouNameInput.text = "";
        newOUDialog?.SetActive(true);
    }

    private void ConfirmNewOU()
    {
        string name = ouNameInput != null ? ouNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(name)) { Debug.LogWarning("[ADUC] OU name required."); return; }

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;
        if (state.OrganizationalUnits.Exists(o => o.Name == name))
        {
            Debug.LogWarning($"[ADUC] OU '{name}' already exists.");
            return;
        }
        state.OrganizationalUnits.Add(new OUData { Name = name });
        newOUDialog?.SetActive(false);
        RefreshAll();
        ShowContent("Domain", GetAllItems(state));
        ActivityLogManager.Log($"Created Organizational Unit: {name}", ActivityLogManager.EntryType.Action);
    }

    // ── New User ──────────────────────────────────────────────────────────────

    private void OpenNewUserDialog()
    {
        CloseContextMenu();
        if (firstNameInput    != null) firstNameInput.text    = "";
        if (lastNameInput     != null) lastNameInput.text     = "";
        if (usernameInput     != null) usernameInput.text     = "";
        if (userPasswordInput != null) userPasswordInput.text = "";
        if (isAdminToggle     != null) isAdminToggle.isOn     = false;

        RefreshOUDropdown();
        newUserDialog?.SetActive(true);
    }

    private void RefreshOUDropdown()
    {
        if (ouDropdown == null) return;
        ouDropdown.ClearOptions();
        var state = ServerVirtualOSManager.Instance?.ServerState;
        var opts  = new List<string>();
        if (state != null)
            foreach (var ou in state.OrganizationalUnits)
                opts.Add(ou.Name);
        ouDropdown.AddOptions(opts);
        // Pre-select the OU the player right-clicked, if available
        int preselect = opts.IndexOf(_selectedOU);
        if (preselect >= 0) ouDropdown.value = preselect;
    }

    private void ConfirmNewUser()
    {
        string first   = firstNameInput?.text.Trim()    ?? "";
        string last    = lastNameInput?.text.Trim()     ?? "";
        string uname   = usernameInput?.text.Trim()     ?? "";
        string pass    = userPasswordInput?.text        ?? "";
        bool   isAdmin = isAdminToggle != null && isAdminToggle.isOn;

        if (string.IsNullOrEmpty(uname)) { Debug.LogWarning("[ADUC] Username required."); return; }

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;

        string ouName = "";
        if (ouDropdown != null && ouDropdown.options.Count > 0)
            ouName = ouDropdown.options[ouDropdown.value].text;

        state.UserAccounts.Add(new UserData
        {
            FirstName = first,
            LastName  = last,
            Username  = uname,
            Password  = pass,
            OUName    = ouName,
            IsAdmin   = isAdmin
        });

        newUserDialog?.SetActive(false);
        RefreshAll();
        ShowContent(_selectedOU, GetUsersInOU(state, _selectedOU));
        string type = isAdmin ? "Administrator" : "User";
        ActivityLogManager.Log($"Created {type} account: {uname} in OU: {ouName}", ActivityLogManager.EntryType.Action);
    }

    // ── Tree refresh ──────────────────────────────────────────────────────────

    private void RefreshAll()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (domainNodeLabel != null)
            domainNodeLabel.text = !string.IsNullOrEmpty(state?.DomainName)
                ? state.DomainName : "css.com";

        // Clear old OU nodes
        foreach (Transform child in ouNodeParent) Destroy(child.gameObject);

        if (state == null) return;
        foreach (var ou in state.OrganizationalUnits)
        {
            var node = Instantiate(ouNodePrefab, ouNodeParent);
            var label = node.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = ou.Name;

            var btn = node.GetComponentInChildren<Button>();
            string captured = ou.Name;
            btn?.onClick.AddListener(() => OnOUNodeClicked(captured));
        }
    }

    private void ShowContent(string title, List<string> items)
    {
        if (contentTitleTMP != null) contentTitleTMP.text = title;
        foreach (Transform child in itemListParent) Destroy(child.gameObject);
        foreach (string item in items)
        {
            var row = Instantiate(itemRowPrefab, itemListParent);
            var tmp = row.GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.text = item;
        }
    }

    private static List<string> GetAllItems(ServerDeviceState state)
    {
        var list = new List<string>();
        if (state == null) return list;
        foreach (var ou in state.OrganizationalUnits) list.Add($"[OU] {ou.Name}");
        foreach (var u  in state.UserAccounts)        list.Add($"[User] {u.Username}");
        return list;
    }

    private static List<string> GetUsersInOU(ServerDeviceState state, string ouName)
    {
        var list = new List<string>();
        if (state == null) return list;
        foreach (var u in state.UserAccounts)
            if (u.OUName == ouName)
                list.Add($"{u.Username}{(u.IsAdmin ? " [Admin]" : "")}");
        return list;
    }
}
