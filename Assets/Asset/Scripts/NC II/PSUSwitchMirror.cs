using UnityEngine;

/// <summary>
/// On the PSU sprite object in the system unit side view.
/// Mirrors the on/off sprite from PSUSwitchController (system unit back)
/// so both views stay in visual sync.
/// </summary>
public class PSUSwitchMirror : MonoBehaviour
{
    [SerializeField] private PSUSwitchController psuSwitch;
    [SerializeField] private Sprite offSprite;
    [SerializeField] private Sprite onSprite;

    private SpriteRenderer _sr;
    private bool _lastState;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        if (psuSwitch == null) return;
        if (psuSwitch.IsOn != _lastState)
            Refresh();
    }

    private void Refresh()
    {
        if (_sr == null || psuSwitch == null) return;
        _lastState = psuSwitch.IsOn;
        _sr.sprite = _lastState ? onSprite : offSprite;
    }
}
