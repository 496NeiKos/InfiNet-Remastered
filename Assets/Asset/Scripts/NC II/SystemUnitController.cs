using UnityEngine;

public class SystemUnitController : MonoBehaviour
{
    public GameObject snapshotGroup;
    public GameObject detailGroup;

    void Start()
    {
        ShowSnapshot(); // default view
    }

    public void ShowSnapshot()
    {
        snapshotGroup.SetActive(true);
        detailGroup.SetActive(false);
    }

    public void ShowDetail()
    {
        snapshotGroup.SetActive(false);
        detailGroup.SetActive(true);
    }
}
