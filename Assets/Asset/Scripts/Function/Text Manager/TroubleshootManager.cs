using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TroubleshootManager : MonoBehaviour
{
    public static TroubleshootManager Instance;

    public Text messageText; // Assign in inspector
    private Queue<string> messageLog = new Queue<string>();
    private Queue<Color> colorLog = new Queue<Color>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Show a new message in the troubleshoot area.
    /// Green for correct, Red for incorrect.
    /// </summary>
    public void ShowMessage(string message, bool isWarning = false)
    {
        // ✅ Explicit: pass true for errors, false for success
        Color msgColor = isWarning ? Color.red : Color.green;

        messageLog.Enqueue(message);
        colorLog.Enqueue(msgColor);

        if (messageLog.Count > 5)
        {
            messageLog.Dequeue();
            colorLog.Dequeue();
        }

        UpdateMessageText();
    }

    private void UpdateMessageText()
    {
        messageText.text = "";
        string[] messages = messageLog.ToArray();
        Color[] colors = colorLog.ToArray();

        for (int i = 0; i < messages.Length; i++)
        {
            string colorHex = ColorUtility.ToHtmlStringRGB(colors[i]);
            messageText.text += $"<color=#{colorHex}>{messages[i]}</color>\n";
        }

        Debug.Log("Troubleshoot log updated:\n" + messageText.text);
    }
}
