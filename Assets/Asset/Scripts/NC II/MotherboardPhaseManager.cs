using UnityEngine;

public class MotherboardPhaseManager : MonoBehaviour
{
    public enum Phase { Phase1, Phase2 }

    [SerializeField] private GameObject phase1Root;
    [SerializeField] private GameObject phase2Root;
    [SerializeField] private GPUPhase1CableInteraction gpuPhase1CableInteraction;

    public Phase CurrentPhase { get; private set; } = Phase.Phase1;

    public Transform GetPhase1Root() => phase1Root != null ? phase1Root.transform : null;
    public Transform GetPhase2Root() => phase2Root != null ? phase2Root.transform : null;
    public GPUPhase1CableInteraction GetGPUPhase1CableInteraction() => gpuPhase1CableInteraction;

    public void SetPhase1Interactive()
    {
        CurrentPhase = Phase.Phase1;
        SetPhase1Enabled(true);
        SetPhase2Enabled(false);

        // Re-enable full GPU interaction in Phase 1 AFTER SetPhase2Enabled, which sweeps
        // and disables all phase2Root Collider2Ds (including the GPU root collider).
        if (gpuPhase1CableInteraction != null)
        {
            gpuPhase1CableInteraction.enabled = true;
            foreach (DragPrefab dp in gpuPhase1CableInteraction.GetComponents<DragPrefab>())
                dp.enabled = true;
            foreach (Collider2D col in gpuPhase1CableInteraction.GetComponents<Collider2D>())
                col.enabled = true;
        }
    }

    public void SetPhase2Interactive()
    {
        CurrentPhase = Phase.Phase2;
        // Close and fully disable GPU — all GPU work is done in Phase 1.
        if (gpuPhase1CableInteraction != null)
        {
            gpuPhase1CableInteraction.ClosePanel();
            gpuPhase1CableInteraction.enabled = false;
            foreach (DragPrefab dp in gpuPhase1CableInteraction.GetComponents<DragPrefab>())
                dp.enabled = false;
            foreach (Collider2D col in gpuPhase1CableInteraction.GetComponents<Collider2D>())
                col.enabled = false;
        }

        SetPhase1Enabled(false);
        SetPhase2Enabled(true);
    }

    private void SetPhase1Enabled(bool enabled)
    {
        if (phase1Root == null) return;

        // Toggle screws and cables � both script and collider
        foreach (var sc in phase1Root.GetComponentsInChildren<ScrewController>(true))
        {
            sc.enabled = enabled;
            foreach (Collider2D col in sc.GetComponents<Collider2D>())
                col.enabled = enabled;
        }

        foreach (var cs in phase1Root.GetComponentsInChildren<CableSlot>(true))
        {
            cs.enabled = enabled;
            foreach (Collider2D col in cs.GetComponents<Collider2D>())
                col.enabled = enabled;
        }

        // Toggle MBCable � blocks hold-to-detach and drag when Phase 2 is active
        // Skip cables that are already detached � disabling them mid-flight breaks drag
        foreach (var mc in phase1Root.GetComponentsInChildren<MBCable>(true))
        {
            if (!enabled && mc.IsDetached) continue; // don't disable a cable being dragged
            mc.enabled = enabled;
            foreach (Collider2D col in mc.GetComponents<Collider2D>())
                col.enabled = enabled;
        }
    }

    private void SetPhase2Enabled(bool enabled)
    {
        if (phase2Root == null) return;

        // GPU is fully managed by Phase 1 — skip it entirely here.
        GPUController gpuCtrl = gpuPhase1CableInteraction != null
            ? gpuPhase1CableInteraction.GetComponent<GPUController>()
            : null;

        foreach (var dp in phase2Root.GetComponentsInChildren<DragPrefab>(true))
        {
            if (gpuCtrl != null && dp.GetComponentInParent<GPUController>() == gpuCtrl) continue;
            dp.enabled = enabled;
        }

        foreach (var pi in phase2Root.GetComponentsInChildren<PrefabInteraction>(true))
        {
            if (gpuCtrl != null && pi.GetComponentInParent<GPUController>() == gpuCtrl) continue;
            pi.enabled = enabled;
        }

        foreach (var lk in phase2Root.GetComponentsInChildren<CPULockController>(true))
            lk.enabled = enabled;

        foreach (var col in phase2Root.GetComponentsInChildren<Collider2D>(true))
        {
            if (gpuCtrl != null && col.GetComponentInParent<GPUController>() == gpuCtrl) continue;
            col.enabled = enabled;
        }
    }
}