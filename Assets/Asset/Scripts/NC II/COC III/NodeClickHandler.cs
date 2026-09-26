using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class NodeClickHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
{
    public Action onLeftClick;
    public Action onRightClick;
    public Action<Vector2> onRightClickWithPos;

    // Claims pointer press so OnPointerClick fires regardless of which child Graphic was hit.
    public void OnPointerDown(PointerEventData eventData) { }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            onLeftClick?.Invoke();
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            onRightClick?.Invoke();
            onRightClickWithPos?.Invoke(eventData.position);
        }
    }
}
