using UnityEngine;

/// <summary>
/// On the FrontPanelConnectorDetailed child GameObject. Enables sub-port CablePorts and
/// sub-cable CableBehaviors (both ProperSubPorts and LoosePorts) when the detail view opens,
/// and disables them when it closes. Mirrors the pattern from HDDDetailView.
/// </summary>
public class FrontPanelDetailedView : MonoBehaviour
{
    private void OnEnable()
    {
        foreach (var cp in GetComponentsInChildren<CablePort>(true))
        {
            cp.enabled = true;
            foreach (Collider2D col in cp.GetComponents<Collider2D>())
                col.enabled = true;
        }

        foreach (var cb in GetComponentsInChildren<CableBehavior>(true))
        {
            cb.enabled = true;
            foreach (Collider2D col in cb.GetComponents<Collider2D>())
                col.enabled = true;
        }
    }

    private void OnDisable()
    {
        foreach (var cp in GetComponentsInChildren<CablePort>(true))
        {
            cp.enabled = false;
            foreach (Collider2D col in cp.GetComponents<Collider2D>())
                col.enabled = false;
        }

        foreach (var cb in GetComponentsInChildren<CableBehavior>(true))
        {
            if (cb.IsDetached) continue; // never disable a cable mid-drag
            cb.enabled = false;
            foreach (Collider2D col in cb.GetComponents<Collider2D>())
                col.enabled = false;
        }
    }
}
