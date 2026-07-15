using UnityEngine;

/// <summary>
/// Stub — HardwarePrefabRegistry was removed. Prefab instantiation by name is no longer
/// supported; world objects are used directly instead.
/// </summary>
public static class PrefabInstaller
{
    public static GameObject InstantiatePrefabByName(string prefabName, Transform parentTransform)
    {
        Debug.LogWarning($"[PrefabInstaller] Registry removed — cannot instantiate '{prefabName}' by name.");
        return null;
    }

    public static GameObject InstantiatePrefabByName(string prefabName, Transform parentTransform, Vector3 position)
    {
        Debug.LogWarning($"[PrefabInstaller] Registry removed — cannot instantiate '{prefabName}' by name.");
        return null;
    }

    public static GameObject InstantiatePrefabByName(string prefabName)
    {
        Debug.LogWarning($"[PrefabInstaller] Registry removed — cannot instantiate '{prefabName}' by name.");
        return null;
    }
}
