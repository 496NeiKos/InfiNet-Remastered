/*
 * ================================================================
 *  UNITY SETUP GUIDE — LoginUIController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Login UI Panel" GameObject (starts INACTIVE).
 *    Place as a DIRECT CHILD of Virtual OS Canvas — sibling of
 *    Restart UI Panel and Desktop Panel.
 *
 *  HIERARCHY
 *    Login UI Panel                        ← this script here (starts INACTIVE)
 *      ├── UsernameTMP                     → usernameTMP
 *      │     (large, prominent — shows the selected account name)
 *      ├── PasswordInput                   → passwordInput
 *      │     Content Type: Password
 *      │     Placeholder: "Password"
 *      ├── PasswordHintTMP                 → passwordHintTMP
 *      │     (small text below input — shows actual password for educational clarity)
 *      ├── WrongPasswordTMP                → wrongPasswordTMP (starts INACTIVE)
 *      │     Text: "The password is incorrect. Try again."
 *      ├── SignInBtn                       → signInBtn
 *      │     Label: "Sign in" or an arrow icon
 *      │
 *      ├── ChangeUserArea  (anchored bottom-left)
 *      │     ├── CurrentUserPanel          (small panel)
 *      │     │     └── CurrentUserTMP      → currentUserTMP
 *      │     ├── OtherUsersBtn             → otherUsersBtn
 *      │     │     Label: "Other users"
 *      │     └── UserListPanel             → userListPanel (starts INACTIVE)
 *      │           └── UserListContent     → userListContent
 *      │                 (UserButtonPrefab spawned here at runtime)
 *      │
 *      └── ChangePCArea  (anchored bottom-right)
 *            ├── CurrentPCPanel            (small panel)
 *            │     └── CurrentPCTMP        → currentPCTMP
 *            ├── PCListBtn                 → pcListBtn
 *            │     Label: "Switch PC"
 *            └── PCListPanel               → pcListPanel (starts INACTIVE)
 *                  ├── ServerPCBtn         → serverPCBtn
 *                  │     Label: "Server PC"
 *                  └── ClientPCBtn         → clientPCBtn
 *                        Label: "Client PC"
 *
 *  INSPECTOR ASSIGNMENTS
 *    userButtonPrefab  A Button prefab with a child TMP_Text.
 *                      Used to spawn user entries in the Change User list.
 *
 *  HOW IT WORKS
 *    ServerVirtualOSManager calls Show(ActivePC) after Restart UI completes.
 *    Show() builds the correct user list for that PC, selects the default
 *    user (last logged-in or first in list), and activates the panel.
 *    Sign In validates the typed password against the selected user's stored
 *    password (AdminPassword for server admin; LocalUserPassword or
 *    UserData.Password for client users).
 *    Change User: "Other users" toggles the user list. Selecting a name
 *    updates the prominent username TMP and adapts the password hint.
 *    Change PC: "Switch PC" toggles the PC list. Selecting a different PC
 *    calls ServerVirtualOSManager.SwitchToPC() — which triggers Restart.
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginUIController : MonoBehaviour
{
    // ── Login fields ──────────────────────────────────────────────────────────

    [Header("Login")]
    [SerializeField] private TMP_Text       usernameTMP;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text       passwordHintTMP;
    [SerializeField] private TMP_Text       wrongPasswordTMP;
    [SerializeField] private Button         signInBtn;

    // ── Change User (bottom-left) ─────────────────────────────────────────────

    [Header("Change User")]
    [SerializeField] private TMP_Text   currentUserTMP;
    [SerializeField] private Button     otherUsersBtn;
    [SerializeField] private GameObject userListPanel;
    [SerializeField] private Transform  userListContent;
    [SerializeField] private Button     userButtonPrefab;

    // ── Change PC (bottom-right) ──────────────────────────────────────────────

    [Header("Change PC")]
    [SerializeField] private TMP_Text   currentPCTMP;
    [SerializeField] private Button     pcListBtn;
    [SerializeField] private GameObject pcListPanel;
    [SerializeField] private Button     serverPCBtn;
    [SerializeField] private Button     clientPCBtn;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private string _selectedUser = "";
    private ActivePC _currentDisplayPC;
    private readonly List<Button> _spawnedUserButtons = new List<Button>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        signInBtn?.onClick.AddListener(OnSignIn);
        otherUsersBtn?.onClick.AddListener(ToggleUserList);
        pcListBtn?.onClick.AddListener(TogglePCList);
        serverPCBtn?.onClick.AddListener(() => OnSelectPC(ActivePC.Server));
        clientPCBtn?.onClick.AddListener(() => OnSelectPC(ActivePC.Client));
        passwordInput?.onSubmit.AddListener(_ => OnSignIn());

        userListPanel?.SetActive(false);
        pcListPanel?.SetActive(false);
        wrongPasswordTMP?.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Show(ActivePC pc)
    {
        _currentDisplayPC = pc;
        gameObject.SetActive(true);

        userListPanel?.SetActive(false);
        pcListPanel?.SetActive(false);
        wrongPasswordTMP?.gameObject.SetActive(false);
        if (passwordInput != null) passwordInput.text = "";

        RefreshPCLabel(pc);
        BuildUserList(pc);
    }

    // ── Sign In ───────────────────────────────────────────────────────────────

    private void OnSignIn()
    {
        string input = passwordInput != null ? passwordInput.text : "";

        if (!ValidatePassword(_selectedUser, input))
        {
            if (wrongPasswordTMP != null)
            {
                wrongPasswordTMP.text = "The password is incorrect. Try again.";
                wrongPasswordTMP.gameObject.SetActive(true);
            }
            if (passwordInput != null) passwordInput.text = "";
            return;
        }

        gameObject.SetActive(false);
        ServerVirtualOSManager.Instance?.OnLoginSuccess(_selectedUser);
        ActivityLogManager.Log($"Logged in as: {_selectedUser}", ActivityLogManager.EntryType.Action);
    }

    // ── Password Validation ───────────────────────────────────────────────────

    private bool ValidatePassword(string username, string input)
    {
        var mgr = ServerVirtualOSManager.Instance;
        if (mgr == null) return true;

        if (mgr.CurrentPC == ActivePC.Server)
        {
            string adminPw = mgr.ServerState?.AdminPassword ?? "";
            // Allow any input if admin password not yet set (pre-config first boot)
            return string.IsNullOrEmpty(adminPw) || input == adminPw;
        }
        else
        {
            var client = mgr.ClientState;
            if (client == null) return true;

            if (username == client.LocalUserName)
                return input == client.LocalUserPassword;

            if (client.DomainJoined)
            {
                var userData = mgr.ServerState?.UserAccounts
                    .Find(u => u.Username == username);
                if (userData != null)
                    return input == userData.Password;
            }

            return false;
        }
    }

    // ── User List ─────────────────────────────────────────────────────────────

    private void BuildUserList(ActivePC pc)
    {
        foreach (var btn in _spawnedUserButtons)
            if (btn != null) Destroy(btn.gameObject);
        _spawnedUserButtons.Clear();

        var users = GetUsersForPC(pc);
        string defaultUser = GetDefaultUser(pc, users);
        SelectUser(defaultUser);

        if (userButtonPrefab == null || userListContent == null) return;

        foreach (string uname in users)
        {
            var btn = Instantiate(userButtonPrefab, userListContent);
            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = uname;
            string captured = uname;
            btn.onClick.AddListener(() => OnSelectUserFromList(captured));
            _spawnedUserButtons.Add(btn);
        }
    }

    private List<string> GetUsersForPC(ActivePC pc)
    {
        var list = new List<string>();
        var mgr  = ServerVirtualOSManager.Instance;
        if (mgr == null) return list;

        if (pc == ActivePC.Server)
        {
            list.Add("Administrator");
        }
        else
        {
            var client = mgr.ClientState;
            if (client != null)
                list.Add(client.LocalUserName);

            if (client != null && client.DomainJoined && mgr.ServerState != null)
            {
                foreach (var u in mgr.ServerState.UserAccounts)
                    if (!string.IsNullOrEmpty(u.Username))
                        list.Add(u.Username);
            }
        }

        return list;
    }

    private string GetDefaultUser(ActivePC pc, List<string> users)
    {
        var mgr = ServerVirtualOSManager.Instance;
        string lastUser = pc == ActivePC.Server
            ? mgr?.ServerState?.CurrentLoggedInUser ?? ""
            : mgr?.ClientState?.CurrentLoggedInUser ?? "";

        if (!string.IsNullOrEmpty(lastUser) && users.Contains(lastUser))
            return lastUser;

        return users.Count > 0 ? users[0] : "";
    }

    private void SelectUser(string username)
    {
        _selectedUser = username;
        if (usernameTMP    != null) usernameTMP.text    = username;
        if (currentUserTMP != null) currentUserTMP.text = username;
        RefreshPasswordHint(username);
        if (passwordInput    != null) passwordInput.text = "";
        wrongPasswordTMP?.gameObject.SetActive(false);
    }

    private void RefreshPasswordHint(string username)
    {
        if (passwordHintTMP == null) return;
        var mgr = ServerVirtualOSManager.Instance;
        if (mgr == null) { passwordHintTMP.text = ""; return; }

        if (mgr.CurrentPC == ActivePC.Server)
        {
            string pw = mgr.ServerState?.AdminPassword ?? "";
            passwordHintTMP.text = string.IsNullOrEmpty(pw) ? "No password set yet" : pw;
        }
        else
        {
            var client = mgr.ClientState;
            if (client != null && username == client.LocalUserName)
            {
                passwordHintTMP.text = client.LocalUserPassword;
            }
            else if (client != null && client.DomainJoined)
            {
                var ud = mgr.ServerState?.UserAccounts.Find(u => u.Username == username);
                passwordHintTMP.text = ud?.Password ?? "";
            }
            else
            {
                passwordHintTMP.text = "";
            }
        }
    }

    private void OnSelectUserFromList(string username)
    {
        SelectUser(username);
        userListPanel?.SetActive(false);
    }

    private void ToggleUserList()
    {
        if (userListPanel == null) return;
        bool show = !userListPanel.activeSelf;
        userListPanel.SetActive(show);
        if (show) pcListPanel?.SetActive(false);
    }

    // ── PC Switch ─────────────────────────────────────────────────────────────

    private void RefreshPCLabel(ActivePC pc)
    {
        if (currentPCTMP == null) return;
        var mgr = ServerVirtualOSManager.Instance;
        currentPCTMP.text = pc == ActivePC.Server
            ? (mgr?.ServerState?.ComputerName is string s && s.Length > 0 ? s : "Server PC")
            : (mgr?.ClientState?.ComputerName ?? "Client PC");
    }

    private void OnSelectPC(ActivePC pc)
    {
        pcListPanel?.SetActive(false);
        var mgr = ServerVirtualOSManager.Instance;
        if (mgr == null || pc == mgr.CurrentPC) return;
        gameObject.SetActive(false);
        mgr.SwitchToPC(pc);
    }

    private void TogglePCList()
    {
        if (pcListPanel == null) return;
        bool show = !pcListPanel.activeSelf;
        pcListPanel.SetActive(show);
        if (show) userListPanel?.SetActive(false);
    }
}
