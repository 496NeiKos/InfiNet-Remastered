using UnityEngine;

/// <summary>
/// Handles save/load state for SystemUnit hardware.
/// Now tracks slot states instead of just component active/inactive.
///
/// Saves:
/// - Which child is in which slot
/// - That child's full state (recursively)
///
/// Loads:
/// - Instantiates children in slots
/// - Restores their states
/// </summary>
public class SystemUnitStateManager : MonoBehaviour, IHardwareState
{
    [SerializeField] private string hardwareId = "SystemUnit_0";

    private HardwareSlotManager _slotManager;
    private HardwareEditLock _editLock;

    private void Start()
    {
        // Auto-generate ID if empty
        if (string.IsNullOrEmpty(hardwareId))
            hardwareId = $"SystemUnit_{GetInstanceID()}";

        // Get slot manager
        _slotManager = GetComponent<HardwareSlotManager>();
        _editLock = GetComponent<HardwareEditLock>();

        if (_slotManager == null)
            Debug.LogWarning($"[SystemUnitStateManager] No HardwareSlotManager found on {name}");

        // Try to load any previously saved state
        if (HardwareStateManager.Instance != null)
            HardwareStateManager.Instance.LoadHardwareState(this);
    }

    public string GetHardwareId() => hardwareId;

    public HardwareStateData SaveState()
    {
        HardwareStateData stateData = new HardwareStateData("SystemUnit");

        // Save all slot states through the slot manager
        if (_slotManager != null)
        {
            _slotManager.SaveAllSlotStatesToStateData(stateData);
        }

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

        // Load all slot states through the slot manager
        if (_slotManager != null)
        {
            _slotManager.LoadAllSlotStates();
        }

        Debug.Log($"[SystemUnitStateManager] Loaded state for {hardwareId}");
    }

    public void ClearState()
    {
        if (HardwareStateManager.Instance != null)
            HardwareStateManager.Instance.ClearHardwareState(hardwareId);

        Debug.Log($"[SystemUnitStateManager] Cleared state for {hardwareId}");
    }

    public bool IsEditable()
    {
        if (_editLock == null)
            return true;

        return !_editLock.IsAnyLocked();
    }
}