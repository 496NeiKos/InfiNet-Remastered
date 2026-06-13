/*
 * ================================================================
 *  UNITY SETUP GUIDE — T3MonitorController
 * ================================================================
 *  STEP 1 — Create the UEFI Monitor root GameObject
 *    - Place it inside Topic 3's worldRootContainer.
 *    - Add components: SpriteRenderer (monitor sprite),
 *      Collider2D, T3MonitorInteraction, T3MonitorController,
 *      UEFINavigator.
 *    - Do NOT add DragPrefab — monitor is not draggable.
 *    - This world-space object is what the player right-clicks.
 *
 *  STEP 2 — Create the UEFI canvas as a child
 *    a) Add a child GameObject named "UEFICanvas".
 *    b) Add a Canvas component:
 *         Render Mode:    Screen Space - Camera
 *         Render Camera:  Main Camera
 *         Plane Distance: 100
 *    c) CanvasScaler:
 *         UI Scale Mode:  Constant Pixel Size
 *         Scale Factor:   1
 *    d) Start with UEFICanvas INACTIVE.
 *
 *  STEP 3 — Build the panel children inside UEFICanvas
 *
 *    UEFICanvas
 *      ├─ LoadingPanel          ← start INACTIVE
 *      │    (UEFILoadingPanel script goes here)
 *      │    ├─ [your art / logo]
 *      │    ├─ HintText         ← start INACTIVE; shown after inputDelay
 *      │    └─ BootPopup        ← start INACTIVE; shown on timeout
 *      │         └─ MessageText (TMP_Text)
 *      │
 *      ├─ UEFIPanel             ← start INACTIVE
 *      │    ├─ Navbar
 *      │    │    ├─ Tab_Main     → UEFINavigator.GoToTab(0)
 *      │    │    ├─ Tab_Advanced → UEFINavigator.GoToTab(1)
 *      │    │    ├─ Tab_Boot     → UEFINavigator.GoToTab(2)
 *      │    │    ├─ Tab_Security → UEFINavigator.GoToTab(3)
 *      │    │    └─ Tab_Exit     → UEFINavigator.GoToTab(4)
 *      │    ├─ TabContent
 *      │    │    ├─ Panel_Main     (index 0)
 *      │    │    ├─ Panel_Advanced (index 1)
 *      │    │    ├─ Panel_Boot     (index 2)
 *      │    │    ├─ Panel_Security (index 3)
 *      │    │    └─ Panel_Exit     (index 4)
 *      │    │         └─ SaveExitButton → UEFINavigator.SaveAndExit()
 *      │    └─ UEFIOptionPopup   ← start INACTIVE
 *      │
 *      └─ WindowsSetupPanel     ← start INACTIVE
 *
 *  STEP 4 — Wire the inspector
 *    T3MonitorController:
 *      uefiCanvasRoot    → UEFICanvas child GameObject
 *      loadingPanel      → LoadingPanel child inside UEFICanvas
 *      uefiPanel         → UEFIPanel wrapper child inside UEFICanvas
 *      windowsSetupPanel → WindowsSetupPanel child inside UEFICanvas
 *      navigator         → UEFINavigator on this same root GameObject
 *      systemUnit        → T3SystemUnitController on the T3 System Unit
 *
 *  HOW IT WORKS — Panel state machine (resets on every power cycle)
 *
 *    State: Loading (default after power-on)
 *      Right-click → shows LoadingPanel.
 *      UEFILoadingPanel runs its 1 s delay then 5 s DEL/F2 window.
 *        • DEL/F2 pressed in time  → OpenUEFIPanel()  → state: UEFI
 *        • 5 s expires without key → TransitionToTimedOut():
 *            – BootStateSaved AND AllCorrect() → ProceedToWindowsSetup() → state: WindowsSetup
 *            – otherwise            → BootPopup shows error message
 *
 *    State: UEFI
 *      Right-click (after ESC) → shows UEFIPanel directly (skips loading).
 *      Player configures settings and presses F10 → UEFISaveConfirmationPopup:
 *        • Confirm → CommitSave() latches BootStateSaved, OnBootStateSaved() closes canvas.
 *      Right-click again (no power cycle) → still shows UEFIPanel.
 *
 *    State: WindowsSetup
 *      Right-click (after ESC) → shows WindowsSetupPanel directly.
 *
 *    Power cycle (OFF → ON):
 *      _panelState resets to Loading; UEFILoadingPanel.ResetState() resets its timer.
 *      navigator.BootStateSaved is NOT cleared — it persists so the next timeout
 *      validation can succeed without pressing F10 again.
 * ================================================================
 */

