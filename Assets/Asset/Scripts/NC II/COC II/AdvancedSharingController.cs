/*
 * ================================================================
 *  UNITY SETUP GUIDE — AdvancedSharingController
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to "Advance Sharing Settings Panel".
 *
 *  HIERARCHY
 *    Advance Sharing Settings Panel  (this script here)
 *      ├── Body
 *      │     ├── Header
 *      │     └── Content
 *      │           ├── Sharing Settings Option 2  → option2Header  (Button)
 *      │           ├── Option 2 Container          → option2Container
 *      │           │     ├── Option 2 Content 1   (Network Discovery)
 *      │           │     │     └── Select Panel
 *      │           │     │           ├── On_ND    → onNDBtn
 *      │           │     │           └── Off_ND   → offNDBtn
 *      │           │     └── Option 2 Content 2   (File and Printer Sharing)
 *      │           │           └── Select Panel
 *      │           │                 ├── On_FPS   → onFPSBtn
 *      │           │                 └── Off_FPS  → offFPSBtn
 *      │           ├── Sharing Settings Option 3  → option3Header  (Button)
 *      │           └── Option 3 Container          → option3Container
 *      │                 ├── Option 3 Content 1   (Public Folder Sharing)
 *      │                 │     └── Select Panel
 *      │                 │           ├── On_PFS   → onPFSBtn
 *      │                 │           └── Off_PFS  → offPFSBtn
 *      │                 ├── Option 3 Content 2   (Media Streaming)
 *      │                 │     └── Select Panel
 *      │                 │           ├── On_MS    → onMSBtn
 *      │                 │           └── Off_MS   → offMSBtn
 *      │                 └── Option 3 Content 3   (Password Protected Sharing)
 *      │                       └── Select Panel
 *      │                             ├── On_PPS   → onPPSBtn
 *      │                             └── Off_PPS  → offPPSBtn
 *      └── DecisionPanel
 *            ├── Save Changes  → Button OnClick: AdvancedSharingController.SaveChanges()
 *            └── Cancel        → Button OnClick: AdvancedSharingController.Cancel()
 *
 *  BUTTON OnClick WIRING
 *    Sharing Settings Option 2  → AdvancedSharingController.ToggleOption2()
 *    Sharing Settings Option 3  → AdvancedSharingController.ToggleOption3()
 *    On_ND    → AdvancedSharingController.SetNetworkDiscovery(true)
 *    Off_ND   → AdvancedSharingController.SetNetworkDiscovery(false)
 *    On_FPS   → AdvancedSharingController.SetFilePrinterSharing(true)
 *    Off_FPS  → AdvancedSharingController.SetFilePrinterSharing(false)
 *    On_PFS   → AdvancedSharingController.SetPublicFolderSharing(true)
 *    Off_PFS  → AdvancedSharingController.SetPublicFolderSharing(false)
 *    On_MS    → AdvancedSharingController.SetMediaStreaming(true)
 *    Off_MS   → AdvancedSharingController.SetMediaStreaming(false)
 *    On_PPS   → AdvancedSharingController.SetPasswordSharing(true)
 *    Off_PPS  → AdvancedSharingController.SetPasswordSharing(false)
 *    Save Changes → AdvancedSharingController.SaveChanges()
 *    Cancel       → AdvancedSharingController.Cancel()
 *
 *  INSPECTOR ASSIGNMENTS
 *    Assign all button references and container GameObjects as listed above.
 *    selectedColor   → color tint applied to the "On" or "Off" button that is
 *                      currently active (e.g., a slightly darker shade)
 *    defaultColor    → normal button color
 * ================================================================
 */

using UnityEngine;
using UnityEngine.UI;

public class AdvancedSharingController : MonoBehaviour
{
    [Header("Accordion Containers")]
    [SerializeField] private GameObject option2Container;
    [SerializeField] private GameObject option3Container;

    [Header("Option 2 — Guest/Public (Network Discovery + File Sharing)")]
    [SerializeField] private Button onNDBtn;
    [SerializeField] private Button offNDBtn;
    [SerializeField] private Button onFPSBtn;
    [SerializeField] private Button offFPSBtn;

    [Header("Option 3 — All Networks")]
    [SerializeField] private Button onPFSBtn;
    [SerializeField] private Button offPFSBtn;
    [SerializeField] private Button onMSBtn;
    [SerializeField] private Button offMSBtn;
    [SerializeField] private Button onPPSBtn;
    [SerializeField] private Button offPPSBtn;

    [Header("Colors")]
    [SerializeField] private Color selectedColor = new Color(0.35f, 0.65f, 0.35f); // soft green
    [SerializeField] private Color defaultColor  = new Color(0.55f, 0.55f, 0.55f); // gray

    // Working copies (committed on Save Changes)
    private bool _ndOn, _fpsOn, _pfsOn, _msOn, _ppsOff;

