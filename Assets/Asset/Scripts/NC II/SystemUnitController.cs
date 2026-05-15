using UnityEngine;

public class SystemUnitController : MonoBehaviour
{
    [Header("Editing Panel View")]
    public GameObject systemUnitSide;

    [Header("Cover & Hardware")]
    public GameObject systemUnitCover;
    public GameObject systemUnitHardwareComponents;

    [Header("References")]
    [SerializeField] private CoverController coverController;

    private Vector3 _sideOriginalLocalPos;
    private Quaternion _sideOriginalLocalRot;

    void Start()
    {
        if (systemUnitSide != null)
            systemUnitSide.SetActive(false);
    }

    public void ShowDetailAtCenter()
    {
        if (systemUnitSide == null) return;

        _sideOriginalLocalPos = systemUnitSide.transform.localPosition;
        _sideOriginalLocalRot = systemUnitSide.transform.localRotation;

        if (GameManager.Instance != null && GameManager.Instance.editingPanel != null)
        {
            RectTransform rect = GameManager.Instance.editingPanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector3 panelCenter = rect.TransformPoint(
                    new Vector3(rect.rect.center.x, rect.rect.center.y, 0f)
                );
                panelCenter.z = 0f;

                systemUnitSide.transform.position = panelCenter;
                systemUnitSide.transform.rotation = Quaternion.identity;
            }
        }

        // Cover is always active (visible)
        if (systemUnitCover != null)
            systemUnitCover.SetActive(true);

        // ✅ Check cover state to decide hardware visibility
        if (systemUnitHardwareComponents != null)
        {
            if (coverController != null && coverController.IsOpen())
            {
                // Cover is open → hardware should be visible
                systemUnitHardwareComponents.SetActive(true);
            }
            else
            {
                // Cover is closed → hardware should be hidden
                systemUnitHardwareComponents.SetActive(false);
            }
        }

        systemUnitSide.SetActive(true);
    }

    public void HideDetail()
    {
        if (systemUnitSide == null) return;

        systemUnitSide.SetActive(false);

        systemUnitSide.transform.localPosition = _sideOriginalLocalPos;
        systemUnitSide.transform.localRotation = _sideOriginalLocalRot;
    }

    /// <summary>
    /// Called when cover slides open.
    /// Cover stays active. Only reveals hardware components.
    /// </summary>
    public void RemoveCover()
    {
        if (systemUnitHardwareComponents != null)
            systemUnitHardwareComponents.SetActive(true);
    }

    /// <summary>
    /// Called when cover slides back to closed position.
    /// Hides hardware components.
    /// </summary>
    public void AttachCover()
    {
        if (systemUnitHardwareComponents != null)
            systemUnitHardwareComponents.SetActive(false);
    }
}