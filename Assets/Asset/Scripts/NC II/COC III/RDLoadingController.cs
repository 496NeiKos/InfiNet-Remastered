/*
 * ================================================================
 *  UNITY SETUP GUIDE — RDLoadingController (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to "RD Loading Panel" (starts INACTIVE).
 *    This is Stage 3 of the 3-stage Remote Desktop chain.
 *    Place as a sibling of the other RD panels inside the Desktop
 *    Panel's App Panels group.
 *
 *  HIERARCHY
 *    RD Loading Panel                     ← this script here
 *      ├── LoadingLabelTMP                TMP_Text  → loadingLabelTMP
 *      │     (auto-filled: "Please wait while Remote Desktop
 *      │      connects to [ComputerName]...")
 *      └── ProgressSlider                 Slider  → progressSlider
 *            Min Value:      0
 *            Max Value:      1
 *            Whole Numbers:  false
 *            Interactable:   false
 *            Starting Value: 0
 *
 *  INSPECTOR ASSIGNMENTS
 *    loadingLabelTMP    → LoadingLabelTMP
 *    progressSlider     → ProgressSlider
 *    connectionDuration → float (seconds). Adjust freely — default 3.
 *                         This is how long the slider takes to fill.
 *    serverManager      → ServerManagerController (assign in Inspector)
 *
 *  HOW IT WORKS
 *    Open(computerName) is called by WindowsSecurityRDController after
 *    valid credentials are entered. It:
 *      • Sets loadingLabelTMP text
 *      • Resets slider to 0
 *      • Starts a coroutine (RunLoading) that increments the slider
 *        over connectionDuration seconds using Time.deltaTime
 *
 *    When the slider reaches 1.0 (loading completes):
 *      • state.RDSessionOpened = true
 *      • state.ClientConnected = true
 *      • This panel is hidden
 *      • ServerManagerController.Open() is called — the full Server
 *        Manager is now accessible from the Client PC context
 *
 *  NOTES
 *    • connectionDuration is clamped to a minimum of 0.1 s internally
 *      so a value of 0 in the Inspector won't cause a divide-by-zero.
 *    • Desktop icons are refreshed immediately when loading completes
 *      via ServerVirtualOSManager.RefreshIcons() — rdConnectionIcon
 *      hides and serverManagerIcon appears on the client desktop
 *      without requiring a re-login.
 *    • There is no Cancel during loading — the chain is committed.
 * ================================================================
 */

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RDLoadingController : MonoBehaviour
{
    public static RDLoadingController Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text loadingLabelTMP;
    [SerializeField] private Slider   progressSlider;

    [Header("Settings")]
    [Tooltip("How many seconds the loading slider takes to fill. Adjustable in the Inspector.")]
    [SerializeField] private float connectionDuration = 3f;

    [Header("References")]
    [SerializeField] private ServerManagerController serverManager;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open(string computerName)
    {
        StopAllCoroutines();

        gameObject.SetActive(true);

        if (loadingLabelTMP != null)
            loadingLabelTMP.text = $"Please wait while Remote Desktop connects to {computerName}...";

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value    = 0f;
        }

        StartCoroutine(RunLoading(computerName));
        ActivityLogManager.Log($"RD Loading started — connecting to {computerName}", ActivityLogManager.EntryType.Action);
    }

    // ── Loading coroutine ─────────────────────────────────────────────────────

    private IEnumerator RunLoading(string computerName)
    {
        float elapsed  = 0f;
        float duration = Mathf.Max(0.1f, connectionDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (progressSlider != null)
                progressSlider.value = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        if (progressSlider != null) progressSlider.value = 1f;

        var state = ServerVirtualOSManager.Instance?.ServerState;
        if (state != null)
        {
            state.RDSessionOpened = true;
            state.ClientConnected = true;
        }

        // Refresh desktop icons immediately so rdConnectionIcon hides and
        // serverManagerIcon appears on the client desktop without waiting for re-login.
        ServerVirtualOSManager.Instance?.RefreshIcons();

        gameObject.SetActive(false);
        serverManager?.Open();

        ActivityLogManager.Log(
            $"RD Session established — Server Manager opened on Client PC (connected to {computerName})",
            ActivityLogManager.EntryType.Action);
    }
}
