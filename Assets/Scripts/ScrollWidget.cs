using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ScrollWidget : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    enum Axis
    {
        Horizontal,
        Vertical,
    }

    static readonly Dictionary<Axis, Vector2> AxisMasks = new Dictionary<Axis, Vector2>
    {
        { Axis.Horizontal, Vector2.right },
        { Axis.Vertical, Vector2.up },
    };

    public event Action<Vector2> onScroll;

    [SerializeField]
    Axis _axis;
    [SerializeField]
    RectTransform _viewport;
    [SerializeField]
    float _speedCoef;
    [SerializeField]
    float _inertiaCoef;
    [SerializeField]
    float _elasticityCoef;

    Vector2 _startPosition;
    Vector2 _lastDelta;
    Vector2 _inertiaVelocity;
    Vector2 _edgeDelta;
    bool _isDragging;
    float _elasticity;

    void Reset()
    {
        _speedCoef = 15f;
        _inertiaCoef = 3f;
        _elasticityCoef = 0.5f;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position,
            eventData.pressEventCamera, out _startPosition))
        {
            _isDragging = true;
            _inertiaVelocity = Vector2.zero;
            _edgeDelta = Vector2.zero;
            _elasticity = 1f;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        Vector2 delta = GetDeltaPosition(eventData);
        if (CheckVectorMagnitude(delta))
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

    public void SetEdgeDelta(Vector2 edgesDelta)
    {
        Vector2 edgesDeltaAxis = Vector2.Scale(edgesDelta, AxisMasks[_axis]);
        float edgesDeltaAxisSqr = edgesDeltaAxis.sqrMagnitude;
        if (_isDragging)
        {
            float viewportLengthSqr = Vector2.Scale(_viewport.rect.size, AxisMasks[_axis]).sqrMagnitude;
            _elasticity = 1f - Mathf.Clamp01(edgesDeltaAxisSqr / viewportLengthSqr);
        }

        _edgeDelta = edgesDeltaAxis * _elasticityCoef;
        if (edgesDeltaAxisSqr > 0f)
            _inertiaVelocity = Vector2.zero;
    }

    public Vector2 GetAxisMask()
    {
        return AxisMasks[_axis];
    }

    void LateUpdate()
    {
        if (_isDragging || (!CheckVectorMagnitude(_inertiaVelocity) && !CheckVectorMagnitude(_edgeDelta)))
            return;

        float dt = Time.unscaledDeltaTime;
        Vector2 totalVelocity = _inertiaVelocity + _edgeDelta;
        Vector2 delta = totalVelocity * _speedCoef * dt;
        _inertiaVelocity *= 1f - Mathf.Clamp01(dt * _inertiaCoef);

        OnScroll(delta);
    }

    void OnScroll(Vector2 delta)
    {
        if (CheckVectorMagnitude(delta))
            onScroll?.Invoke(delta);
    }

    Vector2 GetDeltaPosition(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, eventData.pressEventCamera,
            out Vector2 finishPosition);
        var delta = Vector2.Scale(finishPosition - _startPosition, AxisMasks[_axis]);
        _startPosition = finishPosition;

        if (_elasticity < 1f)
            delta *= _elasticity * _elasticityCoef;
        return delta;
    }

    static bool CheckVectorMagnitude(Vector2 v)
    {
        return v.sqrMagnitude >= 1e-6;
    }
}
