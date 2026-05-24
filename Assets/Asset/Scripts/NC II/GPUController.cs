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

    [Header("Cable State Sprites (in slot)")]
    [SerializeField] private Sprite cableInstalledSprite;
    [SerializeField] private Sprite cableRemovedSprite;

    private bool _isLatched = true;
    private bool _inSlot = false;
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
        _inSlot = GetComponentInParent<SlotContainer>() != null;
        RefreshSprite();
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

    public void OnSnappedToSlot()
    {
        _inSlot = true;
        RefreshSprite();
    }

    public void OnRemovedFromSlot()
    {
        _inSlot = false;
        RefreshSprite();
    }

    public void RefreshCableSprite() => RefreshSprite();

    private void RefreshSprite()
    {
        if (_sr == null) return;

        if (!_inSlot)
        {
            if (defaultSprite != null) _sr.sprite = defaultSprite;
            return;
        }

        bool cableConnected = IsCableConnected();
        Sprite cable = cableConnected ? cableInstalledSprite : cableRemovedSprite;
        _sr.sprite = cable != null ? cable : snappedSprite;
    }

    private bool IsCableConnected()
    {
        foreach (var cs in GetComponentsInChildren<CableSlot>(true))
            if (cs.IsInstalled()) return true;
        return false;
    }
}
