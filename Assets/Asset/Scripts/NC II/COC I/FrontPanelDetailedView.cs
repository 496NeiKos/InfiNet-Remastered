using UnityEngine;

/// <summary>
/// On the FrontPanelConnectorDetailed child GameObject. Enables sub-port CablePorts and
/// sub-cable CableBehaviors (both ProperSubPorts and LoosePorts) when the detail view opens,
/// and disables them when it closes. Also pushes the listed parent renderers to sorting
/// order -1 while the detail view is open so they visually recede behind the sub-pin panel.
/// </summary>
public class FrontPanelDetailedView : MonoBehaviour
{
    [Tooltip("SpriteRenderers to push to sorting order -1 while this detail view is active " +
             "(e.g. FrontPanelConnector_Port, FrontPanelConnector_Cable, FrontPanelConnector_Cable_Outline).")]
    [SerializeField] private SpriteRenderer[] recessRenderers;

    [Tooltip("The Phase 1 CableBehavior (FrontPanelConnector_Cable). Its colliders are disabled " +
             "while the detail view is open so it cannot intercept clicks meant for the sub-cables.")]
    [SerializeField] private CableBehavior phase1Cable;

    private int[] _originalSortingOrders;

    private void Awake()
    {
        if (recessRenderers == null) return;
        _originalSortingOrders = new int[recessRenderers.Length];
        for (int i = 0; i < recessRenderers.Length; i++)
            _originalSortingOrders[i] = recessRenderers[i] != null ? recessRenderers[i].sortingOrder : 0;
    }

    private void OnEnable()
    {
        NCIITaskListManager.CheckConditions();
        SetRecessRenderers(-1);
        SetPhase1CableCollidersEnabled(false);

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
        RestoreRecessRenderers();
        SetPhase1CableCollidersEnabled(true);

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

    private void SetPhase1CableCollidersEnabled(bool enabled)
    {
        if (phase1Cable == null) return;
        phase1Cable.gameObject.SetActive(enabled);
    }

    private void SetRecessRenderers(int order)
    {
        if (recessRenderers == null) return;
        foreach (var sr in recessRenderers)
            if (sr != null) sr.sortingOrder = order;
    }

    private void RestoreRecessRenderers()
    {
        if (recessRenderers == null || _originalSortingOrders == null) return;
        for (int i = 0; i < recessRenderers.Length && i < _originalSortingOrders.Length; i++)
            if (recessRenderers[i] != null)
                recessRenderers[i].sortingOrder = _originalSortingOrders[i];
    }
}
