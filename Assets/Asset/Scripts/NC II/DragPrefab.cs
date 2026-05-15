using UnityEngine;
using UnityEngine.EventSystems;

public class DragPrefab : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform workspaceArea;
    private RectTransform hardwareArea;
    private Vector3 _originalPos;
    private Transform _originalParent;
    private bool _isDragging = false;
    private bool _wasInSlot = false;

    private void Start()
    {
        workspaceArea = GameManager.Instance.workspaceArea;
        hardwareArea = GameManager.Instance.hardwareArea;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        bool isInSlot = GetComponentInParent<SlotContainer>() != null;

        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen && !isInSlot)
        {
            _isDragging = false;
            return;
        }

        var dvm = FindObjectOfType<DetailViewManager>();
        if (dvm != null && dvm.IsInnerPanelOpen)
        {
            _isDragging = false;
            return;
        }

        var cover = GetComponentInParent<CoverController>();
        if (cover != null && cover.IsSliding)
        {
            _isDragging = false;
            return;
        }

        if (!AreAllScrewsEmpty())
        {
            _isDragging = false;
            return;
        }

        if (!AreAllCablesDetached())
        {
            _isDragging = false;
            return;
        }

        _isDragging = true;
        _originalPos = transform.position;
        _originalParent = transform.parent;
        _wasInSlot = isInSlot;

        if (_wasInSlot)
            transform.SetParent(GameManager.Instance.workspaceArea, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f));
        transform.position = worldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        bool onHardwareArea = RectTransformUtility.RectangleContainsScreenPoint(
            hardwareArea, eventData.position, eventData.pressEventCamera);
        bool onWorkspace = RectTransformUtility.RectangleContainsScreenPoint(
            workspaceArea, eventData.position, eventData.pressEventCamera);

        if (_wasInSlot)
        {
            if (onHardwareArea)
            {
                var slot = _originalParent.GetComponent<SlotContainer>();
                if (slot != null) slot.RemoveChild();

                transform.SetParent(GameManager.Instance.workspaceArea, true);

                var mb = GetComponent<MotherboardController>();
                if (mb != null) mb.MarkUninstalled();
            }
            else
            {
                transform.SetParent(_originalParent, true);
                transform.position = _originalPos;
            }
        }
        else
        {
            if (onHardwareArea)
                Destroy(gameObject);
            else if (!onWorkspace)
                transform.position = _originalPos;
        }
    }

    private bool AreAllScrewsEmpty()
    {
        foreach (var s in GetComponentsInChildren<ScrewController>(true))
            if (!s.IsUnscrewed()) return false;
        return true;
    }

    private bool AreAllCablesDetached()
    {
        foreach (var c in GetComponentsInChildren<CableSlot>(true))
            if (c.IsInstalled()) return false;
        return true;
    }
}