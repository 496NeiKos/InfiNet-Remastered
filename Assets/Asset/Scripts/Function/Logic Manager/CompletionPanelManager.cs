using UnityEngine;

public class CompletionPanelManager : MonoBehaviour
{
    [Header("References")]
    public GameObject completionPanel;       // drag your completion panel here
    public MonoBehaviour[] hardwareScripts;  // drag all CPU/GPU/RAM/Motherboard drag scripts here

    // Called when tasks are complete
    public void ShowCompletionPanel()
    {
        completionPanel.SetActive(true);

        // Disable hardware scripts
        foreach (var script in hardwareScripts)
        {
            if (script != null)
                script.enabled = false;
        }
    }

    // Called when Return button is clicked
    public void CloseCompletionPanel()
    {
        completionPanel.SetActive(false);

        // Re‑enable hardware scripts
        foreach (var script in hardwareScripts)
        {
            if (script != null)
                script.enabled = true;
        }
    }
}
