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
    public string LocalUserName = "User";

    [Tooltip("Password for the default local account. Also shown as the password hint.")]
    public string LocalUserPassword = "password";

    // ── Runtime state (reset each play session) ───────────────────────────────

    [NonSerialized] public bool   DomainJoined        = false;
    [NonSerialized] public string JoinedDomain        = "";
    [NonSerialized] public string CurrentLoggedInUser = "";
    [NonSerialized] public bool   FirstBootDone       = false;

    // ── Client-side navigation latches ────────────────────────────────────────

    [NonSerialized] public bool ClientLocalUserLoggedIn   = false; // local user signed in before domain join
    [NonSerialized] public bool ClientSettingsOpened      = false;
    [NonSerialized] public bool ClientSettingsAboutOpened = false;
    [NonSerialized] public bool ClientAdvancedSysOpened   = false; // Advanced System Settings dialog
    [NonSerialized] public bool ClientDomainChangeOpened  = false; // Computer Name/Domain Changes dialog
    [NonSerialized] public bool ClientDomainNameEntered   = false; // Domain radio toggled + name typed
    [NonSerialized] public bool ClientDomainCredsEntered  = false; // Server credentials entered
    [NonSerialized] public bool ClientOtherUserClicked    = false; // "Other Users" pressed at login screen
    [NonSerialized] public bool ClientDomainUserSelected  = false; // domain user selected at login
}
