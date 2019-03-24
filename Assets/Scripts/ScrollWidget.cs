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
    [SerializeField]
    float _speedCoef = 12f;
    [SerializeField]
    float _inertiaCoef = 3.5f;
    [SerializeField]
    float _elasticityCoef = 0.5f;

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
        _edgesDelta = mask * edgesDelta * _elasticityCoef;
        if (!Mathf.Approximately(edgesDelta, 0f))
            _inertiaVelocity = Vector2.zero;
    }

    void LateUpdate()
    {
        // Todo: застревает на низких фпс контент, если резко крутнуть за пределы вьюпорта
        // (при этом эджес и инерциа велосити == 0)

        if (_isDragging || (!CheckVectorMagnitude(_inertiaVelocity) && !CheckVectorMagnitude(_edgesDelta)))
            return;

        float dt = Time.unscaledDeltaTime;
        Vector2 totalVelocity = _inertiaVelocity + _edgesDelta;
        Vector2 delta = totalVelocity * _speedCoef * dt;
        _inertiaVelocity *= 1f - Mathf.Clamp01(dt * _inertiaCoef);

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
            delta *= _elasticity * _elasticityCoef;
        return delta;
    }

    static bool CheckVectorMagnitude(Vector2 v)
    {
        return v.sqrMagnitude >= 0.0001f;
    }
}
