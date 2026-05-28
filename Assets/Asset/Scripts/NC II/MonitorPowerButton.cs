using UnityEngine;
using UnityEngine.InputSystem;

public class MonitorPowerButton : MonoBehaviour, IPowerButton
{
    public enum PowerState { On, Off }

    [Header("Button Sprites")]
    [SerializeField] private Sprite spriteOn;
    [SerializeField] private Sprite spriteOff;

    [Header("Monitor Root Sprite (changes when powered on/off)")]
    [SerializeField] private SpriteRenderer monitorRootSprite;
    [SerializeField] private Sprite monitorRootSpriteOn;
    [SerializeField] private Sprite monitorRootSpriteOff;

    [Header("Monitor Front Detail Sprite (changes when powered on/off)")]
    [SerializeField] private SpriteRenderer monitorDetailedSprite;
    [SerializeField] private Sprite monitorDetailedSpriteOn;
    [SerializeField] private Sprite monitorDetailedSpriteOff;

    [Header("Turn On Condition")]
    [Tooltip("Monitor back VGA port cable must be installed before the monitor can turn on.")]
    [SerializeField] private BackPortSlot monitorVGAPort;

    [Header("Power Off Gate")]
    [Tooltip("SU front power button must be off before the monitor can be turned off.")]
    [SerializeField] private PowerButton suPowerButton;

    [Header("Initial State")]
    [SerializeField] private bool startOn = false;

    [Header("Double-Click Window (seconds)")]
    [SerializeField] private float doubleClickWindow = 0.5f;

    private SpriteRenderer _sr;
    private PowerState _state = PowerState.Off;
    private bool _initialized = false;

    private int _clickCount;
    private float _clickTimer;

    public PowerState State => _state;
    // Falls back to startOn if Awake hasn't run yet (object was inactive at scene load).
    public bool IsPoweredOn => _initialized ? _state == PowerState.On : startOn;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _state = startOn ? PowerState.On : PowerState.Off;
        _initialized = true;
        ApplySprites();
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
                    TryTurnOff();
                else
                    TryTurnOn();
            }
        }
    }

    private void TryTurnOn()
    {
        if (monitorVGAPort == null || monitorVGAPort.IsUninstalled)
        {
            ActivityLogManager.Log("Cannot turn on monitor — connect the VGA cable first.", ActivityLogManager.EntryType.Warning);
            Debug.Log("[MonitorPowerButton] FAIL — monitor VGA port cable not connected.");
            return;
        }

        SetState(PowerState.On);
    }

    private void TryTurnOff()
    {
        if (suPowerButton != null && suPowerButton.IsPoweredOn)
        {
            ActivityLogManager.Log("Cannot turn off monitor — turn off the System Unit first.", ActivityLogManager.EntryType.Warning);
            Debug.Log("[MonitorPowerButton] Cannot turn off — System Unit power button is still on.");
            return;
        }

        SetState(PowerState.Off);
    }

    private void SetState(PowerState newState)
    {
        _state = newState;
        ApplySprites();
        ActivityLogManager.Log(
            _state == PowerState.On ? "Monitor powered ON" : "Monitor powered OFF",
            _state == PowerState.On ? ActivityLogManager.EntryType.Install : ActivityLogManager.EntryType.Remove);
        Debug.Log($"[MonitorPowerButton] State → {_state}");
    }

    private void ApplySprites()
    {
        if (_sr != null)
            _sr.sprite = (_state == PowerState.On) ? spriteOn : spriteOff;

        if (monitorRootSprite != null)
            monitorRootSprite.sprite = (_state == PowerState.On) ? monitorRootSpriteOn : monitorRootSpriteOff;

        if (monitorDetailedSprite != null)
            monitorDetailedSprite.sprite = (_state == PowerState.On) ? monitorDetailedSpriteOn : monitorDetailedSpriteOff;
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        foreach (Collider2D col in GetComponents<Collider2D>())
            if (col.OverlapPoint(mouseWorld)) return true;
        return false;
    }
}
