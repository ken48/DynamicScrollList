﻿using System;
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
    Vector2 _lastDelta;
    bool _isDragging;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position,
            eventData.pressEventCamera, out _prevPos))
        {
            _isDragging = true;
            _velocity = Vector2.zero;
            _lastDelta = Vector2.zero;
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
            _lastDelta = localDelta;

        OnScroll(localDelta);

        _prevPos = localPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _velocity = _lastDelta;

        _isDragging = false;
    }

    void LateUpdate()
    {
        const float minSpeedSqr = 0.001f;
        const float speedCoef = 25f;
        const float maxMagnitude = 110f; // Todo: dependency from canvas resolution
        const float inertiaCoef = 0.94f;

        if (_velocity.sqrMagnitude < minSpeedSqr)
            return;

        Vector2 delta = _velocity * speedCoef * Time.unscaledDeltaTime;
        delta = Vector2.ClampMagnitude(delta, maxMagnitude);
        _velocity *= inertiaCoef;

        OnScroll(delta);
    }

    void OnScroll(Vector2 delta)
    {
        onScroll?.Invoke(_axis == RectTransform.Axis.Horizontal ? delta.x : delta.y);
        _position += delta;
    }
}
