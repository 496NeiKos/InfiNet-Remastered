using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Self-contained tooltip for hardware area button icons.
/// Attach to the tooltip Panel GameObject (a child of your Canvas).
/// Assign the TMP Text child in the Inspector.
/// HardwareHolder calls Show/Hide automatically.
/// Works with Screen Space - Overlay and Screen Space - Camera canvases.
/// </summary>
public class HardwareAreaTooltip : MonoBehaviour
{
    public static HardwareAreaTooltip Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Vector2 offset = new Vector2(12f, 20f);

    private RectTransform _rect;
    private Canvas _canvas;

    private void Awake()
    {
        Instance = this;
        _rect = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        // Walk up to the root canvas so coordinate conversion is correct
        if (_canvas != null && !_canvas.isRootCanvas)
            _canvas = _canvas.rootCanvas;

        // Prevent the tooltip from intercepting pointer events and causing flicker
        foreach (var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            graphic.raycastTarget = false;

        // Render above the hardware area (sortingOrder 50) and all detail panel layers
        Canvas overrideCanvas = GetComponent<Canvas>();
        if (overrideCanvas == null) overrideCanvas = gameObject.AddComponent<Canvas>();
        overrideCanvas.overrideSorting = true;
        overrideCanvas.sortingOrder = 60;
        if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        gameObject.SetActive(false);
    }

    public void Show(string text)
    {
        if (label != null) label.text = text;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Mouse.current == null || _canvas == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        // Convert screen position to local position inside the root canvas rect
        Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        RectTransform canvasRect = _canvas.GetComponent<RectTransform>();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, cam, out Vector2 local))
            _rect.anchoredPosition = local + offset;
    }
}
