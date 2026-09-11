/*
 * ================================================================
 *  UNITY SETUP GUIDE — PrintManagementController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Print Management Panel" (starts INACTIVE).
 *    Opened from ServerManagerController Tools → Print Management.
 *
 *  HIERARCHY
 *    Print Management Panel             ← this script here
 *      ├── TitleBar / CloseBtn          → closeBtn
 *      ├── TreePanel (left)
 *      │     ├── PrintServersNode
 *      │     │     └── DriversNode      → driversNodeBtn
 *      │     └── (printer list added at runtime under printerNodeParent)
 *      ├── ContentPanel (right)
 *      │     ├── DriversView            → driversView (starts INACTIVE)
 *      │     │     ├── DriverList       → driverListParent
 *      │     │     ├── DriverRowPrefab  → driverRowPrefab
 *      │     │     └── AddDriverBtn    → addDriverBtn
 *      │     └── PrintersView          → printersView (starts INACTIVE)
 *      │           ├── PrinterList      → printerListParent
 *      │           ├── PrinterRowPrefab → printerRowPrefab
 *      │           └── ShareBtn         → sharePrinterBtn
 *      ├── Add Driver Wizard            → addDriverWizard (starts INACTIVE)
 *      │     ├── Step 0 — Welcome       → drvStep0
 *      │     ├── Step 1 — Processor     → drvStep1 (static — 64-bit pre-selected)
 *      │     ├── Step 2 — Printer Driver → drvStep2
 *      │     │     └── ManufacturerDropdown → manufacturerDropdown
 *      │     │     └── ModelDropdown    → modelDropdown
 *      │     ├── Step 3 — Results       → drvStep3
 *      │     ├── NextBtn               → drvNextBtn
 *      │     ├── PrevBtn               → drvPrevBtn
 *      │     └── FinishBtn             → drvFinishBtn
 *      └── Share Printer Dialog         → sharePrinterDialog (starts INACTIVE)
 *            ├── ShareNameInput        → shareNameInput
 *            ├── OKBtn                 → shareOKBtn
 *            └── CancelBtn             → shareCancelBtn
 *
 *  INSPECTOR ASSIGNMENTS
 *    All fields as above.
 *    manufacturerOptions → string[] array (e.g. {"HP","Canon","Epson","Brother"})
 *    modelOptions        → string[] array (e.g. {"LaserJet Pro","PIXMA","L3150","HL-L2350DW"})
 *    Both populated in Inspector — no hardcoded strings.
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrintManagementController : MonoBehaviour
{
    public static PrintManagementController Instance { get; private set; }

    [Header("Nav")]
    [SerializeField] private Button closeBtn;
    [SerializeField] private Button driversNodeBtn;
    [SerializeField] private Button printersNodeBtn;

    [Header("Views")]
    [SerializeField] private GameObject driversView;
    [SerializeField] private GameObject printersView;

    [Header("Driver List")]
    [SerializeField] private Transform  driverListParent;
    [SerializeField] private GameObject driverRowPrefab;
    [SerializeField] private Button     addDriverBtn;

    [Header("Printer List")]
    [SerializeField] private Transform  printerListParent;
    [SerializeField] private GameObject printerRowPrefab;
    [SerializeField] private Button     sharePrinterBtn;

    [Header("Add Driver Wizard")]
    [SerializeField] private GameObject  addDriverWizard;
    [SerializeField] private GameObject  drvStep0;
    [SerializeField] private GameObject  drvStep1;
    [SerializeField] private GameObject  drvStep2;
    [SerializeField] private GameObject  drvStep3;
    [SerializeField] private Button      drvNextBtn;
    [SerializeField] private Button      drvPrevBtn;
    [SerializeField] private Button      drvFinishBtn;
    [SerializeField] private TMP_Dropdown manufacturerDropdown;
    [SerializeField] private TMP_Dropdown modelDropdown;

    [Header("Share Printer Dialog")]
    [SerializeField] private GameObject    sharePrinterDialog;
    [SerializeField] private TMP_InputField shareNameInput;
    [SerializeField] private Button        shareOKBtn;
    [SerializeField] private Button        shareCancelBtn;

    [Header("Driver Options (Inspector)")]
    [SerializeField] private string[] manufacturerOptions = { "HP", "Canon", "Epson", "Brother" };
    [SerializeField] private string[] modelOptions        = { "LaserJet Pro M404dn", "PIXMA G3010", "EcoTank L3150", "HL-L2350DW" };

    private int    _drvStep        = 0;
    private string _selectedDriver = "";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        closeBtn?.onClick.AddListener(Close);
        driversNodeBtn?.onClick.AddListener(ShowDrivers);
        printersNodeBtn?.onClick.AddListener(ShowPrinters);
        addDriverBtn?.onClick.AddListener(OpenAddDriverWizard);
        sharePrinterBtn?.onClick.AddListener(OpenShareDialog);
        drvNextBtn?.onClick.AddListener(WizardNext);
        drvPrevBtn?.onClick.AddListener(WizardBack);
        drvFinishBtn?.onClick.AddListener(WizardFinish);
        shareOKBtn?.onClick.AddListener(ConfirmShare);
        shareCancelBtn?.onClick.AddListener(() => sharePrinterDialog?.SetActive(false));

        gameObject.SetActive(false);
    }

    private void Start()
    {
        if (manufacturerDropdown != null)
        {
            manufacturerDropdown.ClearOptions();
            manufacturerDropdown.AddOptions(new List<string>(manufacturerOptions));
        }
        if (modelDropdown != null)
        {
            modelDropdown.ClearOptions();
            modelDropdown.AddOptions(new List<string>(modelOptions));
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.PrintMgmtOpened = true;
        ShowDrivers();
        ActivityLogManager.Log("Opened Print Management", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        addDriverWizard?.SetActive(false);
        sharePrinterDialog?.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── View switching ────────────────────────────────────────────────────────

    private void ShowDrivers()
    {
        driversView?.SetActive(true);
        printersView?.SetActive(false);
        RefreshDriverList();
    }

    private void ShowPrinters()
    {
        driversView?.SetActive(false);
        printersView?.SetActive(true);
        RefreshPrinterList();
    }

    // ── Add Driver Wizard ─────────────────────────────────────────────────────

    private void OpenAddDriverWizard()
    {
        _drvStep = 0;
        addDriverWizard?.SetActive(true);
        ShowWizardStep(0);
    }

    private void WizardNext()
    {
        if (_drvStep < 2) ShowWizardStep(_drvStep + 1);
    }

    private void WizardBack()
    {
        if (_drvStep > 0) ShowWizardStep(_drvStep - 1);
    }

    private void WizardFinish()
    {
        string mfr   = manufacturerDropdown != null && manufacturerDropdown.options.Count > 0
            ? manufacturerDropdown.options[manufacturerDropdown.value].text : "Unknown";
        string model = modelDropdown != null && modelDropdown.options.Count > 0
            ? modelDropdown.options[modelDropdown.value].text : "Unknown";
        _selectedDriver = $"{mfr} {model}";

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.PrinterDriverAdded = true;

        addDriverWizard?.SetActive(false);
        ShowWizardStep(3);
        RefreshDriverList();
        ActivityLogManager.Log($"Printer driver added: {_selectedDriver}", ActivityLogManager.EntryType.Action);

        // Auto-switch to show printer after driver install
        ShowPrinters();
    }

    private void ShowWizardStep(int step)
    {
        _drvStep = step;
        drvStep0?.SetActive(step == 0);
        drvStep1?.SetActive(step == 1);
        drvStep2?.SetActive(step == 2);
        drvStep3?.SetActive(step == 3);
        drvNextBtn?.gameObject.SetActive(step < 2);
        drvPrevBtn?.gameObject.SetActive(step > 0 && step < 3);
        drvFinishBtn?.gameObject.SetActive(step == 2);
    }

    // ── Share Printer ─────────────────────────────────────────────────────────

    private void OpenShareDialog()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || !state.PrinterDriverAdded)
        {
            Debug.LogWarning("[PrintMgmt] Install a printer driver first.");
            return;
        }
        if (shareNameInput != null) shareNameInput.text = "";
        sharePrinterDialog?.SetActive(true);
    }

    private void ConfirmShare()
    {
        string name = shareNameInput != null ? shareNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(name)) { Debug.LogWarning("[PrintMgmt] Share name required."); return; }

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null)
        {
            state.PrinterShared    = true;
            state.PrinterShareName = name;
        }
        sharePrinterDialog?.SetActive(false);
        RefreshPrinterList();
        ActivityLogManager.Log($"Printer shared as: {name}", ActivityLogManager.EntryType.Action);
    }

    // ── Refresh lists ─────────────────────────────────────────────────────────

    private void RefreshDriverList()
    {
        foreach (Transform child in driverListParent) Destroy(child.gameObject);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || !state.PrinterDriverAdded) return;

        var row   = Instantiate(driverRowPrefab, driverListParent);
        var label = row.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = _selectedDriver;
    }

    private void RefreshPrinterList()
    {
        foreach (Transform child in printerListParent) Destroy(child.gameObject);
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || !state.PrinterDriverAdded) return;

        var row    = Instantiate(printerRowPrefab, printerListParent);
        var labels = row.GetComponentsInChildren<TMP_Text>();
        if (labels.Length > 0) labels[0].text = _selectedDriver;
        if (labels.Length > 1) labels[1].text = state.PrinterShared
            ? $"Shared as: {state.PrinterShareName}" : "Not shared";
    }
}
