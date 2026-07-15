using UnityEngine;

public class SystemUnitController : MonoBehaviour, IHardwareController
{
    [Header("Editing Panel Views")]
    public GameObject systemUnitFront;
    public GameObject systemUnitSide;
    public GameObject systemUnitBack;

    [Header("Cover & Hardware")]
    public GameObject systemUnitCover;
    public GameObject systemUnitHardwareComponents;

    [Header("References")]
    [SerializeField] private CoverController coverController;
    [SerializeField] private HardwareViewController viewController;
    [SerializeField] private MonitorController monitorController;

    void Start()
    {
        if (coverController == null)
            coverController = GetComponentInChildren<CoverController>(true);

        if (viewController == null)
            viewController = GetComponent<HardwareViewController>();

        systemUnitFront?.SetActive(false);
        systemUnitSide?.SetActive(false);
        systemUnitBack?.SetActive(false);
    }

    public void ShowDetailAtCenter()
    {
        if (coverController == null)
            coverController = GetComponentInChildren<CoverController>(true);

        if (viewController == null)
            viewController = GetComponent<HardwareViewController>();

        var layer = GameManager.Instance?.firstLayer;
        if (layer != null)
        {
            RectTransform rect = layer.GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector3 panelCenter = rect.TransformPoint(
                    new Vector3(rect.rect.center.x, rect.rect.center.y, 0f));
                panelCenter.z = 0f;
                transform.position = panelCenter;
            }
        }

        monitorController?.PushBehind();

        viewController?.ShowLastActive();

        if (systemUnitCover != null)
            systemUnitCover.SetActive(true);

        if (systemUnitHardwareComponents != null)
            systemUnitHardwareComponents.SetActive(coverController != null && coverController.IsOpen());
    }

    public void HideDetail()
    {
        viewController?.HideIndicator();

        systemUnitFront?.SetActive(false);
        systemUnitSide?.SetActive(false);
        systemUnitBack?.SetActive(false);

        monitorController?.RestoreLayer();
    }

    public void RemoveCover()
    {
        if (systemUnitHardwareComponents != null)
            systemUnitHardwareComponents.SetActive(true);
    }

    public void AttachCover()
    {
        if (systemUnitHardwareComponents != null)
            systemUnitHardwareComponents.SetActive(false);
    }
}