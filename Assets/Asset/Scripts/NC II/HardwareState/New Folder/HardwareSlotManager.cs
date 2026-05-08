using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all installation slots in a parent hardware's DetailGroup.
/// Orchestrates: installation, removal, validation, state management.
///
/// Attach to the ROOT of any hardware that can have child installations
/// (e.g., SystemUnit, Motherboard, etc.)
/// </summary>
public class HardwareSlotManager : MonoBehaviour
{
    [Tooltip("Reference to the HardwarePrefabRegistry (ScriptableObject)")]
    [SerializeField] private HardwarePrefabRegistry prefabRegistry;

    [Tooltip("The DetailGroup that contains all slot containers")]
    [SerializeField] private GameObject detailGroup;

    /// <summary>
    /// Dictionary mapping slot type → SlotContainer.
    /// Populated at Start() by finding all SlotContainer children.
    /// </summary>
    private Dictionary<string, SlotContainer> slots = new Dictionary<string, SlotContainer>();

    private void Start()
    {
        // Find the DetailGroup if not assigned
        if (detailGroup == null)
        {
            SystemUnitController controller = GetComponent<SystemUnitController>();
            if (controller != null)
                detailGroup = controller.detailGroup;
        }

        if (detailGroup == null)
        {
            Debug.LogError($"[HardwareSlotManager] DetailGroup not found on {name}");
            return;
        }

        // Discover all SlotContainers in DetailGroup
        FindAllSlots();

        // Load saved state from HardwareStateManager
        LoadAllSlotStates();
    }

    // ── Slot Discovery ────────────────────────────────────────────────────

    private void FindAllSlots()
    {
        slots.Clear();

        SlotContainer[] allSlots = detailGroup.GetComponentsInChildren<SlotContainer>(true);
        foreach (SlotContainer slot in allSlots)
        {
            string slotType = slot.GetSlotType();
            if (slots.ContainsKey(slotType))
            {
                Debug.LogWarning($"[HardwareSlotManager] Duplicate slot type '{slotType}' found");
                continue;
            }

            slots[slotType] = slot;
            Debug.Log($"[HardwareSlotManager] Found slot type: {slotType}");
        }

        if (slots.Count == 0)
            Debug.LogWarning($"[HardwareSlotManager] No slot containers found in {detailGroup.name}");
    }

    // ── Slot Access ───────────────────────────────────────────────────────

    /// <summary>
    /// Get a slot by its type.
    /// </summary>
    public SlotContainer GetSlotByType(string slotType)
    {
        if (slots.TryGetValue(slotType, out SlotContainer slot))
            return slot;

        Debug.LogWarning($"[HardwareSlotManager] Slot type '{slotType}' not found");
        return null;
    }

    // ── Installation/Removal ───────────────────────────────────────────────

    /// <summary>
    /// Install a prefab in a specific slot.
    /// </summary>
    /// <param name="prefabName">Name of prefab to install (e.g., "Motherboard")</param>
    /// <param name="slotType">Type of slot to install into (e.g., "motherboard")</param>
    public bool InstallPrefabInSlot(string prefabName, string slotType)
    {
        SlotContainer slot = GetSlotByType(slotType);
        if (slot == null)
            return false;

        // Validate: slot type matches prefab name
        if (!slot.CanAcceptPrefab(prefabName))
        {
            Debug.LogError($"[HardwareSlotManager] Slot {slotType} cannot accept {prefabName}");
            return false;
        }

        // Validate: slot is empty
        if (!slot.IsSlotEmpty())
        {
            Debug.LogWarning($"[HardwareSlotManager] Slot {slotType} is already full");
            return false;
        }

        // Instantiate the prefab
        if (prefabRegistry == null)
        {
            Debug.LogError("[HardwareSlotManager] PrefabRegistry is not assigned");
            return false;
        }

        GameObject childInstance = PrefabInstaller.InstantiatePrefabByName(prefabName, slot.transform);
        if (childInstance == null)
            return false;

        // Install it
        slot.InstallChild(childInstance, prefabName);

        // Lock the child (prevent editing while installed)
        HardwareEditLock childLock = childInstance.GetComponent<HardwareEditLock>();
        if (childLock != null)
            childLock.Lock();

        // Notify state manager
        if (HardwareStateManager.Instance != null)
        {
            IHardwareState parentState = GetComponent<IHardwareState>();
            if (parentState != null)
                HardwareStateManager.Instance.SaveHardwareState(parentState);
        }

        return true;
    }

