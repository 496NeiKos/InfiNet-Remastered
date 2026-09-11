using System.Collections.Generic;

/// <summary>
/// Shared interface implemented by both VirtualOSManager (COC II) and
/// ServerVirtualOSManager (COC III). Allows IPv4PropertiesController and
/// NetworkSharingCenterController to work in either scene without
/// referencing a concrete manager type.
/// </summary>
public interface IVirtualOSManager
{
    DeviceOSState CurrentState  { get; }
    DeviceID      CurrentDevice { get; }

    bool          IsIPTopologySatisfied();
    string        GetIPBlockReason();
    string        GetNetworkPrefix();
    List<string>  GetUsedHostOctets();
}

/// <summary>
/// Scene-agnostic locator. Each OS manager sets Current in its Awake and
/// clears it in OnDestroy. All shared UI controllers reference this instead
/// of a concrete manager singleton.
/// </summary>
public static class VirtualOSManagerLocator
{
    public static IVirtualOSManager Current { get; set; }
}
