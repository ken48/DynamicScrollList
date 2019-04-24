using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ScrollWidget : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public event Action<Vector2> onScroll;

    public DynamicScrollDescription.Axis axis => _axis;

    [SerializeField]
    DynamicScrollDescription.Axis _axis;
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
        if (GetLocalPosition(eventData, out _startPosition))
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
        float edgesDeltaAxis = DynamicScrollHelpers.GetVectorComponent(edgesDelta, _axis);
        float edgesDeltaAxisAbs = Mathf.Abs(edgesDeltaAxis);
        if (_isDragging)
        {
            float viewportLengthAxis = DynamicScrollHelpers.GetVectorComponent(_viewport.rect.size, _axis);
            _elasticity = 1f - Mathf.Clamp01(edgesDeltaAxisAbs / viewportLengthAxis);
        }

        _edgeDelta = DynamicScrollDescription.AxisMasks[_axis] * edgesDeltaAxis * _elasticityCoef;
        if (edgesDeltaAxisAbs > 0f)
            _inertiaVelocity = Vector2.zero;
    }

    void OnScroll(Vector2 delta)
    {
        if (CheckVectorMagnitude(delta))
            onScroll?.Invoke(delta);
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

    //
    // Helpers
    //

    Vector2 GetDeltaPosition(PointerEventData eventData)
    {
        GetLocalPosition(eventData, out Vector2 finishPosition);
        Vector2 deltaAxis = (finishPosition - _startPosition) * DynamicScrollDescription.AxisMasks[_axis];
        _startPosition = finishPosition;

        if (_elasticity < 1f)
            deltaAxis *= _elasticity * _elasticityCoef;
        return deltaAxis;
    }

    bool GetLocalPosition(PointerEventData eventData, out Vector2 position)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport,
            eventData.position, eventData.pressEventCamera, out position);
    }

    static bool CheckVectorMagnitude(Vector2 vector)
    {
        return vector.sqrMagnitude >= 1e-6;
    }
}
