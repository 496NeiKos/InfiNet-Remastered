using UnityEngine;

/// <summary>
/// On RAM1 / RAM2 root.
/// Owns the state of both retention clip latches (left and right).
/// IsInstalled is true when either latch is still closed — both must be open before
/// DragPrefab will allow the stick to be dragged out of the slot.
/// </summary>
public class RAMController : MonoBehaviour
{
    [Header("Slot Sprites")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite snappedSprite;

    private SpriteRenderer _sr;

    public bool IsLeftLatched  { get; private set; } = true;
    public bool IsRightLatched { get; private set; } = true;

    // True while at least one clip is closed (RAM cannot be removed).
    public bool IsInstalled => IsLeftLatched || IsRightLatched;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        ApplySlotSprite(GetComponentInParent<SlotContainer>() != null);
    }

    /// <summary>Called by RAMLatchController after a successful gesture.</summary>
    public void SetLatchState(RAMLatchController.LatchSide side, bool latched)
    {
        bool wasInstalled = IsInstalled;

        if (side == RAMLatchController.LatchSide.Left)  IsLeftLatched  = latched;
        else                                             IsRightLatched = latched;

        if (IsInstalled != wasInstalled)
            GetComponentInParent<MotherboardController>()?.RefreshCableSprite();

        GetComponentInChildren<RAMDetailedView>(true)?.SyncSprite();
        Debug.Log($"[RAMController:{name}] {side} → {(latched ? "Closed" : "Opened")} | IsInstalled={IsInstalled}");
        NCIITaskListManager.CheckConditions();
    }

    public void OnSnappedToSlot()
    {
        // Seat the stick with both clips open — player must close them manually.
        IsLeftLatched  = false;
        IsRightLatched = false;
        ApplySlotSprite(true);
        SetIndicatorActive(true);
        GetComponentInParent<MotherboardController>()?.RefreshCableSprite();
    }

    public void OnRemovedFromSlot()
    {
        ApplySlotSprite(false);
        SetIndicatorActive(false);
        GetComponentInParent<MotherboardController>()?.RefreshCableSprite();
    }

    private void SetIndicatorActive(bool active)
    {
        foreach (Transform child in transform)
            if (child.name.Contains("Indicator"))
                child.gameObject.SetActive(active);
    }

    private void ApplySlotSprite(bool inSlot)
    {
        if (_sr == null) return;
        Sprite s = inSlot ? snappedSprite : defaultSprite;
        if (s != null) _sr.sprite = s;
    }
}
