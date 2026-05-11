using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Controls a single screw hole on the system unit cover.
/// 
/// Three states:
///   Empty   → no screw, shows empty sprite
///   Pending → screw placed but not tightened, shows pending sprite
///   Screwed → fully tightened, shows screwed sprite
///
/// Transitions:
///   Screwed  + Screwdriver (2s hold) → Empty
///   Empty    + Screw drag (tag "Screw") → Pending (instant, only if cover closed)
///   Pending  + Screwdriver (2s hold) → Screwed
///   Pending  + Drag to hardware area → Empty
///   Pending  + Drag to another empty screw → source becomes Empty, target becomes Pending
/// </summary>
public class ScrewController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum ScrewState { Empty, Pending, Screwed }

    [Header("Sprites")]
    [SerializeField] private Sprite screwedSprite;
    [SerializeField] private Sprite pendingSprite;
    [SerializeField] private Sprite emptySprite;

    [Header("Settings")]
    [SerializeField] private float screwdriverDuration = 2f;

    [Header("References")]
    [SerializeField] private CoverController coverController;

    private SpriteRenderer _spriteRenderer;
    private ScrewState _state = ScrewState.Screwed;
    private float _currentProgress = 0f;
    private bool _isScrewdriverTouching = false;

    // Drag for pending screw
    private bool _isDragging = false;
    private GameObject _dragVisual;
    private RectTransform _hardwareArea;

    // Track our own drag visual so we can ignore it in OnTriggerEnter2D
    private GameObject _ownDragVisual;

    public System.Action<ScrewController> OnStateChanged;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSprite();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            _hardwareArea = GameManager.Instance.hardwareArea;
    }

    private void Update()
    {
        if (!_isScrewdriverTouching) return;

        if (_state == ScrewState.Screwed)
        {
            _currentProgress += Time.deltaTime;
            if (_currentProgress >= screwdriverDuration)
            {
                SetState(ScrewState.Empty);
                _currentProgress = 0f;
                _isScrewdriverTouching = false;
            }
        }

        if (_state == ScrewState.Pending)
        {
            _currentProgress += Time.deltaTime;
            if (_currentProgress >= screwdriverDuration)
            {
                SetState(ScrewState.Screwed);
                _currentProgress = 0f;
                _isScrewdriverTouching = false;
            }
        }
    }

    // ── Collision Detection ───────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Screwdriver → start timer
        if (other.CompareTag("Screwdriver"))
        {
            if (_state == ScrewState.Screwed || _state == ScrewState.Pending)
            {
                _isScrewdriverTouching = true;
            }
        }

        // Screw from hardware area OR pending screw from another hole
        if (other.CompareTag("Screw"))
        {
            // Ignore our OWN drag visual
            if (other.gameObject == _ownDragVisual) return;

            if (_state != ScrewState.Empty)
            {
                Debug.Log($"[ScrewController] {name}: cannot place screw, state is {_state}");
                return;
            }

            if (coverController != null && coverController.IsOpen())
            {
                Debug.Log($"[ScrewController] {name}: cannot place screw, cover is open");
                return;
            }

            SetState(ScrewState.Pending);
            Destroy(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Screwdriver"))
        {
            _isScrewdriverTouching = false;
        }
    }

    // ── Drag (Pending screw movement/removal) ─────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_state != ScrewState.Pending)
        {
            _isDragging = false;
            return;
        }

        if (coverController != null && coverController.IsOpen())
        {
            _isDragging = false;
            return;
        }

        _isDragging = true;

        // Immediately show empty sprite (screw is being picked up)
        SetStateVisualOnly(ScrewState.Empty);

        // Create drag visual tagged "Screw" so other empty holes can accept it
        _dragVisual = new GameObject("PendingScrewDrag");
        _dragVisual.tag = "Screw";
        _ownDragVisual = _dragVisual; // Track it so we ignore it in our own trigger

        SpriteRenderer sr = _dragVisual.AddComponent<SpriteRenderer>();
        sr.sprite = pendingSprite;
        sr.sortingOrder = 100;

        CircleCollider2D col = _dragVisual.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f;

        Rigidbody2D rb = _dragVisual.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f)
        );
        worldPos.z = 0f;
        _dragVisual.transform.position = worldPos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || _dragVisual == null) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, 10f)
        );
        worldPos.z = 0f;
        _dragVisual.transform.position = worldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging)
        {
            CleanupDrag();
            return;
        }
        _isDragging = false;

        // Check if drag visual was already consumed by another screw hole
        // (Destroy was called by the other ScrewController's OnTriggerEnter2D)
        bool wasConsumed = _dragVisual == null;

        // Clean up drag visual if still exists
        CleanupDrag();

        if (wasConsumed)
        {
            // Another screw hole accepted it → this hole stays Empty
            Debug.Log($"[ScrewController] {name}: pending screw moved to another hole");
            SetState(ScrewState.Empty);
            return;
        }

        // Check if dropped in hardware area
        if (_hardwareArea != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                _hardwareArea, eventData.position, eventData.pressEventCamera))
        {
            Debug.Log($"[ScrewController] {name}: pending screw returned to hardware area");
            SetState(ScrewState.Empty);
            return;
        }

        // Invalid drop → put screw back to Pending
        Debug.Log($"[ScrewController] {name}: drag cancelled, screw back to pending");
        SetState(ScrewState.Pending);
    }

    private void CleanupDrag()
    {
        if (_dragVisual != null)
        {
            Destroy(_dragVisual);
            _dragVisual = null;
        }
        _ownDragVisual = null;
    }

    // ── State Management ──────────────────────────────────────────────────

    private void SetState(ScrewState newState)
    {
        _state = newState;
        _currentProgress = 0f;
        UpdateSprite();

        Debug.Log($"[ScrewController] {name} → {_state}");
        OnStateChanged?.Invoke(this);
    }

    /// <summary>
    /// Changes only the visual sprite without changing the actual state.
    /// Used during drag start to show empty hole while state is still Pending.
    /// </summary>
    private void SetStateVisualOnly(ScrewState visualState)
    {
        if (_spriteRenderer == null) return;

        switch (visualState)
        {
            case ScrewState.Screwed:
                if (screwedSprite != null) _spriteRenderer.sprite = screwedSprite;
                break;
            case ScrewState.Pending:
                if (pendingSprite != null) _spriteRenderer.sprite = pendingSprite;
                break;
            case ScrewState.Empty:
                if (emptySprite != null) _spriteRenderer.sprite = emptySprite;
                break;
        }
    }

    private void UpdateSprite()
    {
        SetStateVisualOnly(_state);
    }

    // ── Public Getters ────────────────────────────────────────────────────

    public ScrewState GetState() => _state;
    public bool IsScrewed() => _state == ScrewState.Screwed;
    public bool IsEmpty() => _state == ScrewState.Empty;
    public bool IsPending() => _state == ScrewState.Pending;
    public bool IsUnscrewed() => _state != ScrewState.Screwed;
}