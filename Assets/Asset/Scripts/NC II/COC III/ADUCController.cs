/*
 * ================================================================
 *  UNITY SETUP GUIDE — ADUCController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "ADUC Panel" (starts INACTIVE).
 *    Opened via ServerManagerController Tools → Active Directory Users and Computers.
 *
 *  HIERARCHY  (match exactly)
 *
 *    ADUC Panel                              ← this script
 *      ├── TitleBar
 *      │     └── CloseBtn (Button)           → closeBtn
 *      ├── MainArea (HorizontalLayoutGroup, child force expand H:ON)
 *      │     ├── TreePanel (fixed width ~220, VerticalLayoutGroup)
 *      │     │     └── TreeScrollView (ScrollView)
 *      │     │           └── Viewport
 *      │     │                 └── TreeContent
 *      │     │                       VLG + ContentSizeFitter Vertical=Preferred
 *      │     │                       anchor top-stretch, pivot (0.5,1)
 *      │     │                       → treeNodeParent
 *      │     └── ContentPanel (flexible, VerticalLayoutGroup)
 *      │           └── ContentScrollView (ScrollView)
 *      │                 └── Viewport
 *      │                       └── ContentListContent
 *      │                             VLG + ContentSizeFitter Vertical=Preferred
 *      │                             anchor top-stretch, pivot (0.5,1)
 *      │                             → contentListParent
 *      ├── ContextMenu (starts INACTIVE)     → contextMenu
 *      │     ├── NewOUBtn (Button)           → ctxNewOUBtn
 *      │     └── NewUserBtn (Button)         → ctxNewUserBtn
 *      ├── New OU Dialog (starts INACTIVE)   → newOUDialog
 *      │     ├── OUNameInput (TMP_InputField) → ouNameInput
 *      │     ├── OKBtn (Button)              → ouOKBtn
 *      │     └── CancelBtn (Button)          → ouCancelBtn
 *      └── New User Dialog (starts INACTIVE) → newUserDialog
 *            [layout identical to prior guide — all wizard fields unchanged]
 *            ├── CreateInLabel (TMP_Text)    → createInLabel
 *            ├── StepPanel0                  → stepPanel0
 *            │     [FirstNameInput, LastNameInput, InitialsInput, FullNameInput,
 *            │      UserLogonNameInput, DomainDropdown, PreWin2000Display]
 *            ├── StepPanel1                  → stepPanel1
 *            │     [PasswordInput, ConfirmPasswordInput, Toggles x5]
 *            ├── StepPanel2                  → stepPanel2
 *            │     [SummaryTMP]
 *            └── Footer
 *                  [BackBtn, NextFinishBtn (+ label), CancelBtn]
 *
 *  PREFABS
 *    treeNodePrefab   — shared
 *    contentRowPrefab — shared
 *
 *  INSPECTOR ASSIGNMENTS
 *    closeBtn, treeNodeParent, treeNodePrefab
 *    contentListParent, contentRowPrefab
 *    contextMenu, ctxNewOUBtn, ctxNewUserBtn
 *    newOUDialog, ouNameInput, ouOKBtn, ouCancelBtn
 *    newUserDialog, createInLabel
 *    stepPanel0, stepPanel1, stepPanel2
 *    firstNameInput, lastNameInput, initialsInput, fullNameInput
 *    userLogonNameInput, domainDropdown, preWin2000Display
 *    passwordInput, confirmPasswordInput
 *    mustChangeToggle, cannotChangeToggle, neverExpiresToggle
 *    accountDisabledToggle, addToDomainAdminsToggle
 *    summaryTMP
 *    backBtn, nextFinishBtn, nextFinishLabel, userCancelBtn
 *    selectedColor → (0.2, 0.5, 0.9, 0.35)
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ADUCController : MonoBehaviour
{
    public static ADUCController Instance { get; private set; }

    // ── Serialized fields ─────────────────────────────────────────────────────

    [Header("Nav")]
    [SerializeField] private Button closeBtn;

    [Header("Tree")]
    [SerializeField] private Transform  treeNodeParent;
    [SerializeField] private GameObject treeNodePrefab;

    [Header("Content")]
    [SerializeField] private Transform  contentListParent;
    [SerializeField] private GameObject contentRowPrefab;

    [Header("Context Menu")]
    [SerializeField] private GameObject contextMenu;
    [SerializeField] private Button     ctxNewOUBtn;
    [SerializeField] private Button     ctxNewUserBtn;

    [Header("New OU Dialog")]
    [SerializeField] private GameObject      newOUDialog;
    [SerializeField] private TMP_InputField  ouNameInput;
    [SerializeField] private Button          ouOKBtn;
    [SerializeField] private Button          ouCancelBtn;

    [Header("New User Dialog — Root")]
    [SerializeField] private GameObject   newUserDialog;
    [SerializeField] private TMP_Text     createInLabel;

    [Header("New User Dialog — Step Panels")]
    [SerializeField] private GameObject   stepPanel0;
    [SerializeField] private GameObject   stepPanel1;
    [SerializeField] private GameObject   stepPanel2;

    [Header("New User Dialog — Step 0")]
    [SerializeField] private TMP_InputField firstNameInput;
    [SerializeField] private TMP_InputField lastNameInput;
    [SerializeField] private TMP_InputField initialsInput;
    [SerializeField] private TMP_InputField fullNameInput;
    [SerializeField] private TMP_InputField userLogonNameInput;
    [SerializeField] private TMP_Dropdown   domainDropdown;
    [SerializeField] private TMP_Text       preWin2000Display;

    [Header("New User Dialog — Step 1")]
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;
    [SerializeField] private Toggle         mustChangeToggle;
    [SerializeField] private Toggle         cannotChangeToggle;
    [SerializeField] private Toggle         neverExpiresToggle;
    [SerializeField] private Toggle         accountDisabledToggle;
    [SerializeField] private Toggle         addToDomainAdminsToggle;

    [Header("New User Dialog — Step 2")]
    [SerializeField] private TMP_Text summaryTMP;

    [Header("New User Dialog — Footer")]
    [SerializeField] private Button   backBtn;
    [SerializeField] private Button   nextFinishBtn;
    [SerializeField] private TMP_Text nextFinishLabel;
    [SerializeField] private Button   userCancelBtn;

    [Header("Colors")]
    [SerializeField] private Color selectedColor = new Color(0.2f, 0.5f, 0.9f, 0.35f);

    // ── Private state ─────────────────────────────────────────────────────────

    private const float IndentWidth = 16f;

    private readonly Dictionary<string, bool> _expanded      = new Dictionary<string, bool>();
    private string                            _selectedNodeId = "";
    private string                            _selectedOU     = "";
    private int                               _currentStep;
    private bool                              _suppressConstraints;

    private static readonly string[] StaticContainerIds    =
        { "Builtin", "Computers", "DomainControllers", "ForeignSecurityPrincipals", "ManagedServiceAccounts", "Users" };

    private static readonly string[] StaticContainerLabels =
        { "Builtin", "Computers", "Domain Controllers", "Foreign Security Principals", "Managed Service Accounts", "Users" };

    private static readonly string[] BuiltinGroups =
    {
        "Administrators", "Backup Operators", "Certificate Service DCOM Access",
        "Cryptographic Operators", "Distributed COM Users", "Event Log Readers",
        "Guests", "IIS_IUSRS", "Network Configuration Operators",
        "Performance Log Users", "Performance Monitor Users", "Power Users",
        "Print Operators", "Remote Desktop Users", "Replicator", "Users"
    };

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        closeBtn?.onClick.AddListener(Close);

        ctxNewOUBtn ?.onClick.AddListener(OpenNewOUDialog);
        ctxNewUserBtn?.onClick.AddListener(OpenNewUserDialog);

        ouOKBtn    ?.onClick.AddListener(ConfirmNewOU);
        ouCancelBtn?.onClick.AddListener(() => newOUDialog?.SetActive(false));

        backBtn      ?.onClick.AddListener(OnBackClick);
        nextFinishBtn?.onClick.AddListener(OnNextFinishClick);
        userCancelBtn?.onClick.AddListener(() => newUserDialog?.SetActive(false));

        firstNameInput     ?.onValueChanged.AddListener(_ => AutoFillFullName());
        lastNameInput      ?.onValueChanged.AddListener(_ => AutoFillFullName());
        userLogonNameInput ?.onValueChanged.AddListener(_ => AutoFillPreWin2000());

        neverExpiresToggle  ?.onValueChanged.AddListener(_ => ApplyToggleConstraints(neverExpiresToggle));
        cannotChangeToggle  ?.onValueChanged.AddListener(_ => ApplyToggleConstraints(cannotChangeToggle));
        mustChangeToggle    ?.onValueChanged.AddListener(_ => ApplyToggleConstraints(mustChangeToggle));

        newUserDialog?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        _expanded.Clear();
        _selectedNodeId = "";
        _selectedOU     = "";

        contextMenu?.SetActive(false);
        newOUDialog?.SetActive(false);
        newUserDialog?.SetActive(false);

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.ADUCOpened = true;

        gameObject.SetActive(true);
        RebuildTree();
        RefreshContentPanel();

        ActivityLogManager.Log("Opened Active Directory Users and Computers", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        contextMenu?.SetActive(false);
        newOUDialog?.SetActive(false);
        newUserDialog?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Tree ──────────────────────────────────────────────────────────────────

    private void RebuildTree()
    {
        foreach (Transform t in treeNodeParent) Destroy(t.gameObject);

        var state      = ServerVirtualOSManager.Instance?.ServerState;
        string domain  = state != null && !string.IsNullOrEmpty(state.DomainName)
                         ? state.DomainName : "domain.local";

        bool domainExp = GetExpanded("Domain");
        SpawnNode("Domain", domain, 0,
            isLeaf: false, isExpanded: domainExp,
            isSelected: _selectedNodeId == "Domain",
            leftClick: true, rightClick: true);

        if (!domainExp) { RebuildTreeLayout(); return; }

        // Static L1 containers (all leaf)
        for (int i = 0; i < StaticContainerIds.Length; i++)
        {
            string sid = StaticContainerIds[i];
            SpawnNode(sid, StaticContainerLabels[i], 1,
                isLeaf: true, isExpanded: false,
                isSelected: _selectedNodeId == sid,
                leftClick: true, rightClick: false);
        }

        // Dynamic OU nodes (L1, expandable)
        if (state != null)
        {
            foreach (var ou in state.OrganizationalUnits)
            {
                string ouId  = "OU:" + ou.Name;
                bool   ouExp = GetExpanded(ouId);
                SpawnNode(ouId, ou.Name, 1,
                    isLeaf: false, isExpanded: ouExp,
                    isSelected: _selectedNodeId == ouId,
                    leftClick: true, rightClick: true);

                if (!ouExp) continue;

                // L2 user nodes (leaf, no click)
                foreach (var u in state.UserAccounts)
                {
                    if (u.OUName != ou.Name) continue;
                    string uid     = "User:" + ou.Name + ":" + u.Username;
                    string display = string.IsNullOrEmpty(u.FullName) ? u.Username : u.FullName;
                    SpawnNode(uid, display, 2,
                        isLeaf: true, isExpanded: false,
                        isSelected: false,
                        leftClick: false, rightClick: false);
                }
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
        if (ui == null) { Debug.LogError("[ADUC] treeNodePrefab missing TreeNodeUI."); return; }

        ui.indentSpacer.preferredWidth = depth * IndentWidth;
        ui.arrowLabel.gameObject.SetActive(!isLeaf);
        if (!isLeaf) ui.arrowLabel.text = isExpanded ? "▼" : "▶";
        ui.nodeLabel.text   = label;
        ui.background.color = isSelected ? selectedColor : Color.clear;

        if (!leftClick && !rightClick) return;

        var handler       = go.AddComponent<NodeClickHandler>();
        string capturedId = id;

        if (leftClick)  handler.onLeftClick  = () => OnNodeLeftClick(capturedId);
        if (rightClick) handler.onRightClick = () => OnNodeRightClick(capturedId);
    }

    // ── Tree interaction ──────────────────────────────────────────────────────

    private void OnNodeLeftClick(string id)
    {
        contextMenu?.SetActive(false);

        if (id == "Domain" || id.StartsWith("OU:"))
            _expanded[id] = !GetExpanded(id);

        _selectedNodeId = id;

        if (id.StartsWith("OU:"))
            _selectedOU = id.Substring(3);

        RebuildTree();
        RefreshContentPanel();
    }

    private void OnNodeRightClick(string id)
    {
        if (id == "Domain")
        {
            ctxNewOUBtn ?.gameObject.SetActive(true);
            ctxNewUserBtn?.gameObject.SetActive(false);
            contextMenu?.SetActive(true);
        }
        else if (id.StartsWith("OU:"))
        {
            _selectedOU      = id.Substring(3);
            _selectedNodeId  = id;
            ctxNewOUBtn ?.gameObject.SetActive(false);
            ctxNewUserBtn?.gameObject.SetActive(true);
            contextMenu?.SetActive(true);
            RebuildTree();
            RefreshContentPanel();
        }
    }

    // ── Content panel ─────────────────────────────────────────────────────────

    private void RefreshContentPanel()
    {
        foreach (Transform t in contentListParent) Destroy(t.gameObject);

        var state = ServerVirtualOSManager.Instance?.ServerState;

        if (_selectedNodeId == "Domain")
        {
            for (int i = 0; i < StaticContainerLabels.Length; i++)
                SpawnContentRow(StaticContainerLabels[i]);
            if (state != null)
                foreach (var ou in state.OrganizationalUnits)
                    SpawnContentRow(ou.Name);
        }
        else if (_selectedNodeId == "Builtin")
        {
            foreach (string g in BuiltinGroups)
                SpawnContentRow(g);
        }
        else if (_selectedNodeId == "Computers")
        {
            var client = ServerVirtualOSManager.Instance?.ClientState;
            if (client != null && client.DomainJoined)
                SpawnContentRow(client.ComputerName);
        }
        else if (_selectedNodeId == "DomainControllers")
        {
            string dc = state != null && !string.IsNullOrEmpty(state.ComputerName)
                        ? state.ComputerName : "SERVER";
            SpawnContentRow(dc);
        }
        else if (_selectedNodeId == "Users")
        {
            SpawnContentRow("Administrator");
            SpawnContentRow("Guest");
        }
        else if (_selectedNodeId.StartsWith("OU:"))
        {
            string ouName = _selectedNodeId.Substring(3);
            if (state != null)
            {
                foreach (var u in state.UserAccounts)
                {
                    if (u.OUName != ouName) continue;
                    string display = string.IsNullOrEmpty(u.FullName) ? u.Username : u.FullName;
                    SpawnContentRow(display, u.Username);
                }
            }
        }
        // ForeignSecurityPrincipals, ManagedServiceAccounts, User nodes: empty

        RebuildContentLayout();
    }

    private void SpawnContentRow(string col1, string col2 = "",
                                  bool showBtn = false, string btnLabel = "",
                                  System.Action onBtnClick = null)
    {
        var go = Instantiate(contentRowPrefab, contentListParent);
        var ui = go.GetComponent<ContentRowUI>();
        if (ui == null) return;

        ui.col1TMP.text = col1;

        bool hasCol2 = !string.IsNullOrEmpty(col2);
        ui.col2TMP?.gameObject.SetActive(hasCol2);
        if (hasCol2 && ui.col2TMP != null) ui.col2TMP.text = col2;

        ui.actionBtn?.gameObject.SetActive(showBtn);
        if (showBtn)
        {
            if (ui.actionBtnLabel != null) ui.actionBtnLabel.text = btnLabel;
            if (onBtnClick != null)        ui.actionBtn?.onClick.AddListener(() => onBtnClick());
        }
    }

    // ── New OU ────────────────────────────────────────────────────────────────

    private void OpenNewOUDialog()
    {
        contextMenu?.SetActive(false);
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

        _expanded["Domain"] = true;
        _selectedNodeId     = "Domain";
        RebuildTree();
        RefreshContentPanel();

        ActivityLogManager.Log($"Created Organizational Unit: {name}", ActivityLogManager.EntryType.Action);
    }

    // ── New User — open & step navigation ─────────────────────────────────────

    private void OpenNewUserDialog()
    {
        contextMenu?.SetActive(false);
        _currentStep = 0;

        var state   = ServerVirtualOSManager.Instance?.ServerState;
        string domain  = !string.IsNullOrEmpty(state?.DomainName) ? state.DomainName : "css.com";
        string netbios = GetNetBIOSName(state);

        if (createInLabel != null)
            createInLabel.text = $"Create in:  {domain}/{_selectedOU}";

        if (domainDropdown != null)
        {
            domainDropdown.ClearOptions();
            domainDropdown.AddOptions(new List<string> { $"@{domain}" });
            domainDropdown.value        = 0;
            domainDropdown.interactable = false;
        }

        if (firstNameInput      != null) firstNameInput.text      = "";
        if (lastNameInput       != null) lastNameInput.text       = "";
        if (initialsInput       != null) initialsInput.text       = "";
        if (fullNameInput       != null) fullNameInput.text       = "";
        if (userLogonNameInput  != null) userLogonNameInput.text  = "";
        if (preWin2000Display   != null) preWin2000Display.text   = $"{netbios}\\";
        if (passwordInput       != null) passwordInput.text       = "";
        if (confirmPasswordInput!= null) confirmPasswordInput.text= "";

        _suppressConstraints = true;
        if (neverExpiresToggle      != null) { neverExpiresToggle.isOn      = true;  neverExpiresToggle.interactable      = true;  }
        if (mustChangeToggle        != null) { mustChangeToggle.isOn        = false; mustChangeToggle.interactable        = false; }
        if (cannotChangeToggle      != null) { cannotChangeToggle.isOn      = false; cannotChangeToggle.interactable      = true;  }
        if (accountDisabledToggle   != null) { accountDisabledToggle.isOn   = false; accountDisabledToggle.interactable   = true;  }
        if (addToDomainAdminsToggle != null) { addToDomainAdminsToggle.isOn = false; addToDomainAdminsToggle.interactable = true;  }
        _suppressConstraints = false;

        RefreshStep();
        newUserDialog?.SetActive(true);
    }

    private void OnBackClick()
    {
        if (_currentStep > 0) _currentStep--;
        RefreshStep();
    }

    private void OnNextFinishClick()
    {
        if (_currentStep == 0)
        {
            if (!ValidateStep0()) return;
            _currentStep = 1;
            RefreshStep();
        }
        else if (_currentStep == 1)
        {
            if (!ValidateStep1()) return;
            BuildSummary();
            _currentStep = 2;
            RefreshStep();
        }
        else
        {
            ConfirmNewUser();
        }
    }

    private void RefreshStep()
    {
        stepPanel0?.SetActive(_currentStep == 0);
        stepPanel1?.SetActive(_currentStep == 1);
        stepPanel2?.SetActive(_currentStep == 2);

        if (backBtn         != null) backBtn.interactable         = _currentStep > 0;
        if (nextFinishLabel != null) nextFinishLabel.text         = _currentStep == 2 ? "Finish" : "Next";
    }

    // ── New User — validation ─────────────────────────────────────────────────

    private bool ValidateStep0()
    {
        string logon = userLogonNameInput?.text.Trim() ?? "";
        if (string.IsNullOrEmpty(logon)) { Debug.LogWarning("[ADUC] User logon name is required."); return false; }

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null && state.UserAccounts.Exists(u =>
            string.Equals(u.Username, logon, System.StringComparison.OrdinalIgnoreCase)))
        {
            Debug.LogWarning($"[ADUC] Logon name '{logon}' already exists.");
            return false;
        }
        return true;
    }

    private bool ValidateStep1()
    {
        string pass    = passwordInput?.text        ?? "";
        string confirm = confirmPasswordInput?.text ?? "";
        if (string.IsNullOrEmpty(pass))  { Debug.LogWarning("[ADUC] Password is required."); return false; }
        if (pass != confirm)             { Debug.LogWarning("[ADUC] Passwords do not match."); return false; }
        return true;
    }

    // ── New User — summary & confirm ──────────────────────────────────────────

    private void BuildSummary()
    {
        if (summaryTMP == null) return;

        var state      = ServerVirtualOSManager.Instance?.ServerState;
        string domain  = !string.IsNullOrEmpty(state?.DomainName) ? state.DomainName : "css.com";
        string netbios = GetNetBIOSName(state);
        string logon   = userLogonNameInput?.text.Trim() ?? "";
        string full    = fullNameInput?.text.Trim()      ?? "";

        bool mustChange   = mustChangeToggle        != null && mustChangeToggle.isOn;
        bool cannotChange = cannotChangeToggle      != null && cannotChangeToggle.isOn;
        bool neverExpires = neverExpiresToggle      != null && neverExpiresToggle.isOn;
        bool disabled     = accountDisabledToggle   != null && accountDisabledToggle.isOn;
        bool domainAdmins = addToDomainAdminsToggle != null && addToDomainAdminsToggle.isOn;

        string C(bool v) => v ? "[x]" : "[ ]";

        summaryTMP.text =
            $"Full name:                       {full}\n" +
            $"User logon name:                 {logon}@{domain}\n" +
            $"User logon name (pre-Win 2000):  {netbios}\\{logon}\n\n" +
            $"{C(mustChange)}  User must change password at next logon\n" +
            $"{C(cannotChange)}  User cannot change password\n" +
            $"{C(neverExpires)}  Password never expires\n" +
            $"{C(disabled)}  Account is disabled\n" +
            $"{C(domainAdmins)}  Add to Domain Admins group";
    }

    private void ConfirmNewUser()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;

        string first    = firstNameInput?.text.Trim()     ?? "";
        string last     = lastNameInput?.text.Trim()      ?? "";
        string initials = initialsInput?.text.Trim()      ?? "";
        string full     = fullNameInput?.text.Trim()      ?? "";
        string logon    = userLogonNameInput?.text.Trim() ?? "";
        string pass     = passwordInput?.text             ?? "";

        bool mustChange   = mustChangeToggle        != null && mustChangeToggle.isOn;
        bool cannotChange = cannotChangeToggle      != null && cannotChangeToggle.isOn;
        bool neverExpires = neverExpiresToggle      != null && neverExpiresToggle.isOn;
        bool disabled     = accountDisabledToggle   != null && accountDisabledToggle.isOn;
        bool isAdmin      = addToDomainAdminsToggle != null && addToDomainAdminsToggle.isOn;

        state.UserAccounts.Add(new UserData
        {
            FirstName            = first,
            LastName             = last,
            Initials             = initials,
            FullName             = full,
            Username             = logon,
            Password             = pass,
            OUName               = _selectedOU,
            IsAdmin              = isAdmin,
            MustChangePassword   = mustChange,
            CannotChangePassword = cannotChange,
            PasswordNeverExpires = neverExpires,
            AccountDisabled      = disabled,
        });

        newUserDialog?.SetActive(false);

        // Select the OU and expand it to show the new user
        string ouId      = "OU:" + _selectedOU;
        _selectedNodeId  = ouId;
        _expanded[ouId]  = true;
        RebuildTree();
        RefreshContentPanel();

        string type = isAdmin ? "Administrator" : "User";
        ActivityLogManager.Log(
            $"Created {type} account: {logon} in OU: {_selectedOU}",
            ActivityLogManager.EntryType.Action);
    }

    // ── Auto-fill helpers ─────────────────────────────────────────────────────

    private void AutoFillFullName()
    {
        string first = firstNameInput?.text.Trim() ?? "";
        string last  = lastNameInput?.text.Trim()  ?? "";
        if (fullNameInput != null) fullNameInput.text = (first + " " + last).Trim();
    }

    private void AutoFillPreWin2000()
    {
        var state      = ServerVirtualOSManager.Instance?.ServerState;
        string netbios = GetNetBIOSName(state);
        string logon   = userLogonNameInput?.text.Trim() ?? "";
        if (preWin2000Display != null) preWin2000Display.text = $"{netbios}\\{logon}";
    }

    private static string GetNetBIOSName(ServerDeviceState state)
    {
        if (state != null && !string.IsNullOrEmpty(state.NetBIOSName))
            return state.NetBIOSName.ToUpper();

        if (state != null && !string.IsNullOrEmpty(state.DomainName))
        {
            int dot    = state.DomainName.IndexOf('.');
            string pre = dot > 0 ? state.DomainName.Substring(0, dot) : state.DomainName;
            return pre.ToUpper();
        }
        return "DOMAIN";
    }

    // ── Toggle mutual exclusion ───────────────────────────────────────────────

    private void ApplyToggleConstraints(Toggle changed)
    {
        if (_suppressConstraints) return;
        _suppressConstraints = true;

        if (changed == neverExpiresToggle)
        {
            if (neverExpiresToggle.isOn)
            { if (mustChangeToggle != null) { mustChangeToggle.isOn = false; mustChangeToggle.interactable = false; } }
            else
            { bool blocked = cannotChangeToggle != null && cannotChangeToggle.isOn;
              if (mustChangeToggle != null) mustChangeToggle.interactable = !blocked; }
        }
        else if (changed == cannotChangeToggle)
        {
            if (cannotChangeToggle.isOn)
            { if (mustChangeToggle != null) { mustChangeToggle.isOn = false; mustChangeToggle.interactable = false; } }
            else
            { bool blocked = neverExpiresToggle != null && neverExpiresToggle.isOn;
              if (mustChangeToggle != null) mustChangeToggle.interactable = !blocked; }
        }
        else if (changed == mustChangeToggle)
        {
            if (mustChangeToggle.isOn)
            {
                if (neverExpiresToggle != null) { neverExpiresToggle.isOn = false; neverExpiresToggle.interactable = false; }
                if (cannotChangeToggle != null) { cannotChangeToggle.isOn = false; cannotChangeToggle.interactable = false; }
            }
            else
            {
                if (neverExpiresToggle != null) neverExpiresToggle.interactable = true;
                if (cannotChangeToggle != null) cannotChangeToggle.interactable = true;
            }
        }

        _suppressConstraints = false;
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
}
