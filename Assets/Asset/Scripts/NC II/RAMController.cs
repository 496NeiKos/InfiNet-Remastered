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

    [Header("Slot Sprites")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite snappedSprite;

    private RAMState _state = RAMState.Installed;
    private SpriteRenderer _sr;

    public bool IsInstalled => _state == RAMState.Installed;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // Restore correct sprite when scene loads with RAM already in a slot
        ApplySlotSprite(GetComponentInParent<SlotContainer>() != null);
    }

    public void SetInstalled()
    {
        if (_state == RAMState.Installed) return;
        _state = RAMState.Installed;
        GetComponentInParent<MotherboardController>()?.RefreshCableSprite();
        Debug.Log($"[RAMController:{name}] State → Installed");
    }

    public void SetUninstalled()
    {
        if (_state == RAMState.Uninstalled) return;
        _state = RAMState.Uninstalled;
        GetComponentInParent<MotherboardController>()?.RefreshCableSprite();
        Debug.Log($"[RAMController:{name}] State → Uninstalled");
    }

    public void OnSnappedToSlot()
    {
        SetInstalled();
        ApplySlotSprite(true);
    }

    public void OnRemovedFromSlot()
    {
        ApplySlotSprite(false);
        GetComponentInParent<MotherboardController>()?.RefreshCableSprite();
    }

    private void ApplySlotSprite(bool inSlot)
    {
        if (_sr == null) return;
        Sprite s = inSlot ? snappedSprite : defaultSprite;
        if (s != null) _sr.sprite = s;
    }
}
