using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HardwareInfoPanel : MonoBehaviour
{
    private static HardwareInfoPanel _instance;
    public static HardwareInfoPanel Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<HardwareInfoPanel>(FindObjectsInactive.Include);
            return _instance;
        }
        private set => _instance = value;
    }

    [SerializeField] private Image            itemImage;
    [SerializeField] private TextMeshProUGUI  itemNameText;
    [SerializeField] private TextMeshProUGUI  descriptionText;
    [SerializeField] private Button           backButton;

    [Header("Image Carousel")]
    [SerializeField] private Button           prevImageButton;
    [SerializeField] private Button           nextImageButton;
    [SerializeField] private TextMeshProUGUI  imagePageLabel;

    private Sprite[] _images;
    private int      _currentImageIdx;
    private bool     _listenersSetup;

    private void Awake()
    {
        Instance = this;
        // If Show() already called SetupListeners before triggering this Awake
        // (disabled-at-start path), skip the SetActive(false) so we don't hide
        // a panel that's currently being shown.
        bool firstInit = !_listenersSetup;
        SetupListeners();
        if (firstInit) gameObject.SetActive(false);
    }

    private void SetupListeners()
    {
        if (_listenersSetup) return;
        _listenersSetup = true;
        backButton.onClick.AddListener(Hide);
        if (prevImageButton != null) prevImageButton.onClick.AddListener(PrevImage);
        if (nextImageButton != null) nextImageButton.onClick.AddListener(NextImage);
    }

    public void Show(Sprite[] images, string itemName, string description)
    {
        SetupListeners(); // no-op if Awake already ran; ensures listeners exist on disabled-at-start path
        _images          = images;
        _currentImageIdx = 0;

        itemNameText.text    = itemName;
        descriptionText.text = description;

        RefreshImage();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void NextImage()
    {
        if (_images == null || _images.Length == 0) return;
        _currentImageIdx = (_currentImageIdx + 1) % _images.Length;
        RefreshImage();
    }

    private void PrevImage()
    {
        if (_images == null || _images.Length == 0) return;
        _currentImageIdx = (_currentImageIdx - 1 + _images.Length) % _images.Length;
        RefreshImage();
    }

    private void RefreshImage()
    {
        bool has      = _images != null && _images.Length > 0;
        bool multiPage = has && _images.Length > 1;

        if (itemImage != null)
        {
            itemImage.gameObject.SetActive(has);
            if (has) itemImage.sprite = _images[_currentImageIdx];
        }

        if (prevImageButton != null) prevImageButton.gameObject.SetActive(multiPage);
        if (nextImageButton != null) nextImageButton.gameObject.SetActive(multiPage);

        if (imagePageLabel != null)
        {
            imagePageLabel.gameObject.SetActive(multiPage);
            if (multiPage) imagePageLabel.text = $"{_currentImageIdx + 1} / {_images.Length}";
        }
    }
}
