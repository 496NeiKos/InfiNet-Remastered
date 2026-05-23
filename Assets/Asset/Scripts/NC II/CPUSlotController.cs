using UnityEngine;

public class CPUSlotController : MonoBehaviour
{
    public enum SlotState
    {
        BothInstalled,
        HeatsinkUninstalled,
        CPUUninstalled,
        BothUninstalled
    }

    [Header("References")]
    [SerializeField] private GameObject cpu;
    [SerializeField] private GameObject heatsink;
    [SerializeField] private CPULockController cpuLock;

    private SlotState _state = SlotState.BothInstalled;
    public SlotState State => _state;

    public bool IsCPUInstalled => _state == SlotState.BothInstalled || _state == SlotState.HeatsinkUninstalled;
    public bool IsHeatsinkInstalled => _state == SlotState.BothInstalled || _state == SlotState.CPUUninstalled;
    public bool IsLockClosed => cpuLock != null && cpuLock.IsLocked;

    private void Awake()
    {
        ApplyState();
    }

    // Called when Heatsink is removed — derives new state from current
    public void OnHeatsinkUninstalled()
    {
        switch (_state)
        {
            case SlotState.BothInstalled: SetState(SlotState.HeatsinkUninstalled); break;
            case SlotState.CPUUninstalled: SetState(SlotState.BothUninstalled); break;
            default: Debug.LogWarning($"[CPUSlotController] OnHeatsinkUninstalled from unexpected state: {_state}"); break;
        }
    }

    // Called when CPU is removed — derives new state from current
    public void OnCPUUninstalled()
    {
        switch (_state)
        {
            case SlotState.HeatsinkUninstalled: SetState(SlotState.BothUninstalled); break;
            case SlotState.BothInstalled:
                Debug.LogWarning("[CPUSlotController] CPU removed while heatsink installed — forcing CPUUninstalled.");
                SetState(SlotState.CPUUninstalled);
                break;
            default: Debug.LogWarning($"[CPUSlotController] OnCPUUninstalled from unexpected state: {_state}"); break;
        }
    }

    // Called when Heatsink is installed back
    public void OnHeatsinkInstalled()
    {
        switch (_state)
        {
            case SlotState.HeatsinkUninstalled: SetState(SlotState.BothInstalled); break;
            case SlotState.BothUninstalled: SetState(SlotState.CPUUninstalled); break;
            default: Debug.LogWarning($"[CPUSlotController] OnHeatsinkInstalled from unexpected state: {_state}"); break;
        }
    }

    // Called when CPU is installed back
    public void OnCPUInstalled()
    {
        switch (_state)
        {
            case SlotState.CPUUninstalled: SetState(SlotState.BothInstalled); break;
            case SlotState.BothUninstalled: SetState(SlotState.HeatsinkUninstalled); break;
            default: Debug.LogWarning($"[CPUSlotController] OnCPUInstalled from unexpected state: {_state}"); break;
        }
    }

    // Force-reset state — call this if state gets out of sync
    public void ForceState(SlotState state) => SetState(state);

    private void SetState(SlotState newState)
    {
        _state = newState;
        ApplyState();
        Debug.Log($"[CPUSlotController] State → {_state}");
    }

    private void ApplyState()
    {
        if (cpu == null || heatsink == null) return;

        Collider2D cpuCol = cpu.GetComponent<Collider2D>();

        switch (_state)
        {
            case SlotState.BothInstalled:
                if (cpuCol != null) cpuCol.enabled = false;
                SetInteractable(cpu, false);
                SetInteractable(heatsink, true);
                break;

            case SlotState.HeatsinkUninstalled:
                if (cpuCol != null) cpuCol.enabled = true;
                SetInteractable(cpu, true);
                break;

            case SlotState.CPUUninstalled:
                if (cpuCol != null) cpuCol.enabled = false;
                SetInteractable(cpu, false);
                SetInteractable(heatsink, true);
                break;

            case SlotState.BothUninstalled:
                if (cpuCol != null) cpuCol.enabled = false;
                SetInteractable(cpu, false);
                SetInteractable(heatsink, false);
                break;
        }
    }

    private void SetInteractable(GameObject obj, bool interactable)
    {
        if (obj == null) return;
        DragPrefab dp = obj.GetComponent<DragPrefab>();
        if (dp != null) dp.enabled = interactable;
    }

    public bool HasCPU() => IsCPUInstalled;
    public bool HasHeatsink() => IsHeatsinkInstalled;
}