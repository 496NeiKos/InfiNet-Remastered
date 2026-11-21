/*
 * ================================================================
 *  COCScoreManager  — session-scoped scoring singleton
 * ================================================================
 *  PLACEMENT
 *    Add to a persistent GameObject in the LessonSelection scene.
 *    DontDestroyOnLoad keeps it alive for the whole app session.
 *    All score data lives in memory only — it is gone on app exit.
 *
 *  FLOW
 *    1. LessonOverviewManager calls SetupRun() after the player
 *       chooses difficulty (and optionally retains old score).
 *    2. COC scene loads → OnSceneLoaded() starts the timer.
 *    3. Update() ticks the timer and polls task managers.
 *    4. When all task managers for the active COC report AllDone,
 *       StopAndSaveScore() is called automatically.
 *    5. Settings panel calls PauseTimer/ResumeTimer on open/close.
 *    6. ReportWrong() is called by any script that detects a wrong
 *       interaction (failed power-on, wrong component placement, etc.)
 * ================================================================
 */

using UnityEngine;
using UnityEngine.SceneManagement;

public class COCScoreManager : MonoBehaviour
{
    public static COCScoreManager Instance { get; private set; }

    // ── Saved scores (session, 4 COCs) ───────────────────────────────────────

    private readonly COCScoreData[] _scores = new COCScoreData[4]
    {
        new COCScoreData(), new COCScoreData(),
        new COCScoreData(), new COCScoreData()
    };

    // ── Current run state ────────────────────────────────────────────────────

    private int        _activeCOC      = -1;
    private Difficulty _difficulty     = Difficulty.Normal;
    private float      _elapsedSeconds = 0f;
    private bool       _timerRunning   = false;
    private bool       _isRetainMode   = false;
    private int        _wrongCount     = 0;
    private bool       _scoreSaved     = false;
    private bool       _setupDone      = false;

    // ── Public read-only state ────────────────────────────────────────────────

    public int        ActiveCOC    => _activeCOC;
    public Difficulty CurrentDiff  => _difficulty;
    public bool       IsRetainMode => _isRetainMode;
    public bool       ScoreSaved   => _scoreSaved;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Must be root-level for DontDestroyOnLoad to survive scene transitions.
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        // Auto-detect if we first wake up inside a COC scene (direct play or lazy bootstrap).
        // This only runs when no SetupRun has been called yet (e.g. editor testing).
        if (!_setupDone)
        {
            int coc = GetCOCIndexForScene(SceneManager.GetActiveScene().name);
            if (coc >= 0)
            {
                _activeCOC    = coc;
                _difficulty   = Difficulty.Normal;
                _isRetainMode = false;
                _setupDone    = true;
                StartTimer();
            }
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (!_timerRunning || _scoreSaved) return;

        _elapsedSeconds += Time.unscaledDeltaTime;

        if (_activeCOC >= 0 && AreAllTaskManagersDone(_activeCOC))
            StopAndSaveScore();
    }

    // ── Scene detection ───────────────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        int coc = GetCOCIndexForScene(scene.name);
        if (coc < 0) return;

        if (!_setupDone)
        {
            // Fallback: direct scene entry (editor testing, no difficulty panel flow).
            // Defaults to Normal difficulty so the panel can still display live data.
            _activeCOC    = coc;
            _difficulty   = Difficulty.Normal;
            _isRetainMode = false;
            _scoreSaved   = false;
            _wrongCount   = 0;
            _elapsedSeconds = 0f;
            _setupDone    = true;
        }

