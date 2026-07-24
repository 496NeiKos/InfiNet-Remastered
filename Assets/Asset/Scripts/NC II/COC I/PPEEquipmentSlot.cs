using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PPEEquipmentSlot : MonoBehaviour, IPointerDownHandler
{
    [Header("Accepted Tags")]
    [Tooltip("Tags this slot will accept. Multiple allowed, e.g. 'head'. Equippable PPE whose slotTag matches any entry can fill this slot.")]
    [SerializeField] private string[] acceptedTags;

    [Header("Visuals")]
    [SerializeField] private Image displayImage;
    [SerializeField] private Image slotBGImage;
    [SerializeField] private Sprite slotBGOccupied;

    private Sprite _emptySprite;
    private Sprite _slotBGDefault;

    private const float ClickWindow = 0.5f;
    private int _clickCount;
    private float _lastClickTime;

    public bool IsOccupied      => _occupant != null;
    public PPEItemSlot Occupant => _occupant;

    private PPEItemSlot _occupant;

    private void Awake()
    {
        if (displayImage != null)
        {
            _emptySprite = displayImage.sprite;
            displayImage.color = Color.clear;
        }
        if (slotBGImage != null)
            _slotBGDefault = slotBGImage.sprite;
    }

    public bool CanAccept(string tag)
    {
        foreach (string t in acceptedTags)
            if (t == tag) return true;
        return false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!IsOccupied) return;

        PPEInventoryManager.Instance?.OnItemClicked(_occupant);

        if (Time.unscaledTime - _lastClickTime > ClickWindow)
            _clickCount = 0;

        _lastClickTime = Time.unscaledTime;
        _clickCount++;

        if (_clickCount >= 2)
        {
            _clickCount = 0;
            PPEInventoryManager.Instance?.OnItemDoubleClicked(_occupant);
        }
    }

    public bool TryOccupy(PPEItemSlot item)
    {
        if (IsOccupied || !CanAccept(item.SlotTag)) return false;
        _occupant = item;
        if (displayImage != null)
        {
            displayImage.sprite = item.ItemSprite != null ? item.ItemSprite : _emptySprite;
            displayImage.color = Color.white;
        }
        if (slotBGImage != null && slotBGOccupied != null)
            slotBGImage.sprite = slotBGOccupied;
        return true;
    }

    public void Release()
    {
        _occupant = null;
        if (displayImage != null)
        {
            displayImage.sprite = _emptySprite;
            displayImage.color = Color.clear;
        }
        if (slotBGImage != null)
            slotBGImage.sprite = _slotBGDefault;
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
