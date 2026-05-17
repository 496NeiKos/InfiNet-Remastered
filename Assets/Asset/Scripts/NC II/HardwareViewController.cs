using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generic view controller for any hardware with multiple angles (Front, Side, Back).
/// Replaces SystemUnitViewController. Attach to hardware root.
/// Assign only the views that exist — null views are skipped.
/// </summary>
public class HardwareViewController : MonoBehaviour
{
    [Header("Views (assign only those that exist)")]
    [SerializeField] private GameObject frontView;
    [SerializeField] private GameObject sideView;
    [SerializeField] private GameObject backView;

    [Header("Default View")]
    [SerializeField] private ViewType defaultView = ViewType.Front;

    public enum ViewType { Front, Side, Back }

    private GameObject _activeView;

    private void Start()
    {
        frontView?.SetActive(false);
        sideView?.SetActive(false);
        backView?.SetActive(false);

        _activeView = GetViewObject(defaultView) ?? frontView ?? sideView ?? backView;
    }

    public void ShowLastActive()
    {
        ShowView(_activeView ?? GetViewObject(defaultView));
    }

    public void ShowView(GameObject view)
    {
        frontView?.SetActive(false);
        sideView?.SetActive(false);
        backView?.SetActive(false);

        if (view != null)
        {
            view.SetActive(true);
            _activeView = view;
            CenterView(view);
        }
    }

    public void WireButtons()
    {
        GameObject panel = GameManager.Instance?.editingPanel;
        if (panel == null) return;

        WireButton(panel, "FrontButton", frontView);
        WireButton(panel, "SideButton", sideView);
        WireButton(panel, "BackButton", backView);
    }

    public void HideButtons()
    {
        GameObject panel = GameManager.Instance?.editingPanel;
        if (panel == null) return;

        FindButton(panel, "FrontButton")?.gameObject.SetActive(false);
        FindButton(panel, "SideButton")?.gameObject.SetActive(false);
        FindButton(panel, "BackButton")?.gameObject.SetActive(false);
    }

    private void WireButton(GameObject panel, string buttonName, GameObject targetView)
    {
        if (targetView == null) return; // skip buttons for views that don't exist

        Button btn = FindButton(panel, buttonName);
        if (btn == null) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => ShowView(targetView));
        btn.gameObject.SetActive(true);
    }

    private void CenterView(GameObject view)
    {
        if (view == null || GameManager.Instance?.editingPanel == null) return;

        RectTransform rect = GameManager.Instance.editingPanel.GetComponent<RectTransform>();
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
            ViewType.Front => frontView,
            ViewType.Side => sideView,
            ViewType.Back => backView,
            _ => null
        };
    }
}