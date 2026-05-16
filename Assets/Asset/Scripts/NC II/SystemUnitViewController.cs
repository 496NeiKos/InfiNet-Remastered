using UnityEngine;
using UnityEngine.UI;

public class SystemUnitViewController : MonoBehaviour
{
    [SerializeField] private GameObject frontView;
    [SerializeField] private GameObject sideView;
    [SerializeField] private GameObject backView;

    private GameObject _activeView;

    private void Start()
    {
        frontView?.SetActive(false);
        backView?.SetActive(false);
        _activeView = sideView;
    }

    public void ShowLastActive()
    {
        ShowView(_activeView ?? sideView);
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
        }

        CenterView(view);
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

    public void WireButtons()
    {
        GameObject panel = GameManager.Instance?.editingPanel;
        if (panel == null) return;

        Button frontBtn = FindButtonInPanel(panel, "FrontButton");
        Button sideBtn = FindButtonInPanel(panel, "SideButton");
        Button backBtn = FindButtonInPanel(panel, "BackButton");

        if (frontBtn != null)
        {
            frontBtn.onClick.RemoveAllListeners();
            frontBtn.onClick.AddListener(() => ShowView(frontView));
            frontBtn.gameObject.SetActive(true);
        }

        if (sideBtn != null)
        {
            sideBtn.onClick.RemoveAllListeners();
            sideBtn.onClick.AddListener(() => ShowView(sideView));
            sideBtn.gameObject.SetActive(true);
        }

        if (backBtn != null)
        {
            backBtn.onClick.RemoveAllListeners();
            backBtn.onClick.AddListener(() => ShowView(backView));
            backBtn.gameObject.SetActive(true);
        }
    }

    public void HideButtons()
    {
        GameObject panel = GameManager.Instance?.editingPanel;
        if (panel == null) return;

        FindButtonInPanel(panel, "FrontButton")?.gameObject.SetActive(false);
        FindButtonInPanel(panel, "SideButton")?.gameObject.SetActive(false);
        FindButtonInPanel(panel, "BackButton")?.gameObject.SetActive(false);
    }

    // GetComponentsInChildren finds buttons at any nesting depth, including inactive
    private Button FindButtonInPanel(GameObject panel, string buttonName)
    {
        Button[] allButtons = panel.GetComponentsInChildren<Button>(true);
        foreach (Button btn in allButtons)
        {
            if (btn.gameObject.name == buttonName)
                return btn;
        }
        Debug.LogWarning($"[SystemUnitViewController] Button '{buttonName}' not found in EditingPanel.");
        return null;
    }
}