using UnityEngine;
using UnityEngine.InputSystem;

public class DetailViewManager : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private CoverController coverController;

    [Header("Inner Editing Panel (auto-found if empty)")]
    [SerializeField] private GameObject innerEditingPanel;

    private GameObject _activeChildPrefab;
    private Transform _childOriginalParent;
    private Vector3 _childOriginalLocalPos;
    private bool _isInnerPanelOpen = false;

    public bool IsInnerPanelOpen => _isInnerPanelOpen;

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsEditorOpen)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!_isInnerPanelOpen)
                CheckCoverClick();
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (_isInnerPanelOpen)
                return;

            if (coverController != null && coverController.IsSliding)
                return;

            if (coverController != null && coverController.IsOpen())
            {
                // Gate: check condition before allowing inner panel open
                SystemUnitConditionChecker checker = GetComponent<SystemUnitConditionChecker>();
                if (checker != null && !checker.IsHardwareInteractable())
                {
                    Debug.Log("[DetailViewManager] Hardware locked — conditions not met.");
                    return;
                }

                CheckChildRightClick();
            }
        }
    }

    private GameObject GetInnerEditingPanel()
    {
        if (innerEditingPanel != null)
            return innerEditingPanel;

        if (GameManager.Instance == null || GameManager.Instance.editingPanel == null)
            return null;

        GameObject editingPanelObj = GameManager.Instance.editingPanel;
        Transform[] allChildren = editingPanelObj.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            if (child.name == "InnerEditingPanel")
            {
                innerEditingPanel = child.gameObject;
                return innerEditingPanel;
            }
        }

        return null;
    }

    private void CheckCoverClick()
    {
        if (coverController == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        if (hit.collider != null && hit.collider.gameObject == coverController.gameObject)
        {
            coverController.OnCoverClicked();
        }
    }

    private void CheckChildRightClick()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        if (hit.collider == null) return;

        GameObject clicked = hit.collider.gameObject;
        SlotContainer parentSlot = clicked.GetComponentInParent<SlotContainer>();

        if (parentSlot == null) return;

        HardwareSlotManager slotManager = GetComponent<HardwareSlotManager>();
        if (slotManager == null) return;

        SlotContainer matchedSlot = slotManager.GetSlotByType(parentSlot.GetSlotType());
        if (matchedSlot == null || matchedSlot != parentSlot) return;

        OpenInnerPanel(clicked);
    }

    private void OpenInnerPanel(GameObject childPrefab)
    {
        GameObject panel = GetInnerEditingPanel();
        if (panel == null)
        {
            Debug.LogError("[DetailViewManager] Cannot open inner panel: InnerEditingPanel not found!");
            return;
        }

        _activeChildPrefab = childPrefab;
        _isInnerPanelOpen = true;

        _childOriginalParent = childPrefab.transform.parent;
        _childOriginalLocalPos = childPrefab.transform.localPosition;

        gameObject.SetActive(false);

        panel.SetActive(true);

        childPrefab.transform.SetParent(panel.transform, true);

        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector3 panelCenter = rect.TransformPoint(
                new Vector3(rect.rect.center.x, rect.rect.center.y, 0f)
            );
            panelCenter.z = 0f;
            childPrefab.transform.position = panelCenter;
        }

        ActivateChildDetailedView(childPrefab, true);

        var mb = childPrefab.GetComponent<MotherboardController>();
        if (mb != null) mb.MarkInstalledInSystemUnit();

        var phase = childPrefab.GetComponent<MotherboardPhaseManager>();
        if (phase != null) phase.SetPhase1Interactive();
    }

    public void CloseInnerPanel()
    {
        if (!_isInnerPanelOpen || _activeChildPrefab == null) return;

        ActivateChildDetailedView(_activeChildPrefab, false);

        gameObject.SetActive(true);

        _activeChildPrefab.transform.SetParent(_childOriginalParent, true);
        _activeChildPrefab.transform.localPosition = _childOriginalLocalPos;

        SystemUnitController controller = GetComponent<SystemUnitController>();
        if (controller != null)
        {
            if (coverController != null && coverController.IsOpen())
                controller.RemoveCover();
            else
                controller.AttachCover();
        }

        if (innerEditingPanel != null)
            innerEditingPanel.SetActive(false);

        _activeChildPrefab = null;
        _isInnerPanelOpen = false;
    }

    private void ActivateChildDetailedView(GameObject childPrefab, bool active)
    {
        foreach (Transform child in childPrefab.transform)
        {
            if (child.name.Contains("Detailed"))
            {
                child.gameObject.SetActive(active);
                return;
            }
        }
    }
}