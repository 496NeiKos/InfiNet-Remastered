using UnityEngine;

public class SystemUnitManager : MonoBehaviour
{
    public Transform motherboardInstallPoint; // where motherboard should snap
    private DragItem installedMotherboard;

    public void InstallMotherboard(DragItem motherboard)
    {
        installedMotherboard = motherboard;
        motherboard.transform.SetParent(motherboardInstallPoint);
        motherboard.transform.position = motherboardInstallPoint.position;

        Debug.Log("Motherboard installed into System Unit.");
        TaskListManager.Instance.SetTaskCompleted(1, true);
    }

    public void RemoveMotherboard()
    {
        if (installedMotherboard != null)
        {
            Debug.Log("Motherboard removed from System Unit.");
            TaskListManager.Instance.SetTaskCompleted(1, false);
            installedMotherboard = null;
        }
    }
}
