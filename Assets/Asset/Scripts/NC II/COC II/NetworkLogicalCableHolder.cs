using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Icon proxy in the hardware area for the logical network cable.
///
/// Unlike NetworkHardwareHolder, this holder NEVER hides — it instantiates a new
/// cable prefab every time the player drags it onto a valid device, so unlimited
/// cables can be deployed.
///
/// Drop the icon near a device (Computer, Router, PatchPanel, Switch) that has a
/// NetworkDevicePort component. If the drop lands within snapRadius of a device's
/// anchor, a new cable is spawned and its first end is attached to that device.
/// The cable then enters PendingSecondEnd state until the player clicks a second device.
///
/// Only one cable can be pending a second end at a time — deploying is blocked while
/// NetworkLogicalCable.AnyPendingSecondEnd is true.
/// </summary>
public class NetworkLogicalCableHolder : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Cable")]
    [Tooltip("Prefab that has NetworkLogicalCable + LineRenderer on the root and two endpoint plug children.")]
    [SerializeField] private GameObject cablePrefab;

    [Tooltip("Optional sprite for the drag indicator. Falls back to the prefab's own SpriteRenderer sprite.")]
    [SerializeField] private Sprite dragIndicatorSprite;

    [Header("Snap")]
    [Tooltip("World-unit radius for finding the closest NetworkDevicePort on drop.")]
    [SerializeField] private float snapRadius = 2f;

    [Header("Info Panel")]
    [SerializeField] private Sprite infoImage;
    [SerializeField] private string infoName;
    [TextArea(3, 6)]
    [SerializeField] private string infoDescription;

    private GameObject _dragIndicator;
    private bool _isDragging;

    // ----------------------------------------------------------------
    //  Hover info (mirrors NetworkHardwareHolder pattern)
    // ----------------------------------------------------------------

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(infoName))
            StartCoroutine(ShowInfoAfterDelay());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllCoroutines();
        HardwareInfoPanel.Instance?.Hide();
    }

    private System.Collections.IEnumerator ShowInfoAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        HardwareInfoPanel.Instance?.Show(new[] { infoImage }, infoName, infoDescription);
    }

    // ----------------------------------------------------------------
    //  Drag
    // ----------------------------------------------------------------

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen) return;
        if (NetworkLogicalCable.AnyPendingSecondEnd)
        {
            Debug.Log("[NetworkLogicalCableHolder] Blocked — a cable is waiting for its second end.");
            return;
        }

        _isDragging = true;

        _dragIndicator = new GameObject("LogicalCableDragIndicator");
        SpriteRenderer sr = _dragIndicator.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 999;
        sr.sprite = dragIndicatorSprite != null
            ? dragIndicatorSprite
            : cablePrefab != null ? cablePrefab.GetComponent<SpriteRenderer>()?.sprite : null;

        Vector3 worldPos = ScreenToWorld(eventData.position);
        _dragIndicator.transform.position = worldPos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || _dragIndicator == null) return;
        _dragIndicator.transform.position = ScreenToWorld(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragIndicator != null) { Destroy(_dragIndicator); _dragIndicator = null; }
        if (!_isDragging) return;
        _isDragging = false;

        if (GameManager.Instance == null) return;
        if (GameManager.Instance.IsEditorOpen) return;

        bool onWorkspace = RectTransformUtility.RectangleContainsScreenPoint(
            GameManager.Instance.workspaceArea, eventData.position, eventData.pressEventCamera);
        if (!onWorkspace) return;

        Vector3 dropWorldPos = ScreenToWorld(eventData.position);

        NetworkDevicePort target = FindClosestPort(dropWorldPos);
        if (target == null)
        {
            Debug.Log("[NetworkLogicalCableHolder] No device within snap radius — cable not deployed.");
            return;
        }

        if (cablePrefab == null)
        {
            Debug.LogWarning("[NetworkLogicalCableHolder] cablePrefab is not assigned.");
            return;
        }

        GameObject cableGO = Object.Instantiate(cablePrefab, GameManager.Instance.ActiveWorldContainer);
        cableGO.transform.position = target.GetAnchorWorldPosition();

        NetworkLogicalCable cable = cableGO.GetComponent<NetworkLogicalCable>();
        if (cable == null)
        {
            Debug.LogWarning("[NetworkLogicalCableHolder] cablePrefab is missing a NetworkLogicalCable component.");
            Destroy(cableGO);
            return;
        }

        cable.AttachFirstEnd(target);

        // Icon stays active — holder is never hidden so more cables can be deployed.
    }

    // ----------------------------------------------------------------
    //  Helpers
    // ----------------------------------------------------------------

    private static Vector3 ScreenToWorld(Vector2 screenPos)
    {
        Vector3 w = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
        w.z = 0f;
        return w;
    }

    private NetworkDevicePort FindClosestPort(Vector3 worldPos)
    {
        NetworkDevicePort[] ports = FindObjectsByType<NetworkDevicePort>(FindObjectsSortMode.None);
        NetworkDevicePort closest = null;
        float bestDist = snapRadius;

        foreach (NetworkDevicePort port in ports)
        {
            if (!port.gameObject.activeInHierarchy) continue;
            if (!port.CanAcceptCable()) continue;

            float dist = Vector3.Distance(port.GetAnchorWorldPosition(), worldPos);
            if (dist < bestDist) { bestDist = dist; closest = port; }
        }

        return closest;
    }
}
