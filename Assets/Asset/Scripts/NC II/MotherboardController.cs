using UnityEngine;

public class MotherboardController : MonoBehaviour
{
    public bool IsUninstalledFromSystemUnit { get; private set; } = false;
    private bool _wasEverInSystemUnit = false;

    public void MarkInstalledInSystemUnit() => _wasEverInSystemUnit = true;
    public void MarkUninstalled() => IsUninstalledFromSystemUnit = true;
    public void MarkInstalled()
    {
        IsUninstalledFromSystemUnit = false;
        _wasEverInSystemUnit = false;
    }

    public bool IsPhase1Complete()
    {
        if (!_wasEverInSystemUnit) return true;
        if (!IsUninstalledFromSystemUnit) return false;

        // Scope checks to Phase1 root only — avoids finding heatsink screws in Phase2
        MotherboardPhaseManager phase = GetComponent<MotherboardPhaseManager>();
        Transform phase1Root = phase != null ? phase.GetPhase1Root() : transform;
        if (phase1Root == null) phase1Root = transform;

        foreach (var s in phase1Root.GetComponentsInChildren<ScrewController>(true))
            if (!s.IsUnscrewed()) return false;

        foreach (var c in phase1Root.GetComponentsInChildren<CableSlot>(true))
            if (c.IsInstalled()) return false;

        return true;
    }
}