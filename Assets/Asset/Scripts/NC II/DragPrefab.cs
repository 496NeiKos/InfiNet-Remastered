using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragPrefab : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform workspaceArea;
    private RectTransform hardwareArea;
    private Vector3 originalPos;
    private bool _isDragging = false;

    private void Start()
    {
        workspaceArea = GameManager.Instance.workspaceArea;
        hardwareArea = GameManager.Instance.hardwareArea;

        if (HardwareStateManager.Instance != null)
        {
            IHardwareState hardwareState = GetComponent<IHardwareState>();
            if (hardwareState != null)
                HardwareStateManager.Instance.LoadHardwareState(hardwareState);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen && !IsInEditingPanel())
        {
            _isDragging = false;
            return;
        }

        _isDragging = true;
        originalPos = transform.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f)
        );
        transform.position = worldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        bool isChild = IsInEditingPanel();

        if (isChild)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                hardwareArea, eventData.position, eventData.pressEventCamera))
            {
                HardwareSlotManager parentSlotManager = GetComponentInParent<HardwareSlotManager>();

                if (parentSlotManager != null)
                {
                    Dictionary<string, string> slotStates = parentSlotManager.GetAllSlotStates();
                    foreach (string slotType in slotStates.Keys)
                    {
                        SlotContainer slot = parentSlotManager.GetSlotByType(slotType);
                        if (slot != null && slot.GetInstalledChild() == gameObject)
                        {
                            parentSlotManager.RemovePrefabFromSlot(slotType);
                            return;
                        }
                    }
                }

                Destroy(gameObject);
            }
            else
            {
                transform.position = originalPos;
            }
            return;
        }

        // Parent prefab in workspace
        if (RectTransformUtility.RectangleContainsScreenPoint(
            hardwareArea, eventData.position, eventData.pressEventCamera))
        {
            IHardwareState hardwareState = GetComponent<IHardwareState>();
            if (hardwareState != null && HardwareStateManager.Instance != null)
                HardwareStateManager.Instance.SaveHardwareState(hardwareState);

            Destroy(gameObject);
        }
        else if (!RectTransformUtility.RectangleContainsScreenPoint(
            workspaceArea, eventData.position, eventData.pressEventCamera))
        {
            transform.position = originalPos;
        }
    }

    /// <summary>
    /// Check if this prefab is inside the editing panel view.
    /// Walks up the parent chain looking for the EditingPanel.
    /// </summary>
    private bool IsInEditingPanel()
    {
        if (GameManager.Instance == null || GameManager.Instance.editingPanel == null)
            return false;

        Transform editingPanelTransform = GameManager.Instance.editingPanel.transform;
        Transform current = transform.parent;

        while (current != null)
        {
            if (current == editingPanelTransform)
                return true;
            current = current.parent;
        }
        return false;
    }
}