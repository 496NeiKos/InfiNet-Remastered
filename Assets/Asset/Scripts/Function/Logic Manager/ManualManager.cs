using UnityEngine;

public class ManualManager : MonoBehaviour
{
    [Header("References")]
    public GameObject manualPanel;          // Assign your manual panel here
    public MonoBehaviour[] hardwareScripts; // Drag all drag/drop scripts here

    private bool isOpen = false;

    public void ToggleManual()
    {
        isOpen = !isOpen; // flip state

        // Show/hide panel
        manualPanel.SetActive(isOpen);

        // Enable/disable hardware scripts
        foreach (var script in hardwareScripts)
        {
            if (script != null)
                script.enabled = !isOpen;
        }
    }
}
