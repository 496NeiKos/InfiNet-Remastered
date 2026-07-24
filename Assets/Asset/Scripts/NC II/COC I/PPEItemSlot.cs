using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PPEItemSlot : MonoBehaviour, IPointerDownHandler
{
    [Header("Item Data")]
    [SerializeField] private Sprite itemSprite;
    [SerializeField] private string itemName;
    [TextArea(2, 4)]
    [SerializeField] private string itemDescription;
    [Tooltip("head | hands | feet | body | unequippable")]
    [SerializeField] private string slotTag;

    [Header("Visuals")]
    [SerializeField] private Image slotBGImage;
    [SerializeField] private Sprite slotBGEquipped;
    [SerializeField] private TextMeshProUGUI statusText;

    private Sprite _slotBGDefault;

    private const float ClickWindow = 0.5f;
    private int _clickCount;
    private float _lastClickTime;

    public bool IsActive        { get; private set; }
    public string SlotTag       => slotTag;
    public Sprite ItemSprite    => itemSprite;
    public string ItemName      => itemName;
    public string ItemDescription => itemDescription;
    public bool IsUnequippable  => slotTag == "unequippable";

    private void Awake()
    {
        if (slotBGImage != null)
            _slotBGDefault = slotBGImage.sprite;
    }

    private void Start()
    {
        RefreshVisuals();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        PPEInventoryManager.Instance?.OnItemClicked(this);

        if (Time.unscaledTime - _lastClickTime > ClickWindow)
            _clickCount = 0;

        _lastClickTime = Time.unscaledTime;
        _clickCount++;

        if (_clickCount >= 2)
        {
            _clickCount = 0;
            PPEInventoryManager.Instance?.OnItemDoubleClicked(this);
        }
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        if (slotBGImage != null)
            slotBGImage.sprite = IsActive ? slotBGEquipped : _slotBGDefault;

        if (statusText != null)
        {
            statusText.text = IsUnequippable ? "Placed" : "Equipped";
            statusText.gameObject.SetActive(IsActive);
        }
    }

    public void Shake() => StartCoroutine(ShakeRoutine());

    private IEnumerator ShakeRoutine()
    {
        RectTransform rt = GetComponent<RectTransform>();
        Vector2 origin = rt.anchoredPosition;
        float t = 0f;

        while (t < 0.4f)
        {
            rt.anchoredPosition = new Vector2(origin.x + Mathf.Sin(t * 50f) * 8f, origin.y);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        rt.anchoredPosition = origin;
    }
}
