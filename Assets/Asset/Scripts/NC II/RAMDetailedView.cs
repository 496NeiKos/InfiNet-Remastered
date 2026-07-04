using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On the RAMDetailedView child of RAM1 / RAM2.
/// Detects vertical pointer-drag gestures on this object's 2D collider (100 px threshold,
/// no time constraint — same as CPULockController) to toggle the RAM latch state.
///
/// Slide Down (≥ 100 px) → Install  → InstalledSprite  → parent RAMController.SetInstalled()
/// Slide Up   (≥ 100 px) → Uninstall → UninstalledSprite → parent RAMController.SetUninstalled()
///
/// This child is only active while the InnerEditingPanel is open (MotherboardDetailViewManager
/// activates/deactivates it via SetDetailedView), so no extra panel-open guard is needed beyond
/// the standard IsEditorOpen check.
/// </summary>
public class RAMDetailedView : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite installedSprite;
    [SerializeField] private Sprite uninstalledSprite;

    private const float DragThreshold = 100f;

    private SpriteRenderer _sr;
    private RAMController _ramController;
    private bool _isPressed;
    private Vector2 _pressStartScreenPos;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _ramController = GetComponentInParent<RAMController>();
    }

    private void OnEnable()
    {
        // Sync sprite each time the panel opens (state may have changed since last session)
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
                    TryInstall();
                else
                    TryUninstall();
            }
        }

        // Safety cancel if button is released without triggering wasReleasedThisFrame
        if (_isPressed && !mouse.leftButton.isPressed)
            _isPressed = false;
    }

    private void TryInstall()
    {
        if (_ramController == null || _ramController.IsInstalled) return;
        _ramController.SetInstalled();
        ApplySprite();
        ActivityLogManager.Log($"{transform.parent.name} latch closed — RAM seated.", ActivityLogManager.EntryType.Install);
        Debug.Log($"[RAMDetailedView:{name}] Slide-down → Installed");
    }

    private void TryUninstall()
    {
        if (_ramController == null || !_ramController.IsInstalled) return;
        _ramController.SetUninstalled();
        ApplySprite();
        ActivityLogManager.Log($"{transform.parent.name} latch opened — RAM released.", ActivityLogManager.EntryType.Remove);
        Debug.Log($"[RAMDetailedView:{name}] Slide-up → Uninstalled");
    }

    private void ApplySprite()
    {
        if (_sr == null) return;
        bool installed = _ramController != null && _ramController.IsInstalled;
        _sr.sprite = installed ? installedSprite : uninstalledSprite;
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        foreach (Collider2D col in GetComponents<Collider2D>())
            if (col.OverlapPoint(mouseWorld)) return true;
        return false;
    }
}
