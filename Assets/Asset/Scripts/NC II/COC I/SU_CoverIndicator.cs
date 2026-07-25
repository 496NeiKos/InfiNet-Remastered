using UnityEngine;

/// <summary>
/// Attach to Interactive_Indicator_SU_Cover.
/// Mirrors the world position (and optionally rotation) of the SystemUnitCover
/// so the indicator slides in sync with the CoverController animation.
/// </summary>
public class SU_CoverIndicator : MonoBehaviour
{
    [SerializeField] private CoverController coverTarget;
    [SerializeField] private Vector3 positionOffset;
    [SerializeField] private bool matchRotation = false;

    private void LateUpdate()
    {
        if (coverTarget == null) return;

        transform.position = coverTarget.transform.position + positionOffset;

        if (matchRotation)
            transform.rotation = coverTarget.transform.rotation;
    }
}
