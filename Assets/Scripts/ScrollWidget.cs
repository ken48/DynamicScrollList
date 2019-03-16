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
    Vector2 _lastDelta;
    bool _isDragging;
    float _elasticityCoef;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position,
            eventData.pressEventCamera, out _prevPos))
        {
            _isDragging = true;
            _velocity = Vector2.zero;
            _lastDelta = Vector2.zero;
            _elasticityCoef = 1f;
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

        if (_elasticityCoef < 1f)
            localDelta *= _elasticityCoef * 0.7f;
        OnScroll(localDelta);

        _prevPos = localPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _velocity = _lastDelta;

        _isDragging = false;
    }

    public void SetEdgesDelta(float edgesDelta)
    {
        Vector2 mask = _axis == RectTransform.Axis.Horizontal ? Vector2.right : Vector2.up;

        if (!_isDragging)
            _velocity = mask * edgesDelta;
        else
            _elasticityCoef = 1f - Mathf.Clamp01(Mathf.Abs(edgesDelta) / _viewport.rect.height);
    }

    void LateUpdate()
    {
        const float minSpeedSqr = 0.001f;
        const float speedCoef = 25f;
        const float maxMagnitude = 110f; // Todo: dependency from canvas resolution
        const float inertiaCoef = 5.5f;

        if (_velocity.sqrMagnitude < minSpeedSqr)
            return;

        float dt = Time.unscaledDeltaTime;
        Vector2 delta = _velocity * speedCoef * Time.unscaledDeltaTime;
        delta = Vector2.ClampMagnitude(delta, maxMagnitude);
        _velocity *= 1f - Mathf.Clamp01(dt * inertiaCoef);

        OnScroll(delta);
    }

    void OnScroll(Vector2 delta)
    {
        onScroll?.Invoke(_axis == RectTransform.Axis.Horizontal ? delta.x : delta.y);
        _position += delta;
    }
}
