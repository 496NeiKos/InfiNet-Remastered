using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable container for hardware state.
/// Stores the data needed to reconstruct a hardware device's state.
/// </summary>
[System.Serializable]
public class HardwareStateData
{
    /// <summary>
    /// Type of hardware (e.g., "SystemUnit", "HDD", "GPU").
    /// Used to know which handler to instantiate on load.
    /// </summary>
    public string hardwareType;

    /// <summary>
    /// Generic key-value pairs for flexible state storage.
    /// Examples:
    ///   "motherboard_active" -> "true"
    ///   "hdd_count" -> "2"
    ///   "psu_wattage" -> "750"
    /// </summary>
    public Dictionary<string, string> stateValues = new Dictionary<string, string>();

    public HardwareStateData(string type)
    {
        hardwareType = type;
    }

    /// <summary>
    /// Helper to set a bool value in state.
    /// </summary>
    public void SetBool(string key, bool value)
    {
        stateValues[key] = value.ToString();
    }

    /// <summary>
    /// Helper to get a bool value from state.
    /// </summary>
    public bool GetBool(string key, bool defaultValue = false)
    {
        if (stateValues.TryGetValue(key, out string value))
            return bool.Parse(value);
        return defaultValue;
    }

    /// <summary>
    /// Helper to set an int value in state.
    /// </summary>
    public void SetInt(string key, int value)
    {
        stateValues[key] = value.ToString();
    }

    /// <summary>
    /// Helper to get an int value from state.
    /// </summary>
    public int GetInt(string key, int defaultValue = 0)
    {
        if (stateValues.TryGetValue(key, out string value))
            return int.Parse(value);
        return defaultValue;
    }

    /// <summary>
    /// Helper to set a string value in state.
    /// </summary>
    public void SetString(string key, string value)
    {
        stateValues[key] = value;
    }

    /// <summary>
    /// Helper to get a string value from state.
    /// </summary>
    public string GetString(string key, string defaultValue = "")
    {
        if (stateValues.TryGetValue(key, out string value))
            return value;
        return defaultValue;
    }
}