/*
 * ================================================================
 *  UNITY SETUP GUIDE — T3SystemUnitController
 * ================================================================
 *  Mirrors T2SystemUnitController but adds a power button that
 *  toggles the system unit ON/OFF and notifies the loading panel
 *  to reset whenever the unit powers back on.
 *
 *  STEP 1 — Create the T3 System Unit root GameObject
 *    - Place it inside Topic 3's worldRootContainer.
 *    - Add components: SpriteRenderer, Collider2D, PrefabInteraction,
 *      T3SystemUnitController.
 *    - Do NOT add DragPrefab — this object is not draggable.
 *    - Assign the powered-off sprite to the SpriteRenderer initially.
 *
 *  STEP 2 — Create the front view child
 *    a) Add a child GameObject named "T3SystemUnitFront".
 *       Start it INACTIVE.
 *    b) Inside it, create a child "USBPort":
 *         Components: SpriteRenderer (port art), Collider2D, CablePort
 *         CablePort settings:
 *           acceptedCableTypes: ["USB"]
 *           startEmpty: true
 *    c) Inside it, create a child "PowerButton":
 *         Components: SpriteRenderer (button art), Collider2D
 *         This is the clickable world-space power toggle.
 *         (Left-click is detected by this script via raycast.)
 *
 *  STEP 3 — Wire the inspector
 *    T3SystemUnitController:
 *      frontView           → T3SystemUnitFront
 *      powerButtonCollider → Collider2D on PowerButton child
 *      rootSpriteRenderer  → SpriteRenderer on this root GameObject
 *      poweredOffSprite    → sprite shown when OFF
 *      poweredOnSprite     → sprite shown when ON
 *      frontSpriteRenderer → (optional) SpriteRenderer inside front view
 *                            for a separate front-panel art swap
 *      frontOffSprite      → (optional) front art when OFF
 *      frontOnSprite       → (optional) front art when ON
 *
 *  STEP 4 — Flash drive HardwareHolder
 *    - Create a HardwareHolder UI element in Topic 3's hardware area.
 *    - Its hardwarePrefab is a Flash Drive GameObject:
 *        Components: SpriteRenderer, Collider2D, CableBehavior
 *        CableBehavior settings:
 *          cableType: "USB"
 *          hardwareHolder: reference back to this HardwareHolder
 *          homePort: leave blank (auto-detected on install)
 *
 *  STEP 5 — Loading panel reset wiring
 *    UEFILoadingPanel's inspector has a "systemUnit" field — assign
 *    this T3SystemUnitController there. The loading panel subscribes
 *    to OnPoweredOn and calls ResetState() automatically.
 *
 *  POWER BEHAVIOUR
 *    Default: OFF. Left-clicking the PowerButton sprite in the front
 *    view toggles the state. OFF → ON fires the OnPoweredOn event,
 *    which resets the UEFI loading panel. The monitor's right-click
 *    interaction is gated on IsPoweredOn == true.
 * ================================================================
 */

using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class T3SystemUnitController : MonoBehaviour, IHardwareController
{
    [Header("Views")]
    [SerializeField] private GameObject frontView;
    [SerializeField] private Collider2D powerButtonCollider;

    [Header("Root Sprites (power state)")]
    [SerializeField] private SpriteRenderer rootSpriteRenderer;
    [SerializeField] private Sprite poweredOffSprite;
    [SerializeField] private Sprite poweredOnSprite;

    [Header("Front View Sprites (optional)")]
    [SerializeField] private SpriteRenderer frontSpriteRenderer;
    [SerializeField] private Sprite frontOffSprite;
    [SerializeField] private Sprite frontOnSprite;

    public bool IsPoweredOn     { get; private set; }
    // True once the unit has been powered OFF then back ON at least once (Task 5).
    public bool HasPowerCycled  { get; private set; }

    // Fired on OFF → ON transition. UEFILoadingPanel and T3TaskListManager subscribe.
    public event Action OnPoweredOn;

    private bool _hasPoweredOff;

    private void Start()
    {
        if (rootSpriteRenderer == null)
            rootSpriteRenderer = GetComponent<SpriteRenderer>();

        frontView?.SetActive(false);
        ApplySprites();
    }

    private void Update()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (frontView == null || !frontView.activeSelf) return;
        if (powerButtonCollider == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        if (hit.collider == powerButtonCollider)
            TogglePower();
    }

    public void ShowDetailAtCenter() => frontView?.SetActive(true);

    public void HideDetail() => frontView?.SetActive(false);

    public void TogglePower()
    {
        bool wasOn = IsPoweredOn;
        IsPoweredOn = !IsPoweredOn;
        ApplySprites();

        if (!wasOn && IsPoweredOn)
        {
            if (_hasPoweredOff) HasPowerCycled = true;
            OnPoweredOn?.Invoke();
            Debug.Log("[T3SystemUnitController] System unit powered ON — loading panel reset.");
        }
        else
        {
            _hasPoweredOff = true;
            T3TaskListManager.CheckConditions();
            Debug.Log("[T3SystemUnitController] System unit powered OFF.");
        }
    }

    private void ApplySprites()
    {
        if (rootSpriteRenderer != null)
            rootSpriteRenderer.sprite = IsPoweredOn ? poweredOnSprite : poweredOffSprite;

        if (frontSpriteRenderer != null)
            frontSpriteRenderer.sprite = IsPoweredOn ? frontOnSprite : frontOffSprite;
    }
}
