using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the system unit side cover panel.
/// Slide right gesture (≥ dragThreshold px) opens the cover; slide left closes it.
/// Screws can live anywhere in the scene (assign via inspector — they are now under SystemUnitBack).
/// Opening: hardware shown immediately, interactable after slide finishes.
/// Closing: hardware stays visible during slide, hidden after slide finishes.
/// </summary>
public class CoverController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SystemUnitController systemUnitController;

    [Header("Screws (assign all 4 — now located under SystemUnitBack)")]
    [SerializeField] private ScrewController screw1;
    [SerializeField] private ScrewController screw2;
    [SerializeField] private ScrewController screw3;
    [SerializeField] private ScrewController screw4;

    [Header("Slide Settings")]
    [SerializeField] private float slideDistance = 3f;
    [SerializeField] private float slideSpeed = 5f;
    [SerializeField] private float dragThreshold = 100f;

    private bool _isOpen = false;
    private bool _isSliding = false;
    private bool _isOpening = false;
    private Vector3 _closedPosition;
    private Vector3 _openPosition;
    private Vector3 _targetPosition;

    private bool _isPressed = false;
    private Vector2 _pressStartScreenPos;

    public bool IsSliding => _isSliding;

    private void Start()
    {
        _closedPosition = transform.localPosition;
        _openPosition = _closedPosition + new Vector3(slideDistance, 0f, 0f);
        _targetPosition = _closedPosition;
    }

    private void Update()
    {
        if (_isSliding)
        {
            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition,
                _targetPosition,
                slideSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.localPosition, _targetPosition) < 0.01f)
            {
                transform.localPosition = _targetPosition;
                _isSliding = false;

                if (_isOpening)
                {
                    Debug.Log("[CoverController] Cover fully open.");
                    ActivityLogManager.Log("Side cover removed", ActivityLogManager.EntryType.Remove);
                    NCIITaskListManager.CheckConditions();
                }
                else
                {
                    if (systemUnitController != null)
                        systemUnitController.AttachCover();
                    Debug.Log("[CoverController] Cover fully closed, hardware hidden.");
                    ActivityLogManager.Log("Side cover attached", ActivityLogManager.EntryType.Install);
                    NCIITaskListManager.CheckConditions();
                }
            }
        }

        if (GameManager.Instance == null || !GameManager.Instance.IsEditorOpen) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame && IsMouseOver())
        {
            _isPressed = true;
            _pressStartScreenPos = mouse.position.ReadValue();
        }

        if (_isPressed && mouse.leftButton.wasReleasedThisFrame)
        {
            _isPressed = false;
            Vector2 delta = mouse.position.ReadValue() - _pressStartScreenPos;
            if (Mathf.Abs(delta.x) >= dragThreshold)
            {
                if (delta.x > 0f) TryOpenCover();
                else CloseCover();
            }
        }

        if (_isPressed && !mouse.leftButton.isPressed)
            _isPressed = false;
    }

    private void TryOpenCover()
    {
        if (_isSliding || _isOpen) return;

        if (!AllScrewsUnscrewed())
        {
            Debug.Log("[CoverController] Cannot open: not all screws are unscrewed.");
            return;
        }

        _isOpen = true;
        _isOpening = true;
        _targetPosition = _openPosition;
        _isSliding = true;

        if (systemUnitController != null)
            systemUnitController.RemoveCover();

        Debug.Log("[CoverController] Opening cover...");
    }

    private void CloseCover()
    {
        if (_isSliding || !_isOpen) return;

        _isOpen = false;
        _isOpening = false;
        _targetPosition = _closedPosition;
        _isSliding = true;

        Debug.Log("[CoverController] Closing cover...");
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

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        foreach (Collider2D col in GetComponents<Collider2D>())
            if (col.OverlapPoint(mouseWorld)) return true;
        return false;
    }

    public bool IsOpen() => _isOpen;

    public bool AreAllScrewsUnscrewed() => AllScrewsUnscrewed();

    public bool AreAllScrewsScrewed()
    {
        if (screw1 == null || screw2 == null || screw3 == null || screw4 == null) return false;
        return screw1.IsScrewed() && screw2.IsScrewed() && screw3.IsScrewed() && screw4.IsScrewed();
    }
}
