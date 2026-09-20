using System;
using UnityEngine;

/// <summary>
/// Holds runtime + inspector-configurable state for the Client PC context.
/// Attach as a [SerializeField] on ServerVirtualOSManager so defaults
/// are editable in the Inspector. NonSerialized fields reset each play session.
/// </summary>
[Serializable]
public class ClientPCData
{
    [Tooltip("Display name shown for this PC in Change PC UI and login screen.")]
    public string ComputerName = "Client_PC-001";

    [Tooltip("Display name of the default local account on the client PC.")]
    public string LocalUserName = "Administrator";

    [Tooltip("Password for the default local account. Also shown as the password hint.")]
    public string LocalUserPassword = "password";

    // ── Runtime state (reset each play session) ───────────────────────────────

    [NonSerialized] public bool   DomainJoined        = false;
    [NonSerialized] public string JoinedDomain        = "";
    [NonSerialized] public string CurrentLoggedInUser = "";
    [NonSerialized] public bool   FirstBootDone       = false;
}
