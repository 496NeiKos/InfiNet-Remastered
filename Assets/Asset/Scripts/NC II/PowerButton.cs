using UnityEngine;
using UnityEngine.InputSystem;

public class PowerButton : MonoBehaviour
{
    public enum PowerState { On, Off, Restarting }

    [Header("Sprites")]
    [SerializeField] private Sprite spriteOn;
    [SerializeField] private Sprite spriteOff;

    private SpriteRenderer _sr;
    private PowerState _state = PowerState.On;

    private float _rightHoldTimer = 0f;
    private bool _isRightHolding = false;
    private const float RestartHoldDuration = 3f;

    public PowerState State => _state;
    public bool IsPoweredOn => _state == PowerState.On || _state == PowerState.Restarting;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        ApplySprite();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // Left click — toggle on/off only
        if (mouse.leftButton.wasPressedThisFrame && IsMouseOver())
        {
            if (_state == PowerState.On)
                SetState(PowerState.Off);
            else if (_state == PowerState.Off)
                SetState(PowerState.On);
        }

        // Right click hold — restart (only when On)
        if (_state == PowerState.On)
        {
            if (mouse.rightButton.wasPressedThisFrame && IsMouseOver())
            {
                _isRightHolding = true;
                _rightHoldTimer = 0f;
            }

            if (_isRightHolding)
            {
                if (mouse.rightButton.isPressed)
                {
                    _rightHoldTimer += Time.deltaTime;
                    if (_rightHoldTimer >= RestartHoldDuration)
                    {
                        _isRightHolding = false;
                        _rightHoldTimer = 0f;
                        TriggerRestart();
                    }
                }
                else
                {
                    // Released before 3s — reset timer
                    _isRightHolding = false;
                    _rightHoldTimer = 0f;
                    Debug.Log("[PowerButton] Right-click released early, restart cancelled.");
                }
            }
        }
        else
        {
            // Not On — cancel any pending restart
            _isRightHolding = false;
            _rightHoldTimer = 0f;
        }
    }

    private void TriggerRestart()
    {
        SetState(PowerState.Restarting);
        Debug.Log("[PowerButton] Restarting — no logic yet.");
        SetState(PowerState.On);
    }

    private void SetState(PowerState newState)
    {
        _state = newState;
        ApplySprite();
        Debug.Log($"[PowerButton] State → {_state}");
    }

    private void ApplySprite()
    {
        if (_sr == null) return;
        _sr.sprite = (_state == PowerState.Off) ? spriteOff : spriteOn;
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D col = GetComponent<Collider2D>();
        return col != null && col.OverlapPoint(mouseWorld);
    }
}