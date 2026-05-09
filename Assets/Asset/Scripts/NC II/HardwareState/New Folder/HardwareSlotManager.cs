using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all installation slots in a parent hardware's DetailGroup.
/// Orchestrates: installation, removal, validation, state management.
///
/// KEY BEHAVIOR:
/// - When a child is removed from slot: child GameObject is DESTROYED
/// - When state loads and child should be installed: child is INSTANTIATED and installed
/// - State determines what should exist in DetailGroup
/// - Prefab default has all children, state determines which stay/leave
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

        // ✅ Save parent state immediately
        SaveParentState();

        return true;
    }

    /// <summary>
    /// Remove a prefab from a specific slot and DESTROY it.
    /// </summary>
    /// <param name="slotType">Type of slot to remove from</param>
    public bool RemovePrefabFromSlot(string slotType)
    {
        SlotContainer slot = GetSlotByType(slotType);
        if (slot == null)
            return false;

        GameObject removed = slot.RemoveChild();
        if (removed == null)
        {
            Debug.LogWarning($"[HardwareSlotManager] Slot {slotType} is already empty");
            return false;
        }

        Debug.Log($"[HardwareSlotManager] Removed and destroying child from slot {slotType}");

        // ✅ DESTROY the child immediately
        Destroy(removed);

        // ✅ Save parent state immediately (slot now reflects removal)
        SaveParentState();

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
        Debug.Log($"[HardwareSlotManager] LoadAllSlotStates called, checking {slots.Count} slots");
        foreach (KeyValuePair<string, SlotContainer> kvp in slots)
        {
            string slotType = kvp.Key;
            SlotContainer slot = kvp.Value;
            GameObject currentChild = slot.GetInstalledChild();

            Debug.Log($"[HardwareSlotManager] Checking slot {slotType}: currentChild = {(currentChild != null ? currentChild.name : "null")}");
            states[kvp.Key] = kvp.Value.GetSlotState();
        }

        return states;
    }

    /// <summary>
    /// Load and restore all slots from saved state.
    /// 
    /// KEY LOGIC:
    /// - If state says slot should be EMPTY: Destroy the child if it exists
    /// - If state says slot should have child: Instantiate and install it
    /// </summary>
    public void LoadAllSlotStates()
    {
        IHardwareState parentState = GetComponent<IHardwareState>();
        if (parentState == null)
            return;

        if (HardwareStateManager.Instance == null)
            return;

        HardwareStateData stateData = HardwareStateManager.Instance.GetHardwareStateData(parentState.GetHardwareId());

        // For each slot, check what state says it should contain
        foreach (KeyValuePair<string, SlotContainer> kvp in slots)
        {
            string slotType = kvp.Key;
            SlotContainer slot = kvp.Value;

            // Get what's currently in the slot
            GameObject currentChild = slot.GetInstalledChild();

            // Get what state says should be there
            string stateKey = $"slot_{slotType}_installed";
            string savedPrefabName = stateData != null ? stateData.GetString(stateKey, "") : "";

            // ✅ KEY LOGIC: Sync current state with saved state

            if (string.IsNullOrEmpty(savedPrefabName))
            {
                // State says: slot should be EMPTY
                if (currentChild != null)
                {
                    // But slot has a child → DESTROY it
                    Debug.Log($"[HardwareSlotManager] State says slot {slotType} should be empty, destroying child");

                    GameObject removed = slot.RemoveChild();
                    if (removed != null)
                    {
                        Destroy(removed);
                    }
                }
                else
                {
                    // Slot is already empty → OK
                    Debug.Log($"[HardwareSlotManager] Slot {slotType} is empty (as per state)");
                }
            }
            else
            {
                // State says: slot should have a child
                if (currentChild != null && currentChild.name == savedPrefabName)
                {
                    // Correct child is already there → OK
                    Debug.Log($"[HardwareSlotManager] Slot {slotType} already has correct child {savedPrefabName}");

                    // Load that child's state too
                    IHardwareState childState = currentChild.GetComponent<IHardwareState>();
                    if (childState != null && HardwareStateManager.Instance != null)
                    {
                        HardwareStateManager.Instance.LoadHardwareState(childState);
                    }
                }
                else if (currentChild != null)
                {
                    // Wrong child is there → destroy it and install correct one
                    Debug.Log($"[HardwareSlotManager] Slot {slotType} has wrong child, replacing");
                    GameObject removed = slot.RemoveChild();
                    if (removed != null)
                        Destroy(removed);

                    InstallPrefabInSlot(savedPrefabName, slotType);
                }
                else
                {
                    // Slot is empty but should have child → instantiate and install
                    Debug.Log($"[HardwareSlotManager] Slot {slotType} should have {savedPrefabName}, installing");
                    InstallPrefabInSlot(savedPrefabName, slotType);
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
                    stateData.SetString(childStateKey, JsonUtility.ToJson(childStateData));
                }
            }
        }
    }

    /// <summary>
    /// Helper: Save parent state immediately when slots change.
    /// Called after InstallPrefabInSlot or RemovePrefabFromSlot.
    /// </summary>
    private void SaveParentState()
    {
        IHardwareState parentState = GetComponent<IHardwareState>();
        if (parentState != null && HardwareStateManager.Instance != null)
        {
            HardwareStateManager.Instance.SaveHardwareState(parentState);
            Debug.Log($"[HardwareSlotManager] Saved parent state after slot change");
        }
    }
}