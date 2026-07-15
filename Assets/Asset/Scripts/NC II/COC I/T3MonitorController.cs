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
 *      uefiLoadingPanel  → UEFILoadingPanel component on LoadingPanel
 *      uefiPanel         → UEFIPanel wrapper child inside UEFICanvas
 *      windowsSetupPanel → WindowsSetupPanel child inside UEFICanvas
 *      navigator         → UEFINavigator on this same root GameObject
 *      systemUnit        → T3SystemUnitController on the T3 System Unit
 *      windows10Manager  → Windows10Manager on Windows10Panel (child of WindowsSetupPanel)
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
 *    State: Windows10
 *      Entered after Windows Setup wizard completes AND W10 password is set up.
 *      Right-click → LoadingPanel plays again → ProceedToWindowsSetup detects
 *      W10_PasswordSet == 1 → skips wizard, calls ProceedToWindows10Panel.
 *      Shutdown button → ResetToLoading() → state = Loading → canvas closed.
 *
 *    Power cycle (OFF → ON):
 *      _panelState resets to Loading; UEFILoadingPanel.ResetState() resets its timer.
 *      navigator.BootStateSaved is NOT cleared — it persists so the next timeout
 *      validation can succeed without pressing F10 again.
 *      If W10_PasswordSet == 1, the Loading timeout routes to Windows10Panel instead
 *      of the setup wizard.
 * ================================================================
 */

using UnityEngine;

public class T3MonitorController : MonoBehaviour
{
    private enum PanelState { Loading, UEFI, WindowsSetup, Windows10 }

    [Header("References")]
    [SerializeField] private GameObject uefiCanvasRoot;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private UEFILoadingPanel uefiLoadingPanel;
    [SerializeField] private GameObject uefiPanel;
    [SerializeField] private GameObject windowsSetupPanel;
    [SerializeField] private UEFINavigator navigator;
    [SerializeField] private T3SystemUnitController systemUnit;

    [Header("Windows10")]
    [Tooltip("Windows10Manager component on the Windows10Panel GameObject (child of WindowsSetupPanel).")]
    [SerializeField] private Windows10Manager windows10Manager;

    private PanelState _panelState = PanelState.Loading;
    private WindowsSetupNavigator _windowsSetupNavigator;

    private void Start()
    {
        if (navigator == null)
            navigator = GetComponent<UEFINavigator>();

        navigator?.SetUEFIPanel(uefiPanel);

        if (windowsSetupPanel != null)
            _windowsSetupNavigator = windowsSetupPanel.GetComponent<WindowsSetupNavigator>();

        // Auto-resolve uefiLoadingPanel from loadingPanel if not wired in the Inspector.
        if (uefiLoadingPanel == null && loadingPanel != null)
        {
            uefiLoadingPanel = loadingPanel.GetComponent<UEFILoadingPanel>();
            if (uefiLoadingPanel != null)
                Debug.Log("[T3MonitorController] uefiLoadingPanel auto-resolved from loadingPanel.");
            else
                Debug.LogError("[T3MonitorController] uefiLoadingPanel is NULL — assign it in the Inspector or add UEFILoadingPanel to LoadingPanel.");
        }

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
        Debug.Log($"[T3MonitorController] ShowDetailAtCenter — _panelState = {_panelState}");
        uefiCanvasRoot?.SetActive(true);

        switch (_panelState)
        {
            case PanelState.Loading:
                Debug.Log($"[T3MonitorController] Activating LoadingPanel — uefiLoadingPanel ref: {(uefiLoadingPanel == null ? "NULL" : "OK")}");
                loadingPanel?.SetActive(true);
                break;
            case PanelState.UEFI:
                uefiPanel?.SetActive(true);
                navigator?.Open();
                break;
            case PanelState.WindowsSetup:
                windowsSetupPanel?.SetActive(true);
                break;
            // After Shutdown the state becomes Loading (via ResetToLoading), so
            // the next right-click always plays LoadingPanel → then routes to Windows10Panel.
            case PanelState.Windows10:
                loadingPanel?.SetActive(true);
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
        // The Accept button in FifthPhase > PrivacySetting marks the setup as complete.
        // Once the user has clicked Accept, every subsequent boot skips SetUpInitialize
        // and SetUpLicenseAgreement and goes straight to the Windows10 login screen.
        if (FifthPhaseManager.PrivacyAccepted)
        {
            ProceedToWindows10Panel();
            return;
        }

        _panelState = PanelState.WindowsSetup;
        loadingPanel?.SetActive(false);
        _windowsSetupNavigator?.ResetToStart();
        windowsSetupPanel?.SetActive(true);
        Debug.Log("[T3MonitorController] Boot validated — proceeding to Windows Setup.");
    }

    // Called by ProceedToWindowsSetup (skip path) after Windows Setup has been completed once.
    // Also available for direct calls if needed.
    public void ProceedToWindows10Panel()
    {
        _panelState = PanelState.Windows10;
        loadingPanel?.SetActive(false);

        // Clean up any leftover setup children before showing the panel, so that
        // SetUpInitialize, SetUpLicenseAgreement, etc. don't bleed through.
        _windowsSetupNavigator?.PrepareForWindows10();

        windowsSetupPanel?.SetActive(true);
        windows10Manager?.gameObject.SetActive(true); // ensure Windows10Panel itself is visible
        windows10Manager?.InitWindows10Panel();
        Debug.Log("[T3MonitorController] Windows10Panel — password login.");
    }

    // Called by Windows10Manager.OnShutdown() to reset the boot cycle.
    // The next right-click will play LoadingPanel again, then route to Windows10Panel.
    public void ResetToLoading()
    {
        Debug.Log($"[T3MonitorController] ResetToLoading — uefiLoadingPanel is {(uefiLoadingPanel == null ? "NULL (not assigned!)" : "assigned")}");

        _panelState = PanelState.Loading;

        // UEFILoadingPanel stays in TimedOut after a successful boot, so we must
        // explicitly reset it here — otherwise its Update() bails immediately next open.
        uefiLoadingPanel?.ResetState();

        // Hide windowsSetupPanel so it doesn't bleed through during the next LoadingPanel phase.
        windowsSetupPanel?.SetActive(false);

        Debug.Log("[T3MonitorController] Reset to Loading state (Shutdown) — done.");
    }
}
