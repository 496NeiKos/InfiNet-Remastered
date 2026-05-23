using UnityEngine;

/// <summary>
/// On RAM1 / RAM2 root.
/// Tracks the latch state of the RAM stick.
/// DragPrefab checks IsInstalled before allowing drag out of a RAMSlot.
/// Default state is Installed (latches engaged when seated in slot).
/// </summary>
public class RAMController : MonoBehaviour
{
    public enum RAMState { Installed, Uninstalled }

    private RAMState _state = RAMState.Installed;

    public bool IsInstalled => _state == RAMState.Installed;

    public void SetInstalled()
    {
        if (_state == RAMState.Installed) return;
        _state = RAMState.Installed;
        Debug.Log($"[RAMController:{name}] State → Installed");
    }

    public void SetUninstalled()
    {
        if (_state == RAMState.Uninstalled) return;
        _state = RAMState.Uninstalled;
        Debug.Log($"[RAMController:{name}] State → Uninstalled");
    }
}
