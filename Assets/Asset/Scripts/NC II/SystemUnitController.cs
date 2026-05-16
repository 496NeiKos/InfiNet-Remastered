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
        // Auto-find if not wired in inspector
        if (coverController == null)
            coverController = GetComponentInChildren<CoverController>(true);

        if (viewController == null)
            viewController = GetComponent<SystemUnitViewController>();

        systemUnitFront?.SetActive(false);
        systemUnitSide?.SetActive(false);
        systemUnitBack?.SetActive(false);
    }

    public void ShowDetailAtCenter()
    {
        // Auto-find in case Start() hasn't run yet or references were cleared
        if (coverController == null)
            coverController = GetComponentInChildren<CoverController>(true);

        if (viewController == null)
            viewController = GetComponent<SystemUnitViewController>();

        viewController?.WireButtons();
        viewController?.ShowLastActive();

        if (systemUnitCover != null)
            systemUnitCover.SetActive(true);

        if (systemUnitHardwareComponents != null)
            systemUnitHardwareComponents.SetActive(coverController != null && coverController.IsOpen());
    }

    public void HideDetail()
    {
        viewController?.HideButtons();

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

    // Used by SystemUnitViewController to center the active view
    public GameObject GetActiveView()
    {
        if (systemUnitFront != null && systemUnitFront.activeSelf) return systemUnitFront;
        if (systemUnitSide != null && systemUnitSide.activeSelf) return systemUnitSide;
        if (systemUnitBack != null && systemUnitBack.activeSelf) return systemUnitBack;
        return systemUnitSide;
    }
}