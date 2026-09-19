/*
 * ================================================================
 *  UNITY SETUP GUIDE — ADUCController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "ADUC Panel" GameObject (starts INACTIVE).
 *    Opened from ServerManagerController Tools → Active Directory Users and Computers.
 *
 *  HIERARCHY
 *    ADUC Panel                              ← this script here
 *      ├── TitleBar / CloseBtn              → closeBtn
 *      ├── TreePanel (left)
 *      │     ├── DomainNode (TMP)           → domainNodeLabel
 *      │     │     └── NodeClickHandler     ← pre-place this component on DomainNode
 *      │     └── DomainChildren (Inactive)  → domainChildrenGroup
 *      │           ├── Builtin [TMP]         (static, no interaction)
 *      │           ├── Computers [TMP]
 *      │           ├── Domain Controllers [TMP]
 *      │           ├── ForeignSecurityPrincipals [TMP]
 *      │           ├── Managed Service Accounts [TMP]
 *      │           ├── Users [TMP]
 *      │           └── OUNodeParent          → ouNodeParent (OU prefabs spawn here)
 *      ├── ContentPanel (right)
 *      │     ├── ContentTitle               → contentTitleTMP
 *      │     ├── StaticFolders (Inactive)   → staticFoldersGroup
 *      │     │     ├── Builtin [TMP]
 *      │     │     ├── Computers [TMP]
 *      │     │     ├── Domain Controllers [TMP]
 *      │     │     ├── ForeignSecurityPrincipals [TMP]
 *      │     │     ├── Managed Service Accounts [TMP]
 *      │     │     └── Users [TMP]
 *      │     └── DynamicItems               → dynamicItemsParent (rows spawn here)
 *      ├── Context Menu (Inactive)          → contextMenu
 *      │     ├── NewOUBtn                   → ctxNewOUBtn
 *      │     └── NewUserBtn                 → ctxNewUserBtn
 *      ├── New OU Dialog (Inactive)         → newOUDialog
 *      │     ├── OUNameInput               → ouNameInput
 *      │     ├── OKBtn                     → ouOKBtn
 *      │     └── CancelBtn                 → ouCancelBtn
 *      └── New User Dialog (Inactive)       → newUserDialog
 *            RectTransform: anchor center-center, pivot 0.5/0.5, width:460, height:500
 *            Image: color (0.13,0.13,0.13,1)
 *            VerticalLayoutGroup: padding T:8 B:8 L:10 R:10, spacing:4,
 *              childControlHeight:OFF, childForceExpandHeight:OFF
 *            ├── CreateInLabel              → createInLabel
 *            │     TMP font-size:11, italic, color:(0.6,0.6,0.6,1), align:left-middle
 *            │     LayoutElement preferredHeight:20
 *            ├── StepPanel0 (starts ACTIVE) → stepPanel0
 *            │     VerticalLayoutGroup spacing:4 childControlHeight:OFF childForceExpandHeight:OFF
 *            │     LayoutElement flexibleHeight:1
 *            │     Each form row: HorizontalLayoutGroup, LayoutElement preferredHeight:26
 *            │     ├── FormRow_FirstName
 *            │     │     ├── Lbl_FirstName (TMP "First name:" LE preferredWidth:140 right-middle)
 *            │     │     └── FirstNameInput (TMP_InputField LE flexibleWidth:1) → firstNameInput
 *            │     ├── FormRow_LastName
 *            │     │     ├── Lbl_LastName (TMP "Last name:")
 *            │     │     └── LastNameInput (TMP_InputField)                    → lastNameInput
 *            │     ├── FormRow_Initials (LE preferredHeight:26)
 *            │     │     ├── Lbl_Initials (TMP "Initials:" LE preferredWidth:140)
 *            │     │     └── InitialsInput (TMP_InputField LE preferredWidth:50 characterLimit:3)
 *            │     │                                                           → initialsInput
 *            │     ├── FormRow_FullName
 *            │     │     ├── Lbl_FullName (TMP "Full name:")
 *            │     │     └── FullNameInput (TMP_InputField LE flexibleWidth:1)  → fullNameInput
 *            │     ├── Divider (Image height:1 color:(0.35,0.35,0.35,1) LE preferredHeight:1)
 *            │     ├── FormRow_LogonName (HLG spacing:4)
 *            │     │     ├── Lbl_LogonName (TMP "User logon name:" LE preferredWidth:140)
 *            │     │     ├── UserLogonNameInput (TMP_InputField LE flexibleWidth:1)
 *            │     │     │                                                     → userLogonNameInput
 *            │     │     └── DomainDropdown (TMP_Dropdown LE preferredWidth:150 interactable:OFF)
 *            │     │                                                           → domainDropdown
 *            │     └── FormRow_PreWin2000 (LE preferredHeight:36)
 *            │           ├── Lbl_PreWin2000 (TMP "User logon name\n(pre-Windows 2000):"
 *            │           │     LE preferredWidth:140, font-size:11, right-middle)
 *            │           └── PreWin2000Display (TMP font-size:12 color:(0.75,0.75,0.75,1)
 *            │                 LE flexibleWidth:1)                             → preWin2000Display
 *            ├── StepPanel1 (starts INACTIVE) → stepPanel1
 *            │     VerticalLayoutGroup spacing:4, LayoutElement flexibleHeight:1
 *            │     ├── FormRow_Password (HLG LE preferredHeight:26)
 *            │     │     ├── Lbl_Password (TMP "Password:" LE preferredWidth:140)
 *            │     │     └── PasswordInput (TMP_InputField contentType:Password LE flexibleWidth:1)
 *            │     │                                                           → passwordInput
 *            │     ├── FormRow_ConfirmPassword
 *            │     │     ├── Lbl_ConfirmPassword (TMP "Confirm password:")
 *            │     │     └── ConfirmPasswordInput (TMP_InputField contentType:Password)
 *            │     │                                                           → confirmPasswordInput
 *            │     ├── Divider (Image height:1 color:(0.35,0.35,0.35,1))
 *            │     ├── ToggleRow_MustChange (HLG LE preferredHeight:24 spacing:6)
 *            │     │     ├── MustChangeToggle (Toggle LE preferredWidth:20
 *            │     │     │     isOn:OFF interactable:OFF)                      → mustChangeToggle
 *            │     │     └── Lbl_MustChange (TMP "User must change password at next logon" size:11)
 *            │     ├── ToggleRow_CannotChange
 *            │     │     ├── CannotChangeToggle (Toggle isOn:OFF interactable:ON) → cannotChangeToggle
 *            │     │     └── Lbl_CannotChange (TMP "User cannot change password")
 *            │     ├── ToggleRow_NeverExpires
 *            │     │     ├── NeverExpiresToggle (Toggle isOn:ON interactable:ON)  → neverExpiresToggle
 *            │     │     └── Lbl_NeverExpires (TMP "Password never expires")
 *            │     ├── ToggleRow_Disabled
 *            │     │     ├── AccountDisabledToggle (Toggle isOn:OFF interactable:ON)
 *            │     │     │                                                     → accountDisabledToggle
 *            │     │     └── Lbl_Disabled (TMP "Account is disabled")
 *            │     └── ToggleRow_DomainAdmins
 *            │           ├── AddToDomainAdminsToggle (Toggle isOn:OFF interactable:ON)
 *            │           │                                                     → addToDomainAdminsToggle
 *            │           └── Lbl_DomainAdmins (TMP "Add to Domain Admins group")
 *            ├── StepPanel2 (starts INACTIVE) → stepPanel2
 *            │     LayoutElement flexibleHeight:1
 *            │     └── SummaryTMP (TMP wrapping:ON overflow:Overflow font-size:11) → summaryTMP
 *            └── Footer (always visible — NOT inside any step panel)
 *                  HorizontalLayoutGroup padding:L4 R4 spacing:6 childForceExpandWidth:OFF
 *                  LayoutElement preferredHeight:36
 *                  ├── BackBtn (Button LE preferredWidth:80 preferredHeight:28) → backBtn
 *                  │     └── Label (TMP "Back")
 *                  ├── Spacer (LayoutElement flexibleWidth:1)
 *                  ├── NextFinishBtn (Button LE preferredWidth:80 preferredHeight:28) → nextFinishBtn
 *                  │     └── Lbl_NextFinish (TMP "Next")                       → nextFinishLabel
 *                  └── CancelBtn (Button LE preferredWidth:80 preferredHeight:28) → userCancelBtn
 *                        └── Label (TMP "Cancel")
 *
 *  INTERACTION MODEL
 *    DomainNode left-click  → expand/collapse DomainChildren + update ContentPanel
 *    DomainNode right-click → context menu (New OU)
 *    OUNode left-click      → select OU, show its users in ContentPanel
 *    OUNode right-click     → select OU + context menu (New User)
 *
 *  NEW USER WIZARD RULES
 *    Step 0 → Next: user logon name must not be empty and must be unique
 *    Step 1 → Next: password must not be empty and match confirm password
 *    Step 2 → Finish: saves all data, closes dialog
 *    Cancel: closes dialog on any step without saving
 *    Back:   disabled on step 0; re-enabled on steps 1 and 2
 *
 *  IMPORTANT SCENE RULES
 *    - DomainChildren starts INACTIVE in scene
 *    - StaticFolders starts INACTIVE in scene
 *    - DynamicItems is empty at start
 *    - StepPanel0, StepPanel1, StepPanel2 all start INACTIVE
 *      (script activates the correct one via RefreshStep on open)
 *    - NodeClickHandler must be pre-placed on DomainNode
 *    - OUNode prefab Button children get NodeClickHandler added at runtime
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ADUCController : MonoBehaviour
{
    public static ADUCController Instance { get; private set; }

    private enum Selection { None, Domain, OU }

    // ── Serialized fields ─────────────────────────────────────────────────────

    [Header("Nav")]
    [SerializeField] private Button closeBtn;

    [Header("Tree Panel")]
    [SerializeField] private TMP_Text     domainNodeLabel;
    [SerializeField] private GameObject   domainChildrenGroup;
    [SerializeField] private Transform    ouNodeParent;
    [SerializeField] private GameObject   ouNodePrefab;
    [SerializeField] private RectTransform treePanelRect;

    [Header("Content Panel")]
    [SerializeField] private TMP_Text     contentTitleTMP;
    [SerializeField] private GameObject   staticFoldersGroup;
    [SerializeField] private Transform    dynamicItemsParent;
    [SerializeField] private GameObject   itemRowPrefab;
    [SerializeField] private RectTransform contentPanelRect;

    [Header("Context Menu")]
    [SerializeField] private GameObject   contextMenu;
    [SerializeField] private Button       ctxNewOUBtn;
    [SerializeField] private Button       ctxNewUserBtn;

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

    // ── Private state ─────────────────────────────────────────────────────────

    private bool      _domainExpanded;
    private Selection _currentSelection = Selection.None;
    private string    _selectedOU       = "";
    private int       _currentStep;
    private bool      _suppressConstraints;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        closeBtn?.onClick.AddListener(Close);

        var domainHandler = domainNodeLabel?.GetComponent<NodeClickHandler>();
        if (domainHandler != null)
        {
            domainHandler.onLeftClick  = OnDomainLeftClick;
            domainHandler.onRightClick = OnDomainRightClick;
        }

        ctxNewOUBtn?.onClick.AddListener(OpenNewOUDialog);
        ctxNewUserBtn?.onClick.AddListener(OpenNewUserDialog);

        ouOKBtn?.onClick.AddListener(ConfirmNewOU);
        ouCancelBtn?.onClick.AddListener(() => newOUDialog?.SetActive(false));

        // Footer buttons
        backBtn      ?.onClick.AddListener(OnBackClick);
        nextFinishBtn?.onClick.AddListener(OnNextFinishClick);
        userCancelBtn?.onClick.AddListener(() => newUserDialog?.SetActive(false));

        // Auto-fill listeners
        firstNameInput     ?.onValueChanged.AddListener(_ => AutoFillFullName());
        lastNameInput      ?.onValueChanged.AddListener(_ => AutoFillFullName());
        userLogonNameInput ?.onValueChanged.AddListener(_ => AutoFillPreWin2000());

        // Toggle mutual-exclusion listeners
        neverExpiresToggle      ?.onValueChanged.AddListener(_ => ApplyToggleConstraints(neverExpiresToggle));
        cannotChangeToggle      ?.onValueChanged.AddListener(_ => ApplyToggleConstraints(cannotChangeToggle));
        mustChangeToggle        ?.onValueChanged.AddListener(_ => ApplyToggleConstraints(mustChangeToggle));

        newUserDialog?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.ADUCOpened = true;

        _domainExpanded   = false;
        _currentSelection = Selection.None;
        _selectedOU       = "";

        domainChildrenGroup?.SetActive(false);
        staticFoldersGroup?.SetActive(false);
        ClearOUNodesFromStatic();
        ClearDynamicItems();
        CloseContextMenu();
        newOUDialog?.SetActive(false);
        newUserDialog?.SetActive(false);
        if (contentTitleTMP != null) contentTitleTMP.text = "";

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

    private void OnDomainLeftClick()
    {
        CloseContextMenu();
        _domainExpanded = !_domainExpanded;
        domainChildrenGroup?.SetActive(_domainExpanded);
        RebuildLayout(treePanelRect);

        if (_domainExpanded)
        {
            _currentSelection = Selection.Domain;
            ShowDomainContent();
        }
        else
        {
            _currentSelection = Selection.None;
            _selectedOU       = "";
            ClearOUNodesFromStatic();
            staticFoldersGroup?.SetActive(false);
            ClearDynamicItems();
            if (contentTitleTMP != null) contentTitleTMP.text = "";
            RebuildLayout(contentPanelRect);
        }
    }

    private void OnDomainRightClick()
    {
        ctxNewOUBtn?.gameObject.SetActive(true);
        ctxNewUserBtn?.gameObject.SetActive(false);
        contextMenu?.SetActive(true);
    }

    // ── OU node ───────────────────────────────────────────────────────────────

    private void SelectOU(string ouName)
    {
        CloseContextMenu();
        _selectedOU       = ouName;
        _currentSelection = Selection.OU;
        ShowOUContent(ouName);
    }

    private void OnOUNodeRightClick(string ouName)
    {
        SelectOU(ouName);
        ctxNewOUBtn?.gameObject.SetActive(false);
        ctxNewUserBtn?.gameObject.SetActive(true);
        contextMenu?.SetActive(true);
    }

    private void CloseContextMenu() => contextMenu?.SetActive(false);

    // ── Content panel ─────────────────────────────────────────────────────────

    private void ShowDomainContent()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        string title = !string.IsNullOrEmpty(state?.DomainName) ? state.DomainName : "css.com";
        if (contentTitleTMP != null) contentTitleTMP.text = title;

        ClearOUNodesFromStatic();
        ClearDynamicItems();
        staticFoldersGroup?.SetActive(true);

        if (state == null) { RebuildLayout(contentPanelRect); return; }
        foreach (var ou in state.OrganizationalUnits)
            SpawnOUNode(ou.Name, staticFoldersGroup.transform);
        RebuildLayout(contentPanelRect);
    }

    private void ShowOUContent(string ouName)
    {
        if (contentTitleTMP != null) contentTitleTMP.text = ouName;
        staticFoldersGroup?.SetActive(false);
        ClearDynamicItems();

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) { RebuildLayout(contentPanelRect); return; }

        foreach (var u in state.UserAccounts)
        {
            if (u.OUName != ouName) continue;
            var row = Instantiate(itemRowPrefab, dynamicItemsParent);
            var tmp = row.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
                tmp.text = $"{u.Username} ({(u.IsAdmin ? "Administrator" : "User")})";
        }
        RebuildLayout(contentPanelRect);
    }

    private void ClearDynamicItems()
    {
        if (dynamicItemsParent == null) return;
        foreach (Transform child in dynamicItemsParent) Destroy(child.gameObject);
    }

    private void ClearOUNodesFromStatic()
    {
        if (staticFoldersGroup == null) return;
        var toDestroy = new List<GameObject>();
        foreach (Transform child in staticFoldersGroup.transform)
            if (child.GetComponentInChildren<Button>() != null)
                toDestroy.Add(child.gameObject);
        foreach (var go in toDestroy) Destroy(go);
    }

    private void RebuildLayout(RectTransform rect)
    {
        if (rect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

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
        if (_currentSelection == Selection.Domain) ShowDomainContent();

        ActivityLogManager.Log($"Created Organizational Unit: {name}", ActivityLogManager.EntryType.Action);
    }

    // ── New User — open & step navigation ─────────────────────────────────────

    private void OpenNewUserDialog()
    {
        CloseContextMenu();

        _currentStep = 0;

        var state   = ServerVirtualOSManager.Instance?.ServerState;
        string domain  = !string.IsNullOrEmpty(state?.DomainName) ? state.DomainName : "css.com";
        string netbios = GetNetBIOSName(state);

        if (createInLabel != null)
            createInLabel.text = $"Create in:  {domain}/{_selectedOU}";

        // Domain dropdown — single option, always non-interactable
        if (domainDropdown != null)
        {
            domainDropdown.ClearOptions();
            domainDropdown.AddOptions(new List<string> { $"@{domain}" });
            domainDropdown.value        = 0;
            domainDropdown.interactable = false;
        }

        // Clear step 0 inputs
        if (firstNameInput     != null) firstNameInput.text     = "";
        if (lastNameInput      != null) lastNameInput.text      = "";
        if (initialsInput      != null) initialsInput.text      = "";
        if (fullNameInput      != null) fullNameInput.text      = "";
        if (userLogonNameInput != null) userLogonNameInput.text = "";
        if (preWin2000Display  != null) preWin2000Display.text  = $"{netbios}\\";

        // Clear step 1 inputs
        if (passwordInput        != null) passwordInput.text        = "";
        if (confirmPasswordInput != null) confirmPasswordInput.text = "";

        // Reset toggles to their default states
        // NeverExpires ON by default → MustChange starts disabled
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

        if (backBtn        != null) backBtn.interactable        = _currentStep > 0;
        if (nextFinishLabel != null) nextFinishLabel.text       = _currentStep == 2 ? "Finish" : "Next";
    }

    // ── New User — validation ─────────────────────────────────────────────────

    private bool ValidateStep0()
    {
        string logon = userLogonNameInput?.text.Trim() ?? "";
        if (string.IsNullOrEmpty(logon))
        {
            Debug.LogWarning("[ADUC] User logon name is required.");
            return false;
        }

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

        if (string.IsNullOrEmpty(pass))
        {
            Debug.LogWarning("[ADUC] Password is required.");
            return false;
        }

        if (pass != confirm)
        {
            Debug.LogWarning("[ADUC] Passwords do not match.");
            return false;
        }

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
        _currentSelection = Selection.OU;
        ShowOUContent(_selectedOU);

        string type = isAdmin ? "Administrator" : "User";
        ActivityLogManager.Log(
            $"Created {type} account: {logon} in OU: {_selectedOU}",
            ActivityLogManager.EntryType.Action);
    }

    // ── New User — auto-fill helpers ──────────────────────────────────────────

    private void AutoFillFullName()
    {
        string first = firstNameInput?.text.Trim() ?? "";
        string last  = lastNameInput?.text.Trim()  ?? "";
        if (fullNameInput != null)
            fullNameInput.text = (first + " " + last).Trim();
    }

    private void AutoFillPreWin2000()
    {
        var state      = ServerVirtualOSManager.Instance?.ServerState;
        string netbios = GetNetBIOSName(state);
        string logon   = userLogonNameInput?.text.Trim() ?? "";
        if (preWin2000Display != null)
            preWin2000Display.text = $"{netbios}\\{logon}";
    }

    private static string GetNetBIOSName(ServerDeviceState state)
    {
        if (state != null && !string.IsNullOrEmpty(state.NetBIOSName))
            return state.NetBIOSName.ToUpper();

        if (state != null && !string.IsNullOrEmpty(state.DomainName))
        {
            int dot = state.DomainName.IndexOf('.');
            string prefix = dot > 0 ? state.DomainName.Substring(0, dot) : state.DomainName;
            return prefix.ToUpper();
        }

        return "DOMAIN";
    }

    // ── Toggle mutual exclusion ───────────────────────────────────────────────
    //
    //  Rules (matching real Windows ADUC behaviour):
    //    NeverExpires ON   → MustChange disabled (unchecked)
    //    CannotChange ON   → MustChange disabled (unchecked)
    //    MustChange ON     → NeverExpires disabled (unchecked) AND CannotChange disabled (unchecked)
    //    MustChange OFF    → re-enable NeverExpires and CannotChange
    //    NeverExpires OFF  → re-enable MustChange only if CannotChange is also OFF
    //    CannotChange OFF  → re-enable MustChange only if NeverExpires is also OFF

    private void ApplyToggleConstraints(Toggle changed)
    {
        if (_suppressConstraints) return;
        _suppressConstraints = true;

        if (changed == neverExpiresToggle)
        {
            if (neverExpiresToggle.isOn)
            {
                if (mustChangeToggle != null) { mustChangeToggle.isOn = false; mustChangeToggle.interactable = false; }
            }
            else
            {
                bool blockedByCannotChange = cannotChangeToggle != null && cannotChangeToggle.isOn;
                if (mustChangeToggle != null) mustChangeToggle.interactable = !blockedByCannotChange;
            }
        }
        else if (changed == cannotChangeToggle)
        {
            if (cannotChangeToggle.isOn)
            {
                if (mustChangeToggle != null) { mustChangeToggle.isOn = false; mustChangeToggle.interactable = false; }
            }
            else
            {
                bool blockedByNeverExpires = neverExpiresToggle != null && neverExpiresToggle.isOn;
                if (mustChangeToggle != null) mustChangeToggle.interactable = !blockedByNeverExpires;
            }
        }
        else if (changed == mustChangeToggle)
        {
            if (mustChangeToggle.isOn)
            {
                if (neverExpiresToggle  != null) { neverExpiresToggle.isOn  = false; neverExpiresToggle.interactable  = false; }
                if (cannotChangeToggle  != null) { cannotChangeToggle.isOn  = false; cannotChangeToggle.interactable  = false; }
            }
            else
            {
                if (neverExpiresToggle != null) neverExpiresToggle.interactable = true;
                if (cannotChangeToggle != null) cannotChangeToggle.interactable = true;
            }
        }

        _suppressConstraints = false;
    }

    // ── Tree refresh ──────────────────────────────────────────────────────────

    private void RefreshAll()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (domainNodeLabel != null)
            domainNodeLabel.text = !string.IsNullOrEmpty(state?.DomainName)
                ? state.DomainName : "css.com";

        foreach (Transform child in ouNodeParent) Destroy(child.gameObject);

        if (state == null) return;
        foreach (var ou in state.OrganizationalUnits)
            SpawnOUNode(ou.Name, ouNodeParent);

        if (domainChildrenGroup != null)
            RebuildLayout(domainChildrenGroup.GetComponent<RectTransform>());
        RebuildLayout(treePanelRect);
    }

    // ── Shared OU node spawner ────────────────────────────────────────────────

    private void SpawnOUNode(string ouName, Transform parent)
    {
        var node  = Instantiate(ouNodePrefab, parent);
        var label = node.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = ouName;

        string captured = ouName;
        var btn = node.GetComponentInChildren<Button>();
        btn?.onClick.AddListener(() => SelectOU(captured));

        if (btn != null)
        {
            var handler = btn.gameObject.AddComponent<NodeClickHandler>();
            handler.onRightClick = () => OnOUNodeRightClick(captured);
        }
    }
}
