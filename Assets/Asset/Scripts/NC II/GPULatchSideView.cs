using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On the GPUSideView child of GPUDetailed.
/// Detects vertical pointer-drag gestures (100 px threshold, same as CPULockController/RAMDetailedView)
/// to toggle the PCIe latch.
///
/// Slide Down (>= 100 px) -> Latch On  (always allowed)
/// Slide Up   (>= 100 px) -> Latch Off (blocked unless all screws removed and all cables detached)
///
/// After any latch change, notifies GPUDetailedView.ApplyHardwareInteractable() to gate screws.
/// </summary>
public class GPULatchSideView : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite latchedSprite;
    [SerializeField] private Sprite unlatchedSprite;

    private const float DragThreshold = 100f;

    private SpriteRenderer _sr;
    private GPUController _gpuController;
    private GPUDetailedView _gpuDetailedView;
    private bool _isPressed;
    private Vector2 _pressStartScreenPos;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _gpuController = GetComponentInParent<GPUController>();
        _gpuDetailedView = GetComponentInParent<GPUDetailedView>();
    }

    private void OnEnable()
    {
        ApplySprite();
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsEditorOpen) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame && IsMouseOver())
        {
            _isPressed = true;
            _pressStartScreenPos = mouse.position.ReadValue();
        }

        if (_isPressed && mouse.leftButton.wasReleasedThisFrame)
        {
            _isPressed = false;
            Vector2 delta = mouse.position.ReadValue() - _pressStartScreenPos;

            if (Mathf.Abs(delta.y) >= DragThreshold)
            {
                if (delta.y < 0f)
                    TryLatch();
                else
                    TryUnlatch();
            }
        }

        if (_isPressed && !mouse.leftButton.isPressed)
            _isPressed = false;
    }

    private void TryLatch()
    {
        if (_gpuController == null || _gpuController.IsLatched) return;
        _gpuController.SetLatched();
        ApplySprite();
        _gpuDetailedView?.ApplyHardwareInteractable();
        Debug.Log($"[GPULatchSideView:{name}] Slide-down -> Latched");
    }

    private void TryUnlatch()
    {
        if (_gpuController == null || !_gpuController.IsLatched) return;

        if (!AllScrewsEmpty())
        {
            Debug.Log("[GPULatchSideView] Cannot unlatch -- screws still installed.");
            return;
        }
        if (!AllCablesDetached())
        {
            Debug.Log("[GPULatchSideView] Cannot unlatch -- cable still connected.");
            return;
        }

        _gpuController.SetUnlatched();
        ApplySprite();
        _gpuDetailedView?.ApplyHardwareInteractable();
        Debug.Log($"[GPULatchSideView:{name}] Slide-up -> Unlatched");
    }

    private bool AllScrewsEmpty()
    {
        if (_gpuController == null) return true;
        foreach (var sc in _gpuController.GetComponentsInChildren<ScrewController>(true))
            if (!sc.IsUnscrewed()) return false;
        return true;
    }

    private bool AllCablesDetached()
    {
        if (_gpuController == null) return true;
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
        foreach (Collider2D col in GetComponents<Collider2D>())
            if (col.OverlapPoint(mouseWorld)) return true;
        return false;
    }
}
