using UnityEngine;

/// <summary>
/// On CPU root object inside CPUSlot.
/// Tracks thermal paste state and heatsink installation.
/// Controls CPU root sprite and CPUDetailed sprite changes.
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

    private PasteState _pasteState = PasteState.PasteApplied; // default: paste already on
    private bool _isHeatsinkInstalled = false;

    public PasteState CurrentPasteState => _pasteState;
    public bool IsHeatsinkInstalled => _isHeatsinkInstalled;

    private void Awake()
    {
        if (cpuRootSprite == null)
            cpuRootSprite = GetComponent<SpriteRenderer>();

        ApplySprites();
    }

    public void SetHeatsinkInstalled(bool installed)
    {
        _isHeatsinkInstalled = installed;
        Debug.Log($"[CPUController] Heatsink installed: {installed}");
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
        Debug.Log("[CPUController] Thermal paste removed.");
    }

    private void ApplySprites()
    {
        bool hasPaste = _pasteState == PasteState.PasteApplied;

        if (cpuRootSprite != null)
            cpuRootSprite.sprite = hasPaste ? cpuPasteAppliedSprite : cpuNoPasteSprite;

        if (cpuDetailedSprite != null)
            cpuDetailedSprite.sprite = hasPaste ? cpuDetailedPasteAppliedSprite : cpuDetailedNoPasteSprite;
    }
}