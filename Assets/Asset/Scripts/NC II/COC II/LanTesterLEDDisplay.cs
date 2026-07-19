using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the 9-LED sequence panel on one port face of the LAN tester,
/// accurately simulating the master/remote behaviour of a real cable tester.
///
/// MASTER panel (isMasterPanel = true  — wire to CableEnd1):
///   Pulses LEDs 1–8 in order, always green.
///   Represents the master unit transmitting a signal sequentially on each pin.
///
/// REMOTE panel (isMasterPanel = false — wire cableEnd → CableEnd2, masterCableEnd → CableEnd1):
///   For each master step N the script traces the physical wire:
///     1. Reads which wire color occupies slot N on the master end (CableEnd1).
///     2. Finds which slot that same wire color occupies on this end (CableEnd2).
///     3. Lights THAT slot's LED green.
///
///   The resulting pattern reveals the cable type without any explicit label:
///     Straight-through  →  remote LEDs pulse 1, 2, 3, 4, 5, 6, 7, 8  (same order as master)
///     Crossover         →  remote LEDs pulse in crossed order, e.g. 3, 6, 1, 4, 5, 2, 7, 8
///     Bad crimp         →  remote LEDs pulse in a random / incomplete order
///
/// LED G (index 8) is always off — UTP cable carries no shield/ground.
/// All LEDs turn off when the cable is removed or the switch is off.
/// </summary>
public class LanTesterLEDDisplay : MonoBehaviour
{
    /// <summary>
    /// Fired by the master panel after each complete 8-pin + G-gap cycle.
    /// NetworkCableTaskManager subscribes to this to evaluate Tasks 12 and 26.
    /// </summary>
    public static event Action OnSequenceComplete;
    [Header("Panel Mode")]
    [Tooltip("True = Master panel (pulses 1–8 in order).\nFalse = Remote panel (traces wire continuity from master end to this end).")]
    [SerializeField] private bool isMasterPanel = true;

    [Header("Cable Ends")]
    [Tooltip("The cable end this panel reads from.\nMaster panel → CableEnd1.  Remote panel → CableEnd2.")]
    [SerializeField] private NetworkCableEndController cableEnd;
    [Tooltip("Remote panel only — the master-side cable end (CableEnd1).\nLeave null on the master panel.")]
    [SerializeField] private NetworkCableEndController masterCableEnd;

    [Header("Tester State")]
    [Tooltip("LanTesterPortController — provides IsCableInstalled.")]
    [SerializeField] private LanTesterPortController port;
    [Tooltip("LanTesterSwitchController — provides IsOn.")]
    [SerializeField] private LanTesterSwitchController lanSwitch;

    [Header("LED SpriteRenderers")]
    [Tooltip("Exactly 9 SpriteRenderers: index 0=LED1 … 7=LED8, 8=LED_G.\nAll should start inactive — Start() enforces this.")]
    [SerializeField] private SpriteRenderer[] ledRenderers;

    [Header("LED Sprite")]
    [Tooltip("Sprite shown when a LED is lit (wire continuity confirmed on that pin).")]
    [SerializeField] private Sprite ledGreen;

    [Header("Timing")]
    [Tooltip("Seconds each LED stays lit before advancing to the next pin.")]
    [SerializeField] private float stepDelay = 0.5f;

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
            for (int masterSlot = 0; masterSlot < 8; masterSlot++)
            {
                SetAllOff();

                if (ledRenderers != null)
                {
                    if (isMasterPanel)
                    {
                        // Master always transmits on slots 1–8 in order.
                        LightSlot(masterSlot);
                    }
                    else
                    {
                        // Remote: trace which physical wire the master is sending through,
                        // then find where that wire lands on this end and light that LED.
                        NetworkCableEndController source = masterCableEnd != null ? masterCableEnd : cableEnd;
                        int wireColor  = source.GetWireColorAtSlot(masterSlot);
                        int remoteSlot = cableEnd != null ? cableEnd.GetSlotForWireColor(wireColor) : -1;
                        if (remoteSlot >= 0) LightSlot(remoteSlot);
                    }
                }

                yield return new WaitForSeconds(stepDelay);
            }

            // LED G gap — UTP has no shield; just a pause, stays off.
            SetAllOff();
            yield return new WaitForSeconds(stepDelay);

            // Notify task manager that one full pin sequence has completed (master panel only).
            if (isMasterPanel)
                OnSequenceComplete?.Invoke();
        }
    }

    private void LightSlot(int slot)
    {
        if (slot < 0 || ledRenderers == null || slot >= ledRenderers.Length) return;
        if (ledRenderers[slot] == null) return;
        ledRenderers[slot].sprite = ledGreen;
        ledRenderers[slot].gameObject.SetActive(true);
    }

    private void SetAllOff()
    {
        if (ledRenderers == null) return;
        foreach (var sr in ledRenderers)
            if (sr != null) sr.gameObject.SetActive(false);
    }
}
