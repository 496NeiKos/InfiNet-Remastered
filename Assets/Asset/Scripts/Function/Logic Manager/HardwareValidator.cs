using UnityEngine;
using System.Collections.Generic;

public class HardwareValidator : MonoBehaviour
{
    public static HardwareValidator Instance;

    private List<ItemSlot> allSlots = new List<ItemSlot>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // ✅ Collect all slots in scene
        allSlots.AddRange(FindObjectsByType<ItemSlot>(FindObjectsSortMode.None));
    }

    public bool AreAllComponentsCorrect()
    {
        foreach (ItemSlot slot in allSlots)
        {
            // Only validate slots that expect a specific item
            if (!string.IsNullOrEmpty(slot.expectedItemName))
            {
                if (slot.currentItem != null)
                {
                    // Compare installed item name with expected name
                    string installedName = slot.currentItem.name.Replace("(Clone)", "");
                    if (!string.Equals(installedName, slot.expectedItemName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"HardwareValidator: {slot.currentItem.name} is in wrong slot {slot.name}. Expected {slot.expectedItemName}.");
                        return false;
                    }
                }
                else
                {
                    // Slot expects an item but is empty
                    Debug.Log($"HardwareValidator: Missing component {slot.expectedItemName} for slot {slot.name}.");
                    return false;
                }
            }
        }

        Debug.Log("HardwareValidator: All components installed correctly.");
        return true;
    }
}
