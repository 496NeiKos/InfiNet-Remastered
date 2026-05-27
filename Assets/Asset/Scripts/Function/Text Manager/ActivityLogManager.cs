using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        StopAllCoroutines();
        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        // Wait two frames: first for TMP to rebuild its mesh,
        // second for ContentSizeFitter to resize the content rect.
        yield return null;
        yield return null;
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
