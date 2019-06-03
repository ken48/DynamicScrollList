using System;
using System.Collections;
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
        Action _refreshViewportFunc;
        Coroutine _centerOnIndexCo;

        public void Init(ItemsViewport itemsViewport, WidgetsViewport widgetsViewport, Action refreshViewportFunc)
        {
            _itemsViewport = itemsViewport;
            _widgetsViewport = widgetsViewport;
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
            if (needShift)
            {
                // Set position of needed index to appropriate viewport edge
                int deltaHeadIndex = index - prevHeadIndex;
                ItemsEdge itemsEdgeDirection = deltaHeadIndex < 0 ? ItemsEdge.Head : ItemsEdge.Tail;

                // Todo: move relativeIndex widget outside the viewport
                // ...
            }

            Vector2 widgetWorldPosition = _widgetsViewport.GetWidgetWorldPositionByRelativeIndex(relativeIndex);
            Vector2 viewportWorldCenter = viewportWorldRect.center;
            float totalDelta = _widgetsViewport.GetLocalCoordinate(viewportWorldCenter - widgetWorldPosition);
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
}
