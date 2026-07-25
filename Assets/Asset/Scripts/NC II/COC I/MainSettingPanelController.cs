using UnityEngine;
using UnityEngine.UI;

public class MainSettingPanelController : MonoBehaviour
{
    public static MainSettingPanelController Instance { get; private set; }

    [SerializeField] private GameObject mainSettingPanel;

    [Header("Buttons that close the panel when clicked")]
    [Tooltip("Guide toggle button")]
    [SerializeField] private Button guideToggleButton;

    [Tooltip("Inventory toggle button")]
    [SerializeField] private Button inventoryToggleButton;

    private void Awake()
    {
        Instance = this;
    }

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

        if (inventoryToggleButton != null)
            inventoryToggleButton.onClick.AddListener(() =>
                WalkthroughGuideManager.Instance?.TryTrigger(WalkthroughGuideManager.WalkthroughTrigger.InventoryToggle));
    }

    private void RegisterClose(Button btn)
    {
        if (btn == null) return;
        btn.onClick.AddListener(ClosePanel);
    }

    public bool IsOpen => mainSettingPanel != null && mainSettingPanel.activeSelf;

    public void OpenPanel()
    {
        mainSettingPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        mainSettingPanel.SetActive(false);
    }
}
