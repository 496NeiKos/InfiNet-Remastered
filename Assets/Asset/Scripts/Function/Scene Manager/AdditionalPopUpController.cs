using UnityEngine;
using UnityEngine.UI;

public class AdditionalPopUpController : MonoBehaviour
{
    [SerializeField] private GameObject additionalPopUp;
    [SerializeField] private RectTransform bodyPanel;

    private GameObject _overlay;

    private void Start()
    {
        CreateOverlay();
    }

    private void CreateOverlay()
    {
        _overlay = new GameObject("PopUpOverlay");
        _overlay.transform.SetParent(additionalPopUp.transform, false);

        RectTransform rt = _overlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = _overlay.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);

        Button btn = _overlay.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => additionalPopUp.SetActive(false));

        // Overlay must be behind body panel
        _overlay.transform.SetAsFirstSibling();
    }

    public void TogglePopUp()
    {
        additionalPopUp.SetActive(!additionalPopUp.activeSelf);
    }
}
