using UnityEngine;
using UnityEngine.InputSystem; // new Input System

public class PrefabInteraction : MonoBehaviour
{
    private SystemUnitController controller;
    private GameObject editingPanel;

    void Awake()
    {
        controller = GetComponent<SystemUnitController>();
    }

    void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (IsMouseOver())
            {
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

    // 🔑 Called by DragFromUI after instantiation
    public void SetEditingPanel(GameObject panel)
    {
        editingPanel = panel;
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
