using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to the RJ45 hardware prefab (the actual object that lives in the slot,
/// not the icon proxy in the hardware area).
///
/// While the RJ45 is installed in a slot, click-and-hold for holdDuration seconds
/// to detach it. The physical RJ45 then becomes draggable (Update-driven, same
/// pattern as CableBehavior / BackCable in COC I):
///   • Drop on hardware area  → stored; icon proxy reappears so the player can
///                               pick it up again via drag-from-holder.
///   • Drop near a valid slot → slides into that slot (calls InstallRJ45).
///   • Drop anywhere else     → snaps back to the original slot.
///
/// Implements the EventSystem drag interfaces as empty consumers so drag events
/// do not bubble up to parent NetworkDragPrefab components.
/// </summary>
public class RJ45HoldUninstall : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("Seconds the player must hold to detach the installed RJ45.")]
    [SerializeField] private float holdDuration = 1f;

    [Tooltip("World-unit radius used when searching for a nearby slot on drop.")]
    [SerializeField] private float snapRadius = 1.5f;

    private NetworkCableEndController _homeEnd;
    private NetworkHardwareHolder _hardwareHolder;

    private float _holdTimer;
    private bool _detached;
    private bool _isDragging;
    private GameObject _dragIndicator;

    private static RJ45HoldUninstall _holdTarget;

    // Consume EventSystem drag events so they don't bubble to parent NetworkDragPrefab.
    public void OnBeginDrag(PointerEventData eventData) { }
    public void OnDrag(PointerEventData eventData)      { }
    public void OnEndDrag(PointerEventData eventData)   { }

    private void Awake()
    {
        var dp = GetComponent<NetworkDragPrefab>();
        if (dp != null)
        {
            _hardwareHolder = dp.hardwareHolder;
            dp.enabled = false;
        }

        var oldConnector = GetComponent<RJ45Connector>();
        if (oldConnector != null) oldConnector.enabled = false;
    }

    /// <summary>Called by NetworkCableEndController after reparenting the RJ45 into the slot.</summary>
    public void OnInstalled(NetworkCableEndController cableEnd)
    {
        _homeEnd   = cableEnd;
        _holdTimer = 0f;
    }

    /// <summary>
    /// Instant-store path used by the wire-stripper reset (NetworkCableEndController.UninstallRJ45).
    /// The caller already cleared the slot state; we just send the RJ45 back to storage.
    /// </summary>
    public void StoreImmediately()
    {
        _holdTimer = 0f;
        _detached  = false;
        SendToHolder();
    }

    private void Update()
    {
        if (_detached)
        {
            DragUpdate();
            return;
        }

        if (_homeEnd == null) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame && IsMouseOver())
            _holdTarget = this;

        if (_holdTarget == this)
        {
            if (mouse.leftButton.isPressed)
            {
                _holdTimer += Time.deltaTime;
                if (_holdTimer >= holdDuration)
                {
                    _holdTarget = null;
                    Detach();
                }
            }
            else
            {
                _holdTarget = null;
                _holdTimer  = 0f;
            }
        }
        else
        {
            _holdTimer = 0f;
        }
    }

    private void Detach()
    {
        _holdTimer = 0f;
        _isDragging = false;

        // Cache the current parent (rj45SlotObject) so we can restore it if detach is blocked.
        Transform previousParent = transform.parent;

        // Reparent BEFORE calling DetachRJ45ForDrag — that method disables rj45SlotObject,
        // and the RJ45 is currently a child of it. Moving out first prevents it from being
        // hidden when the slot object is disabled.
        transform.SetParent(GameManager.Instance.ActiveWorldContainer, true);

        if (!_homeEnd.DetachRJ45ForDrag())
        {
            // Crimped or already uninstalled — abort and restore original parent.
            transform.SetParent(previousParent, true);
            return;
        }

        _detached = true;

        _dragIndicator = new GameObject("RJ45DragIndicator");
        SpriteRenderer sr = _dragIndicator.AddComponent<SpriteRenderer>();
        sr.sprite       = GetComponent<SpriteRenderer>()?.sprite;
        sr.sortingOrder = 999;
        _dragIndicator.transform.position   = transform.position;
        _dragIndicator.transform.localScale = transform.lossyScale;

        ActivityLogManager.Log("RJ45 unplugged", ActivityLogManager.EntryType.Remove);
    }

    private void DragUpdate()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (!_isDragging)
        {
            if (mouse.leftButton.isPressed)
                _isDragging = true;
            return;
        }

        if (mouse.leftButton.isPressed)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(mouse.position.ReadValue().x, mouse.position.ReadValue().y, 10f));
            worldPos.z = 0f;
            transform.position = worldPos;
            if (_dragIndicator != null)
                _dragIndicator.transform.position = worldPos;
        }
        else
        {
            OnDragReleased();
        }
    }

    private void OnDragReleased()
    {
        if (_dragIndicator != null) { Destroy(_dragIndicator); _dragIndicator = null; }
        _isDragging = false;
        _detached   = false;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        bool onHardwareArea = RectTransformUtility.RectangleContainsScreenPoint(
            GameManager.Instance.hardwareArea, screenPos, Camera.main);

        if (onHardwareArea)
        {
            SendToHolder();
            NetworkCableTaskManager.CheckConditions();
            return;
        }

        Vector3 dropPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
        dropPos.z = 0f;

        NetworkCableEndController target = FindSlotAtPosition(dropPos);
        if (target != null)
        {
            target.InstallRJ45(gameObject);
            ActivityLogManager.Log("RJ45 plugged in", ActivityLogManager.EntryType.Install);
            NetworkCableTaskManager.CheckConditions();
        }
        else
        {
            SnapBack();
        }
    }

    private NetworkCableEndController FindSlotAtPosition(Vector3 worldPos)
    {
        NetworkCableEndController[] allEnds =
            FindObjectsByType<NetworkCableEndController>(FindObjectsSortMode.None);

        NetworkCableEndController best     = null;
        float                     bestDist = float.MaxValue;

        foreach (var end in allEnds)
        {
            if (!end.gameObject.activeInHierarchy) continue;
            if (!end.IsStripped || end.IsRJ45Installed) continue;
            float dist = Vector3.Distance(end.SlotWorldPosition, worldPos);
            if (dist < snapRadius && dist < bestDist) { bestDist = dist; best = end; }
        }
        return best;
    }

    private void SnapBack()
    {
        if (_homeEnd != null && _homeEnd.IsStripped && !_homeEnd.IsRJ45Installed)
        {
            _homeEnd.InstallRJ45(gameObject);
            ActivityLogManager.Log("RJ45 plugged in", ActivityLogManager.EntryType.Install);
        }
        else
        {
            // Home slot is no longer valid (re-stripped or occupied) — store instead.
            SendToHolder();
        }
        NetworkCableTaskManager.CheckConditions();
    }

    private void SendToHolder()
    {
        if (_hardwareHolder == null)
        {
            foreach (var h in FindObjectsByType<NetworkHardwareHolder>(FindObjectsSortMode.None))
            {
                if (h.hardwarePrefab == gameObject) { _hardwareHolder = h; break; }
            }
        }

        if (_hardwareHolder != null)
            _hardwareHolder.StoreHardware();
        else
            gameObject.SetActive(false);
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            if (!col.enabled) continue;
            if (col is BoxCollider2D box && (box.size.x < 0.01f || box.size.y < 0.01f)) continue;
            if (col.OverlapPoint(mouseWorld)) return true;
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            return sr.bounds.Contains(new Vector3(mouseWorld.x, mouseWorld.y, sr.bounds.center.z));

        return false;
    }
}
