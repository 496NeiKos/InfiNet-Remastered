/*
 * ================================================================
 *  UNITY SETUP GUIDE — EthernetPropertiesController
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to "EthernetPropertiesPanel".
 *
 *  EACH PROTOCOL ROW — two buttons side-by-side
 *    [SelectBtn]  clicking toggles row selection (blue = selected, white = not).
 *                 Selecting IPv4 enables Properties. Selecting IPv6 does not.
 *    [ToggleBtn]  clicking toggles enabled/disabled (green = ON, gray = OFF).
 *                 If IPv4 toggle is OFF, Properties is disabled even if selected.
 *
 *    IPv4 starts: toggle ON (green), not selected.
 *    IPv6 starts: toggle ON (green), not selected. Student must toggle OFF.
 *    Properties button: always VISIBLE, interactable only when IPv4 selected AND ON.
 *
 *  INSPECTOR ASSIGNMENTS
 *    ipv4SelectBtn    → IPv4Btn (existing — the select button for the IPv4 row)
 *    ipv4ToggleBtn    → IPv4ToggleBtn (ADD TO SCENE next to IPv4Btn)
 *    ipv6SelectBtn    → IPv6Btn (existing)
 *    ipv6ToggleBtn    → IPv6ToggleBtn (ADD TO SCENE next to IPv6Btn)
 *    propertiesBtn    → PropertiesBtn inside SelectecItemDecision
 *    ipv4PropertiesPanel → IPv4PropertiesPanel
 *    descriptionTMP   → TMP_Text inside Connection Items Description Panel
 *
 *  BUTTON OnClick — Wire manually in the Inspector (persistent listeners).
 *    Do NOT also add them via AddListener in Awake — that causes double-fire
 *    on every toggle/select, making booleans always return to their start value.
 *
 *  COLORS (applied to each button's own Image component)
 *    Select highlight : blue  (0.29, 0.56, 0.85)
 *    Toggle ON        : green (0.35, 0.65, 0.35)
 *    Toggle OFF       : gray  (0.55, 0.55, 0.55)
 *    Default          : white
 *
 *  IMPLEMENTATION GUIDE (what to add in the Editor)
 *    1. Add "IPv6Btn" next to IPv4Btn in Connection Items Panel
 *       (copy IPv4Btn style, label "Internet Protocol Version 6 (TCP/IPv6)").
 *    2. Add "IPv4ToggleBtn" next to IPv4Btn — small square Button, colored Image.
 *    3. Add "IPv6ToggleBtn" next to IPv6Btn — same style.
 *    4. Add 4-5 display-only TMP_Text labels in Connection Items Panel for realism.
 *    5. PropertiesBtn: keep ACTIVE in scene (always visible), starts non-interactable.
 * ================================================================
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EthernetPropertiesController : MonoBehaviour
{
    [Header("IPv4 Row")]
    [SerializeField] private Button ipv4SelectBtn;
    [SerializeField] private Button ipv4ToggleBtn;

    [Header("IPv6 Row")]
    [SerializeField] private Button ipv6SelectBtn;
    [SerializeField] private Button ipv6ToggleBtn;

    [Header("Decision")]
    [SerializeField] private Button propertiesBtn;

    [Header("Panels")]
    [SerializeField] private GameObject ipv4PropertiesPanel;

    [Header("Description")]
    [SerializeField] private TMP_Text descriptionTMP;

    [Header("Colors")]
    [SerializeField] private Color selectColor   = new Color(0.29f, 0.56f, 0.85f);
    [SerializeField] private Color toggleOnColor = new Color(0.35f, 0.65f, 0.35f);
    [SerializeField] private Color toggleOffColor = new Color(0.55f, 0.55f, 0.55f);
    [SerializeField] private Color defaultColor  = Color.white;

    private bool _ipv4Selected;
    private bool _ipv6Selected;
    private bool _ipv4Enabled = true;
    private bool _ipv6Enabled = true;

    private bool _snapIPv6Enabled;

    private const string IPv4Desc =
        "Transmission Control Protocol/Internet Protocol. Provides communication " +
        "across diverse interconnected networks.";
    private const string IPv6Desc =
        "Internet Protocol Version 6. The latest version of the internet layer " +
        "protocol for packet-switched internetworking.";

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void OnEnable()
    {
        LoadFromState();
        TakeSnapshot();
        _ipv4Selected = false;
        _ipv6Selected = false;
        RefreshAll();
    }

    // ----------------------------------------------------------------
    //  Select buttons
    // ----------------------------------------------------------------

    public void ClickIPv4Select()
    {
        _ipv4Selected = !_ipv4Selected;
        _ipv6Selected = false;
        if (descriptionTMP != null)
            descriptionTMP.text = _ipv4Selected ? IPv4Desc : "";
        RefreshAll();
        Debug.Log($"[EthernetPropertiesController] IPv4 select = {_ipv4Selected}.");
    }

    public void ClickIPv6Select()
    {
        _ipv6Selected = !_ipv6Selected;
        _ipv4Selected = false;
        if (descriptionTMP != null)
            descriptionTMP.text = _ipv6Selected ? IPv6Desc : "";
        RefreshAll();
        Debug.Log($"[EthernetPropertiesController] IPv6 select = {_ipv6Selected}.");
    }

    // ----------------------------------------------------------------
    //  Toggle buttons
    // ----------------------------------------------------------------

    public void ClickIPv4Toggle()
    {
        _ipv4Enabled = !_ipv4Enabled;
        RefreshAll();
        Debug.Log($"[EthernetPropertiesController] IPv4 enabled = {_ipv4Enabled}.");
    }

    public void ClickIPv6Toggle()
    {
        _ipv6Enabled = !_ipv6Enabled;
        GetState().IPv6Unchecked = !_ipv6Enabled;
        RefreshAll();
        Debug.Log($"[EthernetPropertiesController] IPv6 enabled = {_ipv6Enabled}.");
    }

    // ----------------------------------------------------------------
    //  Properties / OK / Cancel
    // ----------------------------------------------------------------

    public void OpenIPv4Properties()
    {
        ipv4PropertiesPanel?.SetActive(true);
        Debug.Log("[EthernetPropertiesController] Opening IPv4PropertiesPanel.");
    }

    public void OnOK()
    {
        gameObject.SetActive(false);
        Debug.Log("[EthernetPropertiesController] OK.");
    }

    public void OnCancel()
    {
        _ipv6Enabled = _snapIPv6Enabled;
        GetState().IPv6Unchecked = !_ipv6Enabled;
        gameObject.SetActive(false);
        Debug.Log("[EthernetPropertiesController] Cancel — reverted.");
    }

    // ----------------------------------------------------------------
    //  State / Snapshot
    // ----------------------------------------------------------------

    private void LoadFromState()
    {
        _ipv6Enabled = !GetState().IPv6Unchecked;
        _ipv4Enabled = true;
    }

    private void TakeSnapshot() => _snapIPv6Enabled = _ipv6Enabled;

    // ----------------------------------------------------------------
    //  UI refresh — single source of truth
    // ----------------------------------------------------------------

    private void RefreshAll()
    {
        // Select highlights on SelectBtns
        SetColor(ipv4SelectBtn, _ipv4Selected ? selectColor : defaultColor);
        SetColor(ipv6SelectBtn, _ipv6Selected ? selectColor : defaultColor);

        // Toggle colors on ToggleBtns
        SetColor(ipv4ToggleBtn, _ipv4Enabled ? toggleOnColor : toggleOffColor);
        SetColor(ipv6ToggleBtn, _ipv6Enabled ? toggleOnColor : toggleOffColor);

        // Properties: interactable only when IPv4 is selected AND enabled
        if (propertiesBtn != null)
            propertiesBtn.interactable = _ipv4Selected && _ipv4Enabled;
    }

    private void SetColor(Button btn, Color c)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = c;
    }

    private static DeviceOSState GetState() =>
        VirtualOSManager.Instance?.CurrentState ?? new DeviceOSState();
}
