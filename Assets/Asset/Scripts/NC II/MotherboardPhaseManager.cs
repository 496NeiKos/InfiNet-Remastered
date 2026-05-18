using UnityEngine;

public class MotherboardPhaseManager : MonoBehaviour
{
    [SerializeField] private GameObject phase1Root;
    [SerializeField] private GameObject phase2Root;

    public Transform GetPhase1Root() => phase1Root != null ? phase1Root.transform : null;
    public Transform GetPhase2Root() => phase2Root != null ? phase2Root.transform : null;

    public void SetPhase1Interactive()
    {
        SetInteractable(phase1Root, true);
        SetInteractable(phase2Root, false);
    }

    public void SetPhase2Interactive()
    {
        SetInteractable(phase1Root, false);
        SetInteractable(phase2Root, true);
    }

    private void SetInteractable(GameObject root, bool interactable)
    {
        if (root == null) return;

        foreach (var sc in root.GetComponentsInChildren<ScrewController>(true))
            sc.enabled = interactable;

        foreach (var cs in root.GetComponentsInChildren<CableSlot>(true))
            cs.enabled = interactable;

        foreach (var dp in root.GetComponentsInChildren<DragPrefab>(true))
            dp.enabled = interactable;
    }
}