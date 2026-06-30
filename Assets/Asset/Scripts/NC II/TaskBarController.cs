/*
 * ================================================================
 *  UNITY SETUP GUIDE — TaskBarController
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to the TaskBar GameObject.
 *
 *  HIERARCHY
 *
 *    TaskBar  (this script here)
 *      └─ WindowsIcon                     ← windowsIconButton (Button)
 *          └─ WindowIconContent           → windowIconContent (GameObject, start INACTIVE)
 *               ├─ ShutdownIcon           → shutdownIconButton (Button)
 *               │    └─ ShutdownTray      → shutdownTray (GameObject, start INACTIVE)
 *               │         ├─ SleepBtn     (Button — non-interactable, no function)
 *               │         ├─ RestartBtn   (Button — non-interactable, no function)
 *               │         └─ ShutdownBtn  → Windows10Manager.OnShutdown()
 *               ├─ SettingsBtn            → settingsButton (Button)
 *               └─ DeviceManagerBtn       → deviceManagerButton (Button)
 *
 *    BackgroundBlocker  (full-screen transparent Image+Button, child of the same
 *                        Canvas as WindowIconContent; sort order BELOW WindowIconContent
 *                        but ABOVE other content — start INACTIVE)
 *                       → backgroundBlocker (GameObject)
 *
 *  INSPECTOR ASSIGNMENTS
 *    windowIconContent     → TaskBar > WindowsIcon > WindowIconContent
 *    shutdownTray          → WindowIconContent > ShutdownIcon > ShutdownTray
 *    backgroundBlocker     → full-screen transparent overlay GameObject
 *    settingPanel          → Windows10Desktop > WindowsContent > SettingPanel
 *    deviceManagerPanel    → Windows10Desktop > WindowsContent > DeviceManagerPanel
 *    settingController     → SettingPanelController on SettingPanel (optional — auto-resolved)
 *    deviceManagerController → DeviceManagerController on DeviceManagerPanel (optional)
 *
 *  BUTTON OnClick WIRING
 *    WindowsIcon Button       → TaskBarController.ToggleWindowIconContent()
 *    ShutdownIcon Button      → TaskBarController.ToggleShutdownTray()
 *    BackgroundBlocker Button → TaskBarController.CloseAll()
 *    SettingsBtn              → TaskBarController.OpenSettings()
 *    DeviceManagerBtn         → TaskBarController.OpenDeviceManager()
 *
 *  HOW IT WORKS
 *    ToggleWindowIconContent: opens / closes the tray and activates the
 *    BackgroundBlocker so any click outside the tray invokes CloseAll().
 *    ToggleShutdownTray: shows / hides the shutdown sub-tray inside the tray.
 *    CloseAll: closes everything (tray + shutdown sub-tray + blocker).
 *    OpenSettings / OpenDeviceManager: CloseAll first, then show the panel.
 * ================================================================
 */

using UnityEngine;

public class TaskBarController : MonoBehaviour
{
    [Header("Tray")]
    [SerializeField] private GameObject windowIconContent;
    [SerializeField] private GameObject shutdownTray;
    [SerializeField] private GameObject backgroundBlocker;

    [Header("WindowsContent Panels")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private GameObject deviceManagerPanel;

    [Header("Controllers (auto-resolved if left empty)")]
    [SerializeField] private SettingPanelController settingController;
    [SerializeField] private DeviceManagerController deviceManagerController;

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        windowIconContent?.SetActive(false);
        shutdownTray?.SetActive(false);
        backgroundBlocker?.SetActive(false);

        if (settingController == null && settingPanel != null)
            settingController = settingPanel.GetComponent<SettingPanelController>();

        if (deviceManagerController == null && deviceManagerPanel != null)
            deviceManagerController = deviceManagerPanel.GetComponent<DeviceManagerController>();
    }

    // ----------------------------------------------------------------
    //  Public — wired to buttons
    // ----------------------------------------------------------------

    public void ToggleWindowIconContent()
    {
        if (windowIconContent == null) return;

        bool open = !windowIconContent.activeSelf;
        SetTrayOpen(open);

        if (!open)
            shutdownTray?.SetActive(false);

        Debug.Log($"[TaskBarController] WindowIconContent {(open ? "opened" : "closed")}.");
    }

    public void ToggleShutdownTray()
    {
        if (shutdownTray == null) return;

        bool open = !shutdownTray.activeSelf;
        shutdownTray.SetActive(open);
        Debug.Log($"[TaskBarController] ShutdownTray {(open ? "opened" : "closed")}.");
    }

    public void CloseAll()
    {
        shutdownTray?.SetActive(false);
        SetTrayOpen(false);
        Debug.Log("[TaskBarController] CloseAll — tray and blocker hidden.");
    }

    public void OpenSettings()
    {
        CloseAll();

        if (settingPanel != null)
        {
            settingPanel.SetActive(true);
            settingController?.Open();
        }

        Debug.Log("[TaskBarController] Settings opened.");
    }

    public void OpenDeviceManager()
    {
        CloseAll();
        deviceManagerPanel?.SetActive(true);
        Debug.Log("[TaskBarController] DeviceManager opened.");
    }

    // ----------------------------------------------------------------
    //  Private helpers
    // ----------------------------------------------------------------

    private void SetTrayOpen(bool open)
    {
        windowIconContent?.SetActive(open);
        backgroundBlocker?.SetActive(open);
    }
}
