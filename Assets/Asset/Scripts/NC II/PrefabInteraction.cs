using UnityEngine;
using UnityEngine.InputSystem;

public class PrefabInteraction : MonoBehaviour
{
    private SystemUnitController controller;

    [SerializeField] private GameObject editingPanel;

    void Start()
    {
        controller = GetComponent<SystemUnitController>();
    }

    void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            // ✅ NEW: Check if prefab is locked (installed in parent)
            HardwareEditLock editLock = GetComponent<HardwareEditLock>();
            if (editLock != null && editLock.IsAnyLocked())
            {
                Debug.LogWarning($"{name} → Cannot edit: This hardware is installed in a parent. Uninstall first.");
                return;
            }

            // Block right-click while the editing panel is open
            if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen)
            {
                Debug.Log($"{name} → Right-click blocked: editor is open");
                return;
            }

            if (IsMouseOver())
            {
                Debug.Log($"{name} → Right-click detected, opening editor panel");

                if (GameManager.Instance != null)
                    GameManager.Instance.OpenEditor(this);
                else
                {
                    ShowDetailCentered();
                    if (editingPanel != null)
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

    public void ShowDetailCentered()
    {
        if (controller != null)
            controller.ShowDetailAtCenter();
    }

    public void OnEditorClosed()
    {
        if (controller != null)
            controller.HideDetail();

        if (editingPanel != null)
            editingPanel.SetActive(false);
    }

    public void CloseEditor()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.CloseEditor();
        else
            OnEditorClosed();
    }
}