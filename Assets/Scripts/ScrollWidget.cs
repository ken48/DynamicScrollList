using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ScrollWidget : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public event Action<Vector2> onScroll;

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

    public void SetEdgeDelta(Vector2 edgeDelta)
    {
        if (_isDragging)
        {
            Vector2 viewportSize = _viewport.rect.size;
            float normalizedEdgeDeltaX = Mathf.Abs(edgeDelta.x / viewportSize.x);
            float normalizedEdgeDeltaY = Mathf.Abs(edgeDelta.y / viewportSize.y);
            _elasticity = 1f - Mathf.Clamp01(new Vector2(normalizedEdgeDeltaX, normalizedEdgeDeltaY).magnitude);
        }

        if (edgeDelta.sqrMagnitude > 0f)
            _inertiaVelocity = edgeDelta * _elasticityCoef;
    }

    void OnScroll(Vector2 delta)
    {
        onScroll?.Invoke(delta);
    }

    void LateUpdate()
    {
        if (_isDragging || !CheckVectorMagnitude(_inertiaVelocity))
            return;

        float dt = Time.unscaledDeltaTime;
        float timeStep = _speedCoef * dt;
        Vector2 delta = _inertiaVelocity * timeStep;
        _inertiaVelocity *= 1f - Mathf.Clamp01(dt * _inertiaCoef);

        OnScroll(delta);
    }

    //
    // Helpers
    //

    Vector2 GetDeltaPosition(PointerEventData eventData)
    {
        GetLocalPosition(eventData, out Vector2 finishPosition);
        Vector2 delta = finishPosition - _startPosition;
        _startPosition = finishPosition;

        if (_elasticity < 1f)
            delta *= _elasticity * _elasticityCoef;
        return delta;
    }

    bool GetLocalPosition(PointerEventData eventData, out Vector2 position)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport,
            eventData.position, eventData.pressEventCamera, out position);
    }

    static bool CheckVectorMagnitude(Vector2 vector)
    {
        return vector.sqrMagnitude >= 1e-6f;
    }
}
