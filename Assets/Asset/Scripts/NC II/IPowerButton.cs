/// <summary>
/// Interface implemented by PowerButton and AVRPowerButton.
/// Allows BackCable to check power state without knowing the specific type.
/// </summary>
public interface IPowerButton
{
    bool IsPoweredOn { get; }
}