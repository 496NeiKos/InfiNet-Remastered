using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all installation slots in a parent hardware's components container.
///
/// KEY BEHAVIOR:
/// - On Start: Finds all slots in hardwareComponents, each slot auto-detects its default child
/// - LoadAllSlotStates: If state says slot is empty, DESTROY the default child
/// - RemovePrefabFromSlot: Destroys child and immediately saves parent state
/// - InstallPrefabInSlot: Instantiates child and immediately saves parent state
/// </summary>
public class HardwareSlotManager : MonoBehaviour
{
    [Tooltip("The container that holds all slot containers (e.g., SystemUnitHardwareComponents)")]
    [SerializeField] private GameObject hardwareComponents;

    private Dictionary<string, SlotContainer> slots = new Dictionary<string, SlotContainer>();

    private void Start()
    {
        // Try to find hardwareComponents from SystemUnitController if not assigned
        if (hardwareComponents == null)
        {
            SystemUnitController controller = GetComponent<SystemUnitController>();
            if (controller != null)
                hardwareComponents = controller.systemUnitHardwareComponents;
        }

        if (hardwareComponents == null)
        {
            Debug.LogError($"[HardwareSlotManager] No hardwareComponents found on '{name}'");
            return;
        }

        FindAllSlots();
        Invoke(nameof(LoadAllSlotStates), 0f);
    }

    private void FindAllSlots()
    {
        slots.Clear();

        SlotContainer[] found = hardwareComponents.GetComponentsInChildren<SlotContainer>(true);
        foreach (SlotContainer slot in found)
        {
            string type = slot.GetSlotType();
            if (!slots.ContainsKey(type))
                slots[type] = slot;
        }

        Debug.Log($"[HardwareSlotManager] Found {slots.Count} slots on '{name}'");
    }

    // ── Public Access ─────────────────────────────────────────────────────

    public SlotContainer GetSlotByType(string slotType)
    {
        slots.TryGetValue(slotType, out SlotContainer slot);
        return slot;
    }

    public Dictionary<string, string> GetAllSlotStates()
    {
        Dictionary<string, string> states = new Dictionary<string, string>();
        foreach (KeyValuePair<string, SlotContainer> kvp in slots)
            states[kvp.Key] = kvp.Value.GetSlotState();
        return states;
    }

    // ── Install / Remove ──────────────────────────────────────────────────

    public bool InstallPrefabInSlot(string prefabName, string slotType)
    {
        SlotContainer slot = GetSlotByType(slotType);
        if (slot == null) return false;
        if (!slot.CanAcceptPrefab(prefabName)) return false;
        if (!slot.IsSlotEmpty()) return false;

        GameObject child = PrefabInstaller.InstantiatePrefabByName(prefabName, slot.transform);
        if (child == null) return false;

        slot.InstallChild(child, prefabName);

        HardwareEditLock lk = child.GetComponent<HardwareEditLock>();
        if (lk != null) lk.Lock();

        SaveParentState();
        return true;
    }

    public bool RemovePrefabFromSlot(string slotType)
    {
        SlotContainer slot = GetSlotByType(slotType);
        if (slot == null || slot.IsSlotEmpty()) return false;

        slot.DestroyChild();
        SaveParentState();
        return true;
    }

    // ── State Save / Load ─────────────────────────────────────────────────

    public void SaveAllSlotStatesToStateData(HardwareStateData stateData)
    {
        foreach (KeyValuePair<string, SlotContainer> kvp in slots)
        {
            string key = $"slot_{kvp.Key}_installed";
            stateData.SetString(key, kvp.Value.GetSlotState());
        }
    }

    public void LoadAllSlotStates()
    {
        IHardwareState parentState = GetComponent<IHardwareState>();
        if (parentState == null) return;
        if (HardwareStateManager.Instance == null) return;

        if (!HardwareStateManager.Instance.HasSavedState(parentState.GetHardwareId()))
        {
            Debug.Log($"[HardwareSlotManager] No saved state for '{parentState.GetHardwareId()}', keeping defaults");
            return;
        }

        HardwareStateData stateData =
            HardwareStateManager.Instance.GetHardwareStateData(parentState.GetHardwareId());

        foreach (KeyValuePair<string, SlotContainer> kvp in slots)
        {
            string slotType = kvp.Key;
            SlotContainer slot = kvp.Value;
            string stateKey = $"slot_{slotType}_installed";
            string savedName = stateData.GetString(stateKey, null);
            GameObject current = slot.GetInstalledChild();

            if (savedName == null)
            {
                continue;
            }

            if (savedName == "")
            {
                if (current != null)
                {
                    Debug.Log($"[HardwareSlotManager] Slot '{slotType}': state=empty, destroying child");
                    slot.DestroyChild();
                }
                continue;
            }

            if (current != null)
            {
                IHardwareState cs = current.GetComponent<IHardwareState>();
                if (cs != null) HardwareStateManager.Instance.LoadHardwareState(cs);
            }
            else
            {
                InstallPrefabInSlot(savedName, slotType);
            }
        }
    }

    private void SaveParentState()
    {
        IHardwareState ps = GetComponent<IHardwareState>();
        if (ps != null && HardwareStateManager.Instance != null)
            HardwareStateManager.Instance.SaveHardwareState(ps);
    }
}