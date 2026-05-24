using UnityEngine;

/// <summary>
/// On the HDD root object in the system unit side view.
/// Tracks installation state and swaps sprites when snapped to/removed from a slot.
/// IsFullyInstalled is computed from all CableSlots and ScrewControllers in children.
/// </summary>
public class HDDController : MonoBehaviour
{
    [Header("Slot Sprites")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite snappedSprite;

    private SpriteRenderer _sr;

    public bool IsFullyInstalled
    {
        get
        {
            foreach (var cs in GetComponentsInChildren<CableSlot>(true))
                if (!cs.IsInstalled()) return false;
            foreach (var sc in GetComponentsInChildren<ScrewController>(true))
                if (!sc.IsScrewed()) return false;
            return true;
        }
    }

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        ApplySlotSprite(GetComponentInParent<SlotContainer>() != null);
    }

    public void OnSnappedToSlot() => ApplySlotSprite(true);
    public void OnRemovedFromSlot() => ApplySlotSprite(false);

    private void ApplySlotSprite(bool inSlot)
    {
        if (_sr == null) return;
        Sprite s = inSlot ? snappedSprite : defaultSprite;
        if (s != null) _sr.sprite = s;
    }
}
