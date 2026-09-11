using System.Collections;
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

    [Header("Scene Controls")]
    [SerializeField] private Button resetButton;
    [SerializeField] private Button exitButton;

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

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetScene);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitToLessonSelection);
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

    private void ResetScene()
    {
        ClosePanel();
        if (GameManager.Instance != null && GameManager.Instance.IsEditorOpen)
            GameManager.Instance.CloseEditor();
        SceneController.Instance?.ReloadScene();
    }

    private void ExitToLessonSelection()
    {
        if (SoundManager.instance != null && SoundManager.instance.backSFX != null)
        {
            SoundManager.instance.PlaySFX(SoundManager.instance.backSFX);
            StartCoroutine(LoadAfterDelay("LessonSelection", SoundManager.instance.backSFX.length));
        }
        else
        {
            SceneController.Instance?.LoadScene("LessonSelection");
        }
    }

    private IEnumerator LoadAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneController.Instance?.LoadScene(sceneName);
    }
}
