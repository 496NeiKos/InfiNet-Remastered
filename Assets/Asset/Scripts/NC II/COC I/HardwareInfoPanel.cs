using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HardwareInfoPanel : MonoBehaviour
{
    public static HardwareInfoPanel Instance { get; private set; }

    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        Instance = this;
        backButton.onClick.AddListener(Hide);
        gameObject.SetActive(false);
    }

    public void Show(Sprite image, string itemName, string description)
    {
        itemImage.sprite = image;
        itemNameText.text = itemName;
        descriptionText.text = description;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