    /// <summary>
    /// Remove a prefab from a specific slot (does NOT destroy it).
    /// </summary>
    /// <param name="slotType">Type of slot to remove from</param>
    /// <returns>The removed child GameObject, or null if slot was empty</returns>
    public GameObject RemovePrefabFromSlot(string slotType)
    {
        SlotContainer slot = GetSlotByType(slotType);
        if (slot == null)
            return null;

        GameObject removed = slot.RemoveChild();
        if (removed == null)
        {
            Debug.LogWarning($"[HardwareSlotManager] Slot {slotType} is already empty");
            return null;
        }

        // Unlock the child (allow editing after removal)
        HardwareEditLock childLock = removed.GetComponent<HardwareEditLock>();
        if (childLock != null)
            childLock.Unlock();

        // Notify state manager
        if (HardwareStateManager.Instance != null)
        {
            IHardwareState parentState = GetComponent<IHardwareState>();
            if (parentState != null)
                HardwareStateManager.Instance.SaveHardwareState(parentState);
        }

        return removed;
    }

    // ── Validation ─────────────────────────────────────────────────────────

    /// <summary>
    /// Check if a prefab can be dropped into a slot.
    /// Validates: slot type match + not locked in another parent.
    /// </summary>
    public bool CanDropInSlot(string prefabName, string slotType)
    {
        SlotContainer slot = GetSlotByType(slotType);
        if (slot == null)
            return false;

        // Slot type must match
        if (!slot.CanAcceptPrefab(prefabName))
            return false;

        // Slot must be empty
        if (!slot.IsSlotEmpty())
            return false;

        return true;
    }

    // ── State Management ───────────────────────────────────────────────────

    /// <summary>
    /// Get the state of all slots (for saving).
    /// Returns a Dictionary: slotType → installedPrefabName (or "" if empty)
    /// </summary>
    public Dictionary<string, string> GetAllSlotStates()
    {
        Dictionary<string, string> states = new Dictionary<string, string>();

        foreach (KeyValuePair<string, SlotContainer> kvp in slots)
        {
            states[kvp.Key] = kvp.Value.GetSlotState();
        }

        return states;
    }

    /// <summary>
    /// Restore all slots from saved state (for loading).
    /// </summary>
    public void LoadAllSlotStates()
    {
        IHardwareState parentState = GetComponent<IHardwareState>();
        if (parentState == null)
            return;

        if (HardwareStateManager.Instance == null)
            return;

        HardwareStateData stateData = HardwareStateManager.Instance.GetHardwareStateData(parentState.GetHardwareId());
        if (stateData == null)
        {
            Debug.Log($"[HardwareSlotManager] No saved state found for {parentState.GetHardwareId()}, using defaults");
            return;
        }

        // Restore each slot from state
        foreach (KeyValuePair<string, SlotContainer> kvp in slots)
        {
            string slotType = kvp.Key;
            SlotContainer slot = kvp.Value;

            // Get saved prefab name for this slot (or "" if empty)
            string stateKey = $"slot_{slotType}_installed";
            string installedPrefabName = stateData.GetString(stateKey, "");

            // If slot should have a child, instantiate and install it
            if (!string.IsNullOrEmpty(installedPrefabName))
            {
                InstallPrefabInSlot(installedPrefabName, slotType);

                // Load that child's state too
                GameObject installedChild = slot.GetInstalledChild();
                if (installedChild != null)
                {
                    IHardwareState childState = installedChild.GetComponent<IHardwareState>();
                    if (childState != null)
                    {
                        HardwareStateManager.Instance.LoadHardwareState(childState);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Save all slot states through the parent's IHardwareState.
    /// Called by SystemUnitStateManager.SaveState(), etc.
    /// </summary>
    public void SaveAllSlotStatesToStateData(HardwareStateData stateData)
    {
        foreach (KeyValuePair<string, SlotContainer> kvp in slots)
        {
            string slotType = kvp.Key;
            SlotContainer slot = kvp.Value;
            string installedName = slot.GetSlotState(); // "" if empty, or prefab name

            string stateKey = $"slot_{slotType}_installed";
            stateData.SetString(stateKey, installedName);

            // Also save the child's state if it exists
            if (!slot.IsSlotEmpty())
            {
                GameObject child = slot.GetInstalledChild();
                IHardwareState childState = child.GetComponent<IHardwareState>();
                if (childState != null)
                {
                    HardwareStateData childStateData = childState.SaveState();
                    string childStateKey = $"slot_{slotType}_state";
                    // Store child state as nested data (serialized as JSON or flattened)
                    stateData.SetString(childStateKey, JsonUtility.ToJson(childStateData));
                }
            }
        }
    }
}