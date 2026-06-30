/*
 * ================================================================
 *  UNITY SETUP GUIDE — DeviceManagerController
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to the DeviceManagerPanel GameObject.
 *
 *  HIERARCHY
 *
 *    DeviceManagerPanel  (this script here, start INACTIVE)
 *      ├─ DeviceManagerNavBar
 *      │    └─ DM_NavFlow
 *      │         ├─ DM_NavControl  [4 buttons — no function yet]
 *      │         ├─ DM_Minimize → OnClick: DeviceManagerController.Minimize()
 *      │         └─ DM_Exit     → OnClick: DeviceManagerController.Exit()
 *      └─ DeviceManagerContent
 *           └─ [category buttons → sub-panels — deferred]
 *
 *  INSPECTOR ASSIGNMENTS
 *    contentSubPanels → all sub-panel GameObjects inside DeviceManagerContent
 *                       that should be hidden on Exit/reset (assign as array).
 *
 *  BUTTON OnClick WIRING
 *    DM_Minimize → DeviceManagerController.Minimize()
 *    DM_Exit     → DeviceManagerController.Exit()
 *
 *  HOW IT WORKS
 *    Minimize(): hides the entire DeviceManagerPanel while preserving its
 *    internal state (expanded sub-panels remain open on next show).
 *    Re-opening is done by the TaskBarController calling SetActive(true).
 *
 *    Exit(): resets all internal sub-panels to closed, then hides the panel.
 *    The next time the panel is opened it starts in a clean state.
 * ================================================================
 */

using UnityEngine;

public class DeviceManagerController : MonoBehaviour
{
    [Header("Content Sub-Panels (hidden on Exit)")]
    [SerializeField] private GameObject[] contentSubPanels;

    // ----------------------------------------------------------------
    //  Public — wired to DM_Minimize and DM_Exit buttons
    // ----------------------------------------------------------------

    public void Minimize()
    {
        gameObject.SetActive(false);
        Debug.Log("[DeviceManagerController] Minimized — state preserved.");
    }

    public void Exit()
    {
        ResetContent();
        gameObject.SetActive(false);
        Debug.Log("[DeviceManagerController] Exited — state reset.");
    }

    // ----------------------------------------------------------------
    //  Helpers
    // ----------------------------------------------------------------

    private void ResetContent()
    {
        foreach (GameObject panel in contentSubPanels)
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }
}
