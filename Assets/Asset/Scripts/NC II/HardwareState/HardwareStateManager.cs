using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager for saving and loading hardware device states.
/// Automatically called by DragPrefab before destroy and after instantiate.
/// </summary>
public class HardwareStateManager : MonoBehaviour
{
    public static HardwareStateManager Instance { get; private set; }

    /// <summary>
    /// Dictionary mapping hardware ID to its saved state.
    /// Key: hardware ID (e.g., "SystemUnit_0")
    /// Value: the serialized state data
    /// </summary>
    private Dictionary<string, HardwareStateData> _hardwareStates = new Dictionary<string, HardwareStateData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Save the state of a hardware device before it's destroyed.
    /// Called by DragPrefab.OnEndDrag when hardware is destroyed.
    /// </summary>
    public void SaveHardwareState(IHardwareState hardware)
    {
        if (hardware == null) return;

        string hardwareId = hardware.GetHardwareId();
        HardwareStateData stateData = hardware.SaveState();

        _hardwareStates[hardwareId] = stateData;
        Debug.Log($"[HardwareStateManager] Saved state for {hardwareId}");
    }

    /// <summary>
    /// Load the state of a hardware device after it's instantiated.
    /// Called by DragPrefab.OnEndDrag when hardware is spawned in workspace.
    /// </summary>
    public void LoadHardwareState(IHardwareState hardware)
    {
        if (hardware == null) return;

        string hardwareId = hardware.GetHardwareId();

        if (_hardwareStates.TryGetValue(hardwareId, out HardwareStateData stateData))
        {
            hardware.LoadState(stateData);
            Debug.Log($"[HardwareStateManager] Loaded state for {hardwareId}");
        }
        else
        {
            Debug.Log($"[HardwareStateManager] No saved state found for {hardwareId}, using defaults");
        }
    }

    /// <summary>
    /// Clear saved state for a specific hardware device.
    /// </summary>
    public void ClearHardwareState(string hardwareId)
    {
        if (_hardwareStates.Remove(hardwareId))
            Debug.Log($"[HardwareStateManager] Cleared state for {hardwareId}");
    }

    /// <summary>
    /// Check if a saved state exists for a hardware device.
    /// </summary>
    public bool HasSavedState(string hardwareId)
    {
        return _hardwareStates.ContainsKey(hardwareId);
    }

    /// <summary>
    /// Get the raw state data (for debugging or advanced use).
    /// </summary>
    public HardwareStateData GetHardwareStateData(string hardwareId)
    {
        _hardwareStates.TryGetValue(hardwareId, out HardwareStateData data);
        return data;
    }
}