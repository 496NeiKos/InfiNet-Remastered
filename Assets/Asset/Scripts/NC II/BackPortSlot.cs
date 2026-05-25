using UnityEngine;

/// <summary>
/// On each back port object (PSUBackPort, VGAPort, etc.).
/// Tracks installed/uninstalled state. Used by SystemUnitConditionChecker.
/// The cable child GameObject being active/inactive is the visual indicator — no sprites needed here.
/// </summary>
public class BackPortSlot : MonoBehaviour
{
    public enum PortState { Installed, Uninstalled }

    private PortState _state = PortState.Installed;

    public bool IsInstalled => _state == PortState.Installed;
    public bool IsUninstalled => _state == PortState.Uninstalled;

    public void SetInstalled()
    {
        _state = PortState.Installed;
        Debug.Log($"[BackPortSlot] {gameObject.name} → Installed");
    }

    public void SetUninstalled()
    {
        _state = PortState.Uninstalled;
        Debug.Log($"[BackPortSlot] {gameObject.name} → Uninstalled");
    }

    public bool CanAcceptCable(string cableType)
    {
        if (string.IsNullOrEmpty(cableType)) return false;
        return gameObject.name == cableType || gameObject.name.Contains(cableType);
    }
}
