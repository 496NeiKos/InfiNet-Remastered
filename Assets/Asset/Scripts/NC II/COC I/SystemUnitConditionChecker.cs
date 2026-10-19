using UnityEngine;

public class SystemUnitConditionChecker : MonoBehaviour
{
    [SerializeField] private PowerButton powerButton;
    [SerializeField] private CablePort hdmiPort;
    [SerializeField] private CablePort psuBackPort;

    public bool IsHardwareInteractable()
    {
        if (powerButton == null || hdmiPort == null || psuBackPort == null)
        {
            Debug.LogWarning("[SystemUnitConditionChecker] Missing references.");
            return false;
        }

        return !powerButton.IsPoweredOn
            && hdmiPort.IsUninstalled
            && psuBackPort.IsUninstalled;
    }
}