using UnityEngine;

/// <summary>
/// Wire stripper drag behavior for COC II — world-space version (stored in scene).
/// Drag to a cable end body or head to strip the jacket and expose the 8 inner wires.
///
/// Drop targets and results:
///   - Body or head area, cable intact  → Expose() (strip the jacket)
///   - Body area, already stripped      → Feedback: "Cable jacket has already been stripped."
///   - Wires area, already stripped     → Feedback: "Cable jacket stripped, internal wires shown."
///
/// Returns to storage after each use.
/// </summary>
public class NetworkWireStripperDrag : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Radius to detect the cable end body/head zone when dropped.")]
    [SerializeField] private float dropRadius = 1.5f;
    [Tooltip("Radius to detect the wires container zone for the 'already stripped' feedback.")]
    [SerializeField] private float wiresRadius = 1.5f;

    private Vector3 _homePosition;
    private bool _isDragging;
    private Vector3 _dragOffset;
    private Camera _cam;

    private void Start()
    {
        _homePosition = transform.position;
        _cam = Camera.main;
    }

    private void OnMouseDown()
    {
        _isDragging = true;
        _dragOffset = transform.position - GetMouseWorld();
    }

    private void OnMouseDrag()
    {
        if (!_isDragging) return;
        transform.position = GetMouseWorld() + _dragOffset;
    }

    private void OnMouseUp()
    {
        if (!_isDragging) return;
        _isDragging = false;

        Vector3 dropPos = transform.position;

        // Check body/head zone first (root position covers both since they're adjacent).
        NetworkCableEndController bodyHit = FindNearestCableEnd(dropPos, e => e.BodyWorldPosition, dropRadius);

        if (bodyHit != null)
        {
            if (!bodyHit.IsStripped)
            {
                bodyHit.Expose();
                ActivityLogManager.Log($"Cable jacket stripped — {bodyHit.name}", ActivityLogManager.EntryType.Action);
            }
            else
            {
                CrimpingFeedbackPopup.Instance?.ShowMessage("Cable jacket has already been stripped.");
            }

            ReturnToStorage();
            return;
        }

        // Check wires zone — only reachable when already stripped.
        NetworkCableEndController wiresHit = FindNearestCableEnd(dropPos, e => e.WiresWorldPosition, wiresRadius);

        if (wiresHit != null && wiresHit.IsStripped)
        {
            CrimpingFeedbackPopup.Instance?.ShowMessage("Cable jacket stripped, internal wires shown.");
        }

        ReturnToStorage();
    }

    private void ReturnToStorage()
    {
        transform.position = _homePosition;
    }

    private NetworkCableEndController FindNearestCableEnd(
        Vector3 worldPos,
        System.Func<NetworkCableEndController, Vector3> positionSelector,
        float radius)
    {
        NetworkCableEndController[] ends = FindObjectsByType<NetworkCableEndController>(FindObjectsSortMode.None);
        NetworkCableEndController closest = null;
        float bestDist = radius;

        foreach (var end in ends)
        {
            float dist = Vector3.Distance(worldPos, positionSelector(end));
            if (dist < bestDist)
            {
                bestDist = dist;
                closest = end;
            }
        }

        return closest;
    }

    private Vector3 GetMouseWorld()
    {
        Vector3 mp = Input.mousePosition;
        mp.z = Mathf.Abs(_cam.transform.position.z);
        return _cam.ScreenToWorldPoint(mp);
    }
}
