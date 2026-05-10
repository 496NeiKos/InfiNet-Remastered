using UnityEngine;

/// <summary>
/// Represents one installation slot in a parent hardware's DetailGroup.
/// On Start(), auto-detects any existing child already in the hierarchy.
/// </summary>
public class SlotContainer : MonoBehaviour
{
    [Tooltip("Type of hardware this slot accepts (e.g., 'Motherboard', 'HDD', 'PSU')")]
    [SerializeField] private string slotType = "Unknown";

    private GameObject installedChild = null;
    private string installedPrefabName = "";

    private void Awake()
    {
        // Auto-detect any existing child already placed in this slot by the prefab
        // This is critical because default prefab has children already in slots
        if (transform.childCount > 0)
        {
            installedChild = transform.GetChild(0).gameObject;
            installedPrefabName = slotType; // assume existing child matches slot type
            Debug.Log($"[SlotContainer] Auto-detected existing child '{installedChild.name}' in slot '{slotType}'");
        }
    }

    public string GetSlotType() => slotType;
    public bool IsSlotEmpty() => installedChild == null;
    public GameObject GetInstalledChild() => installedChild;
    public string GetInstalledPrefabName() => installedPrefabName;
    public bool HasInstalledChild() => !IsSlotEmpty();

    public bool CanAcceptPrefab(string prefabName)
    {
        return slotType == prefabName;
    }

    public void InstallChild(GameObject childInstance, string prefabName)
    {
        if (childInstance == null)
        {
            Debug.LogError($"[SlotContainer] Cannot install null child in slot '{slotType}'");
            return;
        }

        installedChild = childInstance;
        installedPrefabName = prefabName;

        childInstance.transform.SetParent(transform, false);
        childInstance.transform.localPosition = Vector3.zero;

        Debug.Log($"[SlotContainer] Installed '{prefabName}' in slot '{slotType}'");
    }

    /// <summary>
    /// Removes the child reference from this slot but does NOT destroy it.
    /// Caller is responsible for destroying.
    /// </summary>
    public GameObject RemoveChild()
    {
        if (installedChild == null)
            return null;

        GameObject removed = installedChild;
        installedChild = null;
        installedPrefabName = "";

        removed.transform.SetParent(null, false);

        Debug.Log($"[SlotContainer] Removed child from slot '{slotType}'");
        return removed;
    }

    /// <summary>
    /// Removes AND destroys the child immediately.
    /// </summary>
    public void DestroyChild()
    {
        if (installedChild == null)
            return;

        GameObject toDestroy = installedChild;
        installedChild = null;
        installedPrefabName = "";

        Debug.Log($"[SlotContainer] Destroying child in slot '{slotType}'");
        Object.Destroy(toDestroy);
    }

    public string GetSlotState() => installedPrefabName;
}