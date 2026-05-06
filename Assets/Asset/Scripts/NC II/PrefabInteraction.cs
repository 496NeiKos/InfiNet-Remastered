using UnityEngine;
using UnityEngine.InputSystem; // new Input System

public class PrefabInteraction : MonoBehaviour
{
    private SystemUnitController controller;

    [SerializeField] private GameObject editingPanel; // assign in scene, not prefab

    void Start()
    {
        controller = GetComponent<SystemUnitController>();

        if (editingPanel == null)
        {
            Debug.LogError("EditingPanel reference not set in Inspector!");
        }
    }

    void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (IsMouseOver())
            {
                Debug.Log($"{name} → Right click detected, opening editor panel");

                controller.ShowDetail();

                if (editingPanel != null)
                {
                    editingPanel.SetActive(true);
                }
            }
        }
    }

    private bool IsMouseOver()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        return hit.collider != null && hit.collider.gameObject == gameObject;
    }

    // Called by Close button
    public void CloseEditor()
    {
        controller.ShowSnapshot();
        if (editingPanel != null)
        {
            editingPanel.SetActive(false);
        }
    }
}
