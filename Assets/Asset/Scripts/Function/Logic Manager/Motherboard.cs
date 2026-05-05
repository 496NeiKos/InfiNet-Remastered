using UnityEngine;

public class Motherboard : MonoBehaviour
{
    [Tooltip("Assign all CPU/GPU/RAM/etc. slots here in Inspector")]
    public ItemSlot[] itemSlots;

    public bool AreAllSlotsCorrect()
    {
        foreach (ItemSlot slot in itemSlots)
        {
            // Slot must not be empty
            if (slot.currentItem == null)
            {
                Debug.Log($"❌ Slot {slot.name} is empty.");
                return false;
            }

            // Get actual item name (strip "(Clone)")
            string actualName = slot.currentItem.name.Replace("(Clone)", "").Trim();
            string expectedName = slot.expectedItemName.Trim();

            // Compare case-insensitive
            if (!string.Equals(expectedName, actualName, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"❌ Slot {slot.name} has wrong item: expected '{expectedName}', got '{actualName}'");
                return false;
            }

            Debug.Log($"✅ Slot {slot.name} correctly has {actualName}");
        }

        // ✅ All slots filled correctly
        return true;
    }

    private void OnValidate()
    {
        if (itemSlots == null || itemSlots.Length == 0)
        {
            Debug.LogWarning("Motherboard has no item slots assigned!");
        }
        else
        {
            Debug.Log($"Motherboard has {itemSlots.Length} slots assigned.");
        }
    }

}
