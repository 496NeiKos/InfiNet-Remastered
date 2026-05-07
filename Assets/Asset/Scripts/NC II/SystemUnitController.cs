using UnityEngine;

public class SystemUnitController : MonoBehaviour
{
    public GameObject snapshotGroup;
    public GameObject detailGroup;

    private Vector3 _detailOriginalLocalPos;
    private Quaternion _detailOriginalLocalRot;
    private Transform _detailOriginalParent;

    void Start()
    {
        ShowSnapshot();
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

    public void ShowDetailAtCenter()
    {
        snapshotGroup.SetActive(true);

        _detailOriginalParent = detailGroup.transform.parent;
        _detailOriginalLocalPos = detailGroup.transform.localPosition;
        _detailOriginalLocalRot = detailGroup.transform.localRotation;

        Vector3 center = Camera.main.ViewportToWorldPoint(
            new Vector3(0.5f, 0.5f, Mathf.Abs(Camera.main.transform.position.z))
        );
        center.z = 0f;

        detailGroup.transform.SetParent(null, true);
        detailGroup.transform.position = center;
        detailGroup.transform.rotation = Quaternion.identity;

        detailGroup.SetActive(true);
    }

    public void HideDetail()
    {
        detailGroup.SetActive(false);

        if (_detailOriginalParent != null)
            detailGroup.transform.SetParent(_detailOriginalParent, false);

        detailGroup.transform.localPosition = _detailOriginalLocalPos;
        detailGroup.transform.localRotation = _detailOriginalLocalRot;
    }
}