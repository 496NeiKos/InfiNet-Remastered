using UnityEngine;
using UnityEngine.UI;

public class MainSettingPanelController : MonoBehaviour
{
    [SerializeField] private GameObject mainSettingPanel;

    [Header("Buttons that close the panel when clicked")]
    [Tooltip("Guide toggle button")]
    [SerializeField] private Button guideToggleButton;

    [Tooltip("Inventory toggle button")]
    [SerializeField] private Button inventoryToggleButton;

    private void OnEnable()
    {
        TopicManager.OnDropdownClosed += ClosePanel;
    }

    private void OnDisable()
    {
        TopicManager.OnDropdownClosed -= ClosePanel;
    }

    private void Start()
    {
        RegisterClose(guideToggleButton);
        RegisterClose(inventoryToggleButton);
    }

    private void RegisterClose(Button btn)
    {
        if (btn == null) return;
        btn.onClick.AddListener(ClosePanel);
    }

    public void OpenPanel()
    {
        mainSettingPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        mainSettingPanel.SetActive(false);
    }
}
