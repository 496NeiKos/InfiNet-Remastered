using UnityEngine;

public class SystemUnitConditionChecker : MonoBehaviour
{
    [SerializeField] private PowerButton powerButton;
    [SerializeField] private SlotContainer vgaPort;
    [SerializeField] private SlotContainer psuBackPort;

    public bool IsHardwareInteractable()
    {
        if (powerButton == null || vgaPort == null || psuBackPort == null)
        {
            Debug.LogWarning("[SystemUnitConditionChecker] Missing references.");
            return false;
        }

        return !powerButton.IsPoweredOn
            && vgaPort.IsSlotEmpty()
            && psuBackPort.IsSlotEmpty();
    }
}