/*
 * ================================================================
 *  UNITY SETUP GUIDE — Windows10Manager
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to the Windows10Panel GameObject.
 *
 *  HIERARCHY
 *
 *    Windows10Panel  (this script here)
 *      ├─ PasswordLogin                   → passwordLoginPanel   (start ACTIVE — hides on login, shown on init)
 *      │    ├─ Default                    ← shown only on re-entry (password already created)
 *      │    │    ├─ PasswordInput         → passwordLoginInput   (TMP_InputField)
 *      │    │    ├─ WrongPasswordText     → wrongPasswordText    (TMP_Text, start INACTIVE)
 *      │    │    └─ LoginButton           → OnClick: Windows10Manager.OnLoginButtonClick()
 *      │    └─ SetupPassword              ← shown only on first access
 *      │         ├─ CreatePasswordInput   → createPasswordInput  (TMP_InputField)
 *      │         ├─ ConfirmPasswordInput  → confirmPasswordInput (TMP_InputField)
 *      │         ├─ WrongPasswordText     → setupWrongText       (TMP_Text, start INACTIVE)
 *      │         └─ SignUpButton          → OnClick: Windows10Manager.OnSignUpButtonClick()
 *      └─ Windows10Desktop                → windows10Desktop     (child of Windows10Panel)
 *           └─ [desktop content + TaskBar]
 *                TaskBar > ShutdownTray > Shutdown → OnClick: Windows10Manager.OnShutdown()
 *
 *  INSPECTOR ASSIGNMENTS
 *    passwordLoginPanel    → Windows10Panel > PasswordLogin
 *    defaultPanel          → PasswordLogin > Default
 *    setupPasswordPanel    → PasswordLogin > SetupPassword
 *    windows10Desktop      → Windows10Panel > Windows10Desktop
 *    monitorController     → T3MonitorController on the monitor root GameObject
 *    settingController        → SettingPanelController on Windows10Desktop > WindowsContent > SettingPanel
 *    deviceManagerController  → DeviceManagerController on Windows10Desktop > WindowsContent > DeviceManagerPanel
 *    taskBarController        → TaskBarController on Windows10Desktop > TaskBar
 *    passwordLoginInput    → Default > PasswordInput
 *    wrongPasswordText     → Default > WrongPasswordText
 *    createPasswordInput   → SetupPassword > CreatePasswordInput
 *    confirmPasswordInput  → SetupPassword > ConfirmPasswordInput
 *    setupWrongText        → SetupPassword > WrongPasswordText
 *
 *  HOW IT WORKS
 *    Enter key detection is done in Update() via Input.GetKeyDown + isFocused,
 *    which is more reliable than TMP_InputField.onSubmit / onEndEdit events.
 *    Buttons (SignUpButton, LoginButton) call the same logic directly.
 *
 *    First access (_sessionPasswordSet == false):
 *      PasswordLogin is shown; only SetupPassword sub-panel is visible.
 *      User fills CreatePasswordInput and ConfirmPasswordInput, then clicks SignUpButton
 *      (or presses Enter while focused on ConfirmPasswordInput):
 *        – Passwords match    → saves password, hides PasswordLogin, shows Windows10Desktop.
 *        – Passwords mismatch → clears ConfirmPasswordInput, shows setupWrongText.
 *      Pressing Enter in CreatePasswordInput moves focus to ConfirmPasswordInput.
 *
 *    Subsequent access (_sessionPasswordSet == true):
 *      PasswordLogin is shown; only Default sub-panel is visible.
 *      User types in PasswordInput, presses Enter or LoginButton:
 *        – Correct password → hides PasswordLogin, shows Windows10Desktop.
 *        – Wrong password   → clears PasswordInput, shows wrongPasswordText.
 *
 *    Shutdown button (TaskBar > ShutdownTray > Shutdown OR any caller):
 *      Resets SettingPanel navigation to 1stLevel and hides it.
 *      Hides Windows10Desktop, re-enables PasswordLogin, resets T3MonitorController
 *      to Loading, closes canvas.
 *      Next right-click: canvas → LoadingPanel → (timeout) → Windows10Panel.
 *      Player must log in again; SettingPanel opens at 1stLevel.
 *
 *  PERSISTENCE
 *    Session-only (static fields) — resets when the game restarts.
 *    _sessionPasswordSet (bool)   — whether setup has been completed this session.
 *    _sessionPassword    (string) — the password created this session.
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Windows10Manager : MonoBehaviour
{
    // Session-only storage — resets when the game restarts (no PlayerPrefs persistence intended)
    private static bool   _sessionPasswordSet;
    private static string _sessionPassword = "";

    // Latches true the first time InitWindows10Panel() runs — signals Task 6 completion.
    private static bool _passwordLoginShown;
    public static bool PasswordLoginShown => _passwordLoginShown;

    // Latches true when the player clicks the TaskBar shutdown button — signals Task 11 completion.
    private static bool _shutdownTriggered;
    public static bool ShutdownTriggered => _shutdownTriggered;

    // ----------------------------------------------------------------
    //  Panels
    // ----------------------------------------------------------------

    [Header("Panels")]
    [SerializeField] private GameObject passwordLoginPanel;
    [SerializeField] private GameObject defaultPanel;
    [SerializeField] private GameObject setupPasswordPanel;
    [SerializeField] private GameObject windows10Desktop;

    // ----------------------------------------------------------------
    //  Default — Login
    // ----------------------------------------------------------------

    [Header("Default — Login")]
    [SerializeField] private TMP_InputField passwordLoginInput;
    [SerializeField] private TMP_Text wrongPasswordText;

    // ----------------------------------------------------------------
    //  SetupPassword
    // ----------------------------------------------------------------

    [Header("SetupPassword")]
    [SerializeField] private TMP_InputField createPasswordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;
    [SerializeField] private TMP_Text setupWrongText;

    // ----------------------------------------------------------------
    //  References
    // ----------------------------------------------------------------

    [Header("References")]
    [SerializeField] private T3MonitorController    monitorController;
    [SerializeField] private SettingPanelController settingController;
    [SerializeField] private DeviceManagerController deviceManagerController;
    [SerializeField] private TaskBarController      taskBarController;

    // ----------------------------------------------------------------
    //  Runtime state
    // ----------------------------------------------------------------

    private string _pendingPassword;
    private bool _awaitingConfirm;           // true after create-password Enter; false until then
    private TMP_InputField _lastFocusedField; // field that was focused the frame before Enter fires

    public bool IsPasswordSetUp => _sessionPasswordSet;

    // ----------------------------------------------------------------
    //  Init — called by FifthPhaseManager.OnAcceptPrivacy()
    //         and by T3MonitorController.ProceedToWindows10Panel()
    // ----------------------------------------------------------------

    public void InitWindows10Panel()
    {
        bool isSetUp = IsPasswordSetUp;

        windows10Desktop?.SetActive(false);
        passwordLoginPanel?.SetActive(true);

        _passwordLoginShown = true;
        T3TaskListManager.CheckConditions();

        _pendingPassword  = "";
        _awaitingConfirm  = false;
        _lastFocusedField = null;

        if (isSetUp)
        {
            // Re-entry: show login panel only
            defaultPanel?.SetActive(true);
            setupPasswordPanel?.SetActive(false);

            if (passwordLoginInput != null) passwordLoginInput.text = "";
            SetActive(wrongPasswordText, false);
            passwordLoginInput?.ActivateInputField();
        }
        else
        {
            // First entry: show setup panel only, keep Default hidden
            defaultPanel?.SetActive(false);
            setupPasswordPanel?.SetActive(true);

            if (createPasswordInput  != null) createPasswordInput.text  = "";
            if (confirmPasswordInput != null) confirmPasswordInput.text = "";
            SetActive(setupWrongText, false);
            createPasswordInput?.ActivateInputField();
        }

        Debug.Log($"[Windows10Manager] Initialised — password set up: {isSetUp}");
    }

    // ----------------------------------------------------------------
    //  Enter-key detection
    //  TMP_InputField clears isFocused in the same frame Enter is pressed,
    //  so we track the last focused field across frames and use that instead.
    // ----------------------------------------------------------------

    private void Update()
    {
        // Step 1: record whichever field is focused RIGHT NOW (before Enter clears it)
        if (createPasswordInput != null && createPasswordInput.isFocused)
            _lastFocusedField = createPasswordInput;
        else if (confirmPasswordInput != null && confirmPasswordInput.isFocused)
            _lastFocusedField = confirmPasswordInput;
        else if (passwordLoginInput != null && passwordLoginInput.isFocused)
            _lastFocusedField = passwordLoginInput;

        // Step 2: check Enter
        var kb = Keyboard.current;
        if (kb == null) return;
        if (!kb.enterKey.wasPressedThisFrame && !kb.numpadEnterKey.wasPressedThisFrame) return;
        if (_lastFocusedField == null) return;

        // Step 3: dispatch using the field that was focused the frame before Enter fired
        if (setupPasswordPanel != null && setupPasswordPanel.activeSelf)
        {
            if (_lastFocusedField == createPasswordInput)
            {
                // Move focus to confirm field; user clicks SignUpButton to submit
                confirmPasswordInput?.ActivateInputField();
                return;
            }

            if (_lastFocusedField == confirmPasswordInput)
            {
                OnSignUpButtonClick();
                return;
            }
        }

        if (defaultPanel != null && defaultPanel.activeSelf && _lastFocusedField == passwordLoginInput)
            OnLoginPasswordSubmit(passwordLoginInput.text);
    }

    // ----------------------------------------------------------------
    //  SetupPassword handlers
    // ----------------------------------------------------------------

    private void OnCreatePasswordSubmit(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            ShowSetupError("Password cannot be empty.");
            return;
        }

        _pendingPassword = value;
        _awaitingConfirm = true;
        SetActive(setupWrongText, false);

        confirmPasswordInput?.ActivateInputField();
    }

    private void OnConfirmPasswordSubmit(string value)
    {
        if (string.IsNullOrEmpty(value) || value != _pendingPassword)
        {
            ShowSetupError("Passwords do not match.");
            if (confirmPasswordInput != null) confirmPasswordInput.text = "";
            confirmPasswordInput?.ActivateInputField();
            return;
        }

        _sessionPassword   = _pendingPassword;
        _sessionPasswordSet = true;

        _awaitingConfirm = false;

        setupPasswordPanel?.SetActive(false);
        defaultPanel?.SetActive(false);
        passwordLoginPanel?.SetActive(false);
        windows10Desktop?.SetActive(true);

        Debug.Log("[Windows10Manager] Password created — entering Windows10Desktop.");
    }

    private void ShowSetupError(string message)
    {
        if (setupWrongText == null) return;
        setupWrongText.text = message;
        SetActive(setupWrongText, true);
    }

    // ----------------------------------------------------------------
    //  Default — Login handler
    // ----------------------------------------------------------------

    private void OnLoginPasswordSubmit(string value)
    {
        string saved = _sessionPassword;

        if (value != saved)
        {
            SetActive(wrongPasswordText, true);
            if (passwordLoginInput != null) passwordLoginInput.text = "";
            passwordLoginInput?.ActivateInputField();
            return;
        }

        SetActive(wrongPasswordText, false);
        defaultPanel?.SetActive(false);
        passwordLoginPanel?.SetActive(false);
        windows10Desktop?.SetActive(true);

        Debug.Log("[Windows10Manager] Login successful — entering Windows10Desktop.");
    }

    // ----------------------------------------------------------------
    //  Button click handlers — wired to UI buttons in the scene
    // ----------------------------------------------------------------

    // SetupPassword > SignUpButton: validates both fields match, then enters Windows10Desktop
    public void OnSignUpButtonClick()
    {
        string create  = createPasswordInput  != null ? createPasswordInput.text  : "";
        string confirm = confirmPasswordInput != null ? confirmPasswordInput.text : "";

        if (string.IsNullOrEmpty(create))
        {
            ShowSetupError("Password cannot be empty.");
            return;
        }

        if (create != confirm)
        {
            ShowSetupError("Passwords do not match.");
            if (confirmPasswordInput != null) confirmPasswordInput.text = "";
            confirmPasswordInput?.ActivateInputField();
            return;
        }

        _sessionPassword    = create;
        _sessionPasswordSet = true;
        _awaitingConfirm    = false;

        setupPasswordPanel?.SetActive(false);
        passwordLoginPanel?.SetActive(false);
        windows10Desktop?.SetActive(true);

        Debug.Log("[Windows10Manager] Password created — entering Windows10Desktop.");
    }

    // Default > LoginButton: validate password and enter Windows10Desktop
    public void OnLoginButtonClick()
    {
        OnLoginPasswordSubmit(passwordLoginInput != null ? passwordLoginInput.text : "");
    }

    // ----------------------------------------------------------------
    //  Shutdown — Wired to Windows10Desktop > Shutdown > OnClick
    // ----------------------------------------------------------------

    public void OnShutdown()
    {
        _shutdownTriggered = true;
        T3TaskListManager.CheckConditions();

        // Close the tray UI before the desktop hides.
        taskBarController?.CloseAll();

        // Reset panels back to their default closed state.
        settingController?.Exit();
        deviceManagerController?.Exit();

        windows10Desktop?.SetActive(false);
        monitorController?.ResetToLoading();
        GameManager.Instance?.CloseEditor();

        Debug.Log("[Windows10Manager] Shutdown — canvas closed, loading reset.");
    }

    // ----------------------------------------------------------------
    //  Helper
    // ----------------------------------------------------------------

    private static void SetActive(TMP_Text text, bool value)
    {
        if (text != null) text.gameObject.SetActive(value);
    }
}
