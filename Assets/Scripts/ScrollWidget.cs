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

    Vector2 _startPosition;
    Vector2 _finishPosition;
    Vector2 _lastDelta;
    bool _isDragging;
    Vector2 _velocity;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position,
            eventData.pressEventCamera, out _startPosition))
        {
            _isDragging = true;
            _velocity = Vector2.zero;

            Debug.Log(Time.frameCount + " BDzero");

            // _elasticityCoef = 1f;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position,
            eventData.pressEventCamera, out Vector2 finishPosition);

        Vector2 mask = _axis == RectTransform.Axis.Horizontal ? Vector2.right : Vector2.up;
        Vector2 delta = Vector2.Scale(finishPosition - _startPosition, mask);

        // if (_elasticityCoef < 1f)
        //     localDelta *= _elasticityCoef * 0.01f;
        OnScroll(delta);

        Debug.Log(Time.frameCount + " KUKU " + delta.y);

        _lastDelta = delta;
        _startPosition = finishPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log(Time.frameCount + " ED " + _lastDelta.y);

        _velocity = _lastDelta;
        _isDragging = false;
    }

    public void SetEdgesDelta(float edgesDelta)
    {
        if (_isDragging)
        {
            // _elasticityCoef = 1f - Mathf.Clamp01(Mathf.Abs(edgesDelta) / _viewport.rect.height);
            //
            // Debug.Log(_elasticityCoef);
        }
        else
        {
            Vector2 mask = _axis == RectTransform.Axis.Horizontal ? Vector2.right : Vector2.up;
            _velocity = mask * edgesDelta;

            Debug.Log(Time.frameCount + " EDGE " + _velocity.y);
        }
    }

    void LateUpdate()
    {
        if (_velocity.sqrMagnitude <= 0f)
            return;

        // Todo: max speed restriction
        const float speedCoef = 25f;
        const float inertiaCoef = 5.5f;

        float dt = Time.unscaledDeltaTime;
        Vector2 delta = _velocity * speedCoef * dt;
        _velocity *= 1f - Mathf.Clamp01(dt * inertiaCoef);

        OnScroll(delta);
    }

    void OnScroll(Vector2 delta)
    {
        onScroll?.Invoke(_axis == RectTransform.Axis.Horizontal ? delta.x : delta.y);
        _finishPosition += delta;
    }
}
