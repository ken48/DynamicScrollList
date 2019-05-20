using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DynamicScroll.Internal
{
    internal class Scroller : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public event Action<float> onScroll;

        RectTransform _viewport;
        Axis _axis;
        float _speedCoef;
        float _inertiaCoef;
        float _elasticityCoef;
        float _startPosition;
        float _lastDelta;
        float _inertia;
        float _elasticity;
        bool _isDragging;

        public void Init(RectTransform viewport, Axis axis, float speedCoef, float inertiaCoef, float elasticityCoef)
        {
            _viewport = viewport;
            _speedCoef = speedCoef;
            _elasticityCoef = elasticityCoef;
            _inertiaCoef = inertiaCoef;
            _axis = axis;
        }

        public void SetEdgeDelta(float edgeDelta)
        {
            if (_isDragging)
            {
                float viewportSizeFloat = Helpers.GetVectorComponent(_viewport.rect.size, _axis);
                _elasticity = 1f - Mathf.Clamp01(Mathf.Abs(edgeDelta) / viewportSizeFloat);
            }

            if (!Helpers.IsZeroValue(edgeDelta))
                _inertia = edgeDelta * _elasticityCoef;
        }

        void OnScroll(float delta)
        {
            onScroll?.Invoke(delta);
        }

        void LateUpdate()
        {
            if (_isDragging || Helpers.IsZeroValue(_inertia))
                return;

            float dt = Time.unscaledDeltaTime;
            float timeStep = _speedCoef * dt;
            float delta = _inertia * timeStep;
            _inertia *= 1f - Mathf.Clamp01(dt * _inertiaCoef);

            OnScroll(delta);
        }

        //
        // Drag handlers
        //

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (GetLocalPosition(eventData, out Vector2 startPosition))
            {
                _startPosition = Helpers.GetVectorComponent(startPosition, _axis);
                _isDragging = true;
                _inertia = 0f;
                _elasticity = 1f;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;

            float delta = GetDeltaPosition(eventData);
            if (!Helpers.IsZeroValue(delta))
                _lastDelta = delta;

            OnScroll(delta);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;

            float delta = GetDeltaPosition(eventData);
            _inertia = _lastDelta + delta;
            _isDragging = false;

            OnScroll(delta);
        }

        //
        // Helpers
        //

        float GetDeltaPosition(PointerEventData eventData)
        {
            GetLocalPosition(eventData, out Vector2 finishPositionVector);
            float finishPosition = Helpers.GetVectorComponent(finishPositionVector, _axis);
            float delta = finishPosition - _startPosition;
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
