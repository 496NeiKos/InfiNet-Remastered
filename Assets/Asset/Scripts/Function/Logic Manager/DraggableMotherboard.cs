using UnityEngine;
using UnityEngine.InputSystem;

public class DraggableMotherboard : MonoBehaviour
{
    private Collider2D col;
    private Vector3 startDragPosition;
    private bool isDragging = false;
    public ItemSlot currentSlot = null;
    public static DraggableMotherboard currentlyDraggedItem = null;

    private InputAction mouseClickAction;
    private InputAction mousePositionAction;

    public Collider2D workbenchArea;
    public Transform itemSlotsParent;

    private bool dragItemOnly = false;
    private bool isAttemptingDrag = false;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError("No Collider2D found on the motherboard!");
        }

        mouseClickAction = new InputAction("MouseClick", binding: "<Mouse>/leftButton");
        mousePositionAction = new InputAction("MousePosition", binding: "<Mouse>/position");

        mouseClickAction.performed += context => OnMouseClick();
        mouseClickAction.canceled += context => OnMouseRelease();

        mouseClickAction.Enable();
        mousePositionAction.Enable();

        startDragPosition = transform.position;
    }

    private void OnDestroy()
    {
        mouseClickAction.Disable();
        mousePositionAction.Disable();
    }

    private void OnMouseClick()
    {
        AttemptStartDrag();
    }

    private void OnMouseRelease()
    {
        StopDrag();
    }

    private void AttemptStartDrag()
    {
        if (currentlyDraggedItem != null) return;

        Vector2 mousePos = mousePositionAction.ReadValue<Vector2>();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = transform.position.z;

        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);
        bool hitSelf = false;
        bool hitItem = false;

        foreach (RaycastHit2D hit in hits)
        {
            DragItem dragItem = hit.collider.GetComponent<DragItem>();
            if (dragItem != null)
            {
                hitItem = true;
                break;
            }

            ItemSlot itemSlot = hit.collider.GetComponent<ItemSlot>();
            DraggableMotherboard hitMotherboard = hit.collider.GetComponent<DraggableMotherboard>();

            if (itemSlot != null && itemSlot.currentItem != null)
            {
                hitItem = true;
                break;
            }

            if (hitMotherboard == this)
            {
                hitSelf = true;
            }
        }

        if (hitItem)
        {
            dragItemOnly = true;
            isAttemptingDrag = false;
            return;
        }
        else if (hitSelf)
        {
            dragItemOnly = false;
            isAttemptingDrag = true;
            startDragPosition = transform.position;

            if (currentSlot != null)
            {
                currentSlot.ClearSlot();
                currentSlot = null;
            }
        }
        else
        {
            isAttemptingDrag = false;
        }
    }

    private void StopDrag()
    {
        isDragging = false;

        if (!dragItemOnly)
        {
            Collider2D[] colliders = Physics2D.OverlapPointAll(transform.position);
            foreach (Collider2D col in colliders)
            {
                SystemUnit systemUnit = col.GetComponent<SystemUnit>();
                if (systemUnit != null)
                {
                    systemUnit.ToggleMotherboard();
                    transform.position = startDragPosition;
                    return;
                }
            }

            // ✅ Snap back if outside bounds
            if (workbenchArea != null && !workbenchArea.bounds.Contains(transform.position))
            {
                transform.position = startDragPosition;
            }
        }

        if (itemSlotsParent != null)
        {
            itemSlotsParent.position = transform.position;
        }

        isAttemptingDrag = false;
        currentlyDraggedItem = null;
    }

    void Update()
    {
        if (isAttemptingDrag)
        {
            isDragging = true;
            isAttemptingDrag = false;
            currentlyDraggedItem = this;
        }

        if (isDragging && !dragItemOnly)
        {
            // ✅ Always clamp to workbench
            transform.position = GetMouseWorldPosition();

            if (itemSlotsParent != null)
            {
                itemSlotsParent.position = transform.position;
            }
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 mousePos = mousePositionAction.ReadValue<Vector2>();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0f;

        // ✅ Clamp before returning
        return ClampPositionToWorkBench(worldPos);
    }

    private Vector3 ClampPositionToWorkBench(Vector3 position)
    {
        if (workbenchArea == null || col == null) return position;

        Bounds workbenchBounds = workbenchArea.bounds;
        Bounds motherboardBounds = col.bounds;

        // Calculate half extents of the motherboard collider
        Vector3 halfSize = motherboardBounds.extents;

        // Clamp so the entire collider stays inside
        float x = Mathf.Clamp(position.x,
            workbenchBounds.min.x + halfSize.x,
            workbenchBounds.max.x - halfSize.x);

        float y = Mathf.Clamp(position.y,
            workbenchBounds.min.y + halfSize.y,
            workbenchBounds.max.y - halfSize.y);

        return new Vector3(x, y, position.z);
    }

}
