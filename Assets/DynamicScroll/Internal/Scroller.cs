using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DynamicScroll.Internal
{
    internal class Scroller : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public event Action<Vector2> onScroll;

        RectTransform _viewport;
        float _speedCoef;
        float _inertiaCoef;
        float _elasticityCoef;
        Vector2 _startPosition;
        Vector2 _lastDelta;
        Vector2 _inertiaVelocity;
        Vector2 _moveMask;
        bool _isDragging;
        float _elasticity;

        // Todo: remove dependency moveMask
        public void Init(RectTransform viewport, float speedCoef, float inertiaCoef, float elasticityCoef, Vector2 moveMask)
        {
            _viewport = viewport;
            _speedCoef = speedCoef;
            _elasticityCoef = elasticityCoef;
            _inertiaCoef = inertiaCoef;
            _moveMask = moveMask;
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
            if (_isDragging || !DynamicScrollHelpers.CheckVectorMagnitude(_inertiaVelocity))
                return;

            float dt = Time.unscaledDeltaTime;
            float timeStep = _speedCoef * dt;
            Vector2 delta = _inertiaVelocity * timeStep;
            _inertiaVelocity *= 1f - Mathf.Clamp01(dt * _inertiaCoef);

            OnScroll(delta);
        }

        //
        // Drag handlers
        //

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
            if (DynamicScrollHelpers.CheckVectorMagnitude(delta))
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

        //
        // Helpers
        //

        Vector2 GetDeltaPosition(PointerEventData eventData)
        {
            GetLocalPosition(eventData, out Vector2 finishPosition);
            Vector2 delta = (finishPosition - _startPosition) * _moveMask;
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
    }
}
