using UnityEngine;

/// <summary>
/// On the GPU root object.
/// Tracks latch state. IsFullyInstalled is computed on demand from
/// latch state + all CableSlots + all ScrewControllers in children.
/// GPU starts latched by default (student must follow removal procedure).
/// </summary>
public class GPUController : MonoBehaviour
{
    [Header("Slot Sprites")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite snappedSprite;

    private bool _isLatched = true;
    private SpriteRenderer _sr;

    public bool IsLatched => _isLatched;

    /// <summary>
    /// True when latched AND every CableSlot is installed AND every ScrewController is Screwed.
    /// Used by validation/task systems to confirm GPU is fully seated.
    /// </summary>
    public bool IsFullyInstalled
    {
        get
        {
            if (!_isLatched) return false;
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
        // Restore correct sprite when scene loads with GPU already in a slot
        ApplySlotSprite(GetComponentInParent<SlotContainer>() != null);
    }

    public void SetLatched()
    {
        if (_isLatched) return;
        _isLatched = true;
        Debug.Log($"[GPUController:{name}] Latched");
    }

    public void SetUnlatched()
    {
        if (!_isLatched) return;
        _isLatched = false;
        Debug.Log($"[GPUController:{name}] Unlatched");
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
