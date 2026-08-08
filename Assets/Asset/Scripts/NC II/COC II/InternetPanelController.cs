/*
 * ================================================================
 *  UNITY SETUP GUIDE — InternetPanelController
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to "Internet Panel".
 *
 *  ACTUAL HIERARCHY (Internet Panel children)
 *    Internet Panel  (this script here)
 *      ├── Default  (active — always stays active while Internet Panel is open)
 *      │     └── Network Options  (active container)
 *      │           ├── Network Option 1  (Button)  → ssidButtons[0]
 *      │           ├── Network Option 2  (Button)  → ssidButtons[1]
 *      │           ├── Network Option 3  (Button)  → ssidButtons[2]
 *      │           ├── Option 1 Panel   (inactive) → optionPanels[0]
 *      │           ├── Option 2 Panel   (inactive) → optionPanels[1]
 *      │           │     ├── [AutoConnect ToggleBtn] → autoConnectBtns[1]
 *      │           │     └── NetworkConnect 2        → networkConnectBtns[1]
 *      │           └── Option 3 Panel   (inactive) → optionPanels[2]
 *      └── Network Password  (inactive)  → passwordPanel
 *            ├── [TMP_InputField]         → passwordField
 *            ├── OKBtn                    → confirmBtn
 *            └── CancelBtn                → cancelBtn
 *
 *  IMPORTANT
 *    Default is NEVER deactivated. Option panels live INSIDE Network Options
 *    (inside Default). The script only activates/deactivates Option panels
 *    and Network Password — Default always stays on.
 *
 *  INSPECTOR ASSIGNMENTS
 *    passwordPanel         → Network Password
 *    passwordField         → TMP_InputField inside Network Password
 *    correctPassword       → the WPA key (set here, changeable any time)
 *    ssidButtons[0..2]     → Network Option 1, 2, 3 buttons
 *    optionPanels[0..2]    → Option 1, 2, 3 Panel (inside Network Options)
 *    autoConnectBtns[0..2] → AutoConnect toggle button inside each Option Panel
 *    networkConnectBtns[0..2] → NetworkConnect button inside each Option Panel
 *    confirmBtn            → OKBtn inside Network Password
 *    cancelBtn             → CancelBtn inside Network Password
 *
 *  BUTTON OnClick — DO NOT wire manually. Script auto-wires in Awake.
 *
 *  WiFiBtn WIRING
 *    Handled by VirtualOSManager — just assign internetPanel reference there.
 *
 *  COLORS
 *    autoConnectOnColor  → green (toggle ON)
 *    autoConnectOffColor → gray  (toggle OFF)
 * ================================================================
 */

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InternetPanelController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject passwordPanel;

    [Header("SSID Buttons (index matches Option panels)")]
    [SerializeField] private Button[] ssidButtons;

    [Header("Option Panels (inside Network Options, inactive by default)")]
    [SerializeField] private GameObject[] optionPanels;

    [Header("Per-Option Controls (same index as optionPanels)")]
    [SerializeField] private Button[] autoConnectBtns;
    [SerializeField] private Button[] networkConnectBtns;

    [Header("Password Panel")]
    [SerializeField] private TMP_InputField passwordField;
    [SerializeField] private Button confirmBtn;
    [SerializeField] private Button cancelBtn;
    [Tooltip("Correct WPA key — changeable in Inspector at any time.")]
    [SerializeField] private string correctPassword = "password";

    [Header("Colors")]
    [SerializeField] private Color autoConnectOnColor  = new Color(0.35f, 0.65f, 0.35f);
    [SerializeField] private Color autoConnectOffColor = new Color(0.55f, 0.55f, 0.55f);

    private int    _activeOptionIndex = -1;
    private bool[] _autoConnect;
    private Coroutine _wrongKeyRoutine;

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        int count = optionPanels != null ? optionPanels.Length : 0;
        _autoConnect = new bool[count];

        // Hide all option panels and password panel at start
        foreach (var p in optionPanels) p?.SetActive(false);
        passwordPanel?.SetActive(false);

        // Auto-wire SSID buttons
        if (ssidButtons != null)
            for (int i = 0; i < ssidButtons.Length; i++)
            {
                int idx = i;
                ssidButtons[i]?.onClick.AddListener(() => SelectSSID(idx));
            }

        // Auto-wire AutoConnect toggles
        if (autoConnectBtns != null)
            for (int i = 0; i < autoConnectBtns.Length; i++)
            {
                int idx = i;
                autoConnectBtns[i]?.onClick.AddListener(() => ToggleAutoConnect(idx));
            }

        // Auto-wire NetworkConnect buttons
        if (networkConnectBtns != null)
            for (int i = 0; i < networkConnectBtns.Length; i++)
            {
                int idx = i;
                networkConnectBtns[i]?.onClick.AddListener(() => OpenPasswordPanel(idx));
            }

        // Auto-wire Password panel buttons
        confirmBtn?.onClick.AddListener(ConfirmPassword);
        cancelBtn?.onClick.AddListener(CancelPassword);

        // Refresh all AutoConnect button colors
        for (int i = 0; i < count; i++) RefreshAutoConnectColor(i);
    }

    // ----------------------------------------------------------------
    //  Toggle (called by VirtualOSManager via WiFiBtn listener)
    // ----------------------------------------------------------------

    public void Toggle()
    {
        bool willOpen = !gameObject.activeSelf;
        gameObject.SetActive(willOpen);
        if (willOpen) ResetToSSIDList();
    }

    // ----------------------------------------------------------------
    //  SSID selection → show Option Panel (Default stays active)
    // ----------------------------------------------------------------

    public void SelectSSID(int index)
    {
        if (!IsValidIndex(index)) return;

        // Check already connected
        DeviceOSState state = GetState();
        if (state.WifiConnected)
        {
            Debug.Log($"[InternetPanelController] Already connected — showing connected state.");
            return;
        }

        _activeOptionIndex = index;

        // Hide all option panels, show only the selected one
        foreach (var p in optionPanels) p?.SetActive(false);
        optionPanels[index]?.SetActive(true);

        // NOTE: Default panel stays active — option panel overlays inside it
        Debug.Log($"[InternetPanelController] SSID {index} selected → Option Panel {index} shown.");
    }

    // ----------------------------------------------------------------
    //  AutoConnect toggle
    // ----------------------------------------------------------------

    public void ToggleAutoConnect(int index)
    {
        if (_autoConnect == null || !IsValidIndex(index)) return;
        _autoConnect[index] = !_autoConnect[index];
        RefreshAutoConnectColor(index);
        Debug.Log($"[InternetPanelController] AutoConnect[{index}] = {_autoConnect[index]}.");
    }

    // ----------------------------------------------------------------
    //  NetworkConnect → show Password Panel
    // ----------------------------------------------------------------

    public void OpenPasswordPanel(int index)
    {
        _activeOptionIndex = index;

        // Hide current option panel
        foreach (var p in optionPanels) p?.SetActive(false);

        if (passwordField != null) passwordField.text = "";
        passwordPanel?.SetActive(true);

        Debug.Log($"[InternetPanelController] Password panel opened for option {index}.");
    }

    // ----------------------------------------------------------------
    //  Password confirmation
    // ----------------------------------------------------------------

    public void ConfirmPassword()
    {
        string entered = passwordField != null ? passwordField.text : "";

        if (entered == correctPassword)
        {
            ConnectSuccessfully();
        }
        else
        {
            if (_wrongKeyRoutine != null) StopCoroutine(_wrongKeyRoutine);
            _wrongKeyRoutine = StartCoroutine(ShowWrongKeys(entered));
        }
    }

    public void CancelPassword()
    {
        if (_wrongKeyRoutine != null)
        {
            StopCoroutine(_wrongKeyRoutine);
            _wrongKeyRoutine = null;
        }

        passwordPanel?.SetActive(false);
        // Return to option panel if there was one active
        if (IsValidIndex(_activeOptionIndex))
            optionPanels[_activeOptionIndex]?.SetActive(true);

        Debug.Log("[InternetPanelController] Password cancelled.");
    }

    // ----------------------------------------------------------------
    //  Private
    // ----------------------------------------------------------------

    private void ConnectSuccessfully()
    {
        DeviceOSState state = GetState();
        state.WifiConnected = true;
        state.WifiSSID      = $"Network_Option_{_activeOptionIndex}";
        state.WifiPassword  = correctPassword;

        passwordPanel?.SetActive(false);
        foreach (var p in optionPanels) p?.SetActive(false);

        // Close Internet Panel after connecting
        gameObject.SetActive(false);
        Debug.Log($"[InternetPanelController] Connected to option {_activeOptionIndex}.");
    }

    private IEnumerator ShowWrongKeys(string originalText)
    {
        if (passwordField != null)
            passwordField.text = "Wrong keys";

        yield return new WaitForSeconds(3f);

        if (passwordField != null)
            passwordField.text = originalText;

        _wrongKeyRoutine = null;
    }

    private void ResetToSSIDList()
    {
        foreach (var p in optionPanels) p?.SetActive(false);
        passwordPanel?.SetActive(false);
        _activeOptionIndex = -1;
    }

    private void RefreshAutoConnectColor(int index)
    {
        if (autoConnectBtns == null || index >= autoConnectBtns.Length) return;
        var btn = autoConnectBtns[index];
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null)
            img.color = (_autoConnect != null && _autoConnect[index])
                ? autoConnectOnColor : autoConnectOffColor;
    }

    private bool IsValidIndex(int index) =>
        optionPanels != null && index >= 0 && index < optionPanels.Length;

    private static DeviceOSState GetState() =>
        VirtualOSManager.Instance?.CurrentState ?? new DeviceOSState();
}
