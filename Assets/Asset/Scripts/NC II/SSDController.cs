using UnityEngine;

public class SSDController : MonoBehaviour
{
    private bool _inSlot = false;

    public bool IsInSlot => _inSlot;

    private void Awake()
    {
        _inSlot = GetComponentInParent<SlotContainer>() != null;
    }

    public void OnSnappedToSlot()
    {
        _inSlot = true;
        GetComponentInParent<MotherboardController>()?.RefreshCableSprite();
        Debug.Log($"[SSDController:{name}] Snapped to slot.");
    }

    public void OnRemovedFromSlot()
    {
        _inSlot = false;
        GetComponentInParent<MotherboardController>()?.RefreshCableSprite();
        Debug.Log($"[SSDController:{name}] Removed from slot.");
    }
}
