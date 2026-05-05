using UnityEngine;
using UnityEngine.InputSystem; // ✅ New Input System

public class HoverRaycast : MonoBehaviour
{
    private void Update()
    {
        if (HoverLabelManager.Instance == null) return;

        // Read mouse position from Input System
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = 0f;

        // Raycast at mouse position
        RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);

        if (hit.collider != null)
        {
            HoverLabelManager.Instance.ShowLabel(hit.collider.gameObject.name);
            HoverLabelManager.Instance.FollowMouse(mouseScreen);
            Debug.Log("Hovering over " + hit.collider.gameObject.name);
        }
        else
        {
            HoverLabelManager.Instance.HideLabel();
        }
    }
}
