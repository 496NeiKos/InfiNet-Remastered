using UnityEngine;
using UnityEngine.InputSystem;

public class PrefabInteraction : MonoBehaviour
{
    private SystemUnitController controller;

    void Start()
    {
        controller = GetComponent<SystemUnitController>();

        // SystemUnitController is optional — some prefabs (Motherboard, HDD, etc.)
        // may not have one, and that is fine.
        if (controller == null)
            Debug.Log($"{name} → No SystemUnitController found (this is okay for component prefabs).");

        // Validate that GameManager exists so we fail fast with a clear message.
        if (GameManager.Instance == null)
            Debug.LogError($"{name} → GameManager.Instance is null! Make sure a GameManager exists in the scene.");
    }

    void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (IsMouseOver())
            {
                Debug.Log($"{name} → Right-click detected, opening editor panel");

                // Show the detailed view on the prefab itself (if it has one).
                controller?.ShowDetail();

                // Tell GameManager to open the panel and track this as the active prefab.
                GameManager.Instance?.OpenEditor(this);
            }
        }
    }

    /// <summary>
    /// Called by GameManager.CloseEditor() when the user presses Close.
    /// Reverts the prefab back to its snapshot view.
    /// </summary>
    public void OnEditorClosed()
    {
        controller?.ShowSnapshot();
    }

    private bool IsMouseOver()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        return hit.collider != null && hit.collider.gameObject == gameObject;
    }
}