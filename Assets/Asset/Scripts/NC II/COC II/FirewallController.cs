/*
 * ================================================================
 *  UNITY SETUP GUIDE — FirewallController
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to "Windows Defender Firewall Panel".
 *
 *  HIERARCHY
 *    Windows Defender Firewall Panel  (this script here)
 *      ├── SideNav
 *      │     └── Firewall_ON-OFF  → firewallOnOffBtn (Button)
 *      └── MainContent
 *            ├── Default                                → defaultPanel (active by default)
 *            └── Windows Defender Firewall ON-OFF panel → onOffPanel (inactive by default)
 *                  ├── HeaderTMP
 *                  ├── subHeaderTMP
 *                  ├── PrivateNetworkSettingPanel
 *                  │     └── Panel
 *                  │           ├── On_Private  → onPrivateBtn
 *                  │           └── Off_Private → offPrivateBtn
 *                  ├── PublicNetworkSettingPanel
 *                  │     └── Panel
 *                  │           ├── On_Public   → onPublicBtn
 *                  │           └── Off_Public  → offPublicBtn
 *                  └── DecisionPanel
 *                        ├── OKBtn     → Button OnClick: FirewallController.OnOK()
 *                        └── CancelBtn → Button OnClick: FirewallController.OnCancel()
 *
 *  INSPECTOR ASSIGNMENTS
 *    defaultPanel    → MainContent > Default
 *    onOffPanel      → MainContent > Windows Defender Firewall ON-OFF panel
 *    onPrivateBtn    → On_Private button
 *    offPrivateBtn   → Off_Private button
 *    onPublicBtn     → On_Public button
 *    offPublicBtn    → Off_Public button
 *    selectedColor   → highlight color for the active button in each pair
 *    defaultColor    → normal color
 *
 *  BUTTON OnClick WIRING
 *    Firewall_ON-OFF → FirewallController.OpenOnOffPanel()
 *    On_Private      → FirewallController.SetPrivateFirewall(true)
 *    Off_Private     → FirewallController.SetPrivateFirewall(false)
 *    On_Public       → FirewallController.SetPublicFirewall(true)
 *    Off_Public      → FirewallController.SetPublicFirewall(false)
 *    OKBtn           → FirewallController.OnOK()
 *    CancelBtn       → FirewallController.OnCancel()
 *
 *  HOW IT WORKS
 *    Default panel shows firewall status overview.
 *    Clicking Firewall_ON-OFF swaps to the ON-OFF configuration panel.
 *    Student sets both Private and Public to Off, then clicks OK.
 *    Cancel reverts to the state when the panel was last opened.
 * ================================================================
 */

using UnityEngine;
using UnityEngine.UI;

public class FirewallController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject defaultPanel;
    [SerializeField] private GameObject onOffPanel;

    [Header("Private Network Buttons")]
    [SerializeField] private Button onPrivateBtn;
    [SerializeField] private Button offPrivateBtn;

    [Header("Public Network Buttons")]
    [SerializeField] private Button onPublicBtn;
    [SerializeField] private Button offPublicBtn;

    [Header("Colors")]
    [SerializeField] private Color selectedColor = new Color(0.35f, 0.65f, 0.35f); // soft green
    [SerializeField] private Color defaultColor  = new Color(0.55f, 0.55f, 0.55f); // gray

    private bool _privateOff;
    private bool _publicOff;

    // Snapshot for Cancel
    private bool _snapPrivate, _snapPublic;

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        defaultPanel?.SetActive(true);
        onOffPanel?.SetActive(false);
    }

    private void OnEnable()
    {
        defaultPanel?.SetActive(true);
        onOffPanel?.SetActive(false);
        LoadFromState();
        TakeSnapshot();
    }

    // ----------------------------------------------------------------
    //  Public — wired to buttons
    // ----------------------------------------------------------------

    public void OpenOnOffPanel()
    {
        defaultPanel?.SetActive(false);
        onOffPanel?.SetActive(true);
        Debug.Log("[FirewallController] Opened ON-OFF configuration panel.");
    }

    public void SetPrivateFirewall(bool firewallOn)
    {
        _privateOff = !firewallOn;
        SetButtonHighlight(onPrivateBtn, offPrivateBtn, firewallOn);
        Debug.Log($"[FirewallController] Private firewall off = {_privateOff}.");
    }

    public void SetPublicFirewall(bool firewallOn)
    {
        _publicOff = !firewallOn;
        SetButtonHighlight(onPublicBtn, offPublicBtn, firewallOn);
        Debug.Log($"[FirewallController] Public firewall off = {_publicOff}.");
    }

    public void OnOK()
    {
        SaveToState();
        onOffPanel?.SetActive(false);
        defaultPanel?.SetActive(true);
        Debug.Log("[FirewallController] OK — firewall settings saved.");
    }

    public void OnCancel()
    {
        _privateOff = _snapPrivate;
        _publicOff  = _snapPublic;
        RefreshUI();
        onOffPanel?.SetActive(false);
        defaultPanel?.SetActive(true);
        Debug.Log("[FirewallController] Cancel — reverted.");
    }

    // ----------------------------------------------------------------
    //  State sync
    // ----------------------------------------------------------------

    private static DeviceOSState GetState() =>
        VirtualOSManager.Instance?.CurrentState ?? new DeviceOSState();

    private void LoadFromState()
    {
        DeviceOSState s = GetState();
        _privateOff = s.FirewallPrivateOff;
        _publicOff  = s.FirewallPublicOff;
        RefreshUI();
    }

    private void SaveToState()
    {
        DeviceOSState s = GetState();
        s.FirewallPrivateOff = _privateOff;
        s.FirewallPublicOff  = _publicOff;
    }

    private void TakeSnapshot()
    {
        _snapPrivate = _privateOff;
        _snapPublic  = _publicOff;
    }

    private void RefreshUI()
    {
        SetButtonHighlight(onPrivateBtn, offPrivateBtn, !_privateOff);
        SetButtonHighlight(onPublicBtn,  offPublicBtn,  !_publicOff);
    }

    // ----------------------------------------------------------------
    //  UI helpers
    // ----------------------------------------------------------------

    private void SetButtonHighlight(Button onBtn, Button offBtn, bool onSelected)
    {
        SetColor(onBtn,  onSelected  ? selectedColor : defaultColor);
        SetColor(offBtn, !onSelected ? selectedColor : defaultColor);
    }

    private void SetColor(Button btn, Color c)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = c;
    }
}
