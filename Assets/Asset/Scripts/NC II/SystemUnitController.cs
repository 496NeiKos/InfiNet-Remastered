using UnityEngine;

public class SystemUnitController : MonoBehaviour
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
    [SerializeField] private SystemUnitViewController viewController;

    void Start()
    {
        systemUnitFront?.SetActive(false);
        systemUnitSide?.SetActive(false);
        systemUnitBack?.SetActive(false);
    }

    public void ShowDetailAtCenter()
    {
        viewController?.WireButtons();
        viewController?.ShowLastActive();

        CenterActiveView();

        if (systemUnitCover != null)
            systemUnitCover.SetActive(true);

        if (systemUnitHardwareComponents != null)
            systemUnitHardwareComponents.SetActive(coverController != null && coverController.IsOpen());
    }

    private void CenterActiveView()
    {
        GameObject active = GetActiveView();
        if (active == null || GameManager.Instance?.editingPanel == null) return;

        RectTransform rect = GameManager.Instance.editingPanel.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector3 center = rect.TransformPoint(new Vector3(rect.rect.center.x, rect.rect.center.y, 0f));
            center.z = 0f;
            active.transform.position = center;
        }
    }

    public void HideDetail()
    {
        systemUnitFront?.SetActive(false);
        systemUnitSide?.SetActive(false);
        systemUnitBack?.SetActive(false);
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

    private GameObject GetActiveView()
    {
        if (systemUnitFront != null && systemUnitFront.activeSelf) return systemUnitFront;
        if (systemUnitSide != null && systemUnitSide.activeSelf) return systemUnitSide;
        if (systemUnitBack != null && systemUnitBack.activeSelf) return systemUnitBack;
        return systemUnitSide;
    }
}