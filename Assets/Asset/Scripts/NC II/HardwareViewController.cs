using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generic view controller for any hardware with multiple angles (Top, Front, Side, Back).
/// Attach to hardware root. Assign only the views that exist — null views are skipped.
/// Wires FirstLayer buttons: FirstLayerTop, FirstLayerFront, FirstLayerSide, FirstLayerBack.
/// </summary>
public class HardwareViewController : MonoBehaviour
{
    [Header("Views (assign only those that exist)")]
    [SerializeField] private GameObject topView;
    [SerializeField] private GameObject frontView;
    [SerializeField] private GameObject sideView;
    [SerializeField] private GameObject backView;

    [Header("Default View")]
    [SerializeField] private ViewType defaultView = ViewType.Front;

    public enum ViewType { Top, Front, Side, Back }

    private GameObject _activeView;

    private void Start()
    {
        if (topView != null) topView.SetActive(false);
        if (frontView != null) frontView.SetActive(false);
        if (sideView != null) sideView.SetActive(false);
        if (backView != null) backView.SetActive(false);

        _activeView = GetViewObject(defaultView) ?? topView ?? frontView ?? sideView ?? backView;
    }

    public void ShowLastActive()
    {
        ShowView(_activeView ?? GetViewObject(defaultView));
    }

    // Sets _activeView to the supplied view only when no view has been chosen yet
    // (first open). Call this before ShowLastActive so there is always a default.
    public void SetDefaultIfNone(GameObject defaultFirstView)
    {
        if (_activeView == null)
            _activeView = defaultFirstView;
    }

    public void ShowView(GameObject view)
    {
        if (topView != null) topView.SetActive(false);
        if (frontView != null) frontView.SetActive(false);
        if (sideView != null) sideView.SetActive(false);
        if (backView != null) backView.SetActive(false);

        if (view != null)
        {
            view.SetActive(true);
            _activeView = view;
            CenterView(view);
        }
    }

    public void WireButtons()
    {
        GameObject panel = GameManager.Instance?.firstLayer;
        if (panel == null) return;

        WireButton(panel, "FirstLayerTop", topView);
        WireButton(panel, "FirstLayerFront", frontView);
        WireButton(panel, "FirstLayerSide", sideView);
        WireButton(panel, "FirstLayerBack", backView);
    }

    public void HideButtons()
    {
        GameObject panel = GameManager.Instance?.firstLayer;
        if (panel == null) return;

        FindButton(panel, "FirstLayerTop")?.gameObject.SetActive(false);
        FindButton(panel, "FirstLayerFront")?.gameObject.SetActive(false);
        FindButton(panel, "FirstLayerSide")?.gameObject.SetActive(false);
        FindButton(panel, "FirstLayerBack")?.gameObject.SetActive(false);
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

    private void CenterView(GameObject view)
    {
        if (view == null || GameManager.Instance?.firstLayer == null) return;

        RectTransform rect = GameManager.Instance.firstLayer.GetComponent<RectTransform>();
        if (rect == null) return;

        Vector3 center = rect.TransformPoint(new Vector3(rect.rect.center.x, rect.rect.center.y, 0f));
        center.z = 0f;
        view.transform.position = center;
    }

    private Button FindButton(GameObject panel, string buttonName)
    {
        foreach (Button btn in panel.GetComponentsInChildren<Button>(true))
            if (btn.gameObject.name == buttonName) return btn;
        return null;
    }

    private GameObject GetViewObject(ViewType type)
    {
        return type switch
        {
            ViewType.Top => topView,
            ViewType.Front => frontView,
            ViewType.Side => sideView,
            ViewType.Back => backView,
            _ => null
        };
    }
}
