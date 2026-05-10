using UnityEngine;

/// <summary>
/// Controls a single screw on the system unit cover.
/// Requires screwdriver contact for a duration to unscrew.
/// Timer pauses when screwdriver leaves, continues when it returns.
/// </summary>
public class ScrewController : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite screwedSprite;
    [SerializeField] private Sprite unscrewedSprite;

    [Header("Settings")]
    [SerializeField] private float unscrewDuration = 2f;

    private SpriteRenderer _spriteRenderer;
    private float _currentProgress = 0f;
    private bool _isUnscrewed = false;
    private bool _isScrewdriverTouching = false;

    /// <summary>
    /// Event fired when this screw finishes unscrewing.
    /// CoverController listens to this.
    /// </summary>
    public System.Action<ScrewController> OnUnscrewed;

    /// <summary>
    /// Event fired when this screw is re-screwed.
    /// </summary>
    public System.Action<ScrewController> OnScrewed;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_spriteRenderer != null && screwedSprite != null)
            _spriteRenderer.sprite = screwedSprite;
    }

    private void Update()
    {
        if (_isUnscrewed) return;
        if (!_isScrewdriverTouching) return;

        // Count up while screwdriver is touching
        _currentProgress += Time.deltaTime;

        if (_currentProgress >= unscrewDuration)
        {
            Unscrew();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isUnscrewed) return;

        if (other.CompareTag("Screwdriver"))
        {
            _isScrewdriverTouching = true;
            Debug.Log($"[ScrewController] Screwdriver touching {name}, progress: {_currentProgress:F1}/{unscrewDuration}");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Screwdriver"))
        {
            _isScrewdriverTouching = false;
            Debug.Log($"[ScrewController] Screwdriver left {name}, progress paused at {_currentProgress:F1}/{unscrewDuration}");
        }
    }

    private void Unscrew()
    {
        _isUnscrewed = true;
        _isScrewdriverTouching = false;

        if (_spriteRenderer != null && unscrewedSprite != null)
            _spriteRenderer.sprite = unscrewedSprite;

        Debug.Log($"[ScrewController] {name} unscrewed!");
        OnUnscrewed?.Invoke(this);
    }

    /// <summary>
    /// Re-screw this screw (reverse process).
    /// Called when cover is put back.
    /// </summary>
    public void Rescrew()
    {
        _isUnscrewed = false;
        _currentProgress = 0f;
        _isScrewdriverTouching = false;

        if (_spriteRenderer != null && screwedSprite != null)
            _spriteRenderer.sprite = screwedSprite;

        Debug.Log($"[ScrewController] {name} re-screwed");
        OnScrewed?.Invoke(this);
    }

    public bool IsUnscrewed() => _isUnscrewed;
    public float GetProgress() => _currentProgress;
    public float GetDuration() => unscrewDuration;
}