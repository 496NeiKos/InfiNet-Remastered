using UnityEngine;

/// <summary>
/// Dedicated slot controller for CPUSlot.
/// Manages both CPU and Heatsink as siblings under the same slot.
/// Replaces SlotContainer on CPUSlot.
/// </summary>
public class CPUSlotController : MonoBehaviour
{
    public enum SlotState
    {
        BothInstalled,      // Default: CPU + Heatsink both present
        HeatsinkUninstalled, // Heatsink removed, CPU exposed and interactable
        CPUUninstalled,      // CPU removed (heatsink must already be uninstalled)
        BothUninstalled      // Both removed
    }

    [Header("References")]
    [SerializeField] private GameObject cpu;
    [SerializeField] private GameObject heatsink;

    private SlotState _state = SlotState.BothInstalled;
    public SlotState State => _state;

    // Convenience checks used by DragPrefab and MotherboardDetailViewManager
    public bool IsCPUInstalled => _state == SlotState.BothInstalled || _state == SlotState.HeatsinkUninstalled;
    public bool IsHeatsinkInstalled => _state == SlotState.BothInstalled || _state == SlotState.CPUUninstalled;

    private void Awake()
    {
        // Both are children of this slot at start by default
        ApplyState();
    }

    // Called by DragPrefab.OnEndDrag when Heatsink is sent to hardware area
    public void OnHeatsinkUninstalled()
    {
        if (_state == SlotState.BothInstalled)
            SetState(SlotState.HeatsinkUninstalled);
        else if (_state == SlotState.CPUUninstalled)
            SetState(SlotState.BothUninstalled);
    }

    // Called by DragPrefab.OnEndDrag when CPU is sent to hardware area
    public void OnCPUUninstalled()
    {
        if (_state == SlotState.HeatsinkUninstalled)
            SetState(SlotState.CPUUninstalled);
        else if (_state == SlotState.BothInstalled)
        {
            // Should not happen — CPU is blocked while heatsink installed
            Debug.LogWarning("[CPUSlotController] CPU uninstalled while heatsink still installed — state mismatch.");
            SetState(SlotState.CPUUninstalled);
        }
    }

    // Called by HardwareHolder when Heatsink is dropped back onto CPUSlot
    public void OnHeatsinkInstalled()
    {
        if (_state == SlotState.HeatsinkUninstalled)
            SetState(SlotState.BothInstalled);
        else if (_state == SlotState.BothUninstalled)
            SetState(SlotState.CPUUninstalled);
    }

    // Called by HardwareHolder when CPU is dropped back onto CPUSlot
    public void OnCPUInstalled()
    {
        if (_state == SlotState.CPUUninstalled)
            SetState(SlotState.HeatsinkUninstalled);
        else if (_state == SlotState.BothUninstalled)
            SetState(SlotState.CPUUninstalled);
    }

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
                // Heatsink on top — disable CPU collider and interaction
                if (cpuCol != null) cpuCol.enabled = false;
                SetInteractable(cpu, false);
                SetInteractable(heatsink, true);
                break;

            case SlotState.HeatsinkUninstalled:
                // Heatsink removed — enable CPU collider and interaction
                if (cpuCol != null) cpuCol.enabled = true;
                SetInteractable(cpu, true);
                // Do NOT call heatsink.SetActive(false) — HardwareHolder handles visibility
                break;

            case SlotState.CPUUninstalled:
                // CPU removed — heatsink still interactable if present
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

    // Used by PowerOnConditionChecker via Transform.childCount check —
    // return whether CPU slot has CPU installed for the power-on condition
    public bool HasCPU() => IsCPUInstalled;
    public bool HasHeatsink() => IsHeatsinkInstalled;
}