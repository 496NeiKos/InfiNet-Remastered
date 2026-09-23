/*
 * ================================================================
 *  UNITY SETUP GUIDE — ServerManagerController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to the "Server Manager Panel" GameObject (starts INACTIVE).
 *
 *  ── FULL HIERARCHY ───────────────────────────────────────────────
 *
 *    Server Manager Panel               ← this script here (INACTIVE)
 *      │
 *      ├── TitleBar                     Image (header bar)
 *      │     ├── TitleTMP               TMP_Text "Server Manager"
 *      │     └── CloseBtn               Button  → closeBtn
 *      │
 *      ├── TopBar                       Image (toolbar)
 *      │     ├── ManageTMP              TMP_Text "Manage"  (static label)
 *      │     ├── AddRolesBtn            Button  → addRolesBtn
 *      │     │     Child TMP: "Add Roles and Features"
 *      │     ├── ToolsMenuBtn           Button  → toolsMenuBtn
 *      │     │     Child TMP: "Tools"
 *      │     └── Notification Area      → notificationArea  (INACTIVE until ADDS installed)
 *      │           └── PromoteFlag      Button  → promotionFlag
 *      │                 Child TMP: "! Configuration required for AD DS"
 *      │
 *      ├── ToolsDropdown                → toolsDropdown  (INACTIVE)
 *      │     ├── ADUCBtn                Button  → toolADUCBtn  (hidden until IsDomainController)
 *      │     ├── DNSBtn                 Button  → toolDNSBtn   (hidden until DNSInstalled)
 *      │     ├── DHCPBtn                Button  → toolDHCPBtn  (hidden until DHCPInstalled)
 *      │     ├── GPMBtn                 Button  → toolGPMBtn   (hidden until IsDomainController)
 *      │     ├── FSRMBtn                Button  → toolFSRMBtn  (hidden until FileServicesInstalled)
 *      │     ├── PrintMgmtBtn           Button  → toolPrintMgmtBtn (hidden until PrintServicesInstalled)
 *      │     └── RDSBtn                 Button  → toolRDSBtn   (hidden until RDServicesInstalled)
 *      │
 *      ├── Tree Panel                   Image (left nav column)
 *      │     ├── DashboardBtn           Button  → dashboardTreeBtn
 *      │     │     Child TMP: "Dashboard"
 *      │     │     Button Image background: used for selection highlight
 *      │     ├── LocalServerBtn         Button  → localServerTreeBtn
 *      │     │     Child TMP: "Local Server"
 *      │     └── RoleButtonsContainer   → roleButtonsContainer  (RectTransform)
 *      │           Components required:
 *      │             VerticalLayoutGroup  — Spacing: 2, Child Alignment: Upper Left,
 *      │                                    Control Child Size Width: ✓, Height: ✓,
 *      │                                    Use Child Scale: ✗, Child Force Expand: ✗
 *      │             ContentSizeFitter    — Horizontal Fit: Unconstrained,
 *      │                                    Vertical Fit: Preferred Size
 *      │           These two components make the container grow downward as buttons
 *      │           are added. RefreshTreeRoleButtons() then calls
 *      │           LayoutRebuilder.ForceRebuildLayoutImmediate() so the resize
 *      │           happens on the same frame, not one frame late.
 *      │           └── [TreeRoleButtonPrefab × N spawned here at runtime]
 *      │
 *      ├── Main Panel                   Image (right content area)
 *      │     ├── Dashboard Panel        → dashboardPanelGO  (ACTIVE by default)
 *      │     │     └── DashboardPanelController  → dashboardPanel
 *      │     ├── Local Server Panel     → localServerPanelGO  (INACTIVE)
 *      │     │     └── LocalServerPanelController  → localServerPanel
 *      │     └── Role Placeholder Panel → rolePlaceholderPanelGO  (INACTIVE)
 *      │           └── PlaceholderTMP   TMP_Text "Content not available in simulation."
 *      │
 *      ├── System Properties Remote Panel  → SystemPropertiesRemoteController
 *      │     (sibling of Main Panel, child of Server Manager Panel; starts INACTIVE)
 *      │     Referenced by LocalServerPanelController — no field needed here.
 *      │
 *      ├── Roles Panel                  (MUST be m_IsActive: 1 in scene)
 *      │     Container for all tool-app panels. Individual panels start INACTIVE
 *      │     via their own Awake(). This parent must be active so children can render.
 *      │     ├── Add Roles Wizard Panel      (RoleWizardController — INACTIVE)
 *      │     ├── DCPromo Wizard Panel        (DCPromoWizardController — INACTIVE)
 *      │     ├── DHCP Console Panel          (DHCPConsoleController — INACTIVE)
 *      │     ├── ADUC Panel                  (ADUCController — INACTIVE)
 *      │     ├── Group Policy Mgmt Panel     (GroupPolicyController — INACTIVE)
 *      │     ├── File Server RM Panel        (FileServerRMController — INACTIVE)
 *      │     └── Print Management Panel      (PrintManagementController — INACTIVE)
 *      │
 *      └── Gate Overlay                 → gateOverlay  (ACTIVE until pre-config done)
 *            └── GateTMP               TMP_Text "Complete pre-configuration before
 *                                                using Server Manager:\n
 *                                                • Rename this PC (Settings > System > About)\n
 *                                                • Set a static IP (Network and Sharing Center)"
 *
 *  ── TREE ROLE BUTTON PREFAB ──────────────────────────────────────
 *    (Asset/Prefab/NC II Prefab/COC III/treeRoleButtonPrefab.prefab)
 *    Root:
 *      Button component — targetGraphic: Image on root
 *      Image component  — background; color set to treeButtonNormalColor at runtime
 *    Child:
 *      RoleNameTMP  TMP_Text — default ""  (set at runtime by RefreshTreeRoleButtons)
 *
 *  ── INSPECTOR ASSIGNMENTS ────────────────────────────────────────
 *
 *    closeBtn          → TitleBar/CloseBtn
 *    addRolesBtn       → TopBar/AddRolesBtn
 *    toolsMenuBtn      → TopBar/ToolsMenuBtn
 *    notificationArea  → TopBar/Notification Area (GameObject)
 *    promotionFlag     → TopBar/Notification Area/PromoteFlag (Button)
 *
 *    toolsDropdown     → ToolsDropdown (GameObject)
 *    toolADUCBtn       → ToolsDropdown/ADUCBtn
 *    toolDNSBtn        → ToolsDropdown/DNSBtn
 *    toolDHCPBtn       → ToolsDropdown/DHCPBtn
 *    toolGPMBtn        → ToolsDropdown/GPMBtn
 *    toolFSRMBtn       → ToolsDropdown/FSRMBtn
 *    toolPrintMgmtBtn  → ToolsDropdown/PrintMgmtBtn
 *    toolRDSBtn        → ToolsDropdown/RDSBtn
 *
 *    dashboardTreeBtn     → Tree Panel/DashboardBtn
 *    localServerTreeBtn   → Tree Panel/LocalServerBtn
 *    roleButtonsContainer → Tree Panel/RoleButtonsContainer (Transform)
 *    treeRoleButtonPrefab → treeRoleButtonPrefab prefab asset
 *
 *    dashboardPanelGO     → Main Panel/Dashboard Panel (GameObject)
 *    localServerPanelGO   → Main Panel/Local Server Panel (GameObject)
 *    rolePlaceholderPanelGO → Main Panel/Role Placeholder Panel (GameObject)
 *    dashboardPanel       → DashboardPanelController on Dashboard Panel
 *    localServerPanel     → LocalServerPanelController on Local Server Panel
 *
 *    roleDisplayEntries   → configure in Inspector (one entry per ServerRole):
 *                           ADDS      → "AD DS"
 *                           DNS       → "DNS"
 *                           DHCP      → "DHCP"
 *                           FileServices → "File and Storage Services"
 *                           PrintServices → "Print Services"
 *                           RemoteDesktopServices → "Remote Desktop Services"
 *
 *    treeButtonNormalColor   → set to the tree button's default background color
 *    treeButtonSelectedColor → set to the desired selection highlight color
 *
 *    gateOverlay       → Gate Overlay (GameObject)
 *
 *    roleWizard        → RoleWizardController (on Add Roles Wizard Panel)
 *    dcPromoWizard     → DCPromoWizardController
 *    aduc              → ADUCController
 *    dhcp              → DHCPConsoleController
 *    gpm               → GroupPolicyController
 *    fsrm              → FileServerRMController
 *    printMgmt         → PrintManagementController
 *
 *  ── HOW IT WORKS ─────────────────────────────────────────────────
 *    Gate Overlay blocks interaction until PCRenamed && IPConfigured.
 *    On Open(): tree buttons are rebuilt from installed roles, Dashboard is shown.
 *    Tree buttons (Dashboard, Local Server, role buttons) call ShowDashboard(),
 *    ShowLocalServer(), or ShowRolePlaceholder() respectively.
 *    SetMainPanel() deactivates all three content panels then activates the chosen one.
 *    SelectTreeButton() resets all tree button colors then highlights the chosen one.
 *    RefreshTreeRoleButtons() is called by RoleWizardController.Install() so the
 *    tree updates immediately when a role finishes installing (before wizard closes).
 *    RefreshDashboard() is also called by RoleWizardController.Install() so tile
 *    count and summary stay in sync.
 *    Notification flag (TopBar) shows when ADDS installed && !IsDomainController.
 *    Clicking it opens DCPromoWizardController.
 * ================================================================
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class RoleDisplayEntry
{
    public ServerRole role;
    public string     displayName;
}

public class ServerManagerController : MonoBehaviour
{
    public static ServerManagerController Instance { get; private set; }

    // ── TitleBar / TopBar nav ─────────────────────────────────────────────────

    [Header("Nav")]
    [SerializeField] private Button closeBtn;
    [SerializeField] private Button addRolesBtn;
    [SerializeField] private Button toolsMenuBtn;

    // ── Tools Dropdown ────────────────────────────────────────────────────────

    [Header("Tools Dropdown")]
    [SerializeField] private GameObject toolsDropdown;
    [SerializeField] private Button toolADUCBtn;
    [SerializeField] private Button toolDNSBtn;
    [SerializeField] private Button toolDHCPBtn;
    [SerializeField] private Button toolGPMBtn;
    [SerializeField] private Button toolFSRMBtn;
    [SerializeField] private Button toolPrintMgmtBtn;
    [SerializeField] private Button toolRDSBtn;

    // ── Notification & Gate ───────────────────────────────────────────────────

    [Header("Notification & Gate")]
    [SerializeField] private GameObject notificationArea;
    [SerializeField] private Button     promotionFlag;
    [SerializeField] private GameObject gateOverlay;

    // ── Tree Panel ────────────────────────────────────────────────────────────

    [Header("Tree Panel")]
    [SerializeField] private Button    dashboardTreeBtn;
    [SerializeField] private Button    localServerTreeBtn;
    [SerializeField] private Transform roleButtonsContainer;
    [SerializeField] private GameObject treeRoleButtonPrefab;

    // ── Main Panel — Content Views ────────────────────────────────────────────

    [Header("Main Panel — Views")]
    [SerializeField] private GameObject dashboardPanelGO;
    [SerializeField] private GameObject localServerPanelGO;
    [SerializeField] private GameObject rolePlaceholderPanelGO;

    // ── Panel Controllers ─────────────────────────────────────────────────────

    [Header("Panel Controllers")]
    [SerializeField] private DashboardPanelController   dashboardPanel;
    [SerializeField] private LocalServerPanelController localServerPanel;

    // ── Role Display Mapping ──────────────────────────────────────────────────

    [Header("Role Display Names (configure all 6 entries in Inspector)")]
    [SerializeField] private RoleDisplayEntry[] roleDisplayEntries;

    // ── Tree Button Colors ────────────────────────────────────────────────────

    [Header("Tree Button Colors")]
    [SerializeField] private Color treeButtonNormalColor   = Color.white;
    [SerializeField] private Color treeButtonSelectedColor = new Color(0.18f, 0.36f, 0.62f, 1f);

    // ── App References ────────────────────────────────────────────────────────

    [Header("App References")]
    [SerializeField] private RoleWizardController      roleWizard;
    [SerializeField] private DCPromoWizardController   dcPromoWizard;
    [SerializeField] private ADUCController            aduc;
    [SerializeField] private DHCPConsoleController     dhcp;
    [SerializeField] private GroupPolicyController     gpm;
    [SerializeField] private FileServerRMController    fsrm;
    [SerializeField] private PrintManagementController printMgmt;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private readonly List<Button> _treeRoleButtons = new List<Button>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Title / nav
        closeBtn?.onClick.AddListener(Close);
        addRolesBtn?.onClick.AddListener(OpenAddRoles);
        toolsMenuBtn?.onClick.AddListener(ToggleToolsMenu);
        promotionFlag?.onClick.AddListener(OpenDCPromo);

        // Tools dropdown
        toolDNSBtn?.onClick.AddListener(() => { CloseToolsMenu(); DNSManagerController.Instance?.Open(); });
        toolADUCBtn?.onClick.AddListener(()  => { CloseToolsMenu(); aduc?.Open(); });
        toolDHCPBtn?.onClick.AddListener(()  => { CloseToolsMenu(); dhcp?.Open(); });
        toolGPMBtn?.onClick.AddListener(()   => { CloseToolsMenu(); gpm?.Open(); });
        toolFSRMBtn?.onClick.AddListener(()  => { CloseToolsMenu(); fsrm?.Open(); });
        toolPrintMgmtBtn?.onClick.AddListener(() => { CloseToolsMenu(); printMgmt?.Open(); });
        toolRDSBtn?.onClick.AddListener(() =>
        {
            CloseToolsMenu();
            ActivityLogManager.Log(
                "Remote Desktop Services is installed. Use the Remote Desktop Connection app on the desktop.",
                ActivityLogManager.EntryType.Action);
        });

        // Tree panel static buttons
        dashboardTreeBtn?.onClick.AddListener(ShowDashboard);
        localServerTreeBtn?.onClick.AddListener(ShowLocalServer);

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

        RefreshTreeRoleButtons();
        ShowDashboard();

        ActivityLogManager.Log("Opened Server Manager", ActivityLogManager.EntryType.Action);
    }

    public void Close()
    {
        CloseToolsMenu();
        gameObject.SetActive(false);
    }

    // ── Main Panel Navigation ─────────────────────────────────────────────────

    public void ShowDashboard()
    {
        SetMainPanel(dashboardPanelGO);
        SelectTreeButton(dashboardTreeBtn);
        dashboardPanel?.Refresh(GetInstalledRoleNames());
    }

    public void ShowLocalServer()
    {
        SetMainPanel(localServerPanelGO);
        SelectTreeButton(localServerTreeBtn);
        localServerPanel?.Open();
    }

    public void ShowRolePlaceholder(Button selectedBtn)
    {
        SetMainPanel(rolePlaceholderPanelGO);
        SelectTreeButton(selectedBtn);
    }

    // ── Tree Panel Management ─────────────────────────────────────────────────

    /// <summary>
    /// Destroys and recreates dynamic role buttons in the tree panel.
    /// Called on Open() and by RoleWizardController.Install() immediately after a role installs.
    /// </summary>
    public void RefreshTreeRoleButtons()
    {
        // SetParent(null) detaches each child from the container immediately so
        // ForceRebuildLayoutImmediate below only counts the new buttons, not the
        // stale ones that Destroy() won't actually remove until end-of-frame.
        if (roleButtonsContainer != null)
        {
            for (int i = roleButtonsContainer.childCount - 1; i >= 0; i--)
            {
                var child = roleButtonsContainer.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }
        _treeRoleButtons.Clear();

        if (treeRoleButtonPrefab == null || roleButtonsContainer == null) return;

        foreach (var name in GetInstalledRoleNames())
        {
            var go  = Instantiate(treeRoleButtonPrefab, roleButtonsContainer);
            var btn = go.GetComponent<Button>();
            var tmp = go.GetComponentInChildren<TMP_Text>();

            if (tmp != null) tmp.text = name;

            if (btn != null)
            {
                // Reset color to normal on creation
                SetTreeButtonColor(btn, treeButtonNormalColor);

                var captured = btn;
                btn.onClick.AddListener(() => ShowRolePlaceholder(captured));
                _treeRoleButtons.Add(btn);
            }
        }

        // Rebuild container first (ContentSizeFitter recalculates its height),
        // then rebuild Tree Panel so its own layout reflects the new container size.
        if (roleButtonsContainer is RectTransform rt)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            if (rt.parent is RectTransform treePanel)
                LayoutRebuilder.ForceRebuildLayoutImmediate(treePanel);
        }
    }

    /// <summary>
    /// Refreshes only the dashboard content (tiles + summary counter).
    /// Called by RoleWizardController.Install() alongside RefreshTreeRoleButtons().
    /// </summary>
    public void RefreshDashboard()
    {
        dashboardPanel?.Refresh(GetInstalledRoleNames());
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void SetMainPanel(GameObject target)
    {
        dashboardPanelGO?.SetActive(false);
        localServerPanelGO?.SetActive(false);
        rolePlaceholderPanelGO?.SetActive(false);
        target?.SetActive(true);
    }

    private void SelectTreeButton(Button btn)
    {
        // Reset all tree buttons to normal color
        SetTreeButtonColor(dashboardTreeBtn,   treeButtonNormalColor);
        SetTreeButtonColor(localServerTreeBtn, treeButtonNormalColor);
        foreach (var b in _treeRoleButtons)
            SetTreeButtonColor(b, treeButtonNormalColor);

        // Highlight selected
        SetTreeButtonColor(btn, treeButtonSelectedColor);
    }

    private static void SetTreeButtonColor(Button btn, Color color)
    {
        if (btn == null) return;
        var img = btn.targetGraphic as Image;
        if (img != null) img.color = color;
    }

    private List<string> GetInstalledRoleNames()
    {
        var result = new List<string>();
        var state  = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null || roleDisplayEntries == null) return result;

        foreach (var entry in roleDisplayEntries)
        {
            if (IsRoleInstalled(state, entry.role))
                result.Add(entry.displayName);
        }
        return result;
    }

    private static bool IsRoleInstalled(ServerDeviceState s, ServerRole role) => role switch
    {
        ServerRole.DNS                   => s.DNSInstalled,
        ServerRole.ADDS                  => s.ADDSInstalled,
        ServerRole.DHCP                  => s.DHCPInstalled,
        ServerRole.FileServices          => s.FileServicesInstalled,
        ServerRole.PrintServices         => s.PrintServicesInstalled,
        ServerRole.RemoteDesktopServices => s.RDServicesInstalled,
        _                                => false
    };

    private void RefreshGate()
    {
        if (gateOverlay == null) return;
        var state = ServerVirtualOSManager.Instance?.ServerState;
        bool ready = state != null && state.PCRenamed && state.IPConfigured;
        gateOverlay.SetActive(!ready);
        if (addRolesBtn  != null) addRolesBtn.interactable  = ready;
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
        toolsDropdown?.SetActive(toolsDropdown != null && !toolsDropdown.activeSelf);
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
        ActivityLogManager.Log(
            "Clicked promotion notification — opening dcpromo wizard",
            ActivityLogManager.EntryType.Action);
    }
}
