using TMPro;
using UnityEngine;

public class ActivityLogManager : MonoBehaviour
{
    public enum EntryType { Install, Remove, Action }

    public static ActivityLogManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI logText;

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

        // Prepend so the newest entry is always at line 1.
        _log = $"<color={color}>> {message}</color>\n" + _log;

        if (logText != null)
            logText.text = _log;
    }
}
