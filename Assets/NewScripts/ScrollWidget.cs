using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ScrollWidget : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public event Action<float> onScroll;

    [SerializeField] 
    RectTransform.Axis _axis;
    [SerializeField] 
    RectTransform _viewport;

    Vector2 _prevPos;
    bool _isDragging;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position,
            eventData.pressEventCamera, out _prevPos))
        {
            _isDragging = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position,
            eventData.pressEventCamera, out localPos);

        Vector2 localDelta = localPos - _prevPos;
        _prevPos = localPos;
        OnScroll(_axis == RectTransform.Axis.Horizontal ? localDelta.x : localDelta.y);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
    }

    void OnScroll(float delta)
    {
        Vector2 mask = _axis == RectTransform.Axis.Horizontal ? Vector2.right : Vector2.up;
        onScroll?.Invoke(delta);
    }
}
