using UnityEngine;

public class SlotContainer : MonoBehaviour
{
    [SerializeField] private string slotType = "Unknown";
    [SerializeField] private Vector3 installLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 installLocalScale = Vector3.one;

    private GameObject installedChild = null;
    private string installedPrefabName = "";

    private void Awake()
    {
        if (transform.childCount > 0)
        {
            installedChild = transform.GetChild(0).gameObject;
            installedPrefabName = slotType;
        }
    }

    public string GetSlotType() => slotType;
    public bool IsSlotEmpty() => installedChild == null;
    public GameObject GetInstalledChild() => installedChild;
    public string GetInstalledPrefabName() => installedPrefabName;
    public bool HasInstalledChild() => !IsSlotEmpty();

    public bool CanAcceptPrefab(string prefabName)
    {
        // RAM slots accept any RAM stick (any name starting with "RAM")
        if (slotType == "RAM")
            return prefabName.StartsWith("RAM");

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
        childInstance.transform.localPosition = installLocalPosition;
        childInstance.transform.localScale = installLocalScale;
    }

    public GameObject RemoveChild()
    {
        if (installedChild == null) return null;

        GameObject removed = installedChild;
        installedChild = null;
        installedPrefabName = "";

        removed.transform.SetParent(null, false);
        return removed;
    }

    public void DestroyChild()
    {
        if (installedChild == null) return;

        GameObject toDestroy = installedChild;
        installedChild = null;
        installedPrefabName = "";

        Object.Destroy(toDestroy);
    }

    public string GetSlotState() => installedPrefabName;
}