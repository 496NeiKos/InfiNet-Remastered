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
        foreach (var sc in GetComponentsInChildren<ScrewController>(true))
        {
            sc.enabled = true;
            Collider2D col = sc.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }

        foreach (var cs in GetComponentsInChildren<CableSlot>(true))
        {
            cs.enabled = true;
            Collider2D col = cs.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }

        foreach (var mc in GetComponentsInChildren<MBCable>(true))
        {
            mc.enabled = true;
            Collider2D col = mc.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }
    }
}
