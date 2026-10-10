/*
 * ================================================================
 *  UNITY SETUP GUIDE — DiskCleanupController (COC IV)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Disk Cleanup Chain" GameObject (starts INACTIVE).
 *    Place as a DIRECT CHILD of App Panels so CloseAllApps() covers it.
 *
 *  ── FULL HIERARCHY ──────────────────────────────────────────────
 *
 *    Disk Cleanup Chain                  ← this script (starts INACTIVE)
 *      │
 *      ├── Panel 1 — Drive Selection     → driveSelectionPanel (INACTIVE)
 *      │     ├── Title Bar
 *      │     │     ├── TitleTMP          (static: "Disk Cleanup : Drive Selection")
 *      │     │     └── ExitBtn           → panel1ExitBtn
 *      │     ├── DescriptionTMP          (static: "Select the drive you want to clean up.")
 *      │     ├── DriveDropdown           → driveDropdown  (TMP_Dropdown)
 *      │     └── Footer
 *      │           ├── OKBtn             → panel1OKBtn
 *      │           └── ExitBtn           → panel1CancelBtn   (label: "Exit")
 *      │
 *      ├── Panel 2 — Calculating         → calculatingPanel (INACTIVE)
 *      │     ├── StatusLabelTMP          → calculatingLabelTMP  (dynamic)
 *      │     ├── ProgressSlider          → calculatingSlider    (Interactable: false, Min:0 Max:1)
 *      │     ├── DetailTMP               → calculatingDetailTMP (dynamic)
 *      │     └── CancelBtn              → panel2CancelBtn
 *      │
 *      ├── Panel 3 — File List           → fileListPanel (INACTIVE)
 *      │     ├── Tab Bar
 *      │     │     └── DiskCleanupTab    (static label: "Disk Cleanup" — purely visual)
 *      │     ├── TitleTMP                → fileListTitleTMP  (dynamic: "Disk Cleanup for (C:)")
 *      │     ├── SpaceDescTMP            → spaceDescTMP      (dynamic)
 *      │     ├── Scroll View
 *      │     │     └── Content           → fileListContent   (Transform)
 *      │     ├── TotalSizeTMP            → totalSizeTMP      (dynamic, e.g. "262.4 MB")
 *      │     ├── FileDescriptionTMP      → fileDescriptionTMP (dynamic — updates on row click)
 *      │     ├── CleanupSystemFilesBtn   → cleanupSystemFilesBtn
 *      │     ├── ViewFilesBtn            → viewFilesBtn      (Interactable: false — disabled)
 *      │     └── Footer
 *      │           ├── OKBtn             → panel3OKBtn
 *      │           └── CancelBtn         → panel3CancelBtn
 *      │
 *      └── Panel 4 — Confirmation        → confirmationPanel (INACTIVE)
 *            ├── ConfirmLabelTMP         (static: "Are you sure you want to permanently delete these files?")
 *            ├── DeleteFilesBtn          → deleteFilesBtn
 *            └── CancelBtn              → panel4CancelBtn
 *
 *  ── PREFAB ──────────────────────────────────────────────────────
 *    Create a prefab using DiskCleanupFileRowUI.cs.
 *    Assign to: fileRowPrefab
 *    Assign any icon sprite to: defaultFileIcon
 *      (one icon is used for all file categories — swap per row later if desired)
 *
 *  ── TIMING ──────────────────────────────────────────────────────
 *    analysisDuration    default 4s   — how long the calculating slider takes
 *    deletionDuration    default 5s   — how long the deletion progress takes
 *    completionHoldTime  default 3s   — how long "Disk Cleanup complete." shows before closing
 *    All three are SerializeFields — adjust in the Inspector.
 *
 *  ── INSPECTOR ASSIGNMENTS ───────────────────────────────────────
 *    All fields listed in the hierarchy above, plus fileRowPrefab and defaultFileIcon.
 *
 *  ── WIRING ──────────────────────────────────────────────────────
 *    SearchWindowController.diskCleanupBtn OnClick
 *      → handled in SearchWindowController.OpenDiskCleanup()
 *    Set diskCleanupBtn.interactable = true in the Inspector (it was false).
 *
 *  ── PANEL FLOW ──────────────────────────────────────────────────
 *    Open → Panel 1
 *    Panel 1 OK → Panel 2 (Analyzing)
 *    Panel 2 complete → Panel 3 (pass 1: 8 user-file rows, "Clean up system files" visible)
 *    Panel 3 "Clean up system files" → Panel 1 → Panel 2 (AnalyzingSystem)
 *    Panel 2 complete → Panel 3 (pass 2: 13 rows, "Clean up system files" hidden)
 *    Panel 3 OK (with checked items) → Panel 4
 *    Panel 4 "Delete Files" → Panel 2 (Deleting, Cancel hidden)
 *    Panel 2 complete → brief completion state → CloseChain
 * ================================================================
 */

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiskCleanupController : MonoBehaviour
{
    // ── Enums ─────────────────────────────────────────────────────────────────

    private enum CleanupMode { Idle, Analyzing, AnalyzingSystem, Deleting }

    // ── Timing ────────────────────────────────────────────────────────────────

    [Header("Timing")]
    [SerializeField] private float analysisDuration   = 4f;
    [SerializeField] private float deletionDuration   = 5f;
    [SerializeField] private float completionHoldTime = 3f;

    // ── Panel 1 — Drive Selection ─────────────────────────────────────────────

    [Header("Panel 1 — Drive Selection")]
    [SerializeField] private GameObject  driveSelectionPanel;
    [SerializeField] private TMP_Dropdown driveDropdown;
    [SerializeField] private Button      panel1ExitBtn;
    [SerializeField] private Button      panel1OKBtn;
    [SerializeField] private Button      panel1CancelBtn;

    // ── Panel 2 — Calculating ─────────────────────────────────────────────────

    [Header("Panel 2 — Calculating")]
    [SerializeField] private GameObject calculatingPanel;
    [SerializeField] private TMP_Text   calculatingLabelTMP;
    [SerializeField] private Slider     calculatingSlider;
    [SerializeField] private TMP_Text   calculatingDetailTMP;
    [SerializeField] private Button     panel2CancelBtn;

    // ── Panel 3 — File List ───────────────────────────────────────────────────

    [Header("Panel 3 — File List")]
    [SerializeField] private GameObject fileListPanel;
    [SerializeField] private TMP_Text   fileListTitleTMP;
    [SerializeField] private TMP_Text   spaceDescTMP;
    [SerializeField] private Transform  fileListContent;
    [SerializeField] private TMP_Text   totalSizeTMP;
    [SerializeField] private TMP_Text   fileDescriptionTMP;
    [SerializeField] private Button     cleanupSystemFilesBtn;
    [SerializeField] private Button     viewFilesBtn;
    [SerializeField] private Button     panel3OKBtn;
    [SerializeField] private Button     panel3CancelBtn;

    // ── Panel 4 — Confirmation ────────────────────────────────────────────────

    [Header("Panel 4 — Confirmation")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private Button     deleteFilesBtn;
    [SerializeField] private Button     panel4CancelBtn;

    // ── Prefab ────────────────────────────────────────────────────────────────

    [Header("Prefab")]
    [SerializeField] private GameObject fileRowPrefab;
    [SerializeField] private Sprite     defaultFileIcon;

    // ── Static File Data ──────────────────────────────────────────────────────

    private static readonly DiskCleanupFileEntry[] UserFileEntries =
    {
        new DiskCleanupFileEntry
        {
            Name           = "Downloaded Program Files",
            SizeMB         = 0f,
            DefaultChecked = true,
            IsSystemFile   = false,
            Description    = "Downloaded Program Files are ActiveX controls and Java applets downloaded automatically from the Internet when you view certain pages. They are temporarily stored in the Downloaded Program Files folder on your hard disk."
        },
        new DiskCleanupFileEntry
        {
            Name           = "Temporary Internet Files",
            SizeMB         = 1.23f,
            DefaultChecked = true,
            IsSystemFile   = false,
            Description    = "The Temporary Internet Files folder contains webpages stored on your hard disk for quick viewing. Your personalized settings for webpages will be left intact."
        },
        new DiskCleanupFileEntry
        {
            Name           = "Windows Error Reporting Files",
            SizeMB         = 0.5f,
            DefaultChecked = false,
            IsSystemFile   = false,
            Description    = "Files created by Windows error reporting that have not been sent to Microsoft. These files may help diagnose problems, but they can be deleted safely."
        },
        new DiskCleanupFileEntry
        {
            Name           = "DirectX Shader Cache",
            SizeMB         = 215.4f,
            DefaultChecked = true,
            IsSystemFile   = false,
            Description    = "Images created by the graphics system which speed up application load times and improve responsiveness. They will be automatically regenerated as needed."
        },
        new DiskCleanupFileEntry
        {
            Name           = "Delivery Optimization Files",
            SizeMB         = 0f,
            DefaultChecked = false,
            IsSystemFile   = false,
            Description    = "Files previously downloaded that can be used to provide updates to other PCs on your local network or the Internet. They will be deleted if not used for some time."
        },
        new DiskCleanupFileEntry
        {
            Name           = "Recycle Bin",
            SizeMB         = 0f,
            DefaultChecked = true,
            IsSystemFile   = false,
            Description    = "The Recycle Bin contains files you have deleted from your computer. These files are not permanently removed until you empty the Recycle Bin."
        },
        new DiskCleanupFileEntry
        {
            Name           = "Temporary Files",
            SizeMB         = 45.7f,
            DefaultChecked = true,
            IsSystemFile   = false,
            Description    = "Programs sometimes store temporary information in a TEMP folder. Before a program closes, it usually deletes this information. You can safely delete temporary files that have not been modified in over a week."
        },
        new DiskCleanupFileEntry
        {
            Name           = "Thumbnails",
            SizeMB         = 12.4f,
            DefaultChecked = false,
            IsSystemFile   = false,
            Description    = "Windows keeps a copy of all your picture, video, and document thumbnails so they can be displayed quickly when you open a folder. If deleted, they will be automatically recreated as needed."
        },
    };

    private static readonly DiskCleanupFileEntry[] SystemFileEntries =
    {
        new DiskCleanupFileEntry
        {
            Name           = "Windows Update Cleanup",
            SizeMB         = 1259f,
            DefaultChecked = false,
            IsSystemFile   = true,
            Description    = "After you install Windows updates, older versions may be kept on your computer. If updates ran correctly, you may not need the older versions. They will be permanently removed."
        },
        new DiskCleanupFileEntry
        {
            Name           = "Microsoft Defender Antivirus",
            SizeMB         = 278f,
            DefaultChecked = false,
            IsSystemFile   = true,
            Description    = "Files used by Windows Defender to protect your system. These are non-critical log and definition backup files that can be safely removed."
        },
        new DiskCleanupFileEntry
        {
            Name           = "System Error Memory Dump Files",
            SizeMB         = 256f,
            DefaultChecked = false,
            IsSystemFile   = true,
            Description    = "Memory dump files are created when Windows stops unexpectedly. They may help diagnose the problem but can be deleted once the issue is resolved."
        },
        new DiskCleanupFileEntry
        {
            Name           = "Windows Upgrade Log Files",
            SizeMB         = 45.8f,
            DefaultChecked = false,
            IsSystemFile   = true,
            Description    = "Files used by Windows to record details of upgrade processes. These are used to diagnose Windows Update upgrade failures and can be removed safely."
        },
        new DiskCleanupFileEntry
        {
            Name           = "Previous Windows Installation(s)",
            SizeMB         = 4761f,
            DefaultChecked = false,
            IsSystemFile   = true,
            Description    = "Files left after upgrading from a previous version of Windows. If your upgrade was successful, these files can be permanently deleted to free up disk space."
        },
    };

    private static readonly string[] DriveDisplayOptions = { "(C:) Windows", "(D:) New Volume", "(E:) Backup" };
    private static readonly string[] DriveCodes          = { "C:",            "D:",              "E:"          };

    // ── Runtime State ─────────────────────────────────────────────────────────

    private CleanupMode _mode               = CleanupMode.Idle;
    private string      _selectedDrive      = "C:";
    private float       _calculatedSpaceMB  = 0f;
    private bool        _systemFilesIncluded = false;

    private readonly List<DiskCleanupFileRowUI> _spawnedRows = new List<DiskCleanupFileRowUI>();
    private Coroutine _progressCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Panel 1
        panel1ExitBtn?.onClick.AddListener(CloseChain);
        panel1CancelBtn?.onClick.AddListener(CloseChain);
        panel1OKBtn?.onClick.AddListener(OnPanel1OK);
        driveDropdown?.onValueChanged.AddListener(i => _selectedDrive = DriveCodes[i]);

        // Panel 2
        panel2CancelBtn?.onClick.AddListener(OnPanel2Cancel);

        // Panel 3
        cleanupSystemFilesBtn?.onClick.AddListener(OnCleanupSystemFiles);
        SetInteractable(viewFilesBtn, false);
        panel3OKBtn?.onClick.AddListener(OnPanel3OK);
        panel3CancelBtn?.onClick.AddListener(CloseChain);

        // Panel 4
        deleteFilesBtn?.onClick.AddListener(OnDeleteFiles);
        panel4CancelBtn?.onClick.AddListener(OnPanel4Cancel);

        // Slider is visual only
        if (calculatingSlider != null) calculatingSlider.interactable = false;

        HideAll();
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        _mode                = CleanupMode.Idle;
        _systemFilesIncluded = false;
        HideAll();
        InitDriveDropdown();
        driveSelectionPanel?.SetActive(true);
        ActivityLogManager.Log("Opened Disk Cleanup", ActivityLogManager.EntryType.Action);
    }

    // ── Panel 1 — Drive Selection ─────────────────────────────────────────────

    private void InitDriveDropdown()
    {
        if (driveDropdown == null) return;
        driveDropdown.ClearOptions();
        driveDropdown.AddOptions(new List<string>(DriveDisplayOptions));
        driveDropdown.SetValueWithoutNotify(0);
        _selectedDrive = DriveCodes[0];
    }

    private void OnPanel1OK()
    {
        _mode = _systemFilesIncluded ? CleanupMode.AnalyzingSystem : CleanupMode.Analyzing;
        driveSelectionPanel?.SetActive(false);
        ShowCalculating();
    }

    // ── Panel 2 — Calculating ─────────────────────────────────────────────────

    private void ShowCalculating()
    {
        bool isDeletion = _mode == CleanupMode.Deleting;

        if (calculatingLabelTMP != null)
        {
            calculatingLabelTMP.text = isDeletion
                ? $"Cleaning up ({_selectedDrive})..."
                : $"Disk Cleanup is calculating how much space you will be able to free on ({_selectedDrive}). This may take a few minutes to complete.";
        }

        // Cancel is hidden during deletion (irreversible)
        if (panel2CancelBtn != null)
            panel2CancelBtn.gameObject.SetActive(!isDeletion);

        if (calculatingSlider != null)  calculatingSlider.value = 0f;
        if (calculatingDetailTMP != null) calculatingDetailTMP.text = "";

        calculatingPanel?.SetActive(true);
        _progressCoroutine = StartCoroutine(RunProgress());
    }

    private IEnumerator RunProgress()
    {
        float    duration = _mode == CleanupMode.Deleting ? deletionDuration : analysisDuration;
        string[] messages = GetScanMessages();
        float    elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            if (calculatingSlider != null) calculatingSlider.value = progress;

            int msgIndex = Mathf.Min(
                Mathf.FloorToInt(progress * messages.Length),
                messages.Length - 1);
            if (calculatingDetailTMP != null) calculatingDetailTMP.text = messages[msgIndex];

            yield return null;
        }

        _progressCoroutine = null;
        OnProgressComplete();
    }

    private void OnProgressComplete()
    {
        calculatingPanel?.SetActive(false);

        switch (_mode)
        {
            case CleanupMode.Analyzing:
            case CleanupMode.AnalyzingSystem:
                _calculatedSpaceMB = CalculateDefaultCheckedSize();
                ShowFileList();
                break;

            case CleanupMode.Deleting:
                StartCoroutine(ShowCompletion());
                break;
        }
    }

    private void OnPanel2Cancel()
    {
        if (_progressCoroutine != null)
        {
            StopCoroutine(_progressCoroutine);
            _progressCoroutine = null;
        }
        calculatingPanel?.SetActive(false);
        driveSelectionPanel?.SetActive(true);
        _mode = CleanupMode.Idle;
    }

    private IEnumerator ShowCompletion()
    {
        // Reuse Panel 2 to show the completion state
        calculatingPanel?.SetActive(true);
        if (calculatingLabelTMP  != null) calculatingLabelTMP.text  = "Disk Cleanup complete.";
        if (calculatingDetailTMP != null) calculatingDetailTMP.text = "";
        if (calculatingSlider    != null) calculatingSlider.value   = 1f;

        ActivityLogManager.Log($"Disk Cleanup completed on ({_selectedDrive}).", ActivityLogManager.EntryType.Action);

        yield return new WaitForSeconds(completionHoldTime);

        CloseChain();
    }

    // ── Panel 3 — File List ───────────────────────────────────────────────────

    private void ShowFileList()
    {
        ClearFileRows();

        var entries = new List<DiskCleanupFileEntry>(UserFileEntries);
        if (_systemFilesIncluded) entries.AddRange(SystemFileEntries);

        foreach (DiskCleanupFileEntry entry in entries)
        {
            GameObject rowGO = Instantiate(fileRowPrefab, fileListContent);
            DiskCleanupFileRowUI row = rowGO.GetComponent<DiskCleanupFileRowUI>();
            if (row != null)
            {
                row.Setup(entry, defaultFileIcon, OnRowToggleChanged, OnRowSelected);
                _spawnedRows.Add(row);
            }
        }

        if (fileListTitleTMP != null)
            fileListTitleTMP.text = $"Disk Cleanup for ({_selectedDrive})";

        UpdateSpaceDesc();
        UpdateTotalSize();

        // "Clean up system files" is removed once system files are included
        if (cleanupSystemFilesBtn != null)
            cleanupSystemFilesBtn.gameObject.SetActive(!_systemFilesIncluded);

        // Show description of first row by default
        if (_spawnedRows.Count > 0 && fileDescriptionTMP != null)
            fileDescriptionTMP.text = _spawnedRows[0].Entry.Description;

        fileListPanel?.SetActive(true);
    }

    private void OnCleanupSystemFiles()
    {
        _systemFilesIncluded = true;
        ClearFileRows();
        fileListPanel?.SetActive(false);
        // Re-run the drive selection → analysis flow with system files included
        InitDriveDropdown();
        driveSelectionPanel?.SetActive(true);
        ActivityLogManager.Log("Clean up system files selected — re-running analysis.", ActivityLogManager.EntryType.Action);
    }

    private void OnPanel3OK()
    {
        float total = GetCheckedTotal();
        if (total <= 0f)
        {
            // Nothing selected — close without doing anything (accurate to real Windows)
            CloseChain();
            return;
        }
        fileListPanel?.SetActive(false);
        confirmationPanel?.SetActive(true);
    }

    private void OnRowToggleChanged(DiskCleanupFileRowUI row)
    {
        UpdateTotalSize();
    }

    private void OnRowSelected(DiskCleanupFileRowUI row)
    {
        if (fileDescriptionTMP != null && row?.Entry != null)
            fileDescriptionTMP.text = row.Entry.Description;
    }

    private void UpdateSpaceDesc()
    {
        if (spaceDescTMP == null) return;
        spaceDescTMP.text = $"You can use Disk Cleanup to free up {FormatSize(_calculatedSpaceMB)} of disk space on ({_selectedDrive}).";
    }

    private void UpdateTotalSize()
    {
        if (totalSizeTMP == null) return;
        totalSizeTMP.text = FormatSize(GetCheckedTotal());
    }

    // ── Panel 4 — Confirmation ────────────────────────────────────────────────

    private void OnDeleteFiles()
    {
        _mode = CleanupMode.Deleting;
        confirmationPanel?.SetActive(false);
        ShowCalculating();
        ActivityLogManager.Log($"Deleting files on ({_selectedDrive})...", ActivityLogManager.EntryType.Action);
    }

    private void OnPanel4Cancel()
    {
        confirmationPanel?.SetActive(false);
        // Rows are still alive — just re-show Panel 3
        fileListPanel?.SetActive(true);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private float CalculateDefaultCheckedSize()
    {
        float total = 0f;
        foreach (DiskCleanupFileEntry e in UserFileEntries)
            if (e.DefaultChecked) total += e.SizeMB;
        if (_systemFilesIncluded)
            foreach (DiskCleanupFileEntry e in SystemFileEntries)
                if (e.DefaultChecked) total += e.SizeMB;
        return total;
    }

    private float GetCheckedTotal()
    {
        float total = 0f;
        foreach (DiskCleanupFileRowUI row in _spawnedRows)
            if (row != null && row.IsChecked) total += row.Entry.SizeMB;
        return total;
    }

    private string[] GetScanMessages()
    {
        if (_mode == CleanupMode.Deleting)
        {
            var msgs = new List<string>();
            foreach (DiskCleanupFileRowUI row in _spawnedRows)
                if (row != null && row.IsChecked) msgs.Add($"Cleaning up: {row.Entry.Name}...");
            msgs.Add("Finalizing...");
            return msgs.ToArray();
        }

        if (_mode == CleanupMode.AnalyzingSystem)
        {
            return new[]
            {
                "Scanning: Downloaded Program Files...",
                "Scanning: Temporary Internet Files...",
                "Scanning: Windows Error Reporting Files...",
                "Scanning: DirectX Shader Cache...",
                "Scanning: Delivery Optimization Files...",
                "Scanning: Recycle Bin...",
                "Scanning: Temporary Files...",
                "Scanning: Thumbnails...",
                "Scanning: Windows Update Cleanup...",
                "Scanning: Microsoft Defender Antivirus...",
                "Scanning: System Error Memory Dump Files...",
                "Scanning: Windows Upgrade Log Files...",
                "Scanning: Previous Windows Installation(s)...",
                "Calculating total disk space...",
            };
        }

        // CleanupMode.Analyzing
        return new[]
        {
            "Scanning: Downloaded Program Files...",
            "Scanning: Temporary Internet Files...",
            "Scanning: Windows Error Reporting Files...",
            "Scanning: DirectX Shader Cache...",
            "Scanning: Delivery Optimization Files...",
            "Scanning: Recycle Bin...",
            "Scanning: Temporary Files...",
            "Scanning: Thumbnails...",
            "Calculating total disk space...",
        };
    }

    private void ClearFileRows()
    {
        foreach (DiskCleanupFileRowUI row in _spawnedRows)
            if (row != null) Destroy(row.gameObject);
        _spawnedRows.Clear();
    }

    private void HideAll()
    {
        driveSelectionPanel?.SetActive(false);
        calculatingPanel?.SetActive(false);
        fileListPanel?.SetActive(false);
        confirmationPanel?.SetActive(false);
        ClearFileRows();
    }

    private void CloseChain()
    {
        if (_progressCoroutine != null)
        {
            StopCoroutine(_progressCoroutine);
            _progressCoroutine = null;
        }
        HideAll();
        _mode                = CleanupMode.Idle;
        _systemFilesIncluded = false;
        gameObject.SetActive(false);
    }

    private static void SetInteractable(Button btn, bool interactable)
    {
        if (btn != null) btn.interactable = interactable;
    }

    private static string FormatSize(float mb)
    {
        if (mb == 0f)    return "0 KB";
        if (mb < 1f)     return $"{mb * 1024f:F0} KB";
        if (mb >= 1024f) return $"{mb / 1024f:F2} GB";
        return $"{mb:F2} MB";
    }
}
