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
 *  STEP 3 — Convert "Volume Label" to a TMP_InputField
 *    a) Remove the TMP_Dropdown component from "Volume Label".
 *    b) Add a TMP_InputField component.
 *    c) The field starts non-interactable and auto-fills with the ISO
 *       filename when the user picks an ISO from the file picker.
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
 *    Dropdowns — Device Properties:
 *      deviceDropdown          → Device            (TMP_Dropdown)
 *      bootSelectionDropdown   → Boot Selection    (TMP_Dropdown)
 *      imageOptionDropdown     → Image Option      (TMP_Dropdown)
 *      partitionSchemeDropdown → Partition Scheme  (TMP_Dropdown)
 *      targetSystemDropdown    → Target System     (TMP_Dropdown)
 *
 *    Format Options:
 *      volumeLabelField        → Volume Label      (TMP_InputField)  ← was dropdown
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
 *  STEP 7 — Rewire the Start button
 *    Remove:  T2MonitorNavigator → MarkRufusComplete()
 *    Add:     RufusSetupManager  → OnStartClicked()
 *
 *  CORRECT CONFIGURATION (for task completion):
 *    Boot Selection   → any .iso file selected via the file picker
 *    Image Option     → Standard Windows Installation
 *    Partition Scheme → GPT
 *    Target System    → UEFI (non CSM)
 *    Volume Label     → any non-empty text (auto-filled from ISO name)
 *    File System      → NTFS
 *    Cluster Size     → 4096 bytes (Default)
 *    Device is always valid (only one option).
 *
 *  FORMAT OPTIONS LOCK:
 *    Volume Label, File System, and Cluster Size start non-interactable.
 *    They become interactable only after an ISO file is selected.
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

    [Header("Format Options")]
    [Tooltip("Text input field for Volume Label — auto-filled with ISO filename on selection.")]
    [SerializeField] private TMP_InputField volumeLabelField;
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
    private const int CorrectPartitionScheme  = 1; // "GPT"
    private const int CorrectTargetSystem     = 1; // "UEFI (non CSM)"
    private const int CorrectFileSystem       = 1; // "NTFS"
    private const int CorrectClusterSize      = 3; // "4096 bytes (Default)"

    private bool _formatting;

    private void Awake()
    {
        PopulateDropdowns();
        SetFormatOptionsInteractable(false);

        if (volumeLabelField != null)
            volumeLabelField.text = string.Empty;

        if (filePickerPanel != null)
            filePickerPanel.SetActive(false);

        SetStatus("Ready");
    }

    // ----------------------------------------------------------------
    //  Dropdown population
    // ----------------------------------------------------------------

    private void PopulateDropdowns()
    {
        SetOptions(deviceDropdown, new[]
        {
            "Kingston DataTraveler 32GB (E:)"
        });

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
        if (volumeLabelField != null)    volumeLabelField.interactable    = state;
        if (fileSystemDropdown != null)  fileSystemDropdown.interactable  = state;
        if (clusterSizeDropdown != null) clusterSizeDropdown.interactable = state;
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

        if (volumeLabelField != null)
            volumeLabelField.text = isoName;

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
            hint = "Set Partition Scheme to 'GPT'.";
            return false;
        }
        if (!Check(targetSystemDropdown, CorrectTargetSystem))
        {
            hint = "Set Target System to 'UEFI (non CSM)'.";
            return false;
        }
        if (volumeLabelField == null || string.IsNullOrWhiteSpace(volumeLabelField.text))
        {
            hint = "Volume Label cannot be empty.";
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
