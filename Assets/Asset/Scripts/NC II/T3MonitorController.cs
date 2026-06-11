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
 *  STEP 3 — Build the two panel children inside UEFICanvas
 *
 *    UEFICanvas
 *      ├─ LoadingPanel          ← new; start INACTIVE
 *      │    (UEFILoadingPanel script goes here)
 *      │    ├─ [your art / logo]
 *      │    ├─ HintText         ← start INACTIVE; shown after inputDelay
 *      │    └─ BootPopup        ← start INACTIVE; shown on timeout
 *      │         └─ MessageText (TMP_Text)
 *      │
 *      └─ UEFIPanel             ← new wrapper; start INACTIVE
 *           ├─ Navbar
 *           │    ├─ Tab_Main     → UEFINavigator.GoToTab(0)
 *           │    ├─ Tab_Advanced → UEFINavigator.GoToTab(1)
 *           │    ├─ Tab_Boot     → UEFINavigator.GoToTab(2)
 *           │    ├─ Tab_Security → UEFINavigator.GoToTab(3)
 *           │    └─ Tab_Exit     → UEFINavigator.GoToTab(4)
 *           ├─ TabContent
 *           │    ├─ Panel_Main     (index 0)
 *           │    ├─ Panel_Advanced (index 1)
 *           │    ├─ Panel_Boot     (index 2)
 *           │    ├─ Panel_Security (index 3)
 *           │    └─ Panel_Exit     (index 4)
 *           │         └─ SaveExitButton → UEFINavigator.SaveAndExit()
 *           └─ UEFIOptionPopup   ← start INACTIVE
 *
 *  STEP 4 — Wire the inspector
 *    T3MonitorController:
 *      uefiCanvasRoot → UEFICanvas child GameObject
 *      loadingPanel   → LoadingPanel child inside UEFICanvas
 *      uefiPanel      → UEFIPanel wrapper child inside UEFICanvas
 *      navigator      → UEFINavigator on this same root GameObject
 *
 *  HOW IT WORKS
 *    Right-click monitor (system unit must be ON):
 *      • UEFINavigator.UEFIOpened == false
 *          → enable UEFICanvas, enable LoadingPanel
 *            (LoadingPanel handles the DEL/F2 input flow)
 *      • UEFINavigator.UEFIOpened == true
 *          → enable UEFICanvas, enable UEFIPanel, call navigator.Open()
 *            (skips loading panel — only done once per session)
 *    Escape → HideDetail() disables everything.
 *    DEL/F2 in loading panel → OpenUEFIPanel() transitions to UEFI.
 * ================================================================
 */

using UnityEngine;

public class T3MonitorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject uefiCanvasRoot;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject uefiPanel;
    [SerializeField] private GameObject windowsSetupPanel;
    [SerializeField] private UEFINavigator navigator;

    private void Start()
    {
        if (navigator == null)
            navigator = GetComponent<UEFINavigator>();

        // Give the navigator a direct reference to the panel so it can gate F10 via activeSelf.
        navigator?.SetUEFIPanel(uefiPanel);

        uefiCanvasRoot?.SetActive(false);
        loadingPanel?.SetActive(false);
        uefiPanel?.SetActive(false);
        windowsSetupPanel?.SetActive(false);
    }

    // Called by T3MonitorInteraction.ShowDetail() via GameManager.OpenEditorInPlace.
    // Always enters through the LoadingPanel — it resets on power cycle via OnPoweredOn.
    public void ShowDetailAtCenter()
    {
        uefiCanvasRoot?.SetActive(true);
        loadingPanel?.SetActive(true);
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

    // Called by UEFILoadingPanel when DEL/F2 is pressed after the wait period.
    public void OpenUEFIPanel()
    {
        loadingPanel?.SetActive(false);
        uefiPanel?.SetActive(true);
        navigator?.Open();
    }

    // Called by UEFISaveConfirmationPopup.SaveChanges() — closes the editor cleanly after F10 save.
    public void OnBootStateSaved()
    {
        // Go through GameManager so IsEditorOpen is cleared and the player can interact again.
        GameManager.Instance?.CloseEditor();
        Debug.Log("[T3MonitorController] Boot state saved — UEFI canvas closed.");
    }

    // Called by UEFILoadingPanel when the boot state is valid — swaps to Windows Setup.
    // Canvas stays active because WindowsSetupPanel is a child of it.
    public void ProceedToWindowsSetup()
    {
        loadingPanel?.SetActive(false);
        windowsSetupPanel?.SetActive(true);
        Debug.Log("[T3MonitorController] Proceeding to Windows Setup.");
    }
}
