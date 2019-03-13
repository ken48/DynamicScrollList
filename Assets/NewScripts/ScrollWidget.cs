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
    Vector2 _velocity;
    Vector2 _position;
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

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position,
            eventData.pressEventCamera, out Vector2 localPos);

        Vector2 mask = _axis == RectTransform.Axis.Horizontal ? Vector2.right : Vector2.up;
        Vector2 localDelta = Vector2.Scale(localPos - _prevPos, mask);
        if (localDelta.sqrMagnitude > _velocity.sqrMagnitude)
            _velocity = localDelta;

        _prevPos = localPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
    }

    void LateUpdate()
    {
        if (_velocity.sqrMagnitude < 0.001f)
            return;

        const float speedCoef = 27f;
        Vector2 delta = _velocity * speedCoef * Time.unscaledDeltaTime;
        _velocity *= 0.92f;

        onScroll?.Invoke(_axis == RectTransform.Axis.Horizontal ? delta.x : delta.y);
        _position += delta;
    }
}
