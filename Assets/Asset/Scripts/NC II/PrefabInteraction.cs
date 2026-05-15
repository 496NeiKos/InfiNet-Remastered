using UnityEngine;
using UnityEngine.InputSystem;

public class PrefabInteraction : MonoBehaviour
{
    private SystemUnitController _controller;
    private MotherboardController _mbController;

    [SerializeField] private GameObject editingPanel;

    void Start()
    {
        _controller = GetComponent<SystemUnitController>();
        _mbController = GetComponent<MotherboardController>();
    }

    void Update()
    {
        if (!Mouse.current.rightButton.wasPressedThisFrame) return;

        if (IsInstalledInSlot()) return;

        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen) return;

        if (!IsMouseOver()) return;

        if (_mbController != null && !_mbController.IsPhase1Complete())
        {
            Debug.Log($"{name} -> Phase 1 incomplete: unscrew, detach cables, and drag motherboard out first.");
            return;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OpenEditor(this);
        else
        {
            ShowDetailCentered();
            if (editingPanel != null) editingPanel.SetActive(true);
        }
    }

    private bool IsInstalledInSlot()
    {
        return GetComponentInParent<SlotContainer>() != null;
    }

    private bool IsMouseOver()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        return hit.collider != null && hit.collider.gameObject == gameObject;
    }

    public void ShowDetailCentered()
    {
        if (_controller != null)
            _controller.ShowDetailAtCenter();
    }

    public void OnEditorClosed()
    {
        if (_controller != null)
            _controller.HideDetail();

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