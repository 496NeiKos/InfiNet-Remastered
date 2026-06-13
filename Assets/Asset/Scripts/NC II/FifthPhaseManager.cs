/*
 * ================================================================
 *  UNITY SETUP GUIDE — FifthPhaseManager
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to the FithPhase GameObject.
 *
 *  HIERARCHY
 *
 *    FithPhase  (this script here)
 *      ├─ Region                   → regionPanel
 *      │    ├─ Title
 *      │    ├─ RegionOption        → regionOptionContainer   (all Button children auto-wired)
 *      │    └─ Next                → regionNextButton
 *      ├─ KeyboardLayout           → keyboardLayoutPanel
 *      │    ├─ Title
 *      │    ├─ KeyboardLayoutOption→ keyboardLayoutOptionContainer
 *      │    └─ Next                → keyboardLayoutNextButton
 *      ├─ SecondKeyboardLayout     → secondKeyboardPanel
 *      │    ├─ Title
 *      │    ├─ KeyboardImage       (no script ref needed — just a display image)
 *      │    └─ Footer
 *      │         ├─ Skip           → skipButton
 *      │         └─ Layout         → layoutButton  (disabled on start)
 *      ├─ ConnectToInternet        → connectToInternetPanel
 *      │    ├─ Title
 *      │    ├─ subTitle
 *      │    ├─ InternetOption      (TBD — internet path not yet implemented)
 *      │    └─ NoInternet          → noInternetButton
 *      ├─ NoInternetPanel          → noInternetPanel
 *      │    ├─ Title
 *      │    ├─ Description
 *      │    ├─ InternetConnectionBenefits
 *      │    ├─ LimitedSetup        → limitedSetupButton
 *      │    └─ ConnectNow          → connectNowButton
 *      └─ PrivacySetting           → privacySettingPanel
 *           ├─ Text (TMP)
 *           ├─ Description (1)
 *           ├─ PrivacySettingOption→ privacySettingOptionContainer (toggle buttons auto-wired)
 *           └─ Footer
 *                ├─ Button (Legacy)   → learnMoreButton   (disabled)
 *                └─ Button (Legacy)(1)→ acceptButton
 *
 *    WindowsSetupPanel  (outside FithPhase)
 *      └─ Windows10Panel           → windows10Panel
 *
 *  SELECTION HIGHLIGHT COLOURS
 *    Selected option button  → blue tint  (0.25, 0.55, 1.0, 0.75)
 *    Unselected option button→ white      (1, 1, 1, 1)
 *
 *  PRIVACY TOGGLE COLOURS
 *    Enabled  (default) → green tint  (0.35, 0.85, 0.35, 1)
 *    Disabled           → grey tint   (0.55, 0.55, 0.55, 1)
 *
 * ================================================================
 *  INSPECTOR ASSIGNMENTS
 * ================================================================
 *
 *  Panels
 *    regionPanel              → FithPhase > Region             (GameObject)
 *    keyboardLayoutPanel      → FithPhase > KeyboardLayout
 *    secondKeyboardPanel      → FithPhase > SecondKeyboardLayout
 *    connectToInternetPanel   → FithPhase > ConnectToInternet
 *    noInternetPanel          → FithPhase > NoInternetPanel
 *    privacySettingPanel      → FithPhase > PrivacySetting
 *    windows10Panel           → WindowsSetupPanel > Windows10Panel
 *    windows10Manager         → Windows10Panel (Windows10Manager component)
 *
 *  Option Containers (script finds all Button children automatically)
 *    regionOptionContainer          → Region > RegionOption              (Transform)
 *    keyboardLayoutOptionContainer  → KeyboardLayout > KeyboardLayoutOption
 *    privacySettingOptionContainer  → PrivacySetting > PrivacySettingOption
 *
 *  Navigation Buttons
 *    regionNextButton         → Region > Next                  (Button)
 *    keyboardLayoutNextButton → KeyboardLayout > Next
 *    skipButton               → SecondKeyboardLayout > Footer > Skip
 *    layoutButton             → SecondKeyboardLayout > Footer > Layout  (will be disabled)
 *    noInternetButton         → ConnectToInternet > NoInternet
 *    connectNowButton         → NoInternetPanel > ConnectNow
 *    limitedSetupButton       → NoInternetPanel > LimitedSetup
 *    learnMoreButton          → PrivacySetting > Footer > Button (Legacy)
 *    acceptButton             → PrivacySetting > Footer > Button (Legacy) (1)
 *
 *  BUTTON WIRING (OnClick)
 *    Region > Next                  → FifthPhaseManager.OnRegionNext()
 *    KeyboardLayout > Next          → FifthPhaseManager.OnKeyboardLayoutNext()
 *    SecondKeyboardLayout > Skip    → FifthPhaseManager.OnSkipSecondKeyboard()
 *    ConnectToInternet > NoInternet → FifthPhaseManager.OnNoInternetClicked()
 *    NoInternetPanel > ConnectNow   → FifthPhaseManager.OnConnectNowClicked()
 *    NoInternetPanel > LimitedSetup → FifthPhaseManager.OnLimitedSetupClicked()
 *    PrivacySetting > Accept        → FifthPhaseManager.OnAcceptPrivacy()
 *
 *  NOTE: Option buttons in RegionOption, KeyboardLayoutOption, and
 *  PrivacySettingOption do NOT need OnClick wiring — the script wires
 *  them automatically via GetComponentsInChildren<Button>().
 * ================================================================
 */

