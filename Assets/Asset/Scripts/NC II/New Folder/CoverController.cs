using UnityEngine;

/// <summary>
/// Controls the system unit cover panel.
/// Opens if all screws unscrewed. Can reopen/close freely while unscrewed.
/// Does NOT auto-rescrew on close — user must manually screw each one.
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

        transform.localPosition = Vector3.MoveTowards(
            transform.localPosition,
            _targetPosition,
            slideSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.localPosition, _targetPosition) < 0.01f)
        {
            transform.localPosition = _targetPosition;
            _isSliding = false;

            if (_isOpen)
            {
                if (systemUnitController != null)
                    systemUnitController.RemoveCover();
            }
        }
    }

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

        // Hide hardware components
        if (systemUnitController != null)
            systemUnitController.AttachCover();

        // ✅ Do NOT re-screw. Screws stay in whatever state they are.
        // User must manually drag screws from hardware area to re-screw.

        Debug.Log("[CoverController] Closing cover (screws unchanged)");
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
}