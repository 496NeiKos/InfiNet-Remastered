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

        viewController?.ShowLastActive();
    }

    public void HideDetail()
    {
        viewController?.HideIndicator();
        monitorFront?.SetActive(false);
        monitorBack?.SetActive(false);
    }

    private SpriteRenderer[] _renderers;
    private int[] _originalOrders;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        _originalOrders = new int[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
            _originalOrders[i] = _renderers[i].sortingOrder;
    }

    public void PushBehind()
    {
        foreach (var sr in _renderers)
            sr.sortingOrder = 0;
    }

    public void RestoreLayer()
    {
        for (int i = 0; i < _renderers.Length; i++)
            _renderers[i].sortingOrder = _originalOrders[i];
    }
}