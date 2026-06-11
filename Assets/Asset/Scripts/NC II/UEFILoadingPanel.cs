/*
 * ================================================================
 *  UNITY SETUP GUIDE — UEFILoadingPanel
 * ================================================================
 *  Simulates the POST/boot screen that appears after powering on
 *  the PC. The player must press DEL or F2 within a time window
 *  to enter the UEFI setup. If they miss the window, a boot-failure
 *  popup is shown and they must power-cycle the system unit to retry.
 *
 *  STATE MACHINE
 *    FRESH       — 0 to inputDelay seconds after activation.
 *                  DEL/F2 ignored. Hint text hidden.
 *    INTERACTIVE — inputDelay reached. Hint text visible.
 *                  DEL/F2 opens the UEFI panel.
 *    TIMED_OUT   — bootTimeout seconds elapsed without DEL/F2.
 *                  Boot-failure popup shown. Player is stuck here
 *                  until they power-cycle the system unit.
 *
 *  STATE PERSISTENCE
 *    State is preserved across canvas close/re-open (Escape key).
 *    Only T3SystemUnitController.OnPoweredOn resets it to FRESH.
 *    This is natural Unity behaviour: Update() pauses while the
 *    GameObject is inactive, so the timer freezes on close.
 *
 *  STEP 1 — Create the LoadingPanel GameObject
 *    - Add as a child of uefiCanvasRoot, sibling of UEFIPanel.
 *    - Add UEFILoadingPanel component to it.
 *    - Start it INACTIVE (T3MonitorController activates it).
 *    - Build your loading-screen art as children of this object:
 *        - Manufacturer logo image
 *        - A "HintText" child (e.g. TextMeshProUGUI):
 *            Text: "Press DEL or F2 to enter UEFI Setup"
 *            Start INACTIVE — shown when INTERACTIVE state begins.
 *        - A "BootPopup" child panel:
 *            Contains a TMP_Text for the failure message.
 *            Start INACTIVE — shown when TIMED_OUT state begins.
 *
 *  STEP 2 — Wire the inspector
 *    inputDelay      → seconds before DEL/F2 becomes active (default 2)
 *    bootTimeout     → total seconds from activation before popup (default 10)
 *    hintText        → the HintText child GameObject
 *    bootPopup       → the BootPopup child GameObject
 *    bootMessageText → TMP_Text inside BootPopup
 *    fallbackBootMessage → message if no condition entry matches
 *    monitorController   → T3MonitorController on the UEFI Monitor root
 *    systemUnit          → T3SystemUnitController (subscribes to OnPoweredOn)
 *
 *  STEP 3 — Boot Conditions (inspector list)
 *    Add one entry per failure reason. The first entry whose
 *    condition fails is displayed. Use one condition type per entry:
 *
 *    USB check:
 *      message         → "No Bootable Device"
 *      usbPort         → CablePort on T3SystemUnitFront > USBPort
 *      settingButton   → (leave null)
 *      requiredValue   → (leave empty)
 *
 *    UEFI setting check:
 *      message         → "Invalid Boot Mode"
 *      usbPort         → (leave null)
 *      settingButton   → UEFISettingButton for "Boot Mode"
 *      requiredValue   → "UEFI"
 *
 *    If no entries fail (or the list is empty), fallbackBootMessage
 *    is shown instead.
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class UEFILoadingPanel : MonoBehaviour
{
    private enum LoadingState { Fresh, Interactive, TimedOut }

    [Header("Timing")]
    [Tooltip("Seconds after activation before DEL/F2 becomes active and hint appears.")]
    [SerializeField] private float inputDelay = 2f;

    [Tooltip("Total seconds from activation before the boot-failure popup fires.")]
    [SerializeField] private float bootTimeout = 10f;

    [Header("UI References")]
    [SerializeField] private GameObject hintText;
    [SerializeField] private GameObject bootPopup;
    [SerializeField] private TMP_Text bootMessageText;
    [SerializeField] private string fallbackBootMessage = "Operating System Not Found";

    [Header("Boot Conditions (first failing entry shows its message)")]
    [Tooltip("Checked in order. First entry whose condition fails is displayed in the popup.")]
    [SerializeField] private BootConditionEntry[] bootConditions;

    [Header("References")]
    [SerializeField] private T3MonitorController monitorController;
    [SerializeField] private T3SystemUnitController systemUnit;
    [SerializeField] private UEFINavigator navigator;
    [SerializeField] private UEFIBootStateValidator bootStateValidator;

    private LoadingState _state = LoadingState.Fresh;
    private float _timer;

    private void Start()
    {
        if (systemUnit != null)
            systemUnit.OnPoweredOn += ResetState;

        hintText?.SetActive(false);
        bootPopup?.SetActive(false);
    }

    private void OnDestroy()
    {
        if (systemUnit != null)
            systemUnit.OnPoweredOn -= ResetState;
    }

    private void Update()
    {
        if (_state == LoadingState.TimedOut) return;

        _timer += Time.deltaTime;

        if (_state == LoadingState.Fresh && _timer >= inputDelay)
        {
            _state = LoadingState.Interactive;
            hintText?.SetActive(true);
            Debug.Log("[UEFILoadingPanel] DEL/F2 now active.");
        }

        if (_state == LoadingState.Interactive && _timer >= bootTimeout)
        {
            TransitionToTimedOut();
            return;
        }

        if (_state == LoadingState.Interactive)
        {
            if (Keyboard.current.deleteKey.wasPressedThisFrame ||
                Keyboard.current.f2Key.wasPressedThisFrame)
            {
                EnterUEFI();
            }
        }
    }

    private void TransitionToTimedOut()
    {
        _state = LoadingState.TimedOut;
        hintText?.SetActive(false);

        // If the player saved a valid boot state via F10, proceed to Windows Setup.
        bool bootReady = navigator != null && navigator.BootStateSaved
            && (bootStateValidator == null || bootStateValidator.AllCorrect());

        if (bootReady)
        {
            Debug.Log("[UEFILoadingPanel] Boot state valid — proceeding to Windows Setup.");
            monitorController?.ProceedToWindowsSetup();
            return;
        }

        // Determine the most relevant error message.
        string msg = null;

        if (navigator == null || !navigator.BootStateSaved)
            msg = "UEFI settings not saved. Enter UEFI Setup and press F10 to save.";
        else
            msg = bootStateValidator?.GetFirstFailMessage();

        if (string.IsNullOrEmpty(msg))
            msg = EvaluateBootMessage();
        if (string.IsNullOrEmpty(msg))
            msg = fallbackBootMessage;

        if (bootMessageText != null)
            bootMessageText.text = msg;

        bootPopup?.SetActive(true);
        Debug.Log($"[UEFILoadingPanel] Boot timeout — showing: \"{msg}\"");
    }

    private void EnterUEFI()
    {
        Debug.Log("[UEFILoadingPanel] DEL/F2 pressed — entering UEFI.");
        monitorController?.OpenUEFIPanel();
    }

    private string EvaluateBootMessage()
    {
        if (bootConditions == null || bootConditions.Length == 0)
            return fallbackBootMessage;

        foreach (var entry in bootConditions)
        {
            bool failed = false;

            if (entry.usbPort != null && !entry.usbPort.IsInstalled)
                failed = true;

            if (!failed && entry.settingButton != null && !string.IsNullOrEmpty(entry.requiredValue))
                if (entry.settingButton.CurrentValue != entry.requiredValue)
                    failed = true;

            if (failed)
                return entry.message;
        }

        return fallbackBootMessage;
    }

    // Called via T3SystemUnitController.OnPoweredOn event (OFF → ON transition).
    public void ResetState()
    {
        _state = LoadingState.Fresh;
        _timer = 0f;
        hintText?.SetActive(false);
        bootPopup?.SetActive(false);
        Debug.Log("[UEFILoadingPanel] State reset to Fresh.");
    }
}

[System.Serializable]
public class BootConditionEntry
{
    [Tooltip("Message shown in the popup when this condition fails.")]
    public string message;

    [Tooltip("USB port to check. Fails if no device is installed. Leave null to skip.")]
    public CablePort usbPort;

    [Tooltip("UEFI setting button to check. Leave null to skip.")]
    public UEFISettingButton settingButton;

    [Tooltip("Value that settingButton.CurrentValue must equal. Leave empty to skip value check.")]
    public string requiredValue;
}
