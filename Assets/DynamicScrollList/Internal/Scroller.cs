using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DynamicScroll.Internal
{
    internal class Scroller : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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
        Component _parentHandler;

        public void Init(RectTransform viewport, Axis axis, float speedCoef, float inertiaCoef, float elasticityCoef)
        {
            _viewport = viewport;
            _speedCoef = speedCoef;
            _elasticityCoef = elasticityCoef;
            _inertiaCoef = inertiaCoef;
            _axis = axis;
        }

        public void SetEdgeDelta(float edgeDelta, bool immediate)
        {
            if (_isDragging)
            {
                float viewportSizeFloat = Helpers.GetVectorComponent(_viewport.rect.size, _axis);
                _elasticity = 1f - Mathf.Clamp01(Mathf.Abs(edgeDelta) / viewportSizeFloat);
            }
            else if (!Helpers.IsZeroValue(edgeDelta))
            {
                if (immediate)
                    OnScroll(edgeDelta);
                else
                    _inertia = edgeDelta; // Todo: make elasticity inertia more gentle
            }
        }

        public void StopScrolling()
        {
            if (!Helpers.IsZeroValue(_inertia))
                OnScroll(_inertia);

            _isDragging = false;
            _inertia = 0f;
        }

        void OnScroll(float delta)
        {
            onScroll?.Invoke(delta);
        }

        void LateUpdate()
        {
            if (Helpers.IsZeroValue(_inertia))
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

        public void OnInitializePotentialDrag (PointerEventData eventData)
        {
            GetParentHandler<IInitializePotentialDragHandler>()?.OnInitializePotentialDrag(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            bool isShiftX = Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y);
            bool routeToParent = _axis == Axis.Y && isShiftX || _axis == Axis.X && !isShiftX;
            if (routeToParent)
            {
                var parentBeginDragHandler = GetParentHandler<IBeginDragHandler>();
                if (parentBeginDragHandler != null)
                {
                    parentBeginDragHandler.OnBeginDrag(eventData);
                    _parentHandler = (Component)parentBeginDragHandler;
                    return;
                }
            }

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
            {
                if (_parentHandler != null && _parentHandler is IDragHandler parentDragHandler)
                    parentDragHandler.OnDrag(eventData);
                return;
            }

            float delta = GetDeltaPosition(eventData);
            if (!Helpers.IsZeroValue(delta))
                _lastDelta = delta;

            OnScroll(delta);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging)
            {
                if (_parentHandler != null)
                {
                    if (_parentHandler is IEndDragHandler parentEndDragHandler)
                        parentEndDragHandler.OnEndDrag(eventData);
                    _parentHandler = null;
                }
                return;
            }

            float delta = GetDeltaPosition(eventData);
            _inertia = _lastDelta + delta;
            _isDragging = false;

            OnScroll(delta);
        }

        //
        // Helpers
        //

        T GetParentHandler<T>() where T : class, IEventSystemHandler
        {
            Transform parent = transform.parent;
            return parent != null ? parent.GetComponentInParent(typeof(T)) as T : null;
        }

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
