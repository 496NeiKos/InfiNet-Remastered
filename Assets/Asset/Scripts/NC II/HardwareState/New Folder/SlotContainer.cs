using UnityEngine;

/// <summary>
/// Represents one installation slot in a parent hardware's DetailGroup.
/// Example: MotherboardSlot in SystemUnit can only hold Motherboard prefabs.
///
/// Responsibilities:
/// - Track slot type (configurable, e.g., "Motherboard", "CPU")
/// - Track current installed child (null = empty, GameObject = full)
/// - Validate drop acceptance (only correct prefab types)
/// - Provide visual/data feedback
/// </summary>
public class SlotContainer : MonoBehaviour
{
    [Tooltip("Type of hardware this slot accepts (e.g., 'Motherboard', 'CPU', 'GPU')")]
    [SerializeField] private string slotType = "Unknown";

    [Tooltip("Currently installed child prefab in this slot (null = empty)")]
    private GameObject installedChild = null;

    [Tooltip("Name of the prefab currently installed (e.g., 'Motherboard')")]
    private string installedPrefabName = "";

    // ── Getters ───────────────────────────────────────────────────────────

    /// <summary>
    /// Get the type of hardware this slot accepts.
    /// </summary>
    public string GetSlotType() => slotType;

    /// <summary>
    /// Check if this slot is currently empty.
    /// </summary>
    public bool IsSlotEmpty() => installedChild == null;

    /// <summary>
    /// Get the currently installed child GameObject (or null if empty).
    /// </summary>
    public GameObject GetInstalledChild() => installedChild;

    /// <summary>
    /// Get the prefab name of the installed child (or empty string if empty).
    /// </summary>
    public string GetInstalledPrefabName() => installedPrefabName;

    // ── Setters ───────────────────────────────────────────────────────────

    /// <summary>
    /// Check if this slot can accept a specific prefab type.
    /// </summary>
    /// <param name="prefabName">Name of the prefab to check (e.g., "Motherboard")</param>
    /// <returns>True if this slot's type matches the prefab name</returns>
    public bool CanAcceptPrefab(string prefabName)
    {
        return slotType == prefabName;
    }

    /// <summary>
    /// Install a prefab instance into this slot.
    /// Should only be called by HardwareSlotManager after validation.
    /// </summary>
    /// <param name="childInstance">The instantiated child GameObject</param>
    /// <param name="prefabName">Name of the prefab (for state tracking)</param>
    public void InstallChild(GameObject childInstance, string prefabName)
    {
        if (childInstance == null)
        {
            Debug.LogError($"[SlotContainer] Cannot install null child in slot {slotType}");
            return;
        }

        // Uninstall existing child first if any
        if (installedChild != null)
            RemoveChild();

        // Set new child
        installedChild = childInstance;
        installedPrefabName = prefabName;

        // Make it a child of this slot
        childInstance.transform.SetParent(transform, false);

        Debug.Log($"[SlotContainer] Installed {prefabName} in slot {slotType}");
    }

    /// <summary>
    /// Remove the installed child from this slot (does NOT destroy it).
    /// Returns the removed child so caller can handle it (destroy, move, etc).
    /// </summary>
    public GameObject RemoveChild()
    {
        if (installedChild == null)
        {
            Debug.LogWarning($"[SlotContainer] Slot {slotType} is already empty");
            return null;
        }

        GameObject removed = installedChild;
        installedChild = null;
        installedPrefabName = "";

        // Un-parent it
        removed.transform.SetParent(null, false);

        Debug.Log($"[SlotContainer] Removed child from slot {slotType}");
        return removed;
    }

    // ── State Saving/Loading ───────────────────────────────────────────────

    /// <summary>
    /// Get the current state of this slot for saving.
    /// </summary>
    public string GetSlotState()
    {
        return IsSlotEmpty() ? "" : installedPrefabName;
    }

    /// <summary>
    /// Check if a child is installed in this slot.
    /// Used to determine if we should restore it on load.
    /// </summary>
    public bool HasInstalledChild() => !IsSlotEmpty();
}