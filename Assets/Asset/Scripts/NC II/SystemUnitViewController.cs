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
        // sideView activation is handled by SystemUnitController.ShowDetailAtCenter
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
    }

    public void WireButtons()
    {
        GameObject panel = GameManager.Instance?.editingPanel;
        if (panel == null) return;

        Button frontBtn = FindButtonByName(panel, "FrontButton");
        Button sideBtn = FindButtonByName(panel, "SideButton");
        Button backBtn = FindButtonByName(panel, "BackButton");

        if (frontBtn != null) { frontBtn.onClick.RemoveAllListeners(); frontBtn.onClick.AddListener(() => ShowView(frontView)); }
        if (sideBtn != null) { sideBtn.onClick.RemoveAllListeners(); sideBtn.onClick.AddListener(() => ShowView(sideView)); }
        if (backBtn != null) { backBtn.onClick.RemoveAllListeners(); backBtn.onClick.AddListener(() => ShowView(backView)); }
    }

    private Button FindButtonByName(GameObject root, string buttonName)
    {
        Transform t = root.transform.Find(buttonName);
        if (t == null)
        {
            Debug.LogWarning($"[SystemUnitViewController] Button '{buttonName}' not found in EditingPanel.");
            return null;
        }
        return t.GetComponent<Button>();
    }
}