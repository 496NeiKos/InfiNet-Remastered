using UnityEngine;
using UnityEngine.InputSystem;

public class AVRPowerButton : MonoBehaviour, IPowerButton
{
    public enum PowerState { On, Off }

    [Header("Button Sprites")]
    [SerializeField] private Sprite spriteOn;
    [SerializeField] private Sprite spriteOff;

    [Header("AVR Root Sprite (changes when powered on/off)")]
    [SerializeField] private SpriteRenderer avrRootSprite;
    [SerializeField] private Sprite avrSpriteOn;
    [SerializeField] private Sprite avrSpriteOff;

    [Header("AVR Detailed Sprite (front view — changes with power state)")]
    [SerializeField] private SpriteRenderer avrDetailedSprite;
    [SerializeField] private Sprite avrDetailedSpriteOn;
    [SerializeField] private Sprite avrDetailedSpriteOff;

    [Header("Condition References (AVR back ports only)")]
    [SerializeField] private BackPortSlot aPSUPort;
    [SerializeField] private BackPortSlot aMPort;

    [Header("Power Off Gate")]
    [Tooltip("SU front power button must be off before AVR can be turned off.")]
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
        bool pass = true;

        if (aPSUPort == null || aPSUPort.IsUninstalled)
        {
            ActivityLogManager.Log("Cannot turn on AVR — connect the AVR PSU cable first.", ActivityLogManager.EntryType.Warning);
            Debug.Log("[AVRPowerButton] FAIL — AVR PSU port cable not connected.");
            pass = false;
        }

        if (aMPort == null || aMPort.IsUninstalled)
        {
            ActivityLogManager.Log("Cannot turn on AVR — connect the AVR monitor cable first.", ActivityLogManager.EntryType.Warning);
            Debug.Log("[AVRPowerButton] FAIL — AVR monitor cable not connected.");
            pass = false;
        }

        if (!pass) return;

        SetState(PowerState.On);
    }

    private void TryTurnOff()
    {
        if (suPowerButton != null && suPowerButton.IsPoweredOn)
        {
            ActivityLogManager.Log("Cannot turn off AVR — turn off the System Unit first.", ActivityLogManager.EntryType.Warning);
            Debug.Log("[AVRPowerButton] Cannot turn off — System Unit power button is still on.");
            return;
        }

        SetState(PowerState.Off);
    }

    private void SetState(PowerState newState)
    {
        _state = newState;
        ApplySprites();
        ActivityLogManager.Log(
            _state == PowerState.On ? "AVR powered ON" : "AVR powered OFF",
            _state == PowerState.On ? ActivityLogManager.EntryType.Install : ActivityLogManager.EntryType.Remove);
        Debug.Log($"[AVRPowerButton] State → {_state}");

        NCIITaskListManager.CheckConditions();
    }

    private void ApplySprites()
    {
        if (_sr != null)
            _sr.sprite = (_state == PowerState.On) ? spriteOn : spriteOff;

        if (avrRootSprite != null)
            avrRootSprite.sprite = (_state == PowerState.On) ? avrSpriteOn : avrSpriteOff;

        if (avrDetailedSprite != null)
            avrDetailedSprite.sprite = (_state == PowerState.On) ? avrDetailedSpriteOn : avrDetailedSpriteOff;
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        foreach (Collider2D col in GetComponents<Collider2D>())
            if (col.OverlapPoint(mouseWorld)) return true;
        return false;
    }
}