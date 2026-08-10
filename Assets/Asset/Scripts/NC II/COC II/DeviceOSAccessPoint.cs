/*
 * ================================================================
 *  UNITY SETUP GUIDE — DeviceOSAccessPoint
 * ================================================================
 *  PLACEMENT
 *    Add to the same GameObject as NetworkPrefabInteraction
 *    (the root Computer / Laptop device in the workspace).
 *    No extra collider needed — reuses the existing BoxCollider2D.
 *
 *  INSPECTOR ASSIGNMENTS
 *    deviceID    → Which device this belongs to (Computer1/2/Laptop)
 *    detailView  → The front view child to hide while Virtual OS
 *                  is open (e.g. ComputerFront, LaptopFront)
 * ================================================================
 */

using UnityEngine;
using UnityEngine.InputSystem;

public class DeviceOSAccessPoint : MonoBehaviour
{
    [SerializeField] private DeviceID  deviceID;
    [Tooltip("Front detail view child to hide while Virtual OS is open.")]
    [SerializeField] private GameObject detailView;

    private void Update()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.rightButton.wasPressedThisFrame) return;

        // Only active when this device is the open editor (parented to firstLayer)
        if (GameManager.Instance == null || !GameManager.Instance.IsEditorOpen) return;
        if (GameManager.Instance.firstLayer == null) return;
        if (transform.parent != GameManager.Instance.firstLayer.transform) return;

        if (!IsMouseOver()) return;

        VirtualOSManager.Instance?.OpenForDevice(deviceID, detailView);
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
