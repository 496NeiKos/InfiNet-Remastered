using UnityEngine;

public class MotherboardPhaseManager : MonoBehaviour
{
    [SerializeField] private GameObject phase1Root;
    [SerializeField] private GameObject phase2Root;

    public Transform GetPhase1Root() => phase1Root != null ? phase1Root.transform : null;
    public Transform GetPhase2Root() => phase2Root != null ? phase2Root.transform : null;

    public void SetPhase1Interactive()
    {
        SetPhase1Enabled(true);
        SetPhase2Enabled(false);
    }

    public void SetPhase2Interactive()
    {
        SetPhase1Enabled(false);
        SetPhase2Enabled(true);
    }

    private void SetPhase1Enabled(bool enabled)
    {
        if (phase1Root == null) return;

        // Toggle screws and cables — both script and collider
        foreach (var sc in phase1Root.GetComponentsInChildren<ScrewController>(true))
        {
            sc.enabled = enabled;
            Collider2D col = sc.GetComponent<Collider2D>();
            if (col != null) col.enabled = enabled;
        }

        foreach (var cs in phase1Root.GetComponentsInChildren<CableSlot>(true))
        {
            cs.enabled = enabled;
            Collider2D col = cs.GetComponent<Collider2D>();
            if (col != null) col.enabled = enabled;
        }

        // Toggle MBCable — blocks hold-to-detach and drag when Phase 2 is active
        // Skip cables that are already detached — disabling them mid-flight breaks drag
        foreach (var mc in phase1Root.GetComponentsInChildren<MBCable>(true))
        {
            if (!enabled && mc.IsDetached) continue; // don't disable a cable being dragged
            mc.enabled = enabled;
            Collider2D col = mc.GetComponent<Collider2D>();
            if (col != null) col.enabled = enabled;
        }
    }

    private void SetPhase2Enabled(bool enabled)
    {
        if (phase2Root == null) return;

        // Toggle drag and right-click interaction on all Phase 2 components
        foreach (var dp in phase2Root.GetComponentsInChildren<DragPrefab>(true))
            dp.enabled = enabled;

        foreach (var pi in phase2Root.GetComponentsInChildren<PrefabInteraction>(true))
            pi.enabled = enabled;

        foreach (var lk in phase2Root.GetComponentsInChildren<CPULockController>(true))
            lk.enabled = enabled;

        // Toggle colliders so components cannot be raycasted when phase is inactive
        foreach (var col in phase2Root.GetComponentsInChildren<Collider2D>(true))
            col.enabled = enabled;
    }
}