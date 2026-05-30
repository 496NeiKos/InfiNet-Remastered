/*
 * ================================================================
 *  UNITY SETUP GUIDE — T2MonitorInteraction
 * ================================================================
 *  This script replaces PrefabInteraction on the T2 Monitor.
 *  Do NOT add PrefabInteraction to T2Monitor — use this instead.
 *
 *  STEP 1 — Components on T2Monitor root
 *    SpriteRenderer, Collider2D, T2MonitorInteraction,
 *    T2MonitorController, T2MonitorNavigator
 *    (no PrefabInteraction, no DragPrefab)
 *
 *  STEP 2 — Wire the inspector
 *    T2MonitorInteraction:
 *      monitorController → T2MonitorController on this same GameObject
 *
 *  HOW IT WORKS
 *    Right-click T2Monitor → Canvas enables in place (no reparenting).
 *    Escape → Canvas closes. GameManager.IsEditorOpen is set so other
 *    hardware objects correctly block their interactions while open.
 * ================================================================
 */

using UnityEngine;
using UnityEngine.InputSystem;

public class T2MonitorInteraction : MonoBehaviour
{
    [SerializeField] private T2MonitorController monitorController;

    private void Start()
    {
        if (monitorController == null)
            monitorController = GetComponent<T2MonitorController>();
    }

    private void Update()
    {
        if (!Mouse.current.rightButton.wasPressedThisFrame) return;
        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen) return;
        if (!IsMouseOver()) return;

        GameManager.Instance.OpenEditorInPlace(this);
    }

    public void ShowDetail()
    {
        monitorController?.ShowDetailAtCenter();
    }

    public void HideDetail()
    {
        monitorController?.HideDetail();
    }

    private bool IsMouseOver()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        return hit.collider != null && hit.collider.gameObject == gameObject;
    }
}
