/// <summary>
/// Interface for hardware device state management.
/// Any hardware device (SystemUnit, GPU, RAM, etc.) that needs
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
    /// </summary>
    HardwareStateData SaveState();

    /// <summary>
    /// Restore the hardware to a previously saved state.
    /// Called after the hardware is instantiated.
    /// </summary>
    void LoadState(HardwareStateData stateData);

    /// <summary>
    /// Clear any saved state for this hardware.
    /// Called when the user explicitly deletes/resets the hardware.
    /// </summary>
    void ClearState();
}