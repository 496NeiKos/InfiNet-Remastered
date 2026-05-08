using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject registry that maps hardware prefab names to their asset paths.
/// Used by PrefabInstaller to look up and instantiate prefabs by name.
///
/// Create via: Right-click → Create → HardwarePrefabRegistry
/// </summary>
[CreateAssetMenu(fileName = "HardwarePrefabRegistry", menuName = "Hardware/Prefab Registry")]
public class HardwarePrefabRegistry : ScriptableObject
{
    [System.Serializable]
    public class PrefabEntry
    {
        [Tooltip("Display name of the prefab (e.g., 'Motherboard', 'CPU', 'GPU')")]
        public string prefabName;

        [Tooltip("Path to the prefab asset (e.g., 'Assets/Prefabs/Hardware/Motherboard.prefab')")]
        public string prefabPath;
    }

    [SerializeField]
    [Tooltip("List of all hardware prefabs in the game")]
    private List<PrefabEntry> prefabRegistry = new List<PrefabEntry>();

    /// <summary>
    /// Get the asset path for a prefab by its name.
    /// </summary>
    /// <param name="prefabName">Display name (e.g., "Motherboard")</param>
    /// <returns>Asset path, or empty string if not found</returns>
    public string GetPrefabPath(string prefabName)
    {
        foreach (PrefabEntry entry in prefabRegistry)
        {
            if (entry.prefabName == prefabName)
                return entry.prefabPath;
        }

        Debug.LogWarning($"[HardwarePrefabRegistry] Prefab '{prefabName}' not found in registry");
        return "";
    }

    /// <summary>
    /// Load a prefab asset by its name.
    /// </summary>
    /// <param name="prefabName">Display name (e.g., "Motherboard")</param>
    /// <returns>Prefab GameObject, or null if not found</returns>
    public GameObject GetPrefab(string prefabName)
    {
        string path = GetPrefabPath(prefabName);
        if (string.IsNullOrEmpty(path))
            return null;

        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            Debug.LogError($"[HardwarePrefabRegistry] Failed to load prefab at path: {path}");

        return prefab;
    }

    /// <summary>
    /// Check if a prefab is registered by name.
    /// </summary>
    public bool HasPrefab(string prefabName)
    {
        foreach (PrefabEntry entry in prefabRegistry)
        {
            if (entry.prefabName == prefabName)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Get all registered prefab names.
    /// </summary>
    public List<string> GetAllPrefabNames()
    {
        List<string> names = new List<string>();
        foreach (PrefabEntry entry in prefabRegistry)
        {
            names.Add(entry.prefabName);
        }
        return names;
    }
}