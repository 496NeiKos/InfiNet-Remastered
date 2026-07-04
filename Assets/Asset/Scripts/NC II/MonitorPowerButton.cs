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

    [Header("Turn On Conditions")]
    [Tooltip("AVR must be powered on before the monitor can turn on.")]
    [SerializeField] private AVRPowerButton avrPowerButton;
    [Tooltip("Monitor back VGA cable port must be installed.")]
    [SerializeField] private BackPortSlot monitorVGAPort;
    [Tooltip("Monitor back power cable port must be installed.")]
    [SerializeField] private CablePort monitorPowerCablePort;

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

        if (avrPowerButton == null)
            avrPowerButton = FindObjectOfType<AVRPowerButton>(true);

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
        if (avrPowerButton == null || !avrPowerButton.IsPoweredOn)
        {
            ActivityLogManager.Log("Cannot turn on monitor — turn on the AVR first.", ActivityLogManager.EntryType.Warning);
            Debug.Log("[MonitorPowerButton] FAIL — AVR power button is off.");
            return;
        }

        if (monitorVGAPort == null || monitorVGAPort.IsUninstalled)
        {
            ActivityLogManager.Log("Cannot turn on monitor — connect the VGA cable to the monitor first.", ActivityLogManager.EntryType.Warning);
            Debug.Log("[MonitorPowerButton] FAIL — monitor VGA port cable not connected.");
            return;
        }

        if (monitorPowerCablePort == null || monitorPowerCablePort.IsUninstalled)
        {
            ActivityLogManager.Log("Cannot turn on monitor — connect the power cable to the monitor first.", ActivityLogManager.EntryType.Warning);
            Debug.Log("[MonitorPowerButton] FAIL — monitor power cable port not connected.");
            return;
        }

        SetState(PowerState.On);
    }

    private void TryTurnOff()
    {
        SetState(PowerState.Off);
    }

    public void ForceOn()
    {
        SetState(PowerState.On);
    }

    public void ForceOff()
    {
        SetState(PowerState.Off);
    }

    private void SetState(PowerState newState)
    {
        _initialized = true;
        _state = newState;
        ApplySprites();
        ActivityLogManager.Log(
            _state == PowerState.On ? "Monitor powered ON" : "Monitor powered OFF",
            _state == PowerState.On ? ActivityLogManager.EntryType.Install : ActivityLogManager.EntryType.Remove);
        Debug.Log($"[MonitorPowerButton] State → {_state}");

        NCIITaskListManager.CheckConditions();
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
