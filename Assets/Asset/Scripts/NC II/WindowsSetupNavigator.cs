/*
 * ================================================================
 *  UNITY SETUP GUIDE — WindowsSetupNavigator
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to the WindowsSetupPanel root GameObject
 *    (the same object that T3MonitorController enables/disables).
 *
 *  HIERARCHY EXPECTED
 *
 *    WindowsSetupPanel  (this script here)
 *      ├─ SetUpInitialize
 *      │    ├─ Title              ← persisting; leave active; NOT referenced here
 *      │    ├─ FirstPhase
 *      │    │    ├─ Selection
 *      │    │    │    ├─ Dropdown      ← languageDropdown
 *      │    │    │    ├─ Dropdown (1)  ← timeZoneDropdown
 *      │    │    │    └─ Dropdown (2)  ← keyboardDropdown
 *      │    │    └─ Footer
 *      │    │         └─ Next  →  OnNextFromInitFirstPhase()
 *      │    └─ SecondPhase
 *      │         └─ Selection
 *      │              └─ InstallNow  →  OnInstallNow()
 *      │
 *      └─ SetUpLicenseAgreement
 *           ├─ FirstPhase
 *           │    ├─ Title
 *           │    ├─ Agreement Terms
 *           │    └─ Footer
 *           │         ├─ AgreeCheckBox  ← agreeCheckBox  (Toggle)
 *           │         └─ Next  →  OnNextFromLicenseFirstPhase()
 *           ├─ SecondPhase
 *           │    ├─ Title
 *           │    └─ InstallationOption
 *           │         ├─ Upgrade  →  OnUpgradeSelected()
 *           │         └─ Custom   →  OnCustomSelected()  (TBD)
 *           └─ ThirdPhase
 *                (partitioning UI — fully built; logic TBD)
 *
 *  INSPECTOR WIRING — see "Inspector Assignments" section below.
 *
 *  HOW IT WORKS
 *    T3MonitorController calls ResetToStart() every time boot
 *    validation passes so the panel always opens at the beginning.
 *    ESC closes the UEFICanvas; re-opening restores the current
 *    phase without resetting (T3MonitorController tracks this via
 *    its PanelState enum).
 *
 *  RESET BEHAVIOUR
 *    ResetToStart():
 *      – SetUpInitialize active, FirstPhase active, SecondPhase inactive
 *      – SetUpLicenseAgreement inactive (all phases inactive)
 *      – AgreeCheckBox unticked
 *
 * ================================================================
 *  INSPECTOR ASSIGNMENTS
 * ================================================================
 *
 *  SetUpInitialize
 *    setupInitialize        → SetUpInitialize (child of WindowsSetupPanel)
 *    initFirstPhase         → FirstPhase      (child of SetUpInitialize)
 *    initSecondPhase        → SecondPhase     (child of SetUpInitialize)
 *
 *  SetUpInitialize — Dropdowns inside FirstPhase > Selection
 *    languageDropdown       → Dropdown        (TMP_Dropdown)
 *    timeZoneDropdown       → Dropdown (1)    (TMP_Dropdown)
 *    keyboardDropdown       → Dropdown (2)    (TMP_Dropdown)
 *
 *  SetUpLicenseAgreement
 *    setupLicenseAgreement  → SetUpLicenseAgreement (child of WindowsSetupPanel)
 *    licenseFirstPhase      → FirstPhase      (child of SetUpLicenseAgreement)
 *    licenseSecondPhase     → SecondPhase     (child of SetUpLicenseAgreement)
 *    licenseThirdPhase      → ThirdPhase      (child of SetUpLicenseAgreement)
 *
 *  SetUpLicenseAgreement — FirstPhase > Footer
 *    agreeCheckBox          → AgreeCheckBox   (Toggle component)
 *
 *  BUTTON WIRING
 *    SetUpInitialize > FirstPhase > Footer > Next
 *      OnClick → WindowsSetupNavigator.OnNextFromInitFirstPhase()
 *
 *    SetUpInitialize > SecondPhase > Selection > InstallNow
 *      OnClick → WindowsSetupNavigator.OnInstallNow()
 *
 *    SetUpLicenseAgreement > FirstPhase > Footer > Next
 *      OnClick → WindowsSetupNavigator.OnNextFromLicenseFirstPhase()
 *
 *    SetUpLicenseAgreement > SecondPhase > InstallationOption > Upgrade
 *      OnClick → WindowsSetupNavigator.OnUpgradeSelected()
 *
 *    SetUpLicenseAgreement > SecondPhase > InstallationOption > Custom > Custom
 *      OnClick → WindowsSetupNavigator.OnCustomSelected()
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WindowsSetupNavigator : MonoBehaviour
{
    [Header("SetUpInitialize")]
    [SerializeField] private GameObject setupInitialize;
    [SerializeField] private GameObject initFirstPhase;
    [SerializeField] private GameObject initSecondPhase;

    [Header("SetUpInitialize — Dropdowns (FirstPhase > Selection)")]
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private TMP_Dropdown timeZoneDropdown;
    [SerializeField] private TMP_Dropdown keyboardDropdown;

    [Header("SetUpLicenseAgreement")]
    [SerializeField] private GameObject setupLicenseAgreement;
    [SerializeField] private GameObject licenseFirstPhase;
    [SerializeField] private GameObject licenseSecondPhase;
    [SerializeField] private GameObject licenseThirdPhase;

    [Header("SetUpLicenseAgreement — FirstPhase Controls")]
    [Tooltip("AgreeCheckBox Toggle — Next button is blocked until this is ticked.")]
    [SerializeField] private Toggle agreeCheckBox;

    [Header("ThirdPhase — Partition Manager")]
    [Tooltip("PartitionManager component on the ThirdPhase GameObject.")]
    [SerializeField] private PartitionManager partitionManager;

    [Header("FourthPhase — Windows Installation")]
    [SerializeField] private GameObject licenseFourthPhase;
    [SerializeField] private WindowsInstallationManager installationManager;

    [Header("FifthPhase — Windows Setup (OOBE)")]
    [SerializeField] private GameObject licenseFifthPhase;
    [SerializeField] private FifthPhaseManager fifthPhaseManager;

    // ----------------------------------------------------------------
    //  Reset — called by T3MonitorController.ProceedToWindowsSetup()
    // ----------------------------------------------------------------

    // Called by T3MonitorController.ProceedToWindows10Panel() on the skip path.
    // Hides every setup child so only Windows10Panel (managed separately) is visible.
    public void PrepareForWindows10()
    {
        setupInitialize?.SetActive(false);
        setupLicenseAgreement?.SetActive(false);
        licenseFirstPhase?.SetActive(false);
        licenseSecondPhase?.SetActive(false);
        licenseThirdPhase?.SetActive(false);
        licenseFourthPhase?.SetActive(false);
        licenseFifthPhase?.SetActive(false);

        Debug.Log("[WindowsSetupNavigator] Prepared for Windows10 skip path — all setup panels hidden.");
    }

    public void ResetToStart()
    {
        // SetUpInitialize: show, start on FirstPhase
        setupInitialize?.SetActive(true);
        initFirstPhase?.SetActive(true);
        initSecondPhase?.SetActive(false);

        // SetUpLicenseAgreement: hide everything
        setupLicenseAgreement?.SetActive(false);
        licenseFirstPhase?.SetActive(false);
        licenseSecondPhase?.SetActive(false);
        licenseThirdPhase?.SetActive(false);

        // Reset agreement checkbox
        if (agreeCheckBox != null)
            agreeCheckBox.isOn = false;

        // Reset partition state so ThirdPhase is clean on next entry
        partitionManager?.InitPartitions();

        // Hide FourthPhase and FifthPhase
        licenseFourthPhase?.SetActive(false);
        licenseFifthPhase?.SetActive(false);

        Debug.Log("[WindowsSetupNavigator] Reset to start.");
    }

    // ----------------------------------------------------------------
    //  SetUpInitialize navigation
    // ----------------------------------------------------------------

    // Wired to: SetUpInitialize > FirstPhase > Footer > Next > OnClick
    public void OnNextFromInitFirstPhase()
    {
        initFirstPhase?.SetActive(false);
        initSecondPhase?.SetActive(true);
        Debug.Log("[WindowsSetupNavigator] Moved to SetUpInitialize SecondPhase.");
    }

    // Wired to: SetUpInitialize > SecondPhase > Selection > InstallNow > OnClick
    public void OnInstallNow()
    {
        setupInitialize?.SetActive(false);

        setupLicenseAgreement?.SetActive(true);
        licenseFirstPhase?.SetActive(true);
        licenseSecondPhase?.SetActive(false);
        licenseThirdPhase?.SetActive(false);

        Debug.Log("[WindowsSetupNavigator] Entered SetUpLicenseAgreement.");
    }

    // ----------------------------------------------------------------
    //  SetUpLicenseAgreement navigation
    // ----------------------------------------------------------------

    // Wired to: SetUpLicenseAgreement > FirstPhase > Footer > Next > OnClick
    // Blocked until AgreeCheckBox is ticked.
    public void OnNextFromLicenseFirstPhase()
    {
        if (agreeCheckBox != null && !agreeCheckBox.isOn)
        {
            Debug.Log("[WindowsSetupNavigator] License agreement not accepted — Next blocked.");
            return;
        }

        licenseFirstPhase?.SetActive(false);
        licenseSecondPhase?.SetActive(true);
        Debug.Log("[WindowsSetupNavigator] Moved to SetUpLicenseAgreement SecondPhase.");
    }

    // Wired to: SetUpLicenseAgreement > SecondPhase > InstallationOption > Upgrade > OnClick
    public void OnUpgradeSelected()
    {
        licenseSecondPhase?.SetActive(false);
        licenseThirdPhase?.SetActive(true);
        Debug.Log("[WindowsSetupNavigator] Upgrade selected — entered ThirdPhase.");
    }

    // Wired to: SetUpLicenseAgreement > SecondPhase > InstallationOption > Custom > Custom > OnClick
    public void OnCustomSelected()
    {
        licenseSecondPhase?.SetActive(false);
        licenseThirdPhase?.SetActive(true);

        // Reset partition list every time the user enters the partition screen
        partitionManager?.InitPartitions();

        Debug.Log("[WindowsSetupNavigator] Custom install — entered ThirdPhase (partition screen).");
    }

    // Called by PartitionManager.OnNextClicked() when a valid Primary partition is selected.
    public void OnEnterFourthPhase()
    {
        licenseThirdPhase?.SetActive(false);

        licenseFourthPhase?.SetActive(true);
        installationManager?.StartInstallation();

        Debug.Log("[WindowsSetupNavigator] Entered FourthPhase — Windows installation started.");
    }

    // Called by WindowsInstallationManager when all five steps reach 100%.
    public void OnInstallationComplete()
    {
        Debug.Log("[WindowsSetupNavigator] Windows installation complete — entering FifthPhase.");
        OnEnterFifthPhase();
    }

    // Transitions from FourthPhase to FifthPhase.
    public void OnEnterFifthPhase()
    {
        licenseFourthPhase?.SetActive(false);

        licenseFifthPhase?.SetActive(true);
        fifthPhaseManager?.InitFifthPhase();

        Debug.Log("[WindowsSetupNavigator] Entered FifthPhase (OOBE).");
    }
}
