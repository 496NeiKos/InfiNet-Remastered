using UnityEngine;

/// <summary>
/// Controls the system unit cover panel.
/// Checks if all screws are unscrewed before allowing cover removal.
/// Cover slides to the right when clicked (if unlocked).
/// Click again to slide back and re-screw.
/// </summary>
public class CoverController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SystemUnitController systemUnitController;

    [Header("Screws (assign all 4)")]
    [SerializeField] private ScrewController screw1;
    [SerializeField] private ScrewController screw2;
    [SerializeField] private ScrewController screw3;
    [SerializeField] private ScrewController screw4;

    [Header("Slide Settings")]
    [SerializeField] private float slideDistance = 3f;
    [SerializeField] private float slideSpeed = 5f;

    private bool _isOpen = false;
    private Vector3 _closedPosition;
    private Vector3 _openPosition;
    private Vector3 _targetPosition;
    private bool _isSliding = false;

    private void Start()
    {
        _closedPosition = transform.localPosition;
        _openPosition = _closedPosition + new Vector3(slideDistance, 0f, 0f);
        _targetPosition = _closedPosition;
    }

    private void Update()
    {
        if (!_isSliding) return;

        // Smoothly slide cover to target position
        transform.localPosition = Vector3.MoveTowards(
            transform.localPosition,
            _targetPosition,
            slideSpeed * Time.deltaTime
        );

        // Check if reached target
        if (Vector3.Distance(transform.localPosition, _targetPosition) < 0.01f)
        {
            transform.localPosition = _targetPosition;
            _isSliding = false;

            if (_isOpen)
            {
                // Cover fully opened → reveal hardware
                if (systemUnitController != null)
                    systemUnitController.RemoveCover();
            }
        }
    }

    /// <summary>
    /// Called when user clicks the cover.
    /// Wire this to a click detector or call from DetailViewManager.
    /// </summary>
    public void OnCoverClicked()
    {
        if (_isOpen)
        {
            CloseCover();
        }
        else
        {
            TryOpenCover();
        }
    }

    private void TryOpenCover()
    {
        if (!AllScrewsUnscrewed())
        {
            Debug.Log("[CoverController] Cannot open: not all screws are unscrewed");
            return;
        }

        _isOpen = true;
        _targetPosition = _openPosition;
        _isSliding = true;

        Debug.Log("[CoverController] Opening cover...");
    }

    private void CloseCover()
    {
        _isOpen = false;
        _targetPosition = _closedPosition;
        _isSliding = true;

        // Hide hardware components while cover slides back
        if (systemUnitController != null)
            systemUnitController.AttachCover();

        // Re-screw all screws
        if (screw1 != null) screw1.Rescrew();
        if (screw2 != null) screw2.Rescrew();
        if (screw3 != null) screw3.Rescrew();
        if (screw4 != null) screw4.Rescrew();

        Debug.Log("[CoverController] Closing cover and re-screwing...");
    }

    private bool AllScrewsUnscrewed()
    {
        if (screw1 == null || screw2 == null || screw3 == null || screw4 == null)
        {
            Debug.LogError("[CoverController] Not all screws are assigned!");
            return false;
        }

        return screw1.IsUnscrewed()
            && screw2.IsUnscrewed()
            && screw3.IsUnscrewed()
            && screw4.IsUnscrewed();
    }

    public bool IsOpen() => _isOpen;
    public bool AreAllScrewsUnscrewed() => AllScrewsUnscrewed();
}