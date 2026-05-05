using UnityEngine;

public class ItemSlot : MonoBehaviour
{
    public DragItem currentItem = null;

    // ✅ Expected item for this slot (e.g., "CPU", "Heatsink")
    public string expectedItemName;

    // ✅ Optional dependency: e.g., heatsink requires CPU slot
    public ItemSlot requiredItemSlot;
    public string requiredItemName;

    public void SetCurrentItem(DragItem item)
    {
        // ✅ Check dependency before allowing install
        if (requiredItemSlot != null)
        {
            if (requiredItemSlot.currentItem == null ||
                !string.Equals(requiredItemSlot.currentItem.name.Replace("(Clone)", ""), requiredItemName, System.StringComparison.OrdinalIgnoreCase))
            {
                TroubleshootManager.Instance.ShowMessage(
                    $"Cannot install {item.name} because {requiredItemName} must be installed in {requiredItemSlot.name} first.",
                    true
                );

                // Snap back to item area instead of staying in slot
                item.transform.SetParent(item.itemArea.transform);
                item.transform.position = item.itemArea.transform.position;

                // ✅ Revert sprite since install failed
                item.SetToDefaultSprite();
                return;
            }
        }

        currentItem = item;
        Debug.Log($"ItemSlot {name}: Set current item to {item.name}");

        // ✅ Change sprite when item is successfully installed
        currentItem.SetToSlotSprite();
        AudioClip sfx = SoundManager.instance.dropSFX;
        SoundManager.instance.PlaySFX(sfx);
        // ✅ Task 2: Motherboard installed into System Unit
        string cleanName = item.name.Replace("(Clone)", "");
        if (cleanName.Equals("Motherboard", System.StringComparison.OrdinalIgnoreCase) &&
            expectedItemName.Equals("SystemUnit", System.StringComparison.OrdinalIgnoreCase))
        {
            TaskListManager.Instance.SetTaskCompleted(1, true);
        }

        // ✅ Check motherboard tasks
        ValidateMotherboardTasks();
    }

    public void ClearSlot()
    {
        // ✅ Block removal if a dependent item is still installed
        if (HasDependentItem())
        {
            TroubleshootManager.Instance.ShowMessage(
                $"Cannot remove {currentItem?.name} because {name} has a dependent item installed (e.g., heatsink).",
                true
            );

            if (currentItem != null)
            {
                currentItem.transform.position = transform.position;
                currentItem.transform.SetParent(transform);
            }
            return;
        }

        // ✅ Block motherboard removal if AVR is ON
        AVR avr = FindObjectOfType<AVR>();
        if (avr != null && avr.IsOn() &&
            string.Equals(expectedItemName.Trim(), "Motherboard", System.StringComparison.OrdinalIgnoreCase))
        {
            TroubleshootManager.Instance.ShowMessage(
                "Cannot remove Motherboard while AVR is turned ON. Please turn off AVR first.",
                true
            );

            if (currentItem != null)
            {
                currentItem.transform.position = transform.position;
                currentItem.transform.SetParent(transform);
            }
            return;
        }

        Debug.Log($"ItemSlot {name}: Cleared slot");

        if (currentItem != null)
        {
            string cleanName = currentItem.name.Replace("(Clone)", "");

            // ✅ Task 2 undone if motherboard removed from System Unit
            if (cleanName.Equals("Motherboard", System.StringComparison.OrdinalIgnoreCase) &&
                expectedItemName.Equals("SystemUnit", System.StringComparison.OrdinalIgnoreCase))
            {
                TaskListManager.Instance.SetTaskCompleted(1, false);
            }

            currentItem.SetToDefaultSprite();
            currentItem = null;
        }

        // ✅ Check motherboard tasks
        ValidateMotherboardTasks();
    }

    private bool HasDependentItem()
    {
        // ✅ Check if another slot depends on this one and is occupied
        ItemSlot[] allSlots = FindObjectsOfType<ItemSlot>();
        foreach (ItemSlot slot in allSlots)
        {
            if (slot.requiredItemSlot == this && slot.currentItem != null)
            {
                return true;
            }
        }
        return false;
    }

    private void ValidateMotherboardTasks()
    {
        // Find all slots in the scene
        ItemSlot[] allSlots = FindObjectsOfType<ItemSlot>();

        bool allInstalled = true;

        foreach (ItemSlot slot in allSlots)
        {
            // Only check slots that belong to the motherboard
            if (slot.transform.parent.name == "Motherboard") // adjust if your hierarchy differs
            {
                if (slot.currentItem == null)
                {
                    allInstalled = false;
                    break;
                }
            }
        }

        // Update TaskListManager
        TaskListManager.Instance.SetTaskCompleted(0, allInstalled);
    }
}
