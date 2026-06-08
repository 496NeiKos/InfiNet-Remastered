/*
 * ================================================================
 *  UNITY SETUP GUIDE — T3MonitorInteraction
 * ================================================================
 *  This script handles right-click interaction on the UEFI Monitor.
 *  Do NOT add PrefabInteraction or DragPrefab to this object.
 *
 *  STEP 1 — Components on the UEFI Monitor root GameObject
 *    SpriteRenderer, Collider2D, T3MonitorInteraction,
 *    T3MonitorController, UEFINavigator
 *
 *  STEP 2 — Wire the inspector
 *    T3MonitorInteraction:
 *      monitorController → T3MonitorController on this same GameObject
 *
 *  HOW IT WORKS
 *    Right-click UEFI Monitor → UEFICanvas enables (no reparenting).
 *    Escape → canvas closes. Uses the same GameManager.OpenEditorInPlace
 *    path as T2Monitor so the Escape key and IsEditorOpen flag work
 *    identically.
 * ================================================================
 */

using UnityEngine;
using UnityEngine.InputSystem;

public class T3MonitorInteraction : MonoBehaviour, IInPlaceInteraction
{
    [SerializeField] private T3MonitorController monitorController;

    private void Start()
    {
        if (monitorController == null)
            monitorController = GetComponent<T3MonitorController>();
    }

    private void Update()
    {
        if (!Mouse.current.rightButton.wasPressedThisFrame) return;
        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen) return;
        if (!IsMouseOver()) return;

        GameManager.Instance.OpenEditorInPlace(this);
    }

    public void ShowDetail() => monitorController?.ShowDetailAtCenter();

    public void HideDetail() => monitorController?.HideDetail();

    private bool IsMouseOver()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        return hit.collider != null && hit.collider.gameObject == gameObject;
    }
}
