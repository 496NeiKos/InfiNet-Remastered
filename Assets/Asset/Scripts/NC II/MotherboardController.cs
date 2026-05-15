using UnityEngine;

public class MotherboardController : MonoBehaviour
{
    public bool IsUninstalledFromSystemUnit { get; private set; } = false;

    public void MarkUninstalled() => IsUninstalledFromSystemUnit = true;
    public void MarkInstalled() => IsUninstalledFromSystemUnit = false;

    public bool IsPhase1Complete()
    {
        if (!IsUninstalledFromSystemUnit) return false;

        foreach (var s in GetComponentsInChildren<ScrewController>(true))
            if (!s.IsUnscrewed()) return false;

        foreach (var c in GetComponentsInChildren<CableSlot>(true))
            if (c.IsInstalled()) return false;

        return true;
    }
}