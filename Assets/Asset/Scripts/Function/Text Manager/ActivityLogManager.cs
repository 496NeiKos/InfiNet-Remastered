using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton terminal log. Appends every user action as a colour-coded line.
///
/// UI setup — no ContentSizeFitter needed on Content:
///   - ScrollRect  : the Scroll View root
///   - TextMeshProUGUI logText : inside ScrollRect → Viewport → Content
///     Content RectTransform: anchor top-stretch, pivot (0.5, 1)
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

        if (logText == null || scrollRect == null) return;

        logText.text = _log.ToString();

        // Force TMP to compute the correct height for the new text immediately.
        logText.ForceMeshUpdate();

        float contentHeight  = logText.preferredHeight;
        float viewportHeight = scrollRect.viewport.rect.height;

        // Resize the content rect to fit all text.
        RectTransform content = scrollRect.content;
        content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);

        // Directly set the content's anchored position to show the bottom.
        // For a top-anchored content (pivot.y = 1), moving it up by
        // (contentHeight - viewportHeight) puts the last line in view.
        float scrollY = Mathf.Max(0f, contentHeight - viewportHeight);
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, scrollY);
    }
}
