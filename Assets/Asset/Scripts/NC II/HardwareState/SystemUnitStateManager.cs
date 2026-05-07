using UnityEngine;

/// <summary>
/// Handles save/load state for SystemUnit hardware.
/// Tracks which components (Motherboard, HDD, PSU, etc.) are active.
/// </summary>
public class SystemUnitStateManager : MonoBehaviour, IHardwareState
{
    [SerializeField] private GameObject motherboard;
    [SerializeField] private GameObject hdd;
    [SerializeField] private GameObject psu;
    // Add more components here as needed

    /// <summary>
    /// Unique ID for this system unit instance.
    /// Can be set in Inspector or auto-generated on Start.
    /// </summary>
    [SerializeField] private string hardwareId = "SystemUnit_0";

    private void Start()
    {
        // Auto-generate ID if empty
        if (string.IsNullOrEmpty(hardwareId))
            hardwareId = $"SystemUnit_{GetInstanceID()}";

        // Try to load any previously saved state
        if (HardwareStateManager.Instance != null)
            HardwareStateManager.Instance.LoadHardwareState(this);
    }

    public string GetHardwareId()
    {
        return hardwareId;
    }

    public HardwareStateData SaveState()
    {
        HardwareStateData stateData = new HardwareStateData("SystemUnit");

        // Save the active state of each component
        stateData.SetBool("motherboard_active", motherboard != null ? motherboard.activeInHierarchy : false);
        stateData.SetBool("hdd_active", hdd != null ? hdd.activeInHierarchy : false);
        stateData.SetBool("psu_active", psu != null ? psu.activeInHierarchy : false);

        Debug.Log($"[SystemUnitStateManager] Saved state for {hardwareId}");
        return stateData;
    }

    public void LoadState(HardwareStateData stateData)
    {
        if (stateData == null || stateData.hardwareType != "SystemUnit")
        {
            Debug.LogWarning($"[SystemUnitStateManager] Invalid state data for {hardwareId}");
            return;
        }

        // Restore the active state of each component
        if (motherboard != null)
            motherboard.SetActive(stateData.GetBool("motherboard_active", true));

        if (hdd != null)
            hdd.SetActive(stateData.GetBool("hdd_active", true));

        if (psu != null)
            psu.SetActive(stateData.GetBool("psu_active", true));

        Debug.Log($"[SystemUnitStateManager] Loaded state for {hardwareId}");
    }

    public void ClearState()
    {
        if (HardwareStateManager.Instance != null)
            HardwareStateManager.Instance.ClearHardwareState(hardwareId);

        Debug.Log($"[SystemUnitStateManager] Cleared state for {hardwareId}");
    }
}