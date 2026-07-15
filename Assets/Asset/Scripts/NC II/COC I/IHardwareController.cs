/// <summary>
/// Interface implemented by all hardware root controllers
/// (SystemUnitController, MonitorController, AVRController, etc.)
/// Allows PrefabInteraction to call show/hide without knowing the specific type.
/// </summary>
public interface IHardwareController
{
    void ShowDetailAtCenter();
    void HideDetail();
}