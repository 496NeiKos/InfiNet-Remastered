using UnityEngine;

/// <summary>
/// Handles save/load state for SystemUnit hardware.
/// Delegates slot management to HardwareSlotManager.
/// </summary>
public class SystemUnitStateManager : MonoBehaviour, IHardwareState
{
    [SerializeField] private string hardwareId = "SystemUnit_0";

    private HardwareSlotManager _slotManager;
    private HardwareEditLock _editLock;

    private void Start()
    {
        if (string.IsNullOrEmpty(hardwareId))
            hardwareId = $"SystemUnit_{GetInstanceID()}";

        _slotManager = GetComponent<HardwareSlotManager>();
        _editLock = GetComponent<HardwareEditLock>();

        // NOTE: HardwareSlotManager.Start() also calls LoadAllSlotStates()
        // via Invoke. We do NOT call load here to avoid double-load.
    }

    public string GetHardwareId() => hardwareId;

    public HardwareStateData SaveState()
    {
        HardwareStateData stateData = new HardwareStateData("SystemUnit");

        if (_slotManager != null)
            _slotManager.SaveAllSlotStatesToStateData(stateData);

        Debug.Log($"[SystemUnitStateManager] Saved state for '{hardwareId}'");
        return stateData;
    }

    public void LoadState(HardwareStateData stateData)
    {
        // Loading is handled by HardwareSlotManager.LoadAllSlotStates()
        // which is already called from its own Start() via Invoke.
        // This method exists to satisfy the IHardwareState interface.
        Debug.Log($"[SystemUnitStateManager] LoadState called for '{hardwareId}'");
    }

    public void ClearState()
    {
        if (HardwareStateManager.Instance != null)
            HardwareStateManager.Instance.ClearHardwareState(hardwareId);
    }

    public bool IsEditable()
    {
        if (_editLock == null) return true;
        return !_editLock.IsAnyLocked();
    }
}