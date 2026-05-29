using UnityEngine;

/// <summary>
/// On the "HDDDetailed" child of the HDD root.
/// Activated/deactivated by DetailViewManager when the inner editing panel opens or closes.
/// Ensures all screws and cables become interactive when the detail view opens.
/// Cables use MBCable hold-to-uninstall; screws use ScrewController trigger logic.
/// </summary>
public class HDDDetailView : MonoBehaviour
{
    private void OnEnable()
    {
        GetComponentInParent<HDDController>()?.SetCableIndicatorForView(true);
        foreach (var sc in GetComponentsInChildren<ScrewController>(true))
        {
            sc.enabled = true;
            foreach (Collider2D col in sc.GetComponents<Collider2D>())
                col.enabled = true;
        }

        foreach (var cs in GetComponentsInChildren<CableSlot>(true))
        {
            cs.enabled = true;
            foreach (Collider2D col in cs.GetComponents<Collider2D>())
                col.enabled = true;
        }

        foreach (var mc in GetComponentsInChildren<MBCable>(true))
        {
            mc.enabled = true;
            foreach (Collider2D col in mc.GetComponents<Collider2D>())
                col.enabled = true;
        }
    }

    private void OnDisable()
    {
        GetComponentInParent<HDDController>()?.SetCableIndicatorForView(false);
    }
}
