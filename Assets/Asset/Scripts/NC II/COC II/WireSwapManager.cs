using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton that manages click-to-swap wire selection.
/// First click selects a wire; second click on a different wire swaps them.
/// Second click on same wire deselects.
/// </summary>
public class WireSwapManager : MonoBehaviour
{
    public static WireSwapManager Instance { get; private set; }

    [Tooltip("Duration of the swap animation in seconds.")]
    [SerializeField] private float swapDuration = 0.3f;

    private WireController _selected;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        if (hit.collider == null) return;
        WireController wire = hit.collider.GetComponent<WireController>();
        if (wire == null || !wire.gameObject.activeInHierarchy) return;

        OnWireClicked(wire);
    }

    public void OnWireClicked(WireController wire)
    {
        if (_selected == null)
        {
            Select(wire);
            return;
        }

        if (_selected == wire)
        {
            Deselect();
            return;
        }

        Swap(_selected, wire);
        Deselect();
    }

    private void Select(WireController wire)
    {
        _selected = wire;
        wire.SetHighlight(true);
    }

    private void Deselect()
    {
        if (_selected != null) _selected.SetHighlight(false);
        _selected = null;
    }

    private void Swap(WireController a, WireController b)
    {
        Vector3 posA = a.transform.localPosition;
        Vector3 posB = b.transform.localPosition;
        int slotA = a.CurrentSlotIndex;
        int slotB = b.CurrentSlotIndex;

        a.CurrentSlotIndex = slotB;
        b.CurrentSlotIndex = slotA;

        a.AnimateTo(posB, swapDuration);
        b.AnimateTo(posA, swapDuration);

        NetworkCableTaskManager.CheckConditions();
    }
}
