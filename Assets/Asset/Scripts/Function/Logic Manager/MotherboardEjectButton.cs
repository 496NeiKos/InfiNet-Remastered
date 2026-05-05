using UnityEngine;

public class MotherboardEjectButton : MonoBehaviour
{
    public SystemUnit systemUnit; // drag your SystemUnit GameObject here in Inspector

    public void OnClickEject()
    {
        Debug.Log("Eject button clicked!");
        if (systemUnit != null)
        {
            systemUnit.EjectMotherboard();
        }
    }
}
