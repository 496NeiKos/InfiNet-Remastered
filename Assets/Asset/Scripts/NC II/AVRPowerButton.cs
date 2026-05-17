using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On AVR front power button object.
/// On/Off only — no restart, no hold.
/// Can only turn ON if AVR back PSU port AND SystemUnit back PSU port are both installed.
/// </summary>
public class AVRPowerButton : MonoBehaviour
{
    public enum PowerState { On, Off }

    [Header("Sprites")]
    [SerializeField] private Sprite spriteOn;
    [SerializeField] private Sprite spriteOff;

    [Header("Condition References")]
    [Tooltip("PSU port on AVR back view")]
    [SerializeField] private BackPortSlot avrPsuPort;
    [Tooltip("PSU port on SystemUnit back view")]
    [SerializeField] private BackPortSlot systemUnitPsuPort;

    private SpriteRenderer _sr;
    private PowerState _state = PowerState.Off;

    public PowerState State => _state;
    public bool IsPoweredOn => _state == PowerState.On;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        ApplySprite();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame && IsMouseOver())
        {
            if (_state == PowerState.On)
                SetState(PowerState.Off);
            else
                TryTurnOn();
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
        ApplySprite();
        Debug.Log($"[AVRPowerButton] State → {_state}");
    }

    private void ApplySprite()
    {
        if (_sr == null) return;
        _sr.sprite = (_state == PowerState.On) ? spriteOn : spriteOff;
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D col = GetComponent<Collider2D>();
        return col != null && col.OverlapPoint(mouseWorld);
    }
}