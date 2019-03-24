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

    Vector2 _startPosition;
    Vector2 _finishPosition;
    Vector2 _lastDelta;
    bool _isDragging;
    Vector2 _inertiaVelocity;
    Vector2 _edgesDelta;
    float _elasticity;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position,
            eventData.pressEventCamera, out _startPosition))
        {
            _isDragging = true;
            _inertiaVelocity = Vector2.zero;
            _edgesDelta = Vector2.zero;
            _elasticity = 1f;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        Vector2 delta = GetDeltaPosition(eventData);
        _lastDelta = delta;

        OnScroll(delta);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        Vector2 delta = GetDeltaPosition(eventData);
        _inertiaVelocity = _lastDelta + delta;
        _isDragging = false;

        OnScroll(delta);
    }

    public void SetEdgesDelta(float edgesDelta)
    {
        if (_isDragging)
            _elasticity = 1f - Mathf.Clamp01(Mathf.Abs(edgesDelta) / _viewport.rect.height);

        Vector2 mask = _axis == RectTransform.Axis.Horizontal ? Vector2.right : Vector2.up;
        _edgesDelta = mask * edgesDelta;
    }

    void LateUpdate()
    {
        if (_isDragging || (!CheckVectorMagnitude(_inertiaVelocity) && !CheckVectorMagnitude(_edgesDelta)))
            return;

        // Todo: max speed restriction
        const float speedCoef = 13f;
        const float inertiaCoef = 5f;

        float dt = Time.unscaledDeltaTime;
        Vector2 totalVelocity = _inertiaVelocity + _edgesDelta;
        Vector2 delta = totalVelocity * speedCoef * dt;
        _inertiaVelocity *= 1f - Mathf.Clamp01(dt * inertiaCoef);

        OnScroll(delta);
    }

    void OnScroll(Vector2 delta)
    {
        if (!CheckVectorMagnitude(delta))
            return;

        onScroll?.Invoke(_axis == RectTransform.Axis.Horizontal ? delta.x : delta.y);
        _finishPosition += delta;
    }

    Vector2 GetDeltaPosition(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, eventData.pressEventCamera,
            out Vector2 finishPosition);
        Vector2 mask = _axis == RectTransform.Axis.Horizontal ? Vector2.right : Vector2.up;
        Vector2 delta = Vector2.Scale(finishPosition - _startPosition, mask);
        _startPosition = finishPosition;

        if (_elasticity < 1f)
            delta *= _elasticity * 0.5f;
        return delta;
    }

    static bool CheckVectorMagnitude(Vector2 v)
    {
        return v.sqrMagnitude >= 0.0001f;
    }
}
