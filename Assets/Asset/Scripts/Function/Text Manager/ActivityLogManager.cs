using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActivityLogManager : MonoBehaviour
{
    public enum EntryType { Install, Remove, Action, Warning }

    public static ActivityLogManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private ScrollRect scrollRect;

    private string _log = "";
    private string _timerLabel = "";
    private string _timerLine  = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public static void Log(string message, EntryType type = EntryType.Action)
    {
        if (Instance == null) return;
        Instance.Append(message, type);
    }

    // Show a live "label: Xs" line at the top of the log while an action is in progress.
    public static void StartLiveTimer(string label)
    {
        if (Instance == null) return;
        Instance._timerLabel = label;
        Instance._timerLine  = $"<color=#0D47A1>> {label}: 0s</color>";
        Instance.UpdateDisplay();
    }

    // Update the live timer line with the current elapsed seconds (call every frame or on change).
    public static void UpdateLiveTimer(float elapsed)
    {
        if (Instance == null || Instance._timerLine == null) return;
        Instance._timerLine = $"<color=#0D47A1>> {Instance._timerLabel}: {Mathf.FloorToInt(elapsed)}s</color>";
        Instance.UpdateDisplay();
    }

    // Remove the live timer line (call on cancel or when the action completes).
    public static void StopLiveTimer()
    {
        if (Instance == null) return;
        Instance._timerLine = null;
        Instance.UpdateDisplay();
    }

    private void Append(string message, EntryType type)
    {
        string color = type switch
        {
            EntryType.Install  => "#000000",
            EntryType.Remove   => "#000000",
            EntryType.Action   => "#000000",
            EntryType.Warning  => "#C62828",
            _                  => "#000000",
        };

        _log = $"<color={color}>> {message}</color>\n" + _log;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (logText == null) return;
        logText.text = _timerLine != null ? _timerLine + "\n" + _log : _log;
        LayoutRebuilder.ForceRebuildLayoutImmediate(logText.rectTransform);
        if (scrollRect != null)
            scrollRect.normalizedPosition = new Vector2(0f, 1f);
    }
}
