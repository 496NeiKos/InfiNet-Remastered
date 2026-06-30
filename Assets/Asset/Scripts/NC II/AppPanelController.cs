/*
 * ================================================================
 *  UNITY SETUP GUIDE — AppPanelController
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to each app panel that needs its own
 *    minimize / exit controls. Attach one instance per panel.
 *
 *  USAGE — attach to ChromePanel and WinrarPanel (under AppExecuted)
 *
 *    ChromePanel   (this script here)
 *      └─ [nav bar]
 *           ├─ MinimizeBtn → OnClick: AppPanelController.Minimize()
 *           └─ ExitBtn     → OnClick: AppPanelController.Exit()
 *
 *    WinrarPanel   (this script here — separate component instance)
 *      └─ [nav bar]
 *           ├─ MinimizeBtn → OnClick: AppPanelController.Minimize()
 *           └─ ExitBtn     → OnClick: AppPanelController.Exit()
 *
 *  HOW IT WORKS
 *    Minimize(): hides the panel (SetActive false) while preserving
 *    all internal state — the panel resumes exactly where it was
 *    if shown again.
 *
 *    Exit(): hides the panel. Extend this method in the future if
 *    the panel needs its internal state reset on close.
 * ================================================================
 */

using UnityEngine;

public class AppPanelController : MonoBehaviour
{
    public void Minimize()
    {
        gameObject.SetActive(false);
        Debug.Log($"[AppPanelController] {gameObject.name} minimized.");
    }

    public void Exit()
    {
        gameObject.SetActive(false);
        Debug.Log($"[AppPanelController] {gameObject.name} exited.");
    }
}
