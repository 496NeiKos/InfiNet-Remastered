using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// On GPUDetailed — manages switching between the top-view and side-view sub-panels.
/// Wires Top/Side buttons in whichever layer the GPU currently lives in (SecondLayer in Phase 2,
/// ThirdLayer in Phase 1). Button names follow the layer prefix convention: SecondLayerTop,
/// ThirdLayerSide, etc.
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
        WireButtons();
        ShowView(_activeView != null ? _activeView : topView);
        ApplyHardwareInteractable();
    }

    private void OnDisable()
    {
        topView?.SetActive(false);
        sideView?.SetActive(false);
        HideButtons();
    }

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

    private void WireButtons()
    {
        if (topView == null && sideView == null) return;
        GameObject panel = FindCurrentPanel();
        if (panel == null) return;
        string prefix = GetPanelPrefix(panel);
        WireButton(panel, prefix + "Top", topView);
        WireButton(panel, prefix + "Side", sideView);
    }

    private void HideButtons()
    {
        if (topView == null && sideView == null) return;
        // Hide in both possible panels since we may not know which was used.
        HideButtonsInPanel(GameManager.Instance?.secondLayer, "SecondLayer");
        HideButtonsInPanel(GameManager.Instance?.thirdLayer, "ThirdLayer");
    }

    private void HideButtonsInPanel(GameObject panel, string prefix)
    {
        if (panel == null) return;
        HideButton(panel, prefix + "Top");
        HideButton(panel, prefix + "Side");
    }

    private GameObject FindCurrentPanel()
    {
        if (GameManager.Instance == null) return null;
        GameObject sl = GameManager.Instance.secondLayer;
        GameObject tl = GameManager.Instance.thirdLayer;
        if (sl != null && transform.IsChildOf(sl.transform)) return sl;
        if (tl != null && transform.IsChildOf(tl.transform)) return tl;
        return null;
    }

    private string GetPanelPrefix(GameObject panel)
    {
        if (panel == GameManager.Instance?.secondLayer) return "SecondLayer";
        if (panel == GameManager.Instance?.thirdLayer) return "ThirdLayer";
        return "SecondLayer";
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
