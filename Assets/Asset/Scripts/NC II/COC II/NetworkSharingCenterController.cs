/*
 * ================================================================
 *  UNITY SETUP GUIDE — NetworkSharingCenterController
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to "Network And Sharing Center".
 *
 *  HIERARCHY
 *    Network And Sharing Center  (this script here)
 *      ├── Nav
 *      │     ├── Home  (Button) → GoBack()      → closeBtn
 *      │     └── Exit  (Button) → ClosePanel()  (wire in Inspector — no script field needed)
 *      ├── SideNav
 *      │     ├── SideNavTop
 *      │     │     ├── Adapter Setting          → adapterSettingBtn
 *      │     │     └── Advance sharing settings → advancedSharingBtn
 *      │     └── SideNavBottom
 *      │           └── Firewall                 → firewallBtn
 *      ├── Main Content   (shows when no panel is open)
 *      ├── Network Connections Panel            → panels[0]
 *      ├── Advance Sharing Settings Panel       → panels[1]
 *      └── Windows Defender Firewall Panel      → panels[2]
 *
 *  INSPECTOR ASSIGNMENTS
 *    panels[0]          → Network Connections Panel
 *    panels[1]          → Advance Sharing Settings Panel
 *    panels[2]          → Windows Defender Firewall Panel
 *    adapterSettingBtn  → SideNavTop > Adapter Setting
 *    advancedSharingBtn → SideNavTop > Advance sharing settings
 *    firewallBtn        → SideNavBottom > Firewall
 *    closeBtn           → Nav > Home Button
 *
 *  BUTTON OnClick WIRING (do in Inspector as persistent listeners)
 *    Home (closeBtn)  → NetworkSharingCenterController.GoBack()
 *    Exit             → NetworkSharingCenterController.ClosePanel()
 *    SideNav buttons  → are also auto-wired in Awake (see note below)
 *
 *  NOTE ON DOUBLE-WIRING
 *    SideNav buttons and closeBtn are wired BOTH in the Inspector AND in Awake.
 *    For NavigateTo this is idempotent (safe). For GoBack/CloseCurrentPanel the
 *    second call hits the _currentPanel < 0 guard and is a no-op. No visual bugs.
 *
 *  HOW IT WORKS
 *    SideNav buttons show one panel at a time (closes the previous).
 *    GoBack / CloseCurrentPanel deactivates the current panel → Main Content view.
 *    ClosePanel deactivates the entire NSC GameObject (Exit button).
 *    If a different SideNav button is clicked while a panel is open,
 *    it switches directly to that panel.
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkSharingCenterController : MonoBehaviour
{
    [Header("Content Panels (0=Adapter, 1=Sharing, 2=Firewall)")]
    [SerializeField] private GameObject[] panels = new GameObject[3];

    [Header("SideNav Buttons")]
    [SerializeField] private Button adapterSettingBtn;
    [SerializeField] private Button advancedSharingBtn;
    [SerializeField] private Button firewallBtn;

    [Header("Nav")]
    [Tooltip("Back/close button inside Nav — deactivates the current panel.")]
    [SerializeField] private Button closeBtn;

    [Header("IP Topology Gate")]
    [Tooltip("Full-rect Image child (Raycast Target ON) that blocks NSC interaction. Start INACTIVE.")]
    [SerializeField] private GameObject nscBlocker;
    [Tooltip("TMP_Text child above the panel showing the topology hint. Start INACTIVE.")]
    [SerializeField] private TMP_Text   nscHintTMP;

    private int _currentPanel = -1; // -1 = no panel open (Main Content visible)

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void Update() => UpdateBlocker();

    private void Awake()
    {
        HideAllPanels();

        adapterSettingBtn?.onClick.AddListener(() => NavigateTo(0));
        advancedSharingBtn?.onClick.AddListener(() => NavigateTo(1));
        firewallBtn?.onClick.AddListener(() => NavigateTo(2));
        closeBtn?.onClick.AddListener(CloseCurrentPanel);
    }

    // ----------------------------------------------------------------
    //  State load/save (called by VirtualOSManager)
    // ----------------------------------------------------------------

    public void LoadState(DeviceOSState state)
    {
        HideAllPanels();
        _currentPanel = state.HistoryIndex;
        if (_currentPanel >= 0 && _currentPanel < panels.Length)
        {
            gameObject.SetActive(true);
            panels[_currentPanel]?.SetActive(true);
            UpdateBlocker(); // Set correct blocker state immediately — no one-frame flash.
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void SaveState(DeviceOSState state)
    {
        state.HistoryIndex = _currentPanel;
    }

    // ----------------------------------------------------------------
    //  Public — navigation
    // ----------------------------------------------------------------

    public void NavigateTo(int index)
    {
        if (index < 0 || index >= panels.Length) return;
        HideAllPanels();
        panels[index]?.SetActive(true);
        _currentPanel = index;
        Debug.Log($"[NetworkSharingCenterController] Panel {index} opened.");
    }

    public void CloseCurrentPanel()
    {
        if (_currentPanel < 0) return;
        panels[_currentPanel]?.SetActive(false);
        _currentPanel = -1;
        Debug.Log("[NetworkSharingCenterController] Panel closed — returned to Main Content.");
    }

    // Inspector alias used by the Home button (wired as "GoBack" in the scene).
    public void GoBack() => CloseCurrentPanel();

    // Called by the Exit button — closes the entire NSC panel.
    public void ClosePanel()
    {
        HideAllPanels();
        gameObject.SetActive(false);
        Debug.Log("[NetworkSharingCenterController] NSC closed.");
    }

    // ----------------------------------------------------------------
    //  Private
    // ----------------------------------------------------------------

    private void HideAllPanels()
    {
        foreach (var p in panels) p?.SetActive(false);
        _currentPanel = -1;
    }

    private void UpdateBlocker()
    {
        if (nscBlocker == null) return;
        bool satisfied = VirtualOSManager.Instance != null
                      && VirtualOSManager.Instance.IsIPTopologySatisfied();
        bool shouldBlock = !satisfied;
        nscBlocker.SetActive(shouldBlock);
        if (nscHintTMP != null)
        {
            nscHintTMP.gameObject.SetActive(shouldBlock);
            if (shouldBlock && VirtualOSManager.Instance != null)
                nscHintTMP.text = VirtualOSManager.Instance.GetIPBlockReason();
        }
    }
}
