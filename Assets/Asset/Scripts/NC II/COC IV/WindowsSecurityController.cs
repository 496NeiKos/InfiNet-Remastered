/*
 * ================================================================
 *  UNITY SETUP GUIDE — WindowsSecurityController (COC IV)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Windows Security Chain" GameObject (starts INACTIVE).
 *    Place as a DIRECT CHILD of App Panels so CloseAllApps() covers it.
 *
 *  ── FULL HIERARCHY ──────────────────────────────────────────────
 *
 *    Windows Security Chain               ← this script (starts INACTIVE)
 *      │
 *      ├── Windows Security Panel         → windowsSecurityPanel (INACTIVE)
 *      │     ├── Title Bar
 *      │     │     ├── TitleTMP           (static: "Windows Security")
 *      │     │     └── CloseBtn           → closeBtn
 *      │     ├── Nav Panel
 *      │     │     ├── Home Btn           → navButtons[0]   label: "Home"
 *      │     │     │     └── NavHighlight  child Image GO, starts ACTIVE (default selected)
 *      │     │     ├── Virus Btn          → navButtons[1]   label: "Virus & threat protection"
 *      │     │     │     └── NavHighlight  child Image GO, starts INACTIVE
 *      │     │     ├── Account Btn        → navButtons[2]   label: "Account protection"
 *      │     │     │     └── NavHighlight  child Image GO, starts INACTIVE
 *      │     │     ├── Firewall Btn       → navButtons[3]   label: "Firewall & network protection"
 *      │     │     │     └── NavHighlight  child Image GO, starts INACTIVE
 *      │     │     ├── AppBrowser Btn     → navButtons[4]   label: "App & browser control"
 *      │     │     │     └── NavHighlight  child Image GO, starts INACTIVE
 *      │     │     ├── DeviceSec Btn      → navButtons[5]   label: "Device security"
 *      │     │     │     └── NavHighlight  child Image GO, starts INACTIVE
 *      │     │     ├── DevicePerf Btn     → navButtons[6]   label: "Device performance & health"
 *      │     │     │     └── NavHighlight  child Image GO, starts INACTIVE
 *      │     │     └── FamilyOpt Btn      → navButtons[7]   label: "Family options"
 *      │     │           └── NavHighlight  child Image GO, starts INACTIVE
 *      │     └── Content Panel
 *      │           ├── Placeholder Panel  → placeholderContentPanel (INACTIVE)
 *      │           │     └── LabelTMP     → placeholderLabelTMP  (dynamic section name)
 *      │           └── VTP Panel          → vtpContentPanel (INACTIVE)
 *      │                 ├── CurrentThreatsHeaderTMP  (static: "Current threats")
 *      │                 ├── DynamicTMP               → vtpDynamicTMP  (Overflow: Overflow)
 *      │                 ├── ScanSlider               → scanSlider
 *      │                 │     Interactable: false  Min: 0  Max: 1  starts INACTIVE
 *      │                 └── QuickScanBtn             → quickScanBtn
 *      │
 *      └── Toast Panel                    ← WindowsSecurityToastController lives here
 *            MUST be the LAST CHILD of Windows Security Chain (renders on top).
 *            See WindowsSecurityToastController setup guide.
 *
 *  ── NAV HIGHLIGHT ───────────────────────────────────────────────
 *    Each nav button has a child GO named "NavHighlight" with an Image component.
 *    Recommended color: white at 15–20% alpha (subtle selection tint).
 *    Home's NavHighlight starts ACTIVE; all others start INACTIVE.
 *    Drag all 8 NavHighlight GOs into navHighlights[] in the same order
 *    as navButtons[] (index 0 = Home … index 7 = Family options).
 *
 *  ── VTP DYNAMIC TMP ─────────────────────────────────────────────
 *    Set Overflow mode to "Overflow" so all lines are always visible.
 *    The TMP switches between three states:
 *      Idle/Complete (5 lines):
 *        No current threats.
 *        Last scan: M/D/YYYY H:MM AM/PM (Quick scan)
 *        0 threats found.
 *        Scan lasted X minute(s) Y second(s).
 *        X,XXX files scanned.
 *      Scanning (3 lines):
 *        Quick scan running.
 *        Elapsed: MM:SS
 *        X,XXX files scanned.
 *
 *  ── TIMING ──────────────────────────────────────────────────────
 *    scanDuration    default 8s — slider fill duration; adjust in Inspector.
 *
 *  ── INSPECTOR ASSIGNMENTS ───────────────────────────────────────
 *    windowsSecurityPanel     The main app window GO
 *    closeBtn                 Title bar close button
 *    navButtons               Array of 8 nav Buttons (Home → Family options)
 *    navHighlights            Array of 8 NavHighlight GOs (same order)
 *    placeholderContentPanel  Content panel for non-VTP nav items
 *    placeholderLabelTMP      TMP label inside placeholderContentPanel
 *    vtpContentPanel          Content panel for Virus & threat protection
 *    vtpDynamicTMP            Dynamic TMP inside vtpContentPanel
 *    scanSlider               Slider inside vtpContentPanel
 *    quickScanBtn             Button inside vtpContentPanel
 *    toastController          WindowsSecurityToastController on Toast Panel GO
 *
 *  ── WIRING ──────────────────────────────────────────────────────
 *    SearchWindowController.windowsSecurityBtn OnClick
 *      → handled in SearchWindowController.OpenWindowsSecurity()
 *    Set windowsSecurityBtn.interactable = true in the Inspector.
 *
 *  ── PANEL FLOW ──────────────────────────────────────────────────
 *    Open → Home selected → Placeholder ("Home") shown
 *    Click "Virus & threat protection" → VTP panel shown, Idle state
 *    Click "Quick Scan" → Scanning: slider runs, elapsed/files update live
 *    Click "Cancel" → Idle: slider resets, pre-scan values restored
 *    Scan completes → Complete: post-scan TMP shown, toast slides in
 *    Click "Quick Scan" again → new scan with new random target file count
 * ================================================================
 */

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WindowsSecurityController : MonoBehaviour
{
    // ── Enums ─────────────────────────────────────────────────────────────────

    private enum ScanState { Idle, Scanning, Complete }

    // ── Data ──────────────────────────────────────────────────────────────────

    private struct ScanResultData
    {
        public string DateTimeStr;
        public int    ThreatsFound;
        public int    MinutesElapsed;
        public int    SecondsElapsed;
        public int    FilesScanned;
    }

    // ── Timing ────────────────────────────────────────────────────────────────

    [Header("Timing")]
    [SerializeField] private float scanDuration = 8f;

    // ── Windows Security Panel ────────────────────────────────────────────────

    [Header("Windows Security Panel")]
    [SerializeField] private GameObject windowsSecurityPanel;
    [SerializeField] private Button     closeBtn;

    // ── Nav ───────────────────────────────────────────────────────────────────

    [Header("Nav")]
    [SerializeField] private Button[]     navButtons;
    [SerializeField] private GameObject[] navHighlights;

    // ── Content Panels ────────────────────────────────────────────────────────

    [Header("Content Panels")]
    [SerializeField] private GameObject placeholderContentPanel;
    [SerializeField] private TMP_Text   placeholderLabelTMP;
    [SerializeField] private GameObject vtpContentPanel;

    // ── VTP Elements ──────────────────────────────────────────────────────────

    [Header("VTP — Virus & Threat Protection")]
    [SerializeField] private TMP_Text vtpDynamicTMP;
    [SerializeField] private Slider   scanSlider;
    [SerializeField] private Button   quickScanBtn;

    // ── Toast ─────────────────────────────────────────────────────────────────

    [Header("Toast")]
    [SerializeField] private WindowsSecurityToastController toastController;

    // ── Nav Labels ────────────────────────────────────────────────────────────

    private static readonly string[] NavLabels =
    {
        "Home",
        "Virus & threat protection",    // index 1 — uses vtpContentPanel
        "Account protection",
        "Firewall & network protection",
        "App & browser control",
        "Device security",
        "Device performance & health",
        "Family options",
    };

    private const int VtpNavIndex = 1;

    // ── Runtime State ─────────────────────────────────────────────────────────

    private ScanState      _scanState;
    private int            _selectedNav;
    private ScanResultData _lastScanResult;
    private int            _scanTargetFiles;
    private int            _currentFiles;
    private Coroutine      _scanCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        closeBtn?.onClick.AddListener(CloseChain);

        for (int i = 0; i < navButtons.Length; i++)
        {
            int index = i;
            navButtons[i]?.onClick.AddListener(() => SelectNav(index));
        }

        quickScanBtn?.onClick.AddListener(OnQuickScanClick);

        if (scanSlider != null)
        {
            scanSlider.interactable = false;
            scanSlider.gameObject.SetActive(false);
        }

        HideAll();
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        _scanState      = ScanState.Idle;
        _lastScanResult = GenerateRandomScanResult();

        windowsSecurityPanel?.SetActive(true);
        SelectNav(0);

        ActivityLogManager.Log("Opened Windows Security", ActivityLogManager.EntryType.Action);
    }

    // ── Nav ───────────────────────────────────────────────────────────────────

    private void SelectNav(int index)
    {
        _selectedNav = index;

        for (int i = 0; i < navHighlights.Length; i++)
            if (navHighlights[i] != null)
                navHighlights[i].SetActive(i == index);

        if (index == VtpNavIndex)
        {
            placeholderContentPanel?.SetActive(false);
            vtpContentPanel?.SetActive(true);
            if (scanSlider != null)
                scanSlider.gameObject.SetActive(_scanState == ScanState.Scanning);
            RefreshVtpTMP();
        }
        else
        {
            vtpContentPanel?.SetActive(false);
            placeholderContentPanel?.SetActive(true);
            if (placeholderLabelTMP != null)
                placeholderLabelTMP.text = NavLabels[index];
        }
    }

    // ── VTP TMP ───────────────────────────────────────────────────────────────

    private void RefreshVtpTMP()
    {
        if (vtpDynamicTMP == null || _scanState == ScanState.Scanning) return;
        vtpDynamicTMP.text = BuildResultTMP(ref _lastScanResult);
    }

    private string BuildResultTMP(ref ScanResultData data)
    {
        return "No current threats.\n" +
               $"Last scan: {data.DateTimeStr} (Quick scan)\n" +
               $"{data.ThreatsFound} threats found.\n" +
               BuildDurationStr(data.MinutesElapsed, data.SecondsElapsed) + "\n" +
               $"{data.FilesScanned:N0} files scanned.";
    }

    private static string BuildDurationStr(int mm, int ss)
    {
        string sec = $"{ss} second{(ss == 1 ? "" : "s")}";
        if (mm == 0)
            return $"Scan lasted {sec}.";
        string min = $"{mm} minute{(mm == 1 ? "" : "s")}";
        return $"Scan lasted {min} {sec}.";
    }

    // ── Quick Scan Button ─────────────────────────────────────────────────────

    private void OnQuickScanClick()
    {
        if (_scanState == ScanState.Scanning)
            CancelScan();
        else
            StartScan();
    }

    private void StartScan()
    {
        _scanState       = ScanState.Scanning;
        _scanTargetFiles = UnityEngine.Random.Range(8000, 25001);
        _currentFiles    = 0;

        SetScanBtnLabel("Cancel");

        if (scanSlider != null)
        {
            scanSlider.value = 0f;
            scanSlider.gameObject.SetActive(true);
        }

        _scanCoroutine = StartCoroutine(RunScan());
        ActivityLogManager.Log("Started Quick Scan", ActivityLogManager.EntryType.Action);
    }

    private void CancelScan()
    {
        if (_scanCoroutine != null)
        {
            StopCoroutine(_scanCoroutine);
            _scanCoroutine = null;
        }

        _scanState = ScanState.Idle;

        if (scanSlider != null)
        {
            scanSlider.value = 0f;
            scanSlider.gameObject.SetActive(false);
        }

        SetScanBtnLabel("Quick Scan");
        RefreshVtpTMP();
        ActivityLogManager.Log("Quick Scan cancelled", ActivityLogManager.EntryType.Action);
    }

    // ── Scan Coroutine ────────────────────────────────────────────────────────

    private IEnumerator RunScan()
    {
        float elapsed = 0f;

        while (elapsed < scanDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / scanDuration);

            if (scanSlider != null) scanSlider.value = progress;

            _currentFiles = Mathf.RoundToInt(progress * _scanTargetFiles);

            int totalSec = Mathf.FloorToInt(elapsed);
            if (vtpDynamicTMP != null)
                vtpDynamicTMP.text = $"Quick scan running.\nElapsed: {totalSec / 60:D2}:{totalSec % 60:D2}\n{_currentFiles:N0} files scanned.";

            yield return null;
        }

        _scanCoroutine = null;
        OnScanComplete(elapsed);
    }

    private void OnScanComplete(float totalElapsed)
    {
        if (scanSlider != null)
        {
            scanSlider.value = 1f;
            scanSlider.gameObject.SetActive(false);
        }

        _currentFiles = _scanTargetFiles;

        int totalSec = Mathf.FloorToInt(totalElapsed);
        _lastScanResult = new ScanResultData
        {
            DateTimeStr    = DateTime.Now.ToString("M/d/yyyy h:mm tt"),
            ThreatsFound   = 0,
            MinutesElapsed = totalSec / 60,
            SecondsElapsed = totalSec % 60,
            FilesScanned   = _currentFiles,
        };

        _scanState = ScanState.Complete;
        SetScanBtnLabel("Quick Scan");
        RefreshVtpTMP();
        toastController?.Show();

        ActivityLogManager.Log("Quick Scan completed. No threats found.", ActivityLogManager.EntryType.Action);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetScanBtnLabel(string label)
    {
        if (quickScanBtn == null) return;
        TMP_Text tmp = quickScanBtn.GetComponentInChildren<TMP_Text>();
        if (tmp != null) tmp.text = label;
    }

    private static ScanResultData GenerateRandomScanResult()
    {
        DateTime past = DateTime.Now.AddDays(-UnityEngine.Random.Range(3, 15));
        past = new DateTime(past.Year, past.Month, past.Day,
                            UnityEngine.Random.Range(8, 18),
                            UnityEngine.Random.Range(0, 60), 0);
        return new ScanResultData
        {
            DateTimeStr    = past.ToString("M/d/yyyy h:mm tt"),
            ThreatsFound   = 0,
            MinutesElapsed = UnityEngine.Random.Range(1, 5),
            SecondsElapsed = UnityEngine.Random.Range(10, 56),
            FilesScanned   = UnityEngine.Random.Range(45000, 130001),
        };
    }

    private void HideAll()
    {
        windowsSecurityPanel?.SetActive(false);
        toastController?.Hide();
    }

    private void CloseChain()
    {
        if (_scanCoroutine != null)
        {
            StopCoroutine(_scanCoroutine);
            _scanCoroutine = null;
        }
        _scanState = ScanState.Idle;
        HideAll();
        gameObject.SetActive(false);
    }
}
