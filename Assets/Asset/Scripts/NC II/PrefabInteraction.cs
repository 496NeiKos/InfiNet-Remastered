using UnityEngine;

public class PrefabInteraction : MonoBehaviour
{
    public GameObject editingPanel;       // UI panel prefab
    public GameObject detailedPrefab;     // detailed version of this hardware

    private GameObject activeDetail;

    void Update()
    {
        if (Input.GetMouseButtonDown(1)) // right click
        {
            if (IsMouseOver())
            {
                Debug.Log($"{name} → Right click detected, opening editor panel");
                OpenEditor();
            }
        }
    }

    private bool IsMouseOver()
    {
        // Raycast from mouse to check if this prefab was clicked
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        return hit.collider != null && hit.collider.gameObject == gameObject;
    }

    private void OpenEditor()
    {
        // Show editing panel
        editingPanel.SetActive(true);

        // Spawn detailed prefab inside the panel (magnified version)
        if (activeDetail == null)
        {
            activeDetail = Instantiate(detailedPrefab, editingPanel.transform);
        }
    }
}
