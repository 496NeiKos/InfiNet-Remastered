using UnityEngine;

/// <summary>
/// On CPU object (child of CPUSlot, sibling of Heatsink).
/// Tracks thermal paste state and controls CPU sprites.
/// Heatsink/CPU interaction state is managed by CPUSlotController.
/// </summary>
public class CPUController : MonoBehaviour
{
    public enum PasteState { NoPaste, PasteApplied }

    [Header("CPU Root Sprites")]
    [SerializeField] private SpriteRenderer cpuRootSprite;
    [SerializeField] private Sprite cpuNoPasteSprite;
    [SerializeField] private Sprite cpuPasteAppliedSprite;

    [Header("CPUDetailed Sprites")]
    [SerializeField] private SpriteRenderer cpuDetailedSprite;
    [SerializeField] private Sprite cpuDetailedNoPasteSprite;
    [SerializeField] private Sprite cpuDetailedPasteAppliedSprite;

    [Header("Installed Slot Transform")]
    [Tooltip("When true, the Inspector fields below are used instead of the auto-captured values. " +
             "Enable this and fill in the fields if reinstall still positions the CPU incorrectly " +
             "after the Start()-capture fix.")]
    [SerializeField] private bool overrideInstalledTransform = false;
    [SerializeField] private Vector3 installedLocalPositionOverride;
    [SerializeField] private Vector3 installedLocalScaleOverride;

    private PasteState _pasteState = PasteState.PasteApplied;
    private Vector3 _installedLocalScale;
    private Vector3 _installedLocalPosition;
    private bool _transformCaptured;

    public PasteState CurrentPasteState => _pasteState;
    public Vector3 InstalledLocalScale    => overrideInstalledTransform ? installedLocalScaleOverride    : _installedLocalScale;
    public Vector3 InstalledLocalPosition => overrideInstalledTransform ? installedLocalPositionOverride : _installedLocalPosition;
    public bool IsInstalledInSlot => GetComponentInParent<CPUSlotController>()?.IsCPUInstalled ?? false;

    private void Awake()
    {
        if (cpuRootSprite == null)
            cpuRootSprite = GetComponent<SpriteRenderer>();
        ApplySprites();
    }

    private void OnEnable()
    {
        // OnEnable fires synchronously (unlike Start which is deferred to the next frame).
        // This matters because MotherboardDetailViewManager can call OpenInnerPanel in the
        // same frame that _detailedView.SetActive(true) activates the CPU — by the time
        // Start() would fire, the CPU has already been reparented to the inner panel and
        // its localPosition is panel-relative (wrong). OnEnable captures while the CPU is
        // still seated in CPUSlot. The _transformCaptured guard prevents subsequent
        // OnEnable calls (e.g. from OpenInnerPanel reparenting) from overwriting the value.
        if (_transformCaptured) return;
        if (overrideInstalledTransform) return;
        if (GetComponentInParent<CPUSlotController>(true) == null) return;

        _installedLocalScale    = transform.localScale;
        _installedLocalPosition = transform.localPosition;
        _transformCaptured      = true;
    }

    public void ApplyThermalPaste()
    {
        if (_pasteState == PasteState.PasteApplied)
        {
            Debug.Log("[CPUController] Thermal paste already applied.");
            return;
        }
        _pasteState = PasteState.PasteApplied;
        ApplySprites();
        ActivityLogManager.Log("Thermal paste applied to CPU", ActivityLogManager.EntryType.Install);
        Debug.Log("[CPUController] Thermal paste applied.");
        NCIITaskListManager.CheckConditions();
    }

    public void RemoveThermalPaste()
    {
        if (_pasteState == PasteState.NoPaste)
        {
            Debug.Log("[CPUController] No thermal paste to remove.");
            return;
        }
        _pasteState = PasteState.NoPaste;
        ApplySprites();
        ActivityLogManager.Log("Thermal paste removed from CPU", ActivityLogManager.EntryType.Remove);
        Debug.Log("[CPUController] Thermal paste removed.");
        NCIITaskListManager.CheckConditions();
    }

    private void ApplySprites()
    {
        bool hasPaste = _pasteState == PasteState.PasteApplied;

        if (cpuRootSprite != null)
            cpuRootSprite.sprite = hasPaste ? cpuPasteAppliedSprite : cpuNoPasteSprite;

        if (cpuDetailedSprite != null)
            cpuDetailedSprite.sprite = hasPaste
                ? cpuDetailedPasteAppliedSprite
                : cpuDetailedNoPasteSprite;
    }
}