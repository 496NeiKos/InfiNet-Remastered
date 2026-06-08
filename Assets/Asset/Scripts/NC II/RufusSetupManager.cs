/*
 * ================================================================
 *  UNITY SETUP GUIDE — RufusSetupManager
 * ================================================================
 *  STEP 1 — Component placement
 *    Add this script to the RufusSetUp GameObject.
 *
 *  STEP 2 — Convert the "Select" object to a Button
 *    The "Select" child of RufusSetUp is currently a TMP_Dropdown.
 *    In the Inspector:
 *      a) Remove the TMP_Dropdown component from "Select".
 *      b) Add a Button component.
 *      c) Wire its OnClick → RufusSetupManager.OpenFilePicker()
 *
 *  STEP 3 — Create the ISO file picker popup
 *    Inside RufusSetUp, create a new child panel (e.g. "IsoFilePicker").
 *    It starts INACTIVE. Layout suggestion:
 *
 *      IsoFilePicker (Panel, starts inactive)
 *        ├─ Title (Text — "Select ISO File")
 *        ├─ IsoButton_Win11 (Button)  → OnClick → SelectIso("Win11_23H2_English_x64v2.iso")
 *        ├─ IsoButton_Win10 (Button)  → OnClick → SelectIso("Win10_22H2_English_x64.iso")
 *        └─ CancelButton   (Button)  → OnClick → CloseFilePicker()
 *
 *    Each button passes its ISO filename as a string to SelectIso().
 *    In Unity's OnClick (String) event field you can type the string directly.
 *
 *  STEP 4 — Wire Inspector fields on RufusSetupManager
 *
 *    Dropdowns — Device Properties:
 *      deviceDropdown          → Device            (TMP_Dropdown)
 *      bootSelectionDropdown   → Boot Selection    (TMP_Dropdown)
 *      imageOptionDropdown     → Image Option      (TMP_Dropdown)
 *      partitionSchemeDropdown → Partition Scheme  (TMP_Dropdown)
 *      targetSystemDropdown    → Target System     (TMP_Dropdown)
 *
 *    Dropdowns — Format Options:
 *      volumeLabelDropdown     → Volume Label      (TMP_Dropdown)
 *      fileSystemDropdown      → File System       (TMP_Dropdown)
 *      clusterSizeDropdown     → Cluster Size      (TMP_Dropdown)
 *
 *    File Picker:
 *      filePickerPanel         → IsoFilePicker     (the panel created in STEP 3)
 *
 *    Status:
 *      statusText              → Text (Legacy) child of the "Ready" object
 *
 *    Navigator:
 *      navigator               → T2MonitorNavigator on the T2Monitor GameObject
 *
 *  STEP 5 — Rewire the Start button
 *    Remove:  T2MonitorNavigator → MarkRufusComplete()
 *    Add:     RufusSetupManager  → OnStartClicked()
 *
 *  CORRECT CONFIGURATION (for task completion):
 *    Boot Selection   → any .iso file selected via the file picker
 *    Image Option     → Standard Windows Installation
 *    Partition Scheme → GPT
 *    Target System    → UEFI (non CSM)
 *    Volume Label     → ESD-USB
 *    File System      → FAT32 (Default)
 *    Cluster Size     → 4096 bytes (Default)
 *    Device is always valid (only one option).
 *
 *  HOW VALIDATION WORKS:
 *    - Boot Selection passes only when an ISO was picked (option text ends in .iso).
 *    - All other dropdowns pass only when the correct index is selected.
 *    - On any mismatch the Ready status bar shows a hint; task does NOT complete.
 *    - When all pass: a formatting animation plays (Starting → Writing → DONE)
 *      then MarkRufusComplete() is called.
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

    [Header("Dropdowns — Format Options")]
    [SerializeField] private TMP_Dropdown volumeLabelDropdown;
    [SerializeField] private TMP_Dropdown fileSystemDropdown;
    [SerializeField] private TMP_Dropdown clusterSizeDropdown;

    [Header("ISO File Picker")]
    [Tooltip("The popup panel that lists ISO files. Starts inactive.")]
    [SerializeField] private GameObject filePickerPanel;

    [Header("Status Bar")]
    [Tooltip("Text (Legacy) component inside the 'Ready' button's child object.")]
    [SerializeField] private Text statusText;

    [Header("Navigator")]
    [SerializeField] private T2MonitorNavigator navigator;

    // Correct answer indices for non-Boot-Selection dropdowns.
    private const int CorrectImageOption      = 0; // "Standard Windows Installation"
    private const int CorrectPartitionScheme  = 1; // "GPT"
    private const int CorrectTargetSystem     = 1; // "UEFI (non CSM)"
    private const int CorrectVolumeLabel      = 0; // "ESD-USB"
    private const int CorrectFileSystem       = 0; // "FAT32 (Default)"
    private const int CorrectClusterSize      = 3; // "4096 bytes (Default)"

    private bool _formatting;

    private void Awake()
    {
        PopulateDropdowns();
        SetStatus("Ready");

        if (filePickerPanel != null)
            filePickerPanel.SetActive(false);
    }

    // ----------------------------------------------------------------
    //  Dropdown population
    // ----------------------------------------------------------------

    private void PopulateDropdowns()
    {
        // Device — single simulated USB drive; always valid.
        SetOptions(deviceDropdown, new[]
        {
            "Kingston DataTraveler 32GB (E:)"
        });

        // Boot Selection — placeholder until the user picks an ISO via the file picker.
        SetOptions(bootSelectionDropdown, new[]
        {
            "Disk or ISO image (Please select)",
            "FreeDOS",
            "Non bootable"
        });

        // Image Option
        SetOptions(imageOptionDropdown, new[]
        {
            "Standard Windows Installation",
            "Windows To Go"
        });

        // Partition Scheme
        SetOptions(partitionSchemeDropdown, new[]
        {
            "MBR",
            "GPT"
        });

        // Target System
        SetOptions(targetSystemDropdown, new[]
        {
            "BIOS (or UEFI-CSM)",
            "UEFI (non CSM)"
        });

        // Volume Label
        SetOptions(volumeLabelDropdown, new[]
        {
            "ESD-USB",
            "WINDOWS",
            "BOOT",
            "USB_DRIVE"
        });

        // File System
        SetOptions(fileSystemDropdown, new[]
        {
            "FAT32 (Default)",
            "NTFS",
            "UDF",
            "exFAT"
        });

        // Cluster Size
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

    // Wired to each ISO button inside IsoFilePicker with the filename as the string argument.
    // e.g. OnClick → SelectIso("Win11_23H2_English_x64v2.iso")
    public void SelectIso(string isoName)
    {
        if (bootSelectionDropdown != null && bootSelectionDropdown.options.Count > 0)
        {
            bootSelectionDropdown.options[0] = new TMP_Dropdown.OptionData(isoName);
            bootSelectionDropdown.value = 0;
            bootSelectionDropdown.RefreshShownValue();
        }

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
        // Boot Selection: valid only when an ISO file has been chosen (text ends in .iso).
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
        if (!Check(volumeLabelDropdown, CorrectVolumeLabel))
        {
            hint = "Set Volume Label to 'ESD-USB'.";
            return false;
        }
        if (!Check(fileSystemDropdown, CorrectFileSystem))
        {
            hint = "Set File System to 'FAT32 (Default)'.";
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
