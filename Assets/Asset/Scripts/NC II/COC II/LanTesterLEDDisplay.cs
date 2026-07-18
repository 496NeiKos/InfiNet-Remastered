using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the 9-LED sequence panel on one port face of the LAN tester.
///
/// Place one instance on Port1LEDPanel (wire to CableEnd1) and another on
/// Port2LEDPanel (wire to CableEnd2). Both share the same port and switch references.
///
/// Behaviour mirrors a real LAN tester:
///   • LEDs 1-8 pulse one at a time in order while active.
///   • Green  = wire at that slot matches T568A or T568B standard.
///   • Red    = wrong wire color at that slot (miswired).
///   • LED G  = always off — UTP cable carries no shield/ground.
///   • All LEDs off when cable is not installed or switch is off.
///
/// Activation condition: cable installed in LAN tester port AND switch on.
/// (LanTesterPortController already requires both ends crimped before
///  accepting the cable, so IsCrimped is implicitly guaranteed.)
/// </summary>
public class LanTesterLEDDisplay : MonoBehaviour
{
    [Header("Cable End to Inspect")]
    [Tooltip("NetworkCableEndController this panel reads wire data from.\nPort1LEDPanel → CableEnd1, Port2LEDPanel → CableEnd2.")]
    [SerializeField] private NetworkCableEndController cableEnd;

    [Header("Tester State")]
    [Tooltip("LanTesterPortController in the TopDetail view — provides IsCableInstalled.")]
    [SerializeField] private LanTesterPortController port;
    [Tooltip("LanTesterSwitchController — provides IsOn.")]
    [SerializeField] private LanTesterSwitchController lanSwitch;

    [Header("LED SpriteRenderers")]
    [Tooltip("Exactly 9 SpriteRenderers: index 0=LED1, 1=LED2, ..., 7=LED8, 8=LED_G.\nAll should start inactive in the scene — Start() enforces this.")]
    [SerializeField] private SpriteRenderer[] ledRenderers;

    [Header("LED Sprites")]
    [Tooltip("Sprite shown when a wire is in the correct slot.")]
    [SerializeField] private Sprite ledGreen;
    [Tooltip("Sprite shown when a wire is in the wrong slot.")]
    [SerializeField] private Sprite ledRed;

    [Header("Timing")]
    [Tooltip("Seconds each LED stays lit before advancing to the next one.")]
    [SerializeField] private float stepDelay = 0.5f;

    // ----------------------------------------------------------------
    //  T568 wire standards — slot index (0-7) → expected wireColorIndex
    //  wireColorIndex key:  0=W/Green  1=Green  2=W/Orange  3=Blue
    //                       4=W/Blue   5=Orange  6=W/Brown   7=Brown
    // ----------------------------------------------------------------
    private static readonly int[] T568A = { 0, 1, 2, 3, 4, 5, 6, 7 };
    private static readonly int[] T568B = { 2, 5, 0, 3, 4, 1, 6, 7 };

    private Coroutine _sequence;
    private bool _running;

    private void Start() => SetAllOff();

    private void Update()
    {
        bool shouldRun = port      != null && port.IsCableInstalled
                      && lanSwitch != null && lanSwitch.IsOn;

        if (shouldRun && !_running)
        {
            _running  = true;
            _sequence = StartCoroutine(SequenceLoop());
        }
        else if (!shouldRun && _running)
        {
            _running = false;
            if (_sequence != null) { StopCoroutine(_sequence); _sequence = null; }
            SetAllOff();
        }
    }

    private IEnumerator SequenceLoop()
    {
        while (true)
        {
            // Pulse LEDs 1–8 (slot indices 0–7) one at a time
            for (int slot = 0; slot < 8; slot++)
            {
                SetAllOff();

                if (ledRenderers != null && slot < ledRenderers.Length
                    && ledRenderers[slot] != null)
                {
                    int  colorAtSlot = cableEnd.GetWireColorAtSlot(slot);
                    bool correct     = colorAtSlot >= 0
                                    && (colorAtSlot == T568A[slot]
                                     || colorAtSlot == T568B[slot]);

                    ledRenderers[slot].sprite = correct ? ledGreen : ledRed;
                    ledRenderers[slot].gameObject.SetActive(true);
                }

                yield return new WaitForSeconds(stepDelay);
            }

            // LED G (index 8) — UTP has no shield ground; gap pause only, stays off
            SetAllOff();
            yield return new WaitForSeconds(stepDelay);
        }
    }

    private void SetAllOff()
    {
        if (ledRenderers == null) return;
        foreach (var sr in ledRenderers)
            if (sr != null) sr.gameObject.SetActive(false);
    }
}
