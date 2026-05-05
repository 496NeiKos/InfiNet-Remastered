using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class DragItem : MonoBehaviour
{
    private Collider2D col;
    private Vector3 startDragPosition;
    private bool isDragging = false;
    public ItemSlot currentSlot = null;
    public static DragItem currentlyDraggedItem = null;

    private InputAction mouseClickAction;
    private InputAction mousePositionAction;

    public Collider2D workbenchArea;
    public Collider2D itemArea;

    private static Dictionary<DragItem, Vector3> initialItemPositions = new Dictionary<DragItem, Vector3>();
    private static bool initialPositionsCalculated = false;

    [Header("Sprites")]
    public Sprite defaultSprite;   // side view
    public Sprite slotSprite;      // top view
    private SpriteRenderer spriteRenderer;


    private void Awake()
    {
        col = GetComponent<Collider2D>();

        mouseClickAction = new InputAction("MouseClick", binding: "<Mouse>/leftButton");
        mousePositionAction = new InputAction("MousePosition", binding: "<Mouse>/position");

        mouseClickAction.performed += context => OnMouseClickPerformed();
        mouseClickAction.canceled += context => StopDrag();

        mouseClickAction.Enable();
        mousePositionAction.Enable();

        startDragPosition = transform.position;

        if (!initialPositionsCalculated && itemArea != null)
        {
            CalculateInitialItemPositions();
            initialPositionsCalculated = true;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (defaultSprite == null && spriteRenderer != null)
        {
            defaultSprite = spriteRenderer.sprite; // fallback
        }

    }

    private void OnDestroy()
    {
        mouseClickAction.Disable();
        mousePositionAction.Disable();
    }

    private void OnMouseClickPerformed()
    {
        AttemptStartDrag();
    }

    private void AttemptStartDrag()
    {
        if (currentlyDraggedItem != null) return;

        Vector2 mousePos = mousePositionAction.ReadValue<Vector2>();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = transform.position.z;

        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);

        foreach (RaycastHit2D hit in hits)
        {
            DragItem dragItem = hit.collider.GetComponent<DragItem>();
            if (dragItem == this)
            {
                isDragging = true;
                startDragPosition = transform.position;
                currentlyDraggedItem = this;

                // ✅ Show description immediately when drag begins
                DescSlot descSlot = FindObjectOfType<DescSlot>();
                if (descSlot != null)
                {
                    descSlot.ShowDescription(this);
                }
                if (HoverLabelManager.Instance != null)
                {
                    HoverLabelManager.Instance.ShowLabel(gameObject.name.Replace("(Clone)", ""));
                }

                if (currentSlot != null)
                {
                    currentSlot.ClearSlot();
                    currentSlot = null;

                    if (itemArea != null)
                    {
                        transform.SetParent(itemArea.transform);
                        if (initialItemPositions.ContainsKey(this))
                        {
                            transform.position = initialItemPositions[this];
                        }
                    }
                    else
                    {
                        transform.SetParent(null);
                    }

                    // ✅ Revert sprite when ejected
                    SetToDefaultSprite();
                }
                break;
            }
        }
    }



    private bool IsCorrectSlot(ItemSlot slot)
    {
        string itemName = gameObject.name.Replace("(Clone)", "");
        bool result = string.Equals(slot.expectedItemName, itemName, System.StringComparison.OrdinalIgnoreCase);
        Debug.Log($"IsCorrectSlot check: Item={itemName}, Slot={slot.name}, Expected={slot.expectedItemName}, Result={result}");
        return result;
    }

    private void StopDrag()
    {
        isDragging = false;

        if (HoverLabelManager.Instance != null)
        {
            HoverLabelManager.Instance.HideLabel();
        }

        ItemSlot itemSlot = FindItemSlotCollision();

        if (itemSlot != null)
        {
            Debug.Log($"StopDrag: Colliding with ItemSlot {itemSlot.name}");

            AVR avr = FindObjectOfType<AVR>();
            if (avr != null && avr.IsOn() &&
                string.Equals(itemSlot.expectedItemName.Trim(), "Motherboard", System.StringComparison.OrdinalIgnoreCase))
            {
                TroubleshootManager.Instance.ShowMessage(
                    "Cannot remove Motherboard while AVR is turned ON. Please turn off AVR first.",
                    true
                );

                transform.position = itemSlot.transform.position;
                transform.SetParent(itemSlot.transform);
                currentlyDraggedItem = null;
                return;
            }

            SystemUnit systemUnit = FindObjectOfType<SystemUnit>();
            if (systemUnit != null && systemUnit.IsMotherboardInstalled())
            {
                TroubleshootManager.Instance.ShowMessage(
                    $"Cannot install {name} in {itemSlot.name} because motherboard is installed in system unit.",
                    true
                );

                if (initialItemPositions.ContainsKey(this))
                {
                    transform.position = initialItemPositions[this];
                    transform.SetParent(itemArea.transform);
                }
                else
                {
                    transform.position = startDragPosition;
                    transform.SetParent(itemArea.transform);
                }

                currentlyDraggedItem = null;
                return;
            }

            if (IsCorrectSlot(itemSlot))
            {
                TroubleshootManager.Instance.ShowMessage($"{name} placed correctly in {itemSlot.name}.", false);
            }
            else
            {
                TroubleshootManager.Instance.ShowMessage($"{name} placed in wrong slot {itemSlot.name}.", true);
            }

            transform.position = itemSlot.transform.position;
            transform.SetParent(itemSlot.transform);

            currentSlot = itemSlot;
            itemSlot.SetCurrentItem(this);

            SetToSlotSprite();
        }
        else
        {
            if (itemArea != null && itemArea.bounds.Contains(transform.position))
            {
                if (initialItemPositions.ContainsKey(this))
                {
                    transform.position = initialItemPositions[this];
                    transform.SetParent(itemArea.transform);
                }
                else
                {
                    transform.position = CalculateNearestGridPosition(transform.position);
                    transform.SetParent(itemArea.transform);
                }

                SetToDefaultSprite();
            }
            else if (workbenchArea != null && !workbenchArea.bounds.Contains(transform.position))
            {
                transform.position = startDragPosition;
            }
        }

        currentlyDraggedItem = null;
        // ✅ Clear description when drag ends
        DescSlot descSlot = FindObjectOfType<DescSlot>();
        if (descSlot != null)
        {
            descSlot.ClearDescription();
        }
    }




    private ItemSlot FindItemSlotCollision()
    {
        Collider2D[] colliders = Physics2D.OverlapAreaAll(col.bounds.min, col.bounds.max);
        foreach (Collider2D collider in colliders)
        {
            ItemSlot itemSlot = collider.GetComponent<ItemSlot>();
            if (itemSlot != null && itemSlot.currentItem == null)
            {
                return itemSlot;
            }
        }
        return null;
    }

    public void SetToSlotSprite()
    {
        if (slotSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = slotSprite;
            Debug.Log($"{name}: Sprite changed to slot view.");
        }
    }

    public void SetToDefaultSprite()
    {
        if (defaultSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = defaultSprite;
            Debug.Log($"{name}: Sprite reverted to default view.");
        }
    }

    private Vector3 CalculateNearestGridPosition(Vector3 position)
    {
        GridLayoutGroup gridLayoutGroup = itemArea.GetComponentInParent<GridLayoutGroup>();
        if (gridLayoutGroup == null) return position;

        Vector2 cellSize = gridLayoutGroup.cellSize;
        Vector2 cellSpacing = gridLayoutGroup.spacing;

        Vector3 localPosition = itemArea.transform.InverseTransformPoint(position);

        int xIndex = Mathf.RoundToInt(localPosition.x / (cellSize.x + cellSpacing.x));
        int yIndex = Mathf.RoundToInt(localPosition.y / (cellSize.y + cellSpacing.y));

        float xPosition = xIndex * (cellSize.x + cellSpacing.x);
        float yPosition = yIndex * (cellSize.y + cellSpacing.y);

        Vector3 gridPosition = itemArea.transform.TransformPoint(new Vector3(xPosition, yPosition, 0));
        return gridPosition;
    }

    private void CalculateInitialItemPositions()
    {
        initialItemPositions.Clear();
        DragItem[] dragItemsInArea = FindObjectsByType<DragItem>(FindObjectsSortMode.None)
            .Where(d => itemArea != null && itemArea.bounds.Contains(d.transform.position))
            .ToArray();

        foreach (DragItem dragItem in dragItemsInArea)
        {
            initialItemPositions[dragItem] = dragItem.transform.position;
        }
    }

    public void StoreInitialPosition()
    {
        if (!initialItemPositions.ContainsKey(this))
        {
            initialItemPositions[this] = transform.position;
        }
    }

    void Update()
    {
        if (isDragging)
        {
            transform.position = GetMousePositionInWorldSpace();

            Vector2 mousePos = mousePositionAction.ReadValue<Vector2>();
            if (HoverLabelManager.Instance != null)
            {
                HoverLabelManager.Instance.FollowMouse(mousePos);
            }
        }
    }

    private Vector3 GetMousePositionInWorldSpace()
    {
        Vector2 mousePos = mousePositionAction.ReadValue<Vector2>();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0f;
        return worldPos;
    }
}
