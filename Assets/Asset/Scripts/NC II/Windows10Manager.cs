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
 *      ├─ Default                         ← always active while on login screen
 *      │    ├─ PasswordInput              → passwordLoginInput   (TMP_InputField)
 *      │    └─ WrongPasswordText          → wrongPasswordText    (TMP_Text, start INACTIVE)
 *      └─ SetupPassword                   ← active only on first access
 *           ├─ CreatePasswordInput        → createPasswordInput  (TMP_InputField)
 *           ├─ ConfirmPasswordInput       → confirmPasswordInput (TMP_InputField)
 *           └─ WrongPasswordText          → setupWrongText       (TMP_Text, start INACTIVE)
 *
 *    Windows10Desktop                     → windows10Desktop     (sibling of Windows10Panel)
 *      └─ Shutdown                        → OnClick: Windows10Manager.OnShutdown()
 *
 *  INSPECTOR ASSIGNMENTS
 *    defaultPanel          → Windows10Panel > Default
 *    setupPasswordPanel    → Windows10Panel > SetupPassword
 *    windows10Desktop      → Windows10Desktop panel
 *    monitorController     → T3MonitorController on the monitor root GameObject
 *    passwordLoginInput    → Default > PasswordInput
 *    wrongPasswordText     → Default > WrongPasswordText
 *    createPasswordInput   → SetupPassword > CreatePasswordInput
 *    confirmPasswordInput  → SetupPassword > ConfirmPasswordInput
 *    setupWrongText        → SetupPassword > WrongPasswordText
 *
 *  HOW IT WORKS
 *    Enter key detection is done in Update() via Input.GetKeyDown + isFocused,
 *    which is more reliable than TMP_InputField.onSubmit / onEndEdit events.
 *
 *    First access (W10_PasswordSet == 0):
 *      Both Default and SetupPassword panels are shown.
 *      User types in CreatePasswordInput, presses Enter → focus moves to ConfirmPasswordInput.
 *      User types in ConfirmPasswordInput, presses Enter:
 *        – Passwords match    → saves to PlayerPrefs, hides both panels, shows Windows10Desktop.
 *        – Passwords mismatch → clears ConfirmPasswordInput, shows setupWrongText.
 *
 *    Subsequent access (W10_PasswordSet == 1):
 *      Only Default panel is shown (SetupPassword stays inactive forever).
 *      User types in PasswordInput, presses Enter:
 *        – Correct password → hides Default, shows Windows10Desktop.
 *        – Wrong password   → clears PasswordInput, shows wrongPasswordText.
 *
 *    Shutdown button:
 *      Hides Windows10Desktop, resets T3MonitorController to Loading, closes canvas.
 *      Next right-click: canvas → LoadingPanel → (timeout) → Windows10Panel (skips full setup).
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

    // ----------------------------------------------------------------
    //  Panels
    // ----------------------------------------------------------------

    [Header("Panels")]
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
    [SerializeField] private T3MonitorController monitorController;

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

        defaultPanel?.SetActive(true);
        windows10Desktop?.SetActive(false);

        if (passwordLoginInput != null) passwordLoginInput.text = "";
        SetActive(wrongPasswordText, false);

        _pendingPassword  = "";
        _awaitingConfirm  = false;
        _lastFocusedField = null;

        if (isSetUp)
        {
            setupPasswordPanel?.SetActive(false);
        }
        else
        {
            setupPasswordPanel?.SetActive(true);
            if (createPasswordInput  != null) createPasswordInput.text  = "";
            if (confirmPasswordInput != null) confirmPasswordInput.text = "";
            SetActive(setupWrongText, false);

            // Give focus to the first setup field
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
            if (!_awaitingConfirm && _lastFocusedField == createPasswordInput)
            {
                OnCreatePasswordSubmit(createPasswordInput.text);
                return;
            }

            if (_awaitingConfirm && _lastFocusedField == confirmPasswordInput)
            {
                OnConfirmPasswordSubmit(confirmPasswordInput.text);
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
        windows10Desktop?.SetActive(true);

        Debug.Log("[Windows10Manager] Login successful — entering Windows10Desktop.");
    }

    // ----------------------------------------------------------------
    //  Shutdown — Wired to Windows10Desktop > Shutdown > OnClick
    // ----------------------------------------------------------------

    public void OnShutdown()
    {
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
