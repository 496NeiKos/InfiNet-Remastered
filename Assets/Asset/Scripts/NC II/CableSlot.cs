using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On each MB Phase 1 cable slot (CableSlot1, CableSlot2, CableSlot3).
/// Slot is always visible at ~40% opacity — sprite never changes.
/// Cable child is the draggable object — hold 2s to detach.
/// IsInstalled() used by DragPrefab.AreAllCablesDetached().
/// </summary>
public class CableSlot : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string cableType = "Cable1";

    [Header("Install Gate (optional)")]
    [Tooltip("If assigned, this slot cannot accept a cable unless the referenced slot has a child installed (e.g. PSU side slot).")]
    [SerializeField] private Transform installPrerequisiteSlot;

    private bool _isInstalled = true; // cable starts installed by default

    public string GetCableType() => cableType;
    public bool CanAcceptCable(string type) => cableType == type;

    public bool IsPrerequisiteMet() =>
        installPrerequisiteSlot == null || installPrerequisiteSlot.childCount > 0;

    public bool IsInstalled() => _isInstalled;

    public void SetInstalled()
    {
        _isInstalled = true;
        GetComponentInParent<GPUController>()?.RefreshCableSprite();
        Debug.Log($"[CableSlot] {gameObject.name} → Installed");
    }

    public void SetUninstalled()
    {
        _isInstalled = false;
        GetComponentInParent<GPUController>()?.RefreshCableSprite();
        Debug.Log($"[CableSlot] {gameObject.name} → Uninstalled");
    }
}