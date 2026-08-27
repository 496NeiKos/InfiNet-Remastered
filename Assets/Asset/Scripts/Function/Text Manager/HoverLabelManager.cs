using UnityEngine;
using TMPro;

public class HoverLabelManager : MonoBehaviour
{
    public static HoverLabelManager Instance;

    [Header("Assign your TMP Text here")]
    public TextMeshProUGUI hoverLabel;

    [Header("Screen Space - Camera support")]
    [Tooltip("The canvas this panel lives on. Required for Screen Space - Camera canvases. Leave empty for Screen Space - Overlay.")]
    [SerializeField] private Canvas parentCanvas;

    private RectTransform _rectTransform;

    private void Awake()
    {
        Instance = this;

        if (hoverLabel == null)
            hoverLabel = GetComponentInChildren<TextMeshProUGUI>();

        _rectTransform = GetComponent<RectTransform>();

        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();

        gameObject.SetActive(false);
    }

    public void ShowLabel(string itemName)
    {
        hoverLabel.text = itemName;
        gameObject.SetActive(true);
    }

    public void HideLabel()
    {
        gameObject.SetActive(false);
    }

    public void FollowMouse(Vector2 screenPos)
    {
        if (!gameObject.activeSelf) return;

        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceCamera
            && _rectTransform != null)
        {
            RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
            Camera cam = parentCanvas.worldCamera != null ? parentCanvas.worldCamera : Camera.main;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPos + new Vector2(30f, 50f), cam, out Vector2 localPoint))
            {
                _rectTransform.localPosition = localPoint;
            }
        }
        else
        {
            transform.position = screenPos + new Vector2(30f, 50f);
        }
    }
}
