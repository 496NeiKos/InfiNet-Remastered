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
        // Capture scroll position BEFORE adding content — content height change
        // would shift normalizedPosition even without user input.
        bool wasAtBottom = IsAtBottom();

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

        // Only follow the log when the user hasn't scrolled up to read history.
        if (wasAtBottom)
            StartCoroutine(ScrollToBottom());
    }

    // True when the user is at (or very near) the bottom, or when content hasn't
    // overflowed the viewport yet.
    private bool IsAtBottom()
    {
        if (scrollRect == null) return true;
        if (scrollRect.content.rect.height <= scrollRect.viewport.rect.height) return true;
        return scrollRect.verticalNormalizedPosition <= 0.05f;
    }

    private IEnumerator ScrollToBottom()
    {
        // Wait one frame so the layout system updates the content height first.
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }
}
