using UnityEngine;

/// <summary>
/// On the HDD root object in the system unit side view.
/// Tracks installation state and swaps sprites when snapped to/removed from a slot.
/// IsFullyInstalled is computed from all CableSlots and ScrewControllers in children.
/// CanBeRemoved gates drag-out: HDD's own cable and the mobo HDD cable must be uninstalled.
/// </summary>
public class HDDController : MonoBehaviour
{
    [Header("Slot Sprites")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite snappedSprite;

    [Header("Removal Gates")]
    [Tooltip("Motherboard Phase 1 cable slot (CableMOBO-HDD) — must be uninstalled before drag-out.")]
    [SerializeField] private CableSlot hddMoboCableSlot;

    public bool CanBeRemoved
    {
        get
        {
            if (hddMoboCableSlot != null && hddMoboCableSlot.IsInstalled()) return false;
            return true;
        }
    }

    [Header("Cable Indicators")]
    [Tooltip("Each slot paired by index with its indicator below.")]
    [SerializeField] private CableSlot[] indicatorSlots;
    [SerializeField] private GameObject[] cableIndicators;

    private SpriteRenderer _sr;
    private bool _inSlot;
    private bool _detailViewActive;

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
        _inSlot = GetComponentInParent<SlotContainer>() != null;
        RefreshSprite();
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

    public void SetCableIndicatorForView(bool detailViewActive)
    {
        _detailViewActive = detailViewActive;
        RefreshIndicators(!_detailViewActive && _inSlot);
    }

    private void RefreshSprite()
    {
        if (_sr != null)
        {
            _sr.sprite = _inSlot ? snappedSprite : defaultSprite;
            SyncCollider();
        }

        RefreshIndicators(!_detailViewActive && _inSlot);
    }

    private void SyncCollider()
    {
        if (_sr == null || _sr.sprite == null) return;

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size   = _sr.sprite.bounds.size;
            box.offset = _sr.sprite.bounds.center;
            return;
        }

        PolygonCollider2D poly = GetComponent<PolygonCollider2D>();
        if (poly != null)
        {
            int count = _sr.sprite.GetPhysicsShapeCount();
            poly.pathCount = count;
            var path = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i < count; i++)
            {
                path.Clear();
                _sr.sprite.GetPhysicsShape(i, path);
                poly.SetPath(i, path);
            }
        }
    }

    private void RefreshIndicators(bool canShow)
    {
        for (int i = 0; i < cableIndicators.Length && i < indicatorSlots.Length; i++)
        {
            if (cableIndicators[i] != null && indicatorSlots[i] != null)
                cableIndicators[i].SetActive(canShow && indicatorSlots[i].IsInstalled());
        }
    }
}
