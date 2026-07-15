using UnityEngine;
using UnityEngine.EventSystems;

// Attach to the WorkspaceRoot child object alongside its BoxCollider2D.
// Forwards all drag events up to the parent's DragPrefab, because Unity's
// Physics2DRaycaster executes events on the hit GameObject directly and does
// not automatically bubble them to parent handlers.
public class WorkspaceProxyDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private DragPrefab _parentDrag;

    private void Awake()
    {
        _parentDrag = GetComponentInParent<DragPrefab>();
    }

    public void OnBeginDrag(PointerEventData eventData) => _parentDrag?.OnBeginDrag(eventData);
    public void OnDrag(PointerEventData eventData)      => _parentDrag?.OnDrag(eventData);
    public void OnEndDrag(PointerEventData eventData)   => _parentDrag?.OnEndDrag(eventData);
}
