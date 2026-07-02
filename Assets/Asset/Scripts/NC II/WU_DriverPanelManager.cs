/*
 * ================================================================
 *  UNITY SETUP GUIDE — WU_DriverPanelManager
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to the Windows10Desktop GameObject.
 *    Placing it here means progress runs as long as the desktop is
 *    active — it is NOT tied to SettingPanel or WU_MainContentBody,
 *    so closing/minimizing those panels never pauses downloads.
 *
 *  HIERARCHY CONTEXT
 *
 *    Windows10Desktop  (this script here)
 *      └─ SettingPanel
 *           └─ 2ndLevel
 *                └─ WindowsUpdate
 *                     └─ WU_MainContent
 *                          ├─ WU_MainContentBody
 *                          │    ├─ Panel        ← drivers[0]  (installs normally, ~8 s)
 *                          │    │    ├─ DriverName   (label — set name in Inspector)
 *                          │    │    ├─ DriverStatus → drivers[0].statusText  (TMP_Text)
 *                          │    │    └─ DriverRetry  → drivers[0].retryObject (GameObject)
 *                          │    ├─ Panel (1)    ← drivers[1]  (stalls at 60%, ~7.5 s)
 *                          │    │    ├─ DriverName
 *                          │    │    ├─ DriverStatus → drivers[1].statusText
 *                          │    │    └─ DriverRetry  → drivers[1].retryObject
 *                          │    ├─ Panel (2)    ← drivers[2]  (installs normally, ~20 s)
 *                          │    │    ├─ DriverName
 *                          │    │    ├─ DriverStatus → drivers[2].statusText
 *                          │    │    └─ DriverRetry  → drivers[2].retryObject
 *                          │    └─ Panel (3)    ← drivers[3]  (blocked from start)
 *                          │         ├─ DriverName
 *                          │         ├─ DriverStatus → drivers[3].statusText
 *                          │         └─ DriverRetry  → drivers[3].retryObject
 *                          └─ WU_MainContentFooter
 *                               └─ WU_RestartNow → restartButton  (Button)
 *
 *  INSPECTOR ASSIGNMENTS
 *    drivers[0].statusText   → Panel > DriverStatus        (TMP_Text)
 *    drivers[0].retryObject  → Panel > DriverRetry         (GameObject)
 *    drivers[1].statusText   → Panel (1) > DriverStatus
 *    drivers[1].retryObject  → Panel (1) > DriverRetry
 *    drivers[2].statusText   → Panel (2) > DriverStatus
 *    drivers[2].retryObject  → Panel (2) > DriverRetry
 *    drivers[3].statusText   → Panel (3) > DriverStatus
 *    drivers[3].retryObject  → Panel (3) > DriverRetry
 *    restartButton           → WU_MainContentFooter > WU_RestartNow  (Button)
 *    monitorController       → T3MonitorController on the T3 monitor root GameObject
 *    settingController       → SettingPanelController on Windows10Desktop > WindowsContent > SettingPanel
 *
 *  HOW IT WORKS
 *
 *    Start condition:
 *      Downloads begin the FIRST time Windows10Desktop is enabled — i.e., the first
 *      successful login. Before that, all non-blocked drivers show "Status: Installing: 0%".
 *
 *    Pause/resume:
 *      Disabling Windows10Desktop (Shutdown or Restart) stops Update() naturally,
 *      so progress is frozen until the desktop is re-enabled on the next login.
 *      SettingPanel or WU_MainContent being hidden has NO effect on progress.
 *
 *    Driver behaviours (configured per-entry in the Inspector):
 *      installDuration  — seconds to reach 100% (set per driver)
 *      stallAtPercent   — 0-100: progress % at which the driver stalls into CannotInstall
 *                         set to -1 (or leave at default -1) for no stall
 *      startsBlocked    — tick to put this driver in CannotInstall from the very start
 *      retryDuration    — seconds to reach 100% when retried (defaults to installDuration)
 *
 *    Retry (DriverRetry button):
 *      Visible only when a driver is in CannotInstall state.
 *      If any other driver is still Installing → flash a brief feedback message on
 *      that driver's DriverStatus text for feedbackDuration seconds, then restore it.
 *      If no other driver is Installing → reset progress to 0% and begin Installing.
 *      Drivers that previously stalled (drivers[1]) will install fully on retry (no re-stall).
 *
 *    WU_RestartNow:
 *      Resets SettingPanel to 1stLevel (hides it), then resets the monitor to the
 *      loading screen and closes the canvas.
 *      Driver progress is preserved and will resume on the next login.
 *      PasswordLogin will be re-enabled by Windows10Manager.InitWindows10Panel()
 *      when the player reaches the Windows10 state again.
 * ================================================================
 */

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WU_DriverPanelManager : MonoBehaviour
{
    // ----------------------------------------------------------------
    //  Data types
    // ----------------------------------------------------------------

    public enum DriverState { NotStarted, Installing, Downloaded, CannotInstall }

    [System.Serializable]
    public class DriverEntry
    {
        public TMP_Text   statusText;
        public GameObject retryObject;

        [Tooltip("Seconds to reach 100% during normal installation.")]
        public float installDuration = 10f;

        [Tooltip("Progress % (0–100) at which this driver stalls into CannotInstall. Set to -1 for no stall.")]
        public float stallAtPercent = -1f;

        [Tooltip("If ticked, this driver starts as CannotInstall and never auto-installs.")]
        public bool startsBlocked = false;

        [Tooltip("Seconds to reach 100% when retried. Leave at 0 to reuse installDuration.")]
        public float retryDuration = 0f;

        [HideInInspector] public DriverState state    = DriverState.NotStarted;
        [HideInInspector] public float       progress = 0f;
    }

    // ----------------------------------------------------------------
    //  Inspector
    // ----------------------------------------------------------------

    [Header("Driver Panels  (Panel, Panel(1), Panel(2), Panel(3))")]
    [SerializeField] private DriverEntry[] drivers = new DriverEntry[4];

    [Header("Restart")]
    [SerializeField] private Button                 restartButton;
    [SerializeField] private T3MonitorController    monitorController;
    [SerializeField] private SettingPanelController settingController;

    [Header("Feedback")]
    [Tooltip("Seconds the feedback message stays visible before reverting.")]
    [SerializeField] private float feedbackDuration = 2.5f;

    // ----------------------------------------------------------------
    //  Runtime per-driver data (derived from DriverEntry fields in Awake)
    // ----------------------------------------------------------------

    private float[] _rate;       // progress per second during normal install
    private float[] _retryRate;  // progress per second on retry
    private float[] _stallAt;    // progress [0-1] at which driver stalls; -1 = none
    private bool[]  _stallActive;

    // ----------------------------------------------------------------
    //  Runtime state
    // ----------------------------------------------------------------

    private bool       _started;
    private Coroutine[] _feedback;

    // True once every driver has reached the Downloaded state (Task 7).
    public bool AllDriversDownloaded
    {
        get
        {
            if (drivers == null || drivers.Length == 0) return false;
            foreach (var d in drivers)
                if (d.state != DriverState.Downloaded) return false;
            return true;
        }
    }

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        _feedback    = new Coroutine[drivers.Length];
        _rate        = new float[drivers.Length];
        _retryRate   = new float[drivers.Length];
        _stallAt     = new float[drivers.Length];
        _stallActive = new bool[drivers.Length];

        // Derive runtime data from inspector-set fields
        for (int i = 0; i < drivers.Length; i++)
        {
            var d = drivers[i];
            float dur      = Mathf.Max(d.installDuration, 0.1f);
            float retryDur = d.retryDuration > 0f ? d.retryDuration : dur;

            _rate[i]      = 1f / dur;
            _retryRate[i] = 1f / retryDur;
            _stallAt[i]   = d.stallAtPercent >= 0f ? d.stallAtPercent / 100f : -1f;
            _stallActive[i] = _stallAt[i] > 0f;
        }

        // Blocked drivers start as CannotInstall so their retry button is visible
        for (int i = 0; i < drivers.Length; i++)
            if (drivers[i].startsBlocked) drivers[i].state = DriverState.CannotInstall;

        // Wire retry buttons (search inactive children too)
        for (int i = 0; i < drivers.Length; i++)
        {
            int idx = i;
            if (drivers[i].retryObject == null) continue;
            var btn = drivers[i].retryObject.GetComponentInChildren<Button>(true);
            if (btn != null)
                btn.onClick.AddListener(() => OnRetryClicked(idx));
        }

        restartButton?.onClick.AddListener(OnRestartNow);

        RefreshAllUI();
    }

    private void OnEnable()
    {
        // Unity stops coroutines when a GO is disabled; clear stale refs on re-enable
        for (int i = 0; i < _feedback.Length; i++)
            _feedback[i] = null;

        if (!_started)
        {
            _started = true;
            for (int i = 0; i < drivers.Length; i++)
                if (!drivers[i].startsBlocked) drivers[i].state = DriverState.Installing;
        }

        // Restore correct status text (feedback may have been interrupted on last disable)
        RefreshAllUI();
    }

    private void Update()
    {
        bool dirty = false;

        for (int i = 0; i < drivers.Length; i++)
        {
            if (drivers[i].state != DriverState.Installing) continue;

            drivers[i].progress += _rate[i] * Time.deltaTime;
            dirty = true;

            // Check stall first
            if (_stallActive[i] && drivers[i].progress >= _stallAt[i])
            {
                drivers[i].progress = _stallAt[i];
                drivers[i].state    = DriverState.CannotInstall;
                _stallActive[i]     = false;
                continue;
            }

            // Check completion
            if (drivers[i].progress >= 1f)
            {
                drivers[i].progress = 1f;
                drivers[i].state    = DriverState.Downloaded;
                T3TaskListManager.CheckConditions();
            }
        }

        if (dirty) RefreshAllUI();
    }

    // ----------------------------------------------------------------
    //  Retry
    // ----------------------------------------------------------------

    private void OnRetryClicked(int index)
    {
        if (drivers[index].state != DriverState.CannotInstall) return;

        // Block if any other driver is still Installing
        for (int i = 0; i < drivers.Length; i++)
        {
            if (i == index) continue;
            if (drivers[i].state == DriverState.Installing)
            {
                ShowFeedback(index, "Please wait for other drivers to finish first.");
                return;
            }
        }

        // All clear — reset and begin installing; use retry rate, no re-stall
        drivers[index].progress = 0f;
        drivers[index].state    = DriverState.Installing;
        _rate[index]            = _retryRate[index];
        _stallActive[index]     = false;
        RefreshDriverUI(index);
    }

    // ----------------------------------------------------------------
    //  Restart
    // ----------------------------------------------------------------

    private void OnRestartNow()
    {
        // Reset SettingPanel to 1stLevel before the desktop hides,
        // so the next login doesn't resume mid-navigation.
        settingController?.Exit();

        monitorController?.ResetToLoading();
        GameManager.Instance?.CloseEditor();
        gameObject.SetActive(false);
    }

    // ----------------------------------------------------------------
    //  UI
    // ----------------------------------------------------------------

    private void RefreshAllUI()
    {
        for (int i = 0; i < drivers.Length; i++)
            RefreshDriverUI(i);
    }

    private void RefreshDriverUI(int index)
    {
        var d = drivers[index];

        if (d.retryObject != null)
            d.retryObject.SetActive(d.state == DriverState.CannotInstall);

        // Don't overwrite an active feedback message
        if (_feedback[index] != null || d.statusText == null) return;

        d.statusText.text = d.state switch
        {
            DriverState.NotStarted    => "Status: Installing: 0%",
            DriverState.Installing    => $"Status: Installing: {Mathf.FloorToInt(d.progress * 100f)}%",
            DriverState.Downloaded    => "Status: Downloaded",
            DriverState.CannotInstall => "Status: Cannot install yet, wait for other drivers to finish",
            _                         => string.Empty
        };
    }

    // ----------------------------------------------------------------
    //  Feedback
    // ----------------------------------------------------------------

    private void ShowFeedback(int index, string message)
    {
        if (_feedback[index] != null)
            StopCoroutine(_feedback[index]);
        _feedback[index] = StartCoroutine(FeedbackRoutine(index, message));
    }

    private IEnumerator FeedbackRoutine(int index, string message)
    {
        if (drivers[index].statusText != null)
            drivers[index].statusText.text = message;

        yield return new WaitForSeconds(feedbackDuration);

        _feedback[index] = null;
        RefreshDriverUI(index);
    }
}
