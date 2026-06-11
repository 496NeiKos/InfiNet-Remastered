/*
 * ================================================================
 *  UNITY SETUP GUIDE — UEFISettingButton
 * ================================================================
 *  PURPOSE
 *    Attach to any interactable field button inside Panel_Advanced
 *    (or any UEFI panel). On click it opens the shared UEFIOptionPopup
 *    with this button's option list. When the player picks a value the
 *    button label updates to "SettingLabel: Value" and conditions are
 *    re-evaluated.
 *
 *  STEP 1 — Button GameObject setup
 *    - The Button GameObject needs a TMP_Text child for the label.
 *      (Standard Unity Button → Text (TMP) structure is fine.)
 *    - Add component: UEFISettingButton.
 *    - Wire the Button's OnClick() → UEFISettingButton.OnClick()
 *
 *  STEP 2 — Wire the inspector
 *    settingLabel   → display name shown in the button label,
 *                     e.g. "Boot Mode", "Virtualization", "SATA Mode"
 *    options        → the selectable values for this setting,
 *                     e.g. ["Legacy BIOS", "UEFI"]
 *    defaultIndex   → which option is selected on scene load (0 = first)
 *
 *  STEP 3 — Reading the current value (for task conditions)
 *    Other scripts (T3TaskListManager, UEFINavigator) can read:
 *      button.CurrentValue   — the string currently selected
 *    Assign the specific UEFISettingButton reference in the inspector
 *    of whatever script needs to check the value.
 *
 *  DISPLAY-ONLY buttons
 *    For buttons that show information but are NOT interactable
 *    (e.g. rows in Panel_Home / Panel_Main), do NOT add this component.
 *    Those are plain Buttons or TMP_Text objects with no script.
 *
 *  EXAMPLE SETUP — "Boot Mode" in Panel_Boot
 *    settingLabel  : Boot Mode
 *    options       : Legacy BIOS, UEFI
 *    defaultIndex  : 0   (starts at "Legacy BIOS")
 *    → Button label starts as  "Boot Mode: Legacy BIOS"
 *    → Player clicks → popup shows [Legacy BIOS] [UEFI]
 *    → Selects UEFI → label becomes "Boot Mode: UEFI"
 * ================================================================
 */

using TMPro;
using UnityEngine;

public class UEFISettingButton : MonoBehaviour
{
    [Tooltip("Label shown before the colon, e.g. \"Boot Mode\".")]
    [SerializeField] private string settingLabel;

    [Tooltip("Selectable values shown in the popup, e.g. {\"Legacy BIOS\", \"UEFI\"}.")]
    [SerializeField] private string[] options;

    [Tooltip("Index into options[] that is active on scene load.")]
    [SerializeField] private int defaultIndex = 0;

    [Header("Dynamic USB Option (Boot Priority field only)")]
    [Tooltip("If assigned and IsInstalled, appends usbDeviceLabel as a second option.")]
    [SerializeField] private CablePort usbPort;
    [Tooltip("Label added when USB device is installed, e.g. \"UEFI: Kingston USB\".")]
    [SerializeField] private string usbDeviceLabel = "UEFI: Kingston USB";

    // Backing field so the getter can lazy-initialize before Start() runs.
    // UEFIPanel starts inactive, so Start() is deferred until the panel is first opened.
    // The validator reads CurrentValue on inactive buttons — lazy init ensures the default
    // value is returned correctly without requiring the player to open UEFI first.
    private string _currentValue;
    private bool _valueInitialized;

    public string CurrentValue
    {
        get
        {
            if (!_valueInitialized) InitFromDefault();
            return _currentValue;
        }
    }

    private void InitFromDefault()
    {
        _valueInitialized = true;
        _currentValue = (options != null && options.Length > 0)
            ? options[Mathf.Clamp(defaultIndex, 0, options.Length - 1)]
            : string.Empty;
    }

    private TMP_Text _label;

    private void Start()
    {
        _label = GetComponentInChildren<TMP_Text>();
        if (!_valueInitialized) InitFromDefault();
        UpdateLabel();
    }

    // Wire Button OnClick → this method.
    public void OnClick()
    {
        Debug.Log($"[UEFISettingButton] OnClick — label: '{settingLabel}', options: {options?.Length}");

        if (options == null || options.Length == 0)
        {
            Debug.LogWarning($"[UEFISettingButton] OnClick aborted — '{settingLabel}' has no options configured.");
            return;
        }

        var popup = UEFIOptionPopup.Instance;
        if (popup == null)
        {
            Debug.LogError("[UEFISettingButton] UEFIOptionPopup.Instance is null — add UEFIOptionPopup to the scene.");
            return;
        }

        popup.Show(settingLabel, BuildLiveOptions(), OnValueSelected);
    }

    // Returns the static options list, extended with the USB device label if the port is installed.
    private string[] BuildLiveOptions()
    {
        if (usbPort == null || !usbPort.IsInstalled)
            return options;

        var list = new System.Collections.Generic.List<string>(options);
        if (!list.Contains(usbDeviceLabel))
            list.Add(usbDeviceLabel);
        return list.ToArray();
    }

    private void OnValueSelected(string value)
    {
        _valueInitialized = true;
        _currentValue = value;
        UpdateLabel();
        T3TaskListManager.CheckConditions();
        Debug.Log($"[UEFISettingButton] {settingLabel} set to: {value}");
    }

    private void UpdateLabel()
    {
        if (_label != null)
            _label.text = $"{settingLabel}: {CurrentValue}";
    }
}