    // Snapshot for Cancel
    private bool _snapND, _snapFPS, _snapPFS, _snapMS, _snapPPS;

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        option2Container?.SetActive(false);
        option3Container?.SetActive(false);
    }

    private void OnEnable()
    {
        LoadFromState();
        TakeSnapshot();
    }

    // ----------------------------------------------------------------
    //  Accordion — only one open at a time
    // ----------------------------------------------------------------

    public void ToggleOption2()
    {
        bool willOpen = !option2Container.activeSelf;
        option2Container?.SetActive(willOpen);
        if (willOpen) option3Container?.SetActive(false);
        Debug.Log($"[AdvancedSharingController] Option2 container {(willOpen ? "opened" : "closed")}.");
    }

    public void ToggleOption3()
    {
        bool willOpen = !option3Container.activeSelf;
        option3Container?.SetActive(willOpen);
        if (willOpen) option2Container?.SetActive(false);
        Debug.Log($"[AdvancedSharingController] Option3 container {(willOpen ? "opened" : "closed")}.");
    }

    // ----------------------------------------------------------------
    //  Setting toggles
    // ----------------------------------------------------------------

    public void SetNetworkDiscovery(bool on)
    {
        _ndOn = on;
        SetButtonHighlight(onNDBtn, offNDBtn, on);
        Debug.Log($"[AdvancedSharingController] NetworkDiscovery = {on}.");
    }

    public void SetFilePrinterSharing(bool on)
    {
        _fpsOn = on;
        SetButtonHighlight(onFPSBtn, offFPSBtn, on);
        Debug.Log($"[AdvancedSharingController] FilePrinterSharing = {on}.");
    }

    public void SetPublicFolderSharing(bool on)
    {
        _pfsOn = on;
        SetButtonHighlight(onPFSBtn, offPFSBtn, on);
        Debug.Log($"[AdvancedSharingController] PublicFolderSharing = {on}.");
    }

    public void SetMediaStreaming(bool on)
    {
        _msOn = on;
        SetButtonHighlight(onMSBtn, offMSBtn, on);
        Debug.Log($"[AdvancedSharingController] MediaStreaming = {on}.");
    }

    // Password protected sharing: On_PPS = password ON, Off_PPS = password OFF.
    // TESDA requires password protection to be OFF, so student clicks Off_PPS.
    public void SetPasswordSharing(bool on)
    {
        _ppsOff = !on; // ppsOff is true when the student pressed Off
        SetButtonHighlight(onPPSBtn, offPPSBtn, on);
        Debug.Log($"[AdvancedSharingController] PasswordProtectedSharingOff = {_ppsOff}.");
    }

    // ----------------------------------------------------------------
    //  Save / Cancel
    // ----------------------------------------------------------------

    public void SaveChanges()
    {
        DeviceOSState s = GetState();

        s.NetworkDiscoveryOn         = _ndOn;
        s.FilePrinterSharingOn       = _fpsOn;
        s.PublicFolderSharingOn      = _pfsOn;
        s.MediaStreamingOn           = _msOn;
        s.PasswordProtectedSharingOff = _ppsOff;

        Debug.Log("[AdvancedSharingController] Sharing settings saved.");
    }

    public void Cancel()
    {
        RestoreSnapshot();
        Debug.Log("[AdvancedSharingController] Cancelled — reverted to snapshot.");
    }

    // ----------------------------------------------------------------
    //  State sync
    // ----------------------------------------------------------------

    private static DeviceOSState GetState() =>
        VirtualOSManager.Instance?.CurrentState ?? new DeviceOSState();

    private void LoadFromState()
    {
        DeviceOSState s = GetState();

        _ndOn  = s.NetworkDiscoveryOn;
        _fpsOn = s.FilePrinterSharingOn;
        _pfsOn = s.PublicFolderSharingOn;
        _msOn  = s.MediaStreamingOn;
        _ppsOff = s.PasswordProtectedSharingOff;

        SetButtonHighlight(onNDBtn,  offNDBtn,  _ndOn);
        SetButtonHighlight(onFPSBtn, offFPSBtn, _fpsOn);
        SetButtonHighlight(onPFSBtn, offPFSBtn, _pfsOn);
        SetButtonHighlight(onMSBtn,  offMSBtn,  _msOn);
        SetButtonHighlight(onPPSBtn, offPPSBtn, !_ppsOff);
    }

    private void TakeSnapshot()
    {
        _snapND  = _ndOn;  _snapFPS = _fpsOn;
        _snapPFS = _pfsOn; _snapMS  = _msOn;
        _snapPPS = _ppsOff;
    }

    private void RestoreSnapshot()
    {
        _ndOn = _snapND; _fpsOn = _snapFPS;
        _pfsOn = _snapPFS; _msOn = _snapMS;
        _ppsOff = _snapPPS;
        LoadFromState();
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
