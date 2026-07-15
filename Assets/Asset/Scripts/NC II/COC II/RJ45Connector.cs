using UnityEngine;

/// <summary>
/// Draggable RJ45 connector stored in HardwareStorage.
/// Player drags it to a cable end's RJ45 slot to install it.
/// Returns to storage if dropped outside any valid slot.
///
/// Attach to: the RJ45 object in HardwareStorage (world-space SpriteRenderer with Collider2D).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RJ45Connector : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Radius to find a CableEnd to snap to.")]
    [SerializeField] private float snapRadius = 1.5f;

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

        NetworkCableEndController target = FindEligibleCableEnd(transform.position);
        if (target != null)
        {
            target.InstallRJ45();
            ReturnToStorage();
        }
        else
        {
            ReturnToStorage();
        }
    }

    private void ReturnToStorage()
    {
        transform.position = _homePosition;
    }

    private NetworkCableEndController FindEligibleCableEnd(Vector3 worldPos)
    {
        NetworkCableEndController[] ends = FindObjectsByType<NetworkCableEndController>(FindObjectsSortMode.None);
        NetworkCableEndController closest = null;
        float bestDist = snapRadius;

        foreach (var end in ends)
        {
            // Only install if wires are exposed and RJ45 is not already installed
            if (!end.IsStripped || end.IsRJ45Installed) continue;

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
