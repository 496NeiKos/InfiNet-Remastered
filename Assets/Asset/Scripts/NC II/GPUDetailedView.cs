using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// On GPUDetailed — manages switching between the top-view and side-view sub-panels.
/// Wires "TopViewButton" and "SideViewButton" in the InnerEditingPanel when active.
/// These names are intentionally different from HardwareViewController's "FrontButton"/"SideButton"
/// to avoid any conflict with the main editing panel's view buttons.
/// ApplyHardwareInteractable() is public so GPULatchSideView can call it after latch changes.
/// </summary>
public class GPUDetailedView : MonoBehaviour
{
    [Header("Sub-Views")]
    [SerializeField] private GameObject topView;
    [SerializeField] private GameObject sideView;

    private GPUController _gpuController;
    private GameObject _activeView;

    private void Awake()
    {
        _gpuController = GetComponentInParent<GPUController>();
    }

    private void OnEnable()
    {
        WireInnerPanelButtons();
        ShowView(_activeView != null ? _activeView : topView);
        ApplyHardwareInteractable();
    }

    private void OnDisable()
    {
        topView?.SetActive(false);
        sideView?.SetActive(false);
        HideInnerPanelButtons();
    }

    /// <summary>Gates screws based on latch state. Call after any latch state change.</summary>
    public void ApplyHardwareInteractable()
    {
        if (_gpuController == null) return;
        bool interactable = _gpuController.IsLatched;

        foreach (var sc in _gpuController.GetComponentsInChildren<ScrewController>(true))
        {
            sc.enabled = interactable;
            foreach (Collider2D col in sc.GetComponents<Collider2D>())
                col.enabled = interactable;
        }
    }

    private void ShowView(GameObject view)
    {
        topView?.SetActive(false);
        sideView?.SetActive(false);
        if (view != null)
        {
            view.SetActive(true);
            _activeView = view;
        }
    }

    private void WireInnerPanelButtons()
    {
        if (topView == null && sideView == null) return;

        GameObject panel = FindInnerEditingPanel();
        if (panel == null) return;

        WireButton(panel, "TopViewButton", topView);
        WireButton(panel, "SideViewButton", sideView);
    }

    private void HideInnerPanelButtons()
    {
        if (topView == null && sideView == null) return;

        GameObject panel = FindInnerEditingPanel();
        if (panel == null) return;

        HideButton(panel, "TopViewButton");
        HideButton(panel, "SideViewButton");
    }

    private void WireButton(GameObject panel, string buttonName, GameObject targetView)
    {
        if (targetView == null) return;
        Button btn = FindButton(panel, buttonName);
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => ShowView(targetView));
        btn.gameObject.SetActive(true);
    }

    private void HideButton(GameObject panel, string buttonName)
    {
        FindButton(panel, buttonName)?.gameObject.SetActive(false);
    }

    private Button FindButton(GameObject panel, string buttonName)
    {
        foreach (Button btn in panel.GetComponentsInChildren<Button>(true))
            if (btn.gameObject.name == buttonName) return btn;
        return null;
    }

    private GameObject FindInnerEditingPanel()
    {
        if (GameManager.Instance?.editingPanel == null) return null;
        foreach (Transform t in GameManager.Instance.editingPanel.GetComponentsInChildren<Transform>(true))
            if (t.name == "InnerEditingPanel") return t.gameObject;
        return null;
    }
}
