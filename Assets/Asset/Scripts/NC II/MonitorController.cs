using UnityEngine;

/// <summary>
/// On Monitor root. Handles editing panel view switching.
/// Front view: empty for now.
/// Back view: VGAPort (BackPortSlot).
/// </summary>
public class MonitorController : MonoBehaviour, IHardwareController
{
    [Header("Views")]
    [SerializeField] private GameObject monitorFront;
    [SerializeField] private GameObject monitorBack;

    [Header("References")]
    [SerializeField] private HardwareViewController viewController;

    private void Start()
    {
        if (viewController == null)
            viewController = GetComponent<HardwareViewController>();

        monitorFront?.SetActive(false);
        monitorBack?.SetActive(false);
    }

    public void ShowDetailAtCenter()
    {
        if (viewController == null)
            viewController = GetComponent<HardwareViewController>();

        viewController?.SetDefaultIfNone(monitorFront);
        viewController?.WireButtons();
        viewController?.ShowLastActive();
    }

    public void HideDetail()
    {
        viewController?.HideButtons();
        monitorFront?.SetActive(false);
        monitorBack?.SetActive(false);
    }
}