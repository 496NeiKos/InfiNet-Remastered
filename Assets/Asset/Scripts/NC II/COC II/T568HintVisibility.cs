using UnityEngine;

/// <summary>
/// Attach to the first layer child of DetailPanel (the layer that enables/disables on detail view enter/exit).
/// Reference the hint TMP GameObject via hintObject — it can safely start disabled in the editor.
/// Reference utpCableDetailPanel to the UTP cable's specific detail panel so the hint is only shown
/// when that panel is active, not when other detail views (e.g. LAN tester) are open.
/// Controls the hint's SetActive state based on which task category is active:
/// shown only while Network Cable tasks are selected AND the UTP cable detail view is open.
/// </summary>
public class T568HintVisibility : MonoBehaviour
{
    [SerializeField] private NetworkCableTaskCategoryController categoryController;
    [SerializeField] private GameObject hintObject;
    [Tooltip("The UTP cable's detail panel root. Hint is hidden when any other detail view is open.")]
    [SerializeField] private GameObject utpCableDetailPanel;

    private void OnEnable()
    {
        NetworkCableTaskCategoryController.OnActiveCategoryUpdated += UpdateVisibility;
        UpdateVisibility();
    }

    private void OnDisable()
    {
        NetworkCableTaskCategoryController.OnActiveCategoryUpdated -= UpdateVisibility;
        if (hintObject != null) hintObject.SetActive(false);
    }

    private void UpdateVisibility()
    {
        if (hintObject == null) return;
        bool utpActive = utpCableDetailPanel == null || utpCableDetailPanel.activeInHierarchy;
        hintObject.SetActive(utpActive && categoryController != null && categoryController.IsNetworkCableActive);
    }
}
