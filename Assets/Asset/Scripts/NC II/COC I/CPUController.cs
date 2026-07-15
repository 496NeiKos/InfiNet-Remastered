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

    private PasteState _pasteState = PasteState.PasteApplied;
    private Vector3 _installedLocalScale;
    private Vector3 _installedLocalPosition;

    public PasteState CurrentPasteState => _pasteState;
    public Vector3 InstalledLocalScale => _installedLocalScale;
    public Vector3 InstalledLocalPosition => _installedLocalPosition;
    public bool IsInstalledInSlot => GetComponentInParent<CPUSlotController>()?.IsCPUInstalled ?? false;

    private void Awake()
    {
        if (cpuRootSprite == null)
            cpuRootSprite = GetComponent<SpriteRenderer>();
        _installedLocalScale = transform.localScale;
        _installedLocalPosition = transform.localPosition;
        ApplySprites();
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