using UnityEngine;

/// <summary>
/// On AVR root. Handles editing panel view switching.
/// Front view: AVR power button.
/// Back view: PSUPort (BackPortSlot).
/// </summary>
public class AVRController : MonoBehaviour, IHardwareController
{
    [Header("Views")]
    [SerializeField] private GameObject avrFront;
    [SerializeField] private GameObject avrBack;

    [Header("References")]
    [SerializeField] private HardwareViewController viewController;

    private void Start()
    {
        if (viewController == null)
            viewController = GetComponent<HardwareViewController>();

        avrFront?.SetActive(false);
        avrBack?.SetActive(false);
    }

    public void ShowDetailAtCenter()
    {
        if (viewController == null)
            viewController = GetComponent<HardwareViewController>();

        viewController?.SetDefaultIfNone(avrFront);
        viewController?.WireButtons();
        viewController?.ShowLastActive();
    }

    public void HideDetail()
    {
        viewController?.HideButtons();
        avrFront?.SetActive(false);
        avrBack?.SetActive(false);
    }
}