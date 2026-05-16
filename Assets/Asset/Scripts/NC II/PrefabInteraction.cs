using UnityEngine;
using UnityEngine.InputSystem;

public class PrefabInteraction : MonoBehaviour
{
    private SystemUnitController _controller;
    private MotherboardController _mbController;
    private MotherboardPhaseManager _phaseManager;
    private GameObject _detailedView;

    [SerializeField] private GameObject editingPanel;

    void Start()
    {
        _controller = GetComponent<SystemUnitController>();
        _mbController = GetComponent<MotherboardController>();
        _phaseManager = GetComponent<MotherboardPhaseManager>();

        foreach (Transform child in transform)
        {
            if (child.name.Contains("Detailed"))
            {
                _detailedView = child.gameObject;
                break;
            }
        }
    }

    void Update()
    {
        if (!Mouse.current.rightButton.wasPressedThisFrame) return;

        if (IsInstalledInSlot()) return;

        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen) return;

        if (!IsMouseOver()) return;

        if (_mbController != null)
        {
            if (!_mbController.IsPhase1Complete())
            {
                Debug.Log($"{name} -> Phase 1 incomplete: unscrew, detach cables, and drag motherboard out first.");
                return;
            }
            if (_phaseManager != null) _phaseManager.SetPhase2Interactive();
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
        {
            _controller.ShowDetailAtCenter();
            return;
        }

        if (_detailedView == null) return;

        if (GameManager.Instance != null && GameManager.Instance.editingPanel != null)
        {
            RectTransform rect = GameManager.Instance.editingPanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector3 panelCenter = rect.TransformPoint(
                    new Vector3(rect.rect.center.x, rect.rect.center.y, 0f));
                panelCenter.z = 0f;
                transform.position = panelCenter;
            }
        }

        _detailedView.SetActive(true);
    }

    public void OnEditorClosed()
    {
        if (_controller != null)
            _controller.HideDetail();
        else if (_detailedView != null)
            _detailedView.SetActive(false);

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