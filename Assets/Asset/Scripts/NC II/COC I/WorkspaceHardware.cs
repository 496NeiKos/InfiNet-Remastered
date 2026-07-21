using UnityEngine;
using UnityEngine.EventSystems;

public class WorkspaceHardware : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler
{
    [Header("Info Panel")]
    [SerializeField] private Sprite[] infoImages;
    [SerializeField] private string infoName;
    [TextArea(3, 6)]
    [SerializeField] private string infoDescription;

    private const float ClickWindow = 0.5f;
    private int _clickCount;
    private float _lastClickTime;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(infoName)) return;
        if (eventData.button != PointerEventData.InputButton.Right) return;

        if (Time.unscaledTime - _lastClickTime > ClickWindow)
            _clickCount = 0;

        _lastClickTime = Time.unscaledTime;
        _clickCount++;

        if (_clickCount >= 2)
        {
            _clickCount = 0;
            HardwareInfoPanel.Instance?.Show(infoImages, infoName, infoDescription);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _clickCount = 0;
    }
}
