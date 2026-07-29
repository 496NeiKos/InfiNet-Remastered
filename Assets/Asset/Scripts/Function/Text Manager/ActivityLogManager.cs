using TMPro;
using UnityEngine;

public class ActivityLogManager : MonoBehaviour
{
    public enum EntryType { Install, Remove, Action, Warning }

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
        // Original colors: Install=#1B5E20 (dark green), Remove=#E65100 (deep orange), Action=#263238 (dark blue-grey)
        string color = type switch
        {
            EntryType.Install  => "#000000",
            EntryType.Remove   => "#000000",
            EntryType.Action   => "#000000",
            EntryType.Warning  => "#C62828",
            _                  => "#000000",
        };

        // Prepend so the newest entry is always at line 1.
        _log = $"<color={color}>> {message}</color>\n" + _log;

        if (logText != null)
            logText.text = _log;
    }
}
