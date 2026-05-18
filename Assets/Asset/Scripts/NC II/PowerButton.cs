using UnityEngine;
using UnityEngine.InputSystem;

public class PowerButton : MonoBehaviour, IPowerButton
{
    public enum PowerState { On, Off, Restarting }

    [Header("Sprites")]
    [SerializeField] private Sprite spriteOn;
    [SerializeField] private Sprite spriteOff;

    [Header("References")]
    [SerializeField] private PowerOnConditionChecker conditionChecker;

    private SpriteRenderer _sr;
    private PowerState _state = PowerState.Off;

    private float _rightHoldTimer = 0f;
    private bool _isRightHolding = false;
    private const float RestartHoldDuration = 3f;

    public PowerState State => _state;
    public bool IsPoweredOn => _state == PowerState.On || _state == PowerState.Restarting;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();

        // Auto-find checker on parent root if not wired
        if (conditionChecker == null)
            conditionChecker = GetComponentInParent<PowerOnConditionChecker>();

        ApplySprite();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // Left click — toggle on/off
        if (mouse.leftButton.wasPressedThisFrame && IsMouseOver())
        {
            if (_state == PowerState.On)
            {
                SetState(PowerState.Off);
            }
            else if (_state == PowerState.Off)
            {
                TryTurnOn();
            }
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
                    _isRightHolding = false;
                    _rightHoldTimer = 0f;
                    Debug.Log("[PowerButton] Right-click released early — restart cancelled.");
                }
            }
        }
        else
        {
            _isRightHolding = false;
            _rightHoldTimer = 0f;
        }
    }

    private void TryTurnOn()
    {
        if (conditionChecker != null && !conditionChecker.CanTurnOn())
        {
            Debug.Log("[PowerButton] Cannot turn on — conditions not met. Check logs above.");
            return;
        }

        SetState(PowerState.On);
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