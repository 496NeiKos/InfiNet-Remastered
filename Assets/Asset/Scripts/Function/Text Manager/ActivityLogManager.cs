using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton terminal log. Appends every user action as a colour-coded line and
/// keeps the full history visible in a ScrollRect.
///
/// UI setup required (all assigned in the Inspector):
///   - ScrollRect   : the scroll view wrapping the log content
///   - TextMeshProUGUI logText : inside the ScrollRect content, with ContentSizeFitter
///     set to Preferred Size (Vertical) so the content grows as lines are added
/// </summary>
public class ActivityLogManager : MonoBehaviour
{
    public enum EntryType { Install, Remove, Action }

    public static ActivityLogManager Instance { get; private set; }

    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI logText;

    private readonly StringBuilder _log = new StringBuilder();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Append a log entry. Safe to call when Instance is null (e.g. in scenes without a terminal).
    /// </summary>
    public static void Log(string message, EntryType type = EntryType.Action)
    {
        if (Instance == null) return;
        Instance.Append(message, type);
    }

    private void Append(string message, EntryType type)
    {
        string color = type switch
        {
            EntryType.Install => "#00E676",
            EntryType.Remove  => "#FFB300",
            EntryType.Action  => "#E0E0E0",
            _                 => "#E0E0E0",
        };

        _log.AppendLine($"<color={color}>> {message}</color>");

        if (logText != null)
            logText.text = _log.ToString();

        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        // WaitForEndOfFrame ensures Unity has finished its layout pass for this frame
        // before we force a rebuild and set the scroll position.
        yield return new WaitForEndOfFrame();
        if (scrollRect == null) yield break;
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
