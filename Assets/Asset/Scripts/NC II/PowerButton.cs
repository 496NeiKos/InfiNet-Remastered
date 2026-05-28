using UnityEngine;
using UnityEngine.InputSystem;

public class PowerButton : MonoBehaviour, IPowerButton
{
    public enum PowerState { On, Off, Restarting }

    [Header("Button Sprites")]
    [SerializeField] private Sprite spriteOn;
    [SerializeField] private Sprite spriteOff;

    [Header("System Unit Front Sprites")]
    [SerializeField] private SpriteRenderer systemUnitFrontSR;
    [SerializeField] private Sprite systemUnitFrontOn;
    [SerializeField] private Sprite systemUnitFrontOff;

    [Header("System Unit Root Sprites")]
    [SerializeField] private SpriteRenderer systemUnitRootSR;
    [SerializeField] private Sprite systemUnitRootOn;
    [SerializeField] private Sprite systemUnitRootOff;

    [Header("References")]
    [SerializeField] private PowerOnConditionChecker conditionChecker;

    [Header("Initial State")]
    [SerializeField] private bool startOn = false;

    [Header("Double-Click Window (seconds)")]
    [SerializeField] private float doubleClickWindow = 0.5f;

    private SpriteRenderer _sr;
    private PowerState _state = PowerState.Off;

    private int _clickCount;
    private float _clickTimer;

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

        if (startOn)
            _state = PowerState.On;

        ApplySprite();
    }

    private void OnEnable()
    {
        _clickCount = 0;
        _clickTimer = 0f;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // Double-click to toggle on/off
        if (_clickCount > 0)
        {
            _clickTimer += Time.deltaTime;
            if (_clickTimer >= doubleClickWindow)
            {
                _clickCount = 0;
                _clickTimer = 0f;
            }
        }

        if (mouse.leftButton.wasPressedThisFrame && IsMouseOver())
        {
            _clickCount++;
            if (_clickCount == 1)
            {
                _clickTimer = 0f;
            }
            else if (_clickCount >= 2)
            {
                _clickCount = 0;
                _clickTimer = 0f;
                if (_state == PowerState.On)
                    SetState(PowerState.Off);
                else if (_state == PowerState.Off)
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

        switch (_state)
        {
            case PowerState.On:
                ActivityLogManager.Log("System unit powered ON", ActivityLogManager.EntryType.Install);
                break;
            case PowerState.Off:
                ActivityLogManager.Log("System unit powered OFF", ActivityLogManager.EntryType.Remove);
                break;
            case PowerState.Restarting:
                ActivityLogManager.Log("System unit restarting...", ActivityLogManager.EntryType.Action);
                break;
        }

        Debug.Log($"[PowerButton] State → {_state}");
    }

    private void ApplySprite()
    {
        if (_sr != null)
            _sr.sprite = (_state == PowerState.Off) ? spriteOff : spriteOn;

        bool on = _state != PowerState.Off;

        if (systemUnitFrontSR != null && systemUnitFrontOn != null && systemUnitFrontOff != null)
            systemUnitFrontSR.sprite = on ? systemUnitFrontOn : systemUnitFrontOff;

        if (systemUnitRootSR != null && systemUnitRootOn != null && systemUnitRootOff != null)
            systemUnitRootSR.sprite = on ? systemUnitRootOn : systemUnitRootOff;
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        foreach (Collider2D col in GetComponents<Collider2D>())
            if (col.OverlapPoint(mouseWorld)) return true;
        return false;
    }
}