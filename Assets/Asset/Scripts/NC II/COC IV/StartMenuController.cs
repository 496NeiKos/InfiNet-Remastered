/*
 * ================================================================
 *  UNITY SETUP GUIDE — StartMenuController (COC IV)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Start Menu Root" — a new always-active empty GameObject
 *    that is a DIRECT CHILD of Desktop Panel (sibling of Taskbar,
 *    Desktop Icons, and App Panels). Do NOT place under App Panels.
 *
 *  HIERARCHY
 *    Desktop Panel
 *      ├── Taskbar
 *      │     ├── Windows Icon Btn    → windowsIconBtn
 *      │     ├── Search Bar Btn      (wired in SearchWindowController)
 *      │     └── (other items — Restart Button MOVED to Start Menu Panel)
 *      │
 *      ├── Start Menu Root           ← this script here (ACTIVE)
 *      │     ├── Start Menu Overlay  → overlay  (INACTIVE, fullscreen transparent Button)
 *      │     └── Start Menu Panel    → startMenuPanel  (INACTIVE)
 *      │           └── Restart Btn   (Button → ServerVirtualOSManager.TriggerRestart)
 *      │
 *      ├── Search Window Root        (SearchWindowController lives here)
 *      ├── Desktop Icons
 *      └── App Panels
 *
 *  INSPECTOR ASSIGNMENTS
 *    windowsIconBtn   Button on the Windows icon in the Taskbar
 *    startMenuPanel   The Start Menu panel GameObject (starts INACTIVE)
 *    overlay          A full-screen transparent Button behind the panel
 *                     (INACTIVE by default). Set Image color alpha to 0,
 *                     RaycastTarget = true. Covers the full screen so
 *                     clicking anywhere outside closes the menu.
 *    overlayBtn       Button component on the overlay GameObject
 *
 *  OVERLAY SETUP
 *    - Add a child Image to Start Menu Root named "Start Menu Overlay"
 *    - Set RectTransform: stretch to fill parent, AnchorMin (0,0),
 *      AnchorMax (1,1), no offsets
 *    - Image color: (0,0,0,0) — fully transparent, RaycastTarget ON
 *    - Add a Button component — onClick → StartMenuController.Close()
 *    - Sibling order: Overlay BELOW Start Menu Panel (renders behind it)
 *
 *  HOW IT WORKS
 *    Windows Icon Btn click → ToggleStartMenu()
 *    If panel is open: Close() → hides panel + overlay.
 *    If panel is closed: Open() → shows panel + overlay.
 *    Clicking the overlay (i.e. anywhere outside the panel) → Close().
 *    SearchWindowController also calls Close() when search opens.
 *    The Restart button inside Start Menu Panel is wired directly to
 *    ServerVirtualOSManager.TriggerRestart() — no change from before.
 * ================================================================
 */

using UnityEngine;
using UnityEngine.UI;

public class StartMenuController : MonoBehaviour
{
    public static StartMenuController Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject startMenuPanel;
    [SerializeField] private GameObject overlay;

    [Header("Buttons")]
    [SerializeField] private Button windowsIconBtn;
    [SerializeField] private Button overlayBtn;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        windowsIconBtn?.onClick.AddListener(ToggleStartMenu);
        overlayBtn?.onClick.AddListener(Close);

        startMenuPanel?.SetActive(false);
        overlay?.SetActive(false);
    }

    public void ToggleStartMenu()
    {
        bool isOpen = startMenuPanel != null && startMenuPanel.activeSelf;
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        startMenuPanel?.SetActive(true);
        overlay?.SetActive(true);
    }

    public void Close()
    {
        startMenuPanel?.SetActive(false);
        overlay?.SetActive(false);
    }
}