using UnityEngine;
using UnityEngine.UI;

public class FifthPhaseManager : MonoBehaviour
{
    // ----------------------------------------------------------------
    //  Panels
    // ----------------------------------------------------------------

    [Header("Panels")]
    [SerializeField] private GameObject regionPanel;
    [SerializeField] private GameObject keyboardLayoutPanel;
    [SerializeField] private GameObject secondKeyboardPanel;
    [SerializeField] private GameObject connectToInternetPanel;
    [SerializeField] private GameObject noInternetPanel;
    [SerializeField] private GameObject privacySettingPanel;
    [SerializeField] private GameObject windows10Panel;

    [Header("Windows10")]
    [SerializeField] private Windows10Manager windows10Manager;

    // ----------------------------------------------------------------
    //  Option containers — children are auto-wired on Start
    // ----------------------------------------------------------------

    [Header("Option Containers  (script wires children automatically)")]
    [SerializeField] private Transform regionOptionContainer;
    [SerializeField] private Transform keyboardLayoutOptionContainer;
    [SerializeField] private Transform privacySettingOptionContainer;

    // ----------------------------------------------------------------
    //  Navigation buttons
    // ----------------------------------------------------------------

    [Header("Region")]
    [SerializeField] private Button regionNextButton;

    [Header("KeyboardLayout")]
    [SerializeField] private Button keyboardLayoutNextButton;

    [Header("SecondKeyboardLayout")]
    [SerializeField] private Button skipButton;
    [SerializeField] private Button layoutButton;     // disabled on start

    [Header("ConnectToInternet")]
    [SerializeField] private Button noInternetButton;

    [Header("NoInternetPanel")]
    [SerializeField] private Button connectNowButton;
    [SerializeField] private Button limitedSetupButton;

    [Header("PrivacySetting")]
    [SerializeField] private Button learnMoreButton;  // disabled on start
    [SerializeField] private Button acceptButton;

    // ----------------------------------------------------------------
    //  Colours
    // ----------------------------------------------------------------

    private static readonly Color SelectedColor  = new Color(0.25f, 0.55f, 1.00f, 0.75f);
    private static readonly Color UnselectedColor = Color.white;
    private static readonly Color ToggleOnColor  = new Color(0.35f, 0.85f, 0.35f, 1.00f);
    private static readonly Color ToggleOffColor = new Color(0.55f, 0.55f, 0.55f, 1.00f);

    // ----------------------------------------------------------------
    //  Runtime state
    // ----------------------------------------------------------------

    private Button selectedRegionButton;
    private Button selectedKeyboardButton;
    private bool[] privacyToggles; // true = enabled

    // ----------------------------------------------------------------
    //  Init — called by WindowsSetupNavigator
    // ----------------------------------------------------------------

    public void InitFifthPhase()
    {
        selectedRegionButton   = null;
        selectedKeyboardButton = null;

        ShowOnly(regionPanel);

        // Disable unimplemented / locked buttons
        SetInteractable(layoutButton,   false);
        SetInteractable(learnMoreButton, false);

        // Region Next starts disabled until a region is selected
        SetInteractable(regionNextButton,        false);
        SetInteractable(keyboardLayoutNextButton, false);

        WireOptionButtons(regionOptionContainer,       OnRegionOptionSelected);
        WireOptionButtons(keyboardLayoutOptionContainer, OnKeyboardOptionSelected);
        WirePrivacyToggles();

        ResetOptionHighlights(regionOptionContainer);
        ResetOptionHighlights(keyboardLayoutOptionContainer);

        Debug.Log("[FifthPhaseManager] Initialised — showing Region panel.");
    }

    private void Start() => InitFifthPhase();

    // ----------------------------------------------------------------
    //  Panel helpers
    // ----------------------------------------------------------------

    private void ShowOnly(GameObject panel)
    {
        regionPanel?.SetActive(false);
        keyboardLayoutPanel?.SetActive(false);
        secondKeyboardPanel?.SetActive(false);
        connectToInternetPanel?.SetActive(false);
        noInternetPanel?.SetActive(false);
        privacySettingPanel?.SetActive(false);

        panel?.SetActive(true);
    }

    // ----------------------------------------------------------------
    //  Option button auto-wiring
    // ----------------------------------------------------------------

