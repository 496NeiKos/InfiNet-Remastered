/*
 * ================================================================
 *  UNITY SETUP GUIDE — SystemPropertiesRemoteController (COC III)
 * ================================================================
 *  SCOPE NOTE
 *    This script is UNRELATED to SystemPropertiesController.cs (which handles
 *    the Client PC domain-join chain). This script is the Server-side Remote
 *    tab dialog, opened from Local Server Panel inside Server Manager.
 *
 *  COMPONENT PLACEMENT
 *    Add to "System Properties Remote Panel", a direct child of "Server Manager Panel"
 *    (sibling of Main Panel, Tree Panel, ToolsDropdown, Roles Panel, Gate Overlay).
 *    Starts INACTIVE. Opened by LocalServerPanelController.remoteDesktopBtn.
 *    Positioned to overlay the entire Server Manager window (anchor stretch-stretch
 *    with a semi-transparent backdrop or a centered fixed-size card).
 *
 *  HIERARCHY
 *    System Properties Remote Panel       ← this script here (INACTIVE)
 *      │
 *      ├── TitleBar
 *      │     ├── TitleTMP                 TMP_Text  "System Properties"
 *      │     └── CloseTitleBtn            Button  → closeTitleBtn
 *      │           (same behavior as Cancel — restores snapshot and closes)
 *      │
 *      ├── RemoteAssistanceGroup          Image (group panel)
 *      │     ├── GroupHeaderTMP           TMP_Text  "Remote Assistance"
 *      │     ├── AllowRemoteAssistanceToggle  Toggle  → allowRemoteAssistanceToggle
 *      │     │     isOn: false | interactable: false
 *      │     │     Child Label TMP: "Allow Remote Assistance connections to this computer"
 *      │     └── AdvancedBtn              Button  → advancedBtn
 *      │           interactable: false
 *      │           Child TMP: "Advanced..."
 *      │
 *      ├── RemoteDesktopGroup             Image (group panel)
 *      │     ├── GroupHeaderTMP           TMP_Text  "Remote Desktop"
 *      │     ├── DontAllowToggle          Toggle  → dontAllowToggle
 *      │     │     isOn: true (default)
 *      │     │     Child Label TMP: "Don't allow remote connections to this computer"
 *      │     ├── AllowToggle              Toggle  → allowToggle
 *      │     │     isOn: false (default)
 *      │     │     Child Label TMP: "Allow remote desktop connections to this computer"
 *      │     └── NLAGroup                 GameObject  → nlaGroup  (starts INACTIVE)
 *      │           └── NLAToggle          Toggle  → nlaToggle
 *      │                 isOn: true (default when NLAGroup is shown)
 *      │                 Child Label TMP: "Allow connections only from computers running
 *      │                                  Remote Desktop with Network Level Authentication
 *      │                                  (recommended)"
 *      │
 *      ├── HelpMeChooseBtn                Button  → helpMeChooseBtn
 *      │     interactable: false
 *      │     Child TMP: "Help me choose"
 *      │
 *      ├── SelectUsersBtn                 Button  → selectUsersBtn
 *      │     interactable: false
 *      │     Child TMP: "Select Users..."
 *      │
 *      └── Footer
 *            ├── OKBtn                    Button  → okBtn     Child TMP: "OK"
 *            ├── CancelBtn                Button  → cancelBtn Child TMP: "Cancel"
 *            └── ApplyBtn                 Button  → applyBtn  Child TMP: "Apply"
 *
 *  TOGGLE NOTES
 *    DontAllowToggle and AllowToggle are mutually exclusive radio buttons.
 *    They are NOT in a Unity ToggleGroup — exclusivity is handled manually in
 *    OnDontAllowChanged / OnAllowChanged using SetIsOnWithoutNotify, consistent
 *    with the pattern used throughout the rest of this project.
 *    Do NOT assign them to a ToggleGroup component.
 *
 *  SNAPSHOT / CANCEL PATTERN
 *    On Open(): snapshot = state.RemoteDesktopEnabled
 *    Apply()  : writes state.RemoteDesktopEnabled (snapshot not updated)
 *    Cancel() : restores state.RemoteDesktopEnabled = snapshot (undoes any Apply
 *               calls made during this session), then closes.
 *    This matches the FolderPropertiesController / FolderAdvancedSharingController
 *    pattern used throughout COC III.
 *
 *  INSPECTOR ASSIGNMENTS
 *    closeTitleBtn          → TitleBar/CloseTitleBtn
 *    allowRemoteAssistanceToggle → RemoteAssistanceGroup/AllowRemoteAssistanceToggle
 *    advancedBtn            → RemoteAssistanceGroup/AdvancedBtn
 *    dontAllowToggle        → RemoteDesktopGroup/DontAllowToggle
 *    allowToggle            → RemoteDesktopGroup/AllowToggle
 *    nlaGroup               → RemoteDesktopGroup/NLAGroup
 *    nlaToggle              → RemoteDesktopGroup/NLAGroup/NLAToggle
 *    helpMeChooseBtn        → HelpMeChooseBtn
 *    selectUsersBtn         → SelectUsersBtn
 *    okBtn                  → Footer/OKBtn
 *    cancelBtn              → Footer/CancelBtn
 *    applyBtn               → Footer/ApplyBtn
 *    localServerPanel       → LocalServerPanelController on "Local Server Panel"
 *
 *  HOW IT WORKS
 *    Open() reads state.RemoteDesktopEnabled, sets toggle states, and shows the panel.
 *    DontAllow ↔ Allow toggles are mutually exclusive; NLAGroup shows only when Allow is on.
 *    Apply() writes the selection to state and calls LocalServerPanelController.Refresh()
 *    so the "Remote Desktop: Enabled/Disabled" label in the Local Server panel updates live.
 *    OK = Apply + Close. Cancel = restore snapshot + Close.
 * ================================================================
 */

