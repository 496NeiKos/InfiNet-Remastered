/*
 * ================================================================
 *  UNITY SETUP GUIDE — ServerManagerController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to the "Server Manager Panel" GameObject (starts INACTIVE).
 *
 *  HIERARCHY
 *    Server Manager Panel               ← this script here
 *      ├── TitleBar
 *      │     └── CloseBtn              → closeBtn
 *      ├── TopBar
 *      │     ├── AddRolesBtn           → addRolesBtn
 *      │     └── ToolsMenuBtn          → toolsMenuBtn
 *      ├── ToolsDropdown               → toolsDropdown (starts INACTIVE)
 *      │     ├── ADUCBtn               → toolADUCBtn
 *      │     ├── DNSBtn                → toolDNSBtn       (hidden until DNS installed)
 *      │     ├── DHCPBtn               → toolDHCPBtn      (hidden until DHCP installed)
 *      │     ├── GPMBtn                → toolGPMBtn       (hidden until IsDomainController)
 *      │     ├── FSRMBtn               → toolFSRMBtn      (hidden until File Services)
 *      │     ├── PrintMgmtBtn          → toolPrintMgmtBtn (hidden until Print installed)
 *      │     └── RDSBtn                → toolRDSBtn       (hidden until RD installed)
 *      ├── Dashboard
 *      │     ├── RolesPanel            → rolesPanel
 *      │     │     └── (role status rows — managed at runtime via roleStatusRows)
 *      │     └── Notification Area     → notificationArea
 *      │           └── PromoteFlag     → promotionFlag (INACTIVE until AD DS installed
 *      │                                 and not yet domain controller)
 *      └── Gate Overlay                → gateOverlay (ACTIVE until pre-config done;
 *                                        TMP_Text child explains what is required)
 *
 *  INSPECTOR ASSIGNMENTS
 *    closeBtn, addRolesBtn, toolsMenuBtn → as above
 *    toolsDropdown → ToolsDropdown panel
 *    toolADUCBtn / toolDNSBtn / toolDHCPBtn / toolGPMBtn /
 *    toolFSRMBtn / toolPrintMgmtBtn / toolRDSBtn → tool buttons
 *    promotionFlag → the yellow notification button in Dashboard
 *    gateOverlay   → the blocking panel shown before pre-config is done
 *    roleStatusRows → array of RoleStatusRow GOs (one per role, showing name + status)
 *
 *  REFERENCES (assign in Inspector)
 *    roleWizard    → RoleWizardController
 *    dcPromoWizard → DCPromoWizardController
 *    aduc          → ADUCController
 *    dhcp          → DHCPConsoleController
 *    gpm           → GroupPolicyController
 *    fsrm          → FileServerRMController
 *    printMgmt     → PrintManagementController
 *
 *  DESKTOP ICON WIRING
 *    Server Manager desktop icon → Button OnClick → ServerManagerController.Open()
 *
 *  HOW IT WORKS
 *    Blocked by gateOverlay until ServerDeviceState.PCRenamed && IPConfigured.
 *    Add Roles button opens RoleWizardController.
 *    Tools menu shows only consoles for installed roles (refreshed on open).
 *    Promotion notification appears when ADDSInstalled && !IsDomainController.
 *    Clicking it opens DCPromoWizardController.
 *    Dashboard role rows update each time the panel is opened.
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerManagerController : MonoBehaviour
{
    public static ServerManagerController Instance { get; private set; }

    [Header("Nav")]
    [SerializeField] private Button closeBtn;
    [SerializeField] private Button addRolesBtn;
    [SerializeField] private Button toolsMenuBtn;

    [Header("Tools Dropdown")]
    [SerializeField] private GameObject toolsDropdown;
    [SerializeField] private Button toolADUCBtn;
    [SerializeField] private Button toolDNSBtn;
    [SerializeField] private Button toolDHCPBtn;
    [SerializeField] private Button toolGPMBtn;
    [SerializeField] private Button toolFSRMBtn;
    [SerializeField] private Button toolPrintMgmtBtn;
    [SerializeField] private Button toolRDSBtn;

    [Header("Dashboard")]
    [SerializeField] private GameObject notificationArea;
    [SerializeField] private Button     promotionFlag;
    [SerializeField] private GameObject gateOverlay;

    [Header("App References")]
    [SerializeField] private RoleWizardController      roleWizard;
    [SerializeField] private DCPromoWizardController   dcPromoWizard;
    [SerializeField] private ADUCController            aduc;
    [SerializeField] private DHCPConsoleController     dhcp;
    [SerializeField] private GroupPolicyController     gpm;
    [SerializeField] private FileServerRMController    fsrm;
    [SerializeField] private PrintManagementController printMgmt;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        closeBtn?.onClick.AddListener(Close);
        addRolesBtn?.onClick.AddListener(OpenAddRoles);
        toolsMenuBtn?.onClick.AddListener(ToggleToolsMenu);
        promotionFlag?.onClick.AddListener(OpenDCPromo);

        toolADUCBtn?.onClick.AddListener(()  => { CloseToolsMenu(); aduc?.Open(); });
        toolDHCPBtn?.onClick.AddListener(()  => { CloseToolsMenu(); dhcp?.Open(); });
        toolGPMBtn?.onClick.AddListener(()   => { CloseToolsMenu(); gpm?.Open(); });
        toolFSRMBtn?.onClick.AddListener(()  => { CloseToolsMenu(); fsrm?.Open(); });
        toolPrintMgmtBtn?.onClick.AddListener(() => { CloseToolsMenu(); printMgmt?.Open(); });

        toolsDropdown?.SetActive(false);
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;
        RefreshGate();
        RefreshNotification();
        RefreshToolsVisibility();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        CloseToolsMenu();
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.ServerManagerOpened = true;
        ActivityLogManager.Log("Opened Server Manager", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        CloseToolsMenu();
        gameObject.SetActive(false);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void RefreshGate()
    {
        if (gateOverlay == null) return;
        var state = ServerVirtualOSManager.Instance?.ServerState;
        bool ready = state != null && state.PCRenamed && state.IPConfigured;
        gateOverlay.SetActive(!ready);
        if (addRolesBtn != null) addRolesBtn.interactable = ready;
        if (toolsMenuBtn != null) toolsMenuBtn.interactable = ready;
    }

    private void RefreshNotification()
    {
        if (promotionFlag == null || notificationArea == null) return;
        var state = ServerVirtualOSManager.Instance?.ServerState;
        bool showFlag = state != null && state.ADDSInstalled && !state.IsDomainController;
        notificationArea.SetActive(showFlag);
        promotionFlag.gameObject.SetActive(showFlag);
    }

    private void RefreshToolsVisibility()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;
        toolADUCBtn?.gameObject.SetActive(state.IsDomainController);
        toolDNSBtn?.gameObject.SetActive(state.DNSInstalled);
        toolDHCPBtn?.gameObject.SetActive(state.DHCPInstalled);
        toolGPMBtn?.gameObject.SetActive(state.IsDomainController);
        toolFSRMBtn?.gameObject.SetActive(state.FileServicesInstalled);
        toolPrintMgmtBtn?.gameObject.SetActive(state.PrintServicesInstalled);
        toolRDSBtn?.gameObject.SetActive(state.RDServicesInstalled);
    }

    private void ToggleToolsMenu()
    {
        if (toolsDropdown == null) return;
        toolsDropdown.SetActive(!toolsDropdown.activeSelf);
    }

    private void CloseToolsMenu()
    {
        toolsDropdown?.SetActive(false);
    }

    private void OpenAddRoles()
    {
        CloseToolsMenu();
        roleWizard?.Open();
    }

    private void OpenDCPromo()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.PromotionNotificationClicked = true;
        dcPromoWizard?.Open();
        ActivityLogManager.Log("Clicked promotion notification — opening dcpromo wizard", ActivityLogManager.EntryType.Action);
    }
}
