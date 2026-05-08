using UnityEngine;

/// <summary>
/// Utility for instantiating hardware prefabs by name using the HardwarePrefabRegistry.
/// Used by HardwareSlotManager to install children and by DragFromUI to spawn from hardware area.
/// </summary>
public static class PrefabInstaller
{
    // Static reference to the registry (lazy-loaded on first use)
    private static HardwarePrefabRegistry _registry;

    /// <summary>
    /// Get the HardwarePrefabRegistry singleton.
    /// Searches for it in Resources folder if not already cached.
    /// </summary>
    private static HardwarePrefabRegistry GetRegistry()
    {
        if (_registry != null)
            return _registry;

        // Try to find in Resources folder
        _registry = Resources.Load<HardwarePrefabRegistry>("HardwarePrefabRegistry");
        if (_registry == null)
        {
            Debug.LogError("[PrefabInstaller] HardwarePrefabRegistry not found in Resources folder");
        }

        return _registry;
    }

    /// <summary>
    /// Instantiate a prefab by name, as a child of a parent transform.
    /// </summary>
    /// <param name="prefabName">Display name (e.g., "Motherboard")</param>
    /// <param name="parentTransform">Parent transform for the instantiated object</param>
    /// <returns>Instantiated GameObject, or null if prefab not found</returns>
    public static GameObject InstantiatePrefabByName(string prefabName, Transform parentTransform)
    {
        return InstantiatePrefabByName(prefabName, parentTransform, Vector3.zero);
    }

    /// <summary>
    /// Instantiate a prefab by name, at a specific position.
    /// </summary>
    /// <param name="prefabName">Display name (e.g., "Motherboard")</param>
    /// <param name="parentTransform">Parent transform (or null for world space)</param>
    /// <param name="position">World position or local position (depending on parent)</param>
    /// <returns>Instantiated GameObject, or null if prefab not found</returns>
    public static GameObject InstantiatePrefabByName(string prefabName, Transform parentTransform, Vector3 position)
    {
        HardwarePrefabRegistry registry = GetRegistry();
        if (registry == null)
            return null;

        // Look up prefab path in registry
        GameObject prefab = registry.GetPrefab(prefabName);
        if (prefab == null)
        {
            Debug.LogError($"[PrefabInstaller] Failed to get prefab '{prefabName}' from registry");
            return null;
        }

        // Instantiate with correct parent
        GameObject instance;
        if (parentTransform != null)
        {
            instance = Object.Instantiate(prefab, position, Quaternion.identity, parentTransform);
        }
        else
        {
            instance = Object.Instantiate(prefab, position, Quaternion.identity);
        }

        // Ensure necessary components exist
        if (instance.GetComponent<IHardwareState>() == null)
        {
            Debug.LogWarning($"[PrefabInstaller] Instantiated {prefabName} lacks IHardwareState component");
        }

        if (instance.GetComponent<HardwareEditLock>() == null)
        {
            Debug.LogWarning($"[PrefabInstaller] Instantiated {prefabName} lacks HardwareEditLock component");
        }

        Debug.Log($"[PrefabInstaller] Instantiated {prefabName} as child of {parentTransform?.name ?? "world"}");
        return instance;
    }

    /// <summary>
    /// Instantiate a prefab by name in the world (no parent, no specific position).
    /// Used for spawning new instances from the hardware area.
    /// </summary>
    public static GameObject InstantiatePrefabByName(string prefabName)
    {
        return InstantiatePrefabByName(prefabName, null, Vector3.zero);
    }
}