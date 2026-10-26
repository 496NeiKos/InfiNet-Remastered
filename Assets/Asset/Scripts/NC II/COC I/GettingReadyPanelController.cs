/*
 * ================================================================
 *  UNITY SETUP GUIDE — GettingReadyPanelController
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to the GettingReady GameObject inside Windows10Panel.
 *
 *  HIERARCHY
 *
 *    Windows10Panel
 *      ├─ GettingReady          ← this script here; start INACTIVE
 *      │    └─ ReadyText        ← TMP: "Getting Windows ready
 *      │                                Don't turn off your computer"
 *      ├─ PasswordLogin
 *      └─ Windows10Desktop
 *
 *  INSPECTOR ASSIGNMENTS
 *    displayDuration  — seconds the panel stays visible (default 5)
 *
 *  HOW IT WORKS
 *    FifthPhaseManager.OnAcceptPrivacy() calls Show(callback) once,
 *    inside the restart animation callback, instead of calling
 *    InitWindows10Panel() directly.
 *
 *    Show() activates this panel, waits displayDuration seconds,
 *    deactivates it, then invokes the callback (which calls
 *    InitWindows10Panel() and shows PasswordLogin).
 *
 *    No static flag is needed — OnAcceptPrivacy() is architecturally
 *    called only once per session. All subsequent boots route through
 *    T3MonitorController.ProceedToWindows10Panel(), which calls
 *    InitWindows10Panel() directly and never touches this panel.
 * ================================================================
 */

using System.Collections;
using UnityEngine;

public class GettingReadyPanelController : MonoBehaviour
{
    [SerializeField] private float displayDuration = 5f;

    public void Show(System.Action onComplete)
    {
        gameObject.SetActive(true);
        StartCoroutine(ShowRoutine(onComplete));
    }

    private IEnumerator ShowRoutine(System.Action onComplete)
    {
        yield return new WaitForSeconds(displayDuration);
        gameObject.SetActive(false);
        onComplete?.Invoke();
    }
}
