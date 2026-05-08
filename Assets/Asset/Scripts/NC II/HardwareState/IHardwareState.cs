/// <summary>
/// Interface for hardware device state management.
/// Any hardware device (SystemUnit, Motherboard, CPU, etc.) that needs
/// to save/load state implements this interface.
/// </summary>
public interface IHardwareState
{
    /// <summary>
    /// Unique identifier for this hardware instance.
    /// Used as the key in the save dictionary.
    /// Example: "SystemUnit_1", "HDD_0", etc.
    /// </summary>
    string GetHardwareId();

    /// <summary>
    /// Save the current state of this hardware to a serializable object.
    /// Called before the hardware is destroyed.
    /// Should include all slot states and child states.
    /// </summary>
    HardwareStateData SaveState();

    /// <summary>
    /// Restore the hardware to a previously saved state.
    /// Called after the hardware is instantiated.
    /// Should restore all slots and recursively restore children.
    /// </summary>
    void LoadState(HardwareStateData stateData);

    /// <summary>
    /// Clear any saved state for this hardware.
    /// Called when the user explicitly deletes/resets the hardware.
    /// </summary>
    void ClearState();

    /// <summary>
    /// Check if this hardware can currently be edited in the editing panel.
    /// Returns false if locked (installed in a parent) or nested in a locked parent.
    /// </summary>
    bool IsEditable();
}