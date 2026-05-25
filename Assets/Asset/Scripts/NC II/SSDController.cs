using UnityEngine;

public class SSDController : MonoBehaviour
{
    private bool _inSlot = false;

    public bool IsInSlot => _inSlot;

    public void OnSnappedToSlot()
    {
        _inSlot = true;
        Debug.Log($"[SSDController:{name}] Snapped to slot.");
    }

    public void OnRemovedFromSlot()
    {
        _inSlot = false;
        Debug.Log($"[SSDController:{name}] Removed from slot.");
    }
}