using UnityEngine;
using UnityEngine.UI;

public class SystemPropertiesRemoteController : MonoBehaviour
{
    [Header("Title Bar")]
    [SerializeField] private Button closeTitleBtn;

    [Header("Remote Assistance Group (permanently disabled)")]
    [SerializeField] private Toggle allowRemoteAssistanceToggle;
    [SerializeField] private Button advancedBtn;

    [Header("Remote Desktop Group")]
    [SerializeField] private Toggle     dontAllowToggle;
    [SerializeField] private Toggle     allowToggle;
    [SerializeField] private GameObject nlaGroup;
    [SerializeField] private Toggle     nlaToggle;

    [Header("Disabled Buttons")]
    [SerializeField] private Button helpMeChooseBtn;
    [SerializeField] private Button selectUsersBtn;

    [Header("Footer")]
    [SerializeField] private Button okBtn;
    [SerializeField] private Button cancelBtn;
    [SerializeField] private Button applyBtn;

    [Header("References")]
    [SerializeField] private LocalServerPanelController localServerPanel;

    private bool _snapshot;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        dontAllowToggle?.onValueChanged.AddListener(OnDontAllowChanged);
        allowToggle?.onValueChanged.AddListener(OnAllowChanged);

        okBtn?.onClick.AddListener(() => { Apply(); Close(); });
        cancelBtn?.onClick.AddListener(Cancel);
        closeTitleBtn?.onClick.AddListener(Cancel);
        applyBtn?.onClick.AddListener(Apply);

        // Permanently disabled — Remote Assistance not simulated
        if (allowRemoteAssistanceToggle != null)
        {
            allowRemoteAssistanceToggle.SetIsOnWithoutNotify(false);
            allowRemoteAssistanceToggle.interactable = false;
        }
        if (advancedBtn    != null) advancedBtn.interactable    = false;
        if (helpMeChooseBtn != null) helpMeChooseBtn.interactable = false;
        if (selectUsersBtn  != null) selectUsersBtn.interactable  = false;

        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;

        _snapshot = state.RemoteDesktopEnabled;

        bool allowed = state.RemoteDesktopEnabled;
        dontAllowToggle?.SetIsOnWithoutNotify(!allowed);
        allowToggle?.SetIsOnWithoutNotify(allowed);
        nlaGroup?.SetActive(allowed);
        nlaToggle?.SetIsOnWithoutNotify(true);

        gameObject.SetActive(true);
        ActivityLogManager.Log("Opened System Properties — Remote", ActivityLogManager.EntryType.Action);
    }

    // ── Toggle handlers ───────────────────────────────────────────────────────

    private void OnDontAllowChanged(bool on)
    {
        if (!on) return;
        allowToggle?.SetIsOnWithoutNotify(false);
        nlaGroup?.SetActive(false);
    }

    private void OnAllowChanged(bool on)
    {
        if (!on) return;
        dontAllowToggle?.SetIsOnWithoutNotify(false);
        nlaGroup?.SetActive(true);
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private void Apply()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state == null) return;

        state.RemoteDesktopEnabled = allowToggle != null && allowToggle.isOn;
        localServerPanel?.Refresh();
        ActivityLogManager.Log(
            $"Remote Desktop: {(state.RemoteDesktopEnabled ? "Enabled" : "Disabled")}",
            ActivityLogManager.EntryType.Action);
    }

    private void Cancel()
    {
        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null) state.RemoteDesktopEnabled = _snapshot;
        localServerPanel?.Refresh();
        Close();
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }
}
