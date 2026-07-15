using UnityEngine;

/// <summary>
/// Prevents a hardware prefab from being edited if it's installed in a parent.
/// Example: If Motherboard is in SystemUnit's MotherboardSlot, Motherboard cannot be edited
/// until it's removed from that slot.
///
/// Attach to every hardware prefab root (SystemUnit, Motherboard, CPU, etc.).
/// </summary>
public class HardwareEditLock : MonoBehaviour
{
    /// <summary>
    /// True if this prefab is currently installed in a parent's slot.
    /// </summary>
    private bool _isLocked = false;

    /// <summary>
    /// Reference to the parent's SlotContainer if locked.
    /// Used to check if we're still in that slot.
    /// </summary>
    private SlotContainer _parentSlot = null;

    private void Start()
    {
        // On spawn, check if we're inside a slot (already installed)
        UpdateLockStatus();
    }

    // ── Lock Status ────────────────────────────────────────────────────────

    /// <summary>
    /// Check if this prefab is currently locked (cannot be edited).
    /// </summary>
    public bool IsLocked() => _isLocked;

    /// <summary>
    /// Lock this prefab (prevent editing).
    /// Called by HardwareSlotManager when installed.
    /// </summary>
    public void Lock()
    {
        _isLocked = true;
        Debug.Log($"[HardwareEditLock] {name} is now LOCKED (installed in parent)");
    }

    /// <summary>
    /// Unlock this prefab (allow editing).
    /// Called by HardwareSlotManager when removed.
    /// </summary>
    public void Unlock()
    {
        _isLocked = false;
        _parentSlot = null;
        Debug.Log($"[HardwareEditLock] {name} is now UNLOCKED (can be edited)");
    }

    // ── Lock Validation ────────────────────────────────────────────────────

    /// <summary>
    /// Walk up the parent chain and check if this prefab is installed in any parent.
    /// Called at Start() or when we need to refresh lock status.
    /// </summary>
    public void UpdateLockStatus()
    {
        // Walk up hierarchy looking for a SlotContainer parent
        Transform current = transform.parent;
        while (current != null)
        {
            SlotContainer slot = current.GetComponent<SlotContainer>();
            if (slot != null && slot.GetInstalledChild() == gameObject)
            {
                // We're installed in this slot
                _isLocked = true;
                _parentSlot = slot;
                Debug.Log($"[HardwareEditLock] {name} is installed in slot {slot.GetSlotType()}");
                return;
            }

            current = current.parent;
        }

        // Not installed in any parent
        _isLocked = false;
        _parentSlot = null;
    }

    /// <summary>
    /// Check if this prefab is locked due to being nested in another locked prefab.
    /// Example: CPU is locked if its Motherboard parent is locked in SystemUnit.
    /// </summary>
    public bool IsLockedByParent()
    {
        // Check if any parent is locked
        Transform current = transform.parent;
        while (current != null)
        {
            HardwareEditLock parentLock = current.GetComponent<HardwareEditLock>();
            if (parentLock != null && parentLock.IsLocked())
            {
                return true; // Parent is locked, so we're indirectly locked
            }

            current = current.parent;
        }

        return false;
    }

    /// <summary>
    /// Get the combined lock status: direct lock OR parent lock.
    /// Use this for UI feedback: is this editable?
    /// </summary>
    public bool IsAnyLocked() => _isLocked || IsLockedByParent();
}