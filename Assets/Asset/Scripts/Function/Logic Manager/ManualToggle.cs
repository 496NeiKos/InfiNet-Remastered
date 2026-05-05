using UnityEngine;

public class ManualToggle : MonoBehaviour
{
    public GameObject UserManual;

    public void ToggleManual()
    {
        bool isActive = UserManual.activeSelf;
        UserManual.SetActive(!isActive);
    }
}
