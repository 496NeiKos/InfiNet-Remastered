/*
 * ================================================================
 *  UNITY SETUP GUIDE — DeviceManagerController
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to the DeviceManagerPanel GameObject.
 *
 *  HIERARCHY
 *
 *    DeviceManagerPanel  (this script here, start INACTIVE)
 *      ├─ DeviceManagerNavBar
 *      │    └─ DM_NavFlow
 *      │         ├─ DM_NavControl  [4 buttons — no function yet]
 *      │         ├─ DM_Minimize → OnClick: DeviceManagerController.Minimize()
 *      │         └─ DM_Exit     → OnClick: DeviceManagerController.Exit()
 *      └─ DeviceManagerContent  (VerticalLayoutGroup)
 *           ├─ btn1   → entries[0].toggleButton  (Button)
 *           ├─ p1     → entries[0].panel         (GameObject, start INACTIVE)
 *           ├─ bt2    → entries[1].toggleButton
 *           ├─ p2     → entries[1].panel
 *           ├─ bt3    → entries[2].toggleButton
 *           ├─ p3     → entries[2].panel
 *           ├─ btn4   → entries[3].toggleButton
 *           ├─ p4     → entries[3].panel
 *           ├─ btn5   → entries[4].toggleButton
 *           ├─ p5     → entries[4].panel
 *           ├─ btn6   → entries[5].toggleButton
 *           ├─ p6     → entries[5].panel
 *           ├─ btn7   → entries[6].toggleButton
 *           ├─ p7     → entries[6].panel
 *           ├─ btn8   → entries[7].toggleButton
 *           ├─ p8     → entries[7].panel
 *           ├─ btn9   → entries[8].toggleButton
 *           ├─ p9     → entries[8].panel
 *           ├─ btn10  → entries[9].toggleButton
 *           └─ p10    → entries[9].panel
 *
 *  INSPECTOR ASSIGNMENTS
 *    entries  — array of 10 entries, each pairing a Button with its panel GameObject.
 *               Assign in order: entries[0] = btn1 + p1, entries[1] = bt2 + p2, etc.
 *    Buttons do NOT need OnClick wired manually — the script wires them in Awake.
 *
 *  BUTTON OnClick WIRING  (manual — navbar only)
 *    DM_Minimize → DeviceManagerController.Minimize()
 *    DM_Exit     → DeviceManagerController.Exit()
 *
 *  HOW IT WORKS
 *    Each entries[i].toggleButton independently toggles entries[i].panel on or off.
 *    Multiple panels can be open at the same time — toggling one does not affect others.
 *    Minimize(): hides the entire panel while preserving which sub-panels are open.
 *    Exit(): closes all sub-panels, then hides the panel.
 * ================================================================
 */

using UnityEngine;
using UnityEngine.UI;

public class DeviceManagerController : MonoBehaviour
{
    // ----------------------------------------------------------------
    //  Data
    // ----------------------------------------------------------------

    [System.Serializable]
    public class DeviceEntry
    {
        public Button     toggleButton;
        public GameObject panel;
    }

    // ----------------------------------------------------------------
    //  Inspector
    // ----------------------------------------------------------------

    [Header("Category Entries  (button + panel pairs, index 0–9)")]
    [SerializeField] private DeviceEntry[] entries = new DeviceEntry[10];

    // ----------------------------------------------------------------
    //  Lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        for (int i = 0; i < entries.Length; i++)
        {
            int captured = i;
            if (entries[i].toggleButton != null)
                entries[i].toggleButton.onClick.AddListener(() => OnToggle(captured));

            // Ensure panels start closed
            if (entries[i].panel != null)
                entries[i].panel.SetActive(false);
        }
    }

    // ----------------------------------------------------------------
    //  Toggle logic
    // ----------------------------------------------------------------

    private void OnToggle(int index)
    {
        if (index < 0 || index >= entries.Length) return;

        var panel = entries[index].panel;
        if (panel == null) return;

        bool nowActive = !panel.activeSelf;
        panel.SetActive(nowActive);

        Debug.Log($"[DeviceManagerController] Entry {index} {(nowActive ? "opened" : "closed")}.");
    }

    // ----------------------------------------------------------------
    //  Navbar buttons
    // ----------------------------------------------------------------

    public void Minimize()
    {
        gameObject.SetActive(false);
        Debug.Log("[DeviceManagerController] Minimized — state preserved.");
    }

    public void Exit()
    {
        CloseAll();
        gameObject.SetActive(false);
        Debug.Log("[DeviceManagerController] Exited — state reset.");
    }

    // ----------------------------------------------------------------
    //  Helpers
    // ----------------------------------------------------------------

    private void CloseAll()
    {
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].panel != null)
                entries[i].panel.SetActive(false);
        }
    }
}
