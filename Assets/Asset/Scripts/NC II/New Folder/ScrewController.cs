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
///   Empty    + Screw DROPPED on it   → Pending (only if cover closed)
///   Pending  + Screwdriver (2s hold) → Screwed
///   Pending  + Drag to hardware area → Empty
///   Pending  + Drag to another empty screw → source Empty, target Pending
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

        // Screwdriver on Screwed → unscrew to Empty
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

        // Screwdriver on Pending → screw to Screwed
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
        // Screwdriver contact → start timer (works on Screwed and Pending)
        if (other.CompareTag("Screwdriver"))
        {
            if (_state == ScrewState.Screwed || _state == ScrewState.Pending)
            {
                _isScrewdriverTouching = true;
            }
        }

        // NOTE: "Screw" tag contact NO LONGER installs instantly.
        // Installation only happens on DROP via ScrewDrag.OnEndDrag().
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Screwdriver"))
        {
            _isScrewdriverTouching = false;
        }
    }

    // ── Public: Called by ScrewDrag when screw is DROPPED on this hole ────

    /// <summary>
    /// Called by ScrewDrag.OnEndDrag() when a screw is dropped on this hole.
    /// Only works if state is Empty and cover is closed.
    /// </summary>
    public bool TryPlaceScrew()
    {
        if (_state != ScrewState.Empty)
            return false;

        if (coverController != null && coverController.IsOpen())
        {
            Debug.Log($"[ScrewController] {name}: cannot place screw, cover is open");
            return false;
        }

        SetState(ScrewState.Pending);
        return true;
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

        // Create drag visual tagged "Screw" so other empty holes can accept on drop
        _dragVisual = new GameObject("PendingScrewDrag");
        _dragVisual.tag = "Screw";
        _ownDragVisual = _dragVisual;

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

        // Check if dropped on another empty screw hole
        ScrewController targetScrew = FindScrewAtPosition(eventData.position);
        if (targetScrew != null && targetScrew != this && targetScrew.TryPlaceScrew())
        {
            // Moved to another hole → this stays Empty
            CleanupDrag();
            SetState(ScrewState.Empty);
            return;
        }

        // Check if dropped in hardware area
        if (_hardwareArea != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                _hardwareArea, eventData.position, eventData.pressEventCamera))
        {
            CleanupDrag();
            SetState(ScrewState.Empty);
            return;
        }

        // Invalid drop → put screw back to Pending
        CleanupDrag();
        SetState(ScrewState.Pending);
    }

    /// <summary>
    /// Raycast to find a ScrewController at the drop position.
    /// </summary>
    private ScrewController FindScrewAtPosition(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null) continue;
            ScrewController screw = hit.collider.GetComponent<ScrewController>();
            if (screw != null && screw != this && screw.GetState() == ScrewState.Empty)
                return screw;
        }

        return null;
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
        OnStateChanged?.Invoke(this);
    }

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