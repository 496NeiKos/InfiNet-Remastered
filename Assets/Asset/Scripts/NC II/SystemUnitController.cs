using UnityEngine;

public class SystemUnitController : MonoBehaviour
{
    [Header("Editing Panel View")]
    public GameObject systemUnitSide;

    [Header("Cover & Hardware")]
    public GameObject systemUnitCover;
    public GameObject systemUnitHardwareComponents;

    private Vector3 _sideOriginalLocalPos;
    private Quaternion _sideOriginalLocalRot;

    void Start()
    {
        // Default: side view hidden, root sprite is always visible
        systemUnitSide.SetActive(false);
    }

    public void ShowDetailAtCenter()
    {
        _sideOriginalLocalPos = systemUnitSide.transform.localPosition;
        _sideOriginalLocalRot = systemUnitSide.transform.localRotation;

        // Center on the editing panel
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

        // Cover starts active (blocking hardware)
        if (systemUnitCover != null)
            systemUnitCover.SetActive(true);

        // Hardware components start disabled (hidden behind cover)
        if (systemUnitHardwareComponents != null)
            systemUnitHardwareComponents.SetActive(false);

        systemUnitSide.SetActive(true);
    }

    public void HideDetail()
    {
        systemUnitSide.SetActive(false);

        systemUnitSide.transform.localPosition = _sideOriginalLocalPos;
        systemUnitSide.transform.localRotation = _sideOriginalLocalRot;
    }

    /// <summary>
    /// Called when the cover is removed (all screws unscrewed).
    /// </summary>
    public void RemoveCover()
    {
        if (systemUnitCover != null)
            systemUnitCover.SetActive(false);

        if (systemUnitHardwareComponents != null)
            systemUnitHardwareComponents.SetActive(true);

        Debug.Log("[SystemUnitController] Cover removed, hardware components revealed");
    }

    /// <summary>
    /// Called when the cover is put back on.
    /// </summary>
    public void AttachCover()
    {
        if (systemUnitCover != null)
            systemUnitCover.SetActive(true);

        if (systemUnitHardwareComponents != null)
            systemUnitHardwareComponents.SetActive(false);

        Debug.Log("[SystemUnitController] Cover attached, hardware components hidden");
    }
}