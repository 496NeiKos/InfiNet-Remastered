using UnityEngine;

/// <summary>
/// Attach to Interactive_Indicator_Heatsink.
/// Mirrors the root SpriteRenderer of the HeatsinkController every frame,
/// so it reflects the connected/disconnected cable sprite in real time.
/// </summary>
public class HeatsinkIndicator : MonoBehaviour
{
    [SerializeField] private HeatsinkController heatsinkTarget;

    private SpriteRenderer _indicatorSR;
    private SpriteRenderer _heatsinkSR;

    private void Awake()
    {
        _indicatorSR = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (heatsinkTarget != null)
            _heatsinkSR = heatsinkTarget.GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (_indicatorSR == null || _heatsinkSR == null) return;
        _indicatorSR.sprite = _heatsinkSR.sprite;
    }
}
