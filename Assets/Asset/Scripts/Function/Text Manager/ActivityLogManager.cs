using TMPro;
using UnityEngine;

public class ActivityLogManager : MonoBehaviour
{
    public enum EntryType { Install, Remove, Action }

    public static ActivityLogManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private RectTransform content;

    private string _log = "";

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

        _log = $"<color={color}>> {message}</color>\n" + _log;

        if (logText == null) return;

        logText.text = _log;

        // Force TMP to calculate the new height, then size the Content to match.
        // This tells the ScrollRect the real scrollable area without relying on ContentSizeFitter.
        logText.ForceMeshUpdate();
        if (content != null)
            content.sizeDelta = new Vector2(content.sizeDelta.x, logText.preferredHeight);
    }
}
