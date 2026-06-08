/*
 * ================================================================
 *  UNITY SETUP GUIDE — RufusSetupManager
 * ================================================================
 *  STEP 1 — Component placement
 *    Add this script to the RufusSetUp GameObject (the panel that
 *    overlays the Desktop when Rufus is opened).
 *
 *  STEP 2 — Wire Inspector fields
 *
 *    Dropdowns — Device Properties:
 *      deviceDropdown        → Device           (TMP_Dropdown)
 *      bootSelectionDropdown → Boot Selection   (TMP_Dropdown)
 *      imageOptionDropdown   → Image Option     (TMP_Dropdown)
 *      partitionSchemeDropdown → Partition Scheme (TMP_Dropdown)
 *      targetSystemDropdown  → Target System    (TMP_Dropdown)
 *
 *    Dropdowns — Format Options:
 *      volumeLabelDropdown   → Volume Label     (TMP_Dropdown)
 *      fileSystemDropdown    → File System      (TMP_Dropdown)
 *      clusterSizeDropdown   → Cluster Size     (TMP_Dropdown)
 *
 *    Status:
 *      statusText            → Text (Legacy) child of the "Ready" object
 *                              (the Text component, not the Ready button itself)
 *
 *    Navigator:
 *      navigator             → T2MonitorNavigator on the T2Monitor GameObject
 *
 *  STEP 3 — Rewire the Start button
 *    Remove the existing call:  T2MonitorNavigator → MarkRufusComplete()
 *    Add new call:              RufusSetupManager  → OnStartClicked()
 *
 *    Leave the CLOSE button wired to T2MonitorNavigator.CloseRufus() as-is.
 *    Leave the Ready object as-is (its Text child is used as the status bar).
 *
 *  CORRECT CONFIGURATION (for task completion):
 *    Boot Selection  → Disk or ISO image (Please select)
 *    Image Option    → Standard Windows Installation
 *    Partition Scheme→ GPT
 *    Target System   → UEFI (non CSM)
 *    Volume Label    → ESD-USB
 *    File System     → FAT32 (Default)
 *    Cluster Size    → 4096 bytes (Default)
 *    Device is auto-set (only one option) and always passes.
 *
 *  HOW VALIDATION WORKS:
 *    Clicking Start checks every dropdown.
 *    On mismatch: the Ready status bar shows a hint (e.g. "Set Partition
 *    Scheme to GPT") and the task does NOT complete.
 *    When all settings match: status bar shows "DONE" after a short
 *    formatting animation, then MarkRufusComplete() is called.
 * ================================================================
 */

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

    [Header("Status Bar")]
    [Tooltip("The Text (Legacy) component inside the 'Ready' button's child object.")]
    [SerializeField] private Text statusText;

    [Header("Navigator")]
    [SerializeField] private T2MonitorNavigator navigator;

    // Correct answer indices — matches the order options are added in PopulateDropdowns().
    private const int CorrectBootSelection    = 0; // "Disk or ISO image (Please select)"
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
    }

    // ----------------------------------------------------------------
    //  Dropdown population
    // ----------------------------------------------------------------

    private void PopulateDropdowns()
    {
        // Device — only one simulated USB drive; always pre-selected.
        SetOptions(deviceDropdown, new[]
        {
            "Kingston DataTraveler 32GB (E:)"
        });

        // Boot Selection
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
        if (!Check(bootSelectionDropdown, CorrectBootSelection))
        {
            hint = "Set Boot Selection to 'Disk or ISO image'.";
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
