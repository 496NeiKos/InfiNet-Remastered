using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages which layer of interaction is active in the editing panel.
/// Attached to the ROOT of the hardware prefab (e.g., SystemUnit).
///
/// Handles click detection on the cover for opening/closing.
/// Future: will manage side/back/front detailed views.
/// </summary>
public class DetailViewManager : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private CoverController coverController;

    private void Update()
    {
        // Only process clicks when editing panel is open
        if (GameManager.Instance == null || !GameManager.Instance.IsEditorOpen)
            return;

        // Left click to interact with cover
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            CheckCoverClick();
        }
    }

    private void CheckCoverClick()
    {
        if (coverController == null) return;

        // Raycast to see if user clicked on the cover
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        if (hit.collider != null && hit.collider.gameObject == coverController.gameObject)
        {
            coverController.OnCoverClicked();
        }
    }
}