        if (coc != _activeCOC) return;
        StartTimer();
    }

    private static int GetCOCIndexForScene(string sceneName) => sceneName switch
    {
        "COC I"   => 0,
        "COC II"  => 1,
        "COC III" => 2,
        "COC IV"  => 3,
        _         => -1
    };

    // ── Setup ─────────────────────────────────────────────────────────────────

    public void SetupRun(int cocIndex, Difficulty difficulty, bool overwrite)
    {
        _activeCOC      = cocIndex;
        _difficulty     = difficulty;
        _isRetainMode   = !overwrite;
        _elapsedSeconds = 0f;
        _timerRunning   = false;
        _wrongCount     = 0;
        _scoreSaved     = false;
        _setupDone      = true;
    }

    // ── Timer control ─────────────────────────────────────────────────────────

    private void StartTimer()
    {
        if (_scoreSaved) return;
        _timerRunning = true;
    }

    public void PauseTimer()  => _timerRunning = false;

    public void ResumeTimer()
    {
        if (_scoreSaved) return;
        _timerRunning = true;
    }

    // ── Accuracy tracking ─────────────────────────────────────────────────────

    public void ReportWrong()
    {
        if (_scoreSaved || _activeCOC < 0) return;
        _wrongCount++;
    }

    // ── All-done detection ────────────────────────────────────────────────────

    private static bool AreAllTaskManagersDone(int cocIndex) => cocIndex switch
    {
        0 => NCIITaskListManager.Instance?.IsAllDone == true &&
             T2TaskListManager.Instance?.IsAllDone   == true &&
             T3TaskListManager.Instance?.IsAllDone   == true,
        1 => NetworkCableTaskManager.Instance?.IsAllDone  == true &&
             IPConfigTaskManager.Instance?.IsAllDone      == true &&
             PingCmdTaskManager.Instance?.IsAllDone       == true,
        2 => ServerSetupTaskManager.Instance?.IsAllDone        == true,
        3 => COCIVMaintenanceTaskManager.Instance?.IsAllDone   == true,
        _ => false
    };

    // ── Save ──────────────────────────────────────────────────────────────────

    public void StopAndSaveScore()
    {
        if (_scoreSaved) return;
        _scoreSaved   = true;
        _timerRunning = false;

        if (_isRetainMode) return;

        var (completed, total) = GetTaskProgress(_activeCOC);
        float perf = total > 0 ? Mathf.Clamp((completed / (float)total) * 100f, 0f, 100f) : 0f;
        float acc  = CalcAccuracy(_wrongCount, _difficulty);
        float time = _difficulty == Difficulty.Hard ? CalcTime(_elapsedSeconds) : -1f;
        float tot  = CalcTotal(perf, acc, time, _difficulty);

        _scores[_activeCOC] = new COCScoreData
        {
            isPlayed       = true,
            difficulty     = _difficulty,
            performancePts = perf,
            accuracyPts    = acc,
            timePts        = time,
            totalPts       = tot,
            elapsedSeconds = _elapsedSeconds
        };
    }

    // ── Live getters (used by ScoreProgressPanelController) ──────────────────

    public float GetLivePerformancePts()
    {
        var (completed, total) = GetTaskProgress(_activeCOC);
        return total > 0 ? Mathf.Clamp((completed / (float)total) * 100f, 0f, 100f) : 0f;
    }

    public float GetLiveAccuracyPts() => CalcAccuracy(_wrongCount, _difficulty);
    public float GetLiveTimePts()     => _difficulty == Difficulty.Hard ? CalcTime(_elapsedSeconds) : -1f;
    public float GetLiveElapsed()     => _elapsedSeconds;

    public float GetLiveTotalPts()
    {
        return CalcTotal(GetLivePerformancePts(), GetLiveAccuracyPts(), GetLiveTimePts(), _difficulty);
    }

    // ── Saved score access ────────────────────────────────────────────────────

    public bool        HasSavedScore(int cocIndex) => cocIndex >= 0 && cocIndex < 4 && _scores[cocIndex].isPlayed;
    public COCScoreData GetSavedScore(int cocIndex) => (cocIndex >= 0 && cocIndex < 4) ? _scores[cocIndex] : new COCScoreData();

    // ── Task progress aggregation ─────────────────────────────────────────────

    private static (int completed, int total) GetTaskProgress(int cocIndex) => cocIndex switch
    {
        0 => (
            (NCIITaskListManager.Instance?.GetCompletedTaskCount() ?? 0) +
            (T2TaskListManager.Instance?.GetCompletedTaskCount()   ?? 0) +
            (T3TaskListManager.Instance?.GetCompletedTaskCount()   ?? 0),
            196),
        1 => (
            (NetworkCableTaskManager.Instance?.GetCompletedTaskCount() ?? 0) +
            (IPConfigTaskManager.Instance?.GetCompletedTaskCount()     ?? 0) +
            (PingCmdTaskManager.Instance?.GetCompletedTaskCount()      ?? 0),
            160),
        2 => (ServerSetupTaskManager.Instance?.GetCompletedTaskCount()      ?? 0, 138),
        3 => (COCIVMaintenanceTaskManager.Instance?.GetCompletedTaskCount() ?? 0, 33),
        _ => (0, 1)
    };

    // ── Score formulas ────────────────────────────────────────────────────────

    private static float CalcAccuracy(int wrongs, Difficulty d)
    {
        float penalty = d == Difficulty.Hard
            ? wrongs
            : Mathf.Floor(wrongs * 0.5f);
        return Mathf.Max(0f, 100f - penalty);
    }

    private static float CalcTime(float elapsed)
    {
        if (elapsed <= 3600f) return 100f;
        return Mathf.Max(0f, 100f - Mathf.Floor((elapsed - 3600f) / 300f));
    }

    private static float CalcTotal(float perf, float acc, float time, Difficulty d)
    {
        return d == Difficulty.Hard
            ? (perf + acc + time) / 3f
            : (perf + acc) / 2f;
    }
}