using UnityEngine;

public class T3MonitorController : MonoBehaviour
{
    private enum PanelState { Loading, UEFI, WindowsSetup }

    [Header("References")]
    [SerializeField] private GameObject uefiCanvasRoot;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject uefiPanel;
    [SerializeField] private GameObject windowsSetupPanel;
    [SerializeField] private UEFINavigator navigator;
    [SerializeField] private T3SystemUnitController systemUnit;

    private PanelState _panelState = PanelState.Loading;
    private WindowsSetupNavigator _windowsSetupNavigator;

    private void Start()
    {
        if (navigator == null)
            navigator = GetComponent<UEFINavigator>();

        navigator?.SetUEFIPanel(uefiPanel);

        if (windowsSetupPanel != null)
            _windowsSetupNavigator = windowsSetupPanel.GetComponent<WindowsSetupNavigator>();

        if (systemUnit != null)
            systemUnit.OnPoweredOn += OnPowerCycle;

        uefiCanvasRoot?.SetActive(false);
        loadingPanel?.SetActive(false);
        uefiPanel?.SetActive(false);
        windowsSetupPanel?.SetActive(false);
    }

    private void OnDestroy()
    {
        if (systemUnit != null)
            systemUnit.OnPoweredOn -= OnPowerCycle;
    }

    // Fires on every OFF → ON transition. Resets the panel state so the next
    // right-click always starts from the LoadingPanel, not wherever the player
    // left off in the previous power session.
    private void OnPowerCycle()
    {
        _panelState = PanelState.Loading;
        Debug.Log("[T3MonitorController] Power cycle — panel state reset to Loading.");
    }

    // Called by T3MonitorInteraction.ShowDetail() via GameManager.OpenEditorInPlace.
    // Restores whatever panel the player last reached this power session.
    // ESC only hides the canvas — it does not reset state.
    public void ShowDetailAtCenter()
    {
        uefiCanvasRoot?.SetActive(true);

        switch (_panelState)
        {
            case PanelState.Loading:
                loadingPanel?.SetActive(true);
                break;
            case PanelState.UEFI:
                uefiPanel?.SetActive(true);
                navigator?.Open();
                break;
            case PanelState.WindowsSetup:
                windowsSetupPanel?.SetActive(true);
                break;
        }
    }

    // Called by T3MonitorInteraction.HideDetail() via GameManager.CloseEditor (Escape).
    public void HideDetail()
    {
        navigator?.Close();
        loadingPanel?.SetActive(false);
        uefiPanel?.SetActive(false);
        windowsSetupPanel?.SetActive(false);
        uefiCanvasRoot?.SetActive(false);
    }

    // Called by UEFILoadingPanel when DEL/F2 is pressed during the input window.
    public void OpenUEFIPanel()
    {
        _panelState = PanelState.UEFI;
        loadingPanel?.SetActive(false);
        uefiPanel?.SetActive(true);
        navigator?.Open();
        Debug.Log("[T3MonitorController] Entered UEFI panel.");
    }

    // Called by UEFISaveConfirmationPopup after F10 save — closes the canvas cleanly.
    public void OnBootStateSaved()
    {
        GameManager.Instance?.CloseEditor();
        Debug.Log("[T3MonitorController] Boot state saved — UEFI canvas closed.");
    }

    // Called by UEFILoadingPanel.TransitionToTimedOut() when boot conditions pass.
    public void ProceedToWindowsSetup()
    {
        _panelState = PanelState.WindowsSetup;
        loadingPanel?.SetActive(false);
        _windowsSetupNavigator?.ResetToStart(); // always opens at the beginning
        windowsSetupPanel?.SetActive(true);
        Debug.Log("[T3MonitorController] Boot validated — proceeding to Windows Setup.");
    }
}
