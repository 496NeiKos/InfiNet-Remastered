using UnityEngine;

/// <summary>
/// Attach to the first layer child of DetailPanel (the layer that enables/disables on detail view enter/exit).
/// Reference the hint TMP GameObject via hintObject — it can safely start disabled in the editor.
/// Controls the hint's SetActive state based on which task category is active:
/// shown only while Network Cable tasks are selected.
/// </summary>
public class T568HintVisibility : MonoBehaviour
{
    [SerializeField] private NetworkCableTaskCategoryController categoryController;
    [SerializeField] private GameObject hintObject;

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
        hintObject.SetActive(categoryController != null && categoryController.IsNetworkCableActive);
    }
}
