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

    [Header("Condition References")]
    [SerializeField] private BackPortSlot avrPsuPort;
    [SerializeField] private BackPortSlot systemUnitPsuPort;

    [Header("Double-Click Window (seconds)")]
    [SerializeField] private float doubleClickWindow = 0.5f;

    private SpriteRenderer _sr;
    private PowerState _state = PowerState.Off;

    private int _clickCount;
    private float _clickTimer;

    public PowerState State => _state;
    public bool IsPoweredOn => _state == PowerState.On;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
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
                    SetState(PowerState.Off);
                else
                    TryTurnOn();
            }
        }
    }

    private void TryTurnOn()
    {
        bool pass = true;

        if (avrPsuPort == null || avrPsuPort.IsUninstalled)
        {
            Debug.Log("[AVRPowerButton] FAIL — AVR PSU port cable not connected.");
            pass = false;
        }

        if (systemUnitPsuPort == null || systemUnitPsuPort.IsUninstalled)
        {
            Debug.Log("[AVRPowerButton] FAIL — SystemUnit PSU back port cable not connected.");
            pass = false;
        }

        if (!pass)
        {
            Debug.Log("[AVRPowerButton] Cannot turn on — conditions not met.");
            return;
        }

        SetState(PowerState.On);
    }

    private void SetState(PowerState newState)
    {
        _state = newState;
        ApplySprites();
        Debug.Log($"[AVRPowerButton] State → {_state}");
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