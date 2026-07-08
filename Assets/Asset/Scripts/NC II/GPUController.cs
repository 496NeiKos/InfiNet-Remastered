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
    private bool _inSlot = false;
    private SpriteRenderer _sr;
    private GameObject _cableIndicator;

    public bool IsLatched => _isLatched;
    public bool IsInSlot => _inSlot;

    /// <summary>
    /// True when latched AND every CableSlot is installed AND every ScrewController is Screwed.
    /// Used by validation/task systems to confirm GPU is fully seated.
    /// </summary>
    public bool IsFullyInstalled
    {
        get
        {
            if (!_isLatched) return false;
            foreach (var cs in GetComponentsInChildren<CablePort>(true))
                if (!cs.IsInstalled) return false;
            foreach (var sc in GetComponentsInChildren<ScrewController>(true))
                if (!sc.IsScrewed()) return false;
            return true;
        }
    }

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _inSlot = GetComponentInParent<SlotContainer>() != null;

        foreach (Transform child in transform)
        {
            if (child.name == "CableIndicator")
            {
                _cableIndicator = child.gameObject;
                break;
            }
        }
    }

    private void Start()
    {
        RefreshSprite();
    }

    public void SetLatched()
    {
        if (_isLatched) return;
        _isLatched = true;
        ActivityLogManager.Log("GPU latch engaged", ActivityLogManager.EntryType.Install);
        Debug.Log($"[GPUController:{name}] Latched");
    }

    public void SetUnlatched()
    {
        if (!_isLatched) return;
        _isLatched = false;
        ActivityLogManager.Log("GPU latch released", ActivityLogManager.EntryType.Action);
        Debug.Log($"[GPUController:{name}] Unlatched");
        NCIITaskListManager.CheckConditions();
    }

    public void OnSnappedToSlot()
    {
        _inSlot = true;
        RefreshSprite();
        GetComponentInParent<MotherboardController>()?.RefreshCableSprite();
    }

    public void OnRemovedFromSlot()
    {
        _inSlot = false;
        RefreshSprite();
        GetComponentInParent<MotherboardController>()?.RefreshCableSprite();
    }

    public void RefreshCableSprite() => RefreshSprite();

    // Called by GPUDetailedView when switching sub-views.
    // Side view hides the indicator; top view restores normal cable logic.
    public void SetCableIndicatorForView(bool sideViewActive)
    {
        if (_cableIndicator == null) return;
        _cableIndicator.SetActive(!sideViewActive && _inSlot && IsCableConnected());
    }

    private void RefreshSprite()
    {
        if (_sr != null)
            _sr.sprite = _inSlot ? snappedSprite : defaultSprite;

        if (_cableIndicator != null)
            _cableIndicator.SetActive(_inSlot && IsCableConnected());
    }

    private bool IsCableConnected()
    {
        foreach (var cs in GetComponentsInChildren<CablePort>(true))
            if (cs.IsInstalled) return true;
        return false;
    }
}
