/*
 * ================================================================
 *  UNITY SETUP GUIDE — ServerDeviceOSAccessPoint (COC III)
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add to the "Computer" server device GameObject.
 *    No extra collider needed — reuses the existing BoxCollider2D.
 *
 *  INSPECTOR ASSIGNMENTS
 *    None — no detail view is used in COC III.
 *
 *  HOW IT WORKS
 *    Right-clicking the deployed server device while it is in the
 *    workspace opens the Virtual OS canvas directly (no detail view).
 *    Works independently — does NOT require IsEditorOpen or firstLayer.
 * ================================================================
 */

using UnityEngine;
using UnityEngine.InputSystem;

public class ServerDeviceOSAccessPoint : MonoBehaviour
{
    private void Update()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.rightButton.wasPressedThisFrame) return;
        if (!IsInWorkspace()) return;
        if (!IsMouseOver()) return;

        ServerVirtualOSManager.Instance?.OpenForDevice(DeviceID.Server, null);
    }

    private bool IsInWorkspace()
    {
        if (GameManager.Instance == null) return false;
        Transform worldRoot = GameManager.Instance.worldRoot;
        Transform active    = GameManager.Instance.ActiveWorldContainer;
        return (worldRoot != null && transform.parent == worldRoot) ||
               (active   != null && transform.parent == active);
    }

    private bool IsMouseOver()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction);
        foreach (RaycastHit2D hit in hits)
            if (hit.collider.gameObject == gameObject) return true;
        return false;
    }
}
