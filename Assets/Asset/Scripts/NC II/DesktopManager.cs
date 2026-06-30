/*
 * ================================================================
 *  UNITY SETUP GUIDE — DesktopManager
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to the DesktopContent GameObject.
 *
 *  HIERARCHY
 *
 *    DesktopContent  (this script here)
 *      └─ DesktopDisplay
 *           └─ Application
 *                ├─ MicrosoftEdgeApp  → microsoftEdgeButton  (Button — ENABLED by default)
 *                ├─ GoogleChromeApp   → googleChromeButton   (Button — DISABLED by default)
 *                └─ WinRarApp         → winRarButton         (Button — DISABLED by default)
 *
 *  INSPECTOR ASSIGNMENTS
 *    microsoftEdgeButton → Application > MicrosoftEdgeApp (Button component)
 *    googleChromeButton  → Application > GoogleChromeApp  (Button component)
 *    winRarButton        → Application > WinRarApp        (Button component)
 *    microsoftEdgePanel  → Windows10Desktop > WindowsContent > MicrosoftEdgePanel
 *    microsoftEdgeManager → MicrosoftEdgePanel > MicrosoftEdgeManager component
 *
 *  BUTTON OnClick WIRING
 *    MicrosoftEdgeApp Button → DesktopManager.OpenMicrosoftEdge()
 *    GoogleChromeApp Button  → DesktopManager.OpenChromeApp()
 *    WinRarApp Button        → DesktopManager.OpenWinRarApp()
 *
 *  HOW IT WORKS
 *    On Awake all non-Edge app buttons are disabled (non-interactable).
 *    MicrosoftEdge is the only built-in app and stays interactable.
 *    When an app is downloaded via MicrosoftEdgeManager, InstallApp(AppType)
 *    is called and the matching button becomes interactable.
 * ================================================================
 */

using UnityEngine;
using UnityEngine.UI;

public enum AppType { GoogleChrome, WinRar }

public class DesktopManager : MonoBehaviour
{
    [Header("App Buttons")]
    [SerializeField] private Button microsoftEdgeButton;
    [SerializeField] private Button googleChromeButton;
    [SerializeField] private Button winRarButton;

    [Header("App Panels")]
    [SerializeField] private GameObject microsoftEdgePanel;
    [SerializeField] private GameObject chromePanel;
    [SerializeField] private GameObject winRarPanel;

    [Header("References")]
    [SerializeField] private MicrosoftEdgeManager microsoftEdgeManager;

    // Session-only install state — resets when game restarts
    private static readonly System.Collections.Generic.HashSet<AppType> _installedApps
        = new System.Collections.Generic.HashSet<AppType>();

    public static bool IsInstalled(AppType app) => _installedApps.Contains(app);

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        // Re-apply installed state (active + interactable) in case this object was re-enabled
        ApplyInstallState(googleChromeButton, AppType.GoogleChrome);
        ApplyInstallState(winRarButton,       AppType.WinRar);

        if (microsoftEdgeManager == null && microsoftEdgePanel != null)
            microsoftEdgeManager = microsoftEdgePanel.GetComponent<MicrosoftEdgeManager>();
    }

    // ----------------------------------------------------------------
    //  Public — wired to buttons / called by MicrosoftEdgeManager
    // ----------------------------------------------------------------

    public void OpenMicrosoftEdge()
    {
        if (microsoftEdgePanel == null) return;

        microsoftEdgePanel.SetActive(true);
        microsoftEdgeManager?.ResetToHome();
        Debug.Log("[DesktopManager] Microsoft Edge opened.");
    }

    public void OpenChromeApp()
    {
        chromePanel?.SetActive(true);
        Debug.Log("[DesktopManager] Chrome opened.");
    }

    public void OpenWinRarApp()
    {
        winRarPanel?.SetActive(true);
        Debug.Log("[DesktopManager] WinRar opened.");
    }

    public void InstallApp(AppType app)
    {
        _installedApps.Add(app);

        switch (app)
        {
            case AppType.GoogleChrome:
                ApplyInstallState(googleChromeButton, AppType.GoogleChrome);
                Debug.Log("[DesktopManager] Google Chrome installed — button activated.");
                break;

            case AppType.WinRar:
                ApplyInstallState(winRarButton, AppType.WinRar);
                Debug.Log("[DesktopManager] WinRar installed — button activated.");
                break;
        }
    }

    // ----------------------------------------------------------------
    //  Helpers
    // ----------------------------------------------------------------

    private void ApplyInstallState(Button btn, AppType app)
    {
        if (btn == null) return;
        bool installed = _installedApps.Contains(app);
        btn.gameObject.SetActive(installed);
        btn.interactable = installed;
    }

    private static void SetButtonInteractable(Button btn, bool interactable)
    {
        if (btn != null) btn.interactable = interactable;
    }
}
