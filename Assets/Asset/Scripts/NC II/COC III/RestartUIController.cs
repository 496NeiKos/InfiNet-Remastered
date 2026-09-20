/*
 * ================================================================
 *  UNITY SETUP GUIDE — RestartUIController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "Restart UI Panel" GameObject (starts INACTIVE).
 *    Place as a DIRECT CHILD of Virtual OS Canvas — sibling of
 *    Login UI Panel and Desktop Panel.
 *
 *  HIERARCHY
 *    Restart UI Panel              ← this script here (starts INACTIVE)
 *      └── (visual content only — "Restarting..." text, spinning dots,
 *            dark background overlay, etc. No interactive elements.)
 *
 *  INSPECTOR SETTINGS
 *    restartDuration  seconds before auto-transitioning to Login UI.
 *                     Default: 3. Adjust per feel.
 *
 *  HOW IT WORKS
 *    ServerVirtualOSManager.TriggerRestart() calls Show(callback).
 *    Show() activates this panel, waits restartDuration seconds,
 *    deactivates the panel, then fires the callback (→ Login UI).
 *    Used for: first boot, taskbar Restart button, DCPromo reboot,
 *    domain join restart, and Change PC switches.
 *
 *  WIRING
 *    No manual button wiring needed — entirely driven by code.
 * ================================================================
 */

using System;
using System.Collections;
using UnityEngine;

public class RestartUIController : MonoBehaviour
{
    [Tooltip("Seconds the restart screen is visible before transitioning to Login UI.")]
    [SerializeField] private float restartDuration = 3f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Show(Action onComplete)
    {
        StopAllCoroutines();
        gameObject.SetActive(true);
        StartCoroutine(RunTimer(onComplete));
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private IEnumerator RunTimer(Action onComplete)
    {
        yield return new WaitForSeconds(restartDuration);
        gameObject.SetActive(false);
        onComplete?.Invoke();
    }
}
