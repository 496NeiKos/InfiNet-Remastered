using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// On GPUDetailed — manages switching between the top-view and side-view sub-panels.
/// Wires "SecondLayerTop" and "SecondLayerSide" buttons in SecondLayer when active.
/// These names are unique to SecondLayer and never collide with FirstLayer buttons
/// (FirstLayerTop/Front/Side/Back used by HardwareViewController).
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
        WireSecondLayerButtons();
        ShowView(_activeView != null ? _activeView : topView);
        ApplyHardwareInteractable();
    }

    private void OnDisable()
    {
        topView?.SetActive(false);
        sideView?.SetActive(false);
        HideSecondLayerButtons();
    }

    public void HideSubViews()
    {
        topView?.SetActive(false);
        sideView?.SetActive(false);
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

    private void WireSecondLayerButtons()
    {
        if (topView == null && sideView == null) return;

        GameObject panel = GameManager.Instance?.secondLayer;
        if (panel == null) return;

        WireButton(panel, "SecondLayerTop", topView);
        WireButton(panel, "SecondLayerSide", sideView);
    }

    private void HideSecondLayerButtons()
    {
        if (topView == null && sideView == null) return;

        GameObject panel = GameManager.Instance?.secondLayer;
        if (panel == null) return;

        HideButton(panel, "SecondLayerTop");
        HideButton(panel, "SecondLayerSide");
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
}