    private void WireOptionButtons(Transform container, UnityEngine.Events.UnityAction<Button> callback)
    {
        if (container == null) return;
        foreach (Button btn in container.GetComponentsInChildren<Button>())
        {
            Button captured = btn;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => callback(captured));
        }
    }

    private void ResetOptionHighlights(Transform container)
    {
        if (container == null) return;
        foreach (Button btn in container.GetComponentsInChildren<Button>())
            SetButtonColor(btn, UnselectedColor);
    }

    private static void SetButtonColor(Button btn, Color color)
    {
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    private static void SetInteractable(Button btn, bool value)
    {
        if (btn != null) btn.interactable = value;
    }

    // ----------------------------------------------------------------
    //  Region panel
    // ----------------------------------------------------------------

    private void OnRegionOptionSelected(Button clicked)
    {
        // Deselect previous
        if (selectedRegionButton != null)
            SetButtonColor(selectedRegionButton, UnselectedColor);

        selectedRegionButton = clicked;
        SetButtonColor(clicked, SelectedColor);
        SetInteractable(regionNextButton, true);

        Debug.Log($"[FifthPhaseManager] Region selected: {clicked.gameObject.name}");
    }

    // Wired to: Region > Next > OnClick
    public void OnRegionNext()
    {
        if (selectedRegionButton == null) return;
        ShowOnly(keyboardLayoutPanel);
        Debug.Log("[FifthPhaseManager] → KeyboardLayout");
    }

    // ----------------------------------------------------------------
    //  KeyboardLayout panel
    // ----------------------------------------------------------------

    private void OnKeyboardOptionSelected(Button clicked)
    {
        if (selectedKeyboardButton != null)
            SetButtonColor(selectedKeyboardButton, UnselectedColor);

        selectedKeyboardButton = clicked;
        SetButtonColor(clicked, SelectedColor);
        SetInteractable(keyboardLayoutNextButton, true);

        Debug.Log($"[FifthPhaseManager] Keyboard layout selected: {clicked.gameObject.name}");
    }

    // Wired to: KeyboardLayout > Next > OnClick
    public void OnKeyboardLayoutNext()
    {
        if (selectedKeyboardButton == null) return;
        ShowOnly(secondKeyboardPanel);
        Debug.Log("[FifthPhaseManager] → SecondKeyboardLayout");
    }

    // ----------------------------------------------------------------
    //  SecondKeyboardLayout panel
    //  Layout button is already disabled on Init.
    //  Skip is always interactable.
    // ----------------------------------------------------------------

    // Wired to: SecondKeyboardLayout > Footer > Skip > OnClick
    public void OnSkipSecondKeyboard()
    {
        ShowOnly(connectToInternetPanel);
        Debug.Log("[FifthPhaseManager] → ConnectToInternet");
    }

    // ----------------------------------------------------------------
    //  ConnectToInternet panel
    // ----------------------------------------------------------------

    // Wired to: ConnectToInternet > NoInternet > OnClick
    public void OnNoInternetClicked()
    {
        ShowOnly(noInternetPanel);
        Debug.Log("[FifthPhaseManager] → NoInternetPanel");
    }

    // ----------------------------------------------------------------
    //  NoInternetPanel
    // ----------------------------------------------------------------

    // Wired to: NoInternetPanel > ConnectNow > OnClick
    public void OnConnectNowClicked()
    {
        ShowOnly(connectToInternetPanel);
        Debug.Log("[FifthPhaseManager] ConnectNow → back to ConnectToInternet");
    }

    // Wired to: NoInternetPanel > LimitedSetup > OnClick
    public void OnLimitedSetupClicked()
    {
        ShowOnly(privacySettingPanel);
        Debug.Log("[FifthPhaseManager] → PrivacySetting");
    }

    // ----------------------------------------------------------------
    //  PrivacySetting panel — toggle buttons
    // ----------------------------------------------------------------

    private void WirePrivacyToggles()
    {
        if (privacySettingOptionContainer == null) return;

        Button[] buttons = privacySettingOptionContainer.GetComponentsInChildren<Button>();
        privacyToggles = new bool[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            privacyToggles[i] = true;                // default: enabled
            SetButtonColor(buttons[i], ToggleOnColor);

            int idx = i;
            buttons[idx].onClick.RemoveAllListeners();
            buttons[idx].onClick.AddListener(() => OnPrivacyToggle(buttons[idx], idx));
        }
    }

    private void OnPrivacyToggle(Button btn, int index)
    {
        if (privacyToggles == null || index >= privacyToggles.Length) return;

        privacyToggles[index] = !privacyToggles[index];
        SetButtonColor(btn, privacyToggles[index] ? ToggleOnColor : ToggleOffColor);

        Debug.Log($"[FifthPhaseManager] Privacy toggle [{index}] → {(privacyToggles[index] ? "ON" : "OFF")}");
    }

    // Wired to: PrivacySetting > Footer > Accept button > OnClick
    public void OnAcceptPrivacy()
    {
        gameObject.SetActive(false);
        windows10Panel?.SetActive(true);

        // Resolve manager via inspector ref first, then fall back to GetComponent
        var mgr = windows10Manager;
        if (mgr == null && windows10Panel != null)
            mgr = windows10Panel.GetComponent<Windows10Manager>();

        if (mgr != null)
            mgr.InitWindows10Panel();
        else
            Debug.LogError("[FifthPhaseManager] Windows10Manager not found — assign it in the Inspector or place it on Windows10Panel.");

        Debug.Log("[FifthPhaseManager] Privacy accepted → Windows10Panel");
    }
}
