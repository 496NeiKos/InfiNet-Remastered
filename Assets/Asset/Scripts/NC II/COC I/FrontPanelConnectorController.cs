using UnityEngine;

/// <summary>
/// On FrontPanelConnector_Port. Aggregates Phase 1 (front panel cable connected to port)
/// and Phase 2 (all 5 sub-pin cables connected to their ProperSubPorts) state.
/// Implements IDetachGate so the Phase 1 cable cannot be pulled while any sub-pin is seated.
/// Wire into the Phase 1 CableBehavior's detachGate inspector field.
/// </summary>
public class FrontPanelConnectorController : MonoBehaviour, IDetachGate
{
    [Tooltip("The 5 ProperSubPorts inside FrontPanelConnectorDetailed that are task-tracked. " +
             "Do NOT include LoosePorts here.")]
    [SerializeField] private CablePort[] phase2ProperSubPorts;

    private CablePort _phase1Port;

    public bool IsPhase1Installed => _phase1Port != null && _phase1Port.IsInstalled;

    public bool IsFullyInstalled
    {
        get
        {
            if (!IsPhase1Installed) return false;
            if (phase2ProperSubPorts == null || phase2ProperSubPorts.Length == 0) return false;
            foreach (var port in phase2ProperSubPorts)
                if (port == null || !port.IsInstalled) return false;
            return true;
        }
    }

    // IDetachGate — blocks Phase 1 cable detach while any ProperSubPort has a cable seated.
    public bool CanDetach()
    {
        if (phase2ProperSubPorts == null) return true;
        foreach (var port in phase2ProperSubPorts)
            if (port != null && port.IsInstalled) return false;
        return true;
    }

    public string BlockedReason => "disconnect the front panel pins first";

    private void Awake()
    {
        _phase1Port = GetComponent<CablePort>();
    }
}
