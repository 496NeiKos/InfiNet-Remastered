/*
 * ================================================================
 *  UNITY SETUP GUIDE — UEFIBootStateValidator
 * ================================================================
 *  PURPOSE
 *    Holds the inspector-configured list of UEFI setting fields that
 *    must be correctly set before the system can boot to Windows Setup.
 *    Placed as a separate component on the UEFI Monitor root GameObject
 *    (same object as T3MonitorController and UEFINavigator).
 *
 *  STEP 1 — Add component
 *    Add UEFIBootStateValidator to the UEFI Monitor root GameObject.
 *
 *  STEP 2 — Wire requiredSettings in the inspector
 *    Add one entry per field that must be configured. For each entry:
 *      message       → error text shown in BootPopup if this field fails
 *      button        → the UEFISettingButton for this field
 *      requiredValue → the value CurrentValue must equal (mustNotEqual = false)
 *                      OR must NOT equal (mustNotEqual = true)
 *      mustNotEqual  → set true for Boot Option: passes when value != "None"
 *
 *  REQUIRED SETTINGS REFERENCE
 *    CPU Configuration:
 *      Hyper-Threading          Enabled   mustNotEqual=false
 *      Virtualization (VT-x)    Enabled   mustNotEqual=false
 *      Turbo Boost              Enabled   mustNotEqual=false
 *    Storage Configuration:
 *      SATA Mode                AHCI      mustNotEqual=false
 *      NVMe Configuration       Enabled   mustNotEqual=false
 *    USB Configuration:
 *      USB Ports                Enabled   mustNotEqual=false
 *      Legacy USB Support       Enabled   mustNotEqual=false
 *    Integrated Peripherals:
 *      Audio Controller         Enabled   mustNotEqual=false
 *      LAN Controller           Enabled   mustNotEqual=false
 *      Wi-Fi/Bluetooth          Enabled   mustNotEqual=false
 *    Trusted Computing (TPM):
 *      TPM 2.0                  Enabled   mustNotEqual=false
 *      Intel PTT                Enabled   mustNotEqual=false
 *      AMD fTPM                 Enabled   mustNotEqual=false
 *    Boot Priority:
 *      Boot Option              None      mustNotEqual=true
 *    Secure Boot:
 *      OS Type                  Windows UEFI Mode   mustNotEqual=false
 *
 *  STEP 3 — Reference from other scripts
 *    UEFILoadingPanel  → assign in inspector; reads AllCorrect() on timeout
 *    T3TaskListManager → assign in inspector; reads AllCorrect() for task condition
 * ================================================================
 */

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UEFIRequiredSetting
{
    [Tooltip("Error message shown in the boot popup when this condition fails.")]
    public string message;

    [Tooltip("The UEFISettingButton to check.")]
    public UEFISettingButton button;

    [Tooltip("The value CurrentValue must equal (mustNotEqual=false) or must NOT equal (mustNotEqual=true).")]
    public string requiredValue;

    [Tooltip("If true, condition passes when CurrentValue != requiredValue. Use for Boot Option ('not None').")]
    public bool mustNotEqual;
}

public class UEFIBootStateValidator : MonoBehaviour
{
    [SerializeField] private UEFIRequiredSetting[] requiredSettings;

    public bool AllCorrect()
    {
        if (requiredSettings == null) return true;

        foreach (var entry in requiredSettings)
        {
            if (entry.button == null) continue;
            if (!Passes(entry)) return false;
        }

        return true;
    }

    // Returns the message of the first failing entry, or null if all pass.
    public string GetFirstFailMessage()
    {
        if (requiredSettings == null) return null;

        foreach (var entry in requiredSettings)
        {
            if (entry.button == null) continue;
            if (!Passes(entry)) return entry.message;
        }

        return null;
    }

    // Returns messages for every failing entry (used to build the full guidance popup).
    public List<string> GetAllFailMessages()
    {
        var result = new List<string>();
        if (requiredSettings == null) return result;

        foreach (var entry in requiredSettings)
        {
            if (entry.button == null) continue;
            if (!Passes(entry)) result.Add(entry.message);
        }

        return result;
    }

    private static bool Passes(UEFIRequiredSetting entry)
    {
        bool valueMatches = entry.button.CurrentValue == entry.requiredValue;
        return entry.mustNotEqual ? !valueMatches : valueMatches;
    }
}
