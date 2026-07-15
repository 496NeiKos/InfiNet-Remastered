using UnityEngine;

/// <summary>
/// Wire stripper drag behavior for COC II network cable simulation.
/// Drag from storage to a cable end to expose wires (fresh) or reset wires (stripped).
/// Returns to storage after each use — player must re-fetch.
///
/// Attach to: WireStripper (world-space SpriteRenderer with Collider2D in HardwareStorage).
/// </summary>
public class NetworkWireStripperDrag : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Radius to search for cable ends when dropped.")]
    [SerializeField] private float dropRadius = 1.5f;

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

        NetworkCableEndController hit = FindCableEndAtPosition(transform.position);
        if (hit != null)
        {
            if (!hit.IsStripped)
                hit.Expose();
            else
                hit.ResetEnd();
        }

        ReturnToStorage();
    }

    private void ReturnToStorage()
    {
        transform.position = _homePosition;
    }

    private NetworkCableEndController FindCableEndAtPosition(Vector3 worldPos)
    {
        // Find all cable ends in the scene and return the closest one within dropRadius
        NetworkCableEndController[] ends = FindObjectsByType<NetworkCableEndController>(FindObjectsSortMode.None);
        NetworkCableEndController closest = null;
        float bestDist = dropRadius;

        foreach (var end in ends)
        {
            float dist = Vector3.Distance(worldPos, end.transform.position);
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
