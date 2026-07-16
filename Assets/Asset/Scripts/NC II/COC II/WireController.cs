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

    /// <summary>Current slot position (0=leftmost, 7=rightmost) set by NetworkCableEndController on shuffle.</summary>
    public int CurrentSlotIndex { get; set; }

    private SpriteRenderer _sr;
    private Color _baseColor;
    private bool _highlighted;
    private Coroutine _moveRoutine;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _baseColor = _sr.color;
    }

    public void SetHighlight(bool on)
    {
        _highlighted = on;
        _sr.color = on
            ? new Color(Mathf.Min(_baseColor.r * 1.5f, 1f), Mathf.Min(_baseColor.g * 1.5f, 1f), Mathf.Min(_baseColor.b * 1.5f, 1f), 1f)
            : _baseColor;
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
