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
        RectTransform _viewportNode;
        RectTransform _contentNode;
        Action _refreshViewportFunc;
        Coroutine _centerOnIndexCo;

        public void Init(ItemsViewport itemsViewport, WidgetsViewport widgetsViewport,
            RectTransform viewportNode, RectTransform contentNode, Action refreshViewportFunc)
        {
            _itemsViewport = itemsViewport;
            _widgetsViewport = widgetsViewport;
            _viewportNode = viewportNode;
            _contentNode = contentNode;
            _refreshViewportFunc = refreshViewportFunc;
        }

        void OnDisable()
        {
            TryFinishScrollProcess();
        }

        public void CenterOnIndex(int index, bool immediate)
        {
            TryFinishScrollProcess();

            onScrollStarted?.Invoke();

            if (!_itemsViewport.IsItemInsideViewport(index))
            {
                _itemsViewport.ResetToIndex(index);
                _widgetsViewport.Reset();
                _refreshViewportFunc();

                // Todo: set position of needed index to appropriate viewport edge
                // ...
            }

            // Todo: consider edges
            int relativeIndex = _itemsViewport.GetItemRelativeIndex(index);
            Vector2 headWorldPosition = _widgetsViewport.GetWidgetWorldPositionByRelativeIndex(relativeIndex);
            Rect viewportWorldRect = Helpers.GetWorldRect(_viewportNode);
            Vector2 viewportWorldCenter = viewportWorldRect.center;

            // Todo: AXIS
            float totalDelta = Helpers.GetVectorComponent((viewportWorldCenter - headWorldPosition) / _contentNode.lossyScale, Axis.Y);

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
