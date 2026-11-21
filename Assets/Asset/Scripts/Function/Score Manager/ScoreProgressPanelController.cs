/*
 * ================================================================
 *  ScoreProgressPanelController  — reusable score display panel
 * ================================================================
 *  PLACEMENT
 *    One instance per COC scene canvas. Starts INACTIVE.
 *    Referenced by both the settings panel and lesson complete popup.
 *
 *  HIERARCHY
 *    ScoreProgressPanel  (starts inactive)
 *      ├── COCLabel              TMP_Text  e.g. "COC III"
 *      ├── RetainedBanner        GameObject  → "Your Score Last Time" label
 *      │                                        (active only in retain mode)
 *      ├── PerformanceValue      TMP_Text  e.g. "75.0"
 *      ├── AccuracyValue         TMP_Text  e.g. "97.0"
 *      ├── TimeDurationRow       GameObject  → entire row (shown/hidden by difficulty)
 *      │     └── TimeDurationValue  TMP_Text
 *      ├── TimerCountdown        TMP_Text  e.g. "57:32" or "-01:30"
 *      │   (shown only on Hard; hidden on Normal)
 *      ├── QuizValue             TMP_Text  e.g. "100.0"
 *      ├── TotalValue            TMP_Text  e.g. "90.0"
 *      └── CloseButton           → OnClick: Close()
 *
 *  USAGE
 *    Open()   — shows panel, enters live mode
 *    Close()  — hides panel
 *    The panel auto-refreshes every frame while open.
 *    In retain mode: displays old saved score + banner.
 *    After score is saved: values freeze (timer stops).
 * ================================================================
 */

using TMPro;
using UnityEngine;

public class ScoreProgressPanelController : MonoBehaviour
{
    [Header("Panel Root")]
    [SerializeField] private GameObject panel;

    [Header("Labels")]
    [SerializeField] private TMP_Text cocLabel;
    [SerializeField] private TMP_Text performanceValue;
    [SerializeField] private TMP_Text accuracyValue;
    [SerializeField] private TMP_Text totalValue;

    [Header("Time Duration Row")]
    [SerializeField] private GameObject timeDurationRow;
    [SerializeField] private TMP_Text   timeDurationValue;
    [SerializeField] private TMP_Text   timerCountdown;

    [Header("Retain Banner")]
    [SerializeField] private GameObject retainedBanner;

    private bool _isOpen = false;

    public bool IsOpen => _isOpen;

    // ── Open / Close ──────────────────────────────────────────────────────────

    public void Open()
    {
        // Bootstrap only when no manager exists at all (direct play in editor).
        // When going through LessonSelection the manager persists and SetupRun has
        // already been called with the correct difficulty — do not create a new one.
        if (COCScoreManager.Instance == null)
            new GameObject("[COCScoreManager]").AddComponent<COCScoreManager>();

        _isOpen = true;
        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        _isOpen = false;
        panel.SetActive(false);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_isOpen) return;
        Refresh();
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    private void Refresh()
    {
        var mgr = COCScoreManager.Instance;

        // Always hide the banner first so it never shows stale state
        if (retainedBanner != null) retainedBanner.SetActive(false);

        if (mgr == null)
        {
            ShowNoData();
            return;
        }

        int  coc      = mgr.ActiveCOC;
        bool retain   = mgr.IsRetainMode;
        bool hardMode = mgr.CurrentDiff == Difficulty.Hard;

        if (cocLabel != null)
            cocLabel.text = coc >= 0 ? $"COC {coc + 1}" : "COC —";

        if (retainedBanner != null)
            retainedBanner.SetActive(retain);

        // ── Choose data source ────────────────────────────────────────────────

        float perf, acc, time, total, elapsed;

        if (retain && coc >= 0)
        {
            var old  = mgr.GetSavedScore(coc);
            perf     = old.performancePts;
            acc      = old.accuracyPts;
            time     = old.timePts;
            total    = old.totalPts;
            elapsed  = old.elapsedSeconds;
            hardMode = old.difficulty == Difficulty.Hard;
        }
        else
        {
            perf    = mgr.GetLivePerformancePts();
            acc     = mgr.GetLiveAccuracyPts();
            time    = mgr.GetLiveTimePts();
            total   = mgr.GetLiveTotalPts();
            elapsed = mgr.GetLiveElapsed();
        }

        // ── Populate fields ───────────────────────────────────────────────────

        if (performanceValue != null) performanceValue.text = $"{perf:F1}";
        if (accuracyValue    != null) accuracyValue.text    = $"{acc:F1}";
        if (totalValue       != null) totalValue.text       = $"{total:F1}";

        // ── Time duration row ─────────────────────────────────────────────────

        if (timeDurationRow != null) timeDurationRow.SetActive(true);

        if (hardMode)
        {
            if (timeDurationValue != null) timeDurationValue.text = $"{time:F1}";
            if (timerCountdown    != null) timerCountdown.text    = FormatCountdown(elapsed);
            if (timerCountdown    != null) timerCountdown.gameObject.SetActive(true);
        }
        else
        {
            if (timeDurationValue != null) timeDurationValue.text = "N/A";
            if (timerCountdown    != null) timerCountdown.gameObject.SetActive(false);
        }
    }

    private void ShowNoData()
    {
        if (cocLabel          != null) cocLabel.text          = "COC —";
        if (performanceValue  != null) performanceValue.text  = "0.0";
        if (accuracyValue     != null) accuracyValue.text     = "100.0";
        if (totalValue        != null) totalValue.text        = "—";
        if (timeDurationRow   != null) timeDurationRow.SetActive(true);
        if (timeDurationValue != null) timeDurationValue.text = "N/A";
        if (timerCountdown    != null) timerCountdown.gameObject.SetActive(false);
    }

    // ── Countdown format ──────────────────────────────────────────────────────
    // Countdown = 3600 - elapsed. Goes negative when over time.

    private static string FormatCountdown(float elapsed)
    {
        float remaining = 3600f - elapsed;
        bool  negative  = remaining < 0f;
        int   abs       = Mathf.RoundToInt(Mathf.Abs(remaining));
        int   m         = abs / 60;
        int   s         = abs % 60;
        return (negative ? "-" : "") + $"{m:00}:{s:00}";
    }
}
