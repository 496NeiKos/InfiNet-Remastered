/*
 * ================================================================
 *  UNITY SETUP GUIDE — RoleWizardController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to the "Add Roles Wizard Panel" GameObject (starts INACTIVE).
 *
 *  HIERARCHY
 *    Add Roles Wizard Panel             ← this script here
 *      ├── Step 0 — Before You Begin    → steps[0]
 *      │     └── NextBtn               → nextBtn (wired in Inspector or via Awake)
 *      ├── Step 1 — Server Roles        → steps[1]
 *      │     ├── RoleList (scroll)
 *      │     │     └── (role toggle rows populated at runtime)
 *      │     ├── PrevBtn               → prevBtn
 *      │     └── NextBtn               → nextBtn
 *      ├── Step 2 — Confirmation        → steps[2]
 *      │     ├── ConfirmLabel          → confirmLabelTMP
 *      │     ├── PrevBtn
 *      │     └── InstallBtn            → installBtn
 *      └── Step 3 — Results            → steps[3]
 *            ├── ResultLabel           → resultLabelTMP
 *            └── CloseBtn             → closeWizardBtn
 *
 *  INSPECTOR ASSIGNMENTS
 *    steps[0..3]       → the four step panel GameObjects
 *    roleToggleParent  → scroll content Transform for role toggle rows
 *    roleTogglePrefab  → prefab with Toggle + TMP_Text label
 *    confirmLabelTMP   → TMP in step 2 showing selected role name
 *    resultLabelTMP    → TMP in step 3 showing success message
 *    prevBtn           → Previous button (shared or per-step)
 *    nextBtn           → Next button (shared or per-step)
 *    installBtn        → Install button in step 2
 *    closeWizardBtn    → Close button in step 3 (results only)
 *    cancelBtn         → Cancel/Close button always visible on steps 0–2
 *                        Place outside the step panels so it is always visible.
 *                        Lets the player exit the wizard before completing a role install.
 *
 *  ROLE OPTIONS (populate in Inspector)
 *    roleOptions[]     → array of RoleOption (DisplayName + Role enum + Description)
 *    Only roles NOT yet installed are shown in the list.
 *
 *  HOW IT WORKS
 *    Player opens wizard → selects ONE role via toggle → Next →
 *    Confirmation step shows selected role → Install → Results shows success →
 *    Close → marks role as installed in ServerDeviceState.
 *    After AD DS installs, ServerManagerController shows the promotion flag.
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ServerRole { ADDS, DHCP, FileServices, PrintServices, RemoteDesktopServices }

[System.Serializable]
public class RoleOption
{
    public string     DisplayName;
    [TextArea(2, 4)]
    public string     Description;
    public ServerRole Role;
}

public class RoleWizardController : MonoBehaviour
{
    public static RoleWizardController Instance { get; private set; }

    [Header("Steps")]
    [SerializeField] private GameObject[] steps = new GameObject[4];

    [Header("Role List")]
    [SerializeField] private Transform    roleToggleParent;
    [SerializeField] private GameObject   roleTogglePrefab;  // Toggle + TMP_Text child

    [Header("Labels")]
    [SerializeField] private TMP_Text confirmLabelTMP;
    [SerializeField] private TMP_Text resultLabelTMP;

    [Header("Buttons")]
    [SerializeField] private Button prevBtn;
    [SerializeField] private Button nextBtn;
    [SerializeField] private Button installBtn;
    [SerializeField] private Button closeWizardBtn;  // step 3 only
    [SerializeField] private Button cancelBtn;        // always visible on steps 0–2

    [Header("Role Options (configure in Inspector)")]
    [SerializeField] private RoleOption[] roleOptions;

    private int        _currentStep    = 0;
    private ServerRole _selectedRole;
    private bool       _roleSelected   = false;
    private string     _selectedRoleName = "";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        prevBtn?.onClick.AddListener(GoBack);
        nextBtn?.onClick.AddListener(GoNext);
        installBtn?.onClick.AddListener(Install);
        closeWizardBtn?.onClick.AddListener(CloseWizard);
        cancelBtn?.onClick.AddListener(CloseWizard);

        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        _currentStep  = 0;
        _roleSelected = false;
        gameObject.SetActive(true);
        ShowStep(0);
        ActivityLogManager.Log("Opened Add Roles and Features Wizard", ActivityLogManager.EntryType.Action);
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private void GoNext()
    {
        if (_currentStep == 0)
        {
            PopulateRoleList();
            ShowStep(1);
        }
        else if (_currentStep == 1)
        {
            if (!_roleSelected)
            {
                Debug.LogWarning("[RoleWizardController] No role selected.");
                return;
            }
            if (confirmLabelTMP != null)
                confirmLabelTMP.text = $"The following role will be installed:\n\n{_selectedRoleName}";
            ShowStep(2);
        }
    }

    private void GoBack()
    {
        if (_currentStep > 0)
            ShowStep(_currentStep - 1);
    }

    private void Install()
    {
        ApplyRoleToState(_selectedRole);
        if (resultLabelTMP != null)
            resultLabelTMP.text = $"{_selectedRoleName}\nInstallation succeeded.";
        ShowStep(3);
        ActivityLogManager.Log($"Role installed: {_selectedRoleName}", ActivityLogManager.EntryType.Action);
    }

    private void CloseWizard()
    {
        gameObject.SetActive(false);
    }

    // ── Role list ─────────────────────────────────────────────────────────────

    private void PopulateRoleList()
    {
        foreach (Transform child in roleToggleParent)
            Destroy(child.gameObject);

        var state = ServerVirtualOSManager.Instance?.ServerState;
        foreach (var option in roleOptions)
        {
            if (state != null && IsRoleInstalled(state, option.Role)) continue;

            GameObject row = Instantiate(roleTogglePrefab, roleToggleParent);
            var toggle = row.GetComponentInChildren<Toggle>();
            var label  = row.GetComponentInChildren<TMP_Text>();
            if (label  != null) label.text = option.DisplayName;
            if (toggle != null) toggle.SetIsOnWithoutNotify(false); // always start unchecked

            var captured = option;
            toggle?.onValueChanged.AddListener(on =>
            {
                if (!on) return;
                // Deselect all other toggles
                foreach (Transform t in roleToggleParent)
                {
                    var otherToggle = t.GetComponentInChildren<Toggle>();
                    if (otherToggle != null && otherToggle != toggle)
                        otherToggle.SetIsOnWithoutNotify(false);
                }
                _selectedRole     = captured.Role;
                _selectedRoleName = captured.DisplayName;
                _roleSelected     = true;
                SetWizardOpenedLatch(captured.Role);
            });
        }
    }

    private static bool IsRoleInstalled(ServerDeviceState s, ServerRole role) => role switch
    {
        ServerRole.ADDS                    => s.ADDSInstalled,
        ServerRole.DHCP                    => s.DHCPInstalled,
        ServerRole.FileServices            => s.FileServicesInstalled,
        ServerRole.PrintServices           => s.PrintServicesInstalled,
        ServerRole.RemoteDesktopServices   => s.RDServicesInstalled,
        _                                  => false
    };

    private static void ApplyRoleToState(ServerRole role)
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;
        switch (role)
        {
            case ServerRole.ADDS:                  state.ADDSInstalled          = true; break;
            case ServerRole.DHCP:                  state.DHCPInstalled          = true; break;
            case ServerRole.FileServices:          state.FileServicesInstalled  = true; break;
            case ServerRole.PrintServices:         state.PrintServicesInstalled = true; break;
            case ServerRole.RemoteDesktopServices: state.RDServicesInstalled    = true; break;
        }
    }

    private static void SetWizardOpenedLatch(ServerRole role)
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;
        switch (role)
        {
            case ServerRole.ADDS:                  state.AddRolesWizardOpenedForADDS  = true; break;
            case ServerRole.DHCP:                  state.AddRolesWizardOpenedForDHCP  = true; break;
            case ServerRole.FileServices:          state.AddRolesWizardOpenedForFile  = true; break;
            case ServerRole.PrintServices:         state.AddRolesWizardOpenedForPrint = true; break;
            case ServerRole.RemoteDesktopServices: state.AddRolesWizardOpenedForRD    = true; break;
        }
    }

    // ── Step display ──────────────────────────────────────────────────────────

    private void ShowStep(int index)
    {
        for (int i = 0; i < steps.Length; i++)
            steps[i]?.SetActive(i == index);
        _currentStep = index;

        if (prevBtn        != null) prevBtn.gameObject.SetActive(index > 0 && index < 3);
        if (nextBtn        != null) nextBtn.gameObject.SetActive(index < 2);
        if (installBtn     != null) installBtn.gameObject.SetActive(index == 2);
        if (closeWizardBtn != null) closeWizardBtn.gameObject.SetActive(index == 3);
        if (cancelBtn      != null) cancelBtn.gameObject.SetActive(index < 3);
    }
}
