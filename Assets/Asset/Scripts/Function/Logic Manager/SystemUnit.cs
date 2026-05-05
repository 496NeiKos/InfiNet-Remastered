using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SystemUnit : MonoBehaviour
{
    private bool motherboardInstalled = false;
    private Motherboard motherboard;
    private Vector3 motherboardOriginalPosition;
    private Quaternion motherboardOriginalRotation;

    // Toggle install/eject when motherboard is dragged onto the System Unit

    private IEnumerator WaitAndLoadScene(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
    public void ToggleMotherboard()
    {
        AVR avr = FindObjectOfType<AVR>();
        Motherboard mb = FindObjectOfType<Motherboard>();

        if (!motherboardInstalled)
        {
            // ✅ Install motherboard
            AudioClip sfx = SoundManager.instance.mbSFX;
            SoundManager.instance.PlaySFX(sfx);
            motherboardInstalled = true;
            TroubleshootManager.Instance.ShowMessage("Motherboard installed in system unit.", false);

            if (mb != null)
            {
                mb.gameObject.SetActive(false); // hide motherboard
                Debug.Log("Motherboard installed and hidden.");

                // ✅ Task 2 complete
                TaskListManager.Instance.SetTaskCompleted(1, true);
            }
        }
        else
        {
            // ✅ Prevent eject if AVR is ON
            if (avr != null && avr.IsOn())
            {
                TroubleshootManager.Instance.ShowMessage(
                    "Cannot eject Motherboard while AVR is turned ON. Please turn off AVR first.",
                    true
                );
                Debug.Log("Eject blocked: AVR is ON.");
                return;
            }

            // ✅ Eject motherboard
            motherboardInstalled = false;
            TroubleshootManager.Instance.ShowMessage("Motherboard ejected from the system unit.", false);

            if (mb != null)
            {
                mb.gameObject.SetActive(true); // show motherboard again
                Debug.Log("Motherboard ejected and shown.");

                // ✅ Task 2 undone
                TaskListManager.Instance.SetTaskCompleted(1, false);
            }
        }
    }

    public void EjectMotherboard()
    {
        AVR avr = FindObjectOfType<AVR>();

        if (!motherboardInstalled)
        {
            TroubleshootManager.Instance.ShowMessage(
                "No motherboard is currently installed in the system unit.",
                true
            );
            Debug.Log("Eject failed: No motherboard installed.");
            return;
        }

        if (avr != null && avr.IsOn())
        {
            TroubleshootManager.Instance.ShowMessage(
                "Cannot eject Motherboard while AVR is turned ON. Please turn off AVR first.",
                true
            );
            Debug.Log("Eject blocked: AVR is ON.");
            return;
        }

        // ✅ Eject motherboard
        motherboardInstalled = false;
        TroubleshootManager.Instance.ShowMessage("Motherboard ejected from the system unit.", false);

        if (motherboard != null)
        {
            motherboard.gameObject.SetActive(true); // re‑enable
            motherboard.transform.SetParent(null);  // detach from system unit
            motherboard.transform.position = motherboardOriginalPosition; // reset position
            motherboard.transform.rotation = motherboardOriginalRotation; // reset rotation

            Debug.Log("Motherboard ejected and reset to original transform.");

            // ✅ Task 2 undone
            TaskListManager.Instance.SetTaskCompleted(1, false);
        }
    }

    void Start()
    {
        motherboard = FindObjectOfType<Motherboard>();
        if (motherboard != null)
        {
            motherboardOriginalPosition = motherboard.transform.position;
            motherboardOriginalRotation = motherboard.transform.rotation;
        }
    }

    // Check if motherboard is currently installed
    public bool IsMotherboardInstalled()
    {
        return motherboardInstalled;
    }
}
