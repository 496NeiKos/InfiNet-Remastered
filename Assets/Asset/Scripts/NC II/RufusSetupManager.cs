/*
 * ================================================================
 *  UNITY SETUP GUIDE — RufusSetupManager
 * ================================================================
 *  STEP 1 — Component placement
 *    Add this script to the RufusSetUp GameObject.
 *
 *  STEP 2 — Convert the "Select" object to a Button
 *    a) Remove the TMP_Dropdown component from "Select".
 *    b) Add a Button component.
 *    c) Wire OnClick → RufusSetupManager.OpenFilePicker()
 *
 *  STEP 3 — Convert "Volume Label" to a plain Text display
 *    a) Remove the TMP_Dropdown (or any other component) from "Volume Label".
 *    b) Add (or keep) a Text (Legacy) component on it — same pattern as
 *       the "Ready" status bar.
 *    c) The script writes the ISO filename to it when the user picks an ISO.
 *       The student never types into it.
 *
 *  STEP 4 — Fix the "Ready" status bar
 *    The "Ready" object currently has a Button component that calls
 *    MarkRufusComplete() directly, bypassing all validation.
 *    → Either REMOVE the Button component from "Ready", or clear its
 *      OnClick event list. Leave its Text (Legacy) child alone — the
 *      script writes status messages to that Text at runtime.
 *
 *  STEP 5 — Create the IsoFilePicker popup panel
 *    Inside RufusSetUp, add a child panel (e.g. "IsoFilePicker").
 *    Starts INACTIVE. Layout:
 *
 *      IsoFilePicker (Panel, inactive)
 *        ├─ Title Text  — "Select ISO File"
 *        ├─ IsoButton_Win10 (Button)
 *        │     OnClick → SelectIso("Win10_22H2_English_x64.iso")
 *        └─ CancelButton (Button)
 *              OnClick → CloseFilePicker()
 *
 *  STEP 6 — Wire Inspector fields on RufusSetupManager
 *
 *    USB Port (checked on every open):
 *      usbPort                 → CablePort on the system unit's front USB slot
 *                                Device dropdown shows USB only when this is Installed.
 *
 *    Dropdowns — Device Properties:
 *      deviceDropdown          → Device            (TMP_Dropdown)
 *      bootSelectionDropdown   → Boot Selection    (TMP_Dropdown)
 *      imageOptionDropdown     → Image Option      (TMP_Dropdown)
 *      partitionSchemeDropdown → Partition Scheme  (TMP_Dropdown)
 *      targetSystemDropdown    → Target System     (TMP_Dropdown)
 *
 *    Format Options:
 *      volumeLabelText         → Volume Label      (Text Legacy — display only)
 *      fileSystemDropdown      → File System       (TMP_Dropdown)
 *      clusterSizeDropdown     → Cluster Size      (TMP_Dropdown)
 *
 *    File Picker:
 *      filePickerPanel         → IsoFilePicker panel (starts inactive)
 *
 *    Status:
 *      statusText              → Text (Legacy) child of the "Ready" object
 *
 *    Navigator:
 *      navigator               → T2MonitorNavigator on the T2Monitor GameObject
 *
 *  STEP 7 — Wire buttons
 *    Start button:
 *      Remove:  T2MonitorNavigator → MarkRufusComplete()
 *      Add:     RufusSetupManager  → OnStartClicked()
 *
 *    CLOSE button (resets everything on close):
 *      Remove:  T2MonitorNavigator → CloseRufus()
 *      Add:     RufusSetupManager  → CloseAndReset()
 *
 *    Back button (preserves dropdown state on close):
 *      Keep:    T2MonitorNavigator → CloseRufus()   (no change needed)
 *
 *  CORRECT CONFIGURATION (for task completion):
 *    Boot Selection   → any .iso file selected via the file picker
 *    Image Option     → Standard Windows Installation
 *    Partition Scheme → MBR
 *    Target System    → UEFI (non CSM)
 *    Volume Label     → auto-filled from ISO name (display only, not editable)
 *    File System      → NTFS
 *    Cluster Size     → 4096 bytes (Default)
 *    Device is always valid (only one option).
 *
 *  FORMAT OPTIONS LOCK:
 *    File System and Cluster Size start non-interactable.
 *    They become interactable only after an ISO file is selected.
 *    Volume Label is display-only and never interactable.
 * ================================================================
 */

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RufusSetupManager : MonoBehaviour
{
    [Header("Dropdowns — Device Properties")]
    [SerializeField] private TMP_Dropdown deviceDropdown;
    [SerializeField] private TMP_Dropdown bootSelectionDropdown;
    [SerializeField] private TMP_Dropdown imageOptionDropdown;
    [SerializeField] private TMP_Dropdown partitionSchemeDropdown;
    [SerializeField] private TMP_Dropdown targetSystemDropdown;

    [Header("USB Port")]
    [Tooltip("The CablePort on the system unit's front USB slot. Checked every time the panel opens.")]
    [SerializeField] private CablePort usbPort;

    [Header("Format Options")]
    [Tooltip("Text (Legacy) display for Volume Label — auto-filled with the ISO filename. Not editable by the student.")]
    [SerializeField] private Text volumeLabelText;
    [SerializeField] private TMP_Dropdown fileSystemDropdown;
    [SerializeField] private TMP_Dropdown clusterSizeDropdown;

    [Header("ISO File Picker")]
    [Tooltip("Popup panel listing ISO files. Starts inactive.")]
    [SerializeField] private GameObject filePickerPanel;

    [Header("Status Bar")]
    [Tooltip("Text (Legacy) component inside the 'Ready' object's child — used as the status bar.")]
    [SerializeField] private Text statusText;

    [Header("Navigator")]
    [SerializeField] private T2MonitorNavigator navigator;

    // Correct answer indices.
    private const int CorrectImageOption      = 0; // "Standard Windows Installation"
    private const int CorrectPartitionScheme  = 0; // "MBR"
    private const int CorrectTargetSystem     = 1; // "UEFI (non CSM)"
    private const int CorrectFileSystem       = 1; // "NTFS"
    private const int CorrectClusterSize      = 3; // "4096 bytes (Default)"

    private bool _formatting;

    private void Awake()
    {
        PopulateDropdowns();
        SetFormatOptionsInteractable(false);
        RefreshDeviceDropdown();

        if (volumeLabelText != null)
            volumeLabelText.text = string.Empty;

        if (filePickerPanel != null)
            filePickerPanel.SetActive(false);

        SetStatus("Ready");
    }

    // Refreshes the Device dropdown on every panel open to reflect actual USB state.
    private void OnEnable()
    {
        RefreshDeviceDropdown();
    }

    private void RefreshDeviceDropdown()
    {
        if (deviceDropdown == null) return;
        deviceDropdown.ClearOptions();

        deviceDropdown.AddOptions(new System.Collections.Generic.List<string>
        {
            IsUsbPhysicallyInstalled() ? "Kingston DataTraveler 32GB (E:)" : "(No device detected)"
        });
        deviceDropdown.value = 0;
        deviceDropdown.RefreshShownValue();
    }

    // Checks whether a CableBehavior is physically a child of the USB port transform.
    // CablePort.IsInstalled can't be trusted here — it defaults to true before
    // CablePort.Awake() runs (the port lives in an initially-inactive hierarchy).
    private bool IsUsbPhysicallyInstalled()
    {
        return usbPort != null && usbPort.GetComponentInChildren<CableBehavior>() != null;
    }

    // ----------------------------------------------------------------
    //  Dropdown population
    // ----------------------------------------------------------------

    private void PopulateDropdowns()
    {
        // Device is handled by RefreshDeviceDropdown() instead.

        SetOptions(bootSelectionDropdown, new[]
        {
            "Disk or ISO image (Please select)",
            "FreeDOS",
            "Non bootable"
        });

        SetOptions(imageOptionDropdown, new[]
        {
            "Standard Windows Installation",
            "Windows To Go"
        });

        SetOptions(partitionSchemeDropdown, new[]
        {
            "MBR",
            "GPT"
        });

        SetOptions(targetSystemDropdown, new[]
        {
            "BIOS (or UEFI-CSM)",
            "UEFI (non CSM)"
        });

        SetOptions(fileSystemDropdown, new[]
        {
            "FAT32 (Default)",
            "NTFS",
            "UDF",
            "exFAT"
        });

        SetOptions(clusterSizeDropdown, new[]
        {
            "512 bytes",
            "1024 bytes",
            "2048 bytes",
            "4096 bytes (Default)",
            "8192 bytes",
            "16 kilobytes",
            "32 kilobytes"
        });
    }

    private static void SetOptions(TMP_Dropdown dropdown, string[] options)
    {
        if (dropdown == null) return;
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>(options));
        dropdown.value = 0;
        dropdown.RefreshShownValue();
    }

    // ----------------------------------------------------------------
    //  Format options lock (disabled until ISO is selected)
    // ----------------------------------------------------------------

    private void SetFormatOptionsInteractable(bool state)
    {
        if (fileSystemDropdown != null)  fileSystemDropdown.interactable  = state;
        if (clusterSizeDropdown != null) clusterSizeDropdown.interactable = state;
    }

    // ----------------------------------------------------------------
    //  Close vs Back
    //    Back button  → T2MonitorNavigator.CloseRufus()   (preserves state)
    //    Close button → RufusSetupManager.CloseAndReset() (wipes state)
    // ----------------------------------------------------------------

    // Wired to the CLOSE button's OnClick.
    public void CloseAndReset()
    {
        StopAllCoroutines();
        _formatting = false;

        // Restore Boot Selection placeholder at index 0.
        if (bootSelectionDropdown != null && bootSelectionDropdown.options.Count > 0)
        {
            bootSelectionDropdown.options[0] = new TMP_Dropdown.OptionData("Disk or ISO image (Please select)");
            bootSelectionDropdown.value = 0;
            bootSelectionDropdown.RefreshShownValue();
        }

        ResetDropdown(imageOptionDropdown);
        ResetDropdown(partitionSchemeDropdown);
        ResetDropdown(targetSystemDropdown);
        ResetDropdown(fileSystemDropdown);
        ResetDropdown(clusterSizeDropdown);

        if (volumeLabelText != null)
            volumeLabelText.text = string.Empty;

        SetFormatOptionsInteractable(false);

        if (filePickerPanel != null)
            filePickerPanel.SetActive(false);

        SetStatus("Ready");

        if (navigator != null)
            navigator.CloseRufus();
    }

    private static void ResetDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return;
        dropdown.value = 0;
        dropdown.RefreshShownValue();
    }

    // ----------------------------------------------------------------
    //  ISO file picker
    // ----------------------------------------------------------------

    // Wired to the Select button's OnClick.
    public void OpenFilePicker()
    {
        if (filePickerPanel != null)
            filePickerPanel.SetActive(true);
    }

    // Wired to the Cancel button inside IsoFilePicker.
    public void CloseFilePicker()
    {
        if (filePickerPanel != null)
            filePickerPanel.SetActive(false);
    }

    // Wired to each ISO button: OnClick → SelectIso("Win10_22H2_English_x64.iso")
    public void SelectIso(string isoName)
    {
        if (bootSelectionDropdown != null && bootSelectionDropdown.options.Count > 0)
        {
            bootSelectionDropdown.options[0] = new TMP_Dropdown.OptionData(isoName);
            bootSelectionDropdown.value = 0;
            bootSelectionDropdown.RefreshShownValue();
        }

        if (volumeLabelText != null)
            volumeLabelText.text = isoName;

        SetFormatOptionsInteractable(true);
        CloseFilePicker();
        SetStatus("Ready");
        Debug.Log($"[RufusSetupManager] ISO selected: {isoName}");
    }

    // ----------------------------------------------------------------
    //  Start button
    // ----------------------------------------------------------------

    // Wired to the Start button's OnClick.
    public void OnStartClicked()
    {
        if (_formatting) return;

        if (Validate(out string hint))
            StartCoroutine(RunFormatting());
        else
            SetStatus(hint);
    }

    private IEnumerator RunFormatting()
    {
        _formatting = true;
        SetStatus("Starting...");
        yield return new WaitForSeconds(1f);

        for (int pct = 0; pct <= 100; pct += 20)
        {
            SetStatus($"Writing... {pct}%");
            yield return new WaitForSeconds(0.25f);
        }

        SetStatus("DONE");
        yield return new WaitForSeconds(0.6f);

        _formatting = false;
        if (navigator != null)
            navigator.MarkRufusComplete();
    }

    // ----------------------------------------------------------------
    //  Validation
    // ----------------------------------------------------------------

    private bool Validate(out string hint)
    {
        if (!IsUsbPhysicallyInstalled())
        {
            hint = "Plug a USB flash drive into the front USB port first.";
            return false;
        }
        if (!IsIsoSelected())
        {
            hint = "Click SELECT to choose an ISO file first.";
            return false;
        }
        if (!Check(imageOptionDropdown, CorrectImageOption))
        {
            hint = "Set Image Option to 'Standard Windows Installation'.";
            return false;
        }
        if (!Check(partitionSchemeDropdown, CorrectPartitionScheme))
        {
            hint = "Set Partition Scheme to 'MBR'.";
            return false;
        }
        if (!Check(targetSystemDropdown, CorrectTargetSystem))
        {
            hint = "Set Target System to 'UEFI (non CSM)'.";
            return false;
        }
        if (volumeLabelText == null || string.IsNullOrWhiteSpace(volumeLabelText.text))
        {
            hint = "Select an ISO file to set the Volume Label.";
            return false;
        }
        if (!Check(fileSystemDropdown, CorrectFileSystem))
        {
            hint = "Set File System to 'NTFS'.";
            return false;
        }
        if (!Check(clusterSizeDropdown, CorrectClusterSize))
        {
            hint = "Set Cluster Size to '4096 bytes (Default)'.";
            return false;
        }

        hint = null;
        return true;
    }

    private bool IsIsoSelected()
    {
        if (bootSelectionDropdown == null || bootSelectionDropdown.options.Count == 0)
            return false;
        string current = bootSelectionDropdown.options[bootSelectionDropdown.value].text;
        return current.EndsWith(".iso", StringComparison.OrdinalIgnoreCase);
    }

    private static bool Check(TMP_Dropdown dropdown, int expected)
    {
        if (dropdown == null)
        {
            Debug.LogWarning("[RufusSetupManager] A dropdown is not assigned in the Inspector.");
            return false;
        }
        return dropdown.value == expected;
    }

    // ----------------------------------------------------------------
    //  Status bar
    // ----------------------------------------------------------------

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        Debug.Log($"[RufusSetupManager] {message}");
    }
}
