using UnityEngine;

public class Monitor : MonoBehaviour
{
    public GameObject screenOnVisual;  // Assign a UI image or sprite for ON state
    public GameObject screenOffVisual; // Assign a UI image or sprite for OFF state

    public void TurnOn()
    {
        screenOnVisual.SetActive(true);
        screenOffVisual.SetActive(false);
        Debug.Log("Monitor: Turned ON.");
    }

    public void TurnOff()
    {
        screenOnVisual.SetActive(false);
        screenOffVisual.SetActive(true);
        Debug.Log("Monitor: Turned OFF.");
    }
}
