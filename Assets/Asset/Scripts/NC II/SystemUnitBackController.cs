using UnityEngine;

/// <summary>
/// Drives the system unit back view:
///   - Enables/disables motherboard-dependent ports based on whether the motherboard is installed.
///   - Enables/disables the PSU port based on whether the PSU is installed.
///   - Swaps the back panel sprite across four states (both / MB only / PSU only / neither).
/// Attach to the systemUnitBack GameObject (or any persistent object that can hold these refs).
/// </summary>
public class SystemUnitBackController : MonoBehaviour
{
    [Header("Side Hardware Slots")]
    [SerializeField] private Transform motherboardSlot;
    [SerializeField] private Transform psuSlot;

    [Header("Back Ports")]
    [Tooltip("The entire PSU object on the back (body + port) — disabled when PSU is not installed in the side slot.")]
    [SerializeField] private GameObject suPSUBackObject;
    [Tooltip("All other back ports (VGA, USB, etc.) — disabled when motherboard is not installed.")]
    [SerializeField] private GameObject[] motherboardDependentPorts;

    [Header("Back Panel Sprite")]
    [SerializeField] private SpriteRenderer backPanelSR;
    [SerializeField] private Sprite spriteBothInstalled;
    [SerializeField] private Sprite spriteMBOnlyInstalled;
    [SerializeField] private Sprite spritePSUOnlyInstalled;
    [SerializeField] private Sprite spriteNeitherInstalled;

    private bool _prevMB;
    private bool _prevPSU;

    private void OnEnable()
    {
        // Force a full refresh whenever the back panel becomes visible.
        _prevMB = !MBInstalled();
        _prevPSU = !PSUInstalled();
    }

    private void Update()
    {
        bool mb  = MBInstalled();
        bool psu = PSUInstalled();

        if (mb == _prevMB && psu == _prevPSU) return;

        _prevMB  = mb;
        _prevPSU = psu;

        Refresh(mb, psu);
    }

    private void Refresh(bool mb, bool psu)
    {
        foreach (GameObject port in motherboardDependentPorts)
            if (port != null) port.SetActive(mb);

        if (suPSUBackObject != null)
            suPSUBackObject.SetActive(psu);

        if (backPanelSR == null) return;

        backPanelSR.sprite = (mb, psu) switch
        {
            (true,  true)  => spriteBothInstalled,
            (true,  false) => spriteMBOnlyInstalled,
            (false, true)  => spritePSUOnlyInstalled,
            _              => spriteNeitherInstalled,
        };
    }

    private bool MBInstalled()  => motherboardSlot != null && motherboardSlot.childCount > 0;
    private bool PSUInstalled() => psuSlot         != null && psuSlot.childCount         > 0;
}
