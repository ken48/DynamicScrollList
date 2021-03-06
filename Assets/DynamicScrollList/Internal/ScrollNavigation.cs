﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicScroll.Internal
{
    internal class ScrollNavigation : MonoBehaviour
    {
        const float Duration = 0.33f;

        public event Action<float> onScroll;
        public event Action onScrollStarted;
        public event Action onScrollFinished;

        ItemsViewport _itemsViewport;
        WidgetsViewport _widgetsViewport;
        Axis _axis;
        Action _refreshViewportFunc;
        Coroutine _centerOnIndexCo;

        public void Init(ItemsViewport itemsViewport, WidgetsViewport widgetsViewport, Axis axis, Action refreshViewportFunc)
        {
            _itemsViewport = itemsViewport;
            _widgetsViewport = widgetsViewport;
            _axis = axis;
            _refreshViewportFunc = refreshViewportFunc;
        }

        void OnDisable()
        {
            TryFinishScrollProcess();
        }

        public void CenterOnIndex(int index, Rect viewportWorldRect, bool immediate)
        {
            TryFinishScrollProcess();

            onScrollStarted?.Invoke();

            bool needShift = !_itemsViewport.IsItemInsideViewport(index);
            int prevHeadIndex = _itemsViewport.GetEdgeIndex(ItemsEdge.Head);
            if (needShift)
            {
                _itemsViewport.ResetToIndex(index);
                _widgetsViewport.Reset();
                _refreshViewportFunc();
            }

            int relativeIndex = _itemsViewport.GetItemRelativeIndex(index);
            Rect widgetWorldRect = _widgetsViewport.GetWidgetWorldRectByRelativeIndex(relativeIndex);
            bool alignToEdge = Helpers.GetVectorComponent(widgetWorldRect.size, _axis) >
                Helpers.GetVectorComponent(viewportWorldRect.size, _axis);
            Vector2 viewportWorldPosition = GetRectPosition(viewportWorldRect, alignToEdge);
            Vector2 widgetWorldPosition = GetRectPosition(widgetWorldRect, alignToEdge);
            float shiftDelta = 0f;
            if (needShift)
            {
                // Set position of needed index to appropriate viewport edge
                int deltaHeadIndex = index - prevHeadIndex;
                ItemsEdge itemsEdgeDirection = deltaHeadIndex < 0 ? ItemsEdge.Head : ItemsEdge.Tail;
                float sign = _widgetsViewport.GetInflationSign(itemsEdgeDirection);
                Vector2 shiftWorldSize = (itemsEdgeDirection == ItemsEdge.Head ? widgetWorldRect : viewportWorldRect).size;
                shiftDelta = _widgetsViewport.GetLocalCoordinate(shiftWorldSize) * sign;
                OnScroll(shiftDelta);
            }

            float totalDelta = _widgetsViewport.GetLocalCoordinate(viewportWorldPosition - widgetWorldPosition) - shiftDelta;
            if (immediate)
            {
                OnScroll(totalDelta);
                onScrollFinished?.Invoke();
            }
            else
            {
                _centerOnIndexCo = StartCoroutine(ScrollProcess(totalDelta));
            }
        }

        Vector2 GetRectPosition(Rect rect, bool alignedToEdge)
        {
            return alignedToEdge ? _widgetsViewport.GetRectEdge(rect, AlignmentDesc.AxisAlignment[_axis]) : rect.center;
        }

        IEnumerator ScrollProcess(float totalDelta)
        {
            float time = 0f;
            float prevDelta = 0f;
            while (time < Duration)
            {
                time += Time.unscaledDeltaTime;
                float normalizedTime = Mathf.Clamp01(time / Duration);
                float delta = totalDelta * EasingOutQuad(normalizedTime);
                OnScroll(delta - prevDelta);
                prevDelta = delta;
                yield return null;
            }

            TryFinishScrollProcess();
        }

        void TryFinishScrollProcess()
        {
            if (_centerOnIndexCo == null)
                return;

            StopCoroutine(_centerOnIndexCo);
            _centerOnIndexCo = null;
            onScrollFinished?.Invoke();
        }

        void OnScroll(float delta)
        {
            onScroll?.Invoke(delta);
        }

        static float EasingOutQuad(float t)
        {
            return t * (2f - t);
        }
    }

    static class AlignmentDesc
    {
        public static readonly Dictionary<Axis, WidgetsAlignment> AxisAlignment = new Dictionary<Axis, WidgetsAlignment>
        {
            { Axis.X, WidgetsAlignment.Left },
            { Axis.Y, WidgetsAlignment.Top },
        };
    }
}
