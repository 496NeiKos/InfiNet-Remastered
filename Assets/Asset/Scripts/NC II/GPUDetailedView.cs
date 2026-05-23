using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On the GPUDetailed child of the GPU root.
///
/// Double-click (two clicks within 0.5 s) on this object's own 2D collider toggles the PCIe latch.
///
/// Latch On  → Latch Out : BLOCKED unless all ScrewControllers are Empty AND all CableSlots are detached.
/// Latch Out → Latch On  : Always allowed (no prerequisites).
///
/// Only active while the InnerEditingPanel is open (MotherboardDetailViewManager activates/
/// deactivates this child via SetDetailedView), so no extra panel-open guard is needed.
/// </summary>
public class GPUDetailedView : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite latchedSprite;
    [SerializeField] private Sprite unlatchedSprite;

    [Header("Double-Click Window (seconds)")]
    [SerializeField] private float doubleClickWindow = 0.5f;

    private SpriteRenderer _sr;
    private GPUController _gpuController;

    private int _clickCount;
    private float _clickTimer;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _gpuController = GetComponentInParent<GPUController>();
    }

    private void OnEnable()
    {
        _clickCount = 0;
        _clickTimer = 0f;
        ApplySprite();
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsEditorOpen) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // Tick the double-click window; reset if it expires
        if (_clickCount > 0)
        {
            _clickTimer += Time.deltaTime;
            if (_clickTimer >= doubleClickWindow)
            {
                _clickCount = 0;
                _clickTimer = 0f;
            }
        }

        if (!mouse.leftButton.wasPressedThisFrame || !IsMouseOver()) return;

        _clickCount++;

        if (_clickCount == 1)
        {
            _clickTimer = 0f; // start the window on first click
        }
        else if (_clickCount >= 2)
        {
            _clickCount = 0;
            _clickTimer = 0f;
            TryToggleLatch();
        }
    }

    private void TryToggleLatch()
    {
        if (_gpuController == null) return;

        if (_gpuController.IsLatched)
        {
            // Unlatch requires all screws removed and cable detached first
            if (!AllScrewsEmpty())
            {
                Debug.Log("[GPUDetailedView] Cannot unlatch — screws still installed.");
                return;
            }
            if (!AllCablesDetached())
            {
                Debug.Log("[GPUDetailedView] Cannot unlatch — cable still connected.");
                return;
            }
            _gpuController.SetUnlatched();
        }
        else
        {
            _gpuController.SetLatched();
        }

        ApplySprite();
        Debug.Log($"[GPUDetailedView] Latch → {(_gpuController.IsLatched ? "On" : "Out")}");
    }

    private bool AllScrewsEmpty()
    {
        foreach (var sc in _gpuController.GetComponentsInChildren<ScrewController>(true))
            if (!sc.IsUnscrewed()) return false;
        return true;
    }

    private bool AllCablesDetached()
    {
        foreach (var cs in _gpuController.GetComponentsInChildren<CableSlot>(true))
            if (cs.IsInstalled()) return false;
        return true;
    }

    private void ApplySprite()
    {
        if (_sr == null) return;
        bool latched = _gpuController != null && _gpuController.IsLatched;
        _sr.sprite = latched ? latchedSprite : unlatchedSprite;
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D col = GetComponent<Collider2D>();
        return col != null && col.OverlapPoint(mouseWorld);
    }
}
