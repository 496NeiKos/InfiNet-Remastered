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
            // If this prefab is installed in a slot (child of a parent in editing panel),
            // let the parent's DetailViewManager handle the right-click instead
            if (IsInstalledInSlot())
                return;

            // Check edit lock
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

    /// <summary>
    /// Check if this prefab is currently inside a SlotContainer.
    /// If yes, DetailViewManager handles right-click, not PrefabInteraction.
    /// </summary>
    private bool IsInstalledInSlot()
    {
        SlotContainer slot = GetComponentInParent<SlotContainer>();
        return slot != null;
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