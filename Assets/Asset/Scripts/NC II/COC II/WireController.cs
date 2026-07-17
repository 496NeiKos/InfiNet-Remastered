using System.Collections;
using UnityEngine;

/// <summary>
/// Attached to each of the 8 inner wires inside a CableEnd's wire container.
/// Handles click-to-select and animated position swaps.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class WireController : MonoBehaviour
{
    [Tooltip("0-7 index matching the wire's color (0=W/Green,1=Green,2=W/Orange,3=Blue,4=W/Blue,5=Orange,6=W/Brown,7=Brown).")]
    public int wireColorIndex;

    [Tooltip("Display name shown in the selection indicator when this wire is clicked.\nDefaults: 0=White/Green, 1=Green, 2=White/Orange, 3=Blue, 4=White/Blue, 5=Orange, 6=White/Brown, 7=Brown")]
    public string wireName;

    /// <summary>Current slot position (0=leftmost, 7=rightmost) set by NetworkCableEndController on shuffle.</summary>
    public int CurrentSlotIndex { get; set; }

    [Header("Highlight Pulse")]
    [Tooltip("How bright the wire flashes when selected (multiplier on base color channels).")]
    [SerializeField] private float highlightBrightness = 2f;
    [Tooltip("How fast the pulse oscillates (cycles per second).")]
    [SerializeField] private float pulseSpeed = 4f;
    [Tooltip("Max extra scale added at peak of pulse (0.15 = 15% bigger).")]
    [SerializeField] private float pulseScaleAmount = 0.15f;

    private SpriteRenderer _sr;
    private Color _baseColor;
    private Color _highlightColor;
    private Vector3 _baseScale;
    private bool _highlighted;
    private Coroutine _moveRoutine;
    private Coroutine _pulseRoutine;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _baseColor = _sr.color;
        _highlightColor = new Color(
            Mathf.Min(_baseColor.r * highlightBrightness, 1f),
            Mathf.Min(_baseColor.g * highlightBrightness, 1f),
            Mathf.Min(_baseColor.b * highlightBrightness, 1f),
            1f);
        _baseScale = transform.localScale;
    }

    public void SetHighlight(bool on)
    {
        _highlighted = on;

        if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);

        if (on)
        {
            _pulseRoutine = StartCoroutine(PulseRoutine());
        }
        else
        {
            _sr.color = _baseColor;
            transform.localScale = _baseScale;
            _pulseRoutine = null;
        }
    }

    private IEnumerator PulseRoutine()
    {
        while (_highlighted)
        {
            // Oscillates smoothly 0→1→0 using Sin
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            _sr.color = Color.Lerp(_baseColor, _highlightColor, t);
            float scale = 1f + pulseScaleAmount * t;
            transform.localScale = _baseScale * scale;
            yield return null;
        }
    }

    public void AnimateTo(Vector3 targetLocalPos, float duration)
    {
        if (_moveRoutine != null) StopCoroutine(_moveRoutine);
        _moveRoutine = StartCoroutine(MoveCoroutine(targetLocalPos, duration));
    }

    private IEnumerator MoveCoroutine(Vector3 target, float duration)
    {
        Vector3 start = transform.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        transform.localPosition = target;
    }
}
