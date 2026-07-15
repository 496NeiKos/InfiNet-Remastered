using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable container for hardware state.
/// </summary>
[System.Serializable]
public class HardwareStateData
{
    public string hardwareType;
    public Dictionary<string, string> stateValues = new Dictionary<string, string>();

    public HardwareStateData(string type)
    {
        hardwareType = type;
    }

    public void SetBool(string key, bool value) => stateValues[key] = value.ToString();
    public void SetInt(string key, int value) => stateValues[key] = value.ToString();
    public void SetString(string key, string value)
    {
        // Allow saving empty string (means "slot is empty")
        stateValues[key] = value ?? "";
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        if (stateValues.TryGetValue(key, out string v) && bool.TryParse(v, out bool result))
            return result;
        return defaultValue;
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        if (stateValues.TryGetValue(key, out string v) && int.TryParse(v, out int result))
            return result;
        return defaultValue;
    }

    /// <summary>
    /// Returns null if the key has never been written.
    /// Returns "" if the key was explicitly saved as empty (slot removed).
    /// Returns the name if a prefab was saved there.
    /// </summary>
    public string GetString(string key, string defaultValue = null)
    {
        if (stateValues.TryGetValue(key, out string value))
            return value;
        return defaultValue; // null = key never existed
    }
}