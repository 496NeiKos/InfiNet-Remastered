using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Represents one cable connection slot on the motherboard.
/// The SLOT holds the visual state (installed/uninstalled sprites).
///
/// States:
///   Installed   → shows installed sprite, hold click 2s to detach
///   Uninstalled → shows uninstalled sprite, accepts matching cable drop
/// </summary>
public class CableSlot : MonoBehaviour
{
    public enum SlotState { Installed, Uninstalled }

    [Header("Sprites")]
    [SerializeField] private Sprite installedSprite;
    [SerializeField] private Sprite uninstalledSprite;

    [Header("Settings")]
    [SerializeField] private float detachDuration = 2f;

    [Header("Identity")]
    [Tooltip("Cable type this slot accepts (e.g., 'Cable1', 'SATAPower')")]
    [SerializeField] private string cableType = "Cable1";

    [Header("Cable Prefab")]
    [Tooltip("The draggable cable prefab spawned when detaching")]
    [SerializeField] private GameObject cablePrefab;

    private SpriteRenderer _spriteRenderer;
    private SlotState _state = SlotState.Installed;
    private float _holdProgress = 0f;
    private bool _isHolding = false;

    public System.Action<CableSlot> OnStateChanged;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSprite();
    }

    private void Update()
    {
        if (_state != SlotState.Installed) return;

        if (Mouse.current.leftButton.isPressed && IsMouseOver())
        {
            if (!_isHolding)
            {
                _isHolding = true;
                _holdProgress = 0f;
            }

            _holdProgress += Time.deltaTime;

            if (_holdProgress >= detachDuration)
            {
                DetachCable();
            }
        }
        else
        {
            _isHolding = false;
            _holdProgress = 0f;
        }
    }

    private void DetachCable()
    {
        _isHolding = false;
        _holdProgress = 0f;

        SetState(SlotState.Uninstalled);

        if (cablePrefab != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(mousePos.x, mousePos.y, 10f)
            );
            worldPos.z = 0f;

            GameObject cable = Instantiate(cablePrefab, worldPos, Quaternion.identity);

            CableController controller = cable.GetComponent<CableController>();
            if (controller != null)
            {
                // Pass this slot so the cable can snap back on invalid drop
                controller.StartDragImmediately(this);
            }
        }

        Debug.Log($"[CableSlot] Cable detached from slot '{cableType}'");
    }

    public void InstallCable()
    {
        SetState(SlotState.Installed);
        Debug.Log($"[CableSlot] Cable installed in slot '{cableType}'");
    }

    public bool CanAcceptCable(string type)
    {
        return cableType == type && _state == SlotState.Uninstalled;
    }

    private bool IsMouseOver()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        return hit.collider != null && hit.collider.gameObject == gameObject;
    }

    private void SetState(SlotState newState)
    {
        _state = newState;
        _holdProgress = 0f;
        _isHolding = false;
        UpdateSprite();
        OnStateChanged?.Invoke(this);
    }

    private void UpdateSprite()
    {
        if (_spriteRenderer == null) return;

        switch (_state)
        {
            case SlotState.Installed:
                if (installedSprite != null) _spriteRenderer.sprite = installedSprite;
                break;
            case SlotState.Uninstalled:
                if (uninstalledSprite != null) _spriteRenderer.sprite = uninstalledSprite;
                break;
        }
    }

    public SlotState GetState() => _state;
    public string GetCableType() => cableType;
    public bool IsInstalled() => _state == SlotState.Installed;
    public bool IsUninstalled() => _state == SlotState.Uninstalled;
